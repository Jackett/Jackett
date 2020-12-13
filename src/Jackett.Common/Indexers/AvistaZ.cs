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
    public class AvistaZ : AvistazTracker
    {
        public AvistaZ(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "avistaz",
                   name: "AvistaZ",
                   description: "Aka AsiaTorrents",
                   link: "https://avistaz.to/",
                   caps: new TorznabCapabilities
                   {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId
                       },
                       MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q, MovieSearchParam.ImdbId
                       }
                   },
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cs: cs
                   )
        {
            AddCategoryMapping(1, TorznabCatType.Movies);
            AddCategoryMapping(1, TorznabCatType.MoviesUHD);
            AddCategoryMapping(1, TorznabCatType.MoviesHD);
            AddCategoryMapping(1, TorznabCatType.MoviesSD);
            AddCategoryMapping(2, TorznabCatType.TV);
            AddCategoryMapping(2, TorznabCatType.TVUHD);
            AddCategoryMapping(2, TorznabCatType.TVHD);
            AddCategoryMapping(2, TorznabCatType.TVSD);
            AddCategoryMapping(3, TorznabCatType.Audio);
        }

        // Avistaz has episodes without season. eg Running Man E323
        protected override string GetSearchTerm(TorznabQuery query) =>
            !string.IsNullOrWhiteSpace(query.Episode) && query.Season == 0 ?
            $"{query.SearchTerm} E{query.Episode}" :
            $"{query.SearchTerm} {query.GetEpisodeSearchString()}";
    }
}
