using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
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

namespace Jackett.Common.Indexers
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

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        // based on https://github.com/Prowlarr/Prowlarr/tree/develop/src/NzbDrone.Core/Indexers/Definitions/BroadcastheNet
        private readonly string APIBASE = "https://api.broadcasthe.net";

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
            webclient.requestDelay = 4;
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

        private string JsonRPCRequest(string method, JArray parameters)
        {
            dynamic request = new JObject();
            request["jsonrpc"] = "2.0";
            request["method"] = method;
            request["params"] = parameters;
            request["id"] = Guid.NewGuid().ToString().Substring(0, 8);
            return request.ToString();
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var searchTerm = query.SearchTerm ?? string.Empty;

            var btnResults = query.Limit;
            if (btnResults == 0)
            {
                btnResults = (int)TorznabCaps.LimitsDefault;
            }

            var btnOffset = query.Offset;
            var releases = new List<ReleaseInfo>();

            var parameters = new Dictionary<string, object>();

            if (query.IsTvdbQuery)
            {
                parameters["tvdb"] = query.TvdbID;
            }

            if (searchTerm.IsNotNullOrWhiteSpace())
            {
                parameters["search"] = searchTerm.Replace(" ", "%");
            }

            // If only the season/episode is searched for then change format to match expected format
            if (query.Season > 0 && query.Episode.IsNullOrWhiteSpace())
            {
                parameters["category"] = "Episode";
                parameters["name"] = $"S{query.Season:00}E%";
            }
            else if (DateTime.TryParseExact($"{query.Season} {query.Episode}", "yyyy MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var showDate))
            {
                // Daily Episode
                parameters["name"] = showDate.ToString("yyyy.MM.dd");
                parameters["category"] = "Episode";
            }
            else if (query.Season > 0 && int.TryParse(query.Episode, out var episode) && episode > 0)
            {
                // Standard (S/E) Episode
                parameters["name"] = $"S{query.Season:00}E{episode:00}%";
                parameters["category"] = "Episode";
            }
            else if (searchTerm.IsNotNullOrWhiteSpace() && int.TryParse(searchTerm, out _) && query.TvdbID > 0)
            {
                // Disable ID-based searches for episodes with absolute episode number
                return releases;
            }

            var requestPayload = new JArray
            {
                new JValue(configData.Key.Value),
                JObject.FromObject(parameters),
                new JValue(btnResults),
                new JValue(btnOffset)
            };

            var response = await RequestWithCookiesAndRetryAsync(
                APIBASE, method: RequestType.POST,
                headers: new Dictionary<string, string>
                {
                    {"Accept", "application/json-rpc, application/json"},
                    {"Content-Type", "application/json-rpc"}
                }, rawbody: JsonRPCRequest("getTorrents", requestPayload), emulateBrowser: false);

            try
            {
                var btnResponse = JsonConvert.DeserializeObject<BTNRPCResponse>(response.ContentString);

                if (btnResponse?.Result?.Torrents == null)
                {
                    return releases;
                }

                foreach (var itemKey in btnResponse.Result.Torrents)
                {
                    var btnResult = itemKey.Value;
                    var descriptions = new List<string>();

                    if (!string.IsNullOrWhiteSpace(btnResult.Series))
                    {
                        descriptions.Add("Series: " + btnResult.Series);
                    }

                    if (!string.IsNullOrWhiteSpace(btnResult.GroupName))
                    {
                        descriptions.Add("Group Name: " + btnResult.GroupName);
                    }

                    if (!string.IsNullOrWhiteSpace(btnResult.Source))
                    {
                        descriptions.Add("Source: " + btnResult.Source);
                    }

                    if (!string.IsNullOrWhiteSpace(btnResult.Container))
                    {
                        descriptions.Add("Container: " + btnResult.Container);
                    }

                    if (!string.IsNullOrWhiteSpace(btnResult.Codec))
                    {
                        descriptions.Add("Codec: " + btnResult.Codec);
                    }

                    if (!string.IsNullOrWhiteSpace(btnResult.Resolution))
                    {
                        descriptions.Add("Resolution: " + btnResult.Resolution);
                    }

                    if (!string.IsNullOrWhiteSpace(btnResult.Origin))
                    {
                        descriptions.Add("Origin: " + btnResult.Origin);
                    }

                    if (!string.IsNullOrWhiteSpace(btnResult.YoutubeTrailer))
                    {
                        descriptions.Add(
                            "Youtube Trailer: <a href=\"" + btnResult.YoutubeTrailer + "\">" + btnResult.YoutubeTrailer +
                            "</a>");
                    }

                    var imdb = ParseUtil.GetImdbId(btnResult.ImdbID);
                    var link = new Uri(btnResult.DownloadURL);
                    var details = new Uri($"{SiteLink}torrents.php?id={btnResult.GroupID}&torrentid={btnResult.TorrentID}");
                    var publishDate = DateTimeUtil.UnixTimestampToDateTime(btnResult.Time);

                    var release = new ReleaseInfo
                    {
                        Guid = link,
                        Details = details,
                        Link = link,
                        Title = btnResult.ReleaseName,
                        Description = string.Join("<br />\n", descriptions),
                        Category = MapTrackerCatToNewznab(btnResult.Resolution),
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

                    if (!string.IsNullOrEmpty(btnResult.SeriesBanner))
                    {
                        release.Poster = new Uri(btnResult.SeriesBanner);
                    }

                    if (!release.Category.Any()) // default to TV
                    {
                        release.Category.Add(TorznabCatType.TV.ID);
                    }

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.ContentString, ex);
            }

            return releases;
        }

        public class BTNRPCResponse
        {
            public string Id { get; set; }
            public BTNResultPage Result { get; set; }
        }

        public class BTNResultPage
        {
            public Dictionary<int, BTNResultItem> Torrents { get; set; }
        }

        public class BTNResultItem
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
}
