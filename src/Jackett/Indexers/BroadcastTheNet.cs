using CsQuery;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Jackett.Models.IndexerConfig;
using System.Dynamic;
using Newtonsoft.Json;

namespace Jackett.Indexers
{
    public class BroadcastTheNet : BaseWebIndexer
    {
        string APIBASE = "https://api.broadcasthe.net";

        new ConfigurationDataAPIKey configData
        {
            get { return (ConfigurationDataAPIKey)base.configData; }
            set { base.configData = value; }
        }

        public BroadcastTheNet(IIndexerConfigurationService configService, IWebClient wc, Logger l, IProtectionService ps)
            : base(name: "BroadcastTheNet",
                description: "Needs no description..",
                link: "https://broadcasthe.net/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                configService: configService,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataAPIKey())
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";
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
            catch(Exception e)
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
            var releases = new List<ReleaseInfo>();

            var parameters = new JArray();
            parameters.Add(new JValue(configData.Key.Value));
            parameters.Add(new JValue(searchString.Trim()));
            parameters.Add(new JValue(100));
            parameters.Add(new JValue(0));
            var response = await PostDataWithCookiesAndRetry(APIBASE, null, null, null, new Dictionary<string, string>()
            {
                { "Accept", "application/json-rpc, application/json"},
                {"Content-Type", "application/json-rpc"}
            }, JsonRPCRequest("getTorrents", parameters),false);

            try
            {
                var btnResponse = JsonConvert.DeserializeObject<BTNRPCResponse>(response.Content);

                if (btnResponse != null && btnResponse.Result != null)
                {
                    foreach (var itemKey in btnResponse.Result.Torrents)
                    {
                        var btnResult = itemKey.Value;
                        var item = new ReleaseInfo();
                        if (!string.IsNullOrEmpty(btnResult.SeriesBanner))
                            item.BannerUrl = new Uri(btnResult.SeriesBanner);
                        item.Category = new List<int> { TorznabCatType.TV.ID };
                        item.Comments = new Uri($"https://broadcasthe.net/torrents.php?id={btnResult.GroupID}&torrentid={btnResult.TorrentID}");
                        item.Description = btnResult.ReleaseName;
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
