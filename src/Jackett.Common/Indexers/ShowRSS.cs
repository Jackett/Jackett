using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    public class ShowRSS : BaseWebIndexer
    {
        private string SearchAllUrl { get { return SiteLink + "other/all.rss"; } }

        private new ConfigurationData configData
        {
            get { return (ConfigurationData)base.configData; }
            set { base.configData = value; }
        }

        public ShowRSS(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(name: "ShowRSS",
                description: "showRSS is a service that allows you to keep track of your favorite TV shows",
                link: "https://showrss.info/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                configService: configService,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationData())
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "public";
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);
            var releases = await PerformQuery(new TorznabQuery());

            await ConfigureIfOK(string.Empty, releases.Count() > 0, () =>
            {
                throw new Exception("Could not find releases from this URL");
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        public override Task<byte[]> Download(Uri link)
        {
            throw new NotImplementedException();
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var episodeSearchUrl = string.Format(SearchAllUrl);
            var result = await RequestStringWithCookiesAndRetry(episodeSearchUrl, string.Empty);
            var xmlDoc = new XmlDocument();

            try
            {
                xmlDoc.LoadXml(result.Content);
                ReleaseInfo release;
                string serie_title;

                foreach (XmlNode node in xmlDoc.GetElementsByTagName("item"))
                {
                    release = new ReleaseInfo();

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;

                    serie_title = node.SelectSingleNode(".//*[local-name()='raw_title']").InnerText;
                    release.Title = serie_title;

                    if ((query.ImdbID == null || !TorznabCaps.SupportsImdbSearch) && !query.MatchQueryStringAND(release.Title))
                        continue;

                    release.Comments = new Uri(node.SelectSingleNode("link").InnerText);

                    // Try to guess the category... I'm not proud of myself...
                    int category = 5030;
                    if (serie_title.Contains("720p"))
                        category = 5040;
                    release.Category = new List<int> { category };
                    var test = node.SelectSingleNode("enclosure");
                    release.Guid = new Uri(test.Attributes["url"].Value);
                    release.PublishDate = DateTime.Parse(node.SelectSingleNode("pubDate").InnerText, CultureInfo.InvariantCulture);

                    release.Description = node.SelectSingleNode("description").InnerText;
                    release.InfoHash = node.SelectSingleNode("description").InnerText;
                    release.Size = 0;
                    release.Seeders = 1;
                    release.Peers = 1;
                    release.DownloadVolumeFactor = 0;
                    release.UploadVolumeFactor = 1;
                    release.MagnetUri = new Uri(node.SelectSingleNode("link").InnerText);
                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(result.Content, ex);
            }

            return releases;
        }
    }
}