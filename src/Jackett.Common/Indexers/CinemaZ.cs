using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Jackett.Common.Indexers.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class CinemaZ : AvistazTracker
    {
        public override string Id => "cinemaz";
        public override string Name => "CinemaZ";
        public override string Description => "Part of the Avistaz network.";
        public override string SiteLink { get; protected set; } = "https://cinemaz.to/";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        public CinemaZ(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
                       ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cs: cs
                   )
        {
            configData.AddDynamic("Account Inactivity", new DisplayInfoConfigurationItem("Account Inactivity", "To avoid account deletion you must login at least 1 time every 90 days, and you must download at least 1 torrent every 6 months. Simply keeping torrents seeding long term will not protect your account. Do not rely on inactivity emails, CinemaZ often do not send them."));
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                LimitsDefault = 50,
                LimitsMax = 50,
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId, TvSearchParam.Genre
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q, MovieSearchParam.ImdbId, MovieSearchParam.Genre
                }
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
