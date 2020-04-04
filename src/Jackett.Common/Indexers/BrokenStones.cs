using Jackett.Common.Indexers.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;

namespace Jackett.Common.Indexers
{
    public class BrokenStones : GazelleTracker
    {
        public BrokenStones(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base("BrokenStones",
                   description: "Broken Stones is a Private site for MacOS and iOS APPS / GAMES",
                   link: "https://brokenstones.club/",
                   caps: new TorznabCapabilities(),
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   supportsFreeleechTokens: true)
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
