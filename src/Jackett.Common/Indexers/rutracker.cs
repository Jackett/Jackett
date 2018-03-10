using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Parser.Html;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    public class RuTracker : BaseWebIndexer
    {
        private string LoginUrl
        { get { return SiteLink + "forum/login.php"; } }
        private string SearchUrl
        { get { return SiteLink + "forum/tracker.php"; } }

        protected string cap_sid = null;
        protected string cap_code_field = null;

        private new ConfigurationDataRutracker configData
        {
            get { return (ConfigurationDataRutracker)base.configData; }
            set { base.configData = value; }
        }

        public RuTracker(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(name: "RuTracker",
                   description: "RuTracker is a Semi-Private Russian torrent site with a thriving file-sharing community",
                   link: "https://rutracker.org/",
                   caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   configData: new ConfigurationDataRutracker())
        {
            Encoding = Encoding.GetEncoding("windows-1251");
            Language = "ru-ru";
            Type = "semi-private";

            // Новости
            AddCategoryMapping(2317, TorznabCatType.Other, "NEW YEAR'S SECTION");
            AddCategoryMapping(1241, TorznabCatType.Other, " | - New competitions");
            AddCategoryMapping(2338, TorznabCatType.TV, " | - Entertainment shows and Documentaries");
            AddCategoryMapping(1464, TorznabCatType.Movies, " | - Movies and Cartoons");
            AddCategoryMapping(860, TorznabCatType.Books, " | - Books, Journals, Notes");
            AddCategoryMapping(1340, TorznabCatType.Audio, " | - Music");
            AddCategoryMapping(1346, TorznabCatType.AudioVideo, " | - Music video");
            AddCategoryMapping(1239, TorznabCatType.Console, " | - Games");
            AddCategoryMapping(1299, TorznabCatType.Other, " | - Miscellaneous (postcards, wallpapers, video, etc.).");
            AddCategoryMapping(1289, TorznabCatType.Other, "Rutracker Awards (events and competitions)");
            AddCategoryMapping(1579, TorznabCatType.Other, "| - Photo club. The whole world is in the palm of your hand.");
            AddCategoryMapping(2214, TorznabCatType.Other, " | - Rutracker Awards (distribution)");

            // Кино, Видео и ТВ
            AddCategoryMapping(7, TorznabCatType.MoviesForeign, "Foreign movies");
            AddCategoryMapping(187, TorznabCatType.MoviesForeign, " | - Classic of world cinema");
            AddCategoryMapping(2090, TorznabCatType.MoviesForeign, " | - Movies before 1990");
            AddCategoryMapping(2221, TorznabCatType.MoviesForeign, " | - Movies 1991-2000");
            AddCategoryMapping(2091, TorznabCatType.MoviesForeign, " | - Movies 2001-2005");
            AddCategoryMapping(2092, TorznabCatType.MoviesForeign, " | - Movies 2006-2010");
            AddCategoryMapping(2093, TorznabCatType.MoviesForeign, " | - Movies 2011-2015");
            AddCategoryMapping(2200, TorznabCatType.MoviesForeign, " | - Movies 2016");
            AddCategoryMapping(934, TorznabCatType.MoviesForeign, " | - Asian movies");
            AddCategoryMapping(505, TorznabCatType.MoviesForeign, " | - Indian Cinema");
            AddCategoryMapping(212, TorznabCatType.MoviesForeign, " | - Movie Collections");
            AddCategoryMapping(2459, TorznabCatType.MoviesForeign, " | - Shorts");
            AddCategoryMapping(1235, TorznabCatType.MoviesForeign, " | - Grindhouse");
            AddCategoryMapping(185, TorznabCatType.MoviesForeign, " | - Soundtracks and Translations");
            AddCategoryMapping(22, TorznabCatType.Movies, "our film");
            AddCategoryMapping(941, TorznabCatType.Movies, " | - Cinema of the USSR");
            AddCategoryMapping(1666, TorznabCatType.Movies, " | - Children's domestic films");
            AddCategoryMapping(376, TorznabCatType.Movies, " | - Author Debuts");
            AddCategoryMapping(124, TorznabCatType.Movies, "Art-house cinema and author");
            AddCategoryMapping(1543, TorznabCatType.Movies, " | - Shorts (Art-house cinema and author)");
            AddCategoryMapping(709, TorznabCatType.Movies, " | - Documentaries (Art-house cinema and author)");
            AddCategoryMapping(1577, TorznabCatType.Movies, " | - Animation (Art-house cinema and author)");
            AddCategoryMapping(511, TorznabCatType.Movies, "Theater");
            AddCategoryMapping(656, TorznabCatType.Movies, "| - Benefit. Master of Arts domestic theater and cinema");
            AddCategoryMapping(93, TorznabCatType.Movies, "DVD Video");
            AddCategoryMapping(905, TorznabCatType.Movies, " | - Classic of world cinema (DVD Video)");
            AddCategoryMapping(1576, TorznabCatType.Movies, " | - Asian movies (DVD Video)");
            AddCategoryMapping(101, TorznabCatType.Movies, " | - Foreign movies (DVD)");
            AddCategoryMapping(100, TorznabCatType.Movies, " | - Our cinema (DVD)");
            AddCategoryMapping(572, TorznabCatType.Movies, " | - Art-house and auteur cinema (DVD)");
            AddCategoryMapping(2220, TorznabCatType.Movies, " | - Indian Cinema DVD and HD Video");
            AddCategoryMapping(1670, TorznabCatType.Movies, " |- Грайндхаус DVD и HD Video");
            AddCategoryMapping(2198, TorznabCatType.MoviesHD, "HD Video");
            AddCategoryMapping(2199, TorznabCatType.MoviesHD, " | - Classic of world cinema (HD Video)");
            AddCategoryMapping(313, TorznabCatType.MoviesHD, " | - Foreign movies (HD Video)");
            AddCategoryMapping(2201, TorznabCatType.MoviesHD, " | - Asian movies (HD Video)");
            AddCategoryMapping(312, TorznabCatType.MoviesHD, " | - Our cinema (HD Video)");
            AddCategoryMapping(2339, TorznabCatType.MoviesHD, " | - Art-house and auteur cinema (HD Video)");
            AddCategoryMapping(352, TorznabCatType.Movies3D, "3D / Stereo Film, Video, TV & Sports");
            AddCategoryMapping(549, TorznabCatType.Movies3D, " | - 3D Movies");
            AddCategoryMapping(1213, TorznabCatType.Movies3D, " | - 3D Animation");
            AddCategoryMapping(2109, TorznabCatType.Movies3D, " | - 3D Documentary");
            AddCategoryMapping(514, TorznabCatType.Movies3D, " | - 3D Спорт");
            AddCategoryMapping(2097, TorznabCatType.Movies3D, " | - 3D Clips, Music Videos, Movie Trailers");
            AddCategoryMapping(4, TorznabCatType.Movies, "Cartoons");
            AddCategoryMapping(2343, TorznabCatType.Movies, " | - Animation (Announcements HD Video)");
            AddCategoryMapping(930, TorznabCatType.Movies, " | - Animation (HD Video)");
            AddCategoryMapping(2365, TorznabCatType.Movies, " | - Short Film (HD Video)");
            AddCategoryMapping(1900, TorznabCatType.Movies, " | - Domestic cartoons (DVD)");
            AddCategoryMapping(521, TorznabCatType.Movies, " | - Foreign cartoons (DVD)");
            AddCategoryMapping(2258, TorznabCatType.Movies, " | - Foreign Short Film (DVD)");
            AddCategoryMapping(208, TorznabCatType.Movies, " | - Domestic cartoons");
            AddCategoryMapping(539, TorznabCatType.Movies, " | - Domestic full-length cartoons");
            AddCategoryMapping(209, TorznabCatType.Movies, " | - Foreign cartoons");
            AddCategoryMapping(484, TorznabCatType.Movies, " | - Foreign short cartoons");
            AddCategoryMapping(822, TorznabCatType.Movies, " | - Cartoon Collection");
            AddCategoryMapping(921, TorznabCatType.TV, "Serial cartoons");
            AddCategoryMapping(922, TorznabCatType.TV, " | - Avatar");
            AddCategoryMapping(1247, TorznabCatType.TV, " | - Griffiny / Family guy");
            AddCategoryMapping(923, TorznabCatType.TV, " | - SpongeBob SquarePants");
            AddCategoryMapping(924, TorznabCatType.TV, " | - The Simpsons");
            AddCategoryMapping(1991, TorznabCatType.TV, " | - Skubi-du / Scooby-Doo");
            AddCategoryMapping(925, TorznabCatType.TV, " | - Tom and Jerry");
            AddCategoryMapping(1165, TorznabCatType.TV, " | - Transformers");
            AddCategoryMapping(1245, TorznabCatType.TV, " | - DuckTales / DuckTales");
            AddCategoryMapping(928, TorznabCatType.TV, " | - Futurama / Futurama");
            AddCategoryMapping(926, TorznabCatType.TV, " | - Spider-Man / The Spectacular Spider-Man");
            AddCategoryMapping(1246, TorznabCatType.TV, " | - Turtles Mutant Ninja / Teenage Mutant Ninja Turtles");
            AddCategoryMapping(1250, TorznabCatType.TV, " |- Чип и Дейл / Chip And Dale");
            AddCategoryMapping(927, TorznabCatType.TV, " | - South Park / South Park");
            AddCategoryMapping(1248, TorznabCatType.TV, " | - For sub-standard hands");
            AddCategoryMapping(33, TorznabCatType.TVAnime, "Anime");
            AddCategoryMapping(281, TorznabCatType.TVAnime, " | - Manga");
            AddCategoryMapping(1386, TorznabCatType.TVAnime, " | - Wallpapers, artbook, and others.");
            AddCategoryMapping(1387, TorznabCatType.TVAnime, " | -. AMV and other rollers");
            AddCategoryMapping(1388, TorznabCatType.TVAnime, " |- OST (lossless)");
            AddCategoryMapping(282, TorznabCatType.TVAnime, " | - OST (mp3 and others lossy-format)");
            AddCategoryMapping(599, TorznabCatType.TVAnime, " | - Anime (DVD)");
            AddCategoryMapping(1105, TorznabCatType.TVAnime, " |- Аниме (HD Video)");
            AddCategoryMapping(1389, TorznabCatType.TVAnime, " | - Anime (main subsection)");
            AddCategoryMapping(1391, TorznabCatType.TVAnime, " | - Anime (pleerny subsection)");
            AddCategoryMapping(2491, TorznabCatType.TVAnime, " | - Anime (QC subsection)");
            AddCategoryMapping(404, TorznabCatType.TVAnime, " | - Pokemony");
            AddCategoryMapping(1390, TorznabCatType.TVAnime, " | - Naruto");
            AddCategoryMapping(1642, TorznabCatType.TVAnime, " | - Trade");
            AddCategoryMapping(893, TorznabCatType.TVAnime, " | - Japanese cartoons");
            AddCategoryMapping(1478, TorznabCatType.TVAnime, " | - For sub-standard hands");

            // Документалистика и юмор
            AddCategoryMapping(670, TorznabCatType.TVDocumentary, "Faith and Religion");
            AddCategoryMapping(1475, TorznabCatType.TVDocumentary, " | - Christianity");
            AddCategoryMapping(2107, TorznabCatType.TVDocumentary, " | - Islam");
            AddCategoryMapping(294, TorznabCatType.TVDocumentary, " | - Religions of India, Tibet and East Asia");
            AddCategoryMapping(1453, TorznabCatType.TVDocumentary, " | - Cults and new religious movements");
            AddCategoryMapping(46, TorznabCatType.TVDocumentary, "Documentary movies and TV shows");
            AddCategoryMapping(103, TorznabCatType.TVDocumentary, " | - Documentary (DVD)");
            AddCategoryMapping(671, TorznabCatType.TVDocumentary, "| - Biographies. Personality and idols");
            AddCategoryMapping(2177, TorznabCatType.TVDocumentary, " | - Cinema and animation");
            AddCategoryMapping(2538, TorznabCatType.TVDocumentary, " | - Art, Art History");
            AddCategoryMapping(2159, TorznabCatType.TVDocumentary, " | - Music");
            AddCategoryMapping(251, TorznabCatType.TVDocumentary, " | - Kriminalynaya documentary");
            AddCategoryMapping(98, TorznabCatType.TVDocumentary, " | - Secrets of the Ages / Special Services / Conspiracy Theory");
            AddCategoryMapping(97, TorznabCatType.TVDocumentary, " | - Military");
            AddCategoryMapping(851, TorznabCatType.TVDocumentary, " | - World War II");
            AddCategoryMapping(2178, TorznabCatType.TVDocumentary, " | - Accidents / Accidents / Disasters");
            AddCategoryMapping(821, TorznabCatType.TVDocumentary, " | - Aviation");
            AddCategoryMapping(2076, TorznabCatType.TVDocumentary, " | - Space");
            AddCategoryMapping(56, TorznabCatType.TVDocumentary, " | - Scientific-popular movies");
            AddCategoryMapping(2123, TorznabCatType.TVDocumentary, " | - Flora and fauna");
            AddCategoryMapping(876, TorznabCatType.TVDocumentary, " | - Travel and Tourism");
            AddCategoryMapping(2380, TorznabCatType.TVDocumentary, " | - Social talk show");
            AddCategoryMapping(1467, TorznabCatType.TVDocumentary, " | - Information-analytical and socio-political etc. ..");
            AddCategoryMapping(1469, TorznabCatType.TVDocumentary, " | - Architecture and Construction");
            AddCategoryMapping(672, TorznabCatType.TVDocumentary, " | - All about home, life and design");
            AddCategoryMapping(249, TorznabCatType.TVDocumentary, " |- BBC");
            AddCategoryMapping(552, TorznabCatType.TVDocumentary, " |- Discovery");
            AddCategoryMapping(500, TorznabCatType.TVDocumentary, " |- National Geographic");
            AddCategoryMapping(2112, TorznabCatType.TVDocumentary, " | - History: Ancient World / Antiquity / Middle Ages");
            AddCategoryMapping(1327, TorznabCatType.TVDocumentary, " | - History: modern and contemporary times");
            AddCategoryMapping(1468, TorznabCatType.TVDocumentary, " | - The Age of the USSR");
            AddCategoryMapping(1280, TorznabCatType.TVDocumentary, " | - The Battle of psychics / Theory improbability / Seekers / G ..");
            AddCategoryMapping(752, TorznabCatType.TVDocumentary, " | - Russian sensation / Program Maximum / Profession report ..");
            AddCategoryMapping(1114, TorznabCatType.TVDocumentary, " | - Paranormal");
            AddCategoryMapping(2168, TorznabCatType.TVDocumentary, " | - Alternative history and science");
            AddCategoryMapping(2160, TorznabCatType.TVDocumentary, " | - Vnezhanrovaya documentary");
            AddCategoryMapping(2176, TorznabCatType.TVDocumentary, " | - Other / nekonditsiya");
            AddCategoryMapping(314, TorznabCatType.TVDocumentary, "Documentary (HD Video)");
            AddCategoryMapping(2323, TorznabCatType.TVDocumentary, " | - Information-analytical and socio-political etc. ..");
            AddCategoryMapping(1278, TorznabCatType.TVDocumentary, "| - Biographies. Personality and idols (HD Video)");
            AddCategoryMapping(1281, TorznabCatType.TVDocumentary, " | - Military Science (HD Video)");
            AddCategoryMapping(2110, TorznabCatType.TVDocumentary, " | - Natural History, Science and Technology (HD Video)");
            AddCategoryMapping(979, TorznabCatType.TVDocumentary, " | - Travel and Tourism (HD Video)");
            AddCategoryMapping(2169, TorznabCatType.TVDocumentary, " |- Флора и фауна (HD Video)");
            AddCategoryMapping(2166, TorznabCatType.TVDocumentary, " | - History (HD Video)");
            AddCategoryMapping(2164, TorznabCatType.TVDocumentary, " |- BBC, Discovery, National Geographic (HD Video)");
            AddCategoryMapping(2163, TorznabCatType.TVDocumentary, " | - Kriminalynaya documentary (HD Video)");
            AddCategoryMapping(24, TorznabCatType.TV, "Entertaining TV programs and shows, fun and humor");
            AddCategoryMapping(1959, TorznabCatType.TV, " | - Mind games and quizzes");
            AddCategoryMapping(939, TorznabCatType.TV, " | - Reality and talk show host / category / impressions");
            AddCategoryMapping(1481, TorznabCatType.TV, " | - Children's TV Show");
            AddCategoryMapping(113, TorznabCatType.TV, " | - KVN");
            AddCategoryMapping(115, TorznabCatType.TV, " | - Post KVN");
            AddCategoryMapping(882, TorznabCatType.TV, " | - Distorting Mirror / town / in the town");
            AddCategoryMapping(1482, TorznabCatType.TV, " | - Ice show");
            AddCategoryMapping(393, TorznabCatType.TV, " | - Musical Show");
            AddCategoryMapping(1569, TorznabCatType.TV, " | - Dinner Party");
            AddCategoryMapping(373, TorznabCatType.TV, " | - Good Jokes");
            AddCategoryMapping(1186, TorznabCatType.TV, " | - Evening Quarter");
            AddCategoryMapping(137, TorznabCatType.TV, " | - Movies with a funny transfer (parody)");
            AddCategoryMapping(2537, TorznabCatType.TV, " |- Stand-up comedy");
            AddCategoryMapping(532, TorznabCatType.TV, " | - Ukrainian Shows");
            AddCategoryMapping(827, TorznabCatType.TV, " | - Dance shows, concerts, performances");
            AddCategoryMapping(1484, TorznabCatType.TV, " | - The Circus");
            AddCategoryMapping(1485, TorznabCatType.TV, " | - The School for Scandal");
            AddCategoryMapping(114, TorznabCatType.TV, " | - Satirist, and humorists");
            AddCategoryMapping(1332, TorznabCatType.TV, " | - Humorous Audio Transmissions");
            AddCategoryMapping(1495, TorznabCatType.TV, " | - Audio and video clips (Jokes and humor)");

            // Спорт
            AddCategoryMapping(255, TorznabCatType.TVSport, "Sports tournaments, films and programs");
            AddCategoryMapping(256, TorznabCatType.TVSport, " | - Motorsports");
            AddCategoryMapping(1986, TorznabCatType.TVSport, " | - Motorsports");
            AddCategoryMapping(660, TorznabCatType.TVSport, " | - Formula-1 2016");
            AddCategoryMapping(1551, TorznabCatType.TVSport, " | - Formula-1 2012-2015");
            AddCategoryMapping(626, TorznabCatType.TVSport, " | - Formula 1");
            AddCategoryMapping(262, TorznabCatType.TVSport, " | - Cycling");
            AddCategoryMapping(1326, TorznabCatType.TVSport, " | - Volleyball / Handball");
            AddCategoryMapping(978, TorznabCatType.TVSport, " | - Billiards");
            AddCategoryMapping(1287, TorznabCatType.TVSport, " | - Poker");
            AddCategoryMapping(1188, TorznabCatType.TVSport, " | - Bodybuilding / Power Sports");
            AddCategoryMapping(1667, TorznabCatType.TVSport, " | - Boxing");
            AddCategoryMapping(1675, TorznabCatType.TVSport, " | - Classical arts");
            AddCategoryMapping(257, TorznabCatType.TVSport, " | - MMA and K-1");
            AddCategoryMapping(875, TorznabCatType.TVSport, " | - College Football");
            AddCategoryMapping(263, TorznabCatType.TVSport, " | - Rugby");
            AddCategoryMapping(2073, TorznabCatType.TVSport, " | - Baseball");
            AddCategoryMapping(550, TorznabCatType.TVSport, " | - Tennis");
            AddCategoryMapping(2124, TorznabCatType.TVSport, " | - Badminton / Table Tennis");
            AddCategoryMapping(1470, TorznabCatType.TVSport, " | - Gymnastics / Dance Competitions");
            AddCategoryMapping(528, TorznabCatType.TVSport, " | - Athletics / Water Sports");
            AddCategoryMapping(486, TorznabCatType.TVSport, " | - Winter Sports");
            AddCategoryMapping(854, TorznabCatType.TVSport, " | - Figure skating");
            AddCategoryMapping(2079, TorznabCatType.TVSport, " | - Biathlon");
            AddCategoryMapping(260, TorznabCatType.TVSport, " | - Extreme");
            AddCategoryMapping(1319, TorznabCatType.TVSport, " | - Sports (video)");
            AddCategoryMapping(1608, TorznabCatType.TVSport, "football");
            AddCategoryMapping(1952, TorznabCatType.TVSport, " | - Russia 2016-2017");
            AddCategoryMapping(2075, TorznabCatType.TVSport, " | - Russia 2015-2016");
            AddCategoryMapping(1613, TorznabCatType.TVSport, " | - Russia / USSR");
            AddCategoryMapping(1614, TorznabCatType.TVSport, " | - England");
            AddCategoryMapping(1623, TorznabCatType.TVSport, " | - Spain");
            AddCategoryMapping(1615, TorznabCatType.TVSport, " | - Italy");
            AddCategoryMapping(1630, TorznabCatType.TVSport, " | - Germany");
            AddCategoryMapping(2425, TorznabCatType.TVSport, " | - France");
            AddCategoryMapping(2514, TorznabCatType.TVSport, " | - Ukraine");
            AddCategoryMapping(1616, TorznabCatType.TVSport, " | - Other national championships and cups");
            AddCategoryMapping(2014, TorznabCatType.TVSport, " | - International Events");
            AddCategoryMapping(2171, TorznabCatType.TVSport, " | - European Cups 2016-2017");
            AddCategoryMapping(1491, TorznabCatType.TVSport, " | - European Cups 2015-2016");
            AddCategoryMapping(1987, TorznabCatType.TVSport, " | - European Cups 2011-2015");
            AddCategoryMapping(1617, TorznabCatType.TVSport, " | - European Cups");
            AddCategoryMapping(1610, TorznabCatType.TVSport, " | - European Football Championship 2016");
            AddCategoryMapping(1620, TorznabCatType.TVSport, " | - European Championships");
            AddCategoryMapping(1668, TorznabCatType.TVSport, " | - World Cup 2018");
            AddCategoryMapping(1621, TorznabCatType.TVSport, " | - World Championships");
            AddCategoryMapping(1998, TorznabCatType.TVSport, " | - Friendly tournaments and matches");
            AddCategoryMapping(1343, TorznabCatType.TVSport, " | - The survey and analytical programs 2014-2017");
            AddCategoryMapping(751, TorznabCatType.TVSport, " | - The survey and analytical programs");
            AddCategoryMapping(1697, TorznabCatType.TVSport, " | - Mini football / Football");
            AddCategoryMapping(2004, TorznabCatType.TVSport, "basketball");
            AddCategoryMapping(2001, TorznabCatType.TVSport, " | - International Competitions");
            AddCategoryMapping(2002, TorznabCatType.TVSport, " |- NBA / NCAA (до 2000 г.)");
            AddCategoryMapping(283, TorznabCatType.TVSport, " | - NBA / NCAA (2000-2010 biennium).");
            AddCategoryMapping(1997, TorznabCatType.TVSport, " | - NBA / NCAA (2010-2017 biennium).");
            AddCategoryMapping(2003, TorznabCatType.TVSport, " | - European club basketball");
            AddCategoryMapping(2009, TorznabCatType.TVSport, "Hockey");
            AddCategoryMapping(2010, TorznabCatType.TVSport, " | - Hockey / Bandy");
            AddCategoryMapping(2006, TorznabCatType.TVSport, " | - International Events");
            AddCategoryMapping(2007, TorznabCatType.TVSport, " | - KHL");
            AddCategoryMapping(2005, TorznabCatType.TVSport, " | - NHL (until 2011/12)");
            AddCategoryMapping(259, TorznabCatType.TVSport, " | - NHL (2013)");
            AddCategoryMapping(2008, TorznabCatType.TVSport, " | - USSR - Canada");
            AddCategoryMapping(126, TorznabCatType.TVSport, " | - Documentaries and Analysis");
            AddCategoryMapping(845, TorznabCatType.TVSport, "Wrestling");
            AddCategoryMapping(343, TorznabCatType.TVSport, " |- Professional Wrestling");
            AddCategoryMapping(2111, TorznabCatType.TVSport, " |- Independent Wrestling");
            AddCategoryMapping(1527, TorznabCatType.TVSport, " |- International Wrestling");
            AddCategoryMapping(2069, TorznabCatType.TVSport, " |- Oldschool Wrestling");
            AddCategoryMapping(1323, TorznabCatType.TVSport, " |- Documentary Wrestling");

            // Сериалы
            AddCategoryMapping(9, TorznabCatType.TV, "Russion serials");
            AddCategoryMapping(104, TorznabCatType.TV, " | - Secrets of the investigation");
            AddCategoryMapping(1408, TorznabCatType.TV, " | - National Security Agent");
            AddCategoryMapping(1535, TorznabCatType.TV, " | - Lawyer");
            AddCategoryMapping(91, TorznabCatType.TV, " | - Gangster Petersburg");
            AddCategoryMapping(1356, TorznabCatType.TV, " | - Return of Mukhtar");
            AddCategoryMapping(990, TorznabCatType.TV, " | - Hounds");
            AddCategoryMapping(856, TorznabCatType.TV, " | - Capercaillie / Pyatnitskii / Karpov");
            AddCategoryMapping(188, TorznabCatType.TV, " | - Darya Dontsova");
            AddCategoryMapping(310, TorznabCatType.TV, " | - Kadetstvo / Kremlëvskie kursanty");
            AddCategoryMapping(202, TorznabCatType.TV, " | - Kamenskaya");
            AddCategoryMapping(935, TorznabCatType.TV, " | - Code of Honor");
            AddCategoryMapping(172, TorznabCatType.TV, " | - A cop-in-law");
            AddCategoryMapping(805, TorznabCatType.TV, " | - Cop War");
            AddCategoryMapping(80, TorznabCatType.TV, " | - My Fair Nanny");
            AddCategoryMapping(119, TorznabCatType.TV, " | - Careful, Modern!");
            AddCategoryMapping(812, TorznabCatType.TV, " | - Web");
            AddCategoryMapping(175, TorznabCatType.TV, " | - After");
            AddCategoryMapping(79, TorznabCatType.TV, " | - Soldiers and others.");
            AddCategoryMapping(123, TorznabCatType.TV, " | - Stopping Power / Cops / Opera");
            AddCategoryMapping(189, TorznabCatType.TV, "Foreign TV series");
            AddCategoryMapping(842, TorznabCatType.TV, " | - News and TV shows in the display stage");
            AddCategoryMapping(235, TorznabCatType.TV, " | - TV Shows US and Canada");
            AddCategoryMapping(242, TorznabCatType.TV, " | - TV Shows UK and Ireland");
            AddCategoryMapping(819, TorznabCatType.TV, " | - Scandinavian series");
            AddCategoryMapping(1531, TorznabCatType.TV, " | - Spanish series");
            AddCategoryMapping(721, TorznabCatType.TV, " | - Italian series");
            AddCategoryMapping(1102, TorznabCatType.TV, " | - European series");
            AddCategoryMapping(1120, TorznabCatType.TV, " | - TV Shows in Africa, Middle East");
            AddCategoryMapping(1214, TorznabCatType.TV, " | - TV Shows Australia and New Zealand");
            AddCategoryMapping(387, TorznabCatType.TV, " | - Serials joint production of several countries");
            AddCategoryMapping(1359, TorznabCatType.TV, " | - Web series, webisodes and TV series for the pilot episode ..");
            AddCategoryMapping(271, TorznabCatType.TV, " | - 24 hours / 24");
            AddCategoryMapping(273, TorznabCatType.TV, " |- Альф / ALF");
            AddCategoryMapping(743, TorznabCatType.TV, " | - Grey's Anatomy / Grey's Anatomy + Private Practice / Priv ..");
            AddCategoryMapping(184, TorznabCatType.TV, " | - Buffy - the Vampire Slayer / Buffy + Angel / Angel");
            AddCategoryMapping(194, TorznabCatType.TV, " | - Bludlivaya California / Californication");
            AddCategoryMapping(85, TorznabCatType.TV, " |- Вавилон 5 / Babylon 5");
            AddCategoryMapping(1171, TorznabCatType.TV, " |- Викинги / Vikings");
            AddCategoryMapping(1417, TorznabCatType.TV, " | - Breaking Bad / Breaking Bad");
            AddCategoryMapping(1144, TorznabCatType.TV, " | - The Return of Sherlock Holmes / Return of Sherlock Holmes");
            AddCategoryMapping(595, TorznabCatType.TV, " |- Герои / Heroes");
            AddCategoryMapping(1288, TorznabCatType.TV, " | - Dexter / Dexter");
            AddCategoryMapping(1605, TorznabCatType.TV, " | - Two and a Half Men / Two and a Half Men");
            AddCategoryMapping(1694, TorznabCatType.TV, " |- Династия / Dynasty");
            AddCategoryMapping(1690, TorznabCatType.TV, " | - The Vampire Diaries / The Vampire Diaries + True Blood ..");
            AddCategoryMapping(820, TorznabCatType.TV, " |- Доктор Кто / Doctor Who + Торчвуд / Torchwood");
            AddCategoryMapping(625, TorznabCatType.TV, " |- Доктор Хаус / House M.D.");
            AddCategoryMapping(84, TorznabCatType.TV, " | - Druzyya / Friends + Joey / Joey");
            AddCategoryMapping(623, TorznabCatType.TV, " | - Fringe / Fringe");
            AddCategoryMapping(1798, TorznabCatType.TV, "| - Stargate: Atlantis; Universe / Stargate: Atlanti ..");
            AddCategoryMapping(106, TorznabCatType.TV, " | - Stargate: SG-1 / Stargate: SG1");
            AddCategoryMapping(166, TorznabCatType.TV, " | - Battlestar Galactica / Battlestar Galactica + Copper ..");
            AddCategoryMapping(236, TorznabCatType.TV, " | - Star Trek / Star Trek");
            AddCategoryMapping(1449, TorznabCatType.TV, " |- Игра престолов / Game of Thrones");
            AddCategoryMapping(507, TorznabCatType.TV, " | - How I Met Your Mother The Big Bang Theory +");
            AddCategoryMapping(504, TorznabCatType.TV, " |- Клан Сопрано / The Sopranos");
            AddCategoryMapping(536, TorznabCatType.TV, " |- Клиника / Scrubs");
            AddCategoryMapping(173, TorznabCatType.TV, " | - Коломбо / Columbo");
            AddCategoryMapping(918, TorznabCatType.TV, " | - Inspector Rex / Komissar Rex");
            AddCategoryMapping(920, TorznabCatType.TV, " | - Bones / Bones");
            AddCategoryMapping(203, TorznabCatType.TV, " | - Weeds / Weeds");
            AddCategoryMapping(1243, TorznabCatType.TV, "| - Cool Walker. Justice in Texas / Walker, Texas Ran ..");
            AddCategoryMapping(140, TorznabCatType.TV, " | - Masters of Horror / Masters of Horror");
            AddCategoryMapping(636, TorznabCatType.TV, " | - Mentalist / The Mentalist + Castle / Castle");
            AddCategoryMapping(606, TorznabCatType.TV, " | - Crime / CSI Location: Crime Scene Investigation");
            AddCategoryMapping(776, TorznabCatType.TV, " |- Мисс Марпл / Miss Marple");
            AddCategoryMapping(181, TorznabCatType.TV, "| - NCIS; Los Angeles; New Orleans");
            AddCategoryMapping(1499, TorznabCatType.TV, " | - Murder, She Wrote / Murder, She Wrote + Perry Mason ..");
            AddCategoryMapping(81, TorznabCatType.TV, " | - Survivors / LOST");
            AddCategoryMapping(266, TorznabCatType.TV, " | - Desperate Housewives / Desperate Housewives");
            AddCategoryMapping(252, TorznabCatType.TV, " | - Jailbreak / Prison Break");
            AddCategoryMapping(196, TorznabCatType.TV, " |- Санта Барбара / Santa Barbara");
            AddCategoryMapping(372, TorznabCatType.TV, " | - Supernatural / Supernatural");
            AddCategoryMapping(110, TorznabCatType.TV, " | - The X-Files / The X-Files");
            AddCategoryMapping(193, TorznabCatType.TV, " | - Sex and the City / Sex And The City");
            AddCategoryMapping(237, TorznabCatType.TV, " | - Sliding / Sliders");
            AddCategoryMapping(265, TorznabCatType.TV, " | - Ambulance / ER");
            AddCategoryMapping(1117, TorznabCatType.TV, " | - Octopus / La Piovra");
            AddCategoryMapping(497, TorznabCatType.TV, " | - Smallville / Smallville");
            AddCategoryMapping(121, TorznabCatType.TV, " | - Twin Peaks / Twin Peaks");
            AddCategoryMapping(134, TorznabCatType.TV, " | - Hercule Poirot / Hercule Poirot");
            AddCategoryMapping(195, TorznabCatType.TV, " | - For sub-standard hands");
            AddCategoryMapping(2366, TorznabCatType.TV, "Foreign TV shows (HD Video)");
            AddCategoryMapping(2401, TorznabCatType.TV, " |- Блудливая Калифорния / Californication (HD Video)");
            AddCategoryMapping(2390, TorznabCatType.TV, " | - Two and a Half Men / Two and a Half Men (HD Video)");
            AddCategoryMapping(1669, TorznabCatType.TV, " |- Викинги / Vikings (HD Video)");
            AddCategoryMapping(2391, TorznabCatType.TV, " |- Декстер / Dexter (HD Video)");
            AddCategoryMapping(2392, TorznabCatType.TV, " | - Friends / Friends (HD Video)");
            AddCategoryMapping(2407, TorznabCatType.TV, " |- Доктор Кто / Doctor Who; Торчвуд / Torchwood (HD Video)");
            AddCategoryMapping(2393, TorznabCatType.TV, " |- Доктор Хаус / House M.D. (HD Video)");
            AddCategoryMapping(2370, TorznabCatType.TV, " | - Fringe / Fringe (HD Video)");
            AddCategoryMapping(2394, TorznabCatType.TV, "| - Stargate: a C1; Atlantis; The Universe (HD Video)");
            AddCategoryMapping(2408, TorznabCatType.TV, "| - Battlestar Galactica / Battlestar Galactica; Capri ..");
            AddCategoryMapping(2395, TorznabCatType.TV, " | - Star Trek / Star Trek (HD Video)");
            AddCategoryMapping(2396, TorznabCatType.TV, "| - How I Met Your Mother; The Big Bang Theory (HD Vi ..");
            AddCategoryMapping(2397, TorznabCatType.TV, " |- Кости / Bones (HD Video)");
            AddCategoryMapping(2398, TorznabCatType.TV, " | - Weeds / Weeds (HD Video)");
            AddCategoryMapping(2399, TorznabCatType.TV, "| - Mentalist / The Mentalist; Castle / Castle (HD Video)");
            AddCategoryMapping(2400, TorznabCatType.TV, " | - Crime / CSI Location: Crime Scene Investigation (HD ..");
            AddCategoryMapping(2402, TorznabCatType.TV, " | - Survivors / LOST (HD Video)");
            AddCategoryMapping(2403, TorznabCatType.TV, " | - Jailbreak / Prison Break (HD Video)");
            AddCategoryMapping(2404, TorznabCatType.TV, " |- Сверхъестественное / Supernatural (HD Video)");
            AddCategoryMapping(2405, TorznabCatType.TV, " | - The X-Files / The X-Files (HD Video)");
            AddCategoryMapping(2406, TorznabCatType.TV, " |- Тайны Смолвиля / Smallville (HD Video)");
            AddCategoryMapping(911, TorznabCatType.TV, "Soaps Latin America, Turkey and India");
            AddCategoryMapping(1493, TorznabCatType.TV, " | - Actors and actresses of Latin American soap operas");
            AddCategoryMapping(1301, TorznabCatType.TV, " | - Indian series");
            AddCategoryMapping(704, TorznabCatType.TV, " | - Turkish serials");
            AddCategoryMapping(1940, TorznabCatType.TV, " | - Official brief version of Latin American soap operas");
            AddCategoryMapping(1574, TorznabCatType.TV, " | - Latin American soap operas with the voice acting (folders distribution)");
            AddCategoryMapping(1539, TorznabCatType.TV, " | - Latin American serials with subtitles");
            AddCategoryMapping(1500, TorznabCatType.TV, " |- OST");
            AddCategoryMapping(823, TorznabCatType.TV, " | - Богатые тоже плачут / The Rich Also Cry");
            AddCategoryMapping(1006, TorznabCatType.TV, " | - Вдова бланко / La Viuda de Blanco");
            AddCategoryMapping(877, TorznabCatType.TV, " | - Великолепный век / Magnificent Century");
            AddCategoryMapping(972, TorznabCatType.TV, " | - In the name of love / Por Amor");
            AddCategoryMapping(781, TorznabCatType.TV, " | - A girl named Fate / Milagros");
            AddCategoryMapping(1300, TorznabCatType.TV, " |- Дикий ангел / Muneca Brava");
            AddCategoryMapping(1803, TorznabCatType.TV, " | - Донья Барбара / Female Barbara");
            AddCategoryMapping(1298, TorznabCatType.TV, " | - Дороги Индии / Passage to India");
            AddCategoryMapping(825, TorznabCatType.TV, " | - Durnuška Betti / Yo Soy Betty la Fea");
            AddCategoryMapping(1606, TorznabCatType.TV, " | - The wife of Judas (wine of love) / La Mujer de Judas");
            AddCategoryMapping(1458, TorznabCatType.TV, " | - Cruel Angel / Anjo Mau");
            AddCategoryMapping(1463, TorznabCatType.TV, " | - Замарашка / Cara Sucia");
            AddCategoryMapping(1459, TorznabCatType.TV, " | - A Cinderella Story (Beautiful Loser) / Bella Calamidade ..");
            AddCategoryMapping(1461, TorznabCatType.TV, " | - Kacorri / Kachorra");
            AddCategoryMapping(718, TorznabCatType.TV, " |- Клон / O Clone");
            AddCategoryMapping(1498, TorznabCatType.TV, " | - Клятва / The Oath");
            AddCategoryMapping(907, TorznabCatType.TV, " | - Lalo / Lalola");
            AddCategoryMapping(992, TorznabCatType.TV, " | - Morena Clara / Clara Morena");
            AddCategoryMapping(607, TorznabCatType.TV, " | - Mi Segunda Madre / Mi segunda Madre");
            AddCategoryMapping(594, TorznabCatType.TV, " | - The rebellious spirit / Rebelde Way");
            AddCategoryMapping(775, TorznabCatType.TV, " | - Наследница / The Heiress");
            AddCategoryMapping(534, TorznabCatType.TV, " | - Nobody but you / Tu o Nadie");
            AddCategoryMapping(1462, TorznabCatType.TV, " | - Падре Корахе / Father Courage");
            AddCategoryMapping(1678, TorznabCatType.TV, " | - Падший ангел / Mas Sabe el Diablo");
            AddCategoryMapping(904, TorznabCatType.TV, " | - Предательство / The Betrayal");
            AddCategoryMapping(1460, TorznabCatType.TV, " | - Призрак Элены / The Phantom of Elena");
            AddCategoryMapping(816, TorznabCatType.TV, " | - Live your life / Viver a vida");
            AddCategoryMapping(815, TorznabCatType.TV, " | - Just Maria / Simplemente Maria");
            AddCategoryMapping(325, TorznabCatType.TV, " | - Rabыnya Isaura / Escrava Isaura");
            AddCategoryMapping(1457, TorznabCatType.TV, " | - Реванш 2000 / Retaliation 2000");
            AddCategoryMapping(1692, TorznabCatType.TV, " | - Family Ties / Lacos de Familia");
            AddCategoryMapping(1540, TorznabCatType.TV, " | - Perfect Beauty / Beleza pura");
            AddCategoryMapping(694, TorznabCatType.TV, " | - Secrets of Love / Los Misterios del Amor");
            AddCategoryMapping(1949, TorznabCatType.TV, " | - Фаворитка / A Favorita");
            AddCategoryMapping(1541, TorznabCatType.TV, " | - Цыганская кровь / Soy gitano");
            AddCategoryMapping(1941, TorznabCatType.TV, " | - Шторм / Storm");
            AddCategoryMapping(1537, TorznabCatType.TV, " | - For sub-standard hands");
            AddCategoryMapping(2100, TorznabCatType.TV, "Asian series");
            AddCategoryMapping(717, TorznabCatType.TV, " | - Chinese serials with subtitles");
            AddCategoryMapping(915, TorznabCatType.TV, " | - Korean TV shows with voice acting");
            AddCategoryMapping(1242, TorznabCatType.TV, " | - Korean serials with subtitles");
            AddCategoryMapping(2412, TorznabCatType.TV, " | - Other Asian series with the voice acting");
            AddCategoryMapping(1938, TorznabCatType.TV, " | - Taiwanese serials with subtitles");
            AddCategoryMapping(2104, TorznabCatType.TV, " | - Japanese serials with subtitles");
            AddCategoryMapping(1939, TorznabCatType.TV, " | - Japanese TV series with the voice acting");
            AddCategoryMapping(2102, TorznabCatType.TV, " | -. VMV and other videos");
            AddCategoryMapping(2103, TorznabCatType.TV, " |- OST");

            // Книги и журналы
            AddCategoryMapping(1411, TorznabCatType.Books, " | - Scanning, processing skanov");
            AddCategoryMapping(21, TorznabCatType.Books, "books");
            AddCategoryMapping(2157, TorznabCatType.Books, " | - Film, TV, animation");
            AddCategoryMapping(765, TorznabCatType.Books, " | - Design, Graphic Design");
            AddCategoryMapping(2019, TorznabCatType.Books, " | - Photography and video");
            AddCategoryMapping(31, TorznabCatType.Books, " | - Magazines and newspapers (general section)");
            AddCategoryMapping(1427, TorznabCatType.Books, " | - Esoteric Tarot, Feng Shui");
            AddCategoryMapping(2422, TorznabCatType.Books, " | - Astrology");
            AddCategoryMapping(2195, TorznabCatType.Books, "| - Beauty. Care. housekeeping");
            AddCategoryMapping(2521, TorznabCatType.Books, "| - Fashion. Style. Etiquette");
            AddCategoryMapping(2223, TorznabCatType.Books, " | - Travel and Tourism");
            AddCategoryMapping(2447, TorznabCatType.Books, " | - Celebrity idols");
            AddCategoryMapping(39, TorznabCatType.Books, " | - Miscellaneous");
            AddCategoryMapping(1101, TorznabCatType.Books, "For children, parents and teachers");
            AddCategoryMapping(745, TorznabCatType.Books, " | - Textbooks for kindergarten and primary school (..");
            AddCategoryMapping(1689, TorznabCatType.Books, " | - Textbooks for high school (grades 5-11)");
            AddCategoryMapping(2336, TorznabCatType.Books, " | - Teachers and educators");
            AddCategoryMapping(2337, TorznabCatType.Books, " | - Scientific-popular and informative literature (for children ..");
            AddCategoryMapping(1353, TorznabCatType.Books, " | - Leisure and creativity");
            AddCategoryMapping(1400, TorznabCatType.Books, " | - Education and development");
            AddCategoryMapping(1415, TorznabCatType.Books, "| - Hood. lit-ra for preschool and elementary grades");
            AddCategoryMapping(2046, TorznabCatType.Books, "| - Hood. lit-ra for the middle and upper classes");
            AddCategoryMapping(1802, TorznabCatType.Books, "Sports, physical training, martial arts");
            AddCategoryMapping(2189, TorznabCatType.Books, " | - Football");
            AddCategoryMapping(2190, TorznabCatType.Books, " | - Hockey");
            AddCategoryMapping(2443, TorznabCatType.Books, " | - Team sports");
            AddCategoryMapping(1477, TorznabCatType.Books, "| - Athletics. Swimming. Gymnastics. Weightlifting...");
            AddCategoryMapping(669, TorznabCatType.Books, "| - Motorsport. Motorcycling. cycle racing");
            AddCategoryMapping(2196, TorznabCatType.Books, "| - Chess. Checkers");
            AddCategoryMapping(2056, TorznabCatType.Books, " | - Martial Arts, Martial Arts");
            AddCategoryMapping(1436, TorznabCatType.Books, " | - Extreme");
            AddCategoryMapping(2191, TorznabCatType.Books, " | - Fitness, fitness, bodybuilding");
            AddCategoryMapping(2477, TorznabCatType.Books, " | - Sports press");
            AddCategoryMapping(1680, TorznabCatType.Books, "Humanitarian sciences");
            AddCategoryMapping(1684, TorznabCatType.Books, "| - Arts. Cultural");
            AddCategoryMapping(2446, TorznabCatType.Books, "| - Folklore. Epic. Mythology");
            AddCategoryMapping(2524, TorznabCatType.Books, " | - Literature");
            AddCategoryMapping(2525, TorznabCatType.Books, " | - Linguistics");
            AddCategoryMapping(995, TorznabCatType.Books, " | - Philosophy");
            AddCategoryMapping(2022, TorznabCatType.Books, " | - Political Science");
            AddCategoryMapping(2471, TorznabCatType.Books, " | - Sociology");
            AddCategoryMapping(2375, TorznabCatType.Books, " | - Journalism, Journalism");
            AddCategoryMapping(764, TorznabCatType.Books, " | - Business, Management");
            AddCategoryMapping(1685, TorznabCatType.Books, " | - Marketing");
            AddCategoryMapping(1688, TorznabCatType.Books, " | - Economy");
            AddCategoryMapping(2472, TorznabCatType.Books, " | - Finance");
            AddCategoryMapping(1687, TorznabCatType.Books, "| - Jurisprudence. Right. criminalistics");
            AddCategoryMapping(2020, TorznabCatType.Books, "Historical sciences");
            AddCategoryMapping(1349, TorznabCatType.Books, " | - Philosophy and Methodology of Historical Science");
            AddCategoryMapping(1967, TorznabCatType.Books, " | - Historical sources");
            AddCategoryMapping(2049, TorznabCatType.Books, " | - Historic Person");
            AddCategoryMapping(1681, TorznabCatType.Books, " | - Alternative historical theories");
            AddCategoryMapping(2319, TorznabCatType.Books, " | - Archaeology");
            AddCategoryMapping(2434, TorznabCatType.Books, "| - Ancient World. Antiquity");
            AddCategoryMapping(1683, TorznabCatType.Books, " | - The Middle Ages");
            AddCategoryMapping(2444, TorznabCatType.Books, " | - History of modern and contemporary");
            AddCategoryMapping(2427, TorznabCatType.Books, " | - European History");
            AddCategoryMapping(2452, TorznabCatType.Books, " | - History of Asia and Africa");
            AddCategoryMapping(2445, TorznabCatType.Books, " | - History of America, Australia, Oceania");
            AddCategoryMapping(2435, TorznabCatType.Books, " | - History of Russia");
            AddCategoryMapping(2436, TorznabCatType.Books, " | - The Age of the USSR");
            AddCategoryMapping(2453, TorznabCatType.Books, " | - History of the countries of the former USSR");
            AddCategoryMapping(2320, TorznabCatType.Books, " | - Ethnography, anthropology");
            AddCategoryMapping(1801, TorznabCatType.Books, "| - International relations. Diplomacy");
            AddCategoryMapping(2023, TorznabCatType.Books, "Accurate, natural and engineering sciences");
            AddCategoryMapping(2024, TorznabCatType.Books, " | - Aviation / Cosmonautics");
            AddCategoryMapping(2026, TorznabCatType.Books, " | - Physics");
            AddCategoryMapping(2192, TorznabCatType.Books, " | - Astronomy");
            AddCategoryMapping(2027, TorznabCatType.Books, " | - Biology / Ecology");
            AddCategoryMapping(295, TorznabCatType.Books, " | - Chemistry / Biochemistry");
            AddCategoryMapping(2028, TorznabCatType.Books, " | - Mathematics");
            AddCategoryMapping(2029, TorznabCatType.Books, " | - Geography / Geology / Geodesy");
            AddCategoryMapping(1325, TorznabCatType.Books, " | - Electronics / Radio");
            AddCategoryMapping(2386, TorznabCatType.Books, " | - Diagrams and service manuals (original documents)");
            AddCategoryMapping(2031, TorznabCatType.Books, " | - Architecture / Construction / Engineering networks");
            AddCategoryMapping(2030, TorznabCatType.Books, " | - Engineering");
            AddCategoryMapping(2526, TorznabCatType.Books, " | - Welding / Soldering / Non-Destructive Testing");
            AddCategoryMapping(2527, TorznabCatType.Books, " | - Automation / Robotics");
            AddCategoryMapping(2254, TorznabCatType.Books, " | - Metallurgy / Materials");
            AddCategoryMapping(2376, TorznabCatType.Books, " | - Mechanics, strength of materials");
            AddCategoryMapping(2054, TorznabCatType.Books, " | - Power engineering / electrical");
            AddCategoryMapping(770, TorznabCatType.Books, " | - Oil, Gas and Chemicals");
            AddCategoryMapping(2476, TorznabCatType.Books, " | - Agriculture and food industry");
            AddCategoryMapping(2494, TorznabCatType.Books, " | - Railway case");
            AddCategoryMapping(1528, TorznabCatType.Books, " | - Normative documentation");
            AddCategoryMapping(2032, TorznabCatType.Books, " | - Journals: scientific, popular, radio and others.");
            AddCategoryMapping(768, TorznabCatType.Books, "Warfare");
            AddCategoryMapping(2099, TorznabCatType.Books, " | - Militaria");
            AddCategoryMapping(2021, TorznabCatType.Books, " | - Military History");
            AddCategoryMapping(2437, TorznabCatType.Books, " | - History of the Second World War");
            AddCategoryMapping(1447, TorznabCatType.Books, " | - Military equipment");
            AddCategoryMapping(2468, TorznabCatType.Books, " | - Small arms");
            AddCategoryMapping(2469, TorznabCatType.Books, " | - Educational literature");
            AddCategoryMapping(2470, TorznabCatType.Books, " | - Special forces of the world");
            AddCategoryMapping(1686, TorznabCatType.Books, "Faith and Religion");
            AddCategoryMapping(2215, TorznabCatType.Books, " | - Christianity");
            AddCategoryMapping(2216, TorznabCatType.Books, " | - Islam");
            AddCategoryMapping(2217, TorznabCatType.Books, " | - Religions of India, Tibet and East Asia / Judaism");
            AddCategoryMapping(2218, TorznabCatType.Books, " | - Non-traditional religious, spiritual and mystical teachings ..");
            AddCategoryMapping(2252, TorznabCatType.Books, "| - Religion. History of Religions. Atheism");
            AddCategoryMapping(767, TorznabCatType.Books, "psychology");
            AddCategoryMapping(2515, TorznabCatType.Books, " | - General and Applied Psychology");
            AddCategoryMapping(2516, TorznabCatType.Books, " | - Psychotherapy and Counseling");
            AddCategoryMapping(2517, TorznabCatType.Books, " | - Psychodiagnostics and psyhokorrektsyya");
            AddCategoryMapping(2518, TorznabCatType.Books, " | - Social psychology and psychology of relationships");
            AddCategoryMapping(2519, TorznabCatType.Books, " | - Training and Coaching");
            AddCategoryMapping(2520, TorznabCatType.Books, " | - Personal development and self-improvement");
            AddCategoryMapping(1696, TorznabCatType.Books, " | - Popular Psychology");
            AddCategoryMapping(2253, TorznabCatType.Books, "| - Sexology. Relations between the sexes");
            AddCategoryMapping(2033, TorznabCatType.Books, "Collecting, hobby and hobbies");
            AddCategoryMapping(1412, TorznabCatType.Books, "| - Collecting and auxiliary ist. discipline");
            AddCategoryMapping(1446, TorznabCatType.Books, " | - Embroidery");
            AddCategoryMapping(753, TorznabCatType.Books, " | - Knitting");
            AddCategoryMapping(2037, TorznabCatType.Books, " | - Sewing, Patchwork");
            AddCategoryMapping(2224, TorznabCatType.Books, " | - Lace");
            AddCategoryMapping(2194, TorznabCatType.Books, "| - Beading. Yuvelirika. Jewellery wire.");
            AddCategoryMapping(2418, TorznabCatType.Books, " | - Paper Art");
            AddCategoryMapping(1410, TorznabCatType.Books, " | - Other arts and crafts");
            AddCategoryMapping(2034, TorznabCatType.Books, " | - Pets and aquariums");
            AddCategoryMapping(2433, TorznabCatType.Books, " | - Hunting and fishing");
            AddCategoryMapping(1961, TorznabCatType.Books, " | - Cooking (Book)");
            AddCategoryMapping(2432, TorznabCatType.Books, " | - Cooking (newspapers and magazines)");
            AddCategoryMapping(565, TorznabCatType.Books, " | - Modelling");
            AddCategoryMapping(1523, TorznabCatType.Books, " | - Farmland / Floriculture");
            AddCategoryMapping(1575, TorznabCatType.Books, " | - Repair, private construction, design of interiors");
            AddCategoryMapping(1520, TorznabCatType.Books, " | - Woodworking");
            AddCategoryMapping(2424, TorznabCatType.Books, " | - Board Games");
            AddCategoryMapping(769, TorznabCatType.Books, " | - Other Hobbies");
            AddCategoryMapping(2038, TorznabCatType.Books, "Fiction");
            AddCategoryMapping(2043, TorznabCatType.Books, " | - Russian literature");
            AddCategoryMapping(2042, TorznabCatType.Books, " | - Foreign literature (up to 1900)");
            AddCategoryMapping(2041, TorznabCatType.Books, " | - Foreign literature (XX and XXI century)");
            AddCategoryMapping(2044, TorznabCatType.Books, " | - Detective, Action");
            AddCategoryMapping(2039, TorznabCatType.Books, " | - Female Novel");
            AddCategoryMapping(2045, TorznabCatType.Books, " | - Domestic science fiction / fantasy / mystic");
            AddCategoryMapping(2080, TorznabCatType.Books, " | - International science fiction / fantasy / mystic");
            AddCategoryMapping(2047, TorznabCatType.Books, " | - Adventure");
            AddCategoryMapping(2193, TorznabCatType.Books, " | - Literary Magazines");
            AddCategoryMapping(1418, TorznabCatType.Books, "Computer books");
            AddCategoryMapping(1422, TorznabCatType.Books, " | - Software from Microsoft");
            AddCategoryMapping(1423, TorznabCatType.Books, " | - Other software");
            AddCategoryMapping(1424, TorznabCatType.Books, " |- Mac OS; Linux, FreeBSD и прочие *NIX");
            AddCategoryMapping(1445, TorznabCatType.Books, " | - RDBMS");
            AddCategoryMapping(1425, TorznabCatType.Books, " | - Web Design and Programming");
            AddCategoryMapping(1426, TorznabCatType.Books, " | - Programming");
            AddCategoryMapping(1428, TorznabCatType.Books, " | - Graphics, Video Processing");
            AddCategoryMapping(1429, TorznabCatType.Books, " | - Network / VoIP");
            AddCategoryMapping(1430, TorznabCatType.Books, " | - Hacking and Security");
            AddCategoryMapping(1431, TorznabCatType.Books, " | - Iron (book on a PC)");
            AddCategoryMapping(1433, TorznabCatType.Books, " | - Engineering and scientific programs");
            AddCategoryMapping(1432, TorznabCatType.Books, " | - Computer magazines and annexes");
            AddCategoryMapping(2202, TorznabCatType.Books, " | - Disc applications to gaming magazines");
            AddCategoryMapping(862, TorznabCatType.Books, "Comics");
            AddCategoryMapping(2461, TorznabCatType.Books, " | - Comics in Russian");
            AddCategoryMapping(2462, TorznabCatType.Books, " | - Marvel Comics publishing");
            AddCategoryMapping(2463, TorznabCatType.Books, " | - DC Comics publishing");
            AddCategoryMapping(2464, TorznabCatType.Books, " | - Comics from other publishers");
            AddCategoryMapping(2473, TorznabCatType.Books, " | - Comics in other languages");
            AddCategoryMapping(2465, TorznabCatType.Books, " | - Manga (in foreign languages)");
            AddCategoryMapping(2048, TorznabCatType.Books, "Collections of books and libraries");
            AddCategoryMapping(1238, TorznabCatType.Books, " | - Library (mirror network libraries / collections)");
            AddCategoryMapping(2055, TorznabCatType.Books, " | - Thematic collections (collections)");
            AddCategoryMapping(754, TorznabCatType.Books, " | - Multidisciplinary collections (collections)");
            AddCategoryMapping(2114, TorznabCatType.Books, "Multimedia and online publications");
            AddCategoryMapping(2438, TorznabCatType.Books, " | - Multimedia Encyclopedia");
            AddCategoryMapping(2439, TorznabCatType.Books, " | - Interactive tutorials and educational materials");
            AddCategoryMapping(2440, TorznabCatType.Books, " | - Educational publications for children");
            AddCategoryMapping(2441, TorznabCatType.Books, "| - Cooking. Floriculture. housekeeping");
            AddCategoryMapping(2442, TorznabCatType.Books, "| - Culture. Art. History");

            // Обучение иностранным языкам
            AddCategoryMapping(2362, TorznabCatType.Books, "Foreign Language for Adults");
            AddCategoryMapping(1265, TorznabCatType.Books, " | - English (for adults)");
            AddCategoryMapping(1266, TorznabCatType.Books, " | - German");
            AddCategoryMapping(1267, TorznabCatType.Books, " | - French");
            AddCategoryMapping(1358, TorznabCatType.Books, " | - Spanish");
            AddCategoryMapping(2363, TorznabCatType.Books, " | - Italian");
            AddCategoryMapping(1268, TorznabCatType.Books, " | - Other European languages");
            AddCategoryMapping(1673, TorznabCatType.Books, " | - Arabic");
            AddCategoryMapping(1269, TorznabCatType.Books, " | - Chinese");
            AddCategoryMapping(1270, TorznabCatType.Books, " | - Japanese");
            AddCategoryMapping(1275, TorznabCatType.Books, " | - Other Asian languages");
            AddCategoryMapping(2364, TorznabCatType.Books, " | - Russian as a foreign language");
            AddCategoryMapping(1276, TorznabCatType.Books, " | - Multilanguage collections");
            AddCategoryMapping(1274, TorznabCatType.Books, " | - Other (foreign languages)");
            AddCategoryMapping(2094, TorznabCatType.Books, " | - LIM-courses");
            AddCategoryMapping(1264, TorznabCatType.Books, "Foreign languages &#8203;&#8203;for children");
            AddCategoryMapping(2358, TorznabCatType.Books, " | - English (for children)");
            AddCategoryMapping(2359, TorznabCatType.Books, " | - Other European languages &#8203;&#8203;(for children)");
            AddCategoryMapping(2360, TorznabCatType.Books, " | - Eastern languages &#8203;&#8203;(for children)");
            AddCategoryMapping(2361, TorznabCatType.Books, " | - School textbooks, exam (for children)");
            AddCategoryMapping(2057, TorznabCatType.Books, "Fiction");
            AddCategoryMapping(2355, TorznabCatType.Books, " | - Fiction in English");
            AddCategoryMapping(2474, TorznabCatType.Books, " | - Fiction French");
            AddCategoryMapping(2356, TorznabCatType.Books, " | - Fiction in other European languages");
            AddCategoryMapping(2357, TorznabCatType.Books, " | - Fiction in oriental languages");
            AddCategoryMapping(2413, TorznabCatType.Books, "Audio Books in foreign languages");
            AddCategoryMapping(1501, TorznabCatType.Books, " | - Audiobooks in English");
            AddCategoryMapping(1580, TorznabCatType.Books, " | - Audiobooks in German");
            AddCategoryMapping(525, TorznabCatType.Books, " | - Audiobooks in other languages");

            // Обучающее видео
            AddCategoryMapping(610, TorznabCatType.Books, "Video tutorials and interactive training DVD");
            AddCategoryMapping(1568, TorznabCatType.Books, " | - Cooking");
            AddCategoryMapping(1542, TorznabCatType.Books, " | - Sport");
            AddCategoryMapping(2335, TorznabCatType.Books, " | - Fitness - Cardio, Strength Training");
            AddCategoryMapping(1544, TorznabCatType.Books, " | - Fitness - Mind and Body");
            AddCategoryMapping(1545, TorznabCatType.Books, " | - Extreme");
            AddCategoryMapping(1546, TorznabCatType.Books, " | - Bodybuilding");
            AddCategoryMapping(1549, TorznabCatType.Books, " | - Health Practice");
            AddCategoryMapping(1597, TorznabCatType.Books, " | - Yoga");
            AddCategoryMapping(1552, TorznabCatType.Books, " | - Video and Snapshots");
            AddCategoryMapping(1550, TorznabCatType.Books, " | - Personal care");
            AddCategoryMapping(1553, TorznabCatType.Books, " | - Drawing");
            AddCategoryMapping(1554, TorznabCatType.Books, " | - Playing the guitar");
            AddCategoryMapping(617, TorznabCatType.Books, " | - Percussion");
            AddCategoryMapping(1555, TorznabCatType.Books, " | - Other musical instruments");
            AddCategoryMapping(2017, TorznabCatType.Books, " | - Play the bass guitar");
            AddCategoryMapping(1257, TorznabCatType.Books, " | - Ballroom dancing");
            AddCategoryMapping(1258, TorznabCatType.Books, " | - Belly Dance");
            AddCategoryMapping(2208, TorznabCatType.Books, " | - The street and club dancing");
            AddCategoryMapping(677, TorznabCatType.Books, " | - Dancing, miscellaneous");
            AddCategoryMapping(1255, TorznabCatType.Books, " | - Hunting");
            AddCategoryMapping(1479, TorznabCatType.Books, " | - Fishing and spearfishing");
            AddCategoryMapping(1261, TorznabCatType.Books, " | - Tricks and stunts");
            AddCategoryMapping(614, TorznabCatType.Books, " | - Education");
            AddCategoryMapping(1259, TorznabCatType.Books, " | - Business, Economics and Finance");
            AddCategoryMapping(2065, TorznabCatType.Books, " | - Pregnancy, childbirth, motherhood");
            AddCategoryMapping(1254, TorznabCatType.Books, " | - Training videos for children");
            AddCategoryMapping(1260, TorznabCatType.Books, " | - Psychology");
            AddCategoryMapping(2209, TorznabCatType.Books, " | - Esoteric, self-development");
            AddCategoryMapping(2210, TorznabCatType.Books, " | - Van, dating");
            AddCategoryMapping(1547, TorznabCatType.Books, " | - Construction, repair and design");
            AddCategoryMapping(1548, TorznabCatType.Books, " | - Wood and metal");
            AddCategoryMapping(2211, TorznabCatType.Books, " | - Plants and Animals");
            AddCategoryMapping(615, TorznabCatType.Books, " | - Miscellaneous");
            AddCategoryMapping(1581, TorznabCatType.Books, "Martial Arts (Video Tutorials)");
            AddCategoryMapping(1590, TorznabCatType.Books, " | - Aikido and Aiki-jutsu");
            AddCategoryMapping(1587, TorznabCatType.Books, " | - Vin Chun");
            AddCategoryMapping(1594, TorznabCatType.Books, " | - Jiu-Jitsu");
            AddCategoryMapping(1591, TorznabCatType.Books, " | - Judo and Sambo");
            AddCategoryMapping(1588, TorznabCatType.Books, " | - Karate");
            AddCategoryMapping(1596, TorznabCatType.Books, " | - Knife Fight");
            AddCategoryMapping(1585, TorznabCatType.Books, " | - Work with weapon");
            AddCategoryMapping(1586, TorznabCatType.Books, " | - Russian style");
            AddCategoryMapping(2078, TorznabCatType.Books, " | - Dogfight");
            AddCategoryMapping(1929, TorznabCatType.Books, " | - Mixed styles");
            AddCategoryMapping(1593, TorznabCatType.Books, " | - Percussion styles");
            AddCategoryMapping(1592, TorznabCatType.Books, " | - This is a");
            AddCategoryMapping(1595, TorznabCatType.Books, " | - Miscellaneous");
            AddCategoryMapping(1556, TorznabCatType.Books, "Computer video tutorials and interactive training DVD");
            AddCategoryMapping(1560, TorznabCatType.Books, " | - Networks and Security");
            AddCategoryMapping(1561, TorznabCatType.Books, " | - OS and Microsoft Server Software");
            AddCategoryMapping(1653, TorznabCatType.Books, " | - Microsoft Office program");
            AddCategoryMapping(1570, TorznabCatType.Books, " | - OS and UNIX family program");
            AddCategoryMapping(1654, TorznabCatType.Books, " | - Adobe Photoshop");
            AddCategoryMapping(1655, TorznabCatType.Books, " |- Autodesk Maya");
            AddCategoryMapping(1656, TorznabCatType.Books, " | - Autodesk 3ds Max");
            AddCategoryMapping(1930, TorznabCatType.Books, " |- Autodesk Softimage (XSI)");
            AddCategoryMapping(1931, TorznabCatType.Books, " |- ZBrush");
            AddCategoryMapping(1932, TorznabCatType.Books, " |- Flash, Flex и ActionScript");
            AddCategoryMapping(1562, TorznabCatType.Books, " | - 2D-графика");
            AddCategoryMapping(1563, TorznabCatType.Books, " | - 3D-графика");
            AddCategoryMapping(1626, TorznabCatType.Books, " | - Engineering and scientific programs");
            AddCategoryMapping(1564, TorznabCatType.Books, " | - Web-design");
            AddCategoryMapping(1565, TorznabCatType.Books, " | - Programming");
            AddCategoryMapping(1559, TorznabCatType.Books, " | - Software for Mac OS");
            AddCategoryMapping(1566, TorznabCatType.Books, " | - Working with video");
            AddCategoryMapping(1573, TorznabCatType.Books, " | - Working with sound");
            AddCategoryMapping(1567, TorznabCatType.Books, " | - Other (Computer video tutorials)");

            // Аудиокниги
            AddCategoryMapping(2326, TorznabCatType.Audio, "Auditions, history, memoirs");
            AddCategoryMapping(574, TorznabCatType.Audio, " | - [Audio] Auditions and readings");
            AddCategoryMapping(1036, TorznabCatType.Audio, " | - [Audio] Lots of great people");
            AddCategoryMapping(400, TorznabCatType.Audio, " | - [Audio] Historical Book");
            AddCategoryMapping(2389, TorznabCatType.Audio, "Science fiction, fantasy, mystery, horror, fanfiction");
            AddCategoryMapping(2388, TorznabCatType.Audio, " | - [Audio] Foreign fiction, fantasy, mystery, horror, ..");
            AddCategoryMapping(2387, TorznabCatType.Audio, " | - [Audio] Russian fiction, fantasy, mystery, horror, ..");
            AddCategoryMapping(2348, TorznabCatType.Audio, " | - [Audio] Puzzle / Miscellaneous Science Fiction, Fantasy, Mystery, too ..");
            AddCategoryMapping(2327, TorznabCatType.Audio, "Fiction");
            AddCategoryMapping(695, TorznabCatType.Audio, " | - [Audio] Poetry");
            AddCategoryMapping(399, TorznabCatType.Audio, " | - [Audio] Foreign literature");
            AddCategoryMapping(402, TorznabCatType.Audio, " | - [Audio] Russian literature");
            AddCategoryMapping(490, TorznabCatType.Audio, " | - [Audio] Children's Books");
            AddCategoryMapping(499, TorznabCatType.Audio, " | - [Audio] Detectives, Adventure, Thriller, Action");
            AddCategoryMapping(2324, TorznabCatType.Audio, "religion");
            AddCategoryMapping(2325, TorznabCatType.Audio, " | - [Audio] Orthodoxy");
            AddCategoryMapping(2342, TorznabCatType.Audio, " | - [Audio] Islam");
            AddCategoryMapping(530, TorznabCatType.Audio, " | - [Audio] Other traditional religion");
            AddCategoryMapping(2152, TorznabCatType.Audio, " | - [Audio] Non-traditional religious and philosophical teachings");
            AddCategoryMapping(2328, TorznabCatType.Audio, "other literature");
            AddCategoryMapping(403, TorznabCatType.Audio, " | - [Audio] academic and popular literature");
            AddCategoryMapping(1279, TorznabCatType.Audio, " | - [Audio] lossless-audio books");
            AddCategoryMapping(716, TorznabCatType.Audio, " | - [Audio] Business");
            AddCategoryMapping(2165, TorznabCatType.Audio, " | - [Audio] Miscellaneous");
            AddCategoryMapping(401, TorznabCatType.Audio, " | - [Audio], sub-standard distribution");

            // Все по авто и мото
            AddCategoryMapping(1964, TorznabCatType.Books, "Repair and maintenance of vehicles");
            AddCategoryMapping(1973, TorznabCatType.Books, " | - Original catalogs on selection of spare parts");
            AddCategoryMapping(1974, TorznabCatType.Books, " | - Non-original spare parts catalogs for selection");
            AddCategoryMapping(1975, TorznabCatType.Books, " | - Diagnostic and repair programs");
            AddCategoryMapping(1976, TorznabCatType.Books, " | - Tuning, chip tuning, tuning");
            AddCategoryMapping(1977, TorznabCatType.Books, " | - Books for the repair / maintenance / operation of the vehicle");
            AddCategoryMapping(1203, TorznabCatType.Books, " | - Multimediyki repair / maintenance / operation of the vehicle");
            AddCategoryMapping(1978, TorznabCatType.Books, " | - Accounting, utilities, etc.");
            AddCategoryMapping(1979, TorznabCatType.Books, " | - Virtual Driving School");
            AddCategoryMapping(1980, TorznabCatType.Books, " | - Video lessons on driving vehicles");
            AddCategoryMapping(1981, TorznabCatType.Books, " | - Tutorials repair vehicles");
            AddCategoryMapping(1970, TorznabCatType.Books, " | - Journals by car / moto");
            AddCategoryMapping(334, TorznabCatType.Books, " | - Water transport");
            AddCategoryMapping(1202, TorznabCatType.Books, "Movies and transfer by car / moto");
            AddCategoryMapping(1985, TorznabCatType.Books, " | - Documentary / educational films");
            AddCategoryMapping(1982, TorznabCatType.Books, " | - Entertainment shows");
            AddCategoryMapping(2151, TorznabCatType.Books, " |- Top Gear/Топ Гир");
            AddCategoryMapping(1983, TorznabCatType.Books, " | - Test Drive / Reviews / Motor");
            AddCategoryMapping(1984, TorznabCatType.Books, " | - Tuning / Fast and the Furious");

            // Музыка
            AddCategoryMapping(409, TorznabCatType.Audio, "Classical and contemporary academic music");
            AddCategoryMapping(1660, TorznabCatType.Audio, " | - In-house digitizing (Classical Music)");
            AddCategoryMapping(1164, TorznabCatType.Audio, " | - Multi-channel music (classical and modern classics in ..");
            AddCategoryMapping(1884, TorznabCatType.Audio, " | - Hi-Res stereo (classic and modern classic in obrabot ..");
            AddCategoryMapping(445, TorznabCatType.Audio, " | - Classical music (Video)");
            AddCategoryMapping(984, TorznabCatType.Audio, " | - Classical music (DVD and HD Video)");
            AddCategoryMapping(702, TorznabCatType.Audio, " | - Opera (Video)");
            AddCategoryMapping(983, TorznabCatType.Audio, " |- Опера (DVD и HD Видео)");
            AddCategoryMapping(1990, TorznabCatType.Audio, " | - Ballet and contemporary dance (Video, DVD and HD Video)");
            AddCategoryMapping(560, TorznabCatType.Audio, " | - Complete collection of works and multi-disc edition (lossl ..");
            AddCategoryMapping(794, TorznabCatType.Audio, " |- Опера (lossless)");
            AddCategoryMapping(556, TorznabCatType.Audio, " | - Vocal music (lossless)");
            AddCategoryMapping(2307, TorznabCatType.Audio, " | - Horovaya Music (lossless)");
            AddCategoryMapping(557, TorznabCatType.Audio, " | - Orchestral music (lossless)");
            AddCategoryMapping(2308, TorznabCatType.Audio, " | - Concerto for Orchestra Instrument (lossless)");
            AddCategoryMapping(558, TorznabCatType.Audio, " | - Chamber instrumental music (lossless)");
            AddCategoryMapping(793, TorznabCatType.Audio, " | - Solo instrumental music (lossless)");
            AddCategoryMapping(436, TorznabCatType.Audio, " | - Complete collection of works and multi-disc edition (lossy ..");
            AddCategoryMapping(2309, TorznabCatType.Audio, " | - Vocal and choral music (lossy)");
            AddCategoryMapping(2310, TorznabCatType.Audio, " | - Orchestral music (lossy)");
            AddCategoryMapping(2311, TorznabCatType.Audio, " | - Chamber and solo instrumental music (lossy)");
            AddCategoryMapping(969, TorznabCatType.Audio, " | - Classics in modern processing, Classical Crossover (l ..");
            AddCategoryMapping(1125, TorznabCatType.Audio, "Folklore, Folk and World Music");
            AddCategoryMapping(1130, TorznabCatType.Audio, " |- Восточноевропейский фолк (lossy)");
            AddCategoryMapping(1131, TorznabCatType.Audio, " | - Eastern European Folk (lossless)");
            AddCategoryMapping(1132, TorznabCatType.Audio, " |- Западноевропейский фолк (lossy)");
            AddCategoryMapping(1133, TorznabCatType.Audio, " |- Западноевропейский фолк (lossless)");
            AddCategoryMapping(2084, TorznabCatType.Audio, " | - Klezmer and Jewish folklore (lossy and lossless)");
            AddCategoryMapping(1128, TorznabCatType.Audio, " | - World Music Siberia, Central Asia and East Asia (loss ..");
            AddCategoryMapping(1129, TorznabCatType.Audio, " | - World Music Siberia, Central Asia and East Asia (loss ..");
            AddCategoryMapping(1856, TorznabCatType.Audio, " | - World Music India (lossy)");
            AddCategoryMapping(2430, TorznabCatType.Audio, " | - World Music India (lossless)");
            AddCategoryMapping(1283, TorznabCatType.Audio, " | - World Music Africa and the Middle East (lossy)");
            AddCategoryMapping(2085, TorznabCatType.Audio, " | - World Music Africa and the Middle East (lossless)");
            AddCategoryMapping(1282, TorznabCatType.Audio, " | - Ethnic Music of the Caucasus and Transcaucasia (lossy and lossless ..");
            AddCategoryMapping(1284, TorznabCatType.Audio, " | - World Music Americas (lossy)");
            AddCategoryMapping(1285, TorznabCatType.Audio, " | - World Music Americas (lossless)");
            AddCategoryMapping(1138, TorznabCatType.Audio, " | - World Music Australia, the Pacific and Indian oceans ..");
            AddCategoryMapping(1136, TorznabCatType.Audio, " |- Country, Bluegrass (lossy)");
            AddCategoryMapping(1137, TorznabCatType.Audio, " |- Country, Bluegrass (lossless)");
            AddCategoryMapping(1141, TorznabCatType.Audio, " | - Folklore, Folk and World Music (Video)");
            AddCategoryMapping(1142, TorznabCatType.Audio, " | - Folklore, Folk and World Music (DVD Video)");
            AddCategoryMapping(2530, TorznabCatType.Audio, " | - Folklore, Folk and World Music (HD Video)");
            AddCategoryMapping(506, TorznabCatType.Audio, " | - Folklore, Folk and World Music (own otsif ..");
            AddCategoryMapping(1849, TorznabCatType.Audio, "New Age, Relax, Meditative & Flamenco");
            AddCategoryMapping(1126, TorznabCatType.Audio, " |- NewAge & Meditative (lossy)");
            AddCategoryMapping(1127, TorznabCatType.Audio, " |- NewAge & Meditative (lossless)");
            AddCategoryMapping(1134, TorznabCatType.Audio, " | - Flamenco and acoustic guitar (lossy)");
            AddCategoryMapping(1135, TorznabCatType.Audio, " | - Flamenco and acoustic guitar (lossless)");
            AddCategoryMapping(2352, TorznabCatType.Audio, " |- New Age, Relax, Meditative & Flamenco (Видео)");
            AddCategoryMapping(2351, TorznabCatType.Audio, " |- New Age, Relax, Meditative & Flamenco (DVD и HD Видео)");
            AddCategoryMapping(855, TorznabCatType.Audio, " | - Sounds of Nature");
            AddCategoryMapping(408, TorznabCatType.Audio, "Рэп, Хип-Хоп, R'n'B");
            AddCategoryMapping(441, TorznabCatType.Audio, " | - Domestic Rap, Hip-Hop (lossy)");
            AddCategoryMapping(1173, TorznabCatType.Audio, " |- Отечественный R'n'B (lossy)");
            AddCategoryMapping(1486, TorznabCatType.Audio, " | - Domestic Rap, Hip-Hop, R'n'B (lossless)");
            AddCategoryMapping(1172, TorznabCatType.Audio, " |- Зарубежный R'n'B (lossy)");
            AddCategoryMapping(446, TorznabCatType.Audio, " | - Foreign Rap, Hip-Hop (lossy)");
            AddCategoryMapping(909, TorznabCatType.Audio, " | - Foreign Rap, Hip-Hop (lossless)");
            AddCategoryMapping(1665, TorznabCatType.Audio, " |- Зарубежный R'n'B (lossless)");
            AddCategoryMapping(1835, TorznabCatType.Audio, " | - Rap, Hip-Hop, R'n'B (own digitization)");
            AddCategoryMapping(1189, TorznabCatType.Audio, " | - Domestic Rap, Hip-Hop (Video)");
            AddCategoryMapping(1455, TorznabCatType.Audio, " | - Domestic R'n'B (Video)");
            AddCategoryMapping(442, TorznabCatType.Audio, " | - Foreign Rap, Hip-Hop (Video)");
            AddCategoryMapping(1174, TorznabCatType.Audio, " | - Foreign R'n'B (Video)");
            AddCategoryMapping(1107, TorznabCatType.Audio, " |- Рэп, Хип-Хоп, R'n'B (DVD Video)");
            AddCategoryMapping(2529, TorznabCatType.Audio, " |- Рэп, Хип-Хоп, R'n'B (HD Видео)");
            AddCategoryMapping(1760, TorznabCatType.Audio, "Reggae, Ska, Dub");
            AddCategoryMapping(1764, TorznabCatType.Audio, " | - Rocksteady, Early Reggae, Ska-Jazz, Trad.Ska (lossy и lo ..");
            AddCategoryMapping(1766, TorznabCatType.Audio, " |- Punky-Reggae, Rocksteady-Punk, Ska Revival (lossy)");
            AddCategoryMapping(1767, TorznabCatType.Audio, " | - 3rd Wave Ska (lossy)");
            AddCategoryMapping(1769, TorznabCatType.Audio, " | - Ska-Punk, Ska-Core (lossy)");
            AddCategoryMapping(1765, TorznabCatType.Audio, " |- Reggae (lossy)");
            AddCategoryMapping(1771, TorznabCatType.Audio, " |- Dub (lossy)");
            AddCategoryMapping(1770, TorznabCatType.Audio, " |- Dancehall, Raggamuffin (lossy)");
            AddCategoryMapping(1768, TorznabCatType.Audio, " |- Reggae, Dancehall, Dub (lossless)");
            AddCategoryMapping(1774, TorznabCatType.Audio, " | - Ska Ska-Punk, Ska-Jazz (lossless)");
            AddCategoryMapping(1772, TorznabCatType.Audio, " | - Domestic reggae, dub (lossy and lossless)");
            AddCategoryMapping(1773, TorznabCatType.Audio, " | - Patriotic ska music (lossy and lossless)");
            AddCategoryMapping(2233, TorznabCatType.Audio, " | - Reggae, Ska, Dub (компиляции) (lossy и lossless)");
            AddCategoryMapping(2232, TorznabCatType.Audio, " | - Reggae, Ska, Dub (own digitization)");
            AddCategoryMapping(1775, TorznabCatType.Audio, " | - Reggae, Ska, Dub (Видео)");
            AddCategoryMapping(1777, TorznabCatType.Audio, " | - Reggae, Ska, Dub (DVD и HD Video)");
            AddCategoryMapping(416, TorznabCatType.Audio, "Soundtracks and Karaoke");
            AddCategoryMapping(782, TorznabCatType.Audio, " | - Karaoke (Audio)");
            AddCategoryMapping(2377, TorznabCatType.Audio, " | - Karaoke (Video)");
            AddCategoryMapping(468, TorznabCatType.Audio, " |- Минусовки (lossy и lossless)");
            AddCategoryMapping(1625, TorznabCatType.Audio, " | - Soundtracks (own digitization)");
            AddCategoryMapping(691, TorznabCatType.Audio, " | - Soundtracks for domestic films (lossless)");
            AddCategoryMapping(469, TorznabCatType.Audio, " | - Soundtracks for domestic films (lossy)");
            AddCategoryMapping(786, TorznabCatType.Audio, " | - Soundtracks for foreign films (lossless)");
            AddCategoryMapping(785, TorznabCatType.Audio, " | - Soundtracks for foreign films (lossy)");
            AddCategoryMapping(796, TorznabCatType.Audio, " | - Informal soundtracks for films and TV series (lossy)");
            AddCategoryMapping(784, TorznabCatType.Audio, " | - Soundtracks for games (lossless)");
            AddCategoryMapping(783, TorznabCatType.Audio, " | - Soundtracks for games (lossy)");
            AddCategoryMapping(2331, TorznabCatType.Audio, " | - Informal soundtracks for games (lossy)");
            AddCategoryMapping(2431, TorznabCatType.Audio, " | - The arrangements of music from the game (lossy and lossless)");
            AddCategoryMapping(1397, TorznabCatType.Audio, " | - Hi-Res stereo and multi-channel music (Soundtracks)");
            AddCategoryMapping(1215, TorznabCatType.Audio, "Chanson, Author and military songs");
            AddCategoryMapping(1220, TorznabCatType.Audio, " | - The domestic chanson (lossless)");
            AddCategoryMapping(1221, TorznabCatType.Audio, " | - The domestic chanson (lossy)");
            AddCategoryMapping(1334, TorznabCatType.Audio, " | - Compilations domestic chanson (lossy)");
            AddCategoryMapping(1216, TorznabCatType.Audio, " | - War Song (lossless)");
            AddCategoryMapping(1223, TorznabCatType.Audio, " | - War Song (lossy)");
            AddCategoryMapping(1224, TorznabCatType.Audio, " | - Chanson (lossless)");
            AddCategoryMapping(1225, TorznabCatType.Audio, " | - Chanson (lossy)");
            AddCategoryMapping(1226, TorznabCatType.Audio, " |- Менестрели и ролевики (lossy и lossless)");
            AddCategoryMapping(1217, TorznabCatType.Audio, " | - In-house digitizing (Chanson, and Bards) lossles ..");
            AddCategoryMapping(1227, TorznabCatType.Audio, " | - Video (Chanson, and Bards)");
            AddCategoryMapping(1228, TorznabCatType.Audio, " | - DVD Video (Chanson, and Bards)");
            AddCategoryMapping(413, TorznabCatType.Audio, "Music of other genres");
            AddCategoryMapping(974, TorznabCatType.Audio, " | - In-house digitizing (Music from other genres)");
            AddCategoryMapping(463, TorznabCatType.Audio, " | - Patriotic music of other genres (lossy)");
            AddCategoryMapping(464, TorznabCatType.Audio, " | - Patriotic music of other genres (lossless)");
            AddCategoryMapping(466, TorznabCatType.Audio, " | - International music of other genres (lossy)");
            AddCategoryMapping(465, TorznabCatType.Audio, " | - International music of other genres (lossless)");
            AddCategoryMapping(2018, TorznabCatType.Audio, " | - Music for ballroom dancing (lossy and lossless)");
            AddCategoryMapping(1396, TorznabCatType.Audio, " | - Orthodox chants (lossy)");
            AddCategoryMapping(1395, TorznabCatType.Audio, " | - Orthodox chants (lossless)");
            AddCategoryMapping(1351, TorznabCatType.Audio, " | - A collection of songs for children (lossy and lossless)");
            AddCategoryMapping(475, TorznabCatType.Audio, " | - Video (Music from other genres)");
            AddCategoryMapping(988, TorznabCatType.Audio, " | - DVD Video (Music from other genres)");
            AddCategoryMapping(880, TorznabCatType.Audio, " | - The Musical (lossy and lossless)");
            AddCategoryMapping(655, TorznabCatType.Audio, " | - The Musical (Video and DVD Video)");
            AddCategoryMapping(965, TorznabCatType.Audio, " | - Informal and vnezhanrovye collections (lossy)");
            AddCategoryMapping(919, TorznabCatType.Audio, "Sheet Music literature");
            AddCategoryMapping(944, TorznabCatType.Audio, " | - Academic Music (Notes and Media CD)");
            AddCategoryMapping(980, TorznabCatType.Audio, " | - Other destinations (notes, tablature)");
            AddCategoryMapping(946, TorznabCatType.Audio, " | - Tutorials and Schools");
            AddCategoryMapping(977, TorznabCatType.Audio, " | - Songbooks (Songbooks)");
            AddCategoryMapping(2074, TorznabCatType.Audio, " | - Music Literature and Theory");
            AddCategoryMapping(2349, TorznabCatType.Audio, " | - Music Magazines");

            // Популярная музыка
            AddCategoryMapping(2495, TorznabCatType.Audio, "Domestic Pop");
            AddCategoryMapping(424, TorznabCatType.Audio, " | - Patriotic Pop (lossy)");
            AddCategoryMapping(1361, TorznabCatType.Audio, " | - Patriotic Pop music (collections) (lossy)");
            AddCategoryMapping(425, TorznabCatType.Audio, " | - Patriotic Pop (lossless)");
            AddCategoryMapping(1635, TorznabCatType.Audio, " | - Soviet pop music, retro (lossy)");
            AddCategoryMapping(1634, TorznabCatType.Audio, " | - Soviet pop music, retro (lossless)");
            AddCategoryMapping(2497, TorznabCatType.Audio, "Foreign pop music");
            AddCategoryMapping(428, TorznabCatType.Audio, " | - Foreign pop music (lossy)");
            AddCategoryMapping(1362, TorznabCatType.Audio, " | - Foreign pop music (collections) (lossy)");
            AddCategoryMapping(429, TorznabCatType.Audio, " | - International Pop (lossless)");
            AddCategoryMapping(1219, TorznabCatType.Audio, " | - Foreign chanson (lossy)");
            AddCategoryMapping(1452, TorznabCatType.Audio, " | - Foreign chanson (lossless)");
            AddCategoryMapping(1331, TorznabCatType.Audio, " | - East Asian pop music (lossy)");
            AddCategoryMapping(1330, TorznabCatType.Audio, " | - East Asian Pop (lossless)");
            AddCategoryMapping(2499, TorznabCatType.Audio, "Eurodance, Disco, Hi-NRG");
            AddCategoryMapping(2503, TorznabCatType.Audio, " |- Eurodance, Euro-House, Technopop (lossy)");
            AddCategoryMapping(2504, TorznabCatType.Audio, " |- Eurodance, Euro-House, Technopop (сборники) (lossy)");
            AddCategoryMapping(2502, TorznabCatType.Audio, " |- Eurodance, Euro-House, Technopop (lossless)");
            AddCategoryMapping(2501, TorznabCatType.Audio, " |- Disco, Italo-Disco, Euro-Disco, Hi-NRG (lossy)");
            AddCategoryMapping(2505, TorznabCatType.Audio, " | - Disco, Italo-Disco, Euro-Disco, Hi-NRG (сборники) (lossy ..");
            AddCategoryMapping(2500, TorznabCatType.Audio, " |- Disco, Italo-Disco, Euro-Disco, Hi-NRG (lossless)");
            AddCategoryMapping(2507, TorznabCatType.Audio, "Видео, DVD Video, HD Video (поп-музыка)");
            AddCategoryMapping(1121, TorznabCatType.Audio, " | - Patriotic Pop (Video)");
            AddCategoryMapping(1122, TorznabCatType.Audio, " | - Patriotic Pop (DVD Video)");
            AddCategoryMapping(2510, TorznabCatType.Audio, " | - Soviet pop music, retro (video)");
            AddCategoryMapping(2509, TorznabCatType.Audio, " | - Soviet pop music, retro (DVD Video)");
            AddCategoryMapping(431, TorznabCatType.Audio, " | - Foreign pop music (Video)");
            AddCategoryMapping(986, TorznabCatType.Audio, " | - Foreign pop music (DVD Video)");
            AddCategoryMapping(2532, TorznabCatType.Audio, " |- Eurodance, Disco (видео)");
            AddCategoryMapping(2531, TorznabCatType.Audio, " |- Eurodance, Disco (DVD Video)");
            AddCategoryMapping(2378, TorznabCatType.Audio, " | - East Asian pop music (Video)");
            AddCategoryMapping(2379, TorznabCatType.Audio, " | - East Asian pop music (DVD Video)");
            AddCategoryMapping(2383, TorznabCatType.Audio, " | - Foreign chanson (Video)");
            AddCategoryMapping(2384, TorznabCatType.Audio, " | - Foreign chanson (DVD Video)");
            AddCategoryMapping(2088, TorznabCatType.Audio, " | - Patriotic Pop (National concerts, video dock.) ..");
            AddCategoryMapping(2089, TorznabCatType.Audio, " | - Foreign pop music (National concerts, video dock.) (Bu ..");
            AddCategoryMapping(2426, TorznabCatType.Audio, " | - Patriotic Pop Music, Chanson, Eurodance, Disco (HD V ..");
            AddCategoryMapping(2508, TorznabCatType.Audio, " | - International Pop Music, Chanson, Eurodance, Disco (HD Vide ..");
            AddCategoryMapping(2512, TorznabCatType.Audio, "The multi-channel music and own digitization (pop music)");
            AddCategoryMapping(1444, TorznabCatType.Audio, " | - Foreign pop music (own digitization)");
            AddCategoryMapping(1785, TorznabCatType.Audio, " | - Eastern pop music (own digitization)");
            AddCategoryMapping(239, TorznabCatType.Audio, " | - Patriotic Pop (own digitization)");
            AddCategoryMapping(450, TorznabCatType.Audio, " | - Instrumental Pop (own digitization)");
            AddCategoryMapping(1163, TorznabCatType.Audio, " | - Multi-channel music (pop music)");
            AddCategoryMapping(1885, TorznabCatType.Audio, " |- Hi-Res stereo (Поп-музыка)");

            // Джазовая и Блюзовая музыка
            AddCategoryMapping(2267, TorznabCatType.Audio, "foreign jazz");
            AddCategoryMapping(2277, TorznabCatType.Audio, " |- Early Jazz, Swing, Gypsy (lossless)");
            AddCategoryMapping(2278, TorznabCatType.Audio, " |- Bop (lossless)");
            AddCategoryMapping(2279, TorznabCatType.Audio, " |- Mainstream Jazz, Cool (lossless)");
            AddCategoryMapping(2280, TorznabCatType.Audio, " |- Jazz Fusion (lossless)");
            AddCategoryMapping(2281, TorznabCatType.Audio, " |- World Fusion, Ethnic Jazz (lossless)");
            AddCategoryMapping(2282, TorznabCatType.Audio, " |- Avant-Garde Jazz, Free Improvisation (lossless)");
            AddCategoryMapping(2353, TorznabCatType.Audio, " |- Modern Creative, Third Stream (lossless)");
            AddCategoryMapping(2284, TorznabCatType.Audio, " |- Smooth, Jazz-Pop (lossless)");
            AddCategoryMapping(2285, TorznabCatType.Audio, " |- Vocal Jazz (lossless)");
            AddCategoryMapping(2283, TorznabCatType.Audio, " |- Funk, Soul, R&B (lossless)");
            AddCategoryMapping(2286, TorznabCatType.Audio, " | - Compilations foreign jazz (lossless)");
            AddCategoryMapping(2287, TorznabCatType.Audio, " | - Foreign jazz (lossy)");
            AddCategoryMapping(2268, TorznabCatType.Audio, "foreign blues");
            AddCategoryMapping(2293, TorznabCatType.Audio, " |- Blues (Texas, Chicago, Modern and Others) (lossless)");
            AddCategoryMapping(2292, TorznabCatType.Audio, " |- Blues-rock (lossless)");
            AddCategoryMapping(2290, TorznabCatType.Audio, " |- Roots, Pre-War Blues, Early R&B, Gospel (lossless)");
            AddCategoryMapping(2289, TorznabCatType.Audio, " | - Foreign blues (collections; Tribute VA) (lossless)");
            AddCategoryMapping(2288, TorznabCatType.Audio, " | - Foreign blues (lossy)");
            AddCategoryMapping(2269, TorznabCatType.Audio, "Domestic jazz and blues");
            AddCategoryMapping(2297, TorznabCatType.Audio, " | - Domestic Jazz (lossless)");
            AddCategoryMapping(2295, TorznabCatType.Audio, " | - Domestic jazz (lossy)");
            AddCategoryMapping(2296, TorznabCatType.Audio, " | - Domestic Blues (lossless)");
            AddCategoryMapping(2298, TorznabCatType.Audio, " | - Domestic Blues (lossy)");
            AddCategoryMapping(2270, TorznabCatType.Audio, "The multi-channel music and own digitization (Jazz and Blues)");
            AddCategoryMapping(2303, TorznabCatType.Audio, " | - Multi-channel music (Jazz and Blues)");
            AddCategoryMapping(2302, TorznabCatType.Audio, " | - Hi-Res stereo (Jazz and Blues)");
            AddCategoryMapping(2301, TorznabCatType.Audio, " | - In-house digitizing (Jazz and Blues)");
            AddCategoryMapping(2271, TorznabCatType.Audio, "Video, DVD Video, HD Video (Jazz and Blues)");
            AddCategoryMapping(2305, TorznabCatType.Audio, " | - Jazz and Blues (Video)");
            AddCategoryMapping(2304, TorznabCatType.Audio, " | - Jazz and Blues (DVD Video)");
            AddCategoryMapping(2306, TorznabCatType.Audio, " | - Jazz and Blues (HD Video)");

            // Рок-музыка
            AddCategoryMapping(1698, TorznabCatType.Audio, "foreign Rock");
            AddCategoryMapping(1702, TorznabCatType.Audio, " |- Classic Rock & Hard Rock (lossless)");
            AddCategoryMapping(1703, TorznabCatType.Audio, " |- Classic Rock & Hard Rock (lossy)");
            AddCategoryMapping(1704, TorznabCatType.Audio, " |- Progressive & Art-Rock (lossless)");
            AddCategoryMapping(1705, TorznabCatType.Audio, " |- Progressive & Art-Rock (lossy)");
            AddCategoryMapping(1706, TorznabCatType.Audio, " |- Folk-Rock (lossless)");
            AddCategoryMapping(1707, TorznabCatType.Audio, " |- Folk-Rock (lossy)");
            AddCategoryMapping(2329, TorznabCatType.Audio, " |- AOR (Melodic Hard Rock, Arena rock) (lossless)");
            AddCategoryMapping(2330, TorznabCatType.Audio, " |- AOR (Melodic Hard Rock, Arena rock) (lossy)");
            AddCategoryMapping(1708, TorznabCatType.Audio, " |- Pop-Rock & Soft Rock (lossless)");
            AddCategoryMapping(1709, TorznabCatType.Audio, " |- Pop-Rock & Soft Rock (lossy)");
            AddCategoryMapping(1710, TorznabCatType.Audio, " |- Instrumental Guitar Rock (lossless)");
            AddCategoryMapping(1711, TorznabCatType.Audio, " |- Instrumental Guitar Rock (lossy)");
            AddCategoryMapping(1712, TorznabCatType.Audio, " |- Rockabilly, Psychobilly, Rock'n'Roll (lossless)");
            AddCategoryMapping(1713, TorznabCatType.Audio, " |- Rockabilly, Psychobilly, Rock'n'Roll (lossy)");
            AddCategoryMapping(731, TorznabCatType.Audio, " | - Compilations foreign rock (lossless)");
            AddCategoryMapping(1799, TorznabCatType.Audio, " | - Compilations foreign rock (lossy)");
            AddCategoryMapping(1714, TorznabCatType.Audio, " | - East Asian Rock (lossless)");
            AddCategoryMapping(1715, TorznabCatType.Audio, " | - East Asian rock (lossy)");
            AddCategoryMapping(1716, TorznabCatType.Audio, "foreign Metal");
            AddCategoryMapping(1796, TorznabCatType.Audio, " |- Avant-garde, Experimental Metal (lossless)");
            AddCategoryMapping(1797, TorznabCatType.Audio, " |- Avant-garde, Experimental Metal (lossy)");
            AddCategoryMapping(1719, TorznabCatType.Audio, " |- Black (lossless)");
            AddCategoryMapping(1778, TorznabCatType.Audio, " |- Black (lossy)");
            AddCategoryMapping(1779, TorznabCatType.Audio, " |- Death, Doom (lossless)");
            AddCategoryMapping(1780, TorznabCatType.Audio, " |- Death, Doom (lossy)");
            AddCategoryMapping(1720, TorznabCatType.Audio, " |- Folk, Pagan, Viking (lossless)");
            AddCategoryMapping(798, TorznabCatType.Audio, " |- Folk, Pagan, Viking (lossy)");
            AddCategoryMapping(1724, TorznabCatType.Audio, " |- Gothic Metal (lossless)");
            AddCategoryMapping(1725, TorznabCatType.Audio, " |- Gothic Metal (lossy)");
            AddCategoryMapping(1730, TorznabCatType.Audio, " |- Grind, Brutal Death (lossless)");
            AddCategoryMapping(1731, TorznabCatType.Audio, " |- Grind, Brutal Death (lossy)");
            AddCategoryMapping(1726, TorznabCatType.Audio, " |- Heavy, Power, Progressive (lossless)");
            AddCategoryMapping(1727, TorznabCatType.Audio, " |- Heavy, Power, Progressive (lossy)");
            AddCategoryMapping(1815, TorznabCatType.Audio, " |- Sludge, Stoner, Post-Metal (lossless)");
            AddCategoryMapping(1816, TorznabCatType.Audio, " |- Sludge, Stoner, Post-Metal (lossy)");
            AddCategoryMapping(1728, TorznabCatType.Audio, " |- Thrash, Speed (lossless)");
            AddCategoryMapping(1729, TorznabCatType.Audio, " |- Thrash, Speed (lossy)");
            AddCategoryMapping(2230, TorznabCatType.Audio, " | - Compilations (lossless)");
            AddCategoryMapping(2231, TorznabCatType.Audio, " | - Compilations (lossy)");
            AddCategoryMapping(1732, TorznabCatType.Audio, "Foreign Alternative, Punk, Independent");
            AddCategoryMapping(1736, TorznabCatType.Audio, " |- Alternative & Nu-metal (lossless)");
            AddCategoryMapping(1737, TorznabCatType.Audio, " |- Alternative & Nu-metal (lossy)");
            AddCategoryMapping(1738, TorznabCatType.Audio, " |- Punk (lossless)");
            AddCategoryMapping(1739, TorznabCatType.Audio, " |- Punk (lossy)");
            AddCategoryMapping(1740, TorznabCatType.Audio, " |- Hardcore (lossless)");
            AddCategoryMapping(1741, TorznabCatType.Audio, " |- Hardcore (lossy)");
            AddCategoryMapping(1742, TorznabCatType.Audio, " |- Indie, Post-Rock & Post-Punk (lossless)");
            AddCategoryMapping(1743, TorznabCatType.Audio, " |- Indie, Post-Rock & Post-Punk (lossy)");
            AddCategoryMapping(1744, TorznabCatType.Audio, " |- Industrial & Post-industrial (lossless)");
            AddCategoryMapping(1745, TorznabCatType.Audio, " |- Industrial & Post-industrial (lossy)");
            AddCategoryMapping(1746, TorznabCatType.Audio, " |- Emocore, Post-hardcore, Metalcore (lossless)");
            AddCategoryMapping(1747, TorznabCatType.Audio, " |- Emocore, Post-hardcore, Metalcore (lossy)");
            AddCategoryMapping(1748, TorznabCatType.Audio, " |- Gothic Rock & Dark Folk (lossless)");
            AddCategoryMapping(1749, TorznabCatType.Audio, " |- Gothic Rock & Dark Folk (lossy)");
            AddCategoryMapping(2175, TorznabCatType.Audio, " |- Avant-garde, Experimental Rock (lossless)");
            AddCategoryMapping(2174, TorznabCatType.Audio, " |- Avant-garde, Experimental Rock (lossy)");
            AddCategoryMapping(722, TorznabCatType.Audio, "Domestic Rock");
            AddCategoryMapping(737, TorznabCatType.Audio, " | - Rock, Punk, Alternative (lossless)");
            AddCategoryMapping(738, TorznabCatType.Audio, " | - Rock, Punk, Alternative (lossy)");
            AddCategoryMapping(739, TorznabCatType.Audio, " |- Металл (lossless)");
            AddCategoryMapping(740, TorznabCatType.Audio, " |- Металл (lossy)");
            AddCategoryMapping(951, TorznabCatType.Audio, " | - Rock in the languages &#8203;&#8203;of xUSSR (lossless)");
            AddCategoryMapping(952, TorznabCatType.Audio, " | - Rock in the languages &#8203;&#8203;of xUSSR (lossy)");
            AddCategoryMapping(1752, TorznabCatType.Audio, "The multi-channel music and own digitization (Rock)");
            AddCategoryMapping(1756, TorznabCatType.Audio, " | - Foreign rock (own digitization)");
            AddCategoryMapping(1758, TorznabCatType.Audio, " | - Domestic Rock (own digitization)");
            AddCategoryMapping(1757, TorznabCatType.Audio, " | - Multi-channel music (rock)");
            AddCategoryMapping(1755, TorznabCatType.Audio, " |- Hi-Res stereo (рок)");
            AddCategoryMapping(453, TorznabCatType.Audio, " | - Conversions Quadraphonic (multichannel music)");
            AddCategoryMapping(1170, TorznabCatType.Audio, " | - Conversions SACD (multi-channel music)");
            AddCategoryMapping(1759, TorznabCatType.Audio, " | - Conversions from the Blu-Ray (multichannel music)");
            AddCategoryMapping(1852, TorznabCatType.Audio, " |- Апмиксы-Upmixes/Даунмиксы-Downmix (многоканальная и Hi-R..");
            AddCategoryMapping(1781, TorznabCatType.Audio, "Видео, DVD Video, HD Video (Рок-музыка)");
            AddCategoryMapping(1782, TorznabCatType.Audio, " |- Rock (Видео)");
            AddCategoryMapping(1783, TorznabCatType.Audio, " |- Rock (DVD Video)");
            AddCategoryMapping(2261, TorznabCatType.Audio, " | - Rock (Unofficial DVD Video)");
            AddCategoryMapping(1787, TorznabCatType.Audio, " |- Metal (Видео)");
            AddCategoryMapping(1788, TorznabCatType.Audio, " |- Metal (DVD Video)");
            AddCategoryMapping(2262, TorznabCatType.Audio, " | - Metal (Unofficial DVD Video)");
            AddCategoryMapping(1789, TorznabCatType.Audio, " |- Alternative, Punk, Independent (Видео)");
            AddCategoryMapping(1790, TorznabCatType.Audio, " |- Alternative, Punk, Independent (DVD Video)");
            AddCategoryMapping(2263, TorznabCatType.Audio, " |- Alternative, Punk, Independent (Неофициальные DVD Video)");
            AddCategoryMapping(1791, TorznabCatType.Audio, " | - Domestic Rock, Punk, Alternative (Video)");
            AddCategoryMapping(1792, TorznabCatType.Audio, " | - Domestic Rock, Punk, Alternative (DVD Video)");
            AddCategoryMapping(1793, TorznabCatType.Audio, " | - Domestic Metal (Video)");
            AddCategoryMapping(1794, TorznabCatType.Audio, " | - Domestic Metal (DVD Video)");
            AddCategoryMapping(2264, TorznabCatType.Audio, " | - Domestic Rock, Punk, Alternative, Metal (Neofitsial ..");
            AddCategoryMapping(1795, TorznabCatType.Audio, " | - Rock (HD Video)");

            // Электронная музыка
            AddCategoryMapping(1821, TorznabCatType.Audio, "Trance, Goa Trance, Psy-Trance, PsyChill, Ambient, Dub");
            AddCategoryMapping(1844, TorznabCatType.Audio, " |- Goa Trance, Psy-Trance (lossless)");
            AddCategoryMapping(1822, TorznabCatType.Audio, " |- Goa Trance, Psy-Trance (lossy)");
            AddCategoryMapping(1894, TorznabCatType.Audio, " |- PsyChill, Ambient, Dub (lossless)");
            AddCategoryMapping(1895, TorznabCatType.Audio, " |- PsyChill, Ambient, Dub (lossy)");
            AddCategoryMapping(460, TorznabCatType.Audio, " |- Goa Trance, Psy-Trance, PsyChill, Ambient, Dub (Live Set..");
            AddCategoryMapping(1818, TorznabCatType.Audio, " |- Trance (lossless)");
            AddCategoryMapping(1819, TorznabCatType.Audio, " |- Trance (lossy)");
            AddCategoryMapping(1847, TorznabCatType.Audio, " |- Trance (Singles, EPs) (lossy)");
            AddCategoryMapping(1824, TorznabCatType.Audio, " |- Trance (Radioshows, Podcasts, Live Sets, Mixes) (lossy)");
            AddCategoryMapping(1807, TorznabCatType.Audio, "House, Techno, Hardcore, Hardstyle, Jumpstyle");
            AddCategoryMapping(1829, TorznabCatType.Audio, " |- Hardcore, Hardstyle, Jumpstyle (lossless)");
            AddCategoryMapping(1830, TorznabCatType.Audio, " |- Hardcore, Hardstyle, Jumpstyle (lossy)");
            AddCategoryMapping(1831, TorznabCatType.Audio, " |- Hardcore, Hardstyle, Jumpstyle (vinyl, web)");
            AddCategoryMapping(1857, TorznabCatType.Audio, " |- House (lossless)");
            AddCategoryMapping(1859, TorznabCatType.Audio, " |- House (Radioshow, Podcast, Liveset, Mixes)");
            AddCategoryMapping(1858, TorznabCatType.Audio, " |- House (lossy)");
            AddCategoryMapping(840, TorznabCatType.Audio, " | - House (Promorelyzы, collections of)");
            AddCategoryMapping(1860, TorznabCatType.Audio, " |- House (Singles, EPs) (lossy)");
            AddCategoryMapping(1825, TorznabCatType.Audio, " |- Techno (lossless)");
            AddCategoryMapping(1826, TorznabCatType.Audio, " |- Techno (lossy)");
            AddCategoryMapping(1827, TorznabCatType.Audio, " |- Techno (Radioshows, Podcasts, Livesets, Mixes)");
            AddCategoryMapping(1828, TorznabCatType.Audio, " |- Techno (Singles, EPs) (lossy)");
            AddCategoryMapping(1808, TorznabCatType.Audio, "Drum & Bass, Jungle, Breakbeat, Dubstep, IDM, Electro");
            AddCategoryMapping(797, TorznabCatType.Audio, " |- Electro, Electro-Freestyle, Nu Electro (lossless)");
            AddCategoryMapping(1805, TorznabCatType.Audio, " |- Electro, Electro-Freestyle, Nu Electro (lossy)");
            AddCategoryMapping(1832, TorznabCatType.Audio, " |- Drum & Bass, Jungle (lossless)");
            AddCategoryMapping(1833, TorznabCatType.Audio, " |- Drum & Bass, Jungle (lossy)");
            AddCategoryMapping(1834, TorznabCatType.Audio, " |- Drum & Bass, Jungle (Radioshows, Podcasts, Livesets, Mix..");
            AddCategoryMapping(1836, TorznabCatType.Audio, " |- Breakbeat (lossless)");
            AddCategoryMapping(1837, TorznabCatType.Audio, " |- Breakbeat (lossy)");
            AddCategoryMapping(1839, TorznabCatType.Audio, " |- Dubstep (lossless)");
            AddCategoryMapping(454, TorznabCatType.Audio, " |- Dubstep (lossy)");
            AddCategoryMapping(1838, TorznabCatType.Audio, " |- Breakbeat, Dubstep (Radioshows, Podcasts, Livesets, Mixe..");
            AddCategoryMapping(1840, TorznabCatType.Audio, " |- IDM (lossless)");
            AddCategoryMapping(1841, TorznabCatType.Audio, " |- IDM (lossy)");
            AddCategoryMapping(2229, TorznabCatType.Audio, " |- IDM Discography & Collections (lossy)");
            AddCategoryMapping(1809, TorznabCatType.Audio, "Chillout, Lounge, Downtempo, Trip-Hop");
            AddCategoryMapping(1861, TorznabCatType.Audio, " |- Chillout, Lounge, Downtempo (lossless)");
            AddCategoryMapping(1862, TorznabCatType.Audio, " |- Chillout, Lounge, Downtempo (lossy)");
            AddCategoryMapping(1947, TorznabCatType.Audio, " |- Nu Jazz, Acid Jazz, Future Jazz (lossless)");
            AddCategoryMapping(1946, TorznabCatType.Audio, " |- Nu Jazz, Acid Jazz, Future Jazz (lossy)");
            AddCategoryMapping(1945, TorznabCatType.Audio, " |- Trip Hop, Abstract Hip-Hop (lossless)");
            AddCategoryMapping(1944, TorznabCatType.Audio, " |- Trip Hop, Abstract Hip-Hop (lossy)");
            AddCategoryMapping(1810, TorznabCatType.Audio, "Traditional Electronic, Ambient, Modern Classical, Electroac..");
            AddCategoryMapping(1864, TorznabCatType.Audio, " |- Traditional Electronic, Ambient (lossless)");
            AddCategoryMapping(1865, TorznabCatType.Audio, " |- Traditional Electronic, Ambient (lossy)");
            AddCategoryMapping(1871, TorznabCatType.Audio, " |- Modern Classical, Electroacoustic (lossless)");
            AddCategoryMapping(1867, TorznabCatType.Audio, " |- Modern Classical, Electroacoustic (lossy)");
            AddCategoryMapping(1869, TorznabCatType.Audio, " |- Experimental (lossless)");
            AddCategoryMapping(1873, TorznabCatType.Audio, " |- Experimental (lossy)");
            AddCategoryMapping(1907, TorznabCatType.Audio, " |- 8-bit, Chiptune (lossy & lossless)");
            AddCategoryMapping(1811, TorznabCatType.Audio, "Industrial, Noise, EBM, Dark Electro, Aggrotech, Synthpop, N..");
            AddCategoryMapping(1868, TorznabCatType.Audio, " | - EBM, Dark Electro, Aggrotech (lossless)");
            AddCategoryMapping(1875, TorznabCatType.Audio, " | - EBM, Dark Electro, Aggrotech (lossy)");
            AddCategoryMapping(1877, TorznabCatType.Audio, " |- Industrial, Noise (lossless)");
            AddCategoryMapping(1878, TorznabCatType.Audio, " |- Industrial, Noise (lossy)");
            AddCategoryMapping(1880, TorznabCatType.Audio, " |- Synthpop, New Wave (lossless)");
            AddCategoryMapping(1881, TorznabCatType.Audio, " |- Synthpop, New Wave (lossy)");
            AddCategoryMapping(1866, TorznabCatType.Audio, " |- Darkwave, Neoclassical, Ethereal, Dungeon Synth (lossles..");
            AddCategoryMapping(406, TorznabCatType.Audio, " |- Darkwave, Neoclassical, Ethereal, Dungeon Synth (lossy)");
            AddCategoryMapping(1842, TorznabCatType.Audio, "Label Packs (lossless)");
            AddCategoryMapping(1648, TorznabCatType.Audio, "Label packs, Scene packs (lossy)");
            AddCategoryMapping(1812, TorznabCatType.Audio, "Электронная музыка (Видео, DVD Video/Audio, HD Video, DTS, S..");
            AddCategoryMapping(1886, TorznabCatType.Audio, " | - Electronic music (Official DVD Video)");
            AddCategoryMapping(1887, TorznabCatType.Audio, " | - Electronic music (Informal amateur DVD Vide ..");
            AddCategoryMapping(1912, TorznabCatType.Audio, " | - Electronic music (Video)");
            AddCategoryMapping(1893, TorznabCatType.Audio, " | - Hi-Res stereo (electronic music)");
            AddCategoryMapping(1890, TorznabCatType.Audio, " | - Multi-channel music (electronic music)");
            AddCategoryMapping(1913, TorznabCatType.Audio, " | - Electronic music (HD Video)");
            AddCategoryMapping(1754, TorznabCatType.Audio, " | - Electronic music (own digitization)");

            // Игры
            AddCategoryMapping(5, TorznabCatType.PCGames, "Games for Windows (download)");
            AddCategoryMapping(635, TorznabCatType.PCGames, " | - Hot New Releases");
            AddCategoryMapping(127, TorznabCatType.PCGames, " | - Arcade");
            AddCategoryMapping(2204, TorznabCatType.PCGames, " | - Puzzle Games");
            AddCategoryMapping(53, TorznabCatType.PCGames, " | - Adventure and quests");
            AddCategoryMapping(1008, TorznabCatType.PCGames, " | - Quest-style \"search objects\"");
            AddCategoryMapping(51, TorznabCatType.PCGames, " | - Strategy");
            AddCategoryMapping(961, TorznabCatType.PCGames, " | - Space and flight simulators");
            AddCategoryMapping(962, TorznabCatType.PCGames, " | - Autos and Racing");
            AddCategoryMapping(2187, TorznabCatType.PCGames, " | - Racing Simulators");
            AddCategoryMapping(54, TorznabCatType.PCGames, " | - Other simulators");
            AddCategoryMapping(55, TorznabCatType.PCGames, " |- Action");
            AddCategoryMapping(2203, TorznabCatType.PCGames, " | - Fighting");
            AddCategoryMapping(52, TorznabCatType.PCGames, " |- RPG");
            AddCategoryMapping(900, TorznabCatType.PCGames, " | - Anime games");
            AddCategoryMapping(246, TorznabCatType.PCGames, " | - Erotic Games");
            AddCategoryMapping(278, TorznabCatType.PCGames, " | - Chess");
            AddCategoryMapping(128, TorznabCatType.PCGames, " | - For the little ones");
            AddCategoryMapping(637, TorznabCatType.PCGames, "Old Games");
            AddCategoryMapping(642, TorznabCatType.PCGames, " | - Arcade (Old Games)");
            AddCategoryMapping(2385, TorznabCatType.PCGames, " | - Puzzle games (old games)");
            AddCategoryMapping(643, TorznabCatType.PCGames, " | - Adventure and quests (Old Games)");
            AddCategoryMapping(644, TorznabCatType.PCGames, " | - Strategies (Old Games)");
            AddCategoryMapping(2226, TorznabCatType.PCGames, " | - Space and flight simulators (Old Games)");
            AddCategoryMapping(2227, TorznabCatType.PCGames, " | - Autos and Racing (Old Games)");
            AddCategoryMapping(2225, TorznabCatType.PCGames, " | - Racing Simulators (Old Games)");
            AddCategoryMapping(645, TorznabCatType.PCGames, " | - Other simulators (Old Games)");
            AddCategoryMapping(646, TorznabCatType.PCGames, " | - Action (Old Games)");
            AddCategoryMapping(647, TorznabCatType.PCGames, " | - RPG (Old Games)");
            AddCategoryMapping(649, TorznabCatType.PCGames, " | - Erotic Games (Old Games)");
            AddCategoryMapping(650, TorznabCatType.PCGames, " | - For the little ones (Old Games)");
            AddCategoryMapping(1098, TorznabCatType.PCGames, " | - Game Collection (Old Games)");
            AddCategoryMapping(2228, TorznabCatType.PCGames, " | - IBM PC incompatible (Old Games)");
            AddCategoryMapping(2115, TorznabCatType.PCGames, "Online Games");
            AddCategoryMapping(2117, TorznabCatType.PCGames, " |- World of Warcraft");
            AddCategoryMapping(2155, TorznabCatType.PCGames, " |- Lineage II");
            AddCategoryMapping(2118, TorznabCatType.PCGames, " | - MMO (Official)");
            AddCategoryMapping(2119, TorznabCatType.PCGames, " | - MMO (Neoficialʹnye)");
            AddCategoryMapping(2489, TorznabCatType.PCGames, " | - Multiplayer games");
            AddCategoryMapping(2142, TorznabCatType.PCGames, "Microsoft Flight Simulator add-ons, and for him");
            AddCategoryMapping(2143, TorznabCatType.PCGames, " | - Scripts, meshes and airports [FS2004]");
            AddCategoryMapping(2060, TorznabCatType.PCGames, " |- Сценарии (FSX-P3D)");
            AddCategoryMapping(2145, TorznabCatType.PCGames, " | - Aircraft [FS2004]");
            AddCategoryMapping(2012, TorznabCatType.PCGames, " | - Planes, helicopters (FSX-P3D)");
            AddCategoryMapping(2146, TorznabCatType.PCGames, " | - Mission, traffic sounds, packs and tools");
            AddCategoryMapping(139, TorznabCatType.PCGames, "Others for Windows-based games");
            AddCategoryMapping(2478, TorznabCatType.PCGames, " | - Official patches");
            AddCategoryMapping(2479, TorznabCatType.PCGames, " | - Official Fashion, plug-ins, add-ons");
            AddCategoryMapping(2480, TorznabCatType.PCGames, " | - Informal fashion, plugins, add-ons");
            AddCategoryMapping(2481, TorznabCatType.PCGames, " | - Fun");
            AddCategoryMapping(761, TorznabCatType.PCGames, " | - Editors, emulators and other gaming utility");
            AddCategoryMapping(2482, TorznabCatType.PCGames, " |- NoCD / NoDVD");
            AddCategoryMapping(2533, TorznabCatType.PCGames, " | - Conservation games");
            AddCategoryMapping(2483, TorznabCatType.PCGames, " | - Cheat program and trainers");
            AddCategoryMapping(2484, TorznabCatType.PCGames, " | - Guides and passing");
            AddCategoryMapping(2485, TorznabCatType.PCGames, " | - The bonus discs for games");
            AddCategoryMapping(240, TorznabCatType.PCGames, "video Game");
            AddCategoryMapping(2415, TorznabCatType.PCGames, " | - Walkthroughs");
            AddCategoryMapping(2067, TorznabCatType.PCGames, " |- Lineage II Movies");
            AddCategoryMapping(2147, TorznabCatType.PCGames, " |- World of Warcraft Movies");
            AddCategoryMapping(960, TorznabCatType.PCGames, " |- Counter Strike Movies");
            AddCategoryMapping(548, TorznabCatType.Console, "Games for consoles");
            AddCategoryMapping(129, TorznabCatType.Console, " | - Portable and Console (Games)");
            AddCategoryMapping(908, TorznabCatType.ConsolePS3, " |- PS");
            AddCategoryMapping(357, TorznabCatType.ConsolePS3, " |- PS2");
            AddCategoryMapping(886, TorznabCatType.ConsolePS3, " |- PS3");
            AddCategoryMapping(1352, TorznabCatType.ConsolePSP, " |- PSP");
            AddCategoryMapping(1116, TorznabCatType.ConsolePSP, " | - PS1 games for PSP");
            AddCategoryMapping(973, TorznabCatType.ConsolePSVita, " |- PSVITA");
            AddCategoryMapping(887, TorznabCatType.ConsoleXbox, " | - Original Xbox");
            AddCategoryMapping(510, TorznabCatType.ConsoleXbox360, " |- Xbox 360");
            AddCategoryMapping(773, TorznabCatType.ConsoleWii, " |- Wii");
            AddCategoryMapping(774, TorznabCatType.ConsoleNDS, " |- NDS");
            AddCategoryMapping(968, TorznabCatType.ConsoleOther, " |- Dreamcast");
            AddCategoryMapping(546, TorznabCatType.ConsoleOther, " | - Games for the DVD player");
            AddCategoryMapping(2185, TorznabCatType.Console, "Video consoles");
            AddCategoryMapping(2487, TorznabCatType.ConsolePSVita, " | - Video for PSVita");
            AddCategoryMapping(2182, TorznabCatType.ConsolePSP, " | - Movies for PSP");
            AddCategoryMapping(2181, TorznabCatType.ConsolePSP, " | - For PSP TV Shows");
            AddCategoryMapping(2180, TorznabCatType.ConsolePSP, " | - Cartoons for PSP");
            AddCategoryMapping(2179, TorznabCatType.ConsolePSP, " | - Drama for PSP");
            AddCategoryMapping(2186, TorznabCatType.ConsolePSP, " | - Anime for PSP");
            AddCategoryMapping(700, TorznabCatType.ConsolePSP, " | - Video to PSP");
            AddCategoryMapping(1926, TorznabCatType.ConsolePS3, " | - Videos for the PS3 and other consoles");
            AddCategoryMapping(899, TorznabCatType.PCGames, "Games for Linux");
            AddCategoryMapping(1992, TorznabCatType.PCGames, " | - Native games for Linux");
            AddCategoryMapping(2059, TorznabCatType.PCGames, " | - Game Ported to Linux");

            // Программы и Дизайн
            AddCategoryMapping(1012, TorznabCatType.PC0day, "Operating systems from Microsoft");
            AddCategoryMapping(1019, TorznabCatType.PC0day, " | - Desktop operating system from Microsoft (released prior to Windows XP)");
            AddCategoryMapping(2153, TorznabCatType.PC0day, " | - Desktop operating system from Microsoft (since Windows XP)");
            AddCategoryMapping(1021, TorznabCatType.PC0day, " | - Server operating system from Microsoft");
            AddCategoryMapping(1025, TorznabCatType.PC0day, " | - Other (Operating Systems from Microsoft)");
            AddCategoryMapping(1376, TorznabCatType.PC0day, "Linux, Unix and other operating systems");
            AddCategoryMapping(1379, TorznabCatType.PC0day, " | - Operating Systems (Linux, Unix)");
            AddCategoryMapping(1381, TorznabCatType.PC0day, " | - Software (Linux, Unix)");
            AddCategoryMapping(1473, TorznabCatType.PC0day, " | - Other operating systems and software for them");
            AddCategoryMapping(1195, TorznabCatType.PC0day, "Test drives to adjust the audio / video equipment");
            AddCategoryMapping(1013, TorznabCatType.PC0day, "System programs");
            AddCategoryMapping(1028, TorznabCatType.PC0day, " | - Work with hard drive");
            AddCategoryMapping(1029, TorznabCatType.PC0day, " | - Backup");
            AddCategoryMapping(1030, TorznabCatType.PC0day, " | - Archivers and File Managers");
            AddCategoryMapping(1031, TorznabCatType.PC0day, " | - Software to configure and optimize the operating system");
            AddCategoryMapping(1032, TorznabCatType.PC0day, " | - Service computer service");
            AddCategoryMapping(1033, TorznabCatType.PC0day, " | - Work with data carriers");
            AddCategoryMapping(1034, TorznabCatType.PC0day, " | - Information and Diagnostics");
            AddCategoryMapping(1066, TorznabCatType.PC0day, " | - Software for Internet and networks");
            AddCategoryMapping(1035, TorznabCatType.PC0day, " | - Software to protect your computer (antivirus software, firewalls)");
            AddCategoryMapping(1038, TorznabCatType.PC0day, " | - Anti-spyware and anti-trojan");
            AddCategoryMapping(1039, TorznabCatType.PC0day, " | - Software to protect information");
            AddCategoryMapping(1536, TorznabCatType.PC0day, " | - Drivers and Firmware");
            AddCategoryMapping(1051, TorznabCatType.PC0day, " | - The original disks to computers and accessories");
            AddCategoryMapping(1040, TorznabCatType.PC0day, " | - Server software for Windows");
            AddCategoryMapping(1041, TorznabCatType.PC0day, " | - Change the Windows interface");
            AddCategoryMapping(1636, TorznabCatType.PC0day, " | - Screensavers");
            AddCategoryMapping(1042, TorznabCatType.PC0day, " | - Other (System programs on Windows)");
            AddCategoryMapping(1014, TorznabCatType.PC0day, "Systems for business, office, research and project work");
            AddCategoryMapping(1060, TorznabCatType.PC0day, " | - Everything for the home: dressmaking, sewing, cooking");
            AddCategoryMapping(1061, TorznabCatType.PC0day, " | - Office Systems");
            AddCategoryMapping(1062, TorznabCatType.PC0day, " | - Business Systems");
            AddCategoryMapping(1067, TorznabCatType.PC0day, " | - Recognition of text, sound and speech synthesis");
            AddCategoryMapping(1086, TorznabCatType.PC0day, " | - Work with PDF and DjVu");
            AddCategoryMapping(1068, TorznabCatType.PC0day, " | - Dictionaries, translators");
            AddCategoryMapping(1063, TorznabCatType.PC0day, " | - System for scientific work");
            AddCategoryMapping(1087, TorznabCatType.PC0day, " | - CAD (general and engineering)");
            AddCategoryMapping(1192, TorznabCatType.PC0day, " | - CAD (electronics, automation, GAP)");
            AddCategoryMapping(1088, TorznabCatType.PC0day, " | - Software for architects and builders");
            AddCategoryMapping(1193, TorznabCatType.PC0day, " | - Library and projects for architects and designers inter ..");
            AddCategoryMapping(1071, TorznabCatType.PC0day, " | - Other reference systems");
            AddCategoryMapping(1073, TorznabCatType.PC0day, " | - Miscellaneous (business systems, office, research and design ..");
            AddCategoryMapping(1052, TorznabCatType.PC0day, "Web Development and Programming");
            AddCategoryMapping(1053, TorznabCatType.PC0day, " | - WYSIWYG editors for web diz");
            AddCategoryMapping(1054, TorznabCatType.PC0day, " | - Text editors Illuminated");
            AddCategoryMapping(1055, TorznabCatType.PC0day, " | - Programming environments, compilers and support, etc. ..");
            AddCategoryMapping(1056, TorznabCatType.PC0day, " | - Components for programming environments");
            AddCategoryMapping(2077, TorznabCatType.PC0day, " | - Database Management Systems");
            AddCategoryMapping(1057, TorznabCatType.PC0day, " | - Scripts and engines sites, CMS and extensions to it");
            AddCategoryMapping(1018, TorznabCatType.PC0day, " | - Templates for websites and CMS");
            AddCategoryMapping(1058, TorznabCatType.PC0day, " | - Miscellaneous (Web Development and Programming)");
            AddCategoryMapping(1016, TorznabCatType.PC0day, "Programs to work with multimedia and 3D");
            AddCategoryMapping(1079, TorznabCatType.PC0day, " | - Software Kits");
            AddCategoryMapping(1080, TorznabCatType.PC0day, " | - Plug-ins for Adobe's programs");
            AddCategoryMapping(1081, TorznabCatType.PC0day, " | - Graphic Editors");
            AddCategoryMapping(1082, TorznabCatType.PC0day, " | - Software for typesetting, printing, and working with fonts");
            AddCategoryMapping(1083, TorznabCatType.PC0day, " | - 3D modeling, rendering and plugins for them");
            AddCategoryMapping(1084, TorznabCatType.PC0day, " | - Animation");
            AddCategoryMapping(1085, TorznabCatType.PC0day, " | - Creating a BD / HD / DVD-Video");
            AddCategoryMapping(1089, TorznabCatType.PC0day, " | - Video Editors");
            AddCategoryMapping(1090, TorznabCatType.PC0day, " | - Video converters Audio");
            AddCategoryMapping(1065, TorznabCatType.PC0day, " | - Audio and video, CD- players and catalogers");
            AddCategoryMapping(1064, TorznabCatType.PC0day, " | - Cataloging and graphics viewers");
            AddCategoryMapping(1092, TorznabCatType.PC0day, " | - Miscellaneous (Programme for multimedia and 3D)");
            AddCategoryMapping(1204, TorznabCatType.PC0day, " | - Virtual Studios, sequencers and audio editor");
            AddCategoryMapping(1027, TorznabCatType.PC0day, " | - Virtual Instruments & Synthesizers");
            AddCategoryMapping(1199, TorznabCatType.PC0day, " | - Plug-ins for sound processing");
            AddCategoryMapping(1091, TorznabCatType.PC0day, " | - Miscellaneous (Programs for working with audio)");
            AddCategoryMapping(828, TorznabCatType.PC0day, "Materials for Multimedia and Design");
            AddCategoryMapping(1357, TorznabCatType.PC0day, " | - Authoring");
            AddCategoryMapping(890, TorznabCatType.PC0day, " | - Official compilations vector clipart");
            AddCategoryMapping(830, TorznabCatType.PC0day, " | - Other vector cliparts");
            AddCategoryMapping(1290, TorznabCatType.PC0day, " |- Photostoсks");
            AddCategoryMapping(1962, TorznabCatType.PC0day, " | - Photoshop Costumes");
            AddCategoryMapping(831, TorznabCatType.PC0day, " | - Frames and Vignettes for processing photos");
            AddCategoryMapping(829, TorznabCatType.PC0day, " | - Other raster clipart");
            AddCategoryMapping(633, TorznabCatType.PC0day, " | - 3D models, scenes and materials");
            AddCategoryMapping(1009, TorznabCatType.PC0day, " | - Footage");
            AddCategoryMapping(1963, TorznabCatType.PC0day, " | - Other collections footage");
            AddCategoryMapping(1954, TorznabCatType.PC0day, " | - Music Library");
            AddCategoryMapping(1010, TorznabCatType.PC0day, " | - Sound Effects");
            AddCategoryMapping(1674, TorznabCatType.PC0day, " | - Sample Libraries");
            AddCategoryMapping(2421, TorznabCatType.PC0day, " | - Library and saundbanki for samplers, presets for sy ..");
            AddCategoryMapping(2492, TorznabCatType.PC0day, " |- Multitracks");
            AddCategoryMapping(839, TorznabCatType.PC0day, " | - Materials for creating menus and DVD covers");
            AddCategoryMapping(1679, TorznabCatType.PC0day, " | - Styles, brushes, shapes and patterns for Adobe Photoshop");
            AddCategoryMapping(1011, TorznabCatType.PC0day, " | - Fonts");
            AddCategoryMapping(835, TorznabCatType.PC0day, " | - Miscellaneous (Materials for Multimedia and Design)");
            AddCategoryMapping(1503, TorznabCatType.PC0day, "GIS, navigation systems and maps");
            AddCategoryMapping(1507, TorznabCatType.PC0day, " | - GIS (Geoinformatsionnыe sistemы)");
            AddCategoryMapping(1526, TorznabCatType.PC0day, " | - Maps provided with the program shell");
            AddCategoryMapping(1508, TorznabCatType.PC0day, " | - Atlases and maps modern (after 1950)");
            AddCategoryMapping(1509, TorznabCatType.PC0day, " | - Atlases and antique maps (up to 1950)");
            AddCategoryMapping(1510, TorznabCatType.PC0day, " | - Other Maps (astronomical, historical, topically ..");
            AddCategoryMapping(1511, TorznabCatType.PC0day, " | - Built-in car navigation");
            AddCategoryMapping(1512, TorznabCatType.PC0day, " |- Garmin");
            AddCategoryMapping(1513, TorznabCatType.PC0day, " | -");
            AddCategoryMapping(1514, TorznabCatType.PC0day, " |- TomTom");
            AddCategoryMapping(1515, TorznabCatType.PC0day, " |- Navigon / Navitel");
            AddCategoryMapping(1516, TorznabCatType.PC0day, " |- Igo");
            AddCategoryMapping(1517, TorznabCatType.PC0day, " | - Miscellaneous - navigation and maps");

            // Мобильные устройства
            AddCategoryMapping(285, TorznabCatType.PCPhoneOther, "Games, applications and so on. Mobile");
            AddCategoryMapping(2149, TorznabCatType.PCPhoneAndroid, " | - Games for Android OS");
            AddCategoryMapping(2154, TorznabCatType.PCPhoneAndroid, " | - Applications for Android OS");
            AddCategoryMapping(2419, TorznabCatType.PCPhoneOther, " | - Applications for Windows Phone 7,8");
            AddCategoryMapping(2420, TorznabCatType.PCPhoneOther, " | - Games for Windows Phone 7,8");
            AddCategoryMapping(1004, TorznabCatType.PCPhoneOther, " | - Games for Symbian");
            AddCategoryMapping(289, TorznabCatType.PCPhoneOther, " | - Applications for Symbian");
            AddCategoryMapping(1001, TorznabCatType.PCPhoneOther, " | - Games for Java");
            AddCategoryMapping(1005, TorznabCatType.PCPhoneOther, " | - Applications for Java");
            AddCategoryMapping(1002, TorznabCatType.PCPhoneOther, " | - Games for Windows Mobile, Palm OS, BlackBerry and so on.");
            AddCategoryMapping(290, TorznabCatType.PCPhoneOther, " | - Applications for Windows Mobile, Palm OS, BlackBerry and so on.");
            AddCategoryMapping(288, TorznabCatType.PCPhoneOther, " | - Software for your phone");
            AddCategoryMapping(292, TorznabCatType.PCPhoneOther, " | - Firmware for phones");
            AddCategoryMapping(291, TorznabCatType.PCPhoneOther, " | - Wallpapers and Themes");
            AddCategoryMapping(957, TorznabCatType.PCPhoneOther, "Video for mobile devices");
            AddCategoryMapping(287, TorznabCatType.PCPhoneOther, " | - Video for Smartphones and PDAs");
            AddCategoryMapping(286, TorznabCatType.PCPhoneOther, " | - Mobile Video (3GP)");

            // Apple
            AddCategoryMapping(1366, TorznabCatType.PCMac, "Apple Macintosh");
            AddCategoryMapping(1368, TorznabCatType.PCMac, " |- Mac OS (для Macintosh)");
            AddCategoryMapping(1383, TorznabCatType.PCMac, " | - Mac OS (for RS-Hakintoš)");
            AddCategoryMapping(537, TorznabCatType.PCMac, " | - Game Mac OS");
            AddCategoryMapping(1394, TorznabCatType.PCMac, " | - Software for viewing and video processing");
            AddCategoryMapping(1370, TorznabCatType.PCMac, " | - Software to build and graphics processing");
            AddCategoryMapping(2237, TorznabCatType.PCMac, " | - Plug-ins for Adobe's programs");
            AddCategoryMapping(1372, TorznabCatType.PCMac, " | - Audio editor and converter");
            AddCategoryMapping(1373, TorznabCatType.PCMac, " | - System software");
            AddCategoryMapping(1375, TorznabCatType.PCMac, " | - Office software");
            AddCategoryMapping(1371, TorznabCatType.PCMac, " | - Software for the Internet and network");
            AddCategoryMapping(1374, TorznabCatType.PCMac, " | - Other software");
            AddCategoryMapping(1933, TorznabCatType.PCMac, "iOS");
            AddCategoryMapping(1935, TorznabCatType.PCMac, " | - Software for iOS");
            AddCategoryMapping(1003, TorznabCatType.PCMac, " | - Games for iOS");
            AddCategoryMapping(1937, TorznabCatType.PCMac, " | - Miscellaneous for iOS");
            AddCategoryMapping(2235, TorznabCatType.PCMac, "Video");
            AddCategoryMapping(1908, TorznabCatType.PCMac, " | - Movies for iPod, iPhone, iPad");
            AddCategoryMapping(864, TorznabCatType.PCMac, " | - TV Shows for iPod, iPhone, iPad");
            AddCategoryMapping(863, TorznabCatType.PCMac, " | - Cartoons for iPod, iPhone, iPad");
            AddCategoryMapping(2535, TorznabCatType.PCMac, " | - Anime for iPod, iPhone, iPad");
            AddCategoryMapping(2534, TorznabCatType.PCMac, " | - The music video to iPod, iPhone, iPad");
            AddCategoryMapping(2238, TorznabCatType.PCMac, "Видео HD");
            AddCategoryMapping(1936, TorznabCatType.PCMac, " | - HD Movies to Apple TV");
            AddCategoryMapping(315, TorznabCatType.PCMac, " | - HD TV Shows on Apple TV");
            AddCategoryMapping(1363, TorznabCatType.PCMac, " | - HD Animation for Apple TV");
            AddCategoryMapping(2082, TorznabCatType.PCMac, " | - Documentary HD video for Apple TV");
            AddCategoryMapping(2241, TorznabCatType.PCMac, " | - Musical HD video for Apple TV");
            AddCategoryMapping(2236, TorznabCatType.PCMac, "audio");
            AddCategoryMapping(1909, TorznabCatType.PCMac, " | - Audiobooks (AAC, ALAC)");
            AddCategoryMapping(1927, TorznabCatType.PCMac, " | - Music Lossless (ALAC)");
            AddCategoryMapping(2240, TorznabCatType.PCMac, " |- Музыка Lossy (AAC-iTunes)");
            AddCategoryMapping(2248, TorznabCatType.PCMac, " |- Музыка Lossy (AAC)");
            AddCategoryMapping(2244, TorznabCatType.PCMac, " |- Музыка Lossy (AAC) (Singles, EPs)");
            AddCategoryMapping(2243, TorznabCatType.PCMac, "F.A.Q.");

            // Медицина и здоровье
            AddCategoryMapping(2125, TorznabCatType.Books, "Books, magazines and programs");
            AddCategoryMapping(2133, TorznabCatType.Books, " | - Clinical Medicine until 1980");
            AddCategoryMapping(2130, TorznabCatType.Books, " | - Clinical Medicine from 1980 to 2000");
            AddCategoryMapping(2313, TorznabCatType.Books, " | - Clinical Medicine since 2000");
            AddCategoryMapping(2314, TorznabCatType.Books, " | - Popular medical periodicals (newspapers and magazines)");
            AddCategoryMapping(2528, TorznabCatType.Books, " | - Scientific medical periodicals (newspapers and magazines)");
            AddCategoryMapping(2129, TorznabCatType.Books, " | - Life Sciences");
            AddCategoryMapping(2141, TorznabCatType.Books, " | - Pharmacy and Pharmacology");
            AddCategoryMapping(2132, TorznabCatType.Books, " | - Non-traditional, traditional medicine and popular books on the s ..");
            AddCategoryMapping(2131, TorznabCatType.Books, " | - Veterinary Medicine, Miscellaneous");
            AddCategoryMapping(2315, TorznabCatType.Books, " | - Thematic collection of books");
            AddCategoryMapping(1350, TorznabCatType.Books, " | - Audio Books on medicine");
            AddCategoryMapping(2134, TorznabCatType.Books, " | - Medical software");
            AddCategoryMapping(2126, TorznabCatType.Books, "Tutorials, Doc. movies and TV shows on medicine");
            AddCategoryMapping(2135, TorznabCatType.Books, " | - Medicine and Dentistry");
            AddCategoryMapping(2140, TorznabCatType.Books, " | - Psychotherapy and clinical psychology");
            AddCategoryMapping(2136, TorznabCatType.Books, " | - Massage");
            AddCategoryMapping(2138, TorznabCatType.Books, " | - Health");
            AddCategoryMapping(2139, TorznabCatType.Books, " | - Documentary movies and TV shows on medicine");

            // Разное
            AddCategoryMapping(10, TorznabCatType.Other, "Miscellaneous");
            AddCategoryMapping(865, TorznabCatType.Other, " | - Psihoaktivnye audioprogrammy");
            AddCategoryMapping(1100, TorznabCatType.Other, " | - Avatars, Icons, Smileys");
            AddCategoryMapping(1643, TorznabCatType.Other, " | - Painting, Graphics, Sculpture, Digital Art");
            AddCategoryMapping(848, TorznabCatType.Other, " | - Pictures");
            AddCategoryMapping(808, TorznabCatType.Other, " | - Amateur Photos");
            AddCategoryMapping(630, TorznabCatType.Other, " | - Wallpapers");
            AddCategoryMapping(1664, TorznabCatType.Other, " | - Celebrity Photos");
            AddCategoryMapping(148, TorznabCatType.Other, " | - Audio");
            AddCategoryMapping(807, TorznabCatType.Other, " | - Video");
            AddCategoryMapping(147, TorznabCatType.Other, " | - Publications and educational materials (texts)");
            AddCategoryMapping(847, TorznabCatType.Other, " | - Trailers and additional materials for films");
            AddCategoryMapping(1167, TorznabCatType.Other, " | - Amateur videos");
        }

        public override async Task<ConfigurationData> GetConfigurationForSetup()
        {
            var response = await RequestStringWithCookies(LoginUrl);
            var LoginResultParser = new HtmlParser();
            var LoginResultDocument = LoginResultParser.Parse(response.Content);
            var captchaimg = LoginResultDocument.QuerySelector("img[src^=\"//static.t-ru.org/captcha/\"]");
            if (captchaimg != null)
            {
                var captchaImage = await RequestBytesWithCookies("https:" + captchaimg.GetAttribute("src"));
                configData.CaptchaImage.Value = captchaImage.Content;

                var codefield = LoginResultDocument.QuerySelector("input[name^=\"cap_code_\"]");
                cap_code_field = codefield.GetAttribute("name");

                var sidfield = LoginResultDocument.QuerySelector("input[name=\"cap_sid\"]");
                cap_sid = sidfield.GetAttribute("value");
            }
            else
            {
                configData.CaptchaImage.Value = null;
            }
            return configData;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string>
            {
                { "login_username", configData.Username.Value },
                { "login_password", configData.Password.Value },
                { "login", "entry" }
            };

            if (!string.IsNullOrWhiteSpace(cap_sid))
            {
                pairs.Add("cap_sid", cap_sid);
                pairs.Add(cap_code_field, configData.CaptchaText.Value);

                cap_sid = null;
                cap_code_field = null;
            }

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, CookieHeader, true, null, LoginUrl, true);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("class=\"logged-in-as-uname\""), () =>
            {
                var errorMessage = result.Content;
                var LoginResultParser = new HtmlParser();
                var LoginResultDocument = LoginResultParser.Parse(result.Content);
                var errormsg = LoginResultDocument.QuerySelector("h4[class=\"warnColor1 tCenter mrg_16\"]");
                if (errormsg != null)
                    errorMessage = errormsg.TextContent;

                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.SanitizedSearchTerm;

            var queryCollection = new NameValueCollection();

            // if the search string is empty use the getnew view
            if (string.IsNullOrWhiteSpace(searchString))
            {
                queryCollection.Add("nm", searchString);
            }
            else // use the normal search
            {
                searchString = searchString.Replace("-", " ");
                if (query.Season != 0)
                {
                    searchString += " Сезон: " + query.Season;
                }
                queryCollection.Add("nm", searchString);
            }

            var searchUrl = SearchUrl + "?" + queryCollection.GetQueryString();
            var results = await RequestStringWithCookies(searchUrl);
            if (!results.Content.Contains("class=\"logged-in-as-uname\""))
            {
                // re login
                await ApplyConfiguration(null);
                results = await RequestStringWithCookies(searchUrl);
            }
            try
            {
                string RowsSelector = "table#tor-tbl > tbody > tr";

                var SearchResultParser = new HtmlParser();
                var SearchResultDocument = SearchResultParser.Parse(results.Content);
                var Rows = SearchResultDocument.QuerySelectorAll(RowsSelector);
                foreach (var Row in Rows)
                {
                    try
                    {
                        var release = new ReleaseInfo();

                        release.MinimumRatio = 1;
                        release.MinimumSeedTime = 0;

                        var qDownloadLink = Row.QuerySelector("td.tor-size > a.tr-dl");
                        if (qDownloadLink == null) // Expects moderation
                            continue;

                        var qDetailsLink = Row.QuerySelector("td.t-title > div.t-title > a.tLink");
                        var qSize = Row.QuerySelector("td.tor-size > u");

                        release.Title = qDetailsLink.TextContent;

                        release.Comments = new Uri(SiteLink + "forum/" + qDetailsLink.GetAttribute("href"));
                        release.Link = new Uri(SiteLink + "forum/" + qDownloadLink.GetAttribute("href"));
                        release.Guid = release.Comments;
                        release.Size = ReleaseInfo.GetBytes(qSize.TextContent);

                        var seeders = Row.QuerySelector("td:nth-child(7) > u").TextContent;
                        if (string.IsNullOrWhiteSpace(seeders))
                            seeders = "0";
                        release.Seeders = ParseUtil.CoerceInt(seeders);
                        release.Peers = ParseUtil.CoerceInt(Row.QuerySelector("td:nth-child(8) > b").TextContent) + release.Seeders;
                        release.Grabs = ParseUtil.CoerceLong(Row.QuerySelector("td:nth-child(9)").TextContent);

                        var timestr = Row.QuerySelector("td:nth-child(10) > u").TextContent;
                        release.PublishDate = DateTimeUtil.UnixTimestampToDateTime(long.Parse(timestr));

                        var forum = Row.QuerySelector("td.f-name > div.f-name > a");
                        var forumid = forum.GetAttribute("href").Split('=')[1];
                        release.Category = MapTrackerCatToNewznab(forumid);

                        release.DownloadVolumeFactor = 1;
                        release.UploadVolumeFactor = 1;

                        if (release.Category.Contains(TorznabCatType.TV.ID))
                        {
                            // extract season and episodes
                            var regex = new Regex(".+\\/\\s([^а-яА-я\\/]+)\\s\\/.+Сезон\\s*[:]*\\s+(\\d+).+(?:Серии|Эпизод)+\\s*[:]*\\s+(\\d+-*\\d*).+,\\s+(.+)\\].+(\\(.+\\)).*");

                            var title = regex.Replace(release.Title, "$1 - S$2E$3 - rus $4 $5");
                            title = Regex.Replace(title, "-Rip", "Rip", RegexOptions.IgnoreCase);
                            title = Regex.Replace(title, "WEB-DLRip", "WEBDL", RegexOptions.IgnoreCase);
                            title = Regex.Replace(title, "WEB-DL", "WEBDL", RegexOptions.IgnoreCase);
                            title = Regex.Replace(title, "HDTVRip", "HDTV", RegexOptions.IgnoreCase);
                            title = Regex.Replace(title, "Кураж-Бамбей", "kurazh", RegexOptions.IgnoreCase);

                            release.Title = title;
                        }
                        else if (configData.StripRussianLetters.Value)
                        {
                            var regex = new Regex(@"(\([А-Яа-я\W]+\))|(^[А-Яа-я\W\d]+\/ )|([а-яА-Я \-]+,+)|([а-яА-Я]+)");
                            release.Title = regex.Replace(release.Title, "");
                        }

                        releases.Add(release);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(string.Format("{0}: Error while parsing row '{1}':\n\n{2}", ID, Row.OuterHtml, ex));
                    }
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.Content, ex);
            }

            return releases;
        }
    }
}
