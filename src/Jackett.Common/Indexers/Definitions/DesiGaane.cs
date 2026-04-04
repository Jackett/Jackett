using System.Diagnostics.CodeAnalysis;
using Jackett.Common.Indexers.Definitions.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class DesiGaane : GazelleTracker
    {
        public override string Id => "desigaane";
        public override string Name => "DesiGaane";
        public override string Description => "DesiGaane is a Private Torrent Tracker for DESI MUSIC";
        public override string SiteLink { get; protected set; } = "https://desigaane.rocks/";

        public override string Language => "en-US";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        public DesiGaane(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
                            ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cs: cs,
                   supportsFreeleechTokens: true,
                   has2Fa: true)
        {
            configData.AddDynamic("Account Inactivity", new DisplayInfoConfigurationItem("Account Inactivity", "To keep your account active, sign in and browse the site at least once every 90 days. Seeding torrents does not count as account activity."));
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities();

            caps.Categories.AddCategoryMapping(1, TorznabCatType.Audio, "Music");

            return caps;
        }
    }
}
