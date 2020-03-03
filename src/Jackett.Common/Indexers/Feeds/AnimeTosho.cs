using System;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using NLog;

namespace Jackett.Common.Indexers.Feeds
{
    public class AnimeTosho : BaseNewznabIndexer
    {
        public AnimeTosho(IIndexerConfigurationService configService, WebClient client, Logger logger, IProtectionService p)
            : base(
                "Anime Tosho",
                "https://animetosho.org/",
                "AnimeTosho (AT) is an automated service that provides torrent files, magnet links and DDL for all anime releases",
                configService,
                client,
                logger,
                new ConfigurationData(),
                p)
        {
            // TODO
            // this might be downloaded and refreshed instead of hard-coding it
            TorznabCaps = new TorznabCapabilities(new TorznabCategory(5070, "Anime"))
            {
                SearchAvailable = true,
                TVSearchAvailable = false,
                MovieSearchAvailable = false,
                SupportsImdbMovieSearch = false,
                SupportsTVRageSearch = false
            };

            Encoding = Encoding.UTF8;
            Language = "en-en";
            Type = "public";
        }

        protected override ReleaseInfo ResultFromFeedItem(XElement item)
        {
            var release = base.ResultFromFeedItem(item);
            var enclosures = item.Descendants("enclosure").Where(e => e.Attribute("type").Value == "application/x-bittorrent");
            if (enclosures.Any())
            {
                var enclosure = enclosures.First().Attribute("url").Value;
                release.Link = enclosure.ToUri();
            }
            // add some default values if none returned by feed
            release.Seeders = release.Seeders > 0 ? release.Seeders : 0;
            release.Peers = release.Peers > 0 ? release.Peers : 0;
            release.MinimumRatio = 1;
            release.MinimumSeedTime = 172800; // 48 hours
            release.DownloadVolumeFactor = release.DownloadVolumeFactor > 0 ? release.DownloadVolumeFactor : 0;
            release.UploadVolumeFactor = release.UploadVolumeFactor > 0 ? release.UploadVolumeFactor : 1;
            return release;
        }

        protected override Uri FeedUri => new Uri(SiteLink.Replace("://", "://feed.") + "api");
    }
}
