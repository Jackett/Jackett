using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class ShowRSS : BaseWebIndexer
    {
        private string SearchAllUrl => SiteLink + "other/all.rss";
        private string BrowseUrl => SiteLink + "browse/";
        public override string[] LegacySiteLinks { get; protected set; } = {
            "http://showrss.info/"
        };

        private new ConfigurationData configData => base.configData;

        public ShowRSS(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "showrss",
                   name: "ShowRSS",
                   description: "showRSS is a service that allows you to keep track of your favorite TV shows",
                   link: "https://showrss.info/",
                   caps: new TorznabCapabilities
                   {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                       }
                   },
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationData())
        {
            Encoding = Encoding.UTF8;
            Language = "en-US";
            Type = "public";

            AddCategoryMapping(1, TorznabCatType.TV);
            AddCategoryMapping(2, TorznabCatType.TVSD);
            AddCategoryMapping(3, TorznabCatType.TVHD);
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var releases = await PerformQuery(new TorznabQuery());

            await ConfigureIfOK(string.Empty, releases.Any(),
                                () => throw new Exception("Could not find releases from this URL"));

            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var episodeSearchUrl = string.Format(SearchAllUrl);
            var result = await RequestWithCookiesAndRetryAsync(episodeSearchUrl);
            var xmlDoc = new XmlDocument();

            try
            {
                xmlDoc.LoadXml(result.ContentString);
                foreach (XmlNode node in xmlDoc.GetElementsByTagName("item"))
                {
                    var title = node.SelectSingleNode(".//*[local-name()='raw_title']").InnerText;
                    if (!query.MatchQueryStringAND(title))
                        continue;

                    // TODO: use Jackett.Common.Utils.TvCategoryParser.ParseTvShowQuality
                    // guess category from title
                    var category = title.Contains("720p") || title.Contains("1080p") ?
                        TorznabCatType.TVHD.ID :
                        TorznabCatType.TVSD.ID;

                    var magnetUri = new Uri(node.SelectSingleNode("link")?.InnerText);
                    var publishDate = DateTime.Parse(node.SelectSingleNode("pubDate").InnerText, CultureInfo.InvariantCulture);
                    var infoHash = node.SelectSingleNode(".//*[local-name()='info_hash']").InnerText;
                    var details = new Uri(BrowseUrl + node.SelectSingleNode(".//*[local-name()='show_id']").InnerText);

                    var release = new ReleaseInfo
                    {
                        Title = title,
                        Details = details,
                        Category = new List<int> { category },
                        Guid = magnetUri,
                        PublishDate = publishDate,
                        InfoHash = infoHash,
                        MagnetUri = magnetUri,
                        Size = 512,
                        Seeders = 1,
                        Peers = 2,
                        DownloadVolumeFactor = 0,
                        UploadVolumeFactor = 1
                    };
                    releases.Add(release);
                }
            }
            catch (Exception e)
            {
                OnParseError(result.ContentString, e);
            }

            return releases;
        }
    }
}
