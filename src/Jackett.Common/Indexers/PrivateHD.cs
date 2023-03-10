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
    public class PrivateHD : AvistazTracker
    {
        public override string Id => "privatehd";
        public override string Name => "PrivateHD";
        public override string Description => "BitTorrent site for High Quality, High Definition (HD) movies and TV Shows";
        public override string SiteLink { get; protected set; } = "https://privatehd.to/";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        public PrivateHD(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
                         ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cs: cs
                   )
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

            return caps;
        }
    }
}
