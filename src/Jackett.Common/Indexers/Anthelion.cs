using Jackett.Common.Indexers.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;

namespace Jackett.Common.Indexers
{
    public class TehConnectionMe : GazelleTracker
    {
        public override string[] LegacySiteLinks { get; protected set; } =
        {
            "https://tehconnection.me/"
        };

        public TehConnectionMe(IIndexerConfigurationService configService, WebClient webClient, Logger logger,
                               IProtectionService protectionService) : base(
            name: "Anthelion", // old name: TehConnection.me
            desc: "A movies tracker", link: "https://anthelion.me/", configService: configService, logger: logger,
            protectionService: protectionService, webClient: webClient, supportsFreeleechTokens: true)
        {
            Language = "en-us";
            Type = "private";
            AddCategoryMapping(1, TorznabCatType.Movies, "Feature Film");
            AddCategoryMapping(2, TorznabCatType.Movies, "Short Film");
            AddCategoryMapping(3, TorznabCatType.Movies, "Miniseries");
            AddCategoryMapping(4, TorznabCatType.Movies, "Other");
        }
    }
}
