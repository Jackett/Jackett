using Jackett.Models;
using Jackett.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;

namespace Jackett.Indexers
{
    public class ShowRSS : IIndexer
    {
        public event Action<IIndexer, Newtonsoft.Json.Linq.JToken> OnSaveConfigurationRequested;

        public event Action<IIndexer, string, Exception> OnResultParsingError;

        public string DisplayName
        {
            get { return "ShowRSS"; }
        }

        public string DisplayDescription
        {
            get { return "showRSS is a service that allows you to keep track of your favorite TV shows"; }
        }

        public Uri SiteLink
        {
            get { return new Uri(DefaultUrl); }
        }

        public bool RequiresRageIDLookupDisabled { get { return true; } }

        const string DefaultUrl = "http://showrss.info";
        const string searchAllUrl = DefaultUrl + "/feeds/all.rss";
        string BaseUrl;
        static string chromeUserAgent = BrowserUtil.ChromeUserAgent;

        CookieContainer cookies;
        HttpClientHandler handler;
        HttpClient client;

        public bool IsConfigured
        {
            get;
            private set;
        }

        public ShowRSS()
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
            var config = new ConfigurationDataUrl(DefaultUrl);
            return Task.FromResult<ConfigurationData>(config);
        }

        public async Task ApplyConfiguration(Newtonsoft.Json.Linq.JToken configJson)
        {
            var config = new ConfigurationDataUrl(DefaultUrl);
            config.LoadValuesFromJson(configJson);

            var formattedUrl = config.GetFormattedHostUrl();
            var releases = await PerformQuery(new TorznabQuery(), formattedUrl);
            if (releases.Length == 0)
                throw new Exception("Could not find releases from this URL");

            BaseUrl = formattedUrl;

            var configSaveData = new JObject();
            configSaveData["base_url"] = BaseUrl;

            if (OnSaveConfigurationRequested != null)
                OnSaveConfigurationRequested(this, configSaveData);

            IsConfigured = true;
        }

        public void LoadFromSavedConfiguration(Newtonsoft.Json.Linq.JToken jsonConfig)
        {
            BaseUrl = (string)jsonConfig["base_url"];
            IsConfigured = true;
        }

        public async Task<ReleaseInfo[]> PerformQuery(TorznabQuery query)
        {
            return await PerformQuery(query, BaseUrl);
        }

        public Task<byte[]> Download(Uri link)
        {
            throw new NotImplementedException();
        }

        private WebClient getWebClient()
        {
            WebClient wc = new WebClient();
            WebHeaderCollection headers = new WebHeaderCollection();
            headers.Add("User-Agent", chromeUserAgent);
            wc.Headers = headers;
            return wc;
        }

        async Task<ReleaseInfo[]> PerformQuery(TorznabQuery query, string baseUrl)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            var searchString = query.SanitizedSearchTerm + " " + query.GetEpisodeSearchString();
            var episodeSearchUrl = string.Format(searchAllUrl);

            XmlDocument xmlDoc = new XmlDocument();
            string xml = string.Empty;
            WebClient wc = getWebClient();

            try
            {
                using (wc)
                {
                    xml = await wc.DownloadStringTaskAsync(new Uri(episodeSearchUrl));
                    xmlDoc.LoadXml(xml);
                }

                ReleaseInfo release;
                string serie_title;

                foreach (XmlNode node in xmlDoc.GetElementsByTagName("item"))
                {
                    release = new ReleaseInfo();

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;

                    serie_title = node.SelectSingleNode("title").InnerText;
                    release.Title = serie_title;

                    release.Comments = new Uri(node.SelectSingleNode("link").InnerText);
                    release.Category = node.SelectSingleNode("title").InnerText;
                    var test = node.SelectSingleNode("enclosure");
                    release.Guid = new Uri(test.Attributes["url"].Value);
                    release.PublishDate = DateTime.Parse(node.SelectSingleNode("pubDate").InnerText, CultureInfo.InvariantCulture);

                    release.Description = node.SelectSingleNode("description").InnerText;
                    release.InfoHash = node.SelectSingleNode("description").InnerText;
                    release.Size = 0;
                    release.Seeders = 1;
                    release.Peers = 1;
                    release.MagnetUri = new Uri(node.SelectSingleNode("link").InnerText);
                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnResultParsingError(this, xml, ex);
                throw ex;
            }

            return releases.ToArray();
        }
    }
}
