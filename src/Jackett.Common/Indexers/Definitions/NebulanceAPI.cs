using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Jackett.Common.Extensions;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Serializer;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;
using WebClient = Jackett.Common.Utils.Clients.WebClient;
using WebRequest = Jackett.Common.Utils.Clients.WebRequest;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class NebulanceAPI : IndexerBase
    {
        public override string Id => "nebulanceapi";
        public override string[] Replaces => new[]
        {
            "transmithenet",
            "nebulance"
        };
        public override string Name => "NebulanceAPI";
        public override string Description => "Nebulance is a Private site. At Nebulance we will change the way you think about TV. Using API.";
        // Status: https://nbl.trackerstatus.info/
        public override string SiteLink { get; protected set; } = "https://nebulance.io/";
        public override string Language => "en-US";
        public override string Type => "private";

        public override bool SupportsPagination => true;

        public override int PageSize => 100;

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        // Docs at https://nebulance.io/articles.php?topic=api_key
        protected virtual int KeyLength => 32;

        // TODO: remove ConfigurationDataAPIKey class and use ConfigurationDataPasskey instead
        private new ConfigurationDataAPIKey configData
        {
            get => (ConfigurationDataAPIKey)base.configData;
            set => base.configData = value;
        }

        public NebulanceAPI(IIndexerConfigurationService configService, WebClient wc, Logger l,
            IProtectionService ps, ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataAPIKey())
        {
            configData.AddDynamic("keyInfo", new DisplayInfoConfigurationItem(string.Empty, "Generate a new key by accessing your account profile settings at <a href=\"https://nebulance.io/\" target=_blank>Nebulance</a>, scroll down to the <b>API Keys</b> section, tick the <i>New Key</i>, <i>list</i> and <i>download</i> checkboxes and save."));
            configData.AddDynamic("loginRequirements", new DisplayInfoConfigurationItem(string.Empty, "You must meet the login requirements: VPN with 2FA or ISP in home country."));
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                LimitsDefault = 100,
                LimitsMax = 1000,
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.TvmazeId
                },
                SupportsRawSearch = true
            };

            caps.Categories.AddCategoryMapping("tv", TorznabCatType.TV, "tv");
            caps.Categories.AddCategoryMapping("sd", TorznabCatType.TVSD, "sd");
            caps.Categories.AddCategoryMapping("hd", TorznabCatType.TVHD, "hd");
            caps.Categories.AddCategoryMapping("uhd", TorznabCatType.TVUHD, "uhd");
            caps.Categories.AddCategoryMapping("4k", TorznabCatType.TVUHD, "4k");
            caps.Categories.AddCategoryMapping("480p", TorznabCatType.TVSD, "480p");
            caps.Categories.AddCategoryMapping("720p", TorznabCatType.TVHD, "720p");
            caps.Categories.AddCategoryMapping("1080p", TorznabCatType.TVHD, "1080p");
            caps.Categories.AddCategoryMapping("1080i", TorznabCatType.TVHD, "1080i");
            caps.Categories.AddCategoryMapping("2160p", TorznabCatType.TVUHD, "2160p");

            return caps;
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new NebulanceAPIRequestGenerator(TorznabCaps, configData, SiteLink, logger);
        }

        public override IParseIndexerResponse GetParser()
        {
            return new NebulanceAPIParser(TorznabCaps.Categories, SiteLink);
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            IsConfigured = false;
            var apiKey = configData.Key;
            if (apiKey.Value.Length != KeyLength)
            {
                throw new Exception($"Invalid API Key configured: expected length: {KeyLength}, got {apiKey.Value.Length}");
            }

            try
            {
                var results = await PerformQuery(new TorznabQuery());

                if (!results.Any())
                {
                    throw new Exception("Testing returned no results!");
                }

                IsConfigured = true;
                SaveConfig();
            }
            catch (Exception e)
            {
                throw new ExceptionWithConfigData(e.Message, configData);
            }

            return IndexerConfigurationStatus.Completed;
        }
    }

    public class NebulanceAPIRequestGenerator : IIndexerRequestGenerator
    {
        private readonly TorznabCapabilities _torznabCaps;
        private readonly ConfigurationDataAPIKey _configData;
        private readonly string _siteLink;
        private readonly Logger _logger;

        public NebulanceAPIRequestGenerator(TorznabCapabilities torznabCaps, ConfigurationDataAPIKey configData, string siteLink, Logger logger)
        {
            _torznabCaps = torznabCaps;
            _configData = configData;
            _siteLink = siteLink;
            _logger = logger;
        }

        public IndexerPageableRequestChain GetSearchRequests(TorznabQuery query)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            var limit = query.Limit;
            if (limit == 0)
            {
                limit = _torznabCaps.LimitsDefault.GetValueOrDefault(100);
            }

            var queryParams = new NameValueCollection
            {
                { "action", "search" },
                { "api_key", _configData.Key.Value },
                { "age", ">0" },
                { "per_page", limit.ToString() },
            };

            if (query.Limit > 0 && query.Offset > 0)
            {
                var page = query.Offset / query.Limit;
                queryParams.Add("page", page.ToString());
            }

            if (query.IsTvmazeQuery && query.TvmazeID.HasValue)
            {
                queryParams.Set("tvmaze", query.TvmazeID.ToString());
            }

            var searchQuery = query.SanitizedSearchTerm.Trim();

            if (searchQuery.IsNotNullOrWhiteSpace())
            {
                queryParams.Set("release", searchQuery);
            }

            if (query.Season.HasValue &&
                query.Episode.IsNotNullOrWhiteSpace() &&
                DateTime.TryParseExact($"{query.Season} {query.Episode}", "yyyy MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var showDate))
            {
                if (searchQuery.IsNotNullOrWhiteSpace())
                {
                    queryParams.Set("name", searchQuery);
                }

                queryParams.Set("release", showDate.ToString("yyyy.MM.dd", CultureInfo.InvariantCulture));
            }
            else
            {
                if (query.Season.HasValue)
                {
                    queryParams.Set("season", query.Season.Value.ToString());
                }

                if (query.Episode.IsNotNullOrWhiteSpace() && int.TryParse(query.Episode, out var episodeNumber))
                {
                    queryParams.Set("episode", episodeNumber.ToString());
                }
            }

            if ((queryParams.Get("season").IsNotNullOrWhiteSpace() || queryParams.Get("episode").IsNotNullOrWhiteSpace()) &&
                queryParams.Get("name").IsNullOrWhiteSpace() &&
                queryParams.Get("release").IsNullOrWhiteSpace() &&
                queryParams.Get("tvmaze").IsNullOrWhiteSpace())
            {
                _logger.Warn("NBL API does not support season calls without name, series, id, imdb, tvmaze, or time keys.");

                return new IndexerPageableRequestChain();
            }

            if (queryParams.Get("name") is { Length: > 0 and < 3 } || queryParams.Get("release") is { Length: > 0 and < 3 })
            {
                _logger.Warn("NBL API does not support release calls that are 2 characters or fewer.");

                return new IndexerPageableRequestChain();
            }

            pageableRequests.Add(GetPagedRequests(queryParams));

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(NameValueCollection parameters)
        {
            var webRequest = new WebRequest
            {
                Url = $"{_siteLink}api.php?{parameters.GetQueryString()}",
                Type = RequestType.GET,
                Headers = new Dictionary<string, string>
                {
                    { "Accept", "application/json" },
                },
                EmulateBrowser = false
            };

            yield return new IndexerRequest(webRequest);
        }
    }

    public class NebulanceAPIParser : IParseIndexerResponse
    {
        private readonly TorznabCapabilitiesCategories _categories;
        private readonly string _siteLink;

        private readonly HashSet<string> _validCategories = new HashSet<string>
        {
            "sd",
            "hd",
            "uhd",
            "4k",
            "480p",
            "720p",
            "1080i",
            "1080p",
            "2160p"
        };

        private readonly HashSet<string> _validTags = new HashSet<string>
        {
            "action",
            "adventure",
            "children",
            "biography",
            "comedy",
            "crime",
            "documentary",
            "drama",
            "family",
            "fantasy",
            "game-show",
            "history",
            "horror",
            "medical",
            "music",
            "musical",
            "mystery",
            "news",
            "reality-tv",
            "romance",
            "sci-fi",
            "sitcom",
            "sport",
            "talk-show",
            "thriller",
            "travel",
            "war",
            "western"
        };

        public NebulanceAPIParser(TorznabCapabilitiesCategories categories, string siteLink)
        {
            _categories = categories;
            _siteLink = siteLink;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            if (indexerResponse.WebResponse.Status != HttpStatusCode.OK)
            {
                throw new Exception($"Unexpected response status '{indexerResponse.WebResponse.Status}' code from indexer request. Check the logs for more information.");
            }

            if (indexerResponse.Content != null && indexerResponse.Content.Contains("Invalid params"))
            {
                throw new Exception("Invalid API Key configured");
            }

            if (indexerResponse.Content != null && indexerResponse.Content.Contains("API is down"))
            {
                throw new Exception("NBL API is down at the moment");
            }

            var releases = new List<ReleaseInfo>();

            NebulanceResponse jsonResponse;

            try
            {
                jsonResponse = STJson.Deserialize<NebulanceResponse>(indexerResponse.Content);
            }
            catch (Exception ex)
            {
                throw new Exception($"Unexpected response from indexer request: {ex.Message}", ex);
            }

            if (jsonResponse.Error != null)
            {
                throw new Exception($"Indexer API call returned an error [{jsonResponse.Error?.Message}]");
            }

            if (jsonResponse.TotalResults == 0 || jsonResponse.Items == null || jsonResponse.Items.Count == 0)
            {
                return releases;
            }

            foreach (var row in jsonResponse.Items)
            {
                var details = new Uri(_siteLink + "torrents.php?id=" + row.TorrentId);

                var title = row.ReleaseTitle.IsNotNullOrWhiteSpace() ? row.ReleaseTitle : row.GroupName;

                var tags = row.Tags.Select(t => t.ToLowerInvariant()).ToList();
                var releaseCategories = _validCategories.Intersect(tags).ToList();

                var descriptions = new List<string>();

                if (row.GroupName.IsNotNullOrWhiteSpace())
                {
                    descriptions.Add("Group Name: " + row.GroupName);
                }

                var genres = _validTags.Intersect(tags).ToList();
                if (genres.Any())
                {
                    descriptions.Add("Genre: " + string.Join(", ", genres));
                }

                var release = new ReleaseInfo
                {
                    Guid = details,
                    Details = details,
                    Link = new Uri(row.DownloadLink),
                    Title = title.Trim(),
                    Category = _categories.MapTrackerCatToNewznab(releaseCategories.FirstOrDefault() ?? "TV"),
                    Size = row.Size,
                    Files = row.FileList.Count(),
                    PublishDate = DateTime.Parse(row.PublishDateUtc, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
                    Grabs = row.Snatch,
                    Seeders = row.Seed,
                    Peers = row.Seed + row.Leech,
                    MinimumRatio = 0, // ratioless
                    MinimumSeedTime = row.Category.ToUpperInvariant() == "SEASON" ? 432000 : 86400, // 120 hours for seasons and 24 hours for episodes
                    DownloadVolumeFactor = 0, // ratioless tracker
                    UploadVolumeFactor = 1,
                    Genres = genres,
                    Description = string.Join("<br />\n", descriptions)
                };

                if (row.TvMazeId.HasValue)
                {
                    release.TVMazeId = row.TvMazeId.Value;
                }

                if (row.Banner.IsNotNullOrWhiteSpace() && !row.Banner.Contains("noimage.png"))
                {
                    release.Poster = new Uri(row.Banner);
                }

                releases.Add(release);
            }

            return releases;
        }
    }

    public class NebulanceResponse
    {
        [JsonPropertyName("total_results")]
        public int TotalResults { get; set; }

        public IReadOnlyCollection<NebulanceTorrent> Items { get; set; }

        public NebulanceErrorMessage Error { get; set; }
    }

    public class NebulanceTorrent
    {
        [JsonPropertyName("rls_name")]
        public string ReleaseTitle { get; set; }

        [JsonPropertyName("cat")]
        public string Category { get; set; }

        public long Size { get; set; }
        public int Seed { get; set; }
        public int Leech { get; set; }
        public int Snatch { get; set; }

        [JsonPropertyName("download")]
        public string DownloadLink { get; set; }

        [JsonPropertyName("file_list")]
        public IEnumerable<string> FileList { get; set; } = Array.Empty<string>();

        [JsonPropertyName("group_name")]
        public string GroupName { get; set; }

        [JsonPropertyName("series_banner")]
        public string Banner { get; set; }

        [JsonPropertyName("group_id")]
        public int TorrentId { get; set; }

        [JsonPropertyName("tvmaze_id")]
        public int? TvMazeId { get; set; }

        [JsonPropertyName("rls_utc")]
        public string PublishDateUtc { get; set; }

        public IEnumerable<string> Tags { get; set; } = Array.Empty<string>();
    }

    public class NebulanceErrorMessage
    {
        public string Message { get; set; }
    }
}
