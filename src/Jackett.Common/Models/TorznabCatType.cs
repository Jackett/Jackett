using System.Collections.Generic;
using System.Linq;

namespace Jackett.Common.Models
{
    public static class TorznabCatType
    {
        public static readonly TorznabCategory Console = new TorznabCategory(1000, "Console");
        public static readonly TorznabCategory ConsoleNDS = new TorznabCategory(1010, "Console/NDS");
        public static readonly TorznabCategory ConsolePSP = new TorznabCategory(1020, "Console/PSP");
        public static readonly TorznabCategory ConsoleWii = new TorznabCategory(1030, "Console/Wii");
        public static readonly TorznabCategory ConsoleXBox = new TorznabCategory(1040, "Console/XBox");
        public static readonly TorznabCategory ConsoleXBox360 = new TorznabCategory(1050, "Console/XBox 360");
        public static readonly TorznabCategory ConsoleWiiware = new TorznabCategory(1060, "Console/Wiiware");
        public static readonly TorznabCategory ConsoleXBox360DLC = new TorznabCategory(1070, "Console/XBox 360 DLC");
        public static readonly TorznabCategory ConsolePS3 = new TorznabCategory(1080, "Console/PS3");
        public static readonly TorznabCategory ConsoleOther = new TorznabCategory(1090, "Console/Other");
        public static readonly TorznabCategory Console3DS = new TorznabCategory(1110, "Console/3DS");
        public static readonly TorznabCategory ConsolePSVita = new TorznabCategory(1120, "Console/PS Vita");
        public static readonly TorznabCategory ConsoleWiiU = new TorznabCategory(1130, "Console/WiiU");
        public static readonly TorznabCategory ConsoleXBoxOne = new TorznabCategory(1140, "Console/XBox One");
        public static readonly TorznabCategory ConsolePS4 = new TorznabCategory(1180, "Console/PS4");

        public static readonly TorznabCategory Movies = new TorznabCategory(2000, "Movies");
        public static readonly TorznabCategory MoviesForeign = new TorznabCategory(2010, "Movies/Foreign");
        public static readonly TorznabCategory MoviesOther = new TorznabCategory(2020, "Movies/Other");
        public static readonly TorznabCategory MoviesSD = new TorznabCategory(2030, "Movies/SD");
        public static readonly TorznabCategory MoviesHD = new TorznabCategory(2040, "Movies/HD");
        public static readonly TorznabCategory MoviesUHD = new TorznabCategory(2045, "Movies/UHD");
        public static readonly TorznabCategory MoviesBluRay = new TorznabCategory(2050, "Movies/BluRay");
        public static readonly TorznabCategory Movies3D = new TorznabCategory(2060, "Movies/3D");
        public static readonly TorznabCategory MoviesDVD = new TorznabCategory(2070, "Movies/DVD");
        public static readonly TorznabCategory MoviesWEBDL = new TorznabCategory(2080, "Movies/WEB-DL");

        public static readonly TorznabCategory Audio = new TorznabCategory(3000, "Audio");
        public static readonly TorznabCategory AudioMP3 = new TorznabCategory(3010, "Audio/MP3");
        public static readonly TorznabCategory AudioVideo = new TorznabCategory(3020, "Audio/Video");
        public static readonly TorznabCategory AudioAudiobook = new TorznabCategory(3030, "Audio/Audiobook");
        public static readonly TorznabCategory AudioLossless = new TorznabCategory(3040, "Audio/Lossless");
        public static readonly TorznabCategory AudioOther = new TorznabCategory(3050, "Audio/Other");
        public static readonly TorznabCategory AudioForeign = new TorznabCategory(3060, "Audio/Foreign");

