using Jackett.Common.Indexers.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;

namespace Jackett.Common.Indexers
{
    public class AlphaRatio : GazelleTracker
    {
        public AlphaRatio(IIndexerConfigurationService configService, WebClient webClient, Logger logger, IProtectionService protectionService)
            : base(name: "AlphaRatio",
                desc: "AlphaRatio (AR) is a Private Torrent Tracker for 0DAY / GENERAL",
                link: "https://alpharatio.cc/",
                configService: configService,
                logger: logger,
                protectionService: protectionService,
                webClient: webClient,
                supportsFreeleechTokens: true
                )
        {
            Language = "en-us";
            Type = "private";

            AddCategoryMapping(1, TorznabCatType.TVSD, "TvSD");
            AddCategoryMapping(2, TorznabCatType.TVHD, "TvHD");
            AddCategoryMapping(3, TorznabCatType.TVSD, "TvDVDRip");
            AddCategoryMapping(4, TorznabCatType.TVSD, "TvPackSD");
            AddCategoryMapping(5, TorznabCatType.TVHD, "TvPackHD");
            AddCategoryMapping(6, TorznabCatType.MoviesSD, "MovieSD");
            AddCategoryMapping(7, TorznabCatType.MoviesHD, "MovieHD");
            AddCategoryMapping(8, TorznabCatType.MoviesSD, "MoviePackSD");
            AddCategoryMapping(9, TorznabCatType.MoviesHD, "MoviePackHD");
            AddCategoryMapping(10, TorznabCatType.XXX, "MovieXXX");
            AddCategoryMapping(11, TorznabCatType.AudioVideo, "MviD");
            AddCategoryMapping(12, TorznabCatType.PCGames, "GamesPC");
            AddCategoryMapping(13, TorznabCatType.ConsoleXbox, "GamesxBox");
            AddCategoryMapping(14, TorznabCatType.ConsolePS3, "GamesPS3");
            AddCategoryMapping(15, TorznabCatType.ConsoleWii, "GamesWii");
            AddCategoryMapping(16, TorznabCatType.PC0day, "AppsPC");
            AddCategoryMapping(17, TorznabCatType.PCMac, "AppsMAC");
            AddCategoryMapping(18, TorznabCatType.PC0day, "AppsLinux");
            AddCategoryMapping(19, TorznabCatType.PCPhoneOther, "AppsMobile");
            AddCategoryMapping(20, TorznabCatType.XXX, "0dayXXX");
            AddCategoryMapping(21, TorznabCatType.Books, "eBook");
            AddCategoryMapping(22, TorznabCatType.AudioAudiobook, "AudioBook");
            AddCategoryMapping(23, TorznabCatType.AudioOther, "Music");
            AddCategoryMapping(24, TorznabCatType.Other, "Misc");
        }
    }
}
