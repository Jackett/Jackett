using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Jackett.Common.Extensions;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class BeyondHDAPI : IndexerBase
    {
        public override string Id => "beyond-hd-api";
        public override string Name => "Beyond-HD (API)";
        public override string Description => "Without BeyondHD, your HDTV is just a TV";
        public override string SiteLink { get; protected set; } = "https://beyond-hd.me/";
        public override string Language => "en-US";
        public override string Type => "private";

        public override bool SupportsPagination => true;

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        protected virtual int ApiKeyLength => 32;
        protected virtual int RSSKeyLength => 32;

        private new ConfigurationDataBeyondHDApi configData
        {
            get => (ConfigurationDataBeyondHDApi)base.configData;
            set => base.configData = value;
        }

        public BeyondHDAPI(IIndexerConfigurationService configService, WebClient wc, Logger l,
            IProtectionService ps, ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataBeyondHDApi("Find the API and RSS keys under your security settings (your profile picture -> my security)"))
        {
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                LimitsDefault = 100,
                LimitsMax = 100,
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId, TvSearchParam.TmdbId
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q, MovieSearchParam.ImdbId, MovieSearchParam.TmdbId
                }
            };

            caps.Categories.AddCategoryMapping(1, TorznabCatType.Movies, "Movies");
            caps.Categories.AddCategoryMapping(2, TorznabCatType.TV, "TV");

            return caps;
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new BeyondHDAPIRequestGenerator(configData, TorznabCaps);
        }

        public override IParseIndexerResponse GetParser()
        {
            return new BeyondHDAPIParser(configData, TorznabCaps.Categories, logger);
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            if (configData.ApiKey.Value.IsNullOrWhiteSpace())
            {
                throw new Exception("Missing API Key.");
            }
            if (configData.ApiKey.Value.Length != ApiKeyLength)
            {
                throw new Exception($"Invalid API Key configured: expected length: {ApiKeyLength}, got {configData.ApiKey.Value.Length}");
            }
            if (configData.RSSKey.Value.IsNullOrWhiteSpace())
            {
                throw new Exception("Missing RSS Key.");
            }
            if (configData.RSSKey.Value.Length != RSSKeyLength)
            {
                throw new Exception($"Invalid RSS Key configured: expected length: {RSSKeyLength}, got {configData.RSSKey.Value.Length}");
            }

            IsConfigured = false;
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

    public class BeyondHDAPIRequestGenerator : IIndexerRequestGenerator
    {
        private readonly ConfigurationDataBeyondHDApi _configData;
        private readonly TorznabCapabilities _capabilities;

        private const string ApiBase = "https://beyond-hd.me/api/torrents/";

        public BeyondHDAPIRequestGenerator(ConfigurationDataBeyondHDApi configData, TorznabCapabilities capabilities)
        {
            _configData = configData;
            _capabilities = capabilities;
        }

        public IndexerPageableRequestChain GetSearchRequests(TorznabQuery query)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            var postData = new Dictionary<string, object>
            {
                { BHDParams.action, "search" },
                { BHDParams.rsskey, _configData.RSSKey.Value },
                { BHDParams.search, query.GetQueryString() },
            };

            if (_configData.FilterFreeleech.Value)
            {
                postData.Add(BHDParams.freeleech, 1);
            }

            if (_configData.FilterLimited.Value)
            {
                postData.Add(BHDParams.limited, 1);
            }

            if (_configData.FilterRefund.Value)
            {
                postData.Add(BHDParams.refund, 1);
            }

            if (_configData.FilterRewind.Value)
            {
                postData.Add(BHDParams.rewind, 1);
            }

            if (_configData.SearchTypes.Values.Any())
            {
                postData.Add(BHDParams.types, _configData.SearchTypes.Values.ToArray());
            }

            var categories = _capabilities.Categories.MapTorznabCapsToTrackers(query);

            if (categories.Any())
            {
                postData.Add(BHDParams.categories, categories.Select(int.Parse).ToArray());
            }

            if (query.IsImdbQuery)
            {
                postData.Add(BHDParams.imdb_id, query.ImdbIDShort);
            }
            else if (query.IsTmdbQuery)
            {
                postData.Add(BHDParams.tmdb_id, query.TmdbID.ToString());
            }

            if (query.Limit > 0 && query.Offset > 0)
            {
                var page = (query.Offset / query.Limit) + 1;
                postData.Add("page", page.ToString());
            }

            pageableRequests.Add(GetPagedRequests(postData));

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(Dictionary<string, object> postData)
        {
            var apiKey = _configData.ApiKey.Value;
            var apiUrl = $"{ApiBase}{apiKey}";

            var request = new WebRequest
            {
                Url = apiUrl,
                Type = RequestType.POST,
                Headers = new Dictionary<string, string>
                {
                    { "Accept", "application/json" },
                    { "Content-Type", "application/json" }
                },
                RawBody = JsonConvert.SerializeObject(postData)
            };

            yield return new IndexerRequest(request);
        }
    }

    public class BeyondHDAPIParser : IParseIndexerResponse
    {
        private readonly ConfigurationDataBeyondHDApi _configData;
        private readonly TorznabCapabilitiesCategories _categories;
        private readonly Logger _logger;

        public BeyondHDAPIParser(ConfigurationDataBeyondHDApi configData, TorznabCapabilitiesCategories categories, Logger logger)
        {
            _configData = configData;
            _categories = categories;
            _logger = logger;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var releases = new List<ReleaseInfo>();

            if (indexerResponse.Content.StartsWith("<"))
            {
                // The response was not JSON, likely a HTML page for a server outage
                _logger.Warn("The response was not JSON: {0}", indexerResponse.Content);

                throw new Exception("The response was not JSON");
            }

            if (indexerResponse.Content.ContainsIgnoreCase("Invalid API Key"))
            {
                throw new Exception("API Key invalid or not authorized");
            }

            var jsonResponse = JsonConvert.DeserializeObject<BHDResponse>(indexerResponse.Content);

            if (jsonResponse.StatusCode == 0)
            {
                throw new Exception($"Indexer Error: {jsonResponse.StatusMessage}");
            }

            foreach (var row in jsonResponse.Results)
            {
                if (row.DownloadUrl.IsNullOrWhiteSpace() || row.InfoUrl.IsNullOrWhiteSpace())
                {
                    _logger.Warn("Missing download or info url for release with the Id {0}, skipping.", row.Id);
                    continue;
                }

                // Skip invalid results when freeleech or limited filtering is set
                if ((_configData.FilterFreeleech.Value && !row.Freeleech) || (_configData.FilterLimited.Value && !row.Limited))
                {
                    continue;
                }

                var releaseInfo = new ReleaseInfo
                {
                    Guid = new Uri(row.InfoUrl),
                    Details = new Uri(row.InfoUrl),
                    Link = new Uri(row.DownloadUrl),
                    Title = GetReleaseTitle(row),
                    Category = _categories.MapTrackerCatDescToNewznab(row.Category),
                    InfoHash = row.InfoHash,
                    Grabs = row.Grabs,
                    Size = row.Size,
                    Seeders = row.Seeders,
                    Peers = row.Leechers + row.Seeders,
                    PublishDate = DateTime.Parse(row.CreatedAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal)
                };

                if (row.ImdbId.IsNotNullOrWhiteSpace())
                {
                    releaseInfo.Imdb = ParseUtil.GetImdbId(row.ImdbId);
                }

                if (row.TmdbId.IsNotNullOrWhiteSpace())
                {
                    var tmdbId = row.TmdbId.Split('/').ElementAtOrDefault(1);
                    releaseInfo.TMDb = tmdbId != null && ParseUtil.TryCoerceInt(tmdbId, out var tmdbResult) ? tmdbResult : 0;
                }

                releaseInfo.DownloadVolumeFactor = 1;
                releaseInfo.UploadVolumeFactor = 1;

                if (row.Freeleech || row.Limited)
                {
                    releaseInfo.DownloadVolumeFactor = 0;
                }

                if (row.Promo25)
                {
                    releaseInfo.DownloadVolumeFactor = .75;
                }

                if (row.Promo50)
                {
                    releaseInfo.DownloadVolumeFactor = .50;
                }

                if (row.Promo75)
                {
                    releaseInfo.DownloadVolumeFactor = .25;
                }

                releases.Add(releaseInfo);
            }

            return releases;
        }

        private string GetReleaseTitle(BHDResult row)
        {
            var title = row.Name.Trim();

            if (!_configData.AddHybridFeaturesToTitle.Value)
            {
                return title;
            }

            var features = new HashSet<string>();

            if (row.DolbyVision)
            {
                features.Add("Dolby Vision");
            }

            if (row.Hdr10)
            {
                features.Add("HDR10");
            }

            if (row.Hdr10Plus)
            {
                features.Add("HDR10+");
            }

            if (row.Hlg)
            {
                features.Add("HLG");
            }

            if (features.Count > 1)
            {
                title += $" ({string.Join(" / ", features)})";
            }

            return title;
        }
    }

    internal class BHDParams
    {
        internal const string action = "action"; // string - The torrents endpoint action you wish to perform. (search)
        internal const string rsskey = "rsskey"; // string - Your personal RSS key (RID) if you wish for results to include the uploaded_by and download_url fields
        internal const string page = "page"; // int - The page number of the results. Only if the result set has more than 100 total matches.

        internal const string search = "search"; // string - The torrent name. It does support !negative searching. Example: Christmas Movie
        internal const string info_hash = "info_hash"; // string - The torrent info_hash. This is an exact match.
        internal const string folder_name = "folder_name"; // string - The torrent folder name. This is an exact match.file_name string The torrent included file names. This is an exact match.
        internal const string size = "size"; // int - The torrent size. This is an exact match.
        internal const string uploaded_by = "uploaded_by"; // string - The uploaders username. Only non anonymous results will be returned.
        internal const string imdb_id = "imdb_id"; // int - The ID of the matching IMDB page.
        internal const string tmdb_id = "tmdb_id"; // int - The ID of the matching TMDB page.
        internal const string categories = "categories"; // string - Any categories separated by comma(s). TV, Movies)
        internal const string types = "types"; // string - Any types separated by comma(s). BD Remux, 1080p, etc.)
        internal const string sources = "sources"; // string - Any sources separated by comma(s). Blu-ray, WEB, DVD, etc.)
        internal const string genres = "genres"; // string - Any genres separated by comma(s). Action, Anime, StandUp, Western, etc.)
        internal const string groups = "groups"; // string - Any internal release groups separated by comma(s).FraMeSToR, BHDStudio, BeyondHD, RPG, iROBOT, iFT, ZR, MKVULTRA
        internal const string freeleech = "freeleech"; // int - The torrent freeleech status. 1 = Must match.
        internal const string limited = "limited"; // int - The torrent limited UL promo. 1 = Must match.
        internal const string promo25 = "promo25"; // int - The torrent 25% promo. 1 = Must match.
        internal const string promo50 = "promo50"; // int - The torrent 50% promo. 1 = Must match.
        internal const string promo75 = "promo75"; // int - The torrent 75% promo. 1 = Must match.
        internal const string refund = "refund"; // int - The torrent refund promo. 1 = Must match.
        internal const string rescue = "rescue"; // int - The torrent rescue promo. 1 = Must match.
        internal const string rewind = "rewind"; // int - The torrent rewind promo. 1 = Must match.
        internal const string stream = "stream"; // int - The torrent Stream Optimized flag. 1 = Must match.
        internal const string sd = "sd"; // int - The torrent SD flag. 1 = Must match.
        internal const string pack = "pack"; // int - The torrent TV pack flag. 1 = Must match.
        internal const string h264 = "h264"; // int - The torrent x264/h264 codec flag. 1 = Must match.
        internal const string h265 = "h265"; // int - The torrent x265/h265 codec flag. 1 = Must match.
        internal const string alive = "alive"; // int - The torrent has at least 1 seeder. 1 = Must match.
        internal const string dying = "dying"; // int - The torrent has less than 3 seeders. 1 = Must match.
        internal const string dead = "dead"; // int - The torrent has no seeders. 1 = Must match.
        internal const string reseed = "reseed"; // int - The torrent has no seeders and an active reseed request. 1 = Must match.
        internal const string seeding = "seeding"; // int - The torrent is seeded by you. 1 = Must match.
        internal const string leeching = "leeching"; // int - The torrent is being leeched by you. 1 = Must match.
        internal const string completed = "completed"; // int - The torrent has been completed by you. 1 = Must match.
        internal const string incomplete = "incomplete"; // int - The torrent has not been completed by you. 1 = Must match.
        internal const string notdownloaded = "notdownloaded"; // int - The torrent has not been downloaded you. 1 = Must match.
        internal const string min_bhd = "min_bhd"; // int - The minimum BHD rating.
        internal const string vote_bhd = "vote_bhd"; // int - The minimum number of BHD votes.
        internal const string min_imdb = "min_imdb"; // int - The minimum IMDb rating.
        internal const string vote_imdb = "vote_imdb"; // int - The minimum number of IMDb votes.
        internal const string min_tmdb = "min_tmdb"; // int - The minimum TMDb rating.
        internal const string vote_tmdb = "vote_tmdb"; // int - The minimum number of TDMb votes.
        internal const string min_year = "min_year"; // int - The earliest release year.
        internal const string max_year = "max_year"; // int - The latest release year.
        internal const string sort = "sort"; // string - Field to sort results by. (bumped_at, created_at, seeders, leechers, times_completed, size, name, imdb_rating, tmdb_rating, bhd_rating). Default is bumped_at
        internal const string order = "order"; // string - The direction of the sort of results. (asc, desc). Default is desc

        // Most of the comma separated fields are OR searches.
        internal const string features = "features"; // string - Any features separated by comma(s). DV, HDR10, HDR10P, Commentary)
        internal const string countries = "countries"; // string - Any production countries separated by comma(s). France, Japan, etc.)
        internal const string languages = "languages"; // string - Any spoken languages separated by comma(s). French, English, etc.)
        internal const string audios = "audios"; // string - Any audio tracks separated by comma(s). English, Japanese,etc.)
        internal const string subtitles = "subtitles"; // string - Any subtitles separated by comma(s). Dutch, Finnish, Swedish, etc.)

    }

    class BHDResponse
    {
        [JsonProperty("status_code")]
        public int StatusCode { get; set; } // The status code of the post request. (0 = Failed and 1 = Success)

        [JsonProperty("status_message")]
        public string StatusMessage { get; set; } // If status code=0 then there will be an explanation

        public IReadOnlyCollection<BHDResult> Results { get; set; } // The results that match your query.

        public int page { get; set; } // The current page of results that you're on.
        public int total_pages { get; set; } // int The total number of pages of results matching your query.
        public int total_results { get; set; } // The total number of results matching your query.
        public bool success { get; set; } // The status of the call. (True = Success, False = Error)
    }

    class BHDResult
    {
        public int Id { get; set; }
        public string Name { get; set; }

        [JsonProperty("folder_name")]
        public string FolderName { get; set; }

        [JsonProperty("info_hash")]
        public string InfoHash { get; set; }

        public long Size { get; set; }
        public string Category { get; set; }
        public string Type { get; set; }
        public int Seeders { get; set; }
        public int Leechers { get; set; }

        [JsonProperty("times_completed")]
        public int Grabs { get; set; }

        [JsonProperty("imdb_id")]
        public string ImdbId { get; set; }

        [JsonProperty("tmdb_id")]
        public string TmdbId { get; set; }

        public decimal bhd_rating { get; set; }
        public decimal tmdb_rating { get; set; }
        public decimal imdb_rating { get; set; }
        public bool tv_pack { get; set; }
        public bool Promo25 { get; set; }
        public bool Promo50 { get; set; }
        public bool Promo75 { get; set; }
        public bool Freeleech { get; set; }
        public bool Rewind { get; set; }
        public bool Refund { get; set; }
        public bool Limited { get; set; }
        public bool Rescue { get; set; }

        [JsonProperty("created_at")]
        public string CreatedAt { get; set; }

        [JsonProperty("url")]
        public string InfoUrl { get; set; }

        [JsonProperty("download_url")]
        public string DownloadUrl { get; set; }

        [JsonProperty("dv")]
        public bool DolbyVision { get; set; }

        public bool Hdr10 { get; set; }

        [JsonProperty("hdr10+")]
        public bool Hdr10Plus { get; set; }

        public bool Hlg { get; set; }
    }
}