        public static readonly TorznabCategory PC = new TorznabCategory(4000, "PC");
        public static readonly TorznabCategory PC0day = new TorznabCategory(4010, "PC/0day");
        public static readonly TorznabCategory PCISO = new TorznabCategory(4020, "PC/ISO");
        public static readonly TorznabCategory PCMac = new TorznabCategory(4030, "PC/Mac");
        public static readonly TorznabCategory PCMobileOther = new TorznabCategory(4040, "PC/Mobile-Other");
        public static readonly TorznabCategory PCGames = new TorznabCategory(4050, "PC/Games");
        public static readonly TorznabCategory PCMobileiOS = new TorznabCategory(4060, "PC/Mobile-iOS");
        public static readonly TorznabCategory PCMobileAndroid = new TorznabCategory(4070, "PC/Mobile-Android");

        public static readonly TorznabCategory TV = new TorznabCategory(5000, "TV");
        public static readonly TorznabCategory TVWEBDL = new TorznabCategory(5010, "TV/WEB-DL");
        public static readonly TorznabCategory TVForeign = new TorznabCategory(5020, "TV/Foreign");
        public static readonly TorznabCategory TVSD = new TorznabCategory(5030, "TV/SD");
        public static readonly TorznabCategory TVHD = new TorznabCategory(5040, "TV/HD");
        public static readonly TorznabCategory TVUHD = new TorznabCategory(5045, "TV/UHD");
        public static readonly TorznabCategory TVOther = new TorznabCategory(5050, "TV/Other");
        public static readonly TorznabCategory TVSport = new TorznabCategory(5060, "TV/Sport");
        public static readonly TorznabCategory TVAnime = new TorznabCategory(5070, "TV/Anime");
        public static readonly TorznabCategory TVDocumentary = new TorznabCategory(5080, "TV/Documentary");

        public static readonly TorznabCategory XXX = new TorznabCategory(6000, "XXX");
        public static readonly TorznabCategory XXXDVD = new TorznabCategory(6010, "XXX/DVD");
        public static readonly TorznabCategory XXXWMV = new TorznabCategory(6020, "XXX/WMV");
        public static readonly TorznabCategory XXXXviD = new TorznabCategory(6030, "XXX/XviD");
        public static readonly TorznabCategory XXXx264 = new TorznabCategory(6040, "XXX/x264");
        public static readonly TorznabCategory XXXUHD = new TorznabCategory(6045, "XXX/UHD");
        public static readonly TorznabCategory XXXPack = new TorznabCategory(6050, "XXX/Pack");
        public static readonly TorznabCategory XXXImageSet = new TorznabCategory(6060, "XXX/ImageSet");
        public static readonly TorznabCategory XXXOther = new TorznabCategory(6070, "XXX/Other");
        public static readonly TorznabCategory XXXSD = new TorznabCategory(6080, "XXX/SD");
        public static readonly TorznabCategory XXXWEBDL = new TorznabCategory(6090, "XXX/WEB-DL");

        public static readonly TorznabCategory Books = new TorznabCategory(7000, "Books");
        public static readonly TorznabCategory BooksMags = new TorznabCategory(7010, "Books/Mags");
        public static readonly TorznabCategory BooksEBook = new TorznabCategory(7020, "Books/EBook");
        public static readonly TorznabCategory BooksComics = new TorznabCategory(7030, "Books/Comics");
        public static readonly TorznabCategory BooksTechnical = new TorznabCategory(7040, "Books/Technical");
        public static readonly TorznabCategory BooksOther = new TorznabCategory(7050, "Books/Other");
        public static readonly TorznabCategory BooksForeign = new TorznabCategory(7060, "Books/Foreign");

        public static readonly TorznabCategory Other = new TorznabCategory(8000, "Other");
        public static readonly TorznabCategory OtherMisc = new TorznabCategory(8010, "Other/Misc");
        public static readonly TorznabCategory OtherHashed = new TorznabCategory(8020, "Other/Hashed");

        public static readonly TorznabCategory[] ParentCats =
        {
            Console,
            Movies,
            Audio,
            PC,
            TV,
            XXX,
            Books,
            Other
        };

