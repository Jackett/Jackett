using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Jackett.Common.Indexers.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class Aro : GazelleTracker
    {
        public override string Id => "aro";
        public override string Name => "aro.lol";
        public override string Description => "aro.lol is a SERBIAN/ENGLISH Private Torrent Tracker for ANIME";
        public override string SiteLink { get; protected set; } = "https://aro.lol/";
        public override string Language => "en-US";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        public Aro(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
                   ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cs: cs,
                   has2Fa: true,
                   supportsFreeleechTokens: true)
        {
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q
                }
            };

            caps.Categories.AddCategoryMapping(1, TorznabCatType.Movies, "Movies");
            caps.Categories.AddCategoryMapping(2, TorznabCatType.TVAnime, "Anime");
            caps.Categories.AddCategoryMapping(3, TorznabCatType.Books, "Manga");
            caps.Categories.AddCategoryMapping(4, TorznabCatType.Console, "Games");
            caps.Categories.AddCategoryMapping(5, TorznabCatType.Other, "Other");

            return caps;
        }
    }
}
