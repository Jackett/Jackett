using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Jackett.Common.Extensions;
using Jackett.Common.Indexers.Definitions.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Cache;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using NLog;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class AvistaZ : AvistazTracker
    {
        public override string Id => "avistaz";
        public override string Name => "AvistaZ";
        public override string Description => "AvistaZ (AsiaTorrents) is a Private Torrent Tracker for ASIAN MOVIES / TV / GENERAL";
        public override string SiteLink { get; protected set; } = "https://avistaz.to/";
        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private new ConfigurationDataAvistaZ configData => (ConfigurationDataAvistaZ)base.configData;

        public AvistaZ(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
                       CacheManager cm)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheManager: cm,
                   configData: new ConfigurationDataAvistaZ())
        {
        }

        private static TorznabCapabilities SetCapabilities()
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

        protected override List<KeyValuePair<string, string>> GetSearchQueryParameters(TorznabQuery query)
        {
            var qc = base.GetSearchQueryParameters(query);

            foreach (var languageId in configData.SearchAudioLanguages.Values.Distinct())
            {
                qc.Add("language[]", languageId);
            }

            foreach (var languageId in configData.SearchSubtitleLanguages.Values.Distinct())
            {
                qc.Add("subtitle[]", languageId);
            }

            return qc;
        }

        // Avistaz has episodes without season. eg Running Man E323
        protected override string GetEpisodeSearchTerm(TorznabQuery query) =>
            (query.Season == null || query.Season == 0) && query.Episode.IsNotNullOrWhiteSpace()
                ? $"E{query.Episode}"
                : $"{query.GetEpisodeSearchString()}";
    }
}
