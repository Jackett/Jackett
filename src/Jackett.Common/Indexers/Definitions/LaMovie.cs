using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jackett.Common.Helpers;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
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
        private readonly string _episodesUrl;

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q
                },
                SupportsRawSearch = true,
            };
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
            var maxEpisodes = new ConfigurationData.StringConfigurationItem("Max latest episodes per series (only when query is empty; last 7 days, 0=unlimited)")
            {
                Value = "5"
            };
            configData.AddDynamic("MaxEpisodesPerSeries", maxEpisodes);

            var apiLink = $"{SiteLink}wp-api/v1/";
            _searchUrl = $"{apiLink}search?filter=%7B%7D&postType={{0}}&postsPerPage={ReleasesPerPage}";
            _latestUrl =
                $"{apiLink}listing/{{0}}?filter=%7B%7D&page=1&orderBy=latest&order=DESC&postType={{0}}&postsPerPage=5";
            _detailsUrl = $"{apiLink}single/{{0}}?postType={{0}}";
            _playerUrl = $"{apiLink}player?demo=0";
            _episodesUrl = $"{apiLink}single/episodes/list";
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
            var rawSearchTerm = query.GetQueryString()?.Trim();
            int? searchYear = null;

            // Extract year from search term if present
            if (!string.IsNullOrWhiteSpace(rawSearchTerm))
            {
                var yearMatch = Regex.Match(rawSearchTerm, @"\b(19\d{2}|20\d{2})\b");
                if (yearMatch.Success)
                {
                    searchYear = int.Parse(yearMatch.Value);
                    // Remove year from search term
                    rawSearchTerm = rawSearchTerm.Replace(yearMatch.Value, "").Trim();
                }
            }

            // Remove episode patterns from search term (e.g., "Show Name S01E01" -> "Show Name")
            // but keep the episode info in query object for filtering
            if (!string.IsNullOrWhiteSpace(rawSearchTerm))
            {
                // Remove patterns like S01E01, s01e01
                rawSearchTerm = Regex.Replace(rawSearchTerm, @"\s+[Ss]\d{1,2}[Ee]\d{1,2}$", "").Trim();
                // Remove patterns like 1x01, 1X01
                rawSearchTerm = Regex.Replace(rawSearchTerm, @"\s+\d{1,2}[xX]\d{1,2}$", "").Trim();
            }

            // Limit search term to 16 characters to match the website's search API behavior for better title matching
            // The website uses: const Fe = xe.substring(0, 16);
            if (!string.IsNullOrWhiteSpace(rawSearchTerm) && rawSearchTerm.Length > 16)
            {
                rawSearchTerm = rawSearchTerm.Substring(0, 16);
            }

            var searchTerm = !string.IsNullOrWhiteSpace(rawSearchTerm) ? Uri.EscapeDataString(rawSearchTerm) : string.Empty;
            var isLatest = string.IsNullOrWhiteSpace(rawSearchTerm);

            if (!isLatest && rawSearchTerm.Length < 3)
            {
                var msg = $"Search term must have at least 3 characters. Used search term: '{rawSearchTerm}' (length {rawSearchTerm.Length}).";

                return query.InteractiveSearch
                    ? throw new IndexerException(this, msg)
                    : releases;
            }

            // Determine postType(s) based on categories
            var postTypes = new List<string>();
            if (query.Categories.Length > 0)
            {
                var categories = query.Categories;

                var wantsMovies = categories.Any(c => c == TorznabCatType.Movies.ID ||
                                                     c == TorznabCatType.MoviesHD.ID ||
                                                     c == TorznabCatType.MoviesUHD.ID);
                var wantsTvShows = categories.Any(c => c == TorznabCatType.TV.ID || c == TorznabCatType.TVHD.ID);
                var wantsAnimes = categories.Any(c => c == TorznabCatType.TV.ID || c == TorznabCatType.TVAnime.ID);

                if (wantsMovies)
                {
                    postTypes.Add("movies");
                }

                if (wantsTvShows)
                {
                    postTypes.Add("tvshows");
                }

                if (wantsAnimes)
                {
                    postTypes.Add("animes");
                }
            }

            if (postTypes.Count == 0)
            {
                postTypes.Add("movies");
                postTypes.Add("tvshows");
                postTypes.Add("animes");
            }

            var seenGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var postType in postTypes)
            {
                List<ReleaseInfo> pageReleases = new List<ReleaseInfo>();

                if (!isLatest)
                {
                    // Progressive fallback strategy for Spanish title matching:
                    // 1. Try full search term
                    // 2. If no results, try first 2 words
                    // 3. If still no results, try first word only
                    // This allows matching English titles against Spanish content using original_title field
                    var searchTerms = GetProgressiveFallbackTerms(rawSearchTerm);

                    foreach (var term in searchTerms)
                    {
                        var searchUrl = string.Format(_searchUrl, postType) + $"&q={Uri.EscapeDataString(term)}";
                        var response = await RequestWithCookiesAndRetryAsync(
                            searchUrl, cookieOverride: CookieHeader, method: RequestType.GET, referer: SiteLink, data: null,
                            headers: _headers);

                        // Check if API returned error
                        if (response.ContentString.Contains("\"error\":true"))
                        {
                            continue; // Try next fallback term
                        }

                        pageReleases = await ParseReleasesAsync(response, query, isLatest, term, rawSearchTerm, searchYear);

                        if (pageReleases.Any())
                        {
                            break; // Found results, stop trying
                        }
                    }
                }
                else
                {
                    // Latest releases, no search term
                    var latestUrl = string.Format(_latestUrl, postType);
                    var response = await RequestWithCookiesAndRetryAsync(
                        latestUrl, cookieOverride: CookieHeader, method: RequestType.GET, referer: SiteLink, data: null,
                        headers: _headers);
                    pageReleases = await ParseReleasesAsync(response, query, isLatest, rawSearchTerm, rawSearchTerm, searchYear);
                }

                foreach (var release in pageReleases)
                {
                    if (release?.Guid == null)
                    {
                        continue;
                    }

                    var guidKey = release.Guid.AbsoluteUri;
                    if (seenGuids.Add(guidKey))
                    {
                        releases.Add(release);
                    }
                }
            }

            return releases;
        }

        private async Task<List<ReleaseInfo>> ParseReleasesAsync(WebResult response, TorznabQuery query, bool isLatest, string searchTerm, string originalSearchTerm, int? searchYear)
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

                // Filter by year if specified in search query
                if (searchYear.HasValue && !string.IsNullOrWhiteSpace(post.ReleaseDate))
                {
                    var postYear = post.ReleaseDate.Split('-')[0];
                    if (int.TryParse(postYear, out var postYearInt) && postYearInt != searchYear.Value)
                    {
                        continue; // Skip if year doesn't match
                    }
                }

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    // Match against both Spanish title and original English title
                    // This allows finding content when searching for English titles
                    var matchesTitle = CheckTitleMatchWords(searchTerm, post.Title, originalSearchTerm);
                    var matchesOriginalTitle = !string.IsNullOrWhiteSpace(post.OriginalTitle) &&
                                               CheckTitleMatchWords(searchTerm, post.OriginalTitle, originalSearchTerm);

                    if (!matchesTitle && !matchesOriginalTitle)
                    {
                        // skip if it doesn't match either title
                        continue;
                    }
                }

                var year = post.ReleaseDate?.Split('-')[0];
                var slugType = post.Type switch
                {
                    "movies" => "peliculas/",
                    "tvshows" => "series/",
                    "animes" => "animes/",
                    _ => ""
                };
                var details = new Uri($"{SiteLink}{slugType}{post.Slug}");
                var detailsUrl = string.Format(_detailsUrl, post.Type) + $"&slug={post.Slug}";
                var link = new Uri(detailsUrl);
                var downloadUrls = await GetDownloadUrlsAsync(link, post.Type, isLatest);

                // Filter by episode if specified
                var filteredDownloadUrls = downloadUrls;
                if (query.Season > 0 || !string.IsNullOrWhiteSpace(query.Episode))
                {
                    filteredDownloadUrls = downloadUrls.Where(downloadUrl =>
                    {
                        if (string.IsNullOrWhiteSpace(downloadUrl.Episode))
                        {
                            return false;
                        }

                        // Parse episode tag (e.g., "S01E01")
                        var episodeMatch = Regex.Match(downloadUrl.Episode, @"S(\d{1,2})E(\d{1,2})", RegexOptions.IgnoreCase);
                        if (!episodeMatch.Success)
                        {
                            return false;
                        }

                        var season = int.Parse(episodeMatch.Groups[1].Value);
                        var episode = int.Parse(episodeMatch.Groups[2].Value);

                        // Match season and episode
                        var seasonMatch = query.Season == 0 || query.Season == season;
                        var episodeMatch2 = string.IsNullOrWhiteSpace(query.Episode) || query.Episode == episode.ToString();

                        return seasonMatch && episodeMatch2;
                    }).ToList();
                }

                releases.AddRange(filteredDownloadUrls.Select(downloadUrl =>
                {
                    var uriMagnet = new Uri(downloadUrl.Url);
                    var categories = post.Type switch
                    {
                        "movies" => downloadUrl.Quality.Contains("4K")
                            ? new List<int> { TorznabCatType.MoviesUHD.ID }
                            : new List<int> { TorznabCatType.MoviesHD.ID },
                        "tvshows" => new List<int> { TorznabCatType.TVHD.ID },
                        "animes" => new List<int> { TorznabCatType.TVAnime.ID },
                        _ => new List<int> { TorznabCatType.Other.ID }
                    };

                    var episodePart = downloadUrl.Episode != null ? $"{downloadUrl.Episode}." : "";

                    //Radarr parsing
                    var quality = Regex.Replace(
                        downloadUrl.Quality.ToUpper(),
                        @"\bDUAL\b\s*|\bHD\b|\b4K\b",
                        m => m.Value.Trim() switch
                        {
                            "DUAL" => string.Empty,
                            "HD" => "1080p",
                            "4K" => "2160p",
                            _ => m.Value
                        });

                    var language = Regex.Replace(
                        downloadUrl.Language.Replace("/", "."),
                        @"\bInglés\b|\bCastellano\b",
                        m => m.Value switch
                        {
                            "Inglés" => "English",
                            "Castellano" => "Spanish",
                            _ => m.Value
                        });

                    return new ReleaseInfo
                    {
                        Guid = uriMagnet,
                        Details = details,
                        Link = uriMagnet,
                        Title = $"{post.Title}.{episodePart}{quality}.{language}-LaMovie",
                        Category = categories,
                        Poster = new($"{SiteLink}wp-content/uploads{post.Images.Poster}"),
                        Year = year != null ? long.Parse(year) : DateTime.Now.Year,
                        Size = downloadUrl.Size != null ? ParseUtil.GetBytes(downloadUrl.Size) : 2.Gigabytes(),
                        Seeders = 1,
                        Peers = 1,
                        DownloadVolumeFactor = 0,
                        UploadVolumeFactor = 1,
                        PublishDate = DateTime.Parse(post.LastUpdate)
                    };
                }));
            }

            return releases;
        }

        /// <summary>
        /// Generates progressive fallback search terms for Spanish title matching.
        /// When searching with English titles against Spanish content, this method
        /// creates increasingly shorter search terms to improve match probability.
        /// </summary>
        private static List<string> GetProgressiveFallbackTerms(string searchTerm)
        {
            var terms = new List<string>();
            var words = searchTerm.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (words.Length == 0)
            {
                return terms;
            }

            // Strategy 1: Try with full term (already truncated to 16 chars)
            terms.Add(searchTerm);

            // Strategy 2: Try with first 2 words
            // Helps when full term doesn't match but partial does
            if (words.Length >= 2)
            {
                terms.Add(string.Join(" ", words.Take(2)));
            }

            // Strategy 3: Try with just first word
            // Last resort for very specific titles
            if (words.Length >= 1 && words[0].Length >= 3)
            {
                terms.Add(words[0]);
            }

            return terms;
        }

        private static bool CheckTitleMatchWords(string queryStr, string title, string originalQuery)
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
            
            var queryWordsList = queryWords.ToList();
            
            // If using fallback (queryStr is shorter than originalQuery), be more strict
            // Require at least 2 matching words to avoid false positives
            var originalWords = Regex.Matches(originalQuery, @"\b[\w']+\b").Cast<Match>()
                .Select(m => Encoding.UTF8.GetString(Encoding.GetEncoding("ISO-8859-8").GetBytes(m.Value.ToLower())))
                .Where(w => w.Length > 2 && !Regex.IsMatch(w, @"^\d{4}$")) // Exclude years (4-digit numbers)
                .ToList();
            
            var originalWordCount = originalWords.Count;
            
            if (queryWordsList.Count < originalWordCount)
            {
                // Using fallback: require matching based on original query size
                var matchCount = originalWords.Count(word => titleWords.Contains(word));
                
                if (originalWordCount == 2)
                {
                    // For 2-word queries, require both words to match
                    return matchCount == 2;
                }
                else if (originalWordCount > 2)
                {
                    // For longer queries, require at least 2 words to match
                    return matchCount >= 2;
                }
            }
            
            return queryWordsList.All(word => titleWords.Contains(word));
        }

        private async Task<List<Download>> GetDownloadUrlsAsync(Uri link, string postType, bool isLatest)
        {
            var details = await RequestWithCookiesAndRetryAsync(
                link.AbsoluteUri, cookieOverride: CookieHeader, method: RequestType.GET, referer: SiteLink, data: null,
                headers: _headers);
            var detailsResponse = JsonSerializer.Deserialize<DetailsResponse>(details.ContentString);
            var postId = (int)detailsResponse.Data.Id;
            if (postType is "tvshows" or "animes")
            {
                return await GetMultiplePostDownloadUrls(postId, isLatest: isLatest);
            }

            return await GetSinglePostDownloadUrls(postId);
        }

        private async Task<List<Download>> GetMultiplePostDownloadUrls(
            int postId, int seasonNumber = 1, bool isLatest = false)
        {
            var targetCount = int.MaxValue;
            var cutoffDate = DateTime.MinValue;
            if (isLatest)
            {
                var maxEpisodesSetting = ((ConfigurationData.StringConfigurationItem)configData.GetDynamic("MaxEpisodesPerSeries")).Value;
                var maxEpisodesPerSeries = string.IsNullOrWhiteSpace(maxEpisodesSetting) ? 5 : ParseUtil.CoerceInt(maxEpisodesSetting);
                targetCount = maxEpisodesPerSeries > 0 ? maxEpisodesPerSeries : int.MaxValue;
                cutoffDate = DateTime.Now.AddDays(-7);
            }

            var magnets = new List<Download>();
            var allEpisodes = new List<PostData>();

            var perPage = 15;
            var seasonsResponse = await GetEpisodesResponse(postId, seasonNumber: seasonNumber, maxEpisodes: perPage, page: 1);
            var seasons = seasonsResponse?.Data?.Seasons;
            if (seasons == null || seasons.Count == 0)
            {
                return new();
            }

            var seasonNumbers = seasons
                .Select(s => ParseUtil.CoerceInt(s))
                .Where(s => s > 0)
                .Distinct()
                .OrderByDescending(s => s)
                .ToList();

            var seenEpisodeIds = new HashSet<int>();
            foreach (var season in seasonNumbers)
            {
                if (allEpisodes.Count >= targetCount)
                {
                    break;
                }

                var seasonMeta = await GetEpisodesResponse(postId, seasonNumber: season, maxEpisodes: perPage, page: 1);
                var lastPage = seasonMeta?.Data?.Pagination?.LastPage ?? 1;
                if (lastPage < 1)
                {
                    lastPage = 1;
                }

                for (var page = lastPage; page >= 1 && allEpisodes.Count < targetCount; page--)
                {
                    var pageResponse = await GetEpisodesResponse(postId, seasonNumber: season, maxEpisodes: perPage, page: page);
                    var pagePosts = pageResponse?.Data?.Posts;
                    if (pagePosts == null || pagePosts.Count == 0)
                    {
                        continue;
                    }

                    if (isLatest)
                    {
                        var sortedEpisodes = pagePosts
                            .Select(p => new
                            {
                                Episode = p,
                                ParsedDate = DateTime.Parse(p.Date, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal)
                            })
                            .OrderByDescending(p => p.ParsedDate)
                            .ThenByDescending(p => p.Episode.EpisodeNumber)
                            .ToList();

                        if (sortedEpisodes.Count > 0 && sortedEpisodes[0].ParsedDate < cutoffDate)
                        {
                            break;
                        }

                        foreach (var item in sortedEpisodes)
                        {
                            if (allEpisodes.Count >= targetCount)
                            {
                                break;
                            }

                            if (item.ParsedDate < cutoffDate)
                            {
                                continue;
                            }

                            var episode = item.Episode;
                            if (seenEpisodeIds.Add(episode.Id))
                            {
                                allEpisodes.Add(episode);
                            }
                        }
                    }
                    else
                    {
                        foreach (var episode in pagePosts.OrderByDescending(p => p.EpisodeNumber))
                        {
                            if (seenEpisodeIds.Add(episode.Id))
                            {
                                allEpisodes.Add(episode);
                            }
                        }
                    }
                }
            }

            foreach (var episode in allEpisodes)
            {
                var episodeTag = $"S{episode.SeasonNumber:00}E{episode.EpisodeNumber:00}";

                var episodeDownloadUrls = await GetSinglePostDownloadUrls(episode.Id);
                magnets.AddRange(episodeDownloadUrls.Select(d =>
                {
                    d.Episode = episodeTag;
                    return d;
                }));
            }

            return magnets;
        }

        private async Task<EpisodesResponse> GetEpisodesResponse(int postId, int seasonNumber, int maxEpisodes, int page = 1)
        {
            var episodesUrl = $"{_episodesUrl}?page={page}&_id={postId}&postsPerPage={maxEpisodes}&season={seasonNumber}";
            var response = await RequestWithCookiesAndRetryAsync(
                episodesUrl, cookieOverride: CookieHeader, method: RequestType.GET, referer: SiteLink, data: null,
                headers: _headers);
            var episodesResponse = JsonSerializer.Deserialize<EpisodesResponse>(response.ContentString);
            return episodesResponse;
        }

        private async Task<List<Download>> GetSinglePostDownloadUrls(int postId)
        {
            var playerUrl = $"{_playerUrl}&postId={postId}";
            var response = await RequestWithCookiesAndRetryAsync(
                playerUrl, cookieOverride: CookieHeader, method: RequestType.GET, referer: SiteLink, data: null,
                headers: _headers);
            var playerResponse = JsonSerializer.Deserialize<PlayerResponse>(response.ContentString);
            return playerResponse?.Data?.Downloads?.Where(x => x.Url.StartsWith("magnet:?xt=")).ToList() ??
                   new List<Download>();
        }

        public class ApiResponse
        {
            [JsonPropertyName("data")]
            public DataProp Data { get; set; }
        }

        public class DataProp
        {
            [JsonPropertyName("posts")]
            public List<Post> Posts { get; set; }
        }

        public class Post
        {
            [JsonPropertyName("title")]
            public string Title { get; set; }
            [JsonPropertyName("original_title")]
            public string OriginalTitle { get; set; }

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
        }

        public class ImagesData
        {
            [JsonPropertyName("poster")]
            public string Poster { get; set; }

            [JsonPropertyName("backdrop")]
            public string Backdrop { get; set; }

            [JsonPropertyName("logo")]
            public string Logo { get; set; }
        }

        public class DetailsResponse
        {
            [JsonPropertyName("data")]
            public DetailsData Data { get; set; }
        }

        public class DetailsData
        {
            private int _id;
            [JsonPropertyName("_id")]
            public object Id
            {
                get => _id;
                set => _id = int.Parse(value.ToString());
            }
        }

        public class PlayerResponse
        {
            [JsonPropertyName("data")]
            public PlayerData Data { get; set; }
        }

        public class PlayerData
        {
            [JsonPropertyName("downloads")]
            public List<Download> Downloads { get; set; }
        }

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

            [JsonPropertyName("episode")]
            public string Episode { get; set; }
        }

        public class EpisodesResponse
        {
            [JsonPropertyName("data")]
            public EpisodesData Data { get; set; }
        }

        public class EpisodesData
        {
            [JsonPropertyName("posts")]
            public List<PostData> Posts { get; set; }

            [JsonPropertyName("seasons")]
            public List<string> Seasons { get; set; }

            [JsonPropertyName("pagination")]
            public PaginationData Pagination { get; set; }
        }

        public class PostData
        {
            [JsonPropertyName("_id")]
            public int Id { get; set; }

            [JsonPropertyName("season_number")]
            public int SeasonNumber { get; set; }

            [JsonPropertyName("episode_number")]
            public int EpisodeNumber { get; set; }

            [JsonPropertyName("date")]
            public string Date { get; set; }
        }

        public class PaginationData
        {
            [JsonPropertyName("current_page")]
            public int CurrentPage { get; set; }

            [JsonPropertyName("last_page")]
            public int LastPage { get; set; }

            [JsonPropertyName("total")]
            public int Total { get; set; }

            [JsonPropertyName("per_page")]
            public int PerPage { get; set; }
        }
    }
}