        public static readonly TorznabCategory[] AllCats =
        {
            Console,
            ConsoleNDS,
            ConsolePSP,
            ConsoleWii,
            ConsoleXBox,
            ConsoleXBox360,
            ConsoleWiiware,
            ConsoleXBox360DLC,
            ConsolePS3,
            ConsoleOther,
            Console3DS,
            ConsolePSVita,
            ConsoleWiiU,
            ConsoleXBoxOne,
            ConsolePS4,
            Movies,
            MoviesForeign,
            MoviesOther,
            MoviesSD,
            MoviesHD,
            MoviesUHD,
            MoviesBluRay,
            Movies3D,
            MoviesDVD,
            MoviesWEBDL,
            Audio,
            AudioMP3,
            AudioVideo,
            AudioAudiobook,
            AudioLossless,
            AudioOther,
            AudioForeign,
            PC,
            PC0day,
            PCISO,
            PCMac,
            PCMobileOther,
            PCGames,
            PCMobileiOS,
            PCMobileAndroid,
            TV,
            TVWEBDL,
            TVForeign,
            TVSD,
            TVHD,
            TVUHD,
            TVOther,
            TVSport,
            TVAnime,
            TVDocumentary,
            XXX,
            XXXDVD,
            XXXWMV,
            XXXXviD,
            XXXx264,
            XXXUHD,
            XXXPack,
            XXXImageSet,
            XXXOther,
            XXXSD,
            XXXWEBDL,
            Books,
            BooksMags,
            BooksEBook,
            BooksComics,
            BooksTechnical,
            BooksOther,
            BooksForeign,
            Other,
            OtherMisc,
            OtherHashed
        };

        static TorznabCatType()
        {
            Console.SubCategories.AddRange(
                new List<TorznabCategory>
                {
                    ConsoleNDS,
                    ConsolePSP,
                    ConsoleWii,
                    ConsoleXBox,
                    ConsoleXBox360,
                    ConsoleWiiware,
                    ConsoleXBox360DLC,
                    ConsolePS3,
                    ConsoleOther,
                    Console3DS,
                    ConsolePSVita,
                    ConsoleWiiU,
                    ConsoleXBoxOne,
                    ConsolePS4
                });
            Movies.SubCategories.AddRange(
                new List<TorznabCategory>
                {
                    MoviesForeign,
                    MoviesOther,
                    MoviesSD,
                    MoviesHD,
                    MoviesUHD,
                    MoviesBluRay,
                    Movies3D,
                    MoviesDVD,
                    MoviesWEBDL
                });
            Audio.SubCategories.AddRange(
                new List<TorznabCategory>
                {
                    AudioMP3,
                    AudioVideo,
                    AudioAudiobook,
                    AudioLossless,
                    AudioOther,
                    AudioForeign
                });
            PC.SubCategories.AddRange(
                new List<TorznabCategory>
                {
                    PC0day,
                    PCISO,
                    PCMac,
                    PCMobileOther,
                    PCGames,
                    PCMobileiOS,
                    PCMobileAndroid
                });
            TV.SubCategories.AddRange(
                new List<TorznabCategory>
                {
                    TVWEBDL,
                    TVForeign,
                    TVSD,
                    TVHD,
                    TVUHD,
                    TVOther,
                    TVSport,
                    TVAnime,
                    TVDocumentary
                });
            XXX.SubCategories.AddRange(
                new List<TorznabCategory>
                {
                    XXXDVD,
                    XXXWMV,
                    XXXXviD,
                    XXXx264,
                    XXXUHD,
                    XXXPack,
                    XXXImageSet,
                    XXXOther,
                    XXXSD,
                    XXXWEBDL
                });
            Books.SubCategories.AddRange(
                new List<TorznabCategory>
                {
                    BooksMags,
                    BooksEBook,
                    BooksComics,
                    BooksTechnical,
                    BooksOther,
                    BooksForeign
                });
            Other.SubCategories.AddRange(new List<TorznabCategory> { OtherMisc, OtherHashed });
        }

        public static string GetCatDesc(int torznabCatId) =>
            AllCats.FirstOrDefault(c => c.ID == torznabCatId)?.Name ?? string.Empty;

        public static TorznabCategory GetCatByName(string name) => AllCats.FirstOrDefault(c => c.Name == name);
    }
}
