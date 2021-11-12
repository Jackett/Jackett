using System.Diagnostics.CodeAnalysis;
using Jackett.Common.Indexers.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class CGPeers : GazelleTracker
    {
        public override string[] LegacySiteLinks { get; protected set; } = {
            "https://www.cgpeers.com/"
        };

        public CGPeers(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "cgpeers",
                   name: "CGPeers",
                   description: "CGPeers is a Private Torrent Tracker for GRAPHICS SOFTWARE / TUTORIALS / ETC",
                   link: "https://cgpeers.to/",
                   caps: new TorznabCapabilities(),
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cs: cs,
                   supportsFreeleechTokens: true)
        {
            Language = "en-US";
            Type = "private";

            AddCategoryMapping(1, TorznabCatType.PCISO, "Full Applications");
            AddCategoryMapping(2, TorznabCatType.PC0day, "Plugins");
            AddCategoryMapping(3, TorznabCatType.Other, "Tutorials");
            AddCategoryMapping(4, TorznabCatType.Other, "Models");
            AddCategoryMapping(5, TorznabCatType.Other, "Materials");
            AddCategoryMapping(6, TorznabCatType.OtherMisc, "Misc");
            AddCategoryMapping(7, TorznabCatType.Other, "GameDev");
        }
    }
}
