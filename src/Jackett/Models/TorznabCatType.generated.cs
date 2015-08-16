using System.Collections.Generic;

namespace Jackett.Models
{

	public static partial class TorznabCatType
	{

		public static readonly TorznabCategory Console = new TorznabCategory(1000, "Console");
				
		public static readonly TorznabCategory ConsoleNDS = new TorznabCategory(1010, "Console/NDS");
				
		public static readonly TorznabCategory ConsolePSP = new TorznabCategory(1020, "Console/PSP");
				
		public static readonly TorznabCategory ConsoleWii = new TorznabCategory(1030, "Console/Wii");
				
		public static readonly TorznabCategory ConsoleXbox = new TorznabCategory(1040, "Console/Xbox");
				
		public static readonly TorznabCategory ConsoleXbox360 = new TorznabCategory(1050, "Console/Xbox 360");
				
		public static readonly TorznabCategory ConsoleWiiwareVC = new TorznabCategory(1060, "Console/Wiiware/VC");
				
		public static readonly TorznabCategory ConsoleXBOX360DLC = new TorznabCategory(1070, "Console/XBOX 360 DLC");
				
		public static readonly TorznabCategory ConsolePS3 = new TorznabCategory(1080, "Console/PS3");
				
		public static readonly TorznabCategory ConsoleOther = new TorznabCategory(1090, "Console/Other");
				
		public static readonly TorznabCategory Console3DS = new TorznabCategory(1110, "Console/3DS");
				
		public static readonly TorznabCategory ConsolePSVita = new TorznabCategory(1120, "Console/PS Vita");
				
		public static readonly TorznabCategory ConsoleWiiU = new TorznabCategory(1130, "Console/WiiU");
				
		public static readonly TorznabCategory ConsoleXboxOne = new TorznabCategory(1140, "Console/Xbox One");
				
		public static readonly TorznabCategory ConsolePS4 = new TorznabCategory(1180, "Console/PS4");
				
		public static readonly TorznabCategory Movies = new TorznabCategory(2000, "Movies");
				
		public static readonly TorznabCategory MoviesForeign = new TorznabCategory(2010, "Movies/Foreign");
				
		public static readonly TorznabCategory MoviesOther = new TorznabCategory(2020, "Movies/Other");
				
		public static readonly TorznabCategory MoviesSD = new TorznabCategory(2030, "Movies/SD");
				
		public static readonly TorznabCategory MoviesHD = new TorznabCategory(2040, "Movies/HD");
				
		public static readonly TorznabCategory Movies3D = new TorznabCategory(2050, "Movies/3D");
				
		public static readonly TorznabCategory MoviesBluRay = new TorznabCategory(2060, "Movies/BluRay");
				
		public static readonly TorznabCategory MoviesDVD = new TorznabCategory(2070, "Movies/DVD");
				
		public static readonly TorznabCategory MoviesWEBDL = new TorznabCategory(2080, "Movies/WEBDL");
				
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
				
		public static readonly TorznabCategory PCPhoneOther = new TorznabCategory(4040, "PC/Phone-Other");
				
		public static readonly TorznabCategory PCGames = new TorznabCategory(4050, "PC/Games");
				
		public static readonly TorznabCategory PCPhoneIOS = new TorznabCategory(4060, "PC/Phone-IOS");
				
		public static readonly TorznabCategory PCPhoneAndroid = new TorznabCategory(4070, "PC/Phone-Android");
				
		public static readonly TorznabCategory TV = new TorznabCategory(5000, "TV");
				
		public static readonly TorznabCategory TVWEBDL = new TorznabCategory(5010, "TV/WEB-DL");
				
		public static readonly TorznabCategory TVFOREIGN = new TorznabCategory(5020, "TV/FOREIGN");
				
		public static readonly TorznabCategory TVSD = new TorznabCategory(5030, "TV/SD");
				
		public static readonly TorznabCategory TVHD = new TorznabCategory(5040, "TV/HD");
				
		public static readonly TorznabCategory TVOTHER = new TorznabCategory(5050, "TV/OTHER");
				
		public static readonly TorznabCategory TVSport = new TorznabCategory(5060, "TV/Sport");
				
		public static readonly TorznabCategory TVAnime = new TorznabCategory(5070, "TV/Anime");
				
		public static readonly TorznabCategory TVDocumentary = new TorznabCategory(5080, "TV/Documentary");
				
		public static readonly TorznabCategory XXX = new TorznabCategory(6000, "XXX");
				
		public static readonly TorznabCategory XXXDVD = new TorznabCategory(6010, "XXX/DVD");
				
		public static readonly TorznabCategory XXXWMV = new TorznabCategory(6020, "XXX/WMV");
				
