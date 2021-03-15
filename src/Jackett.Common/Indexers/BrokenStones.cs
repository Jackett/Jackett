using System.Diagnostics.CodeAnalysis;
using Jackett.Common.Indexers.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class BrokenStones : GazelleTracker
    {
        public BrokenStones(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "brokenstones",
                   name: "BrokenStones",
                   description: "Broken Stones is a Private site for MacOS and iOS APPS / GAMES",
                   link: "https://brokenstones.club/",
                   caps: new TorznabCapabilities(),
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cs: cs,
                   supportsFreeleechTokens: true)
        {
            Language = "en-us";
            Type = "private";

            AddCategoryMapping(1, TorznabCatType.PCMac, "MacOS Apps");
            AddCategoryMapping(2, TorznabCatType.PCMac, "MacOS Games");
            AddCategoryMapping(3, TorznabCatType.PCMobileiOS, "iOS Apps");
            AddCategoryMapping(4, TorznabCatType.PCMobileiOS, "iOS Games");
            AddCategoryMapping(5, TorznabCatType.Other, "Graphics");
            AddCategoryMapping(6, TorznabCatType.Audio, "Audio");
            AddCategoryMapping(7, TorznabCatType.Other, "Tutorials");
            AddCategoryMapping(8, TorznabCatType.Other, "Other");
        }
    }
}
