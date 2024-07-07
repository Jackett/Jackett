using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Jackett.Common.Extensions;
using Jackett.Common.Indexers.Definitions.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class AvistaZ : AvistazTracker
    {
        public override string Id => "avistaz";
        public override string Name => "AvistaZ";
        public override string Description => "Aka AsiaTorrents";
        public override string SiteLink { get; protected set; } = "https://avistaz.to/";

        protected override string TimezoneOffset => "+02:00";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        public AvistaZ(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
                       ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cs: cs)
        {
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                LimitsDefault = 50,
                LimitsMax = 50,
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId, TvSearchParam.TvdbId, TvSearchParam.Genre
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q, MovieSearchParam.ImdbId, MovieSearchParam.TmdbId, MovieSearchParam.Genre
                },
                SupportsRawSearch = true,
                TvSearchImdbAvailable = true
            };

            caps.Categories.AddCategoryMapping(1, TorznabCatType.Movies);
            caps.Categories.AddCategoryMapping(1, TorznabCatType.MoviesUHD);
            caps.Categories.AddCategoryMapping(1, TorznabCatType.MoviesHD);
            caps.Categories.AddCategoryMapping(1, TorznabCatType.MoviesSD);
            caps.Categories.AddCategoryMapping(2, TorznabCatType.TV);
            caps.Categories.AddCategoryMapping(2, TorznabCatType.TVUHD);
            caps.Categories.AddCategoryMapping(2, TorznabCatType.TVHD);
            caps.Categories.AddCategoryMapping(2, TorznabCatType.TVSD);
            caps.Categories.AddCategoryMapping(3, TorznabCatType.Audio);

            return caps;
        }

        // Avistaz has episodes without season. eg Running Man E323
        protected override string GetEpisodeSearchTerm(TorznabQuery query) =>
            (query.Season == null || query.Season == 0) && query.Episode.IsNotNullOrWhiteSpace()
                ? $"E{query.Episode}"
                : $"{query.GetEpisodeSearchString()}";
    }
}
