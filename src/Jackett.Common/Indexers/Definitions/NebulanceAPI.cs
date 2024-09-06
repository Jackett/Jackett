using System;
using System.Collections.Generic;
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
        public override string Description => "At Nebulance we will change the way you think about TV. Using API.";
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

            var offset = query.Offset;
            var limit = query.Limit;
            if (limit == 0)
            {
                limit = _torznabCaps.LimitsDefault.GetValueOrDefault(100);
            }

            var queryParams = new NebulanceQuery
            {
                Age = ">0"
            };

            if (query.IsTvmazeQuery && query.TvmazeID.HasValue)
            {
                queryParams.TvMaze = query.TvmazeID;
            }

            var searchQuery = query.SanitizedSearchTerm.Trim();

            if (searchQuery.IsNotNullOrWhiteSpace())
            {
                queryParams.Release = searchQuery;
            }

            if (query.Season.HasValue &&
                query.Episode.IsNotNullOrWhiteSpace() &&
                DateTime.TryParseExact($"{query.Season} {query.Episode}", "yyyy MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var showDate))
            {
                if (searchQuery.IsNotNullOrWhiteSpace())
                {
                    queryParams.Name = searchQuery;
                }

                queryParams.Release = showDate.ToString("yyyy.MM.dd", CultureInfo.InvariantCulture);
            }
            else
            {
                if (query.Season.HasValue)
                {
                    queryParams.Season = query.Season.Value;
                }

                if (query.Episode.IsNotNullOrWhiteSpace() && int.TryParse(query.Episode, out var episodeNumber))
                {
                    queryParams.Episode = episodeNumber;
                }
            }

            if ((queryParams.Season.HasValue || queryParams.Episode.HasValue) &&
                queryParams.Name.IsNullOrWhiteSpace() &&
                queryParams.Release.IsNullOrWhiteSpace() &&
                !queryParams.TvMaze.HasValue)
            {
                _logger.Debug("NBL API does not support season calls without name, series, id, imdb, tvmaze, or time keys.");

                return new IndexerPageableRequestChain();
            }

            if (queryParams.Name is { Length: > 0 and < 3 } || queryParams.Release is { Length: > 0 and < 3 })
            {
                _logger.Debug("NBL API does not support release calls that are 2 characters or fewer.");

                return new IndexerPageableRequestChain();
            }

            pageableRequests.Add(GetPagedRequests(queryParams, limit, offset));

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(NebulanceQuery parameters, int limit, int offset)
        {
            var webRequest = new WebRequest
            {
                Url = _siteLink + "api.php",
                Type = RequestType.POST,
                Headers = new Dictionary<string, string>
                {
                    { "Accept", "application/json-rpc, application/json" },
                    { "Content-Type", "application/json-rpc" }
                },
                RawBody = JsonRpcRequest("getTorrents", new JArray
                {
                    new JValue(_configData.Key.Value),
                    JObject.FromObject(parameters),
                    new JValue(limit),
                    new JValue(offset)
                }),
                EmulateBrowser = false
            };

            yield return new IndexerRequest(webRequest);
        }

        private string JsonRpcRequest(string method, JArray parameters)
        {
            dynamic request = new JObject();
            request["jsonrpc"] = "2.0";
            request["method"] = method;
            request["params"] = parameters;
            request["id"] = Guid.NewGuid().ToString().Substring(0, 8);
            return request.ToString();
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
                STJson.TryDeserialize<NebulanceRpcResponse<NebulanceErrorResponse>>(indexerResponse.Content, out var errorResponse);

                throw new Exception($"Unexpected response status '{indexerResponse.WebResponse.Status}' code from indexer request: {errorResponse?.Result?.Error?.Message ?? "Check the logs for more information."}");
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

            NebulanceRpcResponse<NebulanceResponse> jsonResponse;

            try
            {
                jsonResponse = STJson.Deserialize<NebulanceRpcResponse<NebulanceResponse>>(indexerResponse.Content);
            }
            catch (Exception ex)
            {
                throw new Exception($"Unexpected response from indexer request: {ex.Message}", ex);
            }

            if (jsonResponse.Error != null || jsonResponse.Result == null)
            {
                throw new Exception($"Indexer API call returned an error [{jsonResponse.Error}]");
            }

            if (jsonResponse.Result?.Items == null || jsonResponse.Result.Items.Count == 0)
            {
                return releases;
            }

            var rows = jsonResponse.Result.Items;

            foreach (var row in rows)
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
                    Link = new Uri(row.Download),
                    Title = title.Trim(),
                    Category = _categories.MapTrackerCatToNewznab(releaseCategories.FirstOrDefault() ?? "TV"),
                    Size = ParseUtil.CoerceLong(row.Size),
                    Files = row.FileList.Count(),
                    PublishDate = DateTime.Parse(row.PublishDateUtc, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
                    Grabs = ParseUtil.CoerceInt(row.Snatch),
                    Seeders = ParseUtil.CoerceInt(row.Seed),
                    Peers = ParseUtil.CoerceInt(row.Seed) + ParseUtil.CoerceInt(row.Leech),
                    MinimumRatio = 0, // ratioless
                    MinimumSeedTime = row.Category.ToLower() == "season" ? 432000 : 86400, // 120 hours for seasons and 24 hours for episodes
                    DownloadVolumeFactor = 0, // ratioless tracker
                    UploadVolumeFactor = 1,
                    Genres = genres,
                    Description = string.Join("<br />\n", descriptions)
                };

                if (row.TvMazeId.IsNotNullOrWhiteSpace())
                {
                    release.TVMazeId = ParseUtil.CoerceInt(row.TvMazeId);
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

    public class NebulanceQuery
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Id { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Time { get; set; }

        [JsonProperty(PropertyName = "age", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Age { get; set; }

        [JsonProperty(PropertyName = "tvmaze", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int? TvMaze { get; set; }

        [JsonProperty(PropertyName = "imdb", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Imdb { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Hash { get; set; }

        [JsonProperty(PropertyName = "tags", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string[] Tags { get; set; }

        [JsonProperty(PropertyName = "name", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "release", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Release { get; set; }

        [JsonProperty(PropertyName = "category", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Category { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Series { get; set; }

        [JsonProperty(PropertyName = "season", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int? Season { get; set; }

        [JsonProperty(PropertyName = "episode", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int? Episode { get; set; }

        public NebulanceQuery Clone()
        {
            return MemberwiseClone() as NebulanceQuery;
        }
    }

    public class NebulanceRpcResponse<T>
    {
        public T Result { get; set; }
        public JToken Error { get; set; }
    }

    public class NebulanceResponse
    {
        public List<NebulanceTorrent> Items { get; set; }
    }

    public class NebulanceTorrent
    {
        [JsonPropertyName("rls_name")]
        public string ReleaseTitle { get; set; }

        [JsonPropertyName("cat")]
        public string Category { get; set; }

        public string Size { get; set; }
        public string Seed { get; set; }
        public string Leech { get; set; }
        public string Snatch { get; set; }
        public string Download { get; set; }

        [JsonPropertyName("file_list")]
        public IEnumerable<string> FileList { get; set; } = Array.Empty<string>();

        [JsonPropertyName("group_name")]
        public string GroupName { get; set; }

        [JsonPropertyName("series_banner")]
        public string Banner { get; set; }

        [JsonPropertyName("group_id")]
        public string TorrentId { get; set; }

        [JsonPropertyName("series_id")]
        public string TvMazeId { get; set; }

        [JsonPropertyName("rls_utc")]
        public string PublishDateUtc { get; set; }

        public IEnumerable<string> Tags { get; set; } = Array.Empty<string>();
    }

    public class NebulanceErrorResponse
    {
        public NebulanceErrorMessage Error { get; set; }
    }

    public class NebulanceErrorMessage
    {
        public string Message { get; set; }
    }
}
