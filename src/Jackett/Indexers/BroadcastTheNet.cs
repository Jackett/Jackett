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

namespace Jackett.Indexers
{
    public class BroadcastTheNet : BaseIndexer, IIndexer
    {
        string APIBASE = "http://api.btnapps.net/";

        new ConfigurationDataAPIKey configData
        {
            get { return (ConfigurationDataAPIKey)base.configData; }
            set { base.configData = value; }
        }

        public BroadcastTheNet(IIndexerManagerService i, IWebClient wc, Logger l, IProtectionService ps)
            : base(name: "BroadcastTheNet",
                description: "Needs no description..",
                link: "https://broadcasthe.net/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataAPIKey())
        {
        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);

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


        private string JsonRPCRequest(string method, dynamic parameters)
        {
            dynamic request = new ExpandoObject();
            request["jsonrpc"] = "2.0";
            request["method"] = method;
            request["params"] = parameters;
            request["id"] = Guid.NewGuid().ToString().Substring(0, 8);
            return request.ToString();
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();


            var response = await PostDataWithCookiesAndRetry(APIBASE, null, null, null, new Dictionary<string, string>()
            {
                { "Accept", "application/json-rpc, application/json"},
                {"ContentType", "application/json-rpc"}
            }, JsonRPCRequest("getTorrents", new Object[]
            {
                configData.Key.Value
            }));

            /* 
             var searchUrl = BrowsePage;

             if (!string.IsNullOrWhiteSpace(query.GetQueryString()))
             {
                 searchUrl += string.Format(QueryString, HttpUtility.UrlEncode(query.GetQueryString()));
             }

             var results = await RequestStringWithCookiesAndRetry(searchUrl);

             try
             {
                 CQ dom = results.Content;

                 var rows = dom["#sortabletable tr"];
                 foreach (var row in rows.Skip(1))
                 {
                     var release = new ReleaseInfo();
                     var qRow = row.Cq();
                     release.Title = qRow.Find(".tooltip-content div").First().Text();
                     if (string.IsNullOrWhiteSpace(release.Title))
                         continue;
                     release.Description = qRow.Find(".tooltip-content div").Get(1).InnerText.Trim();

                     var qLink = row.Cq().Find("td:eq(2) a:eq(1)");
                     release.Link = new Uri(qLink.Attr("href"));
                     release.Guid = release.Link;
                     release.Comments = new Uri(qRow.Find(".tooltip-target a").First().Attr("href"));

                     // 07-22-2015 11:08 AM
                     var dateString = qRow.Find("td:eq(1) div").Last().Children().Remove().End().Text().Trim();
                     release.PublishDate = DateTime.ParseExact(dateString, "MM-dd-yyyy hh:mm tt", CultureInfo.InvariantCulture);

                     var sizeStr = qRow.Find("td:eq(4)").Text().Trim();
                     release.Size = ReleaseInfo.GetBytes(sizeStr);

                     release.Seeders = ParseUtil.CoerceInt(qRow.Find("td:eq(6)").Text().Trim());
                     release.Peers = ParseUtil.CoerceInt(qRow.Find("td:eq(7)").Text().Trim()) + release.Seeders;

                     var catLink = row.Cq().Find("td:eq(0) a").First().Attr("href");
                     var catSplit = catLink.IndexOf("category=");
                     if (catSplit > -1)
                     {
                         catLink = catLink.Substring(catSplit + 9);
                     }

                     release.Category = MapTrackerCatToNewznab(catLink);
                     releases.Add(release);
                 }
             }
             catch (Exception ex)
             {
                 OnParseError(results.Content, ex);
             }*/

            return releases;
        }


    }
}
