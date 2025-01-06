using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jackett.Common.Extensions;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class BroadcasTheNet : IndexerBase
    {
        public override string Id => "broadcasthenet";
        public override string[] Replaces => new[] { "broadcastthenet" };
        public override string Name => "BroadcasTheNet";
        public override string Description => "BroadcasTheNet (BTN) is an invite-only torrent tracker focused on TV shows";
        // Status: https://btn.trackerstatus.info/
        public override string SiteLink { get; protected set; } = "https://broadcasthe.net/";
        public override string Language => "en-US";
        public override string Type => "private";

        public override bool SupportsPagination => true;

        public override int PageSize => 100;

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        // TODO: remove ConfigurationDataAPIKey class and use ConfigurationDataPasskey instead
        private new ConfigurationDataAPIKey configData
        {
            get => (ConfigurationDataAPIKey)base.configData;
            set => base.configData = value;
        }

        public BroadcasTheNet(IIndexerConfigurationService configService, WebClient wc, Logger l,
            IProtectionService ps, ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataAPIKey())
        {
            webclient.requestDelay = 5;
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                LimitsDefault = 100,
                LimitsMax = 1000,
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.TvdbId
                }
            };

            caps.Categories.AddCategoryMapping("SD", TorznabCatType.TVSD, "SD");
            caps.Categories.AddCategoryMapping("720p", TorznabCatType.TVHD, "720p");
            caps.Categories.AddCategoryMapping("1080p", TorznabCatType.TVHD, "1080p");
            caps.Categories.AddCategoryMapping("1080i", TorznabCatType.TVHD, "1080i");
            caps.Categories.AddCategoryMapping("2160p", TorznabCatType.TVUHD, "2160p");
            caps.Categories.AddCategoryMapping("Portable Device", TorznabCatType.TVSD, "Portable Device");

            return caps;
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new BroadcastheNetRequestGenerator(configData, TorznabCaps);
        }

        public override IParseIndexerResponse GetParser()
        {
            return new BroadcastheNetParser(SiteLink, TorznabCaps.Categories);
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

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

    public class BroadcastheNetRequestGenerator : IIndexerRequestGenerator
    {
        private readonly ConfigurationDataAPIKey _configData;
        private readonly TorznabCapabilities _torznabCaps;

        // based on https://github.com/Prowlarr/Prowlarr/tree/develop/src/NzbDrone.Core/Indexers/Definitions/BroadcastheNet
        private const string ApiBase = "https://api.broadcasthe.net";

        public BroadcastheNetRequestGenerator(ConfigurationDataAPIKey configData, TorznabCapabilities torznabCaps)
        {
            _configData = configData;
            _torznabCaps = torznabCaps;
        }

        public IndexerPageableRequestChain GetSearchRequests(TorznabQuery query)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            var searchTerm = query.SearchTerm ?? string.Empty;

            var btnResults = query.Limit;
            if (btnResults == 0)
            {
                btnResults = _torznabCaps.LimitsDefault.GetValueOrDefault(100);
            }

            var btnOffset = query.Offset;

            var parameters = new BroadcastheNetSearchQuery();

            if (query.IsTvdbQuery)
            {
                parameters.Tvdb = query.TvdbID.ToString();
            }

            if (searchTerm.IsNotNullOrWhiteSpace())
            {
                parameters.Search = searchTerm.Replace(" ", "%");
            }

            // If only the season/episode is searched for then change format to match expected format
            if (query.Season > 0 && query.Episode.IsNullOrWhiteSpace())
            {
                // Search Season
                parameters.Category = "Season";
                parameters.Name = $"Season {query.Season}%";
                pageableRequests.Add(GetPagedRequests(parameters, btnResults, btnOffset));

                parameters = parameters.Clone();

                // Search Episode
                parameters.Category = "Episode";
                parameters.Name = $"S{query.Season:00}E%";
                pageableRequests.Add(GetPagedRequests(parameters, btnResults, btnOffset));
            }
            else if (DateTime.TryParseExact($"{query.Season} {query.Episode}", "yyyy MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var showDate))
            {
                // Daily Episode
                parameters.Name = showDate.ToString("yyyy.MM.dd", CultureInfo.InvariantCulture);
                parameters.Category = "Episode";
                pageableRequests.Add(GetPagedRequests(parameters, btnResults, btnOffset));
            }
            else if (query.Season > 0 && int.TryParse(query.Episode, out var episode) && episode > 0)
            {
                // Standard (S/E) Episode
                parameters.Name = $"S{query.Season:00}E{episode:00}%";
                parameters.Category = "Episode";
                pageableRequests.Add(GetPagedRequests(parameters, btnResults, btnOffset));
            }
            else if (searchTerm.IsNotNullOrWhiteSpace() && int.TryParse(searchTerm, out _) && query.TvdbID > 0)
            {
                // Disable ID-based searches for episodes with absolute episode number
                return new IndexerPageableRequestChain();
            }
            else
            {
                // Neither a season only search nor daily nor standard, fall back to query
                pageableRequests.Add(GetPagedRequests(parameters, btnResults, btnOffset));
            }

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(BroadcastheNetSearchQuery parameters, int results, int offset)
        {
            var webRequest = new WebRequest
            {
                Url = ApiBase,
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
                    new JValue(results),
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

    public class BroadcastheNetParser : IParseIndexerResponse
    {
        private readonly string _siteLink;
        private readonly TorznabCapabilitiesCategories _categories;

        public BroadcastheNetParser(string siteLink, TorznabCapabilitiesCategories categories)
        {
            _siteLink = siteLink;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var releases = new List<ReleaseInfo>();

            var jsonResponse = JsonConvert.DeserializeObject<BroadcastheNetResponse>(indexerResponse.Content);

            if (jsonResponse?.Result?.Torrents == null)
            {
                return releases;
            }

            foreach (var itemKey in jsonResponse.Result.Torrents)
            {
                var btnResult = itemKey.Value;
                var descriptions = new List<string>();

                if (btnResult.Series.IsNotNullOrWhiteSpace())
                {
                    descriptions.Add("Series: " + btnResult.Series);
                }

                if (btnResult.GroupName.IsNotNullOrWhiteSpace())
                {
                    descriptions.Add("Group Name: " + btnResult.GroupName);
                }

                if (btnResult.Source.IsNotNullOrWhiteSpace())
                {
                    descriptions.Add("Source: " + btnResult.Source);
                }

                if (btnResult.Container.IsNotNullOrWhiteSpace())
                {
                    descriptions.Add("Container: " + btnResult.Container);
                }

                if (btnResult.Codec.IsNotNullOrWhiteSpace())
                {
                    descriptions.Add("Codec: " + btnResult.Codec);
                }

                if (btnResult.Resolution.IsNotNullOrWhiteSpace())
                {
                    descriptions.Add("Resolution: " + btnResult.Resolution);
                }

                if (btnResult.Origin.IsNotNullOrWhiteSpace())
                {
                    descriptions.Add("Origin: " + btnResult.Origin);
                }

                if (btnResult.YoutubeTrailer.IsNotNullOrWhiteSpace())
                {
                    descriptions.Add(
                        "Youtube Trailer: <a href=\"" + btnResult.YoutubeTrailer + "\">" + btnResult.YoutubeTrailer +
                        "</a>");
                }

                var imdb = ParseUtil.GetImdbId(btnResult.ImdbID);
                var link = new Uri(btnResult.DownloadURL);
                var details = new Uri($"{_siteLink}torrents.php?id={btnResult.GroupID}&torrentid={btnResult.TorrentID}");
                var publishDate = DateTimeUtil.UnixTimestampToDateTime(btnResult.Time);

                var release = new ReleaseInfo
                {
                    Guid = link,
                    Details = details,
                    Link = link,
                    Title = GetTitle(btnResult),
                    Description = string.Join("<br />\n", descriptions),
                    Category = _categories.MapTrackerCatToNewznab(btnResult.Resolution),
                    InfoHash = btnResult.InfoHash,
                    Size = btnResult.Size,
                    Grabs = btnResult.Snatched,
                    Seeders = btnResult.Seeders,
                    Peers = btnResult.Seeders + btnResult.Leechers,
                    PublishDate = publishDate,
                    TVDBId = btnResult.TvdbID,
                    RageID = btnResult.TvrageID,
                    Imdb = imdb,
                    DownloadVolumeFactor = 0, // ratioless
                    UploadVolumeFactor = 1,
                    MinimumRatio = 1,
                    MinimumSeedTime = btnResult.Category.ToUpperInvariant() == "SEASON" ? 432000 : 86400 // 120 hours for seasons and 24 hours for episodes
                };

                if (btnResult.SeriesBanner.IsNotNullOrWhiteSpace())
                {
                    var posterUrl = btnResult.SeriesBanner;

                    if (posterUrl.StartsWith("//"))
                    {
                        posterUrl = "https:" + posterUrl;
                    }

                    release.Poster = new Uri(posterUrl);
                }

                if (!release.Category.Any()) // default to TV
                {
                    release.Category.Add(TorznabCatType.TV.ID);
                }

                releases.Add(release);
            }

            return releases;
        }

        private static string GetTitle(BroadcastheNetTorrent torrent)
        {
            var releaseName = torrent.ReleaseName.Replace("\\", "");

            if (torrent.Container.ToUpperInvariant() is "M2TS" or "ISO")
            {
                releaseName = Regex.Replace(releaseName, @"\b(H\.?265)\b", "HEVC", RegexOptions.Compiled);
                releaseName = Regex.Replace(releaseName, @"\b(H\.?264)\b", "AVC", RegexOptions.Compiled);
            }

            return releaseName;
        }
    }

    public class BroadcastheNetSearchQuery
    {
        [JsonProperty("category", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Category { get; set; }

        [JsonProperty("name", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty("search", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Search { get; set; }

        [JsonProperty("tvdb", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Tvdb { get; set; }

        [JsonProperty("tvrage", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Tvrage { get; set; }

        public BroadcastheNetSearchQuery Clone()
        {
            return MemberwiseClone() as BroadcastheNetSearchQuery;
        }
    }

    public class BroadcastheNetResponse
    {
        public string Id { get; set; }
        public BroadcastheNetResult Result { get; set; }
    }

    public class BroadcastheNetResult
    {
        public Dictionary<int, BroadcastheNetTorrent> Torrents { get; set; }
    }

    public class BroadcastheNetTorrent
    {
        public int TorrentID { get; set; }
        public string DownloadURL { get; set; }
        public string GroupName { get; set; }
        public int GroupID { get; set; }
        public int SeriesID { get; set; }
        public string Series { get; set; }
        public string SeriesBanner { get; set; }
        public string SeriesPoster { get; set; }
        public string YoutubeTrailer { get; set; }
        public string Category { get; set; }
        public int? Snatched { get; set; }
        public int? Seeders { get; set; }
        public int? Leechers { get; set; }
        public string Source { get; set; }
        public string Container { get; set; }
        public string Codec { get; set; }
        public string Resolution { get; set; }
        public string Origin { get; set; }
        public string ReleaseName { get; set; }
        public long Size { get; set; }
        public long Time { get; set; }
        public int? TvdbID { get; set; }
        public int? TvrageID { get; set; }
        public string ImdbID { get; set; }
        public string InfoHash { get; set; }
    }
}
