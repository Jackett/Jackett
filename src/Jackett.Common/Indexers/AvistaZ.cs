using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Jackett.Common.Extensions;
using Jackett.Common.Indexers.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class AvistaZ : AvistazTracker
    {
        public override string Id => "avistaz";
        public override string Name => "AvistaZ";
        public override string Description => "Aka AsiaTorrents";
        public override string SiteLink { get; protected set; } = "https://avistaz.to/";

        protected override string TimezoneOffset => "+01:00";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        public AvistaZ(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
                       ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cs: cs)
        {
            configData.AddDynamic("Account Inactivity", new DisplayInfoConfigurationItem("Account Inactivity", "To avoid account deletion you must login at least 1 time every 90 days, and you must download at least 1 torrent every 6 months. Simply keeping torrents seeding long term will not protect your account. Do not rely on inactivity emails, we often do not send them."));
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
            query.Season == 0 && query.Episode.IsNotNullOrWhiteSpace()
                ? $"E{query.Episode}"
                : $"{query.GetEpisodeSearchString()}";
    }
}
