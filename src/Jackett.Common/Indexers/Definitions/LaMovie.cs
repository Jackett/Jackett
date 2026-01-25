using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jackett.Common.Helpers;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
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
        public override string Description => "LaMovie is a Public tracker for MOVIES / TV in Latin Spanish.";
        public sealed override string SiteLink { get; protected set; } = "https://la.movie/";
        public override string Language => "es-419";
        public override string Type => "public";

        private const int ReleasesPerPage = 30;

        private readonly Dictionary<string, string> _headers = new()
        {
            ["Content-Type"] = "application/json",
            ["Accept"] = "application/json"
        };

        private readonly string _searchUrl;
        private readonly string _latestUrl;
        private readonly string _detailsUrl;
        private readonly string _playerUrl;

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities { MovieSearchParams = new() { MovieSearchParam.Q } };
            caps.Categories.AddCategoryMapping(1, TorznabCatType.MoviesHD);
            caps.Categories.AddCategoryMapping(2, TorznabCatType.MoviesUHD);
            caps.Categories.AddCategoryMapping(3, TorznabCatType.TVHD);
            caps.Categories.AddCategoryMapping(4, TorznabCatType.TVAnime);
            return caps;
        }

        public LaMovie(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
                       ICacheService cs) : base(
            configService: configService, client: wc, logger: l, p: ps, cacheService: cs, configData: new())
        {
            var apiLink = $"{SiteLink}wp-api/v1/";

            _searchUrl = $"{apiLink}search?filter=%7B%7D&postType=any&postsPerPage={ReleasesPerPage}";
            _latestUrl =
                $"{apiLink}listing/movies?filter=%7B%7D&page=1&orderBy=latest&order=DESC&postType=any&postsPerPage=5";
            _detailsUrl = $"{apiLink}single/{{0}}?postType={{0}}";
            _playerUrl = $"{apiLink}player?demo=0";
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var releases = await PerformQuery(new());
            await ConfigureIfOK(string.Empty, releases.Any(), () => throw new("Could not find release from this URL."));
            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchTerm = WebUtilityHelpers.UrlEncode(query.GetQueryString(), Encoding.UTF8);

            var releasesUrl = !string.IsNullOrWhiteSpace(searchTerm) ? $"{_searchUrl}&q={searchTerm}" : _latestUrl;

            var response = await RequestWithCookiesAndRetryAsync(
                releasesUrl, cookieOverride: CookieHeader, method: RequestType.GET, referer: SiteLink, data: null,
                headers: _headers);
            var pageReleases = await ParseReleasesAsync(response, query);
            releases.AddRange(pageReleases);
            return releases;
        }

        private async Task<List<ReleaseInfo>> ParseReleasesAsync(WebResult response, TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse>(response.ContentString);
            var posts = apiResponse?.Data?.Posts;
            if (posts == null)
            {
                return new();
            }

            foreach (var post in posts)
            {
                if (post.Images?.Poster == null || post.Images?.Backdrop == null || post.Images?.Logo == null)
                {
                    // skip results without image
                    continue;
                }

                if (!CheckTitleMatchWords(query.GetQueryString(), post.Title))
                {
                    // skip if it doesn't contain all words
                    continue;
                }

                var year = post.ReleaseDate?.Split('-')[0];
                var slugType = post.Type switch
                {
                    "movies" => "peliculas/",
                    "tvshows" => "series/",
                    "anime" => "animes/",
                    _ => ""
                };

                var details = new Uri($"{SiteLink}{slugType}{post.Slug}");
                var detailsUrl = string.Format(_detailsUrl, post.Type) + $"&slug={post.Slug}";
                var link = new Uri(detailsUrl);
                var downloadUrls = await GetDownloadUrlsAsync(link);
                releases.AddRange(
                    from downloadUrl in downloadUrls
                    let uriMagnet = new Uri(downloadUrl.Url)
                    let categories =
                        downloadUrl.Quality.Contains("4K")
                            ? new List<int> { TorznabCatType.MoviesUHD.ID }
                            : new List<int> { TorznabCatType.MoviesHD.ID }
                    select new ReleaseInfo()
                    {
                        Guid = uriMagnet,
                        Details = details,
                        Link = uriMagnet,
                        Title = $"{post.Title}.{downloadUrl.Quality}.{downloadUrl.Language}",
                        Category = categories,
                        Poster = new($"{SiteLink}wp-content/uploads{post.Images.Poster}"),
                        Year = year != null ? long.Parse(year) : DateTime.Now.Year,
                        Size = downloadUrl.Size != null ? ParseUtil.GetBytes(downloadUrl.Size) : 2147483648, //2GB
                        Files = 1,
                        Seeders = 1,
                        Peers = 2,
                        DownloadVolumeFactor = 0,
                        UploadVolumeFactor = 1,
                        PublishDate = DateTime.Parse(post.LastUpdate)
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

        private async Task<List<PlayerResponse.PlayerData.Download>> GetDownloadUrlsAsync(Uri link)
        {
            var details = await RequestWithCookiesAndRetryAsync(
                link.AbsoluteUri, cookieOverride: CookieHeader, method: RequestType.GET, referer: SiteLink, data: null,
                headers: _headers);
            var detailsResponse = JsonSerializer.Deserialize<DetailsResponse>(details.ContentString);
            var movieId = detailsResponse?.Data?.Id;
            var playerUrl = $"{_playerUrl}&postId={movieId}";
            var response = await RequestWithCookiesAndRetryAsync(
                playerUrl, cookieOverride: CookieHeader, method: RequestType.GET, referer: SiteLink, data: null,
                headers: _headers);
            var playerResponse = JsonSerializer.Deserialize<PlayerResponse>(response.ContentString);
            return playerResponse?.Data?.Downloads?.Where(x => x.Url.Contains("magnet")).ToList() ??
                   new List<PlayerResponse.PlayerData.Download>();
        }

        public class ApiResponse
        {
            [JsonPropertyName("data")]
            public DataProp Data { get; set; }

            public class DataProp
            {
                [JsonPropertyName("posts")]
                public List<Post> Posts { get; set; }

                public class Post
                {
                    [JsonPropertyName("title")]
                    public string Title { get; set; }

                    [JsonPropertyName("slug")]
                    public string Slug { get; set; }

                    [JsonPropertyName("type")]
                    public string Type { get; set; }

                    [JsonPropertyName("release_date")]
                    public string ReleaseDate { get; set; }

                    [JsonPropertyName("last_update")]
                    public string LastUpdate { get; set; }

                    [JsonPropertyName("images")]
                    public ImagesData Images { get; set; }

                    public class ImagesData
                    {
                        [JsonPropertyName("poster")]
                        public string Poster { get; set; }

                        [JsonPropertyName("backdrop")]
                        public string Backdrop { get; set; }

                        [JsonPropertyName("logo")]
                        public string Logo { get; set; }
                    }
                }
            }
        }

        public class DetailsResponse
        {
            [JsonPropertyName("data")]
            public DetailsData Data { get; set; }

            public class DetailsData
            {
                [JsonPropertyName("_id")]
                public int Id { get; set; }
            }
        }

        public class PlayerResponse
        {
            [JsonPropertyName("data")]
            public PlayerData Data { get; set; }

            public class PlayerData
            {
                [JsonPropertyName("downloads")]
                public List<Download> Downloads { get; set; }

                public class Download
                {
                    [JsonPropertyName("url")]
                    public string Url { get; set; }

                    [JsonPropertyName("quality")]
                    public string Quality { get; set; }

                    [JsonPropertyName("lang")]
                    public string Language { get; set; }

                    [JsonPropertyName("size")]
                    public string Size { get; set; }
                }
            }
        }
    }
}
