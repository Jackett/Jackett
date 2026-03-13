using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Jackett.Common.Extensions;
using Jackett.Common.Indexers.Definitions.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using NLog;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class AnimeZ : AvistazTracker
    {
        public override string Id => "animez";
        public override string[] Replaces => new[] { "animetorrents" };
        public override string Name => "AnimeZ";
        public override string Description => "AnimeZ (ex-AnimeTorrents) is a Private Torrent Tracker for ANIME / MANGA";
        public override string SiteLink { get; protected set; } = "https://animez.to/";
        public override string[] LegacySiteLinks => new[]
        {
            "https://animetorrents.me/",
        };
        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private new ConfigurationDataAvistaZTracker configData => (ConfigurationDataAvistaZTracker)base.configData;

        public AnimeZ(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps, ICacheService cs)
            : base(configService: configService, client: wc, logger: l, p: ps, cs: cs)
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
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q
                },
                BookSearchParams = new List<BookSearchParam>
                {
                    BookSearchParam.Q,
                }
            };

            caps.Categories.AddCategoryMapping("TV", TorznabCatType.TVAnime, "Anime > TV");
            caps.Categories.AddCategoryMapping("TV_SHORT", TorznabCatType.TVAnime, "Anime > TV Short");
            caps.Categories.AddCategoryMapping("MOVIE", TorznabCatType.Movies, "Anime > Movie");
            caps.Categories.AddCategoryMapping("SPECIAL", TorznabCatType.TVAnime, "Anime > Special");
            caps.Categories.AddCategoryMapping("OVA", TorznabCatType.TVAnime, "Anime > OVA");
            caps.Categories.AddCategoryMapping("ONA", TorznabCatType.TVAnime, "Anime > ONA");
            caps.Categories.AddCategoryMapping("MUSIC", TorznabCatType.TVAnime, "Anime > Music");
            caps.Categories.AddCategoryMapping("MANGA", TorznabCatType.BooksComics, "Manga > Manga");
            caps.Categories.AddCategoryMapping("NOVEL", TorznabCatType.BooksForeign, "Manga > Novel");
            caps.Categories.AddCategoryMapping("ONE_SHOT", TorznabCatType.BooksForeign, "Manga > One-Shot");

            return caps;
        }

        protected override List<KeyValuePair<string, string>> GetSearchQueryParameters(TorznabQuery query)
        {
            var parameters = new List<KeyValuePair<string, string>>
            {
                { "limit", "50" }
            };

            var categoryMappings = MapTorznabCapsToTrackers(query).Distinct().ToList();

            if (categoryMappings.Any())
            {
                foreach (var category in categoryMappings)
                {
                    parameters.Add("format[]", category);
                }
            }

            parameters.Add("search", GetSearchTerm(query).Trim());

            if (query.Limit > 0 && query.Offset > 0)
            {
                var page = query.Offset / query.Limit + 1;
                parameters.Add("page", page.ToString());
            }

            if (configData.Freeleech.Value)
            {
                parameters.Add("freeleech", "1");
            }

            return parameters;
        }

        protected override IReadOnlyList<int> ParseCategories(TorznabQuery query, AvistazRelease row)
        {
            return MapTrackerCatToNewznab(row.Format).ToList();
        }

        protected override string ParseTitle(AvistazRelease row)
        {
            return row.ReleaseTitle.IsNotNullOrWhiteSpace() ? row.ReleaseTitle : row.FileName;
        }
    }
}
