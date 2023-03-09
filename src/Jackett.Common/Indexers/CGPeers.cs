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

        public CGPeers(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(caps: new TorznabCapabilities(),
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cs: cs,
                   supportsFreeleechTokens: true)
        {
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
