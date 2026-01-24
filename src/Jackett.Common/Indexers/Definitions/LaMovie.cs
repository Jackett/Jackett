using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Jackett.Common.Helpers;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Models.IndexerConfig.Bespoke;
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
        public override string Description => "LaMovie is a semi-private site for movies and TV shows in latin spanish.";
        public override string SiteLink { get; protected set; } = "https://la.movie/";
        public override string Language => "es-419";
        public override string Type => "semi-private";
        private string LoginUrl => SiteLink + "wp-json/wpf/v1/auth/login";

        private Dictionary<string, string> headers = new()
        {
            ["Content-Type"] = "application/json", ["Accept"] = "application/json"
        };

        private ConfigurationDataLaMovie Configuration
        {
            get => (ConfigurationDataLaMovie)configData;
            set => configData = value;
        }

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
            configData: new ConfigurationDataLaMovie())
        {
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var payload = new JObject { ["email"] = Configuration.Email.Value, ["password"] = Configuration.Password.Value }
                .ToString();
            var result = await RequestWithCookiesAndRetryAsync(
                LoginUrl, cookieOverride: CookieHeader, method: RequestType.POST, referer: SiteLink, data: null,
                headers: headers, rawbody: payload);
            var json = JObject.Parse(result.ContentString);
            var token = json.SelectToken("data.token")?.ToString();
            await ConfigureIfOK(
                token, IsAuthorized(token), () =>
                {
                    var contentString = result.ContentString;
                    var json = JObject.Parse(contentString);
                    var errorMessage = json.Value<string>("msg");
                    throw new ExceptionWithConfigData(errorMessage, Configuration);
                });
            return IndexerConfigurationStatus.Completed;
        }

        private bool IsAuthorized(string token) => string.IsNullOrWhiteSpace(token)
            ? throw new ExceptionWithConfigData("Login response did not contain a token", Configuration)
            : true;

        protected new async Task ConfigureIfOK(string token, bool isLoggedin, Func<Task> onError)
        {
            if (isLoggedin)
            {
                headers["Authorization"] = $"Bearer {token}";
                IsConfigured = true;
                SaveConfig();
            }
            else
            {
                await onError();
            }
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchTerm = WebUtilityHelpers.UrlEncode(query.GetQueryString(), Encoding.UTF8);
            var searchUrl = $"{SiteLink}wp-api/v1/search?filter=%7B%7D&postType=any&q={searchTerm}&postsPerPage=26";
            var response = await RequestWithCookiesAndRetryAsync(
                searchUrl, cookieOverride: CookieHeader, method: RequestType.GET, referer: SiteLink, data: null,
                headers: headers);
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
                var link = new Uri($"{SiteLink}wp-api/v1/single/movies?slug={slug}&postType=movies");
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
                headers: headers);
            var jsonDetails = JObject.Parse(details.ContentString);
            var movieId = jsonDetails.SelectToken("data._id")?.ToString();
            var response = await RequestWithCookiesAndRetryAsync(
                $"{SiteLink}wp-api/v1/player?postId={movieId}&demo=0", cookieOverride: CookieHeader, method: RequestType.GET,
                referer: SiteLink, data: null, headers: headers);
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
