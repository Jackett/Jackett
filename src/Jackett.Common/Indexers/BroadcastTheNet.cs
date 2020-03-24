using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
    public class BroadcastTheNet : BaseWebIndexer
    {
        // Docs at http://apidocs.broadcasthe.net/docs.php
        private readonly string APIBASE = "https://api.broadcasthe.net";

        private new ConfigurationDataAPIKey configData
        {
            get => (ConfigurationDataAPIKey)base.configData;
            set => base.configData = value;
        }

        public BroadcastTheNet(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(name: "BroadcastTheNet",
                description: "Broadcasthe.net (BTN) is an invite-only torrent tracker focused on TV shows",
                link: "https://broadcasthe.net/",
                caps: new TorznabCapabilities(),
                configService: configService,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataAPIKey())
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";

            TorznabCaps.LimitsDefault = 100;
            TorznabCaps.LimitsMax = 1000;

            AddCategoryMapping("SD", TorznabCatType.TVSD, "SD");
            AddCategoryMapping("720p", TorznabCatType.TVHD, "720p");
            AddCategoryMapping("1080p", TorznabCatType.TVHD, "1080p");
            AddCategoryMapping("1080i", TorznabCatType.TVHD, "1080i");
            AddCategoryMapping("2160p", TorznabCatType.TVHD, "2160p");
            AddCategoryMapping("Portable Device", TorznabCatType.TVSD, "Portable Device");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            IsConfigured = false;
            try
            {
                var results = await PerformQuery(new TorznabQuery());
                if (results.Count() == 0)
                    throw new Exception("Testing returned no results!");
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
            var searchString = query.GetQueryString();
            var btnResults = query.Limit;
            if (btnResults == 0)
                btnResults = (int)TorznabCaps.LimitsDefault;
            var btnOffset = query.Offset;
            var releases = new List<ReleaseInfo>();

            // If only the season is searched for then change format to match expected format
            var seasonOnlyMatch = new Regex(@".*\s[Ss]{1}\d{2}(?<![Ee]{1}\d{2,3})?$").Match(searchString);
            if (seasonOnlyMatch.Success)
            {
                searchString = Regex.Replace(searchString, @"[Ss]{1}\d{2}", $"Season {query.Season}");
            }

            var parameters = new JArray
            {
                new JValue(configData.Key.Value),
                new JValue(searchString.Trim()),
                new JValue(btnResults),
                new JValue(btnOffset)
            };
            var response = await PostDataWithCookiesAndRetry(APIBASE, null, null, null, new Dictionary<string, string>()
            {
                { "Accept", "application/json-rpc, application/json"},
                {"Content-Type", "application/json-rpc"}
            }, JsonRPCRequest("getTorrents", parameters), false);

            try
            {
                var btnResponse = JsonConvert.DeserializeObject<BTNRPCResponse>(response.Content);

                if (btnResponse?.Result?.Torrents != null)
                {
                    foreach (var itemKey in btnResponse.Result.Torrents)
                    {
                        var btnResult = itemKey.Value;
                        var descriptions = new List<string>();
                        if (!string.IsNullOrWhiteSpace(btnResult.Series))
                            descriptions.Add("Series: " + btnResult.Series);
                        if (!string.IsNullOrWhiteSpace(btnResult.GroupName))
                            descriptions.Add("Group Name: " + btnResult.GroupName);
                        if (!string.IsNullOrWhiteSpace(btnResult.Source))
                            descriptions.Add("Source: " + btnResult.Source);
                        if (!string.IsNullOrWhiteSpace(btnResult.Container))
                            descriptions.Add("Container: " + btnResult.Container);
                        if (!string.IsNullOrWhiteSpace(btnResult.Codec))
                            descriptions.Add("Codec: " + btnResult.Codec);
                        if (!string.IsNullOrWhiteSpace(btnResult.Resolution))
                            descriptions.Add("Resolution: " + btnResult.Resolution);
                        if (!string.IsNullOrWhiteSpace(btnResult.Origin))
                            descriptions.Add("Origin: " + btnResult.Origin);
                        if (!string.IsNullOrWhiteSpace(btnResult.Series))
                            descriptions.Add("Youtube Trailer: <a href=\"" + btnResult.YoutubeTrailer + "\">" + btnResult.YoutubeTrailer + "</a>");
                        var imdb = ParseUtil.GetImdbID(btnResult.ImdbID);
                        var guid = new Uri(btnResult.DownloadURL);
                        var comments = new Uri($"{SiteLink}torrents.php?id={btnResult.GroupID}&torrentid={btnResult.TorrentID}");
                        var publishDate = DateTimeUtil.UnixTimestampToDateTime(btnResult.Time);
                        var item = new ReleaseInfo
                        {
                            Category = MapTrackerCatToNewznab(btnResult.Resolution),
                            Comments = comments,
                            Guid = guid,
                            Link = guid,
                            MinimumRatio = 1,
                            PublishDate = publishDate,
                            RageID = btnResult.TvrageID,
                            Seeders = btnResult.Seeders,
                            Peers = btnResult.Seeders + btnResult.Leechers,
                            Size = btnResult.Size,
                            TVDBId = btnResult.TvdbID,
                            Title = btnResult.ReleaseName,
                            UploadVolumeFactor = 1,
                            DownloadVolumeFactor = 0, // ratioless
                            Grabs = btnResult.Snatched,
                            Description = string.Join("<br />\n", descriptions),
                            Imdb = imdb
                        };
                        if (!string.IsNullOrEmpty(btnResult.SeriesBanner))
                            item.BannerUrl = new Uri(btnResult.SeriesBanner);
                        if (!item.Category.Any()) // default to TV
                            item.Category.Add(TorznabCatType.TV.ID);

                        releases.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.Content, ex);
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
