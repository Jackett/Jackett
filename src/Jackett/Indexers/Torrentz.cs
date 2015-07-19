using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using System.Xml;

namespace Jackett.Indexers
{
    public class Torrentz : BaseIndexer, IIndexer
    {
        private readonly string SearchUrl = "";
        string BaseUrl;

        CookieContainer cookies;
        HttpClientHandler handler;
        HttpClient client;

          public Torrentz(IIndexerManagerService i, Logger l) :
            base(name: "Torrentz",
        description: "Torrentz is a meta-search engine and a Multisearch. This means we just search other search engines.",
        link: new Uri("https://torrentz.eu"),
        rageid: true,
        manager: i,
        logger: l)
        {

            SearchUrl = SiteLink + "/feed_verifiedP?f={0}";
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

        public async Task ApplyConfiguration(JToken configJson)
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
            var episodeSearchUrl = string.Format(SearchUrl, HttpUtility.UrlEncode(searchString.Trim()));

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
                TorrentzHelper td;
                string serie_title;

                foreach (XmlNode node in xmlDoc.GetElementsByTagName("item"))
                {
                    release = new ReleaseInfo();

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;
                    serie_title = node.SelectSingleNode("title").InnerText;
                    release.Title = serie_title;

                    release.Comments = new Uri(node.SelectSingleNode("link").InnerText);
                    release.Category = node.SelectSingleNode("category").InnerText;
                    release.Guid = new Uri(node.SelectSingleNode("guid").InnerText);
                    release.PublishDate = DateTime.Parse(node.SelectSingleNode("pubDate").InnerText, CultureInfo.InvariantCulture);

                    td = new TorrentzHelper(node.SelectSingleNode("description").InnerText);
                    release.Description = td.Description;
                    release.InfoHash = td.hash;
                    release.Size = td.Size;
                    release.Seeders = td.Seeders;
                    release.Peers = td.Peers + release.Seeders;
                    release.MagnetUri = TorrentzHelper.createMagnetLink(td.hash, serie_title);
                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(xml, ex);
            }

            return releases.ToArray();
        }


        public void LoadFromSavedConfiguration(JToken jsonConfig)
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
    }

    public class TorrentzHelper
    {
        public TorrentzHelper(string description)
        {
            this.Description = description;
            if (null == description)
            {
                this.Description = "";
                this.Size = 0;
                this.Peers = 0;
                this.Seeders = 0;
                this.hash = "";
            }
            else
                FillProperties();
        }

        public static Uri createMagnetLink(string hash, string title)
        {
            string MagnetLink = "magnet:?xt=urn:btih:{0}&dn={1}&tr={2}";
            string Trackers = WebUtility.UrlEncode("udp://tracker.publicbt.com:80&tr=udp://tracker.openbittorrent.com:80&tr=udp://tracker.ccc.de:80&tr=udp://tracker.istole.it:80");
            title = WebUtility.UrlEncode(title);

            return new Uri(string.Format(MagnetLink, hash, title, Trackers));
        }

        private void FillProperties()
        {
            string description = this.Description;
            int counter = 0;
            while (description.Contains(" "))
            {
                int nextSpace = description.IndexOf(": ") + 1;
                int secondSpace;
                if (counter != 0)
                    secondSpace = description.IndexOf(" ", nextSpace + 1);
                else
                    secondSpace = description.IndexOf(": ", nextSpace + 2) - description.IndexOf(" ", nextSpace);

                string val;
                if (secondSpace == -1)
                {
                    val = description.Substring(nextSpace).Trim();
                    description = string.Empty;
                }
                else
                {
                    val = description.Substring(nextSpace, secondSpace - nextSpace).Trim();
                    description = description.Substring(secondSpace);
                }

                switch (counter)
                {
                    case 0:
                        this.Size = ReleaseInfo.BytesFromMB(ParseUtil.CoerceLong(val.Substring(0, val.IndexOf(" ") - 1)));
                        break;
                    case 1:
                        this.Seeders = ParseUtil.CoerceInt(val.Contains(",") ? val.Remove(val.IndexOf(","), 1) : val);
                        break;
                    case 2:
                        this.Peers = ParseUtil.CoerceInt(val.Contains(",") ? val.Remove(val.IndexOf(","), 1) : val);
                        break;
                    case 3:
                        this.hash = val;
                        break;
                }
                counter++;
            }
        }

        public string Description { get; set; }
        public long Size { get; set; }
        public int Seeders { get; set; }
        public int Peers { get; set; }
        public string hash { get; set; }
    }
}
