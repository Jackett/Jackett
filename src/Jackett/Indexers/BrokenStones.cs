using Jackett.Models;
using NLog;
using Jackett.Services;
using Jackett.Utils.Clients;
using Jackett.Indexers.Abstract;

namespace Jackett.Indexers
{
    public class BrokenStones : GazelleTracker
    {
        public BrokenStones(IIndexerConfigurationService configService, IWebClient webClient, Logger logger, IProtectionService protectionService)
            : base(name: "BrokenStones",
                desc: null,
                link: "https://brokenstones.club/",
                configService: configService,
                logger: logger,
                protectionService: protectionService,
                webClient: webClient
                )
        {
            Language = "en-us";
            Type = "private";

            AddCategoryMapping(1, TorznabCatType.PCMac, "MacOS Apps");
            AddCategoryMapping(2, TorznabCatType.PCMac, "MacOS Games");
            AddCategoryMapping(3, TorznabCatType.PCPhoneIOS, "iOS Apps");
            AddCategoryMapping(4, TorznabCatType.PCPhoneIOS, "iOS Games");
            AddCategoryMapping(5, TorznabCatType.Other, "Graphics");
            AddCategoryMapping(6, TorznabCatType.Audio, "Audio");
            AddCategoryMapping(7, TorznabCatType.Other, "Tutorials");
            AddCategoryMapping(8, TorznabCatType.Other, "Other");
        }
    }
}