using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Jackett.Common.Indexers.Definitions.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class Simurg : GazelleTracker
    {
        public override string Id => "simurg";
        public override string Name => "Simurg";
        public override string Description => "Simurg is a Private Torrent Tracker for EBOOKS and AUDIOBOOKS";
        public override string SiteLink { get; protected set; } = "https://simurg.world/";

        public override string Language => "en-US";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();
        protected override int ApiKeyLength => 116;
        protected override string AuthorizationFormat => "token {0}";

        public Simurg(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
                            ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cs: cs,
                   supportsFreeleechTokens: true,
                   useApiKey: true)
        {
            configData.AddDynamic("Account Inactivity", new DisplayInfoConfigurationItem("Account Inactivity", "To keep your account active, sign in and browse the site at least once every 90 days. Seeding torrents does not count as account activity."));
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities();

            caps.Categories.AddCategoryMapping(3, TorznabCatType.BooksEBook, "E-Books");
            caps.Categories.AddCategoryMapping(4, TorznabCatType.AudioAudiobook, "Audiobooks");

            return caps;
        }

        protected override Uri GetDownloadUrl(int torrentId, bool canUseToken)
        {
            return new Uri($"{SiteLink}ajax.php?action=download{(useTokens && canUseToken ? "&usetoken=1" : "")}&id={torrentId}");
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = await base.PerformQuery(query);
            foreach (var release in releases)
            {
                // the site has a proportional ratio system calculated using (1) the total amount of data you've downloaded and (2) the total number of torrents you're seeding.
                // So we are going to default the MR to the maximim ratio required to cover the whole range as we cannot calculate this for each user.
                release.MinimumRatio = 0.6;
                release.MinimumSeedTime = 259200;
            }
            return releases;
        }
    }
}
