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
    public class HDTorrents : IndexerInterface
    {
        public event Action<IndexerInterface, JToken> OnSaveConfigurationRequested;

        public event Action<IndexerInterface, string, Exception> OnResultParsingError;

        const string DefaultUrl = "https://hd-torrents.org";
        string BaseUrl;
        static string ChromeUserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2272.118 Safari/537.36";
        private string Search_url = "https://hd-torrents.org/torrents.php?search={0}&active=1&options=0&category%5B%5D=59&category%5B%5D=60&category%5B%5D=30&category%5B%5D=38&page={0}";
        private static string LoginUrl = DefaultUrl + "/login.php";
        private static string LoginPostUrl = DefaultUrl + "/index.php";
        CookieContainer cookies;
        HttpClientHandler handler;
        HttpClient client;

        public HDTorrents()
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

        public string DisplayName
        {
            get { return "HD-Torrents"; }
        }

        public string DisplayDescription
        {
            get { return "HD-Torrents is a private torrent website with HD torrents and strict rules on their content."; }
        }

        public Uri SiteLink
        {
            get { return new Uri(DefaultUrl); }
        }

        public bool IsConfigured
        {
            get;
            private set;
        }

        public async Task<ConfigurationData> GetConfigurationForSetup()
        {
            var request = CreateHttpRequest(new Uri(LoginUrl));

            var response = await client.SendAsync(request);
            await response.Content.ReadAsStreamAsync();
            var config = new ConfigurationDataBasicLogin();
            return config;
        }

        HttpRequestMessage CreateHttpRequest(Uri uri)
        {
            var message = new HttpRequestMessage();
            message.Method = HttpMethod.Get;
            message.RequestUri = uri;
            message.Headers.UserAgent.ParseAdd(ChromeUserAgent);
            return message;
        }

        public async Task ApplyConfiguration(JToken configJson)
        {
            var config = new ConfigurationDataBasicLogin();
            config.LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string> {
				{ "uid", config.Username.Value },
				{ "pwd", config.Password.Value }
			};

            var content = new FormUrlEncodedContent(pairs);
            var message = CreateHttpRequest(new Uri(LoginPostUrl));
            message.Method = HttpMethod.Post;
            message.Content = content;
            message.Headers.Referrer = new Uri(LoginPostUrl);

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
                configSaveData["cookies"] = cookies.ToJson(SiteLink);

                if (OnSaveConfigurationRequested != null)
                    OnSaveConfigurationRequested(this, configSaveData);

                IsConfigured = true;
            }
        }

        public void LoadFromSavedConfiguration(JToken jsonConfig)
        {
            BaseUrl = (string)jsonConfig["base_url"];
            IsConfigured = true;
        }

        async Task<ReleaseInfo[]> PerformQuery(TorznabQuery query, string baseUrl)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();


            return releases.ToArray();
        }

        public async Task<ReleaseInfo[]> PerformQuery(TorznabQuery query)
        {
            return await PerformQuery(query, BaseUrl);
        }

        public Task<byte[]> Download(Uri link)
        {
            throw new NotImplementedException();
        }
    }
}
