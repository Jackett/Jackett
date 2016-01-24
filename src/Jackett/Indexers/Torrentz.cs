using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
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
using System.Linq;
using Jackett.Models.IndexerConfig;

namespace Jackett.Indexers
{
    public class Torrentz : BaseIndexer, IIndexer
    {
        readonly static string defaultSiteLink = "https://torrentz.eu/";

        private Uri BaseUri
        {
            get { return new Uri(configData.Url.Value); }
            set { configData.Url.Value = value.ToString(); }
        }

        private string SearchUrl { get { return BaseUri + "feed_verifiedP?f={0}"; } }

        new ConfigurationDataUrl configData
        {
            get { return (ConfigurationDataUrl)base.configData; }
            set { base.configData = value; }
        }


        public Torrentz(IIndexerManagerService i, Logger l, IWebClient wc, IProtectionService ps)
            : base(name: "Torrentz",
                description: "Torrentz is a meta-search engine and a Multisearch. This means we just search other search engines.",
                link: defaultSiteLink,
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataUrl(defaultSiteLink))
        {
        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);
            var releases = await PerformQuery(new TorznabQuery());

            await ConfigureIfOK(string.Empty, releases.Count() > 0, () =>
            {
                throw new Exception("Could not find releases from this URL");
            });
            return IndexerConfigurationStatus.Completed;
        }

        // Override to load legacy config format
        public override void LoadFromSavedConfiguration(JToken jsonConfig)
        {
            if (jsonConfig is JObject)
            {
                BaseUri = new Uri(jsonConfig.Value<string>("base_url"));
                SaveConfig();
                IsConfigured = true;
                return;
            }

            base.LoadFromSavedConfiguration(jsonConfig);
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();
            var episodeSearchUrl = string.Format(SearchUrl, HttpUtility.UrlEncode(searchString.Trim()));
            var xmlDoc = new XmlDocument();
            string xml = string.Empty;
            var result = await RequestStringWithCookiesAndRetry(episodeSearchUrl);

            try
            {
                xmlDoc.LoadXml(result.Content);

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
                    int category = 0;
                    int.TryParse(node.SelectSingleNode("category").InnerText, out category);
                    release.Category = category;
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

            return releases;
        }

        public override Task<byte[]> Download(Uri link)
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
                        this.Size = ReleaseInfo.GetBytes(val);
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
