using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Newtonsoft.Json.Linq;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers.Definitions.Abstract
{
    /// <summary>
    /// Base class for Spanish-language public torrent trackers.
    /// Provides shared defaults (Language, Type, Description template, default category mappings,
    /// and an authless ApplyConfiguration). Concrete indexers must override:
    /// <list type="bullet">
    /// <item><description><see cref="IndexerBase.Id"/>, <see cref="IndexerBase.Name"/>, <see cref="IndexerBase.SiteLink"/></description></item>
    /// <item><description><see cref="IndexerBase.GetRequestGenerator"/> and <see cref="IndexerBase.GetParser"/></description></item>
    /// </list>
    /// Concrete indexers MAY override:
    /// <list type="bullet">
    /// <item><description><see cref="Description"/> if their site description differs from the default template</description></item>
    /// <item><description><see cref="TorznabCaps"/> if their site exposes categories beyond peliculas/series/animes</description></item>
    /// </list>
    /// This base does not set <c>webclient.requestDelay</c>; consumers should set it per-indexer if rate limiting is needed.
    /// It also does not include parsing helpers (those are per-indexer because Spanish trackers vary in API/HTML shape).
    /// </summary>
    public abstract class PublicSpanishIndexerBase : IndexerBase
    {
        protected PublicSpanishIndexerBase(IIndexerConfigurationService configService, WebClient wc, Logger l,
                                           IProtectionService ps, ICacheService cs)
            : base(configService: configService, client: wc, logger: l, p: ps, cacheService: cs,
                   configData: new ConfigurationData())
        {
        }

        public override string Description =>
            $"{Name} is a Public Torrent Tracker for Movies, Series and Anime in Spanish";

        public override string Language => "es-ES";
        public override string Type => "public";
        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                MovieSearchParams = new List<MovieSearchParam> { MovieSearchParam.Q },
                TvSearchParams = new List<TvSearchParam> { TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep }
            };
            caps.Categories.AddCategoryMapping("peliculas", TorznabCatType.Movies);
            caps.Categories.AddCategoryMapping("series", TorznabCatType.TV);
            caps.Categories.AddCategoryMapping("animes", TorznabCatType.TVAnime);
            return caps;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            await ConfigureIfOK(string.Empty, true,
                () => throw new Exception("Could not find releases from this URL"));
            return IndexerConfigurationStatus.Completed;
        }
    }
}
