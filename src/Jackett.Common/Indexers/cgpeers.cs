using Jackett.Common.Indexers.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;

namespace Jackett.Common.Indexers
{
    public class CGPeers : GazelleTracker
    {
        public CGPeers(IIndexerConfigurationService configService, WebClient webClient, Logger logger, IProtectionService protectionService)
            : base(name: "CGPeers",
                desc: "CGPeers is a Private Torrent Tracker for GRAPHICS SOFTWARE / TUTORIALS / ETC",
                link: "https://www.cgpeers.com/",
                configService: configService,
                logger: logger,
                protectionService: protectionService,
                webClient: webClient,
                supportsFreeleechTokens: true
                )
        {
            Language = "en-us";
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
