using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
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
using System.Xml;

namespace Jackett.Indexers
{
    public class ShowRSS : BaseIndexer, IIndexer
    {
        private readonly string searchAllUrl = "";
        string BaseUrl;

        CookieContainer cookies;
        HttpClientHandler handler;
        HttpClient client;

          public ShowRSS(IIndexerManagerService i, Logger l) :
            base(name: "ShowRSS",
          description: "showRSS is a service that allows you to keep track of your favorite TV shows",
          link: new Uri("http://showrss.info"),
          rageid: true,
          manager: i,
          logger: l)
        {
            searchAllUrl = SiteLink + "/feeds/all.rss";

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
            var config = new ConfigurationDataUrl(SiteLink);
            return Task.FromResult<ConfigurationData>(config);
        }

        public async Task ApplyConfiguration(Newtonsoft.Json.Linq.JToken configJson)
        {
            var config = new ConfigurationDataUrl(SiteLink);
            config.LoadValuesFromJson(configJson);

            var formattedUrl = config.GetFormattedHostUrl();
            var releases = await PerformQuery(new TorznabQuery(), formattedUrl);
            if (releases.Length == 0)
                throw new Exception("Could not find releases from this URL");

            BaseUrl = formattedUrl;

            var configSaveData = new JObject();
            configSaveData["base_url"] = BaseUrl;
            SaveConfig(configSaveData);
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
            headers.Add("User-Agent", BrowserUtil.ChromeUserAgent);
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
                OnParseError(xml, ex);
            }

            return releases.ToArray();
        }
    }
}
