using CsQuery;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;

namespace Jackett
{
    public class Freshon : IndexerInterface
    {
        class FreshonConfig : ConfigurationData
        {
            public StringItem Username { get; private set; }
            public StringItem Password { get; private set; }

            public FreshonConfig()
            {
                Username = new StringItem { Name = "Username", ItemType = ItemType.InputString };
                Password = new StringItem { Name = "Password", ItemType = ItemType.InputString };
            }

            public override Item[] GetItems()
            {
                return new Item[] { Username, Password };
            }
        }

        static string BaseUrl = "https://freshon.tv";
        static string LoginUrl = BaseUrl + "/login.php";
        static string LoginPostUrl = BaseUrl + "/login.php?action=makelogin";
        static string SearchUrl = BaseUrl + "/browse.php";

        static string chromeUserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2272.118 Safari/537.36";

        CookieContainer cookies;
        HttpClientHandler handler;
        HttpClient client;

        public bool IsConfigured { get; private set; }

        public string DisplayName { get { return "FreshOnTV"; } }

        public string DisplayDescription { get { return "Our goal is to provide the latest stuff in the TV show domain"; } }

        public Uri SiteLink { get { return new Uri("https://freshon.tv/"); } }

        public event Action<IndexerInterface, JToken> OnSaveConfigurationRequested;

        public Freshon()
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
                //var loginPage = await client.GetAsync(LoginUrl);
                var config = new FreshonConfig();
                return (ConfigurationData)config;
            });
        }

        public Task ApplyConfiguration(JToken configJson)
        {
            return Task.Run(async () =>
            {
                var config = new FreshonConfig();
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
                message.RequestUri = new Uri(LoginPostUrl);
                message.Headers.Referrer = new Uri(LoginUrl);
                message.Headers.UserAgent.ParseAdd(chromeUserAgent);

                var response = await client.SendAsync(message);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!responseContent.Contains("/logout.php"))
                {
                    CQ dom = responseContent;
                    var messageEl = dom[".error_text"];
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
                message.RequestUri = new Uri(SearchUrl);
                message.Headers.UserAgent.ParseAdd(chromeUserAgent);

                var response = await client.SendAsync(message);
                var result = await response.Content.ReadAsStringAsync();
                if (!result.Contains("/logout.php"))
                    throw new Exception("Detected as not logged in");
            });
        }

        public void LoadFromSavedConfiguration(JToken jsonConfig)
        {
            cookies.FillFromJson(new Uri(BaseUrl), (JArray)jsonConfig["cookies"]);
            IsConfigured = true;
        }
    }
}
