using System.Diagnostics.CodeAnalysis;
using Jackett.Common.Indexers.Definitions.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class CGPeers : GazelleTracker
    {
        public override string Id => "cgpeers";
        public override string Name => "CGPeers";
        public override string Description => "CGPeers is a Private Torrent Tracker for GRAPHICS SOFTWARE / TUTORIALS / ETC";
        public override string SiteLink { get; protected set; } = "https://cgpeers.to/";
        public override string[] LegacySiteLinks => new[]
        {
            "https://www.cgpeers.com/"
        };
        public override string Language => "en-US";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        public CGPeers(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
                       ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cs: cs,
                   supportsFreeleechTokens: true,
                   has2Fa: true)
        {
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities();

            caps.Categories.AddCategoryMapping(1, TorznabCatType.PCISO, "Full Applications");
            caps.Categories.AddCategoryMapping(2, TorznabCatType.PC0day, "Plugins");
            caps.Categories.AddCategoryMapping(3, TorznabCatType.Other, "Tutorials");
            caps.Categories.AddCategoryMapping(4, TorznabCatType.Other, "Models");
            caps.Categories.AddCategoryMapping(5, TorznabCatType.Other, "Materials");
            caps.Categories.AddCategoryMapping(6, TorznabCatType.OtherMisc, "Misc");
            caps.Categories.AddCategoryMapping(7, TorznabCatType.Other, "GameDev");

            return caps;
        }
    }
}
