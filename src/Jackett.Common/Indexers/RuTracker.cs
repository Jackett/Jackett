using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using NLog.Targets;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class RuTracker : BaseWebIndexer
    {
        private string LoginUrl => SiteLink + "forum/login.php";
        private string SearchUrl => SiteLink + "forum/tracker.php";

        private string _capSid;
        private string _capCodeField;

        private new ConfigurationDataRutracker configData => (ConfigurationDataRutracker)base.configData;

        public override string[] AlternativeSiteLinks { get; protected set; } = {
            "https://rutracker.org/",
            "https://rutracker.net/"
        };

        private Regex _regexToFindTagsInReleaseTitle = new Regex(@"\[[^\[]+\]|\([^(]+\)");

        public RuTracker(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "rutracker",
                   name: "RuTracker",
                   description: "RuTracker is a Semi-Private Russian torrent site with a thriving file-sharing community",
                   link: "https://rutracker.org/",
                   caps: new TorznabCapabilities
                   {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                       },
                       MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q
                       },
                       MusicSearchParams = new List<MusicSearchParam>
                       {
                           MusicSearchParam.Q
                       },
                       BookSearchParams = new List<BookSearchParam>
                       {
                           BookSearchParam.Q
                       }
                   },
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataRutracker())
        {
            Encoding = Encoding.GetEncoding("windows-1251");
            Language = "ru-RU";
            Type = "semi-private";
            // note: when refreshing the categories use the tracker.php page and NOT the search.php page!
            AddCategoryMapping(22, TorznabCatType.Movies, "Наше кино");
            AddCategoryMapping(941, TorznabCatType.Movies, "|- Кино СССР");
            AddCategoryMapping(1666, TorznabCatType.Movies, "|- Детские отечественные фильмы");
            AddCategoryMapping(376, TorznabCatType.Movies, "|- Авторские дебюты");
            AddCategoryMapping(7, TorznabCatType.MoviesForeign, "Зарубежное кино");
            AddCategoryMapping(187, TorznabCatType.MoviesForeign, "|- Классика мирового кинематографа");
            AddCategoryMapping(2090, TorznabCatType.MoviesForeign, "|- Фильмы до 1990 года");
            AddCategoryMapping(2221, TorznabCatType.MoviesForeign, "|- Фильмы 1991-2000");
            AddCategoryMapping(2091, TorznabCatType.MoviesForeign, "|- Фильмы 2001-2005");
            AddCategoryMapping(2092, TorznabCatType.MoviesForeign, "|- Фильмы 2006-2010");
            AddCategoryMapping(2093, TorznabCatType.MoviesForeign, "|- Фильмы 2011-2015");
            AddCategoryMapping(2200, TorznabCatType.MoviesForeign, "|- Фильмы 2016-2019");
            AddCategoryMapping(1950, TorznabCatType.MoviesForeign, "|- Фильмы 2020");
            AddCategoryMapping(2540, TorznabCatType.MoviesForeign, "|- Фильмы Ближнего Зарубежья");
            AddCategoryMapping(934, TorznabCatType.MoviesForeign, "|- Азиатские фильмы");
            AddCategoryMapping(505, TorznabCatType.MoviesForeign, "|- Индийское кино");
            AddCategoryMapping(212, TorznabCatType.MoviesForeign, "|- Сборники фильмов");
            AddCategoryMapping(2459, TorznabCatType.MoviesForeign, "|- Короткий метр");
            AddCategoryMapping(1235, TorznabCatType.MoviesForeign, "|- Грайндхаус");
            AddCategoryMapping(185, TorznabCatType.Audio, "|- Звуковые дорожки и Переводы");
            AddCategoryMapping(124, TorznabCatType.MoviesOther, "Арт-хаус и авторское кино");
            AddCategoryMapping(1543, TorznabCatType.MoviesOther, "|- Короткий метр (Арт-хаус и авторское кино)");
            AddCategoryMapping(709, TorznabCatType.MoviesOther, "|- Документальные фильмы (Арт-хаус и авторское кино)");
            AddCategoryMapping(1577, TorznabCatType.MoviesOther, "|- Анимация (Арт-хаус и авторское кино)");
            AddCategoryMapping(511, TorznabCatType.TVOther, "Театр");
            AddCategoryMapping(93, TorznabCatType.MoviesDVD, "DVD Video");
            AddCategoryMapping(905, TorznabCatType.MoviesDVD, "|- Классика мирового кинематографа (DVD Video)");
            AddCategoryMapping(101, TorznabCatType.MoviesDVD, "|- Зарубежное кино (DVD Video)");
            AddCategoryMapping(100, TorznabCatType.MoviesDVD, "|- Наше кино (DVD Video)");
            AddCategoryMapping(877, TorznabCatType.MoviesDVD, "|- Фильмы Ближнего Зарубежья (DVD Video)");
            AddCategoryMapping(1576, TorznabCatType.MoviesDVD, "|- Азиатские фильмы (DVD Video)");
            AddCategoryMapping(572, TorznabCatType.MoviesDVD, "|- Арт-хаус и авторское кино (DVD Video)");
            AddCategoryMapping(2220, TorznabCatType.MoviesDVD, "|- Индийское кино (DVD Video)");
            AddCategoryMapping(1670, TorznabCatType.MoviesDVD, "|- Грайндхаус (DVD Video)");
            AddCategoryMapping(2198, TorznabCatType.MoviesHD, "HD Video");
            AddCategoryMapping(1457, TorznabCatType.MoviesUHD, "|- UHD Video");
            AddCategoryMapping(2199, TorznabCatType.MoviesHD, "|- Классика мирового кинематографа (HD Video)");
            AddCategoryMapping(313, TorznabCatType.MoviesHD, "|- Зарубежное кино (HD Video)");
            AddCategoryMapping(312, TorznabCatType.MoviesHD, "|- Наше кино (HD Video)");
            AddCategoryMapping(1247, TorznabCatType.MoviesHD, "|- Фильмы Ближнего Зарубежья (HD Video)");
            AddCategoryMapping(2201, TorznabCatType.MoviesHD, "|- Азиатские фильмы (HD Video)");
            AddCategoryMapping(2339, TorznabCatType.MoviesHD, "|- Арт-хаус и авторское кино (HD Video)");
            AddCategoryMapping(140, TorznabCatType.MoviesHD, "|- Индийское кино (HD Video)");
            AddCategoryMapping(194, TorznabCatType.MoviesHD, "|- Грайндхаус (HD Video)");
            AddCategoryMapping(352, TorznabCatType.Movies3D, "3D/Стерео Кино, Видео, TV и Спорт");
            AddCategoryMapping(549, TorznabCatType.Movies3D, "|- 3D Кинофильмы");
            AddCategoryMapping(1213, TorznabCatType.Movies3D, "|- 3D Мультфильмы");
            AddCategoryMapping(2109, TorznabCatType.Movies3D, "|- 3D Документальные фильмы");
            AddCategoryMapping(514, TorznabCatType.Movies3D, "|- 3D Спорт");
            AddCategoryMapping(2097, TorznabCatType.Movies3D, "|- 3D Ролики, Музыкальное видео, Трейлеры к фильмам");
            AddCategoryMapping(4, TorznabCatType.Movies, "Мультфильмы");
            AddCategoryMapping(2343, TorznabCatType.MoviesHD, "|- Отечественные мультфильмы (HD Video)");
            AddCategoryMapping(930, TorznabCatType.MoviesHD, "|- Иностранные мультфильмы (HD Video)");
            AddCategoryMapping(2365, TorznabCatType.MoviesHD, "|- Иностранные короткометражные мультфильмы (HD Video)");
            AddCategoryMapping(1900, TorznabCatType.MoviesDVD, "|- Отечественные мультфильмы (DVD)");
            AddCategoryMapping(521, TorznabCatType.MoviesDVD, "|- Иностранные мультфильмы (DVD)");
            AddCategoryMapping(2258, TorznabCatType.MoviesDVD, "|- Иностранные короткометражные мультфильмы (DVD)");
            AddCategoryMapping(208, TorznabCatType.Movies, "|- Отечественные мультфильмы");
            AddCategoryMapping(539, TorznabCatType.Movies, "|- Отечественные полнометражные мультфильмы");
            AddCategoryMapping(209, TorznabCatType.MoviesForeign, "|- Иностранные мультфильмы");
            AddCategoryMapping(484, TorznabCatType.MoviesForeign, "|- Иностранные короткометражные мультфильмы");
            AddCategoryMapping(822, TorznabCatType.Movies, "|- Сборники мультфильмов");
            AddCategoryMapping(921, TorznabCatType.TV, "Мультсериалы");
            AddCategoryMapping(815, TorznabCatType.TVSD, "|- Мультсериалы (SD Video)");
            AddCategoryMapping(816, TorznabCatType.TVHD, "|- Мультсериалы (DVD Video)");
            AddCategoryMapping(1460, TorznabCatType.TVHD, "|- Мультсериалы (HD Video)");
            AddCategoryMapping(33, TorznabCatType.TVAnime, "Аниме");
            AddCategoryMapping(2484, TorznabCatType.TVAnime, "|- Артбуки и журналы (Аниме)");
            AddCategoryMapping(1386, TorznabCatType.TVAnime, "|- Обои, сканы, аватары, арт");
            AddCategoryMapping(1387, TorznabCatType.TVAnime, "|- AMV и другие ролики");
            AddCategoryMapping(599, TorznabCatType.TVAnime, "|- Аниме (DVD)");
            AddCategoryMapping(1105, TorznabCatType.TVAnime, "|- Аниме (HD Video)");
            AddCategoryMapping(1389, TorznabCatType.TVAnime, "|- Аниме (основной подраздел)");
            AddCategoryMapping(1391, TorznabCatType.TVAnime, "|- Аниме (плеерный подраздел)");
            AddCategoryMapping(2491, TorznabCatType.TVAnime, "|- Аниме (QC подраздел)");
            AddCategoryMapping(404, TorznabCatType.TVAnime, "|- Покемоны");
            AddCategoryMapping(1390, TorznabCatType.TVAnime, "|- Наруто");
            AddCategoryMapping(1642, TorznabCatType.TVAnime, "|- Гандам");
            AddCategoryMapping(893, TorznabCatType.TVAnime, "|- Японские мультфильмы");
            AddCategoryMapping(809, TorznabCatType.Audio, "|- Звуковые дорожки (Аниме)");
            AddCategoryMapping(9, TorznabCatType.TV, "Русские сериалы");
            AddCategoryMapping(81, TorznabCatType.TVHD, "|- Русские сериалы (HD Video)");
            AddCategoryMapping(80, TorznabCatType.TV, "|- Возвращение Мухтара");
            AddCategoryMapping(1535, TorznabCatType.TV, "|- Воронины");
            AddCategoryMapping(188, TorznabCatType.TV, "|- Чернобыль: Зона отчуждения");
            AddCategoryMapping(91, TorznabCatType.TV, "|- Кухня / Отель Элеон");
            AddCategoryMapping(990, TorznabCatType.TV, "|- Универ / Универ. Новая общага / СашаТаня");
            AddCategoryMapping(1408, TorznabCatType.TV, "|- Ольга / Физрук");
            AddCategoryMapping(175, TorznabCatType.TV, "|- След");
            AddCategoryMapping(79, TorznabCatType.TV, "|- Солдаты и пр.");
            AddCategoryMapping(104, TorznabCatType.TV, "|- Тайны следствия");
            AddCategoryMapping(189, TorznabCatType.TVForeign, "Зарубежные сериалы");
            AddCategoryMapping(842, TorznabCatType.TVForeign, "|- Новинки и сериалы в стадии показа");
            AddCategoryMapping(235, TorznabCatType.TVForeign, "|- Сериалы США и Канады");
            AddCategoryMapping(242, TorznabCatType.TVForeign, "|- Сериалы Великобритании и Ирландии");
            AddCategoryMapping(819, TorznabCatType.TVForeign, "|- Скандинавские сериалы");
            AddCategoryMapping(1531, TorznabCatType.TVForeign, "|- Испанские сериалы");
            AddCategoryMapping(721, TorznabCatType.TVForeign, "|- Итальянские сериалы");
            AddCategoryMapping(1102, TorznabCatType.TVForeign, "|- Европейские сериалы");
            AddCategoryMapping(1120, TorznabCatType.TVForeign, "|- Сериалы стран Африки, Ближнего и Среднего Востока");
            AddCategoryMapping(1214, TorznabCatType.TVForeign, "|- Сериалы Австралии и Новой Зеландии");
            AddCategoryMapping(489, TorznabCatType.TVForeign, "|- Сериалы Ближнего Зарубежья");
            AddCategoryMapping(387, TorznabCatType.TVForeign, "|- Сериалы совместного производства нескольких стран");
            AddCategoryMapping(1359, TorznabCatType.TVForeign, "|- Веб-сериалы, Вебизоды к сериалам и Пилотные серии сериалов");
            AddCategoryMapping(184, TorznabCatType.TVForeign, "|- Бесстыжие / Shameless (US)");
            AddCategoryMapping(1171, TorznabCatType.TVForeign, "|- Викинги / Vikings");
            AddCategoryMapping(1417, TorznabCatType.TVForeign, "|- Во все тяжкие / Breaking Bad");
            AddCategoryMapping(625, TorznabCatType.TVForeign, "|- Доктор Хаус / House M.D.");
            AddCategoryMapping(1449, TorznabCatType.TVForeign, "|- Игра престолов / Game of Thrones");
            AddCategoryMapping(273, TorznabCatType.TVForeign, "|- Карточный Домик / House of Cards");
            AddCategoryMapping(504, TorznabCatType.TVForeign, "|- Клан Сопрано / The Sopranos");
            AddCategoryMapping(372, TorznabCatType.TVForeign, "|- Сверхъестественное / Supernatural");
            AddCategoryMapping(110, TorznabCatType.TVForeign, "|- Секретные материалы / The X-Files");
            AddCategoryMapping(121, TorznabCatType.TVForeign, "|- Твин пикс / Twin Peaks");
            AddCategoryMapping(507, TorznabCatType.TVForeign, "|- Теория большого взрыва + Детство Шелдона");
            AddCategoryMapping(536, TorznabCatType.TVForeign, "|- Форс-мажоры / Костюмы в законе / Suits");
            AddCategoryMapping(1144, TorznabCatType.TVForeign, "|- Ходячие мертвецы + Бойтесь ходячих мертвецов");
            AddCategoryMapping(173, TorznabCatType.TVForeign, "|- Черное зеркало / Black Mirror");
            AddCategoryMapping(195, TorznabCatType.TVForeign, "|- Для некондиционных раздач");
            AddCategoryMapping(2366, TorznabCatType.TVHD, "Зарубежные сериалы (HD Video)");
            AddCategoryMapping(119, TorznabCatType.TVUHD, "|- Зарубежные сериалы (UHD Video)");
            AddCategoryMapping(1803, TorznabCatType.TVHD, "|- Новинки и сериалы в стадии показа (HD Video)");
            AddCategoryMapping(266, TorznabCatType.TVHD, "|- Сериалы США и Канады (HD Video)");
            AddCategoryMapping(193, TorznabCatType.TVHD, "|- Сериалы Великобритании и Ирландии (HD Video)");
            AddCategoryMapping(1690, TorznabCatType.TVHD, "|- Скандинавские сериалы (HD Video)");
            AddCategoryMapping(1459, TorznabCatType.TVHD, "|- Европейские сериалы (HD Video)");
            AddCategoryMapping(1463, TorznabCatType.TVHD, "|- Сериалы стран Африки, Ближнего и Среднего Востока (HD Video)");
            AddCategoryMapping(825, TorznabCatType.TVHD, "|- Сериалы Австралии и Новой Зеландии (HD Video)");
            AddCategoryMapping(1248, TorznabCatType.TVHD, "|- Сериалы Ближнего Зарубежья (HD Video)");
            AddCategoryMapping(1288, TorznabCatType.TVHD, "|- Сериалы совместного производства нескольких стран (HD Video)");
            AddCategoryMapping(1669, TorznabCatType.TVHD, "|- Викинги / Vikings (HD Video)");
            AddCategoryMapping(2393, TorznabCatType.TVHD, "|- Доктор Хаус / House M.D. (HD Video)");
            AddCategoryMapping(265, TorznabCatType.TVHD, "|- Игра престолов / Game of Thrones (HD Video)");
            AddCategoryMapping(2406, TorznabCatType.TVHD, "|- Карточный домик (HD Video)");
            AddCategoryMapping(2404, TorznabCatType.TVHD, "|- Сверхъестественное / Supernatural (HD Video)");
            AddCategoryMapping(2405, TorznabCatType.TVHD, "|- Секретные материалы / The X-Files (HD Video)");
            AddCategoryMapping(2370, TorznabCatType.TVHD, "|- Твин пикс / Twin Peaks (HD Video)");
            AddCategoryMapping(2396, TorznabCatType.TVHD, "|- Теория Большого Взрыва / The Big Bang Theory (HD Video)");
            AddCategoryMapping(2398, TorznabCatType.TVHD, "|- Ходячие мертвецы + Бойтесь ходячих мертвецов (HD Video)");
            AddCategoryMapping(1949, TorznabCatType.TVHD, "|- Черное зеркало / Black Mirror (HD Video)");
            AddCategoryMapping(1498, TorznabCatType.TVHD, "|- Для некондиционных раздач (HD Video)");
            AddCategoryMapping(911, TorznabCatType.TVForeign, "Сериалы Латинской Америки, Турции и Индии");
            AddCategoryMapping(1493, TorznabCatType.TVForeign, "|- Актёры и актрисы латиноамериканских сериалов");
            AddCategoryMapping(325, TorznabCatType.TVForeign, "|- Сериалы Аргентины");
            AddCategoryMapping(534, TorznabCatType.TVForeign, "|- Сериалы Бразилии");
            AddCategoryMapping(594, TorznabCatType.TVForeign, "|- Сериалы Венесуэлы");
            AddCategoryMapping(1301, TorznabCatType.TVForeign, "|- Сериалы Индии");
            AddCategoryMapping(607, TorznabCatType.TVForeign, "|- Сериалы Колумбии");
            AddCategoryMapping(1574, TorznabCatType.TVForeign, "|- Сериалы Латинской Америки с озвучкой (раздачи папками)");
            AddCategoryMapping(1539, TorznabCatType.TVForeign, "|- Сериалы Латинской Америки с субтитрами");
            AddCategoryMapping(1940, TorznabCatType.TVForeign, "|- Официальные краткие версии сериалов Латинской Америки");
            AddCategoryMapping(694, TorznabCatType.TVForeign, "|- Сериалы Мексики");
            AddCategoryMapping(775, TorznabCatType.TVForeign, "|- Сериалы Перу, Сальвадора, Чили и других стран");
            AddCategoryMapping(781, TorznabCatType.TVForeign, "|- Сериалы совместного производства");
            AddCategoryMapping(718, TorznabCatType.TVForeign, "|- Сериалы США (латиноамериканские)");
            AddCategoryMapping(704, TorznabCatType.TVForeign, "|- Сериалы Турции");
            AddCategoryMapping(1537, TorznabCatType.TVForeign, "|- Для некондиционных раздач");
            AddCategoryMapping(2100, TorznabCatType.TVForeign, "Азиатские сериалы");
            AddCategoryMapping(717, TorznabCatType.TVForeign, "|- Китайские сериалы с субтитрами");
            AddCategoryMapping(915, TorznabCatType.TVForeign, "|- Корейские сериалы с озвучкой");
            AddCategoryMapping(1242, TorznabCatType.TVForeign, "|- Корейские сериалы с субтитрами");
            AddCategoryMapping(2412, TorznabCatType.TVForeign, "|- Прочие азиатские сериалы с озвучкой");
            AddCategoryMapping(1938, TorznabCatType.TVForeign, "|- Тайваньские сериалы с субтитрами");
            AddCategoryMapping(2104, TorznabCatType.TVForeign, "|- Японские сериалы с субтитрами");
            AddCategoryMapping(1939, TorznabCatType.TVForeign, "|- Японские сериалы с озвучкой");
            AddCategoryMapping(2102, TorznabCatType.TVForeign, "|- VMV и др. ролики");
            AddCategoryMapping(670, TorznabCatType.TVDocumentary, "Вера и религия");
            AddCategoryMapping(1475, TorznabCatType.TVDocumentary, "|- [Видео Религия] Христианство");
            AddCategoryMapping(2107, TorznabCatType.TVDocumentary, "|- [Видео Религия] Ислам");
            AddCategoryMapping(294, TorznabCatType.TVDocumentary, "|- [Видео Религия] Религии Индии, Тибета и Восточной Азии");
            AddCategoryMapping(1453, TorznabCatType.TVDocumentary, "|- [Видео Религия] Культы и новые религиозные движения");
            AddCategoryMapping(46, TorznabCatType.TVDocumentary, "Документальные фильмы и телепередачи");
            AddCategoryMapping(103, TorznabCatType.TVDocumentary, "|- Документальные (DVD)");
            AddCategoryMapping(671, TorznabCatType.TVDocumentary, "|- [Док] Биографии. Личности и кумиры");
            AddCategoryMapping(2177, TorznabCatType.TVDocumentary, "|- [Док] Кинематограф и мультипликация");
            AddCategoryMapping(656, TorznabCatType.TVDocumentary, "|- [Док] Мастера искусств Театра и Кино");
            AddCategoryMapping(2538, TorznabCatType.TVDocumentary, "|- [Док] Искусство, история искусств");
            AddCategoryMapping(2159, TorznabCatType.TVDocumentary, "|- [Док] Музыка");
            AddCategoryMapping(251, TorznabCatType.TVDocumentary, "|- [Док] Криминальная документалистика");
            AddCategoryMapping(98, TorznabCatType.TVDocumentary, "|- [Док] Тайны века / Спецслужбы / Теории Заговоров");
            AddCategoryMapping(97, TorznabCatType.TVDocumentary, "|- [Док] Военное дело");
            AddCategoryMapping(851, TorznabCatType.TVDocumentary, "|- [Док] Вторая мировая война");
            AddCategoryMapping(2178, TorznabCatType.TVDocumentary, "|- [Док] Аварии / Катастрофы / Катаклизмы");
            AddCategoryMapping(821, TorznabCatType.TVDocumentary, "|- [Док] Авиация");
            AddCategoryMapping(2076, TorznabCatType.TVDocumentary, "|- [Док] Космос");
            AddCategoryMapping(56, TorznabCatType.TVDocumentary, "|- [Док] Научно-популярные фильмы");
            AddCategoryMapping(2123, TorznabCatType.TVDocumentary, "|- [Док] Флора и фауна");
            AddCategoryMapping(876, TorznabCatType.TVDocumentary, "|- [Док] Путешествия и туризм");
            AddCategoryMapping(2139, TorznabCatType.TVDocumentary, "|- [Док] Медицина");
            AddCategoryMapping(2380, TorznabCatType.TVDocumentary, "|- [Док] Социальные ток-шоу");
            AddCategoryMapping(1467, TorznabCatType.TVDocumentary, "|- [Док] Информационно-аналитические и общественно-политические перед..");
            AddCategoryMapping(1469, TorznabCatType.TVDocumentary, "|- [Док] Архитектура и строительство");
            AddCategoryMapping(672, TorznabCatType.TVDocumentary, "|- [Док] Всё о доме, быте и дизайне");
            AddCategoryMapping(249, TorznabCatType.TVDocumentary, "|- [Док] BBC");
            AddCategoryMapping(552, TorznabCatType.TVDocumentary, "|- [Док] Discovery");
            AddCategoryMapping(500, TorznabCatType.TVDocumentary, "|- [Док] National Geographic");
            AddCategoryMapping(2112, TorznabCatType.TVDocumentary, "|- [Док] История: Древний мир / Античность / Средневековье");
            AddCategoryMapping(1327, TorznabCatType.TVDocumentary, "|- [Док] История: Новое и Новейшее время");
            AddCategoryMapping(1468, TorznabCatType.TVDocumentary, "|- [Док] Эпоха СССР");
            AddCategoryMapping(1280, TorznabCatType.TVDocumentary, "|- [Док] Битва экстрасенсов / Теория невероятности / Искатели / Галил..");
            AddCategoryMapping(752, TorznabCatType.TVDocumentary, "|- [Док] Русские сенсации / Программа Максимум / Профессия репортёр");
            AddCategoryMapping(1114, TorznabCatType.TVDocumentary, "|- [Док] Паранормальные явления");
            AddCategoryMapping(2168, TorznabCatType.TVDocumentary, "|- [Док] Альтернативная история и наука");
            AddCategoryMapping(2160, TorznabCatType.TVDocumentary, "|- [Док] Внежанровая документалистика");
            AddCategoryMapping(2176, TorznabCatType.TVDocumentary, "|- [Док] Разное / некондиция");
            AddCategoryMapping(314, TorznabCatType.TVDocumentary, "Документальные (HD Video)");
            AddCategoryMapping(2323, TorznabCatType.TVDocumentary, "|- Информационно-аналитические и общественно-политические (HD Video)");
            AddCategoryMapping(1278, TorznabCatType.TVDocumentary, "|- Биографии. Личности и кумиры (HD Video)");
            AddCategoryMapping(1281, TorznabCatType.TVDocumentary, "|- Военное дело (HD Video)");
            AddCategoryMapping(2110, TorznabCatType.TVDocumentary, "|- Естествознание, наука и техника (HD Video)");
            AddCategoryMapping(979, TorznabCatType.TVDocumentary, "|- Путешествия и туризм (HD Video)");
            AddCategoryMapping(2169, TorznabCatType.TVDocumentary, "|- Флора и фауна (HD Video)");
            AddCategoryMapping(2166, TorznabCatType.TVDocumentary, "|- История (HD Video)");
            AddCategoryMapping(2164, TorznabCatType.TVDocumentary, "|- BBC, Discovery, National Geographic (HD Video)");
            AddCategoryMapping(2163, TorznabCatType.TVDocumentary, "|- Криминальная документалистика (HD Video)");
            AddCategoryMapping(24, TorznabCatType.TVDocumentary, "Развлекательные телепередачи и шоу, приколы и юмор");
            AddCategoryMapping(1959, TorznabCatType.TVOther, "|- [Видео Юмор] Интеллектуальные игры и викторины");
            AddCategoryMapping(939, TorznabCatType.TVOther, "|- [Видео Юмор] Реалити и ток-шоу / номинации / показы");
            AddCategoryMapping(1481, TorznabCatType.TVOther, "|- [Видео Юмор] Детские телешоу");
            AddCategoryMapping(113, TorznabCatType.TVOther, "|- [Видео Юмор] КВН");
            AddCategoryMapping(115, TorznabCatType.TVOther, "|- [Видео Юмор] Пост КВН");
            AddCategoryMapping(882, TorznabCatType.TVOther, "|- [Видео Юмор] Кривое Зеркало / Городок / В Городке");
            AddCategoryMapping(1482, TorznabCatType.TVOther, "|- [Видео Юмор] Ледовые шоу");
            AddCategoryMapping(393, TorznabCatType.TVOther, "|- [Видео Юмор] Музыкальные шоу");
            AddCategoryMapping(1569, TorznabCatType.TVOther, "|- [Видео Юмор] Званый ужин");
            AddCategoryMapping(373, TorznabCatType.TVOther, "|- [Видео Юмор] Хорошие Шутки");
            AddCategoryMapping(1186, TorznabCatType.TVOther, "|- [Видео Юмор] Вечерний Квартал");
            AddCategoryMapping(137, TorznabCatType.TVOther, "|- [Видео Юмор] Фильмы со смешным переводом (пародии)");
            AddCategoryMapping(2537, TorznabCatType.TVOther, "|- [Видео Юмор] Stand-up comedy");
            AddCategoryMapping(532, TorznabCatType.TVOther, "|- [Видео Юмор] Украинские Шоу");
            AddCategoryMapping(827, TorznabCatType.TVOther, "|- [Видео Юмор] Танцевальные шоу, концерты, выступления");
            AddCategoryMapping(1484, TorznabCatType.TVOther, "|- [Видео Юмор] Цирк");
            AddCategoryMapping(1485, TorznabCatType.TVOther, "|- [Видео Юмор] Школа злословия");
            AddCategoryMapping(114, TorznabCatType.TVOther, "|- [Видео Юмор] Сатирики и юмористы");
            AddCategoryMapping(1332, TorznabCatType.TVOther, "|- Юмористические аудиопередачи");
            AddCategoryMapping(1495, TorznabCatType.TVOther, "|- Аудио и видео ролики (Приколы и юмор)");
            AddCategoryMapping(1315, TorznabCatType.TVSport, "Зимние Олимпийские игры 2018");
            AddCategoryMapping(1336, TorznabCatType.TVSport, "|- Биатлон");
            AddCategoryMapping(2171, TorznabCatType.TVSport, "|- Лыжные гонки");
            AddCategoryMapping(1339, TorznabCatType.TVSport, "|- Прыжки на лыжах с трамплина / Лыжное двоеборье");
            AddCategoryMapping(2455, TorznabCatType.TVSport, "|- Горные лыжи / Сноубординг / Фристайл");
            AddCategoryMapping(1434, TorznabCatType.TVSport, "|- Бобслей / Санный спорт / Скелетон");
            AddCategoryMapping(2350, TorznabCatType.TVSport, "|- Конькобежный спорт / Шорт-трек");
            AddCategoryMapping(1472, TorznabCatType.TVSport, "|- Фигурное катание");
            AddCategoryMapping(2068, TorznabCatType.TVSport, "|- Хоккей");
            AddCategoryMapping(2016, TorznabCatType.TVSport, "|- Керлинг");
            AddCategoryMapping(1311, TorznabCatType.TVSport, "|- Обзорные и аналитические программы");
            AddCategoryMapping(255, TorznabCatType.TVSport, "Спортивные турниры, фильмы и передачи");
            AddCategoryMapping(256, TorznabCatType.TVSport, "|- Автоспорт");
            AddCategoryMapping(1986, TorznabCatType.TVSport, "|- Мотоспорт");
            AddCategoryMapping(660, TorznabCatType.TVSport, "|- Формула-1 (2020)");
            AddCategoryMapping(1551, TorznabCatType.TVSport, "|- Формула-1 (2012-2019)");
            AddCategoryMapping(626, TorznabCatType.TVSport, "|- Формула 1 (до 2011 вкл.)");
            AddCategoryMapping(262, TorznabCatType.TVSport, "|- Велоспорт");
            AddCategoryMapping(1326, TorznabCatType.TVSport, "|- Волейбол/Гандбол");
            AddCategoryMapping(978, TorznabCatType.TVSport, "|- Бильярд");
            AddCategoryMapping(1287, TorznabCatType.TVSport, "|- Покер");
            AddCategoryMapping(1188, TorznabCatType.TVSport, "|- Бодибилдинг/Силовые виды спорта");
            AddCategoryMapping(1667, TorznabCatType.TVSport, "|- Бокс");
            AddCategoryMapping(1675, TorznabCatType.TVSport, "|- Классические единоборства");
            AddCategoryMapping(257, TorznabCatType.TVSport, "|- Смешанные единоборства и K-1");
            AddCategoryMapping(875, TorznabCatType.TVSport, "|- Американский футбол");
            AddCategoryMapping(263, TorznabCatType.TVSport, "|- Регби");
            AddCategoryMapping(2073, TorznabCatType.TVSport, "|- Бейсбол");
            AddCategoryMapping(550, TorznabCatType.TVSport, "|- Теннис");
            AddCategoryMapping(2124, TorznabCatType.TVSport, "|- Бадминтон/Настольный теннис");
            AddCategoryMapping(1470, TorznabCatType.TVSport, "|- Гимнастика/Соревнования по танцам");
            AddCategoryMapping(528, TorznabCatType.TVSport, "|- Лёгкая атлетика/Водные виды спорта");
            AddCategoryMapping(486, TorznabCatType.TVSport, "|- Зимние виды спорта");
            AddCategoryMapping(854, TorznabCatType.TVSport, "|- Фигурное катание");
            AddCategoryMapping(2079, TorznabCatType.TVSport, "|- Биатлон");
            AddCategoryMapping(260, TorznabCatType.TVSport, "|- Экстрим");
            AddCategoryMapping(1319, TorznabCatType.TVSport, "|- Спорт (видео)");
            AddCategoryMapping(1608, TorznabCatType.TVSport, "⚽ Футбол");
            AddCategoryMapping(2294, TorznabCatType.TVSport, "|- UHDTV. Футбол в формате высокой четкости");
            AddCategoryMapping(136, TorznabCatType.TVSport, "|- Чемпионат Европы 2020 (квалификация)");
            AddCategoryMapping(592, TorznabCatType.TVSport, "|- Лига Наций");
            AddCategoryMapping(1693, TorznabCatType.TVSport, "|- Чемпионат Мира 2022 (отбор)");
            AddCategoryMapping(2533, TorznabCatType.TVSport, "|- Чемпионат Мира 2018 (игры)");
            AddCategoryMapping(1952, TorznabCatType.TVSport, "|- Чемпионат Мира 2018 (обзорные передачи, документалистика)");
            AddCategoryMapping(1621, TorznabCatType.TVSport, "|- Чемпионаты Мира");
            AddCategoryMapping(2075, TorznabCatType.TVSport, "|- Россия 2018-2019");
            AddCategoryMapping(1668, TorznabCatType.TVSport, "|- Россия 2019-2020");
            AddCategoryMapping(1613, TorznabCatType.TVSport, "|- Россия/СССР");
            AddCategoryMapping(1614, TorznabCatType.TVSport, "|- Англия");
            AddCategoryMapping(1623, TorznabCatType.TVSport, "|- Испания");
            AddCategoryMapping(1615, TorznabCatType.TVSport, "|- Италия");
            AddCategoryMapping(1630, TorznabCatType.TVSport, "|- Германия");
            AddCategoryMapping(2425, TorznabCatType.TVSport, "|- Франция");
            AddCategoryMapping(2514, TorznabCatType.TVSport, "|- Украина");
            AddCategoryMapping(1616, TorznabCatType.TVSport, "|- Другие национальные чемпионаты и кубки");
            AddCategoryMapping(2014, TorznabCatType.TVSport, "|- Международные турниры");
            AddCategoryMapping(1442, TorznabCatType.TVSport, "|- Еврокубки 2020-2021");
            AddCategoryMapping(1491, TorznabCatType.TVSport, "|- Еврокубки 2019-2020");
            AddCategoryMapping(1987, TorznabCatType.TVSport, "|- Еврокубки 2011-2018");
            AddCategoryMapping(1617, TorznabCatType.TVSport, "|- Еврокубки");
            AddCategoryMapping(1620, TorznabCatType.TVSport, "|- Чемпионаты Европы");
            AddCategoryMapping(1998, TorznabCatType.TVSport, "|- Товарищеские турниры и матчи");
            AddCategoryMapping(1343, TorznabCatType.TVSport, "|- Обзорные и аналитические передачи 2018-2020");
            AddCategoryMapping(751, TorznabCatType.TVSport, "|- Обзорные и аналитические передачи");
            AddCategoryMapping(497, TorznabCatType.TVSport, "|- Документальные фильмы (футбол)");
            AddCategoryMapping(1697, TorznabCatType.TVSport, "|- Мини-футбол/Пляжный футбол");
            AddCategoryMapping(2004, TorznabCatType.TVSport, "🏀 Баскетбол");
            AddCategoryMapping(2001, TorznabCatType.TVSport, "|- Международные соревнования");
            AddCategoryMapping(2002, TorznabCatType.TVSport, "|- NBA / NCAA (до 2000 г.)");
            AddCategoryMapping(283, TorznabCatType.TVSport, "|- NBA / NCAA (2000-2010 гг.)");
            AddCategoryMapping(1997, TorznabCatType.TVSport, "|- NBA / NCAA (2010-2020 гг.)");
            AddCategoryMapping(2003, TorznabCatType.TVSport, "|- Европейский клубный баскетбол");
            AddCategoryMapping(2009, TorznabCatType.TVSport, "🏒 Хоккей");
            AddCategoryMapping(2010, TorznabCatType.TVSport, "|- Хоккей с мячом / Бенди");
            AddCategoryMapping(1229, TorznabCatType.TVSport, "|- Чемпионат Мира по хоккею 2019");
            AddCategoryMapping(2006, TorznabCatType.TVSport, "|- Международные турниры");
            AddCategoryMapping(2007, TorznabCatType.TVSport, "|- КХЛ");
            AddCategoryMapping(2005, TorznabCatType.TVSport, "|- НХЛ (до 2011/12)");
            AddCategoryMapping(259, TorznabCatType.TVSport, "|- НХЛ (с 2013)");
            AddCategoryMapping(2008, TorznabCatType.TVSport, "|- СССР - Канада");
            AddCategoryMapping(126, TorznabCatType.TVSport, "|- Документальные фильмы и аналитика");
            AddCategoryMapping(845, TorznabCatType.TVSport, "Рестлинг");
            AddCategoryMapping(343, TorznabCatType.TVSport, "|- Professional Wrestling");
            AddCategoryMapping(2111, TorznabCatType.TVSport, "|- Independent Wrestling");
            AddCategoryMapping(1527, TorznabCatType.TVSport, "|- International Wrestling");
            AddCategoryMapping(2069, TorznabCatType.TVSport, "|- Oldschool Wrestling");
            AddCategoryMapping(1323, TorznabCatType.TVSport, "|- Documentary Wrestling");
            AddCategoryMapping(1411, TorznabCatType.TVSport, "|- Сканирование, обработка сканов");
            AddCategoryMapping(21, TorznabCatType.Books, "Книги и журналы (общий раздел)");
            AddCategoryMapping(2157, TorznabCatType.Books, "|- Кино, театр, ТВ, мультипликация, цирк");
            AddCategoryMapping(765, TorznabCatType.Books, "|- Рисунок, графический дизайн");
            AddCategoryMapping(2019, TorznabCatType.Books, "|- Фото и видеосъемка");
            AddCategoryMapping(31, TorznabCatType.BooksMags, "|- Журналы и газеты (общий раздел)");
            AddCategoryMapping(1427, TorznabCatType.Books, "|- Эзотерика, гадания, магия, фен-шуй");
            AddCategoryMapping(2422, TorznabCatType.Books, "|- Астрология");
            AddCategoryMapping(2195, TorznabCatType.Books, "|- Красота. Уход. Домоводство");
            AddCategoryMapping(2521, TorznabCatType.Books, "|- Мода. Стиль. Этикет");
            AddCategoryMapping(2223, TorznabCatType.Books, "|- Путешествия и туризм");
            AddCategoryMapping(2447, TorznabCatType.Books, "|- Знаменитости и кумиры");
            AddCategoryMapping(39, TorznabCatType.Books, "|- Разное (книги)");
            AddCategoryMapping(2086, TorznabCatType.Books, "- Самиздат, статьи из журналов, фрагменты книг");
            AddCategoryMapping(1101, TorznabCatType.Books, "Для детей, родителей и учителей");
            AddCategoryMapping(745, TorznabCatType.Books, "|- Учебная литература для детского сада и начальной школы (до 4 класс..");
            AddCategoryMapping(1689, TorznabCatType.Books, "|- Учебная литература для старших классов (5-11 класс)");
            AddCategoryMapping(2336, TorznabCatType.Books, "|- Учителям и педагогам");
            AddCategoryMapping(2337, TorznabCatType.Books, "|- Научно-популярная и познавательная литература (для детей)");
            AddCategoryMapping(1353, TorznabCatType.Books, "|- Досуг и творчество");
            AddCategoryMapping(1400, TorznabCatType.Books, "|- Воспитание и развитие");
            AddCategoryMapping(1415, TorznabCatType.Books, "|- Худ. лит-ра для дошкольников и младших классов");
            AddCategoryMapping(2046, TorznabCatType.Books, "|- Худ. лит-ра для средних и старших классов");
            AddCategoryMapping(1802, TorznabCatType.Books, "Спорт, физическая культура, боевые искусства");
            AddCategoryMapping(2189, TorznabCatType.Books, "|- Футбол (книги и журналы)");
            AddCategoryMapping(2190, TorznabCatType.Books, "|- Хоккей (книги и журналы)");
            AddCategoryMapping(2443, TorznabCatType.Books, "|- Игровые виды спорта");
            AddCategoryMapping(1477, TorznabCatType.Books, "|- Легкая атлетика. Плавание. Гимнастика. Тяжелая атлетика. Гребля");
            AddCategoryMapping(669, TorznabCatType.Books, "|- Автоспорт. Мотоспорт. Велоспорт");
            AddCategoryMapping(2196, TorznabCatType.Books, "|- Шахматы. Шашки");
            AddCategoryMapping(2056, TorznabCatType.Books, "|- Боевые искусства, единоборства");
            AddCategoryMapping(1436, TorznabCatType.Books, "|- Экстрим (книги)");
            AddCategoryMapping(2191, TorznabCatType.Books, "|- Физкультура, фитнес, бодибилдинг");
            AddCategoryMapping(2477, TorznabCatType.Books, "|- Спортивная пресса");
            AddCategoryMapping(1680, TorznabCatType.Books, "Гуманитарные науки");
            AddCategoryMapping(1684, TorznabCatType.Books, "|- Искусствоведение. Культурология");
            AddCategoryMapping(2446, TorznabCatType.Books, "|- Фольклор. Эпос. Мифология");
            AddCategoryMapping(2524, TorznabCatType.Books, "|- Литературоведение");
            AddCategoryMapping(2525, TorznabCatType.Books, "|- Лингвистика");
            AddCategoryMapping(995, TorznabCatType.Books, "|- Философия");
            AddCategoryMapping(2022, TorznabCatType.Books, "|- Политология");
            AddCategoryMapping(2471, TorznabCatType.Books, "|- Социология");
            AddCategoryMapping(2375, TorznabCatType.Books, "|- Публицистика, журналистика");
            AddCategoryMapping(764, TorznabCatType.Books, "|- Бизнес, менеджмент");
            AddCategoryMapping(1685, TorznabCatType.Books, "|- Маркетинг");
            AddCategoryMapping(1688, TorznabCatType.Books, "|- Экономика");
            AddCategoryMapping(2472, TorznabCatType.Books, "|- Финансы");
            AddCategoryMapping(1687, TorznabCatType.Books, "|- Юридические науки. Право. Криминалистика");
            AddCategoryMapping(2020, TorznabCatType.Books, "Исторические науки");
            AddCategoryMapping(1349, TorznabCatType.Books, "|- Методология и философия исторической науки");
            AddCategoryMapping(1967, TorznabCatType.Books, "|- Исторические источники (книги, периодика)");
            AddCategoryMapping(1341, TorznabCatType.Books, "|- Исторические источники (документы)");
            AddCategoryMapping(2049, TorznabCatType.Books, "|- Исторические персоны");
            AddCategoryMapping(1681, TorznabCatType.Books, "|- Альтернативные исторические теории");
            AddCategoryMapping(2319, TorznabCatType.Books, "|- Археология");
            AddCategoryMapping(2434, TorznabCatType.Books, "|- Древний мир. Античность");
            AddCategoryMapping(1683, TorznabCatType.Books, "|- Средние века");
            AddCategoryMapping(2444, TorznabCatType.Books, "|- История Нового и Новейшего времени");
            AddCategoryMapping(2427, TorznabCatType.Books, "|- История Европы");
            AddCategoryMapping(2452, TorznabCatType.Books, "|- История Азии и Африки");
            AddCategoryMapping(2445, TorznabCatType.Books, "|- История Америки, Австралии, Океании");
            AddCategoryMapping(2435, TorznabCatType.Books, "|- История России");
            AddCategoryMapping(667, TorznabCatType.Books, "|- История России до 1917 года");
            AddCategoryMapping(2436, TorznabCatType.Books, "|- Эпоха СССР");
            AddCategoryMapping(1335, TorznabCatType.Books, "|- История России после 1991 года");
            AddCategoryMapping(2453, TorznabCatType.Books, "|- История стран бывшего СССР");
            AddCategoryMapping(2320, TorznabCatType.Books, "|- Этнография, антропология");
            AddCategoryMapping(1801, TorznabCatType.Books, "|- Международные отношения. Дипломатия");
            AddCategoryMapping(2023, TorznabCatType.BooksTechnical, "Точные, естественные и инженерные науки");
            AddCategoryMapping(2024, TorznabCatType.BooksTechnical, "|- Авиация / Космонавтика");
            AddCategoryMapping(2026, TorznabCatType.BooksTechnical, "|- Физика");
            AddCategoryMapping(2192, TorznabCatType.BooksTechnical, "|- Астрономия");
            AddCategoryMapping(2027, TorznabCatType.BooksTechnical, "|- Биология / Экология");
            AddCategoryMapping(295, TorznabCatType.BooksTechnical, "|- Химия / Биохимия");
            AddCategoryMapping(2028, TorznabCatType.BooksTechnical, "|- Математика");
            AddCategoryMapping(2029, TorznabCatType.BooksTechnical, "|- География / Геология / Геодезия");
            AddCategoryMapping(1325, TorznabCatType.BooksTechnical, "|- Электроника / Радио");
            AddCategoryMapping(2386, TorznabCatType.BooksTechnical, "|- Схемы и сервис-мануалы (оригинальная документация)");
            AddCategoryMapping(2031, TorznabCatType.BooksTechnical, "|- Архитектура / Строительство / Инженерные сети / Ландшафтный дизайн");
            AddCategoryMapping(2030, TorznabCatType.BooksTechnical, "|- Машиностроение");
            AddCategoryMapping(2526, TorznabCatType.BooksTechnical, "|- Сварка / Пайка / Неразрушающий контроль");
            AddCategoryMapping(2527, TorznabCatType.BooksTechnical, "|- Автоматизация / Робототехника");
            AddCategoryMapping(2254, TorznabCatType.BooksTechnical, "|- Металлургия / Материаловедение");
            AddCategoryMapping(2376, TorznabCatType.BooksTechnical, "|- Механика, сопротивление материалов");
            AddCategoryMapping(2054, TorznabCatType.BooksTechnical, "|- Энергетика / электротехника");
            AddCategoryMapping(770, TorznabCatType.BooksTechnical, "|- Нефтяная, газовая и химическая промышленность");
            AddCategoryMapping(2476, TorznabCatType.BooksTechnical, "|- Сельское хозяйство и пищевая промышленность");
            AddCategoryMapping(2494, TorznabCatType.BooksTechnical, "|- Железнодорожное дело");
            AddCategoryMapping(1528, TorznabCatType.BooksTechnical, "|- Нормативная документация");
            AddCategoryMapping(2032, TorznabCatType.BooksTechnical, "|- Журналы: научные, научно-популярные, радио и др.");
            AddCategoryMapping(919, TorznabCatType.Books, "Ноты и Музыкальная литература");
            AddCategoryMapping(944, TorznabCatType.Books, "|- Академическая музыка (Ноты и Media CD)");
            AddCategoryMapping(980, TorznabCatType.Books, "|- Другие направления (Ноты, табулатуры)");
            AddCategoryMapping(946, TorznabCatType.Books, "|- Самоучители и Школы");
            AddCategoryMapping(977, TorznabCatType.Books, "|- Песенники (Songbooks)");
            AddCategoryMapping(2074, TorznabCatType.Books, "|- Музыкальная литература и Теория");
            AddCategoryMapping(2349, TorznabCatType.Books, "|- Музыкальные журналы");
            AddCategoryMapping(768, TorznabCatType.Books, "Военное дело");
            AddCategoryMapping(2099, TorznabCatType.Books, "|- Милитария");
            AddCategoryMapping(2021, TorznabCatType.Books, "|- Военная история");
            AddCategoryMapping(2437, TorznabCatType.Books, "|- История Второй мировой войны");
            AddCategoryMapping(1337, TorznabCatType.Books, "|- Биографии и мемуары военных деятелей");
            AddCategoryMapping(1447, TorznabCatType.Books, "|- Военная техника");
            AddCategoryMapping(2468, TorznabCatType.Books, "|- Стрелковое оружие");
            AddCategoryMapping(2469, TorznabCatType.Books, "|- Учебно-методическая литература");
            AddCategoryMapping(2470, TorznabCatType.Books, "|- Спецслужбы мира");
            AddCategoryMapping(1686, TorznabCatType.Books, "Вера и религия");
            AddCategoryMapping(2215, TorznabCatType.Books, "|- Христианство");
            AddCategoryMapping(2216, TorznabCatType.Books, "|- Ислам");
            AddCategoryMapping(2217, TorznabCatType.Books, "|- Религии Индии, Тибета и Восточной Азии / Иудаизм");
            AddCategoryMapping(2218, TorznabCatType.Books, "|- Нетрадиционные религиозные, духовные и мистические учения");
            AddCategoryMapping(2252, TorznabCatType.Books, "|- Религиоведение. История Религии");
            AddCategoryMapping(2543, TorznabCatType.Books, "|- Атеизм. Научный атеизм");
            AddCategoryMapping(767, TorznabCatType.Books, "Психология");
            AddCategoryMapping(2515, TorznabCatType.Books, "|- Общая и прикладная психология");
            AddCategoryMapping(2516, TorznabCatType.Books, "|- Психотерапия и консультирование");
            AddCategoryMapping(2517, TorznabCatType.Books, "|- Психодиагностика и психокоррекция");
            AddCategoryMapping(2518, TorznabCatType.Books, "|- Социальная психология и психология отношений");
            AddCategoryMapping(2519, TorznabCatType.Books, "|- Тренинг и коучинг");
            AddCategoryMapping(2520, TorznabCatType.Books, "|- Саморазвитие и самосовершенствование");
            AddCategoryMapping(1696, TorznabCatType.Books, "|- Популярная психология");
            AddCategoryMapping(2253, TorznabCatType.Books, "|- Сексология. Взаимоотношения полов (18+)");
            AddCategoryMapping(2033, TorznabCatType.Books, "Коллекционирование, увлечения и хобби");
            AddCategoryMapping(1412, TorznabCatType.Books, "|- Коллекционирование и вспомогательные ист. дисциплины");
            AddCategoryMapping(1446, TorznabCatType.Books, "|- Вышивание");
            AddCategoryMapping(753, TorznabCatType.Books, "|- Вязание");
            AddCategoryMapping(2037, TorznabCatType.Books, "|- Шитье, пэчворк");
            AddCategoryMapping(2224, TorznabCatType.Books, "|- Кружевоплетение");
            AddCategoryMapping(2194, TorznabCatType.Books, "|- Бисероплетение. Ювелирика. Украшения из проволоки.");
            AddCategoryMapping(2418, TorznabCatType.Books, "|- Бумажный арт");
            AddCategoryMapping(1410, TorznabCatType.Books, "|- Другие виды декоративно-прикладного искусства");
            AddCategoryMapping(2034, TorznabCatType.Books, "|- Домашние питомцы и аквариумистика");
            AddCategoryMapping(2433, TorznabCatType.Books, "|- Охота и рыбалка");
            AddCategoryMapping(1961, TorznabCatType.Books, "|- Кулинария (книги)");
            AddCategoryMapping(2432, TorznabCatType.Books, "|- Кулинария (газеты и журналы)");
            AddCategoryMapping(565, TorznabCatType.Books, "|- Моделизм");
            AddCategoryMapping(1523, TorznabCatType.Books, "|- Приусадебное хозяйство / Цветоводство");
            AddCategoryMapping(1575, TorznabCatType.Books, "|- Ремонт, частное строительство, дизайн интерьеров");
            AddCategoryMapping(1520, TorznabCatType.Books, "|- Деревообработка");
            AddCategoryMapping(2424, TorznabCatType.Books, "|- Настольные игры");
            AddCategoryMapping(769, TorznabCatType.Books, "|- Прочие хобби и игры");
            AddCategoryMapping(2038, TorznabCatType.Books, "Художественная литература");
            AddCategoryMapping(2043, TorznabCatType.Books, "|- Русская литература");
            AddCategoryMapping(2042, TorznabCatType.Books, "|- Зарубежная литература (до 1900 г.)");
            AddCategoryMapping(2041, TorznabCatType.Books, "|- Зарубежная литература (XX и XXI век)");
            AddCategoryMapping(2044, TorznabCatType.Books, "|- Детектив, боевик");
            AddCategoryMapping(2039, TorznabCatType.Books, "|- Женский роман");
            AddCategoryMapping(2045, TorznabCatType.Books, "|- Отечественная фантастика / фэнтези / мистика");
            AddCategoryMapping(2080, TorznabCatType.Books, "|- Зарубежная фантастика / фэнтези / мистика");
            AddCategoryMapping(2047, TorznabCatType.Books, "|- Приключения");
            AddCategoryMapping(2193, TorznabCatType.Books, "|- Литературные журналы");
            AddCategoryMapping(1037, TorznabCatType.Books, "|- Самиздат и книги, изданные за счет авторов");
            AddCategoryMapping(1418, TorznabCatType.BooksTechnical, "Компьютерная литература");
            AddCategoryMapping(1422, TorznabCatType.BooksTechnical, "|- Программы от Microsoft");
            AddCategoryMapping(1423, TorznabCatType.BooksTechnical, "|- Другие программы");
            AddCategoryMapping(1424, TorznabCatType.BooksTechnical, "|- Mac OS; Linux, FreeBSD и прочие *NIX");
            AddCategoryMapping(1445, TorznabCatType.BooksTechnical, "|- СУБД");
            AddCategoryMapping(1425, TorznabCatType.BooksTechnical, "|- Веб-дизайн и программирование");
            AddCategoryMapping(1426, TorznabCatType.BooksTechnical, "|- Программирование (книги)");
            AddCategoryMapping(1428, TorznabCatType.BooksTechnical, "|- Графика, обработка видео");
            AddCategoryMapping(1429, TorznabCatType.BooksTechnical, "|- Сети / VoIP");
            AddCategoryMapping(1430, TorznabCatType.BooksTechnical, "|- Хакинг и безопасность");
            AddCategoryMapping(1431, TorznabCatType.BooksTechnical, "|- Железо (книги о ПК)");
            AddCategoryMapping(1433, TorznabCatType.BooksTechnical, "|- Инженерные и научные программы (книги)");
            AddCategoryMapping(1432, TorznabCatType.BooksTechnical, "|- Компьютерные журналы и приложения к ним");
            AddCategoryMapping(2202, TorznabCatType.BooksTechnical, "|- Дисковые приложения к игровым журналам");
            AddCategoryMapping(862, TorznabCatType.BooksComics, "Комиксы, манга, ранобэ");
            AddCategoryMapping(2461, TorznabCatType.BooksComics, "|- Комиксы на русском языке");
            AddCategoryMapping(2462, TorznabCatType.BooksComics, "|- Комиксы издательства Marvel");
            AddCategoryMapping(2463, TorznabCatType.BooksComics, "|- Комиксы издательства DC");
            AddCategoryMapping(2464, TorznabCatType.BooksComics, "|- Комиксы других издательств");
            AddCategoryMapping(2473, TorznabCatType.BooksComics, "|- Комиксы на других языках");
            AddCategoryMapping(281, TorznabCatType.BooksComics, "|- Манга (на русском языке)");
            AddCategoryMapping(2465, TorznabCatType.BooksComics, "|- Манга (на иностранных языках)");
            AddCategoryMapping(2458, TorznabCatType.BooksComics, "|- Ранобэ");
            AddCategoryMapping(2048, TorznabCatType.BooksOther, "Коллекции книг и библиотеки");
            AddCategoryMapping(1238, TorznabCatType.BooksOther, "|- Библиотеки (зеркала сетевых библиотек/коллекций)");
            AddCategoryMapping(2055, TorznabCatType.BooksOther, "|- Тематические коллекции (подборки)");
            AddCategoryMapping(754, TorznabCatType.BooksOther, "|- Многопредметные коллекции (подборки)");
            AddCategoryMapping(2114, TorznabCatType.BooksEBook, "Мультимедийные и интерактивные издания");
            AddCategoryMapping(2438, TorznabCatType.BooksEBook, "|- Мультимедийные энциклопедии");
            AddCategoryMapping(2439, TorznabCatType.BooksEBook, "|- Интерактивные обучающие и развивающие материалы");
            AddCategoryMapping(2440, TorznabCatType.BooksEBook, "|- Обучающие издания для детей");
            AddCategoryMapping(2441, TorznabCatType.BooksEBook, "|- Кулинария. Цветоводство. Домоводство");
            AddCategoryMapping(2442, TorznabCatType.BooksEBook, "|- Культура. Искусство. История");
            AddCategoryMapping(2125, TorznabCatType.Books, "Медицина и здоровье");
            AddCategoryMapping(2133, TorznabCatType.Books, "|- Клиническая медицина до 1980 г.");
            AddCategoryMapping(2130, TorznabCatType.Books, "|- Клиническая медицина с 1980 по 2000 г.");
            AddCategoryMapping(2313, TorznabCatType.Books, "|- Клиническая медицина после 2000 г.");
            AddCategoryMapping(2528, TorznabCatType.Books, "|- Научная медицинская периодика (газеты и журналы)");
            AddCategoryMapping(2129, TorznabCatType.Books, "|- Медико-биологические науки");
            AddCategoryMapping(2141, TorznabCatType.Books, "|- Фармация и фармакология");
            AddCategoryMapping(2314, TorznabCatType.Books, "|- Популярная медицинская периодика (газеты и журналы)");
            AddCategoryMapping(2132, TorznabCatType.Books, "|- Нетрадиционная, народная медицина и популярные книги о здоровье");
            AddCategoryMapping(2131, TorznabCatType.Books, "|- Ветеринария, разное");
            AddCategoryMapping(2315, TorznabCatType.Books, "|- Тематические коллекции книг");
            AddCategoryMapping(2362, TorznabCatType.BooksEBook, "Иностранные языки для взрослых");
            AddCategoryMapping(1265, TorznabCatType.BooksEBook, "|- Английский язык (для взрослых)");
            AddCategoryMapping(1266, TorznabCatType.BooksEBook, "|- Немецкий язык");
            AddCategoryMapping(1267, TorznabCatType.BooksEBook, "|- Французский язык");
            AddCategoryMapping(1358, TorznabCatType.BooksEBook, "|- Испанский язык");
            AddCategoryMapping(2363, TorznabCatType.BooksEBook, "|- Итальянский язык");
            AddCategoryMapping(734, TorznabCatType.BooksEBook, "|- Финский язык");
            AddCategoryMapping(1268, TorznabCatType.BooksEBook, "|- Другие европейские языки");
            AddCategoryMapping(1673, TorznabCatType.BooksEBook, "|- Арабский язык");
            AddCategoryMapping(1269, TorznabCatType.BooksEBook, "|- Китайский язык");
            AddCategoryMapping(1270, TorznabCatType.BooksEBook, "|- Японский язык");
            AddCategoryMapping(1275, TorznabCatType.BooksEBook, "|- Другие восточные языки");
            AddCategoryMapping(2364, TorznabCatType.BooksEBook, "|- Русский язык как иностранный");
            AddCategoryMapping(1276, TorznabCatType.BooksEBook, "|- Мультиязычные сборники и курсы");
            AddCategoryMapping(2094, TorznabCatType.BooksEBook, "|- LIM-курсы");
            AddCategoryMapping(1274, TorznabCatType.BooksEBook, "|- Разное (иностранные языки)");
            AddCategoryMapping(1264, TorznabCatType.BooksEBook, "Иностранные языки для детей");
            AddCategoryMapping(2358, TorznabCatType.BooksEBook, "|- Английский язык (для детей)");
            AddCategoryMapping(2359, TorznabCatType.BooksEBook, "|- Другие европейские языки (для детей)");
            AddCategoryMapping(2360, TorznabCatType.BooksEBook, "|- Восточные языки (для детей)");
            AddCategoryMapping(2361, TorznabCatType.BooksEBook, "|- Школьные учебники, ЕГЭ");
            AddCategoryMapping(2057, TorznabCatType.BooksEBook, "Художественная литература (ин.языки)");
            AddCategoryMapping(2355, TorznabCatType.BooksEBook, "|- Художественная литература на английском языке");
            AddCategoryMapping(2474, TorznabCatType.BooksEBook, "|- Художественная литература на французском языке");
            AddCategoryMapping(2356, TorznabCatType.BooksEBook, "|- Художественная литература на других европейских языках");
            AddCategoryMapping(2357, TorznabCatType.BooksEBook, "|- Художественная литература на восточных языках");
            AddCategoryMapping(2413, TorznabCatType.AudioAudiobook, "Аудиокниги на иностранных языках");
            AddCategoryMapping(1501, TorznabCatType.AudioAudiobook, "|- Аудиокниги на английском языке");
            AddCategoryMapping(1580, TorznabCatType.AudioAudiobook, "|- Аудиокниги на немецком языке");
            AddCategoryMapping(525, TorznabCatType.AudioAudiobook, "|- Аудиокниги на других иностранных языках");
            AddCategoryMapping(610, TorznabCatType.BooksOther, "Видеоуроки и обучающие интерактивные DVD");
            AddCategoryMapping(1568, TorznabCatType.BooksOther, "|- Кулинария");
            AddCategoryMapping(1542, TorznabCatType.BooksOther, "|- Спорт");
            AddCategoryMapping(2335, TorznabCatType.BooksOther, "|- Фитнес - Кардио-Силовые Тренировки");
            AddCategoryMapping(1544, TorznabCatType.BooksOther, "|- Фитнес - Разум и Тело");
            AddCategoryMapping(1546, TorznabCatType.BooksOther, "|- Бодибилдинг");
            AddCategoryMapping(1549, TorznabCatType.BooksOther, "|- Оздоровительные практики");
            AddCategoryMapping(1597, TorznabCatType.BooksOther, "|- Йога");
            AddCategoryMapping(1552, TorznabCatType.BooksOther, "|- Видео- и фотосъёмка");
            AddCategoryMapping(1550, TorznabCatType.BooksOther, "|- Уход за собой");
            AddCategoryMapping(1553, TorznabCatType.BooksOther, "|- Рисование");
            AddCategoryMapping(1554, TorznabCatType.BooksOther, "|- Игра на гитаре");
            AddCategoryMapping(617, TorznabCatType.BooksOther, "|- Ударные инструменты");
            AddCategoryMapping(1555, TorznabCatType.BooksOther, "|- Другие музыкальные инструменты");
            AddCategoryMapping(2017, TorznabCatType.BooksOther, "|- Игра на бас-гитаре");
            AddCategoryMapping(1257, TorznabCatType.BooksOther, "|- Бальные танцы");
            AddCategoryMapping(1258, TorznabCatType.BooksOther, "|- Танец живота");
            AddCategoryMapping(2208, TorznabCatType.BooksOther, "|- Уличные и клубные танцы");
            AddCategoryMapping(677, TorznabCatType.BooksOther, "|- Танцы, разное");
            AddCategoryMapping(1255, TorznabCatType.BooksOther, "|- Охота");
            AddCategoryMapping(1479, TorznabCatType.BooksOther, "|- Рыболовство и подводная охота");
            AddCategoryMapping(1261, TorznabCatType.BooksOther, "|- Фокусы и трюки");
            AddCategoryMapping(614, TorznabCatType.BooksOther, "|- Образование");
            AddCategoryMapping(1583, TorznabCatType.BooksOther, "|- Финансы");
            AddCategoryMapping(1259, TorznabCatType.BooksOther, "|- Продажи, бизнес");
            AddCategoryMapping(2065, TorznabCatType.BooksOther, "|- Беременность, роды, материнство");
            AddCategoryMapping(1254, TorznabCatType.BooksOther, "|- Учебные видео для детей");
            AddCategoryMapping(1260, TorznabCatType.BooksOther, "|- Психология");
            AddCategoryMapping(2209, TorznabCatType.BooksOther, "|- Эзотерика, саморазвитие");
            AddCategoryMapping(2210, TorznabCatType.BooksOther, "|- Пикап, знакомства");
            AddCategoryMapping(1547, TorznabCatType.BooksOther, "|- Строительство, ремонт и дизайн");
            AddCategoryMapping(1548, TorznabCatType.BooksOther, "|- Дерево- и металлообработка");
            AddCategoryMapping(2211, TorznabCatType.BooksOther, "|- Растения и животные");
            AddCategoryMapping(1596, TorznabCatType.BooksOther, "|- Хобби и рукоделие");
            AddCategoryMapping(2135, TorznabCatType.BooksOther, "|- Медицина и стоматология");
            AddCategoryMapping(2140, TorznabCatType.BooksOther, "|- Психотерапия и клиническая психология");
            AddCategoryMapping(2136, TorznabCatType.BooksOther, "|- Массаж");
            AddCategoryMapping(2138, TorznabCatType.BooksOther, "|- Здоровье");
            AddCategoryMapping(615, TorznabCatType.BooksOther, "|- Разное");
            AddCategoryMapping(1581, TorznabCatType.BooksOther, "Боевые искусства (Видеоуроки)");
            AddCategoryMapping(1590, TorznabCatType.BooksOther, "|- Айкидо и айки-дзюцу");
            AddCategoryMapping(1587, TorznabCatType.BooksOther, "|- Вин чун");
            AddCategoryMapping(1594, TorznabCatType.BooksOther, "|- Джиу-джитсу");
            AddCategoryMapping(1591, TorznabCatType.BooksOther, "|- Дзюдо и самбо");
            AddCategoryMapping(1588, TorznabCatType.BooksOther, "|- Каратэ");
            AddCategoryMapping(1585, TorznabCatType.BooksOther, "|- Работа с оружием");
            AddCategoryMapping(1586, TorznabCatType.BooksOther, "|- Русский стиль");
            AddCategoryMapping(2078, TorznabCatType.BooksOther, "|- Рукопашный бой");
            AddCategoryMapping(1929, TorznabCatType.BooksOther, "|- Смешанные стили");
            AddCategoryMapping(1593, TorznabCatType.BooksOther, "|- Ударные стили");
            AddCategoryMapping(1592, TorznabCatType.BooksOther, "|- Ушу");
            AddCategoryMapping(1595, TorznabCatType.BooksOther, "|- Разное");
            AddCategoryMapping(1556, TorznabCatType.BooksTechnical, "Компьютерные видеоуроки и обучающие интерактивные DVD");
            AddCategoryMapping(1560, TorznabCatType.BooksTechnical, "|- Компьютерные сети и безопасность");
            AddCategoryMapping(1991, TorznabCatType.BooksTechnical, "|- Devops");
            AddCategoryMapping(1561, TorznabCatType.BooksTechnical, "|- ОС и серверные программы Microsoft");
            AddCategoryMapping(1653, TorznabCatType.BooksTechnical, "|- Офисные программы Microsoft");
            AddCategoryMapping(1570, TorznabCatType.BooksTechnical, "|- ОС и программы семейства UNIX");
            AddCategoryMapping(1654, TorznabCatType.BooksTechnical, "|- Adobe Photoshop");
            AddCategoryMapping(1655, TorznabCatType.BooksTechnical, "|- Autodesk Maya");
            AddCategoryMapping(1656, TorznabCatType.BooksTechnical, "|- Autodesk 3ds Max");
            AddCategoryMapping(1930, TorznabCatType.BooksTechnical, "|- Autodesk Softimage (XSI)");
            AddCategoryMapping(1931, TorznabCatType.BooksTechnical, "|- ZBrush");
            AddCategoryMapping(1932, TorznabCatType.BooksTechnical, "|- Flash, Flex и ActionScript");
            AddCategoryMapping(1562, TorznabCatType.BooksTechnical, "|- 2D-графика");
            AddCategoryMapping(1563, TorznabCatType.BooksTechnical, "|- 3D-графика");
            AddCategoryMapping(1626, TorznabCatType.BooksTechnical, "|- Инженерные и научные программы (видеоуроки)");
            AddCategoryMapping(1564, TorznabCatType.BooksTechnical, "|- Web-дизайн");
            AddCategoryMapping(1545, TorznabCatType.BooksTechnical, "|- WEB, SMM, SEO, интернет-маркетинг");
            AddCategoryMapping(1565, TorznabCatType.BooksTechnical, "|- Программирование (видеоуроки)");
            AddCategoryMapping(1559, TorznabCatType.BooksTechnical, "|- Программы для Mac OS");
            AddCategoryMapping(1566, TorznabCatType.BooksTechnical, "|- Работа с видео");
            AddCategoryMapping(1573, TorznabCatType.BooksTechnical, "|- Работа со звуком");
            AddCategoryMapping(1567, TorznabCatType.BooksTechnical, "|- Разное (Компьютерные видеоуроки)");
            AddCategoryMapping(2326, TorznabCatType.AudioAudiobook, "Радиоспектакли, история, мемуары");
            AddCategoryMapping(574, TorznabCatType.AudioAudiobook, "|- [Аудио] Радиоспектакли и литературные чтения");
            AddCategoryMapping(1036, TorznabCatType.AudioAudiobook, "|- [Аудио] Жизнь замечательных людей");
            AddCategoryMapping(400, TorznabCatType.AudioAudiobook, "|- [Аудио] История, культурология, философия");
            AddCategoryMapping(2389, TorznabCatType.AudioAudiobook, "Фантастика, фэнтези, мистика, ужасы, фанфики");
            AddCategoryMapping(2388, TorznabCatType.AudioAudiobook, "|- [Аудио] Зарубежная фантастика, фэнтези, мистика, ужасы, фанфики");
            AddCategoryMapping(2387, TorznabCatType.AudioAudiobook, "|- [Аудио] Российская фантастика, фэнтези, мистика, ужасы, фанфики");
            AddCategoryMapping(661, TorznabCatType.AudioAudiobook, "|- [Аудио] Любовно-фантастический роман");
            AddCategoryMapping(2348, TorznabCatType.AudioAudiobook, "|- [Аудио] Сборники/разное Фантастика, фэнтези, мистика, ужасы, фанфи..");
            AddCategoryMapping(2327, TorznabCatType.AudioAudiobook, "Художественная литература");
            AddCategoryMapping(695, TorznabCatType.AudioAudiobook, "|- [Аудио] Поэзия");
            AddCategoryMapping(399, TorznabCatType.AudioAudiobook, "|- [Аудио] Зарубежная литература");
            AddCategoryMapping(402, TorznabCatType.AudioAudiobook, "|- [Аудио] Русская литература");
            AddCategoryMapping(467, TorznabCatType.AudioAudiobook, "|- [Аудио] Современные любовные романы");
            AddCategoryMapping(490, TorznabCatType.AudioAudiobook, "|- [Аудио] Детская литература");
            AddCategoryMapping(499, TorznabCatType.AudioAudiobook, "|- [Аудио] Зарубежные детективы, приключения, триллеры, боевики");
            AddCategoryMapping(2137, TorznabCatType.AudioAudiobook, "|- [Аудио] Российские детективы, приключения, триллеры, боевики");
            AddCategoryMapping(2127, TorznabCatType.AudioAudiobook, "|- [Аудио] Азиатская подростковая литература, ранобэ, веб-новеллы");
            AddCategoryMapping(2324, TorznabCatType.AudioAudiobook, "Религии");
            AddCategoryMapping(2325, TorznabCatType.AudioAudiobook, "|- [Аудио] Православие");
            AddCategoryMapping(2342, TorznabCatType.AudioAudiobook, "|- [Аудио] Ислам");
            AddCategoryMapping(530, TorznabCatType.AudioAudiobook, "|- [Аудио] Другие традиционные религии");
            AddCategoryMapping(2152, TorznabCatType.AudioAudiobook, "|- [Аудио] Нетрадиционные религиозно-философские учения");
            AddCategoryMapping(2328, TorznabCatType.AudioAudiobook, "Прочая литература");
            AddCategoryMapping(1350, TorznabCatType.AudioAudiobook, "|- [Аудио] Книги по медицине");
            AddCategoryMapping(403, TorznabCatType.AudioAudiobook, "|- [Аудио] Учебная и научно-популярная литература");
            AddCategoryMapping(1279, TorznabCatType.AudioAudiobook, "|- [Аудио] lossless-аудиокниги");
            AddCategoryMapping(716, TorznabCatType.AudioAudiobook, "|- [Аудио] Бизнес");
            AddCategoryMapping(2165, TorznabCatType.AudioAudiobook, "|- [Аудио] Разное");
            AddCategoryMapping(401, TorznabCatType.AudioAudiobook, "|- [Аудио] Некондиционные раздачи");
            AddCategoryMapping(1964, TorznabCatType.Books, "Ремонт и эксплуатация транспортных средств");
            AddCategoryMapping(1973, TorznabCatType.Books, "|- Оригинальные каталоги по подбору запчастей");
            AddCategoryMapping(1974, TorznabCatType.Books, "|- Неоригинальные каталоги по подбору запчастей");
            AddCategoryMapping(1975, TorznabCatType.Books, "|- Программы по диагностике и ремонту");
            AddCategoryMapping(1976, TorznabCatType.Books, "|- Тюнинг, чиптюнинг, настройка");
            AddCategoryMapping(1977, TorznabCatType.Books, "|- Книги по ремонту/обслуживанию/эксплуатации ТС");
            AddCategoryMapping(1203, TorznabCatType.Books, "|- Мультимедийки по ремонту/обслуживанию/эксплуатации ТС");
            AddCategoryMapping(1978, TorznabCatType.Books, "|- Учет, утилиты и прочее");
            AddCategoryMapping(1979, TorznabCatType.Books, "|- Виртуальная автошкола");
            AddCategoryMapping(1980, TorznabCatType.Books, "|- Видеоуроки по вождению транспортных средств");
            AddCategoryMapping(1981, TorznabCatType.Books, "|- Видеоуроки по ремонту транспортных средств");
            AddCategoryMapping(1970, TorznabCatType.Books, "|- Журналы по авто/мото");
            AddCategoryMapping(334, TorznabCatType.Books, "|- Водный транспорт");
            AddCategoryMapping(1202, TorznabCatType.TVDocumentary, "Фильмы и передачи по авто/мото");
            AddCategoryMapping(1985, TorznabCatType.TVDocumentary, "|- Документальные/познавательные фильмы");
            AddCategoryMapping(1982, TorznabCatType.TVOther, "|- Развлекательные передачи");
            AddCategoryMapping(2151, TorznabCatType.TVDocumentary, "|- Top Gear/Топ Гир");
            AddCategoryMapping(1983, TorznabCatType.TVDocumentary, "|- Тест драйв/Обзоры/Автосалоны");
            AddCategoryMapping(1984, TorznabCatType.TVDocumentary, "|- Тюнинг/форсаж");
            AddCategoryMapping(409, TorznabCatType.Audio, "Классическая и современная академическая музыка");
            AddCategoryMapping(560, TorznabCatType.AudioLossless, "|- Полные собрания сочинений и многодисковые издания (lossless)");
            AddCategoryMapping(794, TorznabCatType.AudioLossless, "|- Опера (lossless)");
            AddCategoryMapping(556, TorznabCatType.AudioLossless, "|- Вокальная музыка (lossless)");
            AddCategoryMapping(2307, TorznabCatType.AudioLossless, "|- Хоровая музыка (lossless)");
            AddCategoryMapping(557, TorznabCatType.AudioLossless, "|- Оркестровая музыка (lossless)");
            AddCategoryMapping(2308, TorznabCatType.AudioLossless, "|- Концерт для инструмента с оркестром (lossless)");
            AddCategoryMapping(558, TorznabCatType.AudioLossless, "|- Камерная инструментальная музыка (lossless)");
            AddCategoryMapping(793, TorznabCatType.AudioLossless, "|- Сольная инструментальная музыка (lossless)");
            AddCategoryMapping(1395, TorznabCatType.AudioLossless, "|- Духовные песнопения и музыка (lossless)");
            AddCategoryMapping(1396, TorznabCatType.AudioMP3, "|- Духовные песнопения и музыка (lossy)");
            AddCategoryMapping(436, TorznabCatType.AudioMP3, "|- Полные собрания сочинений и многодисковые издания (lossy)");
            AddCategoryMapping(2309, TorznabCatType.AudioMP3, "|- Вокальная и хоровая музыка (lossy)");
            AddCategoryMapping(2310, TorznabCatType.AudioMP3, "|- Оркестровая музыка (lossy)");
            AddCategoryMapping(2311, TorznabCatType.AudioMP3, "|- Камерная и сольная инструментальная музыка (lossy)");
            AddCategoryMapping(969, TorznabCatType.Audio, "|- Классика в современной обработке, Classical Crossover (lossy и los..");
            AddCategoryMapping(1125, TorznabCatType.Audio, "Фольклор, Народная и Этническая музыка");
            AddCategoryMapping(1130, TorznabCatType.AudioMP3, "|- Восточноевропейский фолк (lossy)");
            AddCategoryMapping(1131, TorznabCatType.AudioLossless, "|- Восточноевропейский фолк (lossless)");
            AddCategoryMapping(1132, TorznabCatType.AudioMP3, "|- Западноевропейский фолк (lossy)");
            AddCategoryMapping(1133, TorznabCatType.AudioLossless, "|- Западноевропейский фолк (lossless)");
            AddCategoryMapping(2084, TorznabCatType.Audio, "|- Klezmer и Еврейский фольклор (lossy и lossless)");
            AddCategoryMapping(1128, TorznabCatType.AudioMP3, "|- Этническая музыка Сибири, Средней и Восточной Азии (lossy)");
            AddCategoryMapping(1129, TorznabCatType.AudioLossless, "|- Этническая музыка Сибири, Средней и Восточной Азии (lossless)");
            AddCategoryMapping(1856, TorznabCatType.AudioMP3, "|- Этническая музыка Индии (lossy)");
            AddCategoryMapping(2430, TorznabCatType.AudioLossless, "|- Этническая музыка Индии (lossless)");
            AddCategoryMapping(1283, TorznabCatType.AudioMP3, "|- Этническая музыка Африки и Ближнего Востока (lossy)");
            AddCategoryMapping(2085, TorznabCatType.AudioLossless, "|- Этническая музыка Африки и Ближнего Востока (lossless)");
            AddCategoryMapping(1282, TorznabCatType.Audio, "|- Фольклорная, Народная, Эстрадная музыка Кавказа и Закавказья (loss..");
            AddCategoryMapping(1284, TorznabCatType.AudioMP3, "|- Этническая музыка Северной и Южной Америки (lossy)");
            AddCategoryMapping(1285, TorznabCatType.AudioLossless, "|- Этническая музыка Северной и Южной Америки (lossless)");
            AddCategoryMapping(1138, TorznabCatType.Audio, "|- Этническая музыка Австралии, Тихого и Индийского океанов (lossy и ..");
            AddCategoryMapping(1136, TorznabCatType.AudioMP3, "|- Country, Bluegrass (lossy)");
            AddCategoryMapping(1137, TorznabCatType.AudioLossless, "|- Country, Bluegrass (lossless)");
            AddCategoryMapping(1849, TorznabCatType.Audio, "New Age, Relax, Meditative & Flamenco");
            AddCategoryMapping(1126, TorznabCatType.AudioMP3, "|- New Age & Meditative (lossy)");
            AddCategoryMapping(1127, TorznabCatType.AudioLossless, "|- New Age & Meditative (lossless)");
            AddCategoryMapping(1134, TorznabCatType.AudioMP3, "|- Фламенко и акустическая гитара (lossy)");
            AddCategoryMapping(1135, TorznabCatType.AudioLossless, "|- Фламенко и акустическая гитара (lossless)");
            AddCategoryMapping(2018, TorznabCatType.Audio, "|- Музыка для бальных танцев (lossy и lossless)");
            AddCategoryMapping(855, TorznabCatType.Audio, "|- Звуки природы");
            AddCategoryMapping(408, TorznabCatType.Audio, "Рэп, Хип-Хоп, R'n'B");
            AddCategoryMapping(441, TorznabCatType.AudioMP3, "|- Отечественный Рэп, Хип-Хоп (lossy)");
            AddCategoryMapping(1173, TorznabCatType.AudioMP3, "|- Отечественный R'n'B (lossy)");
            AddCategoryMapping(1486, TorznabCatType.AudioLossless, "|- Отечественный Рэп, Хип-Хоп, R'n'B (lossless)");
            AddCategoryMapping(1172, TorznabCatType.AudioMP3, "|- Зарубежный R'n'B (lossy)");
            AddCategoryMapping(446, TorznabCatType.AudioMP3, "|- Зарубежный Рэп, Хип-Хоп (lossy)");
            AddCategoryMapping(909, TorznabCatType.AudioLossless, "|- Зарубежный Рэп, Хип-Хоп (lossless)");
            AddCategoryMapping(1665, TorznabCatType.AudioLossless, "|- Зарубежный R'n'B (lossless)");
            AddCategoryMapping(1760, TorznabCatType.Audio, "Reggae, Ska, Dub");
            AddCategoryMapping(1764, TorznabCatType.Audio, "|- Rocksteady, Early Reggae, Ska-Jazz, Trad.Ska (lossy и lossless)");
            AddCategoryMapping(1767, TorznabCatType.AudioMP3, "|- 3rd Wave Ska (lossy)");
            AddCategoryMapping(1769, TorznabCatType.AudioMP3, "|- Ska-Punk, Ska-Core (lossy)");
            AddCategoryMapping(1765, TorznabCatType.AudioMP3, "|- Reggae (lossy)");
            AddCategoryMapping(1771, TorznabCatType.AudioMP3, "|- Dub (lossy)");
            AddCategoryMapping(1770, TorznabCatType.AudioMP3, "|- Dancehall, Raggamuffin (lossy)");
            AddCategoryMapping(1768, TorznabCatType.AudioLossless, "|- Reggae, Dancehall, Dub (lossless)");
            AddCategoryMapping(1774, TorznabCatType.AudioLossless, "|- Ska, Ska-Punk, Ska-Jazz (lossless)");
            AddCategoryMapping(1772, TorznabCatType.Audio, "|- Отечественный Reggae, Dub (lossy и lossless)");
            AddCategoryMapping(1773, TorznabCatType.Audio, "|- Отечественная Ska музыка (lossy и lossless)");
            AddCategoryMapping(2233, TorznabCatType.Audio, "|- Reggae, Ska, Dub (компиляции) (lossy и lossless)");
            AddCategoryMapping(416, TorznabCatType.Audio, "Саундтреки, караоке и мюзиклы");
            AddCategoryMapping(2377, TorznabCatType.AudioVideo, "|- Караоке (видео)");
            AddCategoryMapping(468, TorznabCatType.Audio, "|- Минусовки (lossy и lossless)");
            AddCategoryMapping(691, TorznabCatType.AudioLossless, "|- Саундтреки к отечественным фильмам (lossless)");
            AddCategoryMapping(469, TorznabCatType.AudioMP3, "|- Саундтреки к отечественным фильмам (lossy)");
            AddCategoryMapping(786, TorznabCatType.AudioLossless, "|- Саундтреки к зарубежным фильмам (lossless)");
            AddCategoryMapping(785, TorznabCatType.AudioMP3, "|- Саундтреки к зарубежным фильмам (lossy)");
            AddCategoryMapping(1631, TorznabCatType.AudioLossless, "|- Саундтреки к сериалам (lossless)");
            AddCategoryMapping(1499, TorznabCatType.AudioMP3, "|- Саундтреки к сериалам (lossy)");
            AddCategoryMapping(715, TorznabCatType.Audio, "|- Саундтреки к мультфильмам (lossy и lossless)");
            AddCategoryMapping(1388, TorznabCatType.AudioLossless, "|- Саундтреки к аниме (lossless)");
            AddCategoryMapping(282, TorznabCatType.AudioMP3, "|- Саундтреки к аниме (lossy)");
            AddCategoryMapping(796, TorznabCatType.AudioMP3, "|- Неофициальные саундтреки к фильмам и сериалам (lossy)");
            AddCategoryMapping(784, TorznabCatType.AudioLossless, "|- Саундтреки к играм (lossless)");
            AddCategoryMapping(783, TorznabCatType.AudioMP3, "|- Саундтреки к играм (lossy)");
            AddCategoryMapping(2331, TorznabCatType.AudioMP3, "|- Неофициальные саундтреки к играм (lossy)");
            AddCategoryMapping(2431, TorznabCatType.Audio, "|- Аранжировки музыки из игр (lossy и lossless)");
            AddCategoryMapping(880, TorznabCatType.Audio, "|- Мюзикл (lossy и lossless)");
            AddCategoryMapping(1215, TorznabCatType.Audio, "Шансон, Авторская и Военная песня");
            AddCategoryMapping(1220, TorznabCatType.AudioLossless, "|- Отечественный шансон (lossless)");
            AddCategoryMapping(1221, TorznabCatType.AudioMP3, "|- Отечественный шансон (lossy)");
            AddCategoryMapping(1334, TorznabCatType.AudioMP3, "|- Сборники отечественного шансона (lossy)");
            AddCategoryMapping(1216, TorznabCatType.AudioLossless, "|- Военная песня, марши (lossless)");
            AddCategoryMapping(1223, TorznabCatType.AudioMP3, "|- Военная песня, марши (lossy)");
            AddCategoryMapping(1224, TorznabCatType.AudioLossless, "|- Авторская песня (lossless)");
            AddCategoryMapping(1225, TorznabCatType.AudioMP3, "|- Авторская песня (lossy)");
            AddCategoryMapping(1226, TorznabCatType.Audio, "|- Менестрели и ролевики (lossy и lossless)");
            AddCategoryMapping(1842, TorznabCatType.AudioLossless, "Label Packs (lossless)");
            AddCategoryMapping(1648, TorznabCatType.AudioMP3, "Label packs, Scene packs (lossy)");
            AddCategoryMapping(2495, TorznabCatType.Audio, "Отечественная поп-музыка");
            AddCategoryMapping(424, TorznabCatType.AudioMP3, "|- Отечественная поп-музыка (lossy)");
            AddCategoryMapping(1361, TorznabCatType.AudioMP3, "|- Отечественная поп-музыка (сборники) (lossy)");
            AddCategoryMapping(425, TorznabCatType.AudioLossless, "|- Отечественная поп-музыка (lossless)");
            AddCategoryMapping(1635, TorznabCatType.AudioMP3, "|- Советская эстрада, ретро, романсы (lossy)");
            AddCategoryMapping(1634, TorznabCatType.AudioLossless, "|- Советская эстрада, ретро, романсы (lossless)");
            AddCategoryMapping(2497, TorznabCatType.Audio, "Зарубежная поп-музыка");
            AddCategoryMapping(428, TorznabCatType.AudioMP3, "|- Зарубежная поп-музыка (lossy)");
            AddCategoryMapping(1362, TorznabCatType.AudioMP3, "|- Зарубежная поп-музыка (сборники) (lossy)");
            AddCategoryMapping(429, TorznabCatType.AudioLossless, "|- Зарубежная поп-музыка (lossless)");
            AddCategoryMapping(735, TorznabCatType.AudioMP3, "|- Итальянская поп-музыка (lossy)");
            AddCategoryMapping(1753, TorznabCatType.AudioLossless, "|- Итальянская поп-музыка (lossless)");
            AddCategoryMapping(2232, TorznabCatType.AudioMP3, "|- Латиноамериканская поп-музыка (lossy)");
            AddCategoryMapping(714, TorznabCatType.AudioLossless, "|- Латиноамериканская поп-музыка (lossless)");
            AddCategoryMapping(1331, TorznabCatType.AudioMP3, "|- Восточноазиатская поп-музыка (lossy)");
            AddCategoryMapping(1330, TorznabCatType.AudioLossless, "|- Восточноазиатская поп-музыка (lossless)");
            AddCategoryMapping(1219, TorznabCatType.AudioMP3, "|- Зарубежный шансон (lossy)");
            AddCategoryMapping(1452, TorznabCatType.AudioLossless, "|- Зарубежный шансон (lossless)");
            AddCategoryMapping(2275, TorznabCatType.AudioMP3, "|- Easy Listening, Instrumental Pop (lossy)");
            AddCategoryMapping(2270, TorznabCatType.AudioLossless, "|- Easy Listening, Instrumental Pop (lossless)");
            AddCategoryMapping(1351, TorznabCatType.Audio, "|- Сборники песен для детей (lossy и lossless)");
            AddCategoryMapping(2499, TorznabCatType.Audio, "Eurodance, Disco, Hi-NRG");
            AddCategoryMapping(2503, TorznabCatType.AudioMP3, "|- Eurodance, Euro-House, Technopop (lossy)");
            AddCategoryMapping(2504, TorznabCatType.AudioMP3, "|- Eurodance, Euro-House, Technopop (сборники) (lossy)");
            AddCategoryMapping(2502, TorznabCatType.AudioLossless, "|- Eurodance, Euro-House, Technopop (lossless)");
            AddCategoryMapping(2501, TorznabCatType.AudioMP3, "|- Disco, Italo-Disco, Euro-Disco, Hi-NRG (lossy)");
            AddCategoryMapping(2505, TorznabCatType.AudioMP3, "|- Disco, Italo-Disco, Euro-Disco, Hi-NRG (сборники) (lossy)");
            AddCategoryMapping(2500, TorznabCatType.AudioLossless, "|- Disco, Italo-Disco, Euro-Disco, Hi-NRG (lossless)");
            AddCategoryMapping(2267, TorznabCatType.Audio, "Зарубежный джаз");
            AddCategoryMapping(2277, TorznabCatType.AudioLossless, "|- Early Jazz, Swing, Gypsy (lossless)");
            AddCategoryMapping(2278, TorznabCatType.AudioLossless, "|- Bop (lossless)");
            AddCategoryMapping(2279, TorznabCatType.AudioLossless, "|- Mainstream Jazz, Cool (lossless)");
            AddCategoryMapping(2280, TorznabCatType.AudioLossless, "|- Jazz Fusion (lossless)");
            AddCategoryMapping(2281, TorznabCatType.AudioLossless, "|- World Fusion, Ethnic Jazz (lossless)");
            AddCategoryMapping(2282, TorznabCatType.AudioLossless, "|- Avant-Garde Jazz, Free Improvisation (lossless)");
            AddCategoryMapping(2353, TorznabCatType.AudioLossless, "|- Modern Creative, Third Stream (lossless)");
            AddCategoryMapping(2284, TorznabCatType.AudioLossless, "|- Smooth, Jazz-Pop (lossless)");
            AddCategoryMapping(2285, TorznabCatType.AudioLossless, "|- Vocal Jazz (lossless)");
            AddCategoryMapping(2283, TorznabCatType.AudioLossless, "|- Funk, Soul, R&B (lossless)");
            AddCategoryMapping(2286, TorznabCatType.AudioLossless, "|- Сборники зарубежного джаза (lossless)");
            AddCategoryMapping(2287, TorznabCatType.AudioMP3, "|- Зарубежный джаз (lossy)");
            AddCategoryMapping(2268, TorznabCatType.Audio, "Зарубежный блюз");
            AddCategoryMapping(2293, TorznabCatType.AudioLossless, "|- Blues (Texas, Chicago, Modern and Others) (lossless)");
            AddCategoryMapping(2292, TorznabCatType.AudioLossless, "|- Blues-rock (lossless)");
            AddCategoryMapping(2290, TorznabCatType.AudioLossless, "|- Roots, Pre-War Blues, Early R&B, Gospel (lossless)");
            AddCategoryMapping(2289, TorznabCatType.AudioLossless, "|- Зарубежный блюз (сборники; Tribute VA) (lossless)");
            AddCategoryMapping(2288, TorznabCatType.AudioMP3, "|- Зарубежный блюз (lossy)");
            AddCategoryMapping(2269, TorznabCatType.Audio, "Отечественный джаз и блюз");
            AddCategoryMapping(2297, TorznabCatType.AudioLossless, "|- Отечественный джаз (lossless)");
            AddCategoryMapping(2295, TorznabCatType.AudioMP3, "|- Отечественный джаз (lossy)");
            AddCategoryMapping(2296, TorznabCatType.AudioLossless, "|- Отечественный блюз (lossless)");
            AddCategoryMapping(2298, TorznabCatType.AudioMP3, "|- Отечественный блюз (lossy)");
            AddCategoryMapping(1698, TorznabCatType.Audio, "Зарубежный Rock");
            AddCategoryMapping(1702, TorznabCatType.AudioLossless, "|- Classic Rock & Hard Rock (lossless)");
            AddCategoryMapping(1703, TorznabCatType.AudioMP3, "|- Classic Rock & Hard Rock (lossy)");
            AddCategoryMapping(1704, TorznabCatType.AudioLossless, "|- Progressive & Art-Rock (lossless)");
            AddCategoryMapping(1705, TorznabCatType.AudioMP3, "|- Progressive & Art-Rock (lossy)");
            AddCategoryMapping(1706, TorznabCatType.AudioLossless, "|- Folk-Rock (lossless)");
            AddCategoryMapping(1707, TorznabCatType.AudioMP3, "|- Folk-Rock (lossy)");
            AddCategoryMapping(2329, TorznabCatType.AudioLossless, "|- AOR (Melodic Hard Rock, Arena rock) (lossless)");
            AddCategoryMapping(2330, TorznabCatType.AudioMP3, "|- AOR (Melodic Hard Rock, Arena rock) (lossy)");
            AddCategoryMapping(1708, TorznabCatType.AudioLossless, "|- Pop-Rock & Soft Rock (lossless)");
            AddCategoryMapping(1709, TorznabCatType.AudioMP3, "|- Pop-Rock & Soft Rock (lossy)");
            AddCategoryMapping(1710, TorznabCatType.AudioLossless, "|- Instrumental Guitar Rock (lossless)");
            AddCategoryMapping(1711, TorznabCatType.AudioMP3, "|- Instrumental Guitar Rock (lossy)");
            AddCategoryMapping(1712, TorznabCatType.AudioLossless, "|- Rockabilly, Psychobilly, Rock'n'Roll (lossless)");
            AddCategoryMapping(1713, TorznabCatType.AudioMP3, "|- Rockabilly, Psychobilly, Rock'n'Roll (lossy)");
            AddCategoryMapping(731, TorznabCatType.AudioLossless, "|- Сборники зарубежного рока (lossless)");
            AddCategoryMapping(1799, TorznabCatType.AudioMP3, "|- Сборники зарубежного рока (lossy)");
            AddCategoryMapping(1714, TorznabCatType.AudioLossless, "|- Восточноазиатский рок (lossless)");
            AddCategoryMapping(1715, TorznabCatType.AudioMP3, "|- Восточноазиатский рок (lossy)");
            AddCategoryMapping(1716, TorznabCatType.Audio, "Зарубежный Metal");
            AddCategoryMapping(1796, TorznabCatType.AudioLossless, "|- Avant-garde, Experimental Metal (lossless)");
            AddCategoryMapping(1797, TorznabCatType.AudioMP3, "|- Avant-garde, Experimental Metal (lossy)");
            AddCategoryMapping(1719, TorznabCatType.AudioLossless, "|- Black (lossless)");
            AddCategoryMapping(1778, TorznabCatType.AudioMP3, "|- Black (lossy)");
            AddCategoryMapping(1779, TorznabCatType.AudioLossless, "|- Death, Doom (lossless)");
            AddCategoryMapping(1780, TorznabCatType.AudioMP3, "|- Death, Doom (lossy)");
            AddCategoryMapping(1720, TorznabCatType.AudioLossless, "|- Folk, Pagan, Viking (lossless)");
            AddCategoryMapping(798, TorznabCatType.AudioMP3, "|- Folk, Pagan, Viking (lossy)");
            AddCategoryMapping(1724, TorznabCatType.AudioLossless, "|- Gothic Metal (lossless)");
            AddCategoryMapping(1725, TorznabCatType.AudioMP3, "|- Gothic Metal (lossy)");
            AddCategoryMapping(1730, TorznabCatType.AudioLossless, "|- Grind, Brutal Death (lossless)");
            AddCategoryMapping(1731, TorznabCatType.AudioMP3, "|- Grind, Brutal Death (lossy)");
            AddCategoryMapping(1726, TorznabCatType.AudioLossless, "|- Heavy, Power, Progressive (lossless)");
            AddCategoryMapping(1727, TorznabCatType.AudioMP3, "|- Heavy, Power, Progressive (lossy)");
            AddCategoryMapping(1815, TorznabCatType.AudioLossless, "|- Sludge, Stoner, Post-Metal (lossless)");
            AddCategoryMapping(1816, TorznabCatType.AudioMP3, "|- Sludge, Stoner, Post-Metal (lossy)");
            AddCategoryMapping(1728, TorznabCatType.AudioLossless, "|- Thrash, Speed (lossless)");
            AddCategoryMapping(1729, TorznabCatType.AudioMP3, "|- Thrash, Speed (lossy)");
            AddCategoryMapping(2230, TorznabCatType.AudioLossless, "|- Сборники (lossless)");
            AddCategoryMapping(2231, TorznabCatType.AudioMP3, "|- Сборники (lossy)");
            AddCategoryMapping(1732, TorznabCatType.Audio, "Зарубежные Alternative, Punk, Independent");
            AddCategoryMapping(1736, TorznabCatType.AudioLossless, "|- Alternative & Nu-metal (lossless)");
            AddCategoryMapping(1737, TorznabCatType.AudioMP3, "|- Alternative & Nu-metal (lossy)");
            AddCategoryMapping(1738, TorznabCatType.AudioLossless, "|- Punk (lossless)");
            AddCategoryMapping(1739, TorznabCatType.AudioMP3, "|- Punk (lossy)");
            AddCategoryMapping(1740, TorznabCatType.AudioLossless, "|- Hardcore (lossless)");
            AddCategoryMapping(1741, TorznabCatType.AudioMP3, "|- Hardcore (lossy)");
            AddCategoryMapping(1742, TorznabCatType.AudioLossless, "|- Indie, Post-Rock & Post-Punk (lossless)");
            AddCategoryMapping(1743, TorznabCatType.AudioMP3, "|- Indie, Post-Rock & Post-Punk (lossy)");
            AddCategoryMapping(1744, TorznabCatType.AudioLossless, "|- Industrial & Post-industrial (lossless)");
            AddCategoryMapping(1745, TorznabCatType.AudioMP3, "|- Industrial & Post-industrial (lossy)");
            AddCategoryMapping(1746, TorznabCatType.AudioLossless, "|- Emocore, Post-hardcore, Metalcore (lossless)");
            AddCategoryMapping(1747, TorznabCatType.AudioMP3, "|- Emocore, Post-hardcore, Metalcore (lossy)");
            AddCategoryMapping(1748, TorznabCatType.AudioLossless, "|- Gothic Rock & Dark Folk (lossless)");
            AddCategoryMapping(1749, TorznabCatType.AudioMP3, "|- Gothic Rock & Dark Folk (lossy)");
            AddCategoryMapping(2175, TorznabCatType.AudioLossless, "|- Avant-garde, Experimental Rock (lossless)");
            AddCategoryMapping(2174, TorznabCatType.AudioMP3, "|- Avant-garde, Experimental Rock (lossy)");
            AddCategoryMapping(722, TorznabCatType.Audio, "Отечественный Rock, Metal");
            AddCategoryMapping(737, TorznabCatType.AudioLossless, "|- Rock (lossless)");
            AddCategoryMapping(738, TorznabCatType.AudioMP3, "|- Rock (lossy)");
            AddCategoryMapping(464, TorznabCatType.AudioLossless, "|- Alternative, Punk, Independent (lossless)");
            AddCategoryMapping(463, TorznabCatType.AudioMP3, "|- Alternative, Punk, Independent (lossy)");
            AddCategoryMapping(739, TorznabCatType.AudioLossless, "|- Metal (lossless)");
            AddCategoryMapping(740, TorznabCatType.AudioMP3, "|- Metal (lossy)");
            AddCategoryMapping(951, TorznabCatType.AudioLossless, "|- Rock на языках народов xUSSR (lossless)");
            AddCategoryMapping(952, TorznabCatType.AudioMP3, "|- Rock на языках народов xUSSR (lossy)");
            AddCategoryMapping(1821, TorznabCatType.Audio, "Trance, Goa Trance, Psy-Trance, PsyChill, Ambient, Dub");
            AddCategoryMapping(1844, TorznabCatType.AudioLossless, "|- Goa Trance, Psy-Trance (lossless)");
            AddCategoryMapping(1822, TorznabCatType.AudioMP3, "|- Goa Trance, Psy-Trance (lossy)");
            AddCategoryMapping(1894, TorznabCatType.AudioLossless, "|- PsyChill, Ambient, Dub (lossless)");
            AddCategoryMapping(1895, TorznabCatType.AudioMP3, "|- PsyChill, Ambient, Dub (lossy)");
            AddCategoryMapping(460, TorznabCatType.AudioMP3, "|- Goa Trance, Psy-Trance, PsyChill, Ambient, Dub (Live Sets, Mixes) ..");
            AddCategoryMapping(1818, TorznabCatType.AudioLossless, "|- Trance (lossless)");
            AddCategoryMapping(1819, TorznabCatType.AudioMP3, "|- Trance (lossy)");
            AddCategoryMapping(1847, TorznabCatType.AudioMP3, "|- Trance (Singles, EPs) (lossy)");
            AddCategoryMapping(1824, TorznabCatType.AudioMP3, "|- Trance (Radioshows, Podcasts, Live Sets, Mixes) (lossy)");
            AddCategoryMapping(1807, TorznabCatType.Audio, "House, Techno, Hardcore, Hardstyle, Jumpstyle");
            AddCategoryMapping(1829, TorznabCatType.AudioLossless, "|- Hardcore, Hardstyle, Jumpstyle (lossless)");
            AddCategoryMapping(1830, TorznabCatType.AudioMP3, "|- Hardcore, Hardstyle, Jumpstyle (lossy)");
            AddCategoryMapping(1831, TorznabCatType.AudioMP3, "|- Hardcore, Hardstyle, Jumpstyle (vinyl, web)");
            AddCategoryMapping(1857, TorznabCatType.AudioLossless, "|- House (lossless)");
            AddCategoryMapping(1859, TorznabCatType.AudioMP3, "|- House (Radioshow, Podcast, Liveset, Mixes)");
            AddCategoryMapping(1858, TorznabCatType.AudioMP3, "|- House (lossy)");
            AddCategoryMapping(840, TorznabCatType.AudioMP3, "|- House (Проморелизы, сборники) (lossy)");
            AddCategoryMapping(1860, TorznabCatType.AudioMP3, "|- House (Singles, EPs) (lossy)");
            AddCategoryMapping(1825, TorznabCatType.AudioLossless, "|- Techno (lossless)");
            AddCategoryMapping(1826, TorznabCatType.AudioMP3, "|- Techno (lossy)");
            AddCategoryMapping(1827, TorznabCatType.AudioMP3, "|- Techno (Radioshows, Podcasts, Livesets, Mixes)");
            AddCategoryMapping(1828, TorznabCatType.AudioMP3, "|- Techno (Singles, EPs) (lossy)");
            AddCategoryMapping(1808, TorznabCatType.Audio, "Drum & Bass, Jungle, Breakbeat, Dubstep, IDM, Electro");
            AddCategoryMapping(797, TorznabCatType.AudioLossless, "|- Electro, Electro-Freestyle, Nu Electro (lossless)");
            AddCategoryMapping(1805, TorznabCatType.AudioMP3, "|- Electro, Electro-Freestyle, Nu Electro (lossy)");
            AddCategoryMapping(1832, TorznabCatType.AudioLossless, "|- Drum & Bass, Jungle (lossless)");
            AddCategoryMapping(1833, TorznabCatType.AudioMP3, "|- Drum & Bass, Jungle (lossy)");
            AddCategoryMapping(1834, TorznabCatType.AudioMP3, "|- Drum & Bass, Jungle (Radioshows, Podcasts, Livesets, Mixes)");
            AddCategoryMapping(1836, TorznabCatType.AudioLossless, "|- Breakbeat (lossless)");
            AddCategoryMapping(1837, TorznabCatType.AudioMP3, "|- Breakbeat (lossy)");
            AddCategoryMapping(1839, TorznabCatType.AudioLossless, "|- Dubstep (lossless)");
            AddCategoryMapping(454, TorznabCatType.AudioMP3, "|- Dubstep (lossy)");
            AddCategoryMapping(1838, TorznabCatType.AudioMP3, "|- Breakbeat, Dubstep (Radioshows, Podcasts, Livesets, Mixes)");
            AddCategoryMapping(1840, TorznabCatType.AudioLossless, "|- IDM (lossless)");
            AddCategoryMapping(1841, TorznabCatType.AudioMP3, "|- IDM (lossy)");
            AddCategoryMapping(2229, TorznabCatType.AudioMP3, "|- IDM Discography & Collections (lossy)");
            AddCategoryMapping(1809, TorznabCatType.Audio, "Chillout, Lounge, Downtempo, Trip-Hop");
            AddCategoryMapping(1861, TorznabCatType.AudioLossless, "|- Chillout, Lounge, Downtempo (lossless)");
            AddCategoryMapping(1862, TorznabCatType.AudioMP3, "|- Chillout, Lounge, Downtempo (lossy)");
            AddCategoryMapping(1947, TorznabCatType.AudioLossless, "|- Nu Jazz, Acid Jazz, Future Jazz (lossless)");
            AddCategoryMapping(1946, TorznabCatType.AudioMP3, "|- Nu Jazz, Acid Jazz, Future Jazz (lossy)");
            AddCategoryMapping(1945, TorznabCatType.AudioLossless, "|- Trip Hop, Abstract Hip-Hop (lossless)");
            AddCategoryMapping(1944, TorznabCatType.AudioMP3, "|- Trip Hop, Abstract Hip-Hop (lossy)");
            AddCategoryMapping(1810, TorznabCatType.Audio, "Traditional Electronic, Ambient, Modern Classical, Electroacoustic, Ex..");
            AddCategoryMapping(1864, TorznabCatType.AudioLossless, "|- Traditional Electronic, Ambient (lossless)");
            AddCategoryMapping(1865, TorznabCatType.AudioMP3, "|- Traditional Electronic, Ambient (lossy)");
            AddCategoryMapping(1871, TorznabCatType.AudioLossless, "|- Modern Classical, Electroacoustic (lossless)");
            AddCategoryMapping(1867, TorznabCatType.AudioMP3, "|- Modern Classical, Electroacoustic (lossy)");
            AddCategoryMapping(1869, TorznabCatType.AudioLossless, "|- Experimental (lossless)");
            AddCategoryMapping(1873, TorznabCatType.AudioMP3, "|- Experimental (lossy)");
            AddCategoryMapping(1907, TorznabCatType.Audio, "|- 8-bit, Chiptune (lossy & lossless)");
            AddCategoryMapping(1811, TorznabCatType.Audio, "Industrial, Noise, EBM, Dark Electro, Aggrotech, Synthpop, New Wave");
            AddCategoryMapping(1868, TorznabCatType.AudioLossless, "|- EBM, Dark Electro, Aggrotech (lossless)");
            AddCategoryMapping(1875, TorznabCatType.AudioMP3, "|- EBM, Dark Electro, Aggrotech (lossy)");
            AddCategoryMapping(1877, TorznabCatType.AudioLossless, "|- Industrial, Noise (lossless)");
            AddCategoryMapping(1878, TorznabCatType.AudioMP3, "|- Industrial, Noise (lossy)");
            AddCategoryMapping(1880, TorznabCatType.AudioLossless, "|- Synthpop, Futurepop, New Wave, Electropop (lossless)");
            AddCategoryMapping(1881, TorznabCatType.AudioMP3, "|- Synthpop, Futurepop, New Wave, Electropop (lossy)");
            AddCategoryMapping(466, TorznabCatType.AudioLossless, "|- Synthwave, Spacesynth, Dreamwave, Retrowave, Outrun (lossless)");
            AddCategoryMapping(465, TorznabCatType.AudioMP3, "|- Synthwave, Spacesynth, Dreamwave, Retrowave, Outrun (lossy)");
            AddCategoryMapping(1866, TorznabCatType.AudioLossless, "|- Darkwave, Neoclassical, Ethereal, Dungeon Synth (lossless)");
            AddCategoryMapping(406, TorznabCatType.AudioMP3, "|- Darkwave, Neoclassical, Ethereal, Dungeon Synth (lossy)");
            AddCategoryMapping(1299, TorznabCatType.Audio, "Hi-Res stereo и многоканальная музыка");
            AddCategoryMapping(1884, TorznabCatType.Audio, "|- Классика и классика в современной обработке (Hi-Res stereo)");
            AddCategoryMapping(1164, TorznabCatType.Audio, "|- Классика и классика в современной обработке (многоканальная музыка..");
            AddCategoryMapping(2513, TorznabCatType.Audio, "|- New Age, Relax, Meditative & Flamenco (Hi-Res stereo и многоканаль..");
            AddCategoryMapping(1397, TorznabCatType.Audio, "|- Саундтреки (Hi-Res stereo и многоканальная музыка)");
            AddCategoryMapping(2512, TorznabCatType.Audio, "|- Музыка разных жанров (Hi-Res stereo и многоканальная музыка)");
            AddCategoryMapping(1885, TorznabCatType.Audio, "|- Поп-музыка (Hi-Res stereo)");
            AddCategoryMapping(1163, TorznabCatType.Audio, "|- Поп-музыка (многоканальная музыка)");
            AddCategoryMapping(2302, TorznabCatType.Audio, "|- Джаз и Блюз (Hi-Res stereo)");
            AddCategoryMapping(2303, TorznabCatType.Audio, "|- Джаз и Блюз (многоканальная музыка)");
            AddCategoryMapping(1755, TorznabCatType.Audio, "|- Рок-музыка (Hi-Res stereo)");
            AddCategoryMapping(1757, TorznabCatType.Audio, "|- Рок-музыка (многоканальная музыка)");
            AddCategoryMapping(1893, TorznabCatType.Audio, "|- Электронная музыка (Hi-Res stereo)");
            AddCategoryMapping(1890, TorznabCatType.Audio, "|- Электронная музыка (многоканальная музыка)");
            AddCategoryMapping(2219, TorznabCatType.Audio, "Оцифровки с аналоговых носителей");
            AddCategoryMapping(1660, TorznabCatType.Audio, "|- Классика и классика в современной обработке (оцифровки)");
            AddCategoryMapping(506, TorznabCatType.Audio, "|- Фольклор, народная и этническая музыка (оцифровки)");
            AddCategoryMapping(1835, TorznabCatType.Audio, "|- Rap, Hip-Hop, R'n'B, Reggae, Ska, Dub (оцифровки)");
            AddCategoryMapping(1625, TorznabCatType.Audio, "|- Саундтреки и мюзиклы (оцифровки)");
            AddCategoryMapping(1217, TorznabCatType.Audio, "|- Шансон, авторские, военные песни и марши (оцифровки)");
            AddCategoryMapping(974, TorznabCatType.Audio, "|- Музыка других жанров (оцифровки)");
            AddCategoryMapping(1444, TorznabCatType.Audio, "|- Зарубежная поп-музыка (оцифровки)");
            AddCategoryMapping(2401, TorznabCatType.Audio, "|- Советская эстрада, ретро, романсы (оцифровки)");
            AddCategoryMapping(239, TorznabCatType.Audio, "|- Отечественная поп-музыка (оцифровки)");
            AddCategoryMapping(450, TorznabCatType.Audio, "|- Инструментальная поп-музыка (оцифровки)");
            AddCategoryMapping(2301, TorznabCatType.Audio, "|- Джаз и блюз (оцифровки)");
            AddCategoryMapping(1756, TorznabCatType.Audio, "|- Зарубежная рок-музыка (оцифровки)");
            AddCategoryMapping(1758, TorznabCatType.Audio, "|- Отечественная рок-музыка (оцифровки)");
            AddCategoryMapping(1766, TorznabCatType.Audio, "|- Зарубежный Metal (оцифровки)");
            AddCategoryMapping(1754, TorznabCatType.Audio, "|- Электронная музыка (оцифровки)");
            AddCategoryMapping(860, TorznabCatType.Audio, "Неофициальные конверсии цифровых форматов");
            AddCategoryMapping(453, TorznabCatType.Audio, "|- Конверсии Quadraphonic");
            AddCategoryMapping(1170, TorznabCatType.Audio, "|- Конверсии SACD");
            AddCategoryMapping(1759, TorznabCatType.Audio, "|- Конверсии Blu-Ray, ADVD и DVD-Audio");
            AddCategoryMapping(1852, TorznabCatType.Audio, "|- Апмиксы-Upmixes/Даунмиксы-Downmix");
            AddCategoryMapping(413, TorznabCatType.AudioVideo, "Музыкальное SD видео");
            AddCategoryMapping(445, TorznabCatType.AudioVideo, "|- Классическая и современная академическая музыка (Видео)");
            AddCategoryMapping(702, TorznabCatType.AudioVideo, "|- Опера, Оперетта и Мюзикл (Видео) ");
            AddCategoryMapping(1990, TorznabCatType.AudioVideo, "|- Балет и современная хореография (Видео)");
            AddCategoryMapping(1793, TorznabCatType.AudioVideo, "|- Классика в современной обработке, ical Crossover (Видео)");
            AddCategoryMapping(1141, TorznabCatType.AudioVideo, "|- Фольклор, Народная и Этническая музыка и фламенко (Видео)");
            AddCategoryMapping(1775, TorznabCatType.AudioVideo, "|- New Age, Relax, Meditative, Рэп, Хип-Хоп, R'n'B, Reggae, Ska, Dub .. ");
            AddCategoryMapping(1227, TorznabCatType.AudioVideo, "|- Зарубежный и Отечественный Шансон, Авторская и Военная песня (Виде..");
            AddCategoryMapping(475, TorznabCatType.AudioVideo, "|- Музыка других жанров, Советская эстрада, ретро, романсы (Видео)");
            AddCategoryMapping(1121, TorznabCatType.AudioVideo, "|- Отечественная поп-музыка (Видео)");
            AddCategoryMapping(431, TorznabCatType.AudioVideo, "|- Зарубежная поп-музыка (Видео)");
            AddCategoryMapping(2378, TorznabCatType.AudioVideo, "|- Восточноазиатская поп-музыка (Видео)");
            AddCategoryMapping(2383, TorznabCatType.AudioVideo, "|- Зарубежный шансон (Видео)");
            AddCategoryMapping(2305, TorznabCatType.AudioVideo, "|- Джаз и Блюз (Видео)");
            AddCategoryMapping(1782, TorznabCatType.AudioVideo, "|- Rock (Видео)");
            AddCategoryMapping(1787, TorznabCatType.AudioVideo, "|- Metal (Видео)");
            AddCategoryMapping(1789, TorznabCatType.AudioVideo, "|- Alternative, Punk, Independent (Видео)");
            AddCategoryMapping(1791, TorznabCatType.AudioVideo, "|- Отечественный Рок, Панк, Альтернатива (Видео)");
            AddCategoryMapping(1912, TorznabCatType.AudioVideo, "|- Электронная музыка (Видео)");
            AddCategoryMapping(1189, TorznabCatType.AudioVideo, "|- Документальные фильмы о музыке и музыкантах (Видео)");
            AddCategoryMapping(2403, TorznabCatType.AudioVideo, "Музыкальное DVD видео");
            AddCategoryMapping(984, TorznabCatType.AudioVideo, "|- Классическая и современная академическая музыка (DVD Video)");
            AddCategoryMapping(983, TorznabCatType.AudioVideo, "|- Опера, Оперетта и Мюзикл (DVD видео)");
            AddCategoryMapping(2352, TorznabCatType.AudioVideo, "|- Балет и современная хореография (DVD Video)");
            AddCategoryMapping(2384, TorznabCatType.AudioVideo, "|- Классика в современной обработке, ical Crossover (DVD Video)");
            AddCategoryMapping(1142, TorznabCatType.AudioVideo, "|- Фольклор, Народная и Этническая музыка и Flamenco (DVD Video)");
            AddCategoryMapping(1107, TorznabCatType.AudioVideo, "|- New Age, Relax, Meditative, Рэп, Хип-Хоп, R &#039;n &#039;B, Reggae, Ska, Dub ..");
            AddCategoryMapping(1228, TorznabCatType.AudioVideo, "|- Зарубежный и Отечественный Шансон, Авторская и Военная песня (DVD ..");
            AddCategoryMapping(988, TorznabCatType.AudioVideo, "|- Музыка других жанров, Советская эстрада, ретро, романсы (DVD Video..");
            AddCategoryMapping(1122, TorznabCatType.AudioVideo, "|- Отечественная поп-музыка (DVD Video)");
            AddCategoryMapping(986, TorznabCatType.AudioVideo, "|- Зарубежная Поп-музыка, Eurodance, Disco (DVD Video)");
            AddCategoryMapping(2379, TorznabCatType.AudioVideo, "|- Восточноазиатская поп-музыка (DVD Video)");
            AddCategoryMapping(2088, TorznabCatType.AudioVideo, "|- Разножанровые сборные концерты и сборники видеоклипов (DVD Video)");
            AddCategoryMapping(2304, TorznabCatType.AudioVideo, "|- Джаз и Блюз (DVD Видео)");
            AddCategoryMapping(1783, TorznabCatType.AudioVideo, "|- Зарубежный Rock (DVD Video)");
            AddCategoryMapping(1788, TorznabCatType.AudioVideo, "|- Зарубежный Metal (DVD Video)");
            AddCategoryMapping(1790, TorznabCatType.AudioVideo, "|- Зарубежный Alternative, Punk, Independent (DVD Video)");
            AddCategoryMapping(1792, TorznabCatType.AudioVideo, "|- Отечественный Рок, Метал, Панк, Альтернатива (DVD Video)");
            AddCategoryMapping(1886, TorznabCatType.AudioVideo, "|- Электронная музыка (DVD Video)");
            AddCategoryMapping(2509, TorznabCatType.AudioVideo, "|- Документальные фильмы о музыке и музыкантах (DVD Video)");
            AddCategoryMapping(2507, TorznabCatType.AudioVideo, "Неофициальные DVD видео ");
            AddCategoryMapping(2263, TorznabCatType.AudioVideo, "Классическая музыка, Опера, Балет, Мюзикл (Неофициальные DVD Video)");
            AddCategoryMapping(2511, TorznabCatType.AudioVideo, "Шансон, Авторская песня, Сборные концерты, МДЖ (Неофициальные DVD Video)");
            AddCategoryMapping(2264, TorznabCatType.AudioVideo, "|- Зарубежная и Отечественная Поп-музыка (Неофициальные DVD Video)");
            AddCategoryMapping(2262, TorznabCatType.AudioVideo, "|- Джаз и Блюз (Неофициальные DVD Video)");
            AddCategoryMapping(2261, TorznabCatType.AudioVideo, "|- Зарубежная и Отечественная Рок-музыка (Неофициальные DVD Video)");
            AddCategoryMapping(1887, TorznabCatType.AudioVideo, "|- Электронная музыка (Неофициальные, любительские DVD Video)");
            AddCategoryMapping(2531, TorznabCatType.AudioVideo, "|- Прочие жанры (Неофициальные DVD видео)");
            AddCategoryMapping(2400, TorznabCatType.AudioVideo, "Музыкальное HD видео");
            AddCategoryMapping(1812, TorznabCatType.AudioVideo, "|- Классическая и современная академическая музыка (HD Video)");
            AddCategoryMapping(655, TorznabCatType.AudioVideo, "|- Опера, Оперетта и Мюзикл (HD Видео)");
            AddCategoryMapping(1777, TorznabCatType.AudioVideo, "|- Балет и современная хореография (HD Video)");
            AddCategoryMapping(2530, TorznabCatType.AudioVideo, "|- Фольклор, Народная, Этническая музыка и Flamenco (HD Видео)");
            AddCategoryMapping(2529, TorznabCatType.AudioVideo, "|- New Age, Relax, Meditative, Рэп, Хип-Хоп, R'n'B, Reggae, Ska, Dub ..");
            AddCategoryMapping(1781, TorznabCatType.AudioVideo, "|- Музыка других жанров, Разножанровые сборные концерты (HD видео)");
            AddCategoryMapping(2508, TorznabCatType.AudioVideo, "|- Зарубежная поп-музыка (HD Video)");
            AddCategoryMapping(2426, TorznabCatType.AudioVideo, "|- Отечественная поп-музыка (HD видео)");
            AddCategoryMapping(2351, TorznabCatType.AudioVideo, "|- Восточноазиатская Поп-музыка (HD Video)");
            AddCategoryMapping(2306, TorznabCatType.AudioVideo, "|- Джаз и Блюз (HD Video)");
            AddCategoryMapping(1795, TorznabCatType.AudioVideo, "|- Зарубежный рок (HD Video)");
            AddCategoryMapping(2271, TorznabCatType.AudioVideo, "|- Отечественный рок (HD видео)");
            AddCategoryMapping(1913, TorznabCatType.AudioVideo, "|- Электронная музыка (HD Video)");
            AddCategoryMapping(1784, TorznabCatType.AudioVideo, "|- UHD музыкальное видео");
            AddCategoryMapping(1892, TorznabCatType.AudioVideo, "|- Документальные фильмы о музыке и музыкантах (HD Video)");
            AddCategoryMapping(518, TorznabCatType.AudioVideo, "Некондиционное музыкальное видео (Видео, DVD видео, HD видео)");
            AddCategoryMapping(5, TorznabCatType.PCGames, "Игры для Windows");
            AddCategoryMapping(635, TorznabCatType.PCGames, "|- Горячие Новинки");
            AddCategoryMapping(127, TorznabCatType.PCGames, "|- Аркады");
            AddCategoryMapping(2203, TorznabCatType.PCGames, "|- Файтинги");
            AddCategoryMapping(647, TorznabCatType.PCGames, "|- Экшены от первого лица");
            AddCategoryMapping(646, TorznabCatType.PCGames, "|- Экшены от третьего лица");
            AddCategoryMapping(50, TorznabCatType.PCGames, "|- Хорроры");
            AddCategoryMapping(53, TorznabCatType.PCGames, "|- Приключения и квесты");
            AddCategoryMapping(1008, TorznabCatType.PCGames, "|- Квесты в стиле \"Поиск предметов\"");
            AddCategoryMapping(900, TorznabCatType.PCGames, "|- Визуальные новеллы");
            AddCategoryMapping(128, TorznabCatType.PCGames, "|- Для самых маленьких");
            AddCategoryMapping(2204, TorznabCatType.PCGames, "|- Логические игры");
            AddCategoryMapping(278, TorznabCatType.PCGames, "|- Шахматы");
            AddCategoryMapping(2118, TorznabCatType.PCGames, "|- Многопользовательские игры");
            AddCategoryMapping(52, TorznabCatType.PCGames, "|- Ролевые игры");
            AddCategoryMapping(54, TorznabCatType.PCGames, "|- Симуляторы");
            AddCategoryMapping(51, TorznabCatType.PCGames, "|- Стратегии в реальном времени");
            AddCategoryMapping(2226, TorznabCatType.PCGames, "|- Пошаговые стратегии");
            AddCategoryMapping(2228, TorznabCatType.PCGames, "|- IBM-PC-несовместимые компьютеры");
            AddCategoryMapping(139, TorznabCatType.PCGames, "Прочее для Windows-игр");
            AddCategoryMapping(2478, TorznabCatType.PCGames, "|- Официальные патчи, моды, плагины, дополнения");
            AddCategoryMapping(2480, TorznabCatType.PCGames, "|- Неофициальные модификации, плагины, дополнения");
            AddCategoryMapping(2481, TorznabCatType.PCGames, "|- Русификаторы");
            AddCategoryMapping(2142, TorznabCatType.PCGames, "Прочее для Microsoft Flight Simulator, Prepar3D, X-Plane");
            AddCategoryMapping(2060, TorznabCatType.PCGames, "|- Сценарии, меши и аэропорты для FS2004, FSX, P3D");
            AddCategoryMapping(2145, TorznabCatType.PCGames, "|- Самолёты и вертолёты для FS2004, FSX, P3D");
            AddCategoryMapping(2146, TorznabCatType.PCGames, "|- Миссии, трафик, звуки, паки и утилиты для FS2004, FSX, P3D");
            AddCategoryMapping(2143, TorznabCatType.PCGames, "|- Сценарии, миссии, трафик, звуки, паки и утилиты для X-Plane");
            AddCategoryMapping(2012, TorznabCatType.PCGames, "|- Самолёты и вертолёты для X-Plane");
            AddCategoryMapping(960, TorznabCatType.PCMac, "Игры для Apple Macintosh");
            AddCategoryMapping(537, TorznabCatType.PCMac, "|- Нативные игры для Mac");
            AddCategoryMapping(637, TorznabCatType.PCMac, "|- Портированные игры для Mac");
            AddCategoryMapping(899, TorznabCatType.PCGames, "Игры для Linux");
            AddCategoryMapping(1992, TorznabCatType.PCGames, "|- Нативные игры для Linux");
            AddCategoryMapping(2059, TorznabCatType.PCGames, "|- Портированные игры для Linux");
            AddCategoryMapping(548, TorznabCatType.Console, "Игры для консолей");
            AddCategoryMapping(908, TorznabCatType.Console, "|- PS");
            AddCategoryMapping(357, TorznabCatType.ConsoleOther, "|- PS2");
            AddCategoryMapping(886, TorznabCatType.ConsolePS3, "|- PS3");
            AddCategoryMapping(546, TorznabCatType.Console, "|- Игры PS1, PS2 и PSP для PS3");
            AddCategoryMapping(973, TorznabCatType.ConsolePS4, "|- PS4");
            AddCategoryMapping(1352, TorznabCatType.ConsolePSP, "|- PSP");
            AddCategoryMapping(1116, TorznabCatType.ConsolePSP, "|- Игры PS1 для PSP");
            AddCategoryMapping(595, TorznabCatType.ConsolePSVita, "|- PS Vita");
            AddCategoryMapping(887, TorznabCatType.ConsoleXBox, "|- Original Xbox");
            AddCategoryMapping(510, TorznabCatType.ConsoleXBox360, "|- Xbox 360");
            AddCategoryMapping(773, TorznabCatType.ConsoleWii, "|- Wii/WiiU");
            AddCategoryMapping(774, TorznabCatType.ConsoleNDS, "|- NDS/3DS");
            AddCategoryMapping(1605, TorznabCatType.Console, "|- Switch");
            AddCategoryMapping(968, TorznabCatType.Console, "|- Dreamcast");
            AddCategoryMapping(129, TorznabCatType.Console, "|- Остальные платформы");
            AddCategoryMapping(2185, TorznabCatType.ConsoleOther, "Видео для консолей");
            AddCategoryMapping(2487, TorznabCatType.ConsoleOther, "|- Видео для PS Vita");
            AddCategoryMapping(2182, TorznabCatType.ConsoleOther, "|- Фильмы для PSP");
            AddCategoryMapping(2181, TorznabCatType.ConsoleOther, "|- Сериалы для PSP");
            AddCategoryMapping(2180, TorznabCatType.ConsoleOther, "|- Мультфильмы для PSP");
            AddCategoryMapping(2179, TorznabCatType.ConsoleOther, "|- Дорамы для PSP");
            AddCategoryMapping(2186, TorznabCatType.ConsoleOther, "|- Аниме для PSP");
            AddCategoryMapping(700, TorznabCatType.ConsoleOther, "|- Видео для PSP");
            AddCategoryMapping(1926, TorznabCatType.ConsoleOther, "|- Видео для PS3 и других консолей");
            AddCategoryMapping(650, TorznabCatType.PCMobileOther, "Игры для мобильных устройств");
            AddCategoryMapping(2149, TorznabCatType.PCMobileAndroid, "|- Игры для Android");
            AddCategoryMapping(1001, TorznabCatType.PCMobileOther, "|- Игры для Java");
            AddCategoryMapping(1004, TorznabCatType.PCMobileOther, "|- Игры для Symbian");
            AddCategoryMapping(1002, TorznabCatType.PCMobileOther, "|- Игры для Windows Mobile");
            AddCategoryMapping(2420, TorznabCatType.PCMobileOther, "|- Игры для Windows Phone");
            AddCategoryMapping(240, TorznabCatType.OtherMisc, "Игровое видео");
            AddCategoryMapping(2415, TorznabCatType.OtherMisc, "|- Видеопрохождения игр");
            AddCategoryMapping(1012, TorznabCatType.PC, "Операционные системы от Microsoft");
            AddCategoryMapping(2523, TorznabCatType.PC, "|- Настольные ОС от Microsoft - Windows 8 и далее");
            AddCategoryMapping(2153, TorznabCatType.PC, "|- Настольные ОС от Microsoft - Windows XP - Windows 7");
            AddCategoryMapping(1019, TorznabCatType.PC, "|- Настольные ОС от Microsoft (выпущенные до Windows XP)");
            AddCategoryMapping(1021, TorznabCatType.PC, "|- Серверные ОС от Microsoft");
            AddCategoryMapping(1025, TorznabCatType.PC, "|- Разное (Операционные системы от Microsoft)");
            AddCategoryMapping(1376, TorznabCatType.PC, "Linux, Unix и другие ОС");
            AddCategoryMapping(1379, TorznabCatType.PC, "|- Операционные системы (Linux, Unix)");
            AddCategoryMapping(1381, TorznabCatType.PC, "|- Программное обеспечение (Linux, Unix)");
            AddCategoryMapping(1473, TorznabCatType.PC, "|- Другие ОС и ПО под них");
            AddCategoryMapping(1195, TorznabCatType.PC, "Тестовые диски для настройки аудио/видео аппаратуры");
            AddCategoryMapping(1013, TorznabCatType.PC, "Системные программы");
            AddCategoryMapping(1028, TorznabCatType.PC, "|- Работа с жёстким диском");
            AddCategoryMapping(1029, TorznabCatType.PC, "|- Резервное копирование");
            AddCategoryMapping(1030, TorznabCatType.PC, "|- Архиваторы и файловые менеджеры");
            AddCategoryMapping(1031, TorznabCatType.PC, "|- Программы для настройки и оптимизации ОС");
            AddCategoryMapping(1032, TorznabCatType.PC, "|- Сервисное обслуживание компьютера");
            AddCategoryMapping(1033, TorznabCatType.PC, "|- Работа с носителями информации");
            AddCategoryMapping(1034, TorznabCatType.PC, "|- Информация и диагностика");
            AddCategoryMapping(1066, TorznabCatType.PC, "|- Программы для интернет и сетей");
            AddCategoryMapping(1035, TorznabCatType.PC, "|- ПО для защиты компьютера (Антивирусное ПО, Фаерволлы)");
            AddCategoryMapping(1038, TorznabCatType.PC, "|- Анти-шпионы и анти-трояны");
            AddCategoryMapping(1039, TorznabCatType.PC, "|- Программы для защиты информации");
            AddCategoryMapping(1536, TorznabCatType.PC, "|- Драйверы и прошивки");
            AddCategoryMapping(1051, TorznabCatType.PC, "|- Оригинальные диски к компьютерам и комплектующим");
            AddCategoryMapping(1040, TorznabCatType.PC, "|- Серверное ПО для Windows");
            AddCategoryMapping(1041, TorznabCatType.PC, "|- Изменение интерфейса ОС Windows");
            AddCategoryMapping(1636, TorznabCatType.PC, "|- Скринсейверы");
            AddCategoryMapping(1042, TorznabCatType.PC, "|- Разное (Системные программы под Windows)");
            AddCategoryMapping(1014, TorznabCatType.PC, "Системы для бизнеса, офиса, научной и проектной работы");
            AddCategoryMapping(2134, TorznabCatType.PC, "|- Медицина - интерактивный софт");
            AddCategoryMapping(1060, TorznabCatType.PC, "|- Всё для дома: кройка, шитьё, кулинария");
            AddCategoryMapping(1061, TorznabCatType.PC, "|- Офисные системы");
            AddCategoryMapping(1062, TorznabCatType.PC, "|- Системы для бизнеса");
            AddCategoryMapping(1067, TorznabCatType.PC, "|- Распознавание текста, звука и синтез речи");
            AddCategoryMapping(1086, TorznabCatType.PC, "|- Работа с PDF и DjVu");
            AddCategoryMapping(1068, TorznabCatType.PC, "|- Словари, переводчики");
            AddCategoryMapping(1063, TorznabCatType.PC, "|- Системы для научной работы");
            AddCategoryMapping(1087, TorznabCatType.PC, "|- САПР (общие и машиностроительные)");
            AddCategoryMapping(1192, TorznabCatType.PC, "|- САПР (электроника, автоматика, ГАП)");
            AddCategoryMapping(1088, TorznabCatType.PC, "|- Программы для архитекторов и строителей");
            AddCategoryMapping(1193, TorznabCatType.PC, "|- Библиотеки и проекты для архитекторов и дизайнеров интерьеров");
            AddCategoryMapping(1071, TorznabCatType.PC, "|- Прочие справочные системы");
            AddCategoryMapping(1073, TorznabCatType.PC, "|- Разное (Системы для бизнеса, офиса, научной и проектной работы)");
            AddCategoryMapping(1052, TorznabCatType.PC, "Веб-разработка и Программирование");
            AddCategoryMapping(1053, TorznabCatType.PC, "|- WYSIWYG Редакторы для веб-диза");
            AddCategoryMapping(1054, TorznabCatType.PC, "|- Текстовые редакторы с подсветкой");
            AddCategoryMapping(1055, TorznabCatType.PC, "|- Среды программирования, компиляторы и вспомогательные программы");
            AddCategoryMapping(1056, TorznabCatType.PC, "|- Компоненты для сред программирования");
            AddCategoryMapping(2077, TorznabCatType.PC, "|- Системы управления базами данных");
            AddCategoryMapping(1057, TorznabCatType.PC, "|- Скрипты и движки сайтов, CMS а также расширения к ним");
            AddCategoryMapping(1018, TorznabCatType.PC, "|- Шаблоны для сайтов и CMS");
            AddCategoryMapping(1058, TorznabCatType.PC, "|- Разное (Веб-разработка и программирование)");
            AddCategoryMapping(1016, TorznabCatType.PC, "Программы для работы с мультимедиа и 3D");
            AddCategoryMapping(1079, TorznabCatType.PC, "|- Программные комплекты");
            AddCategoryMapping(1080, TorznabCatType.PC, "|- Плагины для программ компании Adobe");
            AddCategoryMapping(1081, TorznabCatType.PC, "|- Графические редакторы");
            AddCategoryMapping(1082, TorznabCatType.PC, "|- Программы для верстки, печати и работы со шрифтами");
            AddCategoryMapping(1083, TorznabCatType.PC, "|- 3D моделирование, рендеринг и плагины для них");
            AddCategoryMapping(1084, TorznabCatType.PC, "|- Анимация");
            AddCategoryMapping(1085, TorznabCatType.PC, "|- Создание BD/HD/DVD-видео");
            AddCategoryMapping(1089, TorznabCatType.PC, "|- Редакторы видео");
            AddCategoryMapping(1090, TorznabCatType.PC, "|- Видео- Аудио- конверторы");
            AddCategoryMapping(1065, TorznabCatType.PC, "|- Аудио- и видео-, CD- проигрыватели и каталогизаторы");
            AddCategoryMapping(1064, TorznabCatType.PC, "|- Каталогизаторы и просмотрщики графики");
            AddCategoryMapping(1092, TorznabCatType.PC, "|- Разное (Программы для работы с мультимедиа и 3D)");
            AddCategoryMapping(1204, TorznabCatType.PC, "|- Виртуальные студии, секвенсоры и аудиоредакторы");
            AddCategoryMapping(1027, TorznabCatType.PC, "|- Виртуальные инструменты и синтезаторы");
            AddCategoryMapping(1199, TorznabCatType.PC, "|- Плагины для обработки звука");
            AddCategoryMapping(1091, TorznabCatType.PC, "|- Разное (Программы для работы со звуком)");
            AddCategoryMapping(838, TorznabCatType.OtherMisc, "|- Ищу/Предлагаю (Материалы для мультимедиа и дизайна)");
            AddCategoryMapping(1357, TorznabCatType.OtherMisc, "|- Авторские работы");
            AddCategoryMapping(890, TorznabCatType.OtherMisc, "|- Официальные сборники векторных клипартов");
            AddCategoryMapping(830, TorznabCatType.OtherMisc, "|- Прочие векторные клипарты");
            AddCategoryMapping(1290, TorznabCatType.OtherMisc, "|- Photostoсks");
            AddCategoryMapping(1962, TorznabCatType.OtherMisc, "|- Дополнения для программ компоузинга и постобработки");
            AddCategoryMapping(831, TorznabCatType.OtherMisc, "|- Рамки, шаблоны, текстуры и фоны");
            AddCategoryMapping(829, TorznabCatType.OtherMisc, "|- Прочие растровые клипарты");
            AddCategoryMapping(633, TorznabCatType.OtherMisc, "|- 3D модели, сцены и материалы");
            AddCategoryMapping(1009, TorznabCatType.OtherMisc, "|- Футажи");
            AddCategoryMapping(1963, TorznabCatType.OtherMisc, "|- Прочие сборники футажей");
            AddCategoryMapping(1954, TorznabCatType.OtherMisc, "|- Музыкальные библиотеки");
            AddCategoryMapping(1010, TorznabCatType.OtherMisc, "|- Звуковые эффекты");
            AddCategoryMapping(1674, TorznabCatType.OtherMisc, "|- Библиотеки сэмплов");
            AddCategoryMapping(2421, TorznabCatType.OtherMisc, "|- Библиотеки и саундбанки для сэмплеров, пресеты для синтезаторов");
            AddCategoryMapping(2492, TorznabCatType.OtherMisc, "|- Multitracks");
            AddCategoryMapping(839, TorznabCatType.OtherMisc, "|- Материалы для создания меню и обложек DVD");
            AddCategoryMapping(1679, TorznabCatType.OtherMisc, "|- Дополнения, стили, кисти, формы, узоры для программ Adobe");
            AddCategoryMapping(1011, TorznabCatType.OtherMisc, "|- Шрифты");
            AddCategoryMapping(835, TorznabCatType.OtherMisc, "|- Разное (Материалы для мультимедиа и дизайна)");
            AddCategoryMapping(1503, TorznabCatType.OtherMisc, "ГИС, системы навигации и карты");
            AddCategoryMapping(1507, TorznabCatType.OtherMisc, "|- ГИС (Геоинформационные системы)");
            AddCategoryMapping(1526, TorznabCatType.OtherMisc, "|- Карты, снабженные программной оболочкой");
            AddCategoryMapping(1508, TorznabCatType.OtherMisc, "|- Атласы и карты современные (после 1950 г.)");
            AddCategoryMapping(1509, TorznabCatType.OtherMisc, "|- Атласы и карты старинные (до 1950 г.)");
            AddCategoryMapping(1510, TorznabCatType.OtherMisc, "|- Карты прочие (астрономические, исторические, тематические)");
            AddCategoryMapping(1511, TorznabCatType.OtherMisc, "|- Встроенная автомобильная навигация");
            AddCategoryMapping(1512, TorznabCatType.OtherMisc, "|- Garmin");
            AddCategoryMapping(1513, TorznabCatType.OtherMisc, "|- Ozi");
            AddCategoryMapping(1514, TorznabCatType.OtherMisc, "|- TomTom");
            AddCategoryMapping(1515, TorznabCatType.OtherMisc, "|- Navigon / Navitel");
            AddCategoryMapping(1516, TorznabCatType.OtherMisc, "|- Igo");
            AddCategoryMapping(1517, TorznabCatType.OtherMisc, "|- Разное - системы навигации и карты");
            AddCategoryMapping(285, TorznabCatType.PCMobileOther, "Приложения для мобильных устройств");
            AddCategoryMapping(2154, TorznabCatType.PCMobileAndroid, "|- Приложения для Android");
            AddCategoryMapping(1005, TorznabCatType.PCMobileOther, "|- Приложения для Java");
            AddCategoryMapping(289, TorznabCatType.PCMobileOther, "|- Приложения для Symbian");
            AddCategoryMapping(290, TorznabCatType.PCMobileOther, "|- Приложения для Windows Mobile");
            AddCategoryMapping(2419, TorznabCatType.PCMobileOther, "|- Приложения для Windows Phone");
            AddCategoryMapping(288, TorznabCatType.PCMobileOther, "|- Софт для работы с телефоном");
            AddCategoryMapping(292, TorznabCatType.PCMobileOther, "|- Прошивки для телефонов");
            AddCategoryMapping(291, TorznabCatType.PCMobileOther, "|- Обои и темы");
            AddCategoryMapping(957, TorznabCatType.PCMobileOther, "Видео для мобильных устройств");
            AddCategoryMapping(287, TorznabCatType.PCMobileOther, "|- Видео для смартфонов и КПК");
            AddCategoryMapping(286, TorznabCatType.PCMobileOther, "|- Видео в формате 3GP для мобильных");
            AddCategoryMapping(1366, TorznabCatType.PCMac, "Apple Macintosh");
            AddCategoryMapping(1368, TorznabCatType.PCMac, "|- Mac OS (для Macintosh)");
            AddCategoryMapping(1383, TorznabCatType.PCMac, "|- Mac OS (для РС-Хакинтош)");
            AddCategoryMapping(1394, TorznabCatType.PCMac, "|- Программы для просмотра и обработки видео (Mac OS)");
            AddCategoryMapping(1370, TorznabCatType.PCMac, "|- Программы для создания и обработки графики (Mac OS)");
            AddCategoryMapping(2237, TorznabCatType.PCMac, "|- Плагины для программ компании Adobe (Mac OS)");
            AddCategoryMapping(1372, TorznabCatType.PCMac, "|- Аудио редакторы и конвертеры (Mac OS)");
            AddCategoryMapping(1373, TorznabCatType.PCMac, "|- Системные программы (Mac OS)");
            AddCategoryMapping(1375, TorznabCatType.PCMac, "|- Офисные программы (Mac OS)");
            AddCategoryMapping(1371, TorznabCatType.PCMac, "|- Программы для интернета и сетей (Mac OS)");
            AddCategoryMapping(1374, TorznabCatType.PCMac, "|- Другие программы (Mac OS)");
            AddCategoryMapping(1933, TorznabCatType.PCMobileiOS, "iOS");
            AddCategoryMapping(1935, TorznabCatType.PCMobileiOS, "|- Программы для iOS");
            AddCategoryMapping(1003, TorznabCatType.PCMobileiOS, "|- Игры для iOS");
            AddCategoryMapping(1937, TorznabCatType.PCMobileiOS, "|- Разное для iOS");
            AddCategoryMapping(2235, TorznabCatType.PCMobileiOS, "Видео");
            AddCategoryMapping(1908, TorznabCatType.PCMobileiOS, "|- Фильмы для iPod, iPhone, iPad");
            AddCategoryMapping(864, TorznabCatType.PCMobileiOS, "|- Сериалы для iPod, iPhone, iPad");
            AddCategoryMapping(863, TorznabCatType.PCMobileiOS, "|- Мультфильмы для iPod, iPhone, iPad");
            AddCategoryMapping(2535, TorznabCatType.PCMobileiOS, "|- Аниме для iPod, iPhone, iPad");
            AddCategoryMapping(2534, TorznabCatType.PCMobileiOS, "|- Музыкальное видео для iPod, iPhone, iPad");
            AddCategoryMapping(2238, TorznabCatType.PCMac, "Видео HD");
            AddCategoryMapping(1936, TorznabCatType.PCMac, "|- Фильмы HD для Apple TV");
            AddCategoryMapping(315, TorznabCatType.PCMac, "|- Сериалы HD для Apple TV");
            AddCategoryMapping(1363, TorznabCatType.PCMac, "|- Мультфильмы HD для Apple TV");
            AddCategoryMapping(2082, TorznabCatType.PCMac, "|- Документальное видео HD для Apple TV");
            AddCategoryMapping(2241, TorznabCatType.PCMac, "|- Музыкальное видео HD для Apple TV");
            AddCategoryMapping(2236, TorznabCatType.Audio, "Аудио");
            AddCategoryMapping(1909, TorznabCatType.AudioAudiobook, "|- Аудиокниги (AAC, ALAC)");
            AddCategoryMapping(1927, TorznabCatType.AudioLossless, "|- Музыка lossless (ALAC)");
            AddCategoryMapping(2240, TorznabCatType.Audio, "|- Музыка Lossy (AAC-iTunes)");
            AddCategoryMapping(2248, TorznabCatType.Audio, "|- Музыка Lossy (AAC)");
            AddCategoryMapping(2244, TorznabCatType.Audio, "|- Музыка Lossy (AAC) (Singles, EPs)");
            AddCategoryMapping(10, TorznabCatType.OtherMisc, "Разное (раздачи)");
            AddCategoryMapping(865, TorznabCatType.OtherMisc, "|- Психоактивные аудиопрограммы");
            AddCategoryMapping(1100, TorznabCatType.OtherMisc, "|- Аватары, Иконки, Смайлы");
            AddCategoryMapping(1643, TorznabCatType.OtherMisc, "|- Живопись, Графика, Скульптура, Digital Art");
            AddCategoryMapping(848, TorznabCatType.OtherMisc, "|- Картинки");
            AddCategoryMapping(808, TorznabCatType.OtherMisc, "|- Любительские фотографии");
            AddCategoryMapping(630, TorznabCatType.OtherMisc, "|- Обои");
            AddCategoryMapping(1664, TorznabCatType.OtherMisc, "|- Фото знаменитостей");
            AddCategoryMapping(148, TorznabCatType.Audio, "|- Аудио");
            AddCategoryMapping(965, TorznabCatType.AudioMP3, "|- Музыка (lossy)");
            AddCategoryMapping(134, TorznabCatType.AudioLossless, "|- Музыка (lossless)");
            AddCategoryMapping(807, TorznabCatType.TVOther, "|- Видео");
            AddCategoryMapping(147, TorznabCatType.Books, "|- Публикации и учебные материалы (тексты)");
            AddCategoryMapping(847, TorznabCatType.MoviesOther, "|- Трейлеры и дополнительные материалы к фильмам");
            AddCategoryMapping(1167, TorznabCatType.TVOther, "|- Любительские видеоклипы");
        }

        public override async Task<ConfigurationData> GetConfigurationForSetup()
        {
            try
            {
                configData.CookieHeader.Value = null;
                var response = await RequestWithCookiesAsync(LoginUrl);
                var parser = new HtmlParser();
                var doc = parser.ParseDocument(response.ContentString);
                var captchaimg = doc.QuerySelector("img[src^=\"https://static.t-ru.org/captcha/\"]");
                if (captchaimg != null)
                {
                    var captchaImage = await RequestWithCookiesAsync(captchaimg.GetAttribute("src"));
                    configData.CaptchaImage.Value = captchaImage.ContentBytes;

                    var codefield = doc.QuerySelector("input[name^=\"cap_code_\"]");
                    _capCodeField = codefield.GetAttribute("name");

                    var sidfield = doc.QuerySelector("input[name=\"cap_sid\"]");
                    _capSid = sidfield.GetAttribute("value");
                }
                else
                    configData.CaptchaImage.Value = null;
            }
            catch (Exception e)
            {
                logger.Error("Error loading configuration: " + e);
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
                { "login", "Login" }
            };

            if (!string.IsNullOrWhiteSpace(_capSid))
            {
                pairs.Add("cap_sid", _capSid);
                pairs.Add(_capCodeField, configData.CaptchaText.Value);

                _capSid = null;
                _capCodeField = null;
            }

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, CookieHeader, true, null, LoginUrl, true);
            await ConfigureIfOK(result.Cookies, result.ContentString != null && result.ContentString.Contains("id=\"logged-in-username\""), () =>
            {
                logger.Debug(result.ContentString);
                var errorMessage = "Unknown error message, please report";
                var parser = new HtmlParser();
                var doc = parser.ParseDocument(result.ContentString);
                var errormsg = doc.QuerySelector("h4[class=\"warnColor1 tCenter mrg_16\"]");
                if (errormsg != null)
                    errorMessage = errormsg.TextContent;

                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var searchUrl = CreateSearchUrlForQuery(query);

            var results = await RequestWithCookiesAsync(searchUrl);
            if (!results.ContentString.Contains("id=\"logged-in-username\""))
            {
                // re login
                await ApplyConfiguration(null);
                results = await RequestWithCookiesAsync(searchUrl);
            }

            var releases = new List<ReleaseInfo>();

            try
            {
                var rows = GetReleaseRows(results);
                foreach (var row in rows)
                {
                    var release = ParseReleaseRow(row);
                    if (release != null)
                    {
                        releases.Add(release);
                    }
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.ContentString, ex);
            }

            return releases;
        }

        private string CreateSearchUrlForQuery(in TorznabQuery query)
        {
            var queryCollection = new NameValueCollection();

            var searchString = query.SanitizedSearchTerm;

            // if the search string is empty use the getnew view
            if (string.IsNullOrWhiteSpace(searchString))
            {
                queryCollection.Add("nm", searchString);
            }
            else // use the normal search
            {
                searchString = searchString.Replace("-", " ");
                if (query.Season != 0)
                    searchString += " Сезон: " + query.Season;
                queryCollection.Add("nm", searchString);
            }

            if (query.HasSpecifiedCategories)
                queryCollection.Add("f", string.Join(",", MapTorznabCapsToTrackers(query)));

            var searchUrl = SearchUrl + "?" + queryCollection.GetQueryString();
            return searchUrl;
        }

        private IHtmlCollection<IElement> GetReleaseRows(WebResult results)
        {
            var parser = new HtmlParser();
            var doc = parser.ParseDocument(results.ContentString);
            var rows = doc.QuerySelectorAll("table#tor-tbl > tbody > tr");
            return rows;
        }

        private ReleaseInfo ParseReleaseRow(IElement row)
        {
            try
            {
                var qDownloadLink = row.QuerySelector("td.tor-size > a.tr-dl");
                if (qDownloadLink == null) // Expects moderation
                    return null;

                var link = new Uri(SiteLink + "forum/" + qDownloadLink.GetAttribute("href"));

                var qDetailsLink = row.QuerySelector("td.t-title-col > div.t-title > a.tLink");
                var details = new Uri(SiteLink + "forum/" + qDetailsLink.GetAttribute("href"));

                var category = GetCategoryOfRelease(row);

                var size = GetSizeOfRelease(row);

                var seeders = GetSeedersOfRelease(row);
                var leechers = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(8)").TextContent);

                var grabs = ParseUtil.CoerceLong(row.QuerySelector("td:nth-child(9)").TextContent);

                var publishDate = GetPublishDateOfRelease(row);

                var release = new ReleaseInfo
                {
                    MinimumRatio = 1,
                    MinimumSeedTime = 0,
                    Title = qDetailsLink.TextContent,
                    Details = details,
                    Link = link,
                    Guid = details,
                    Size = size,
                    Seeders = seeders,
                    Peers = leechers + seeders,
                    Grabs = grabs,
                    PublishDate = publishDate,
                    Category = category,
                    DownloadVolumeFactor = 1,
                    UploadVolumeFactor = 1
                };

                // TODO finish extracting release variables to simplify release initialization
                if (IsAnyTvCategory(release.Category))
                {
                    // extract season and episodes
                    var regex = new Regex(".+\\/\\s([^а-яА-я\\/]+)\\s\\/.+Сезон\\s*[:]*\\s+(\\d+).+(?:Серии|Эпизод)+\\s*[:]*\\s+(\\d+-*\\d*).+,\\s+(.+)\\][\\s]?(.*)");

                    //replace double 4K quality in title
                    release.Title = release.Title.Replace(", 4K]", "]");

                    var title = regex.Replace(release.Title, "$1 - S$2E$3 - rus $4 $5");
                    title = Regex.Replace(title, "-Rip", "Rip", RegexOptions.IgnoreCase);
                    title = Regex.Replace(title, "WEB-DLRip", "WEBDL", RegexOptions.IgnoreCase);
                    title = Regex.Replace(title, "WEB-DL", "WEBDL", RegexOptions.IgnoreCase);
                    title = Regex.Replace(title, "HDTVRip", "HDTV", RegexOptions.IgnoreCase);
                    title = Regex.Replace(title, "Кураж-Бамбей", "kurazh", RegexOptions.IgnoreCase);

                    release.Title = title;
                }
                else if (IsAnyMovieCategory(release.Category))
                {
                    // remove director's name from title
                    // rutracker movies titles look like: russian name / english name (russian director / english director) other stuff
                    // Ирландец / The Irishman (Мартин Скорсезе / Martin Scorsese) [2019, США, криминал, драма, биография, WEB-DL 1080p] Dub (Пифагор) + MVO (Jaskier) + AVO (Юрий Сербин) + Sub Rus, Eng + Original Eng
                    // this part should be removed: (Мартин Скорсезе / Martin Scorsese)
                    var director = new Regex(@"(\([А-Яа-яЁё\W]+)\s/\s(.+?)\)");
                    release.Title = director.Replace(release.Title, "");

                    // Bluray quality fix: radarr parse Blu-ray Disc as Bluray-1080p but should be BR-DISK
                    release.Title = Regex.Replace(release.Title, "Blu-ray Disc", "BR-DISK", RegexOptions.IgnoreCase);
                    // language fix: all rutracker releases contains russian track
                    if (release.Title.IndexOf("rus", StringComparison.OrdinalIgnoreCase) < 0)
                        release.Title += " rus";
                }

                if (configData.StripRussianLetters.Value)
                {
                    var regex = new Regex(@"(\([А-Яа-яЁё\W]+\))|(^[А-Яа-яЁё\W\d]+\/ )|([а-яА-ЯЁё \-]+,+)|([а-яА-ЯЁё]+)");
                    release.Title = regex.Replace(release.Title, "");
                }

                if (configData.MoveAllTagsToEndOfReleaseTitle.Value)
                {
                    release.Title = MoveAllTagsToEndOfReleaseTitle(release.Title);
                }
                else if (configData.MoveFirstTagsToEndOfReleaseTitle.Value)
                {
                    release.Title = MoveFirstTagsToEndOfReleaseTitle(release.Title);
                }

                if (release.Category.Contains(TorznabCatType.Audio.ID))
                {
                    release.Title = DetectRereleaseInReleaseTitle(release.Title);
                }

                return release;
            }
            catch (Exception ex)
            {
                logger.Error($"{Id}: Error while parsing row '{row.OuterHtml}':\n\n{ex}");
                return null;
            }
        }

        private int GetSeedersOfRelease(in IElement row)
        {
            var seeders = 0;
            var qSeeders = row.QuerySelector("td:nth-child(7)");
            if (qSeeders != null && !qSeeders.TextContent.Contains("дн"))
            {
                var seedersString = qSeeders.QuerySelector("b").TextContent;
                if (!string.IsNullOrWhiteSpace(seedersString))
                    seeders = ParseUtil.CoerceInt(seedersString);
            }
            return seeders;
        }

        private ICollection<int> GetCategoryOfRelease(in IElement row)
        {
            var forum = row.QuerySelector("td.f-name-col > div.f-name > a");
            var forumid = forum.GetAttribute("href").Split('=')[1];
            return MapTrackerCatToNewznab(forumid);
        }

        private long GetSizeOfRelease(in IElement row)
        {
            var qSize = row.QuerySelector("td.tor-size");
            var size = ReleaseInfo.GetBytes(qSize.GetAttribute("data-ts_text"));
            return size;
        }

        private DateTime GetPublishDateOfRelease(in IElement row)
        {
            var timestr = row.QuerySelector("td:nth-child(10)").GetAttribute("data-ts_text");
            var publishDate = DateTimeUtil.UnixTimestampToDateTime(long.Parse(timestr));
            return publishDate;
        }

        private bool IsAnyTvCategory(ICollection<int> category)
        {
            return category.Contains(TorznabCatType.TV.ID)
                || TorznabCatType.TV.SubCategories.Any(subCat => category.Contains(subCat.ID));
        }

        private bool IsAnyMovieCategory(ICollection<int> category)
        {
            return category.Contains(TorznabCatType.Movies.ID)
                || TorznabCatType.Movies.SubCategories.Any(subCat => category.Contains(subCat.ID));
        }

        private string MoveAllTagsToEndOfReleaseTitle(string input)
        {
            var output = input + " ";
            foreach (Match match in _regexToFindTagsInReleaseTitle.Matches(input))
            {
                var tag = match.ToString();
                output = output.Replace(tag, "") + tag;
            }
            output = output.Trim();
            return output;
        }

        private string MoveFirstTagsToEndOfReleaseTitle(string input)
        {
            var output = input + " ";
            var expectedIndex = 0;
            foreach (Match match in _regexToFindTagsInReleaseTitle.Matches(input))
            {
                if (match.Index > expectedIndex)
                {
                    var substring = input.Substring(expectedIndex, match.Index - expectedIndex);
                    if (string.IsNullOrWhiteSpace(substring))
                        expectedIndex = match.Index;
                    else
                        break;
                }
                var tag = match.ToString();
                output = output.Replace(tag, "") + tag;
                expectedIndex += tag.Length;
            }
            output = output.Trim();
            return output;
        }

        /// <summary>
        /// Searches the release title to find a 'year1/year2' pattern that would indicate that this is a re-release of an old music album.
        /// If the release is found to be a re-release, this is added to the title as a new tag.
        /// Not to be confused with discographies; they mostly follow the 'year1-year2' pattern.
        /// </summary>
        private string DetectRereleaseInReleaseTitle(string input)
        {
            var fullTitle = input;

            var squareBracketTags = input.FindSubstringsBetween('[', ']', includeOpeningAndClosing: true);
            input = input.RemoveSubstrings(squareBracketTags);

            var roundBracketTags = input.FindSubstringsBetween('(', ')', includeOpeningAndClosing: true);
            input = input.RemoveSubstrings(roundBracketTags);

            var regex = new Regex(@"\d{4}");
            var yearsInTitle = regex.Matches(input);

            if (yearsInTitle == null || yearsInTitle.Count < 2)
            {
                //Can only be a re-release if there's at least 2 years in the title.
                return fullTitle;
            }

            regex = new Regex(@"(\d{4}) *\/ *(\d{4})");
            var regexMatch = regex.Match(input);
            if (!regexMatch.Success)
            {
                //Not in the expected format. Return the unaltered title.
                return fullTitle;
            }

            var originalYear = regexMatch.Groups[1].ToString();
            fullTitle = fullTitle.Replace(regexMatch.ToString(), originalYear);

            return fullTitle + "(Re-release)";
        }
    }
}
