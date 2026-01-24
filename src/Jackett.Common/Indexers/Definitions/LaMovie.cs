using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jackett.Common.Helpers;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class LaMovie : IndexerBase
    {
        public override string Id => "lamovie";
        public override string Name => "LaMovie";
        public override string Description => "LaMovie is a public site for movies and TV shows in latin spanish.";
        public sealed override string SiteLink { get; protected set; } = "https://la.movie/";
        public override string Language => "es-419";
        public override string Type => "public";

        private readonly Dictionary<string, string> _headers = new()
        {
            ["Content-Type"] = "application/json",
            ["Accept"] = "application/json"
        };

        private string _searchUrl;
        private string _detailsUrl;
        private string _playerUrl;

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities { MovieSearchParams = new() { MovieSearchParam.Q } };
            caps.Categories.AddCategoryMapping(1, TorznabCatType.MoviesHD);
            caps.Categories.AddCategoryMapping(2, TorznabCatType.MoviesUHD);
            return caps;
        }

        public LaMovie(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
                       ICacheService cs) : base(
            configService: configService, client: wc, logger: l, p: ps, cacheService: cs,
            configData: new ())
        {
            var apiLink = $"{SiteLink}wp-api/v1/";
            _searchUrl = $"{apiLink}search?filter=%7B%7D&postType=movies&postsPerPage=26";
            _detailsUrl = $"{apiLink}single/movies?postType=movies";
            _playerUrl = $"{apiLink}player?demo=0";
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var releases = await PerformQuery(new());

            await ConfigureIfOK(string.Empty, releases.Any(), () =>
                                    throw new ("Could not find release from this URL."));

            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchTerm = WebUtilityHelpers.UrlEncode(query.GetQueryString(), Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = "test";
            }
            _searchUrl += $"&q={searchTerm}";
            var response = await RequestWithCookiesAndRetryAsync(
                _searchUrl, cookieOverride: CookieHeader, method: RequestType.GET, referer: SiteLink, data: null,
                headers: _headers);
            var pageReleases = ParseReleases(response, query);
            releases.AddRange(pageReleases);
            return releases;
        }

        private List<ReleaseInfo> ParseReleases(WebResult response, TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var json = JObject.Parse(response.ContentString);
            var posts = json.SelectToken("data.posts")?.ToObject<List<JObject>>();
            if (posts == null)
            {
                return new();
            }

            foreach (var row in posts)
            {
                var images = row.SelectToken("images")?.ToObject<JObject>();
                var poster = images.SelectToken("poster")?.ToString();
                var backdrop = images.SelectToken("poster")?.ToString();
                var logo = images.SelectToken("poster")?.ToString();
                if (poster == null || backdrop == null || logo == null)
                {
                    // skip results without image
                    continue;
                }

                var title = row["title"]?.ToString();
                if (!CheckTitleMatchWords(query.GetQueryString(), title))
                {
                    // skip if it doesn't contain all words
                    continue;
                }

                var year = row.SelectToken("release_date")?.ToString().Split('-')[0];
                var slug = row["slug"]?.ToString();
                var details = new Uri($"{SiteLink}peliculas/{slug}");
                var lastUpdate = row.SelectToken("last_update")?.ToString();
                _detailsUrl += $"&slug={slug}";
                var link = new Uri(_detailsUrl);
                releases.Add(
                    new()
                    {
                        Guid = link,
                        Details = details,
                        Link = link,
                        Title = $"{title}.1080p-Dual-Lat",
                        Category = new List<int> { TorznabCatType.MoviesHD.ID },
                        Poster = new($"{SiteLink}wp-content/uploads{poster}"),
                        Year = long.Parse(year),
                        Size = 2147483648, // 2 GB
                        Files = 1,
                        Seeders = 1,
                        Peers = 2,
                        DownloadVolumeFactor = 0,
                        UploadVolumeFactor = 1,
                        PublishDate = DateTime.Parse(lastUpdate)
                    });
            }

            return releases;
        }

        private static bool CheckTitleMatchWords(string queryStr, string title)
        {
            // this code split the words, remove words with 2 letters or less, remove accents and lowercase
            var queryMatches = Regex.Matches(queryStr, @"\b[\w']*\b");
            var queryWords = from m in queryMatches.Cast<Match>()
                             where !string.IsNullOrEmpty(m.Value) && m.Value.Length > 2
                             select Encoding.UTF8.GetString(Encoding.GetEncoding("ISO-8859-8").GetBytes(m.Value.ToLower()));
            var titleMatches = Regex.Matches(title, @"\b[\w']*\b");
            var titleWords = from m in titleMatches.Cast<Match>()
                             where !string.IsNullOrEmpty(m.Value) && m.Value.Length > 2
                             select Encoding.UTF8.GetString(Encoding.GetEncoding("ISO-8859-8").GetBytes(m.Value.ToLower()));
            titleWords = titleWords.ToArray();
            return queryWords.All(word => titleWords.Contains(word));
        }

        public override async Task<byte[]> Download(Uri link)
        {
            var details = await RequestWithCookiesAndRetryAsync(
                link.AbsoluteUri, cookieOverride: CookieHeader, method: RequestType.GET, referer: SiteLink, data: null,
                headers: _headers);
            var jsonDetails = JObject.Parse(details.ContentString);
            var movieId = jsonDetails.SelectToken("data._id")?.ToString();
            _playerUrl += $"&postId={movieId}";
            var response = await RequestWithCookiesAndRetryAsync(
                _playerUrl, cookieOverride: CookieHeader, method: RequestType.GET,
                referer: SiteLink, data: null, headers: _headers);
            var jsonDownloads = JObject.Parse(response.ContentString);
            var downloadUrls = jsonDownloads.SelectToken("data.downloads")?.ToList();
            var magnetUrl = downloadUrls?.FirstOrDefault(x => (bool)x.SelectToken("url")?.ToString().Contains("magnet"))
                                        ?.SelectToken("url");
            if (magnetUrl == null)
            {
                throw new("No magnet URL found");
            }

            return await base.Download(new(magnetUrl.ToString()));
        }
    }
}
