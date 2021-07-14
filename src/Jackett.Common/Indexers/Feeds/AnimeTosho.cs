using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;

namespace Jackett.Common.Indexers.Feeds
{
    [ExcludeFromCodeCoverage]
    public class AnimeTosho : BaseNewznabIndexer
    {
        public AnimeTosho(IIndexerConfigurationService configService, WebClient client, Logger logger,
            IProtectionService ps, ICacheService cs)
            : base(id: "animetosho",
                   name: "Anime Tosho",
                   description: "AnimeTosho (AT) is an automated service that provides torrent files, magnet links and DDL for all anime releases",
                   link: "https://animetosho.org/",
                   caps: new TorznabCapabilities
                   {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                       }
                   },
                   configService: configService,
                   client: client,
                   logger: logger,
                   p: ps,
                   cs: cs,
                   configData: new ConfigurationData())
        {
            Encoding = Encoding.UTF8;
            Language = "en-US";
            Type = "public";

            AddCategoryMapping(1, TorznabCatType.TVAnime);
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var results = await base.PerformQuery(query);
            // results must contain search terms
            results = results.Where(release => query.MatchQueryStringAND(release.Title));
            return results;
        }

        protected override ReleaseInfo ResultFromFeedItem(XElement item)
        {
            var release = base.ResultFromFeedItem(item);
            var enclosures = item.Descendants("enclosure").Where(e => e.Attribute("type").Value == "application/x-bittorrent");
            if (enclosures.Any())
            {
                var enclosure = enclosures.First().Attribute("url").Value;
                release.Link = new Uri(enclosure);
            }
            // add some default values if none returned by feed
            release.Seeders = release.Seeders > 0 ? release.Seeders : 0;
            release.Peers = release.Peers > 0 ? release.Peers : 0;
            release.DownloadVolumeFactor = release.DownloadVolumeFactor > 0 ? release.DownloadVolumeFactor : 0;
            release.UploadVolumeFactor = release.UploadVolumeFactor > 0 ? release.UploadVolumeFactor : 1;
            return release;
        }

        protected override Uri FeedUri => new Uri(SiteLink.Replace("://", "://feed.") + "api");
    }
}
