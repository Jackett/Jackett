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
        private string SearchAllUrl => SiteLink + "other/all.rss";
        public override string[] LegacySiteLinks { get; protected set; } = new string[] {
            "http://showrss.info/",
        };

        private new ConfigurationData configData
        {
            get => base.configData;
            set => base.configData = value;
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

            await ConfigureIfOK(string.Empty, releases.Any(),
                                () => throw new Exception("Could not find releases from this URL"));

            return IndexerConfigurationStatus.RequiresTesting;
        }

        public override Task<byte[]> Download(Uri link) => throw new NotImplementedException();

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var episodeSearchUrl = string.Format(SearchAllUrl);
            var result = await RequestStringWithCookiesAndRetry(episodeSearchUrl, string.Empty);
            var xmlDoc = new XmlDocument();

            try
            {
                xmlDoc.LoadXml(result.Content);
                foreach (XmlNode node in xmlDoc.GetElementsByTagName("item"))
                {
                    //TODO revisit for refactoring
                    var title = node.SelectSingleNode(".//*[local-name()='raw_title']").InnerText;
                    if ((!query.IsImdbQuery || !TorznabCaps.SupportsImdbMovieSearch) &&
                        !query.MatchQueryStringAND(title))
                        continue;

                    // Try to guess the category... I'm not proud of myself...
                    var category = title.Contains("720p") ? TorznabCatType.TVHD.ID : TorznabCatType.TVSD.ID;
                    var test = node.SelectSingleNode("enclosure");
                    var magnetUri = new Uri(node.SelectSingleNode("link").InnerText);
                    var publishDate = DateTime.Parse(node.SelectSingleNode("pubDate").InnerText, CultureInfo.InvariantCulture);
                    var infoHash = node.SelectSingleNode("description").InnerText;
                    //TODO Maybe use magnetUri instead? https://github.com/Jackett/Jackett/pull/7342#discussion_r397552678
                    var guid = new Uri(test.Attributes["url"].Value);
                    var release = new ReleaseInfo
                    {
                        MinimumRatio = 1,
                        MinimumSeedTime = 172800, // 48 hours
                        Title = title,
                        Comments = magnetUri,
                        Category = new List<int> {category},
                        Guid = guid,
                        PublishDate = publishDate,
                        Description = infoHash,
                        InfoHash = infoHash,
                        Size = 0,
                        //TODO fix seeder/peer counts if available
                        Seeders = 1,
                        Peers = 1,
                        DownloadVolumeFactor = 0,
                        UploadVolumeFactor = 1,
                        MagnetUri = magnetUri
                    };
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
