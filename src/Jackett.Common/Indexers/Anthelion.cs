using System.Diagnostics.CodeAnalysis;
using Jackett.Common.Indexers.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class Anthelion : GazelleTracker
    {
        public override string[] LegacySiteLinks { get; protected set; } = new string[] {
            "https://tehconnection.me/",
        };

        public Anthelion(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(id: "tehconnectionme",
                   name: "Anthelion", // old name: TehConnection.me
                   description: "A movies tracker",
                   link: "https://anthelion.me/",
                   caps: new TorznabCapabilities(),
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   supportsFreeleechTokens: true)
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
