using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

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

        static string SearchUrl = "s/?q=test";
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
    }
}
