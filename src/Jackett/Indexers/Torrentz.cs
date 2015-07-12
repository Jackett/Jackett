using Newtonsoft.Json.Linq;
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
    public class Torrentz : IndexerInterface
    {
        public event Action<IndexerInterface, JToken> OnSaveConfigurationRequested;

        public event Action<IndexerInterface, string, Exception> OnResultParsingError;

        public string DisplayName
        {
            get { return "Torrentz"; }
        }

        public string DisplayDescription
        {
            get { return "Torrentz is a meta-search engine and a Multisearch. This means we just search other search engines."; }
        }

        public Uri SiteLink
        {
            get { return new Uri(DefaultUrl); }
        }

        const string DefaultUrl = "https://torrentz.eu";
        const string SearchUrl = DefaultUrl + "/feed_verifiedP?f={0}";
        string BaseUrl;
        static string chromeUserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2272.118 Safari/537.36";

        CookieContainer cookies;
        HttpClientHandler handler;
        HttpClient client;

        public bool IsConfigured
        {
            get;
            private set;
        }

        public Torrentz()
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

        public async Task ApplyConfiguration(JToken configJson)
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

            foreach (var title in query.ShowTitles ?? new string[] { string.Empty })
            {
                var searchString = title + " " + query.GetEpisodeSearchString();
                var episodeSearchUrl = string.Format(SearchUrl, HttpUtility.UrlEncode(searchString.Trim()));

                XmlDocument xmlDoc = new XmlDocument();
                string xml = string.Empty;
                WebClient wc = getWebClient();

                try
                {
                    using (wc)
                    {
                        xml = wc.DownloadString(episodeSearchUrl);
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
                        release.Peers = td.Peers;
                        release.MagnetUri = TorrentzHelper.createMagnetLink(td.hash, serie_title);
                        releases.Add(release);
                    }
                }
                catch (Exception ex)
                {
                    OnResultParsingError(this, xml, ex);
                    throw ex;
                }
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
