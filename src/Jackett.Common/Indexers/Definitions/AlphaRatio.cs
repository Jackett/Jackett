using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Jackett.Common.Indexers.Definitions.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class AlphaRatio : GazelleTracker
    {
        public override string Id => "alpharatio";
        public override string Name => "AlphaRatio";
        public override string Description => "AlphaRatio (AR) is a Private Torrent Tracker for 0DAY / GENERAL";
        // Status: https://ar.trackerstatus.info/
        public override string SiteLink { get; protected set; } = "https://alpharatio.cc/";
        public override string Language => "en-US";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        public AlphaRatio(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
                          ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cs: cs,
                   supportsFreeleechTokens: true,
                   supportsFreeleechOnly: true,
                   imdbInTags: true)
        {
            configData.AddDynamic("Account Inactivity", new DisplayInfoConfigurationItem("Account Inactivity", "Users must login to the site at least once every 90 days. Failure to do so results in the user account being suspended and automatically demoted to Exiled user class."));
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q, MovieSearchParam.ImdbId
                }
            };

            caps.Categories.AddCategoryMapping(1, TorznabCatType.TVSD, "TvSD");
            caps.Categories.AddCategoryMapping(2, TorznabCatType.TVHD, "TvHD");
            caps.Categories.AddCategoryMapping(3, TorznabCatType.TVUHD, "TvUHD");
            caps.Categories.AddCategoryMapping(4, TorznabCatType.TVSD, "TvDVDRip");
            caps.Categories.AddCategoryMapping(5, TorznabCatType.TVSD, "TvPackSD");
            caps.Categories.AddCategoryMapping(6, TorznabCatType.TVHD, "TvPackHD");
            caps.Categories.AddCategoryMapping(7, TorznabCatType.TVUHD, "TvPackUHD");
            caps.Categories.AddCategoryMapping(8, TorznabCatType.MoviesSD, "MovieSD");
            caps.Categories.AddCategoryMapping(9, TorznabCatType.MoviesHD, "MovieHD");
            caps.Categories.AddCategoryMapping(10, TorznabCatType.MoviesUHD, "MovieUHD");
            caps.Categories.AddCategoryMapping(11, TorznabCatType.MoviesSD, "MoviePackSD");
            caps.Categories.AddCategoryMapping(12, TorznabCatType.MoviesHD, "MoviePackHD");
            caps.Categories.AddCategoryMapping(13, TorznabCatType.MoviesUHD, "MoviePackUHD");
            caps.Categories.AddCategoryMapping(14, TorznabCatType.XXX, "MovieXXX");
            caps.Categories.AddCategoryMapping(15, TorznabCatType.MoviesBluRay, "Bluray");
            caps.Categories.AddCategoryMapping(16, TorznabCatType.TVAnime, "AnimeSD");
            caps.Categories.AddCategoryMapping(17, TorznabCatType.TVAnime, "AnimeHD");
            caps.Categories.AddCategoryMapping(18, TorznabCatType.PCGames, "GamesPC");
            caps.Categories.AddCategoryMapping(19, TorznabCatType.ConsoleXBox, "GamesxBox");
            caps.Categories.AddCategoryMapping(20, TorznabCatType.ConsolePS4, "GamesPS");
            caps.Categories.AddCategoryMapping(21, TorznabCatType.ConsoleWii, "GamesNin");
            caps.Categories.AddCategoryMapping(22, TorznabCatType.PC0day, "AppsWindows");
            caps.Categories.AddCategoryMapping(23, TorznabCatType.PCMac, "AppsMAC");
            caps.Categories.AddCategoryMapping(24, TorznabCatType.PC0day, "AppsLinux");
            caps.Categories.AddCategoryMapping(25, TorznabCatType.PCMobileOther, "AppsMobile");
            caps.Categories.AddCategoryMapping(26, TorznabCatType.XXX, "0dayXXX");
            caps.Categories.AddCategoryMapping(27, TorznabCatType.Books, "eBook");
            caps.Categories.AddCategoryMapping(28, TorznabCatType.AudioAudiobook, "AudioBook");
            caps.Categories.AddCategoryMapping(29, TorznabCatType.AudioOther, "Music");
            caps.Categories.AddCategoryMapping(30, TorznabCatType.Other, "Misc");

            return caps;
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
