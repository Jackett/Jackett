using Jackett.Models;
using NLog;
using Jackett.Services;
using Jackett.Utils.Clients;
using Jackett.Indexers.Abstract;

namespace Jackett.Indexers
{
    public class CGPeers : GazelleTracker
    {
        public CGPeers(IIndexerConfigurationService configService, IWebClient webClient, Logger logger, IProtectionService protectionService)
            : base(name: "CGPeers",
                desc: null,
                link: "https://www.cgpeers.com/",
                configService: configService,
                logger: logger,
                protectionService: protectionService,
                webClient: webClient
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