		public static readonly TorznabCategory XXXXviD = new TorznabCategory(6030, "XXX/XviD");
				
		public static readonly TorznabCategory XXXx264 = new TorznabCategory(6040, "XXX/x264");
				
		public static readonly TorznabCategory XXXOther = new TorznabCategory(6050, "XXX/Other");
				
		public static readonly TorznabCategory XXXImageset = new TorznabCategory(6060, "XXX/Imageset");
				
		public static readonly TorznabCategory XXXPacks = new TorznabCategory(6070, "XXX/Packs");
				
		public static readonly TorznabCategory Other = new TorznabCategory(7000, "Other");
				
		public static readonly TorznabCategory OtherMisc = new TorznabCategory(7010, "Other/Misc");
				
		public static readonly TorznabCategory OtherHashed = new TorznabCategory(7020, "Other/Hashed");
				
		public static readonly TorznabCategory Books = new TorznabCategory(8000, "Books");
				
		public static readonly TorznabCategory BooksEbook = new TorznabCategory(8010, "Books/Ebook");
				
		public static readonly TorznabCategory BooksComics = new TorznabCategory(8020, "Books/Comics");
				
		public static readonly TorznabCategory BooksMagazines = new TorznabCategory(8030, "Books/Magazines");
				
		public static readonly TorznabCategory BooksTechnical = new TorznabCategory(8040, "Books/Technical");
				
		public static readonly TorznabCategory BooksOther = new TorznabCategory(8050, "Books/Other");
				
		public static readonly TorznabCategory BooksForeign = new TorznabCategory(8060, "Books/Foreign");
				 

		public static readonly TorznabCategory[] AllCats = new TorznabCategory[] { Console, ConsoleNDS, ConsolePSP, ConsoleWii, ConsoleXbox, ConsoleXbox360, ConsoleWiiwareVC, ConsoleXBOX360DLC, ConsolePS3, ConsoleOther, Console3DS, ConsolePSVita, ConsoleWiiU, ConsoleXboxOne, ConsolePS4, Movies, MoviesForeign, MoviesOther, MoviesSD, MoviesHD, Movies3D, MoviesBluRay, MoviesDVD, MoviesWEBDL, Audio, AudioMP3, AudioVideo, AudioAudiobook, AudioLossless, AudioOther, AudioForeign, PC, PC0day, PCISO, PCMac, PCPhoneOther, PCGames, PCPhoneIOS, PCPhoneAndroid, TV, TVWEBDL, TVFOREIGN, TVSD, TVHD, TVOTHER, TVSport, TVAnime, TVDocumentary, XXX, XXXDVD, XXXWMV, XXXXviD, XXXx264, XXXOther, XXXImageset, XXXPacks, Other, OtherMisc, OtherHashed, Books, BooksEbook, BooksComics, BooksMagazines, BooksTechnical, BooksOther, BooksForeign };

		static TorznabCatType()
		{
				 
			Console.SubCategories.AddRange(new List<TorznabCategory> { ConsoleNDS, ConsolePSP, ConsoleWii, ConsoleXbox, ConsoleXbox360, ConsoleWiiwareVC, ConsoleXBOX360DLC, ConsolePS3, ConsoleOther, Console3DS, ConsolePSVita, ConsoleWiiU, ConsoleXboxOne, ConsolePS4 });
				 
			Movies.SubCategories.AddRange(new List<TorznabCategory> { MoviesForeign, MoviesOther, MoviesSD, MoviesHD, Movies3D, MoviesBluRay, MoviesDVD, MoviesWEBDL });
				 
			Audio.SubCategories.AddRange(new List<TorznabCategory> { AudioMP3, AudioVideo, AudioAudiobook, AudioLossless, AudioOther, AudioForeign });
				 
			PC.SubCategories.AddRange(new List<TorznabCategory> { PC0day, PCISO, PCMac, PCPhoneOther, PCGames, PCPhoneIOS, PCPhoneAndroid });
				 
			TV.SubCategories.AddRange(new List<TorznabCategory> { TVWEBDL, TVFOREIGN, TVSD, TVHD, TVOTHER, TVSport, TVAnime, TVDocumentary });
				 
			XXX.SubCategories.AddRange(new List<TorznabCategory> { XXXDVD, XXXWMV, XXXXviD, XXXx264, XXXOther, XXXImageset, XXXPacks });
				 
			Other.SubCategories.AddRange(new List<TorznabCategory> { OtherMisc, OtherHashed });
				 
			Books.SubCategories.AddRange(new List<TorznabCategory> { BooksEbook, BooksComics, BooksMagazines, BooksTechnical, BooksOther, BooksForeign });
				 
		}
	}
}

