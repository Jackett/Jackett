using CsQuery;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Jackett.Indexers
{
    public class ThePirateBay : IndexerInterface
    {

        class ThePirateBayConfig : ConfigurationData
        {
            public StringItem Url { get; private set; }

            public ThePirateBayConfig()
            {
                Url = new StringItem { Name = "Url", ItemType = ItemType.InputString, Value = "https://thepiratebay.se/" };
            }

            public override Item[] GetItems()
            {
                return new Item[] { Url };
            }
        }

        public event Action<IndexerInterface, Newtonsoft.Json.Linq.JToken> OnSaveConfigurationRequested;

        public string DisplayName { get { return "The Pirate Bay"; } }

        public string DisplayDescription { get { return "The worlds largest bittorrent indexer"; } }

        public Uri SiteLink { get { return new Uri("https://thepiratebay.se/"); } }

        public bool IsConfigured { get; private set; }

        static string SearchUrl = "s/?q=\"{0}\"&category=205&page=0&orderby=99";
        static string BrowserUrl = "browse";

        string BaseUrl;

        CookieContainer cookies;
        HttpClientHandler handler;
        HttpClient client;


        public ThePirateBay()
        {
            IsConfigured = false;
            cookies = new CookieContainer();
            handler = new HttpClientHandler
            {
                CookieContainer = cookies,
                AllowAutoRedirect = true,
                UseCookies = true,
            };
            client = new HttpClient(handler);
        }

        public Task<ConfigurationData> GetConfigurationForSetup()
        {
            return Task.Run(() =>
            {
                var config = new ThePirateBayConfig();
                return (ConfigurationData)config;
            });
        }

        public Task ApplyConfiguration(Newtonsoft.Json.Linq.JToken configJson)
        {
            return Task.Run(async () =>
            {
                var config = new ThePirateBayConfig();
                config.LoadValuesFromJson(configJson);
                await TestBrowse(config.Url.Value);
                BaseUrl = new Uri(config.Url.Value).ToString();
                var configSaveData = new JObject();
                configSaveData["base_url"] = BaseUrl;

                if (OnSaveConfigurationRequested != null)
                    OnSaveConfigurationRequested(this, configSaveData);

                IsConfigured = true;

            });
        }

        public Task VerifyConnection()
        {
            return Task.Run(async () =>
            {
                await TestBrowse(BaseUrl);
            });
        }


        Task TestBrowse(string url)
        {
            return Task.Run(async () =>
            {
                var result = await client.GetStringAsync(new Uri(url) + BrowserUrl);
                if (!result.Contains("<span>Browse Torrents</span>"))
                {
                    throw new Exception("Could not detect The Pirate Bay content");
                }
            });
        }

        public void LoadFromSavedConfiguration(JToken jsonConfig)
        {
            BaseUrl = (string)jsonConfig["base_url"];
            IsConfigured = true;
        }

        public Task<ReleaseInfo[]> PerformQuery(TorznabQuery query)
        {
            return Task<ReleaseInfo[]>.Run(async () =>
            {
                List<ReleaseInfo> releases = new List<ReleaseInfo>();

                var search = BaseUrl + string.Format(SearchUrl, HttpUtility.UrlEncode("game of thrones s03e09"));
                var results = await client.GetStringAsync(search);
                CQ dom = results;
                var descRegex = new Regex("Uploaded (?<month>.*?)-(?<day>.*?) (?<year>.*?), Size (?<size>.*?) (?<unit>.*?), ULed by");
                var rows = dom["#searchResult > tbody > tr"];
                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();
                    CQ qRow = row.Cq();
                    CQ qLink = qRow[".detLink"].First();
                    CQ qPeerCols = qRow["td[align=\"right\"]"];

                    //Uploaded 08-02 2007, Size 47.15 MiB, ULed
                    var description = qRow[".detDesc"][0].ChildNodes[0].NodeValue.Trim();
                    var descGroups = descRegex.Match(description).Groups;
                    release.PublishDate = new DateTime(
                        int.Parse(descGroups["year"].Value),
                        int.Parse(descGroups["month"].Value),
                        int.Parse(descGroups["day"].Value)
                    );
                    var size = float.Parse(descGroups["size"].Value);
                    switch (descGroups["unit"].Value)
                    {
                        case "GiB": release.Size = ReleaseInfo.BytesFromGB(size); break;
                        case "MiB": release.Size = ReleaseInfo.BytesFromMB(size); break;
                        case "KiB": release.Size = ReleaseInfo.BytesFromKB(size); break;
                    }

                    release.Comments = new Uri(BaseUrl + qLink.Attr("href").TrimStart('/'));
                    release.Guid = release.Comments;
                    release.Title = qLink.Text().Trim();
                    release.Description = release.Title;
                    release.MagnetUrl = new Uri(qRow["td > a"].First().Attr("href"));
                    release.InfoHash = release.MagnetUrl.ToString().Split(':')[3].Split('&')[0];
                    release.Seeders = int.Parse(qPeerCols.ElementAt(0).InnerText);
                    release.Peers = int.Parse(qPeerCols.ElementAt(1).InnerText) + release.Seeders;
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;
                    releases.Add(release);
                }

                return releases.ToArray();
            });
        }
    }
}
