using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
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
        public Aro(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "aro",
                   name: "aro.lol",
                   description: "aro.lol is a SERBIAN/ENGLISH Private Torrent Tracker for ANIME",
                   link: "https://aro.lol/",
                   caps: new TorznabCapabilities
                   {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q
                       },
                       MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q
                       }
                   },
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cs: cs,
                   has2Fa: true,
                   supportsFreeleechTokens: true
                   )
        {
            Language = "en-US";
            Type = "private";

            AddCategoryMapping(1, TorznabCatType.Movies, "Movies");
            AddCategoryMapping(2, TorznabCatType.TVAnime, "Anime");
            AddCategoryMapping(3, TorznabCatType.Books, "Manga");
            AddCategoryMapping(4, TorznabCatType.Console, "Games");
            AddCategoryMapping(5, TorznabCatType.Other, "Other");
        }
    }
}
