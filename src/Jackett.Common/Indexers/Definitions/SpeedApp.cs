using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Jackett.Common.Indexers.Definitions.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class SpeedApp : SpeedAppTracker
    {
        public override string Id => "speedapp";
        public override string[] Replaces => new[]
        {
            "icetorrent",
            "scenefz",
            "xtremezone"
        };
        public override string Name => "SpeedApp";
        public override string Description => "SpeedApp is a ROMANIAN Private Torrent Tracker for MOVIES / TV / GENERAL";
        public override string SiteLink { get; protected set; } = "https://speedapp.io/";
        public override string[] LegacySiteLinks => new[]
        {
            "https://www.icetorrent.org/",
            "https://icetorrent.org/",
            "https://scenefz.me/",
            "https://www.scenefz.me/",
            "https://www.u-torrents.ro/",
            "https://myxz.eu/",
            "https://www.myxz.eu/",
            "https://www.myxz.org/"
        };
        public override string Language => "ro-RO";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        public SpeedApp(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
                        ICacheService cs)
            : base(configService: configService,

                   client: wc,
                   logger: l,
                   p: ps,
                   cs: cs)
        {
            // requestDelay for API Limit (1 request per 2 seconds)
            webclient.requestDelay = 2.1;
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q, MovieSearchParam.ImdbId
                },
                MusicSearchParams = new List<MusicSearchParam>
                {
                    MusicSearchParam.Q
                },
                BookSearchParams = new List<BookSearchParam>
                {
                    BookSearchParam.Q
                }
            };

            caps.Categories.AddCategoryMapping(38, TorznabCatType.Movies, "Movie Packs");
            caps.Categories.AddCategoryMapping(10, TorznabCatType.MoviesSD, "Movies: SD");
            caps.Categories.AddCategoryMapping(35, TorznabCatType.MoviesSD, "Movies: SD Ro");
            caps.Categories.AddCategoryMapping(8, TorznabCatType.MoviesHD, "Movies: HD");
            caps.Categories.AddCategoryMapping(29, TorznabCatType.MoviesHD, "Movies: HD Ro");
            caps.Categories.AddCategoryMapping(7, TorznabCatType.MoviesDVD, "Movies: DVD");
            caps.Categories.AddCategoryMapping(2, TorznabCatType.MoviesDVD, "Movies: DVD Ro");
            caps.Categories.AddCategoryMapping(17, TorznabCatType.MoviesBluRay, "Movies: BluRay");
            caps.Categories.AddCategoryMapping(24, TorznabCatType.MoviesBluRay, "Movies: BluRay Ro");
            caps.Categories.AddCategoryMapping(59, TorznabCatType.Movies, "Movies: Ro");
            caps.Categories.AddCategoryMapping(57, TorznabCatType.MoviesUHD, "Movies: 4K (2160p) Ro");
            caps.Categories.AddCategoryMapping(61, TorznabCatType.MoviesUHD, "Movies: 4K (2160p)");
            caps.Categories.AddCategoryMapping(41, TorznabCatType.TV, "TV Packs");
            caps.Categories.AddCategoryMapping(66, TorznabCatType.TV, "TV Packs Ro");
            caps.Categories.AddCategoryMapping(45, TorznabCatType.TVSD, "TV Episodes");
            caps.Categories.AddCategoryMapping(46, TorznabCatType.TVSD, "TV Episodes Ro");
            caps.Categories.AddCategoryMapping(43, TorznabCatType.TVHD, "TV Episodes HD");
            caps.Categories.AddCategoryMapping(44, TorznabCatType.TVHD, "TV Episodes HD Ro");
            caps.Categories.AddCategoryMapping(60, TorznabCatType.TV, "TV Ro");
            caps.Categories.AddCategoryMapping(11, TorznabCatType.PCGames, "Games: PC-ISO");
            caps.Categories.AddCategoryMapping(52, TorznabCatType.Console, "Games: Console");
            caps.Categories.AddCategoryMapping(1, TorznabCatType.PC0day, "Applications");
            caps.Categories.AddCategoryMapping(14, TorznabCatType.PC, "Applications: Linux");
            caps.Categories.AddCategoryMapping(37, TorznabCatType.PCMac, "Applications: Mac");
            caps.Categories.AddCategoryMapping(19, TorznabCatType.PCMobileOther, "Applications: Mobile");
            caps.Categories.AddCategoryMapping(62, TorznabCatType.TV, "TV Cartoons");
            caps.Categories.AddCategoryMapping(3, TorznabCatType.TVAnime, "TV Anime / Hentai");
            caps.Categories.AddCategoryMapping(6, TorznabCatType.BooksEBook, "E-books");
            caps.Categories.AddCategoryMapping(5, TorznabCatType.Audio, "Music");
            caps.Categories.AddCategoryMapping(64, TorznabCatType.AudioVideo, "Music Video");
            caps.Categories.AddCategoryMapping(18, TorznabCatType.Other, "Images");
            caps.Categories.AddCategoryMapping(22, TorznabCatType.TVSport, "TV Sports");
            caps.Categories.AddCategoryMapping(58, TorznabCatType.TVSport, "TV Sports Ro");
            caps.Categories.AddCategoryMapping(9, TorznabCatType.TVDocumentary, "TV Documentary");
            caps.Categories.AddCategoryMapping(63, TorznabCatType.TVDocumentary, "TV Documentary Ro");
            caps.Categories.AddCategoryMapping(65, TorznabCatType.Other, "Tutorial");
            caps.Categories.AddCategoryMapping(67, TorznabCatType.OtherMisc, "Miscellaneous");
            caps.Categories.AddCategoryMapping(15, TorznabCatType.XXX, "XXX Movies");
            caps.Categories.AddCategoryMapping(47, TorznabCatType.XXX, "XXX DVD");
            caps.Categories.AddCategoryMapping(48, TorznabCatType.XXX, "XXX HD");
            caps.Categories.AddCategoryMapping(49, TorznabCatType.XXXImageSet, "XXX Images");
            caps.Categories.AddCategoryMapping(50, TorznabCatType.XXX, "XXX Packs");
            caps.Categories.AddCategoryMapping(51, TorznabCatType.XXX, "XXX SD");

            return caps;
        }
    }
}
