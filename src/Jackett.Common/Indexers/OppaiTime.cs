using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Jackett.Common.Indexers.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Newtonsoft.Json.Linq;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class OppaiTime : GazelleTracker
    {
        public OppaiTime(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "oppaitime",
                   name: "OppaiTime",
                   description: "A porn tracker",
                   link: "https://oppaiti.me/",
                   caps: new TorznabCapabilities
                   {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q
                       },
                       MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q
                       },
                       MusicSearchParams = new List<MusicSearchParam>
                       {
                           MusicSearchParam.Q, MusicSearchParam.Artist
                       },
                       BookSearchParams = new List<BookSearchParam>
                       {
                           BookSearchParam.Q
                       }
                   },
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cs: cs,
                   supportsFreeleechTokens: true,
                   has2Fa: false,
                   useApiKey: false
                )
        {
            Language = "en-US";
            Type = "private";

            AddCategoryMapping(1, TorznabCatType.Movies, "Movies");
            AddCategoryMapping(2, TorznabCatType.TVAnime, "Anime");
            AddCategoryMapping(3, TorznabCatType.Books, "Manga");
            AddCategoryMapping(4, TorznabCatType.Console, "Games");
            AddCategoryMapping(5, TorznabCatType.Audio, "Audio");
            AddCategoryMapping(6, TorznabCatType.Other, "Other");
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = await base.PerformQuery(query);
            var releaseInfos = releases.ToList();
            if (query.Categories.Length <= 0)
                return releaseInfos;
            var categories = TorznabCaps.Categories.ExpandTorznabQueryCategories(query);

            // Oppaitime does not provide category information, resulting in all results being filtered.
            // This implementation ensures that results never gets filtered
            foreach (var release in releaseInfos)
                release.Category = categories;
            return releaseInfos;
        }
    }
}

