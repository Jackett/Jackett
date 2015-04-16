using CsQuery;
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
    public class IPTorrents : IndexerInterface
    {

        public event Action<IndexerInterface, Newtonsoft.Json.Linq.JToken> OnSaveConfigurationRequested;

        public string DisplayName { get { return "IPTorrents"; } }

        public string DisplayDescription { get { return "Always a step ahead"; } }

        public Uri SiteLink { get { return new Uri("https://iptorrents.com"); } }

        public bool IsConfigured { get; private set; }

        static string BaseUrl = "https://iptorrents.com";

        static string chromeUserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2272.118 Safari/537.36";

        CookieContainer cookies;
        HttpClientHandler handler;
        HttpClient client;

        public IPTorrents()
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
            return Task.Run(async () =>
            {
                await client.GetAsync(new Uri(BaseUrl));
                var config = new ConfigurationDataBasicLogin();
                return (ConfigurationData)config;
            });
        }

        public Task ApplyConfiguration(Newtonsoft.Json.Linq.JToken configJson)
        {
            return Task.Run(async () =>
            {
                var config = new ConfigurationDataBasicLogin();
                config.LoadValuesFromJson(configJson);

                var pairs = new Dictionary<string, string>
                {
                    { "username", config.Username.Value},
                    { "password", config.Password.Value}
                };

                var content = new FormUrlEncodedContent(pairs);
                var message = new HttpRequestMessage();
                message.Method = HttpMethod.Post;
                message.Content = content;
                message.RequestUri = new Uri(BaseUrl);
                message.Headers.Referrer = new Uri(BaseUrl);
                message.Headers.UserAgent.ParseAdd(chromeUserAgent);

                var response = await client.SendAsync(message);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!responseContent.Contains("/my.php"))
                {
                    CQ dom = responseContent;
                    var messageEl = dom["body > div"].First();
                    var errorMessage = messageEl.Text().Trim();
                    throw new ExceptionWithConfigData(errorMessage, (ConfigurationData)config);
                }
                else
                {
                    var configSaveData = new JObject();
                    configSaveData["cookies"] = new JArray((
                        from cookie in cookies.GetCookies(new Uri(BaseUrl)).Cast<Cookie>()
                        select cookie.Name + ":" + cookie.Value
                    ).ToArray());

                    if (OnSaveConfigurationRequested != null)
                        OnSaveConfigurationRequested(this, configSaveData);

                    IsConfigured = true;
                }
            });
        }

        public Task VerifyConnection()
        {
            return Task.Run(async () =>
            {
                var message = new HttpRequestMessage();
                message.Method = HttpMethod.Get;
                message.RequestUri = new Uri(BaseUrl);
                message.Headers.UserAgent.ParseAdd(chromeUserAgent);

                var response = await client.SendAsync(message);
                var result = await response.Content.ReadAsStringAsync();
                if (!result.Contains("/my.php"))
                    throw new Exception("Detected as not logged in");
            });
        }

        public void LoadFromSavedConfiguration(Newtonsoft.Json.Linq.JToken jsonConfig)
        {
            cookies.FillFromJson(new Uri(BaseUrl), (JArray)jsonConfig["cookies"]);
            IsConfigured = true;
        }
    }
}
