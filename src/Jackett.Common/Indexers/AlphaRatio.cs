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
    public class AlphaRatio : GazelleTracker
    {
        public AlphaRatio(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(id: "alpharatio",
                   name: "AlphaRatio",
                   description: "AlphaRatio (AR) is a Private Torrent Tracker for 0DAY / GENERAL",
                   link: "https://alpharatio.cc/",
                   caps: new TorznabCapabilities
                   {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
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
                   supportsFreeleechTokens: true,
                   imdbInTags: true)
        {
            Language = "en-us";
            Type = "private";

            AddCategoryMapping(1, TorznabCatType.TVSD, "TV SD");
            AddCategoryMapping(2, TorznabCatType.TVHD, "TV HD");
            AddCategoryMapping(3, TorznabCatType.TVUHD, "TV UHD");
            AddCategoryMapping(4, TorznabCatType.TVSD, "TV DVDRip");
            AddCategoryMapping(5, TorznabCatType.TVSD, "TV Pack SD");
            AddCategoryMapping(6, TorznabCatType.TVHD, "TV Pack HD");
            AddCategoryMapping(7, TorznabCatType.TVUHD, "TV Pack UHD");
            AddCategoryMapping(8, TorznabCatType.MoviesSD, "Movie SD");
            AddCategoryMapping(9, TorznabCatType.MoviesHD, "Movie HD");
            AddCategoryMapping(10, TorznabCatType.MoviesUHD, "Movie UHD");
            AddCategoryMapping(11, TorznabCatType.MoviesSD, "Movie Pack SD");
            AddCategoryMapping(12, TorznabCatType.MoviesHD, "Movie Pack HD");
            AddCategoryMapping(13, TorznabCatType.MoviesUHD, "Movie Pack UHD");
            AddCategoryMapping(14, TorznabCatType.XXX, "XXX Movie");
            AddCategoryMapping(15, TorznabCatType.MoviesBluRay, "Movie Bluray");
            AddCategoryMapping(16, TorznabCatType.TVAnime, "Anime SD");
            AddCategoryMapping(17, TorznabCatType.TVAnime, "Anime HD");
            AddCategoryMapping(18, TorznabCatType.PCGames, "Games PC");
            AddCategoryMapping(19, TorznabCatType.ConsoleXBox, "Games xBox");
            AddCategoryMapping(20, TorznabCatType.ConsolePS4, "Games PS");
            AddCategoryMapping(21, TorznabCatType.ConsoleWii, "Games Nin");
            AddCategoryMapping(22, TorznabCatType.PC0day, "Apps Windows");
            AddCategoryMapping(23, TorznabCatType.PCMac, "Apps MAC");
            AddCategoryMapping(24, TorznabCatType.PC0day, "Apps Linux");
            AddCategoryMapping(25, TorznabCatType.PCMobileOther, "Apps Mobile");
            AddCategoryMapping(26, TorznabCatType.XXX, "XXX 0day");
            AddCategoryMapping(27, TorznabCatType.Books, "eBook");
            AddCategoryMapping(28, TorznabCatType.AudioAudiobook, "AudioBook");
            AddCategoryMapping(29, TorznabCatType.AudioOther, "Music");
            AddCategoryMapping(30, TorznabCatType.Other, "Misc");
        }

        protected override string GetSearchTerm(TorznabQuery query)
        {
            // Ignore season search without episode. Alpharatio doesn't support it.
            var searchTerm = string.IsNullOrWhiteSpace(query.Episode)
                ? query.SanitizedSearchTerm
                : query.GetQueryString();

            // Alpharatio can't handle dots in the searchstr
            return searchTerm.Replace(".", " ");
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = await base.PerformQuery(query);
            foreach (var release in releases)
            {
                release.MinimumRatio = 1;
                release.MinimumSeedTime = 259200;
            }
            return releases;
        }
    }
}
