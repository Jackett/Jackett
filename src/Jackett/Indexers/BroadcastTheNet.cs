using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jackett.Models;
using Jackett.Models.IndexerConfig;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Indexers
{
    public class BroadcastTheNet : BaseWebIndexer
    {
        // Docs at http://apidocs.broadcasthe.net/docs.php
        string APIBASE = "https://api.broadcasthe.net";

        new ConfigurationDataAPIKey configData
        {
            get { return (ConfigurationDataAPIKey)base.configData; }
            set { base.configData = value; }
        }

        public BroadcastTheNet(IIndexerConfigurationService configService, IWebClient wc, Logger l, IProtectionService ps)
            : base(name: "BroadcastTheNet",
                description: null,
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

            var parameters = new JArray();
            parameters.Add(new JValue(configData.Key.Value));
            parameters.Add(new JValue(searchString.Trim()));
            parameters.Add(new JValue(btnResults));
            parameters.Add(new JValue(btnOffset));
            var response = await PostDataWithCookiesAndRetry(APIBASE, null, null, null, new Dictionary<string, string>()
            {
                { "Accept", "application/json-rpc, application/json"},
                {"Content-Type", "application/json-rpc"}
            }, JsonRPCRequest("getTorrents", parameters), false);

            try
            {
                var btnResponse = JsonConvert.DeserializeObject<BTNRPCResponse>(response.Content);

                if (btnResponse != null && btnResponse.Result != null && btnResponse.Result.Torrents != null)
                {
                    foreach (var itemKey in btnResponse.Result.Torrents)
                    {
                        var descriptions = new List<string>();
                        var btnResult = itemKey.Value;
                        var item = new ReleaseInfo();
                        if (!string.IsNullOrEmpty(btnResult.SeriesBanner))
                            item.BannerUrl = new Uri(btnResult.SeriesBanner);
                        item.Category = MapTrackerCatToNewznab(btnResult.Resolution);
                        if (item.Category.Count == 0) // default to TV
                            item.Category.Add(TorznabCatType.TV.ID);
                        item.Comments = new Uri($"{SiteLink}torrents.php?id={btnResult.GroupID}&torrentid={btnResult.TorrentID}");
                        item.Guid = new Uri(btnResult.DownloadURL);
                        if (!string.IsNullOrWhiteSpace(btnResult.ImdbID))
                            item.Imdb = ParseUtil.CoerceLong(btnResult.ImdbID);
                        item.Link = new Uri(btnResult.DownloadURL);
                        item.MinimumRatio = 1;
                        item.PublishDate = DateTimeUtil.UnixTimestampToDateTime(btnResult.Time);
                        item.RageID = btnResult.TvrageID;
                        item.Seeders = btnResult.Seeders;
                        item.Peers = btnResult.Seeders + btnResult.Leechers;
                        item.Size = btnResult.Size;
                        item.TVDBId = btnResult.TvdbID;
                        item.Title = btnResult.ReleaseName;
                        item.UploadVolumeFactor = 1;
                        item.DownloadVolumeFactor = 0; // ratioless
                        item.Grabs = btnResult.Snatched;

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

                        item.Description = string.Join("<br />\n", descriptions);

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
