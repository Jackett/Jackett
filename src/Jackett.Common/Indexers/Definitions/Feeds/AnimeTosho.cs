using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Jackett.Common.Indexers.Feeds;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;

namespace Jackett.Common.Indexers.Definitions.Feeds
{
    [ExcludeFromCodeCoverage]
    public class AnimeTosho : BaseNewznabIndexer
    {
        public override string Id => "animetosho";
        public override string Name => "Anime Tosho";
        public override string Description => "AnimeTosho (AT) is an automated service that provides torrent files, magnet links and DDL for all anime releases";
        public override string SiteLink { get; protected set; } = "https://animetosho.org/";
        public override string Language => "en-US";
        public override string Type => "public";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        public AnimeTosho(IIndexerConfigurationService configService, WebClient client, Logger logger, IProtectionService ps, ICacheService cs)
            : base(configService: configService,
                   client: client,
                   logger: logger,
                   p: ps,
                   cs: cs,
                   configData: new ConfigurationData())
        {
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                }
            };

            caps.Categories.AddCategoryMapping(1, TorznabCatType.TVAnime);

            return caps;
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
            var enclosure = item.Descendants("enclosure").FirstOrDefault(e => e.Attribute("type").Value == "application/x-bittorrent");
            if (enclosure != null)
            {
                var enclosureUrl = enclosure.Attribute("url").Value;
                release.Link = new Uri(enclosureUrl);
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
