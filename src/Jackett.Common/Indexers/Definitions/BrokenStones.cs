using System.Diagnostics.CodeAnalysis;
using Jackett.Common.Indexers.Definitions.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class BrokenStones : GazelleTracker
    {
        public override string Id => "brokenstones";
        public override string Name => "BrokenStones";
        public override string Description => "Broken Stones is a Private site for MacOS and iOS APPS / GAMES";
        public override string SiteLink { get; protected set; } = "https://brokenstones.is/";
        public override string[] LegacySiteLinks => new[]
        {
            "https://brokenstones.club/",
            "https://broken-stones.club/"
        };
        public override string Language => "en-US";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        public BrokenStones(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
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

            caps.Categories.AddCategoryMapping(1, TorznabCatType.PCMac, "MacOS Apps");
            caps.Categories.AddCategoryMapping(2, TorznabCatType.PCMac, "MacOS Games");
            caps.Categories.AddCategoryMapping(3, TorznabCatType.PCMobileiOS, "iOS Apps");
            caps.Categories.AddCategoryMapping(4, TorznabCatType.PCMobileiOS, "iOS Games");
            caps.Categories.AddCategoryMapping(5, TorznabCatType.Other, "Graphics");
            caps.Categories.AddCategoryMapping(6, TorznabCatType.Audio, "Audio");
            caps.Categories.AddCategoryMapping(7, TorznabCatType.Other, "Tutorials");
            caps.Categories.AddCategoryMapping(8, TorznabCatType.Other, "Other");

            return caps;
        }
    }
}
