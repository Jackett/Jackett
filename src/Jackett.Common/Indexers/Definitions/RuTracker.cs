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
using Jackett.Common.Extensions;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Cache;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class RuTracker : IndexerBase
    {
        public override string Id => "rutracker";
        public override string Name => "RuTracker.org";
        public override string Description => "RuTracker.org is a Semi-Private Russian torrent site with a thriving file-sharing community";
        public override string SiteLink { get; protected set; } = "https://rutracker.org/";
        public override string[] AlternativeSiteLinks => new[]
        {
            "https://rutracker.org/",
            "https://rutracker.net/",
            "https://rutracker.nl/"
        };
        public override Encoding Encoding => Encoding.GetEncoding("windows-1251");
        public override string Language => "ru-RU";
        public override string Type => "semi-private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private new ConfigurationDataRutracker configData => (ConfigurationDataRutracker)base.configData;

        private readonly TitleParser _titleParser = new TitleParser();
        private string LoginUrl => SiteLink + "forum/login.php";
        private string SearchUrl => SiteLink + "forum/tracker.php";

        private string _capSid;
        private string _capCodeField;

        public RuTracker(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps, CacheManager cm)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheManager: cm,
                   configData: new ConfigurationDataRutracker())
        {
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
                    MovieSearchParam.Q
                },
                MusicSearchParams = new List<MusicSearchParam>
                {
                    MusicSearchParam.Q
                },
                BookSearchParams = new List<BookSearchParam>
                {
                    BookSearchParam.Q
                },
                SupportsRawSearch = true
            };

            // note: when refreshing the categories use the tracker.php page and NOT the search.php page!
            caps.Categories.AddCategoryMapping(22, TorznabCatType.Movies, "Наше кино");
            caps.Categories.AddCategoryMapping(941, TorznabCatType.Movies, "|- Кино СССР");
            caps.Categories.AddCategoryMapping(1666, TorznabCatType.Movies, "|- Детские отечественные фильмы");
            caps.Categories.AddCategoryMapping(376, TorznabCatType.Movies, "|- Авторские дебюты");
            caps.Categories.AddCategoryMapping(106, TorznabCatType.Movies, "|- Фильмы России и СССР на национальных языках [без перевода]");
            caps.Categories.AddCategoryMapping(7, TorznabCatType.MoviesForeign, "Зарубежное кино");
            caps.Categories.AddCategoryMapping(187, TorznabCatType.MoviesForeign, "|- Классика мирового кинематографа");
            caps.Categories.AddCategoryMapping(2090, TorznabCatType.MoviesForeign, "|- Фильмы до 1990 года");
            caps.Categories.AddCategoryMapping(2221, TorznabCatType.MoviesForeign, "|- Фильмы 1991-2000");
            caps.Categories.AddCategoryMapping(2091, TorznabCatType.MoviesForeign, "|- Фильмы 2001-2005");
            caps.Categories.AddCategoryMapping(2092, TorznabCatType.MoviesForeign, "|- Фильмы 2006-2010");
            caps.Categories.AddCategoryMapping(2093, TorznabCatType.MoviesForeign, "|- Фильмы 2011-2015");
            caps.Categories.AddCategoryMapping(2200, TorznabCatType.MoviesForeign, "|- Фильмы 2016-2020");
            caps.Categories.AddCategoryMapping(1950, TorznabCatType.MoviesForeign, "|- Фильмы 2021-2022");
            caps.Categories.AddCategoryMapping(252, TorznabCatType.MoviesForeign, "|- Фильмы 2023");
            caps.Categories.AddCategoryMapping(2540, TorznabCatType.MoviesForeign, "|- Фильмы Ближнего Зарубежья");
            caps.Categories.AddCategoryMapping(934, TorznabCatType.MoviesForeign, "|- Азиатские фильмы");
            caps.Categories.AddCategoryMapping(505, TorznabCatType.MoviesForeign, "|- Индийское кино");
            caps.Categories.AddCategoryMapping(212, TorznabCatType.MoviesForeign, "|- Сборники фильмов");
            caps.Categories.AddCategoryMapping(2459, TorznabCatType.MoviesForeign, "|- Короткий метр");
            caps.Categories.AddCategoryMapping(1235, TorznabCatType.MoviesForeign, "|- Грайндхаус");
            caps.Categories.AddCategoryMapping(166, TorznabCatType.MoviesForeign, "|- Зарубежные фильмы без перевода");
            caps.Categories.AddCategoryMapping(185, TorznabCatType.Audio, "|- Звуковые дорожки и Переводы");
            caps.Categories.AddCategoryMapping(124, TorznabCatType.MoviesOther, "Арт-хаус и авторское кино");
            caps.Categories.AddCategoryMapping(1543, TorznabCatType.MoviesOther, "|- Короткий метр (Арт-хаус и авторское кино)");
            caps.Categories.AddCategoryMapping(709, TorznabCatType.MoviesOther, "|- Документальные фильмы (Арт-хаус и авторское кино)");
            caps.Categories.AddCategoryMapping(1577, TorznabCatType.MoviesOther, "|- Анимация (Арт-хаус и авторское кино)");
            caps.Categories.AddCategoryMapping(511, TorznabCatType.TVOther, "Театр");
            caps.Categories.AddCategoryMapping(1493, TorznabCatType.TVOther, "|- Спектакли без перевода");
            caps.Categories.AddCategoryMapping(93, TorznabCatType.MoviesDVD, "DVD Video");
            caps.Categories.AddCategoryMapping(905, TorznabCatType.MoviesDVD, "|- Классика мирового кинематографа (DVD Video)");
            caps.Categories.AddCategoryMapping(101, TorznabCatType.MoviesDVD, "|- Зарубежное кино (DVD Video)");
            caps.Categories.AddCategoryMapping(100, TorznabCatType.MoviesDVD, "|- Наше кино (DVD Video)");
            caps.Categories.AddCategoryMapping(877, TorznabCatType.MoviesDVD, "|- Фильмы Ближнего Зарубежья (DVD Video)");
            caps.Categories.AddCategoryMapping(1576, TorznabCatType.MoviesDVD, "|- Азиатские фильмы (DVD Video)");
            caps.Categories.AddCategoryMapping(572, TorznabCatType.MoviesDVD, "|- Арт-хаус и авторское кино (DVD Video)");
            caps.Categories.AddCategoryMapping(2220, TorznabCatType.MoviesDVD, "|- Индийское кино (DVD Video)");
            caps.Categories.AddCategoryMapping(1670, TorznabCatType.MoviesDVD, "|- Грайндхаус (DVD Video)");
            caps.Categories.AddCategoryMapping(2198, TorznabCatType.MoviesHD, "HD Video");
            caps.Categories.AddCategoryMapping(2199, TorznabCatType.MoviesHD, "|- Классика мирового кинематографа (HD Video)");
            caps.Categories.AddCategoryMapping(313, TorznabCatType.MoviesHD, "|- Зарубежное кино (HD Video)");
            caps.Categories.AddCategoryMapping(312, TorznabCatType.MoviesHD, "|- Наше кино (HD Video)");
            caps.Categories.AddCategoryMapping(1247, TorznabCatType.MoviesHD, "|- Фильмы Ближнего Зарубежья (HD Video)");
            caps.Categories.AddCategoryMapping(2201, TorznabCatType.MoviesHD, "|- Азиатские фильмы (HD Video)");
            caps.Categories.AddCategoryMapping(2339, TorznabCatType.MoviesHD, "|- Арт-хаус и авторское кино (HD Video)");
            caps.Categories.AddCategoryMapping(140, TorznabCatType.MoviesHD, "|- Индийское кино (HD Video)");
            caps.Categories.AddCategoryMapping(194, TorznabCatType.MoviesHD, "|- Грайндхаус (HD Video)");
            caps.Categories.AddCategoryMapping(718, TorznabCatType.MoviesUHD, "UHD Video");
            caps.Categories.AddCategoryMapping(775, TorznabCatType.MoviesUHD, "|- Классика мирового кинематографа (UHD Video)");
            caps.Categories.AddCategoryMapping(1457, TorznabCatType.MoviesUHD, "|- Зарубежное кино (UHD Video)");
            caps.Categories.AddCategoryMapping(1940, TorznabCatType.MoviesUHD, "|- Наше кино (UHD Video)");
            caps.Categories.AddCategoryMapping(272, TorznabCatType.MoviesUHD, "|- Азиатские фильмы (UHD Video)");
            caps.Categories.AddCategoryMapping(271, TorznabCatType.MoviesUHD, "|- Арт-хаус и авторское кино (UHD Video)");
            caps.Categories.AddCategoryMapping(352, TorznabCatType.Movies3D, "3D/Стерео Кино, Видео, TV и Спорт");
            caps.Categories.AddCategoryMapping(549, TorznabCatType.Movies3D, "|- 3D Кинофильмы");
            caps.Categories.AddCategoryMapping(1213, TorznabCatType.Movies3D, "|- 3D Мультфильмы");
            caps.Categories.AddCategoryMapping(2109, TorznabCatType.Movies3D, "|- 3D Документальные фильмы");
            caps.Categories.AddCategoryMapping(514, TorznabCatType.Movies3D, "|- 3D Спорт");
            caps.Categories.AddCategoryMapping(2097, TorznabCatType.Movies3D, "|- 3D Ролики, Музыкальное видео, Трейлеры к фильмам");
            caps.Categories.AddCategoryMapping(4, TorznabCatType.Movies, "Мультфильмы");
            caps.Categories.AddCategoryMapping(84, TorznabCatType.MoviesUHD, "|- Мультфильмы (UHD Video)");
            caps.Categories.AddCategoryMapping(2343, TorznabCatType.MoviesHD, "|- Отечественные мультфильмы (HD Video)");
            caps.Categories.AddCategoryMapping(930, TorznabCatType.MoviesHD, "|- Иностранные мультфильмы (HD Video)");
            caps.Categories.AddCategoryMapping(2365, TorznabCatType.MoviesHD, "|- Иностранные короткометражные мультфильмы (HD Video)");
            caps.Categories.AddCategoryMapping(1900, TorznabCatType.MoviesDVD, "|- Отечественные мультфильмы (DVD)");
            caps.Categories.AddCategoryMapping(2258, TorznabCatType.MoviesDVD, "|- Иностранные короткометражные мультфильмы (DVD)");
            caps.Categories.AddCategoryMapping(521, TorznabCatType.MoviesDVD, "|- Иностранные мультфильмы (DVD)");
            caps.Categories.AddCategoryMapping(208, TorznabCatType.Movies, "|- Отечественные мультфильмы");
            caps.Categories.AddCategoryMapping(539, TorznabCatType.Movies, "|- Отечественные полнометражные мультфильмы");
            caps.Categories.AddCategoryMapping(2183, TorznabCatType.MoviesForeign, "|- Мультфильмы Ближнего Зарубежья");
            caps.Categories.AddCategoryMapping(209, TorznabCatType.MoviesForeign, "|- Иностранные мультфильмы");
            caps.Categories.AddCategoryMapping(484, TorznabCatType.MoviesForeign, "|- Иностранные короткометражные мультфильмы");
            caps.Categories.AddCategoryMapping(822, TorznabCatType.Movies, "|- Сборники мультфильмов");
            caps.Categories.AddCategoryMapping(181, TorznabCatType.Movies, "|- Мультфильмы без перевода");
            caps.Categories.AddCategoryMapping(921, TorznabCatType.TV, "Мультсериалы");
            caps.Categories.AddCategoryMapping(815, TorznabCatType.TVSD, "|- Мультсериалы (SD Video)");
            caps.Categories.AddCategoryMapping(816, TorznabCatType.TVHD, "|- Мультсериалы (DVD Video)");
            caps.Categories.AddCategoryMapping(1460, TorznabCatType.TVHD, "|- Мультсериалы (HD Video)");
            caps.Categories.AddCategoryMapping(498, TorznabCatType.TVUHD, "|- Мультсериалы (UHD Video)");
            caps.Categories.AddCategoryMapping(33, TorznabCatType.TVAnime, "Аниме");
            caps.Categories.AddCategoryMapping(1106, TorznabCatType.TVAnime, "|- Онгоинги (HD Video)");
            caps.Categories.AddCategoryMapping(1105, TorznabCatType.TVAnime, "|- Аниме (HD Video)");
            caps.Categories.AddCategoryMapping(599, TorznabCatType.TVAnime, "|- Аниме (DVD)");
            caps.Categories.AddCategoryMapping(1389, TorznabCatType.TVAnime, "|- Аниме (основной подраздел)");
            caps.Categories.AddCategoryMapping(1391, TorznabCatType.TVAnime, "|- Аниме (плеерный подраздел)");
            caps.Categories.AddCategoryMapping(2491, TorznabCatType.TVAnime, "|- Аниме (QC подраздел)");
            caps.Categories.AddCategoryMapping(2544, TorznabCatType.TVAnime, "|- Ван-Пис");
            caps.Categories.AddCategoryMapping(1642, TorznabCatType.TVAnime, "|- Гандам");
            caps.Categories.AddCategoryMapping(1390, TorznabCatType.TVAnime, "|- Наруто");
            caps.Categories.AddCategoryMapping(404, TorznabCatType.TVAnime, "|- Покемоны");
            caps.Categories.AddCategoryMapping(893, TorznabCatType.TVAnime, "|- Японские мультфильмы");
            caps.Categories.AddCategoryMapping(809, TorznabCatType.Audio, "|- Звуковые дорожки (Аниме)");
            caps.Categories.AddCategoryMapping(2484, TorznabCatType.TVAnime, "|- Артбуки и журналы (Аниме)");
            caps.Categories.AddCategoryMapping(1386, TorznabCatType.TVAnime, "|- Обои, сканы, аватары, арт");
            caps.Categories.AddCategoryMapping(1387, TorznabCatType.TVAnime, "|- AMV и другие ролики");
            caps.Categories.AddCategoryMapping(9, TorznabCatType.TV, "Русские сериалы");
            caps.Categories.AddCategoryMapping(81, TorznabCatType.TVHD, "|- Русские сериалы (HD Video)");
            caps.Categories.AddCategoryMapping(920, TorznabCatType.TVSD, "|- Русские сериалы (DVD Video)");
            caps.Categories.AddCategoryMapping(80, TorznabCatType.TV, "|- Сельский детектив");
            caps.Categories.AddCategoryMapping(1535, TorznabCatType.TV, "|- По законам военного времени");
            caps.Categories.AddCategoryMapping(188, TorznabCatType.TV, "|- Московские тайны");
            caps.Categories.AddCategoryMapping(91, TorznabCatType.TV, "|- Я знаю твои секреты");
            caps.Categories.AddCategoryMapping(990, TorznabCatType.TV, "|- Универ / Универ. Новая общага / СашаТаня");
            caps.Categories.AddCategoryMapping(1408, TorznabCatType.TV, "|- Женская версия");
            caps.Categories.AddCategoryMapping(175, TorznabCatType.TV, "|- След");
            caps.Categories.AddCategoryMapping(79, TorznabCatType.TV, "|- Некрасивая подружка");
            caps.Categories.AddCategoryMapping(104, TorznabCatType.TV, "|- Психология преступления");
            caps.Categories.AddCategoryMapping(189, TorznabCatType.TVForeign, "Зарубежные сериалы");
            caps.Categories.AddCategoryMapping(842, TorznabCatType.TVForeign, "|- Новинки и сериалы в стадии показа");
            caps.Categories.AddCategoryMapping(235, TorznabCatType.TVForeign, "|- Сериалы США и Канады");
            caps.Categories.AddCategoryMapping(242, TorznabCatType.TVForeign, "|- Сериалы Великобритании и Ирландии");
            caps.Categories.AddCategoryMapping(819, TorznabCatType.TVForeign, "|- Скандинавские сериалы");
            caps.Categories.AddCategoryMapping(1531, TorznabCatType.TVForeign, "|- Испанские сериалы");
            caps.Categories.AddCategoryMapping(721, TorznabCatType.TVForeign, "|- Итальянские сериалы");
            caps.Categories.AddCategoryMapping(1102, TorznabCatType.TVForeign, "|- Европейские сериалы");
            caps.Categories.AddCategoryMapping(1120, TorznabCatType.TVForeign, "|- Сериалы стран Африки, Ближнего и Среднего Востока");
            caps.Categories.AddCategoryMapping(1214, TorznabCatType.TVForeign, "|- Сериалы Австралии и Новой Зеландии");
            caps.Categories.AddCategoryMapping(489, TorznabCatType.TVForeign, "|- Сериалы Ближнего Зарубежья");
            caps.Categories.AddCategoryMapping(387, TorznabCatType.TVForeign, "|- Сериалы совместного производства нескольких стран");
            caps.Categories.AddCategoryMapping(1359, TorznabCatType.TVForeign, "|- Веб-сериалы, Вебизоды к сериалам и Пилотные серии сериалов");
            caps.Categories.AddCategoryMapping(184, TorznabCatType.TVForeign, "|- Бесстыжие / Shameless (US)");
            caps.Categories.AddCategoryMapping(1171, TorznabCatType.TVForeign, "|- Викинги / Vikings");
            caps.Categories.AddCategoryMapping(1417, TorznabCatType.TVForeign, "|- Во все тяжкие / Breaking Bad");
            caps.Categories.AddCategoryMapping(625, TorznabCatType.TVForeign, "|- Доктор Хаус / House M.D.");
            caps.Categories.AddCategoryMapping(1449, TorznabCatType.TVForeign, "|- Игра престолов / Game of Thrones");
            caps.Categories.AddCategoryMapping(273, TorznabCatType.TVForeign, "|- Карточный Домик / House of Cards");
            caps.Categories.AddCategoryMapping(504, TorznabCatType.TVForeign, "|- Клан Сопрано / The Sopranos");
            caps.Categories.AddCategoryMapping(372, TorznabCatType.TVForeign, "|- Сверхъестественное / Supernatural");
            caps.Categories.AddCategoryMapping(110, TorznabCatType.TVForeign, "|- Секретные материалы / The X-Files");
            caps.Categories.AddCategoryMapping(121, TorznabCatType.TVForeign, "|- Твин пикс / Twin Peaks");
            caps.Categories.AddCategoryMapping(507, TorznabCatType.TVForeign, "|- Теория большого взрыва + Детство Шелдона");
            caps.Categories.AddCategoryMapping(536, TorznabCatType.TVForeign, "|- Форс-мажоры / Костюмы в законе / Suits");
            caps.Categories.AddCategoryMapping(1144, TorznabCatType.TVForeign, "|- Ходячие мертвецы + Бойтесь ходячих мертвецов");
            caps.Categories.AddCategoryMapping(173, TorznabCatType.TVForeign, "|- Черное зеркало / Black Mirror");
            caps.Categories.AddCategoryMapping(195, TorznabCatType.TVForeign, "|- Для некондиционных раздач");
            caps.Categories.AddCategoryMapping(2366, TorznabCatType.TVHD, "Зарубежные сериалы (HD Video)");
            caps.Categories.AddCategoryMapping(119, TorznabCatType.TVUHD, "|- Зарубежные сериалы (UHD Video)");
            caps.Categories.AddCategoryMapping(1803, TorznabCatType.TVHD, "|- Новинки и сериалы в стадии показа (HD Video)");
            caps.Categories.AddCategoryMapping(266, TorznabCatType.TVHD, "|- Сериалы США и Канады (HD Video)");
            caps.Categories.AddCategoryMapping(193, TorznabCatType.TVHD, "|- Сериалы Великобритании и Ирландии (HD Video)");
            caps.Categories.AddCategoryMapping(1690, TorznabCatType.TVHD, "|- Скандинавские сериалы (HD Video)");
            caps.Categories.AddCategoryMapping(1459, TorznabCatType.TVHD, "|- Европейские сериалы (HD Video)");
            caps.Categories.AddCategoryMapping(1463, TorznabCatType.TVHD, "|- Сериалы стран Африки, Ближнего и Среднего Востока (HD Video)");
            caps.Categories.AddCategoryMapping(825, TorznabCatType.TVHD, "|- Сериалы Австралии и Новой Зеландии (HD Video)");
            caps.Categories.AddCategoryMapping(1248, TorznabCatType.TVHD, "|- Сериалы Ближнего Зарубежья (HD Video)");
            caps.Categories.AddCategoryMapping(1288, TorznabCatType.TVHD, "|- Сериалы совместного производства нескольких стран (HD Video)");
            caps.Categories.AddCategoryMapping(1669, TorznabCatType.TVHD, "|- Викинги / Vikings (HD Video)");
            caps.Categories.AddCategoryMapping(2393, TorznabCatType.TVHD, "|- Доктор Хаус / House M.D. (HD Video)");
            caps.Categories.AddCategoryMapping(265, TorznabCatType.TVHD, "|- Игра престолов / Game of Thrones (HD Video)");
            caps.Categories.AddCategoryMapping(2406, TorznabCatType.TVHD, "|- Карточный домик (HD Video)");
            caps.Categories.AddCategoryMapping(2404, TorznabCatType.TVHD, "|- Сверхъестественное / Supernatural (HD Video)");
            caps.Categories.AddCategoryMapping(2405, TorznabCatType.TVHD, "|- Секретные материалы / The X-Files (HD Video)");
            caps.Categories.AddCategoryMapping(2370, TorznabCatType.TVHD, "|- Твин пикс / Twin Peaks (HD Video)");
            caps.Categories.AddCategoryMapping(2396, TorznabCatType.TVHD, "|- Теория Большого Взрыва / The Big Bang Theory (HD Video)");
            caps.Categories.AddCategoryMapping(2398, TorznabCatType.TVHD, "|- Ходячие мертвецы + Бойтесь ходячих мертвецов (HD Video)");
            caps.Categories.AddCategoryMapping(1949, TorznabCatType.TVHD, "|- Черное зеркало / Black Mirror (HD Video)");
            caps.Categories.AddCategoryMapping(1498, TorznabCatType.TVHD, "|- Для некондиционных раздач (HD Video)");
            caps.Categories.AddCategoryMapping(911, TorznabCatType.TVForeign, "Сериалы Латинской Америки, Турции и Индии");
            caps.Categories.AddCategoryMapping(325, TorznabCatType.TVForeign, "|- Сериалы Аргентины");
            caps.Categories.AddCategoryMapping(534, TorznabCatType.TVForeign, "|- Сериалы Бразилии");
            caps.Categories.AddCategoryMapping(594, TorznabCatType.TVForeign, "|- Сериалы Венесуэлы");
            caps.Categories.AddCategoryMapping(1301, TorznabCatType.TVForeign, "|- Сериалы Индии");
            caps.Categories.AddCategoryMapping(607, TorznabCatType.TVForeign, "|- Сериалы Колумбии");
            caps.Categories.AddCategoryMapping(1574, TorznabCatType.TVForeign, "|- Сериалы Латинской Америки с озвучкой (раздачи папками)");
            caps.Categories.AddCategoryMapping(1539, TorznabCatType.TVForeign, "|- Сериалы Латинской Америки с субтитрами");
            caps.Categories.AddCategoryMapping(694, TorznabCatType.TVForeign, "|- Сериалы Мексики");
            caps.Categories.AddCategoryMapping(781, TorznabCatType.TVForeign, "|- Сериалы совместного производства");
            caps.Categories.AddCategoryMapping(704, TorznabCatType.TVForeign, "|- Сериалы Турции");
            caps.Categories.AddCategoryMapping(1537, TorznabCatType.TVForeign, "|- Для некондиционных раздач");
            caps.Categories.AddCategoryMapping(2100, TorznabCatType.TVForeign, "Азиатские сериалы");
            caps.Categories.AddCategoryMapping(820, TorznabCatType.TVForeign, "|- Азиатские сериалы (UHD Video)");
            caps.Categories.AddCategoryMapping(915, TorznabCatType.TVForeign, "|- Корейские сериалы");
            caps.Categories.AddCategoryMapping(1242, TorznabCatType.TVForeign, "|- Корейские сериалы (HD Video)");
            caps.Categories.AddCategoryMapping(717, TorznabCatType.TVForeign, "|- Китайские сериалы");
            caps.Categories.AddCategoryMapping(1939, TorznabCatType.TVForeign, "|- Японские сериалы");
            caps.Categories.AddCategoryMapping(2412, TorznabCatType.TVForeign, "|- Сериалы Таиланда, Индонезии, Сингапура");
            caps.Categories.AddCategoryMapping(2102, TorznabCatType.TVForeign, "|- VMV и др. ролики");
            caps.Categories.AddCategoryMapping(19, TorznabCatType.TVDocumentary, "СМИ");
            caps.Categories.AddCategoryMapping(670, TorznabCatType.TVDocumentary, "Вера и религия");
            caps.Categories.AddCategoryMapping(1475, TorznabCatType.TVDocumentary, "|- [Видео Религия] Христианство");
            caps.Categories.AddCategoryMapping(2107, TorznabCatType.TVDocumentary, "|- [Видео Религия] Ислам");
            caps.Categories.AddCategoryMapping(1453, TorznabCatType.TVDocumentary, "|- [Видео Религия] Культы и новые религиозные движения");
            caps.Categories.AddCategoryMapping(294, TorznabCatType.TVDocumentary, "|- [Видео Религия] Религии Индии, Тибета и Восточной Азии");
            caps.Categories.AddCategoryMapping(46, TorznabCatType.TVDocumentary, "Документальные фильмы и телепередачи");
            caps.Categories.AddCategoryMapping(103, TorznabCatType.TVDocumentary, "|- Документальные (DVD)");
            caps.Categories.AddCategoryMapping(671, TorznabCatType.TVDocumentary, "|- [Док] Биографии. Личности и кумиры");
            caps.Categories.AddCategoryMapping(2177, TorznabCatType.TVDocumentary, "|- [Док] Кинематограф и мультипликация");
            caps.Categories.AddCategoryMapping(656, TorznabCatType.TVDocumentary, "|- [Док] Мастера искусств Театра и Кино");
            caps.Categories.AddCategoryMapping(2538, TorznabCatType.TVDocumentary, "|- [Док] Искусство, история искусств");
            caps.Categories.AddCategoryMapping(2159, TorznabCatType.TVDocumentary, "|- [Док] Музыка");
            caps.Categories.AddCategoryMapping(251, TorznabCatType.TVDocumentary, "|- [Док] Криминальная документалистика");
            caps.Categories.AddCategoryMapping(98, TorznabCatType.TVDocumentary, "|- [Док] Тайны века / Спецслужбы / Теории Заговоров");
            caps.Categories.AddCategoryMapping(97, TorznabCatType.TVDocumentary, "|- [Док] Военное дело");
            caps.Categories.AddCategoryMapping(851, TorznabCatType.TVDocumentary, "|- [Док] Вторая мировая война");
            caps.Categories.AddCategoryMapping(2178, TorznabCatType.TVDocumentary, "|- [Док] Аварии / Катастрофы / Катаклизмы");
            caps.Categories.AddCategoryMapping(821, TorznabCatType.TVDocumentary, "|- [Док] Авиация");
            caps.Categories.AddCategoryMapping(2076, TorznabCatType.TVDocumentary, "|- [Док] Космос");
            caps.Categories.AddCategoryMapping(56, TorznabCatType.TVDocumentary, "|- [Док] Научно-популярные фильмы");
            caps.Categories.AddCategoryMapping(2123, TorznabCatType.TVDocumentary, "|- [Док] Флора и фауна");
            caps.Categories.AddCategoryMapping(876, TorznabCatType.TVDocumentary, "|- [Док] Путешествия и туризм");
            caps.Categories.AddCategoryMapping(2139, TorznabCatType.TVDocumentary, "|- [Док] Медицина");
            caps.Categories.AddCategoryMapping(2380, TorznabCatType.TVDocumentary, "|- [Док] Социальные ток-шоу");
            caps.Categories.AddCategoryMapping(1467, TorznabCatType.TVDocumentary, "|- [Док] Информационно-аналитические и общественно-политические передачи");
            caps.Categories.AddCategoryMapping(1469, TorznabCatType.TVDocumentary, "|- [Док] Архитектура и строительство");
            caps.Categories.AddCategoryMapping(672, TorznabCatType.TVDocumentary, "|- [Док] Всё о доме, быте и дизайне");
            caps.Categories.AddCategoryMapping(249, TorznabCatType.TVDocumentary, "|- [Док] BBC");
            caps.Categories.AddCategoryMapping(552, TorznabCatType.TVDocumentary, "|- [Док] Discovery");
            caps.Categories.AddCategoryMapping(500, TorznabCatType.TVDocumentary, "|- [Док] National Geographic");
            caps.Categories.AddCategoryMapping(2112, TorznabCatType.TVDocumentary, "|- [Док] История: Древний мир / Античность / Средневековье");
            caps.Categories.AddCategoryMapping(1327, TorznabCatType.TVDocumentary, "|- [Док] История: Новое и Новейшее время");
            caps.Categories.AddCategoryMapping(1468, TorznabCatType.TVDocumentary, "|- [Док] Эпоха СССР");
            caps.Categories.AddCategoryMapping(1280, TorznabCatType.TVDocumentary, "|- [Док] Битва экстрасенсов / Теория невероятности / Искатели / Галилео");
            caps.Categories.AddCategoryMapping(752, TorznabCatType.TVDocumentary, "|- [Док] Русские сенсации / Программа Максимум / Профессия репортёр");
            caps.Categories.AddCategoryMapping(1114, TorznabCatType.TVDocumentary, "|- [Док] Паранормальные явления");
            caps.Categories.AddCategoryMapping(2168, TorznabCatType.TVDocumentary, "|- [Док] Альтернативная история и наука");
            caps.Categories.AddCategoryMapping(2160, TorznabCatType.TVDocumentary, "|- [Док] Внежанровая документалистика");
            caps.Categories.AddCategoryMapping(2176, TorznabCatType.TVDocumentary, "|- [Док] Разное / некондиция");
            caps.Categories.AddCategoryMapping(314, TorznabCatType.TVDocumentary, "Документальные (HD Video)");
            caps.Categories.AddCategoryMapping(2323, TorznabCatType.TVDocumentary, "|- Информационно-аналитические и общественно-политические (HD Video)");
            caps.Categories.AddCategoryMapping(1278, TorznabCatType.TVDocumentary, "|- Биографии. Личности и кумиры (HD Video)");
            caps.Categories.AddCategoryMapping(1281, TorznabCatType.TVDocumentary, "|- Военное дело (HD Video)");
            caps.Categories.AddCategoryMapping(2110, TorznabCatType.TVDocumentary, "|- Естествознание, наука и техника (HD Video)");
            caps.Categories.AddCategoryMapping(979, TorznabCatType.TVDocumentary, "|- Путешествия и туризм (HD Video)");
            caps.Categories.AddCategoryMapping(2169, TorznabCatType.TVDocumentary, "|- Флора и фауна (HD Video)");
            caps.Categories.AddCategoryMapping(2166, TorznabCatType.TVDocumentary, "|- История (HD Video)");
            caps.Categories.AddCategoryMapping(2164, TorznabCatType.TVDocumentary, "|- BBC, Discovery, National Geographic, History Channel, Netflix (HD Video)");
            caps.Categories.AddCategoryMapping(2163, TorznabCatType.TVDocumentary, "|- Криминальная документалистика (HD Video)");
            caps.Categories.AddCategoryMapping(85, TorznabCatType.TVDocumentary, "|- Некондиционное видео - Документальные (HD Video)");
            caps.Categories.AddCategoryMapping(24, TorznabCatType.TVDocumentary, "Развлекательные телепередачи и шоу, приколы и юмор");
            caps.Categories.AddCategoryMapping(1959, TorznabCatType.TVOther, "|- [Видео Юмор] Интеллектуальные игры и викторины");
            caps.Categories.AddCategoryMapping(939, TorznabCatType.TVOther, "|- [Видео Юмор] Реалити и ток-шоу / номинации / показы");
            caps.Categories.AddCategoryMapping(1481, TorznabCatType.TVOther, "|- [Видео Юмор] Детские телешоу");
            caps.Categories.AddCategoryMapping(113, TorznabCatType.TVOther, "|- [Видео Юмор] КВН");
            caps.Categories.AddCategoryMapping(115, TorznabCatType.TVOther, "|- [Видео Юмор] Пост КВН");
            caps.Categories.AddCategoryMapping(882, TorznabCatType.TVOther, "|- [Видео Юмор] Кривое Зеркало / Городок / В Городке");
            caps.Categories.AddCategoryMapping(1482, TorznabCatType.TVOther, "|- [Видео Юмор] Ледовые шоу");
            caps.Categories.AddCategoryMapping(393, TorznabCatType.TVOther, "|- [Видео Юмор] Музыкальные шоу");
            caps.Categories.AddCategoryMapping(1569, TorznabCatType.TVOther, "|- [Видео Юмор] Званый ужин");
            caps.Categories.AddCategoryMapping(373, TorznabCatType.TVOther, "|- [Видео Юмор] Хорошие Шутки");
            caps.Categories.AddCategoryMapping(1186, TorznabCatType.TVOther, "|- [Видео Юмор] Вечерний Квартал");
            caps.Categories.AddCategoryMapping(137, TorznabCatType.TVOther, "|- [Видео Юмор] Фильмы со смешным переводом (пародии)");
            caps.Categories.AddCategoryMapping(2537, TorznabCatType.TVOther, "|- [Видео Юмор] Stand-up comedy");
            caps.Categories.AddCategoryMapping(532, TorznabCatType.TVOther, "|- [Видео Юмор] Украинские Шоу");
            caps.Categories.AddCategoryMapping(827, TorznabCatType.TVOther, "|- [Видео Юмор] Танцевальные шоу, концерты, выступления");
            caps.Categories.AddCategoryMapping(1484, TorznabCatType.TVOther, "|- [Видео Юмор] Цирк");
            caps.Categories.AddCategoryMapping(1485, TorznabCatType.TVOther, "|- [Видео Юмор] Школа злословия");
            caps.Categories.AddCategoryMapping(114, TorznabCatType.TVOther, "|- [Видео Юмор] Сатирики и юмористы");
            caps.Categories.AddCategoryMapping(1332, TorznabCatType.TVOther, "|- Юмористические аудиопередачи");
            caps.Categories.AddCategoryMapping(1495, TorznabCatType.TVOther, "|- Аудио и видео ролики (Приколы и юмор)");
            caps.Categories.AddCategoryMapping(1346, TorznabCatType.TVSport, "XXXIII Летние Олимпийские игры 2024");
            caps.Categories.AddCategoryMapping(2493, TorznabCatType.TVSport, "|- Легкая атлетика. Плавание. Прыжки в воду. Синхронное плавание. Гим..");
            caps.Categories.AddCategoryMapping(2103, TorznabCatType.TVSport, "|- Велоспорт. Академическая гребля. Гребля на байдарках и каноэ");
            caps.Categories.AddCategoryMapping(2485, TorznabCatType.TVSport, "|- Футбол. Баскетбол. Волейбол. Гандбол. Водное поло. Регби. Хоккей н..");
            caps.Categories.AddCategoryMapping(2479, TorznabCatType.TVSport, "|- Теннис. Настольный теннис. Бадминтон");
            caps.Categories.AddCategoryMapping(2089, TorznabCatType.TVSport, "|- Фехтование. Стрельба. Стрельба из лука. Современное пятиборье");
            caps.Categories.AddCategoryMapping(2338, TorznabCatType.TVSport, "|- Бокс. Борьба Вольная и Греко-римская. Дзюдо. Карате. Тхэквондо");
            caps.Categories.AddCategoryMapping(927, TorznabCatType.TVSport, "|- Другие виды спорта");
            caps.Categories.AddCategoryMapping(1392, TorznabCatType.TVSport, "XXXII Летние Олимпийские игры 2020");
            caps.Categories.AddCategoryMapping(2475, TorznabCatType.TVSport, "|- Легкая атлетика. Плавание. Прыжки в воду. Синхронное плавание");
            caps.Categories.AddCategoryMapping(2113, TorznabCatType.TVSport, "|- Гимнастика. Прыжки на батуте. Фехтование. Стрельба. Современное пя..");
            caps.Categories.AddCategoryMapping(2482, TorznabCatType.TVSport, "|- Велоспорт. Академическая гребля. Гребля на байдарках и каноэ");
            caps.Categories.AddCategoryMapping(2522, TorznabCatType.TVSport, "|- Бокс. Борьба Вольная и Греко-римская. Дзюдо. Карате. Тхэквондо");
            caps.Categories.AddCategoryMapping(2486, TorznabCatType.TVSport, "|- Баскетбол. Волейбол. Гандбол. Водное поло. Регби. Хоккей на траве");
            caps.Categories.AddCategoryMapping(1794, TorznabCatType.TVSport, "|- Другие виды спорта");
            caps.Categories.AddCategoryMapping(1315, TorznabCatType.TVSport, "XXIV Зимние Олимпийские игры 2022");
            caps.Categories.AddCategoryMapping(1336, TorznabCatType.TVSport, "|- Биатлон");
            caps.Categories.AddCategoryMapping(2171, TorznabCatType.TVSport, "|- Лыжные гонки");
            caps.Categories.AddCategoryMapping(1339, TorznabCatType.TVSport, "|- Прыжки на лыжах с трамплина / Лыжное двоеборье");
            caps.Categories.AddCategoryMapping(2455, TorznabCatType.TVSport, "|- Горные лыжи / Сноубординг / Фристайл");
            caps.Categories.AddCategoryMapping(1434, TorznabCatType.TVSport, "|- Бобслей / Санный спорт / Скелетон");
            caps.Categories.AddCategoryMapping(2350, TorznabCatType.TVSport, "|- Конькобежный спорт / Шорт-трек");
            caps.Categories.AddCategoryMapping(1472, TorznabCatType.TVSport, "|- Фигурное катание");
            caps.Categories.AddCategoryMapping(2068, TorznabCatType.TVSport, "|- Хоккей");
            caps.Categories.AddCategoryMapping(2016, TorznabCatType.TVSport, "|- Керлинг");
            caps.Categories.AddCategoryMapping(1311, TorznabCatType.TVSport, "|- Обзорные и аналитические программы");
            caps.Categories.AddCategoryMapping(255, TorznabCatType.TVSport, "Спортивные турниры, фильмы и передачи");
            caps.Categories.AddCategoryMapping(256, TorznabCatType.TVSport, "|- Автоспорт");
            caps.Categories.AddCategoryMapping(1986, TorznabCatType.TVSport, "|- Мотоспорт");
            caps.Categories.AddCategoryMapping(660, TorznabCatType.TVSport, "|- Формула-1 (2023)");
            caps.Categories.AddCategoryMapping(1551, TorznabCatType.TVSport, "|- Формула-1 (2012-2022)");
            caps.Categories.AddCategoryMapping(626, TorznabCatType.TVSport, "|- Формула 1 (до 2011 вкл.)");
            caps.Categories.AddCategoryMapping(262, TorznabCatType.TVSport, "|- Велоспорт");
            caps.Categories.AddCategoryMapping(1326, TorznabCatType.TVSport, "|- Волейбол/Гандбол");
            caps.Categories.AddCategoryMapping(978, TorznabCatType.TVSport, "|- Бильярд");
            caps.Categories.AddCategoryMapping(1287, TorznabCatType.TVSport, "|- Покер");
            caps.Categories.AddCategoryMapping(1188, TorznabCatType.TVSport, "|- Бодибилдинг/Силовые виды спорта");
            caps.Categories.AddCategoryMapping(1667, TorznabCatType.TVSport, "|- Бокс");
            caps.Categories.AddCategoryMapping(1675, TorznabCatType.TVSport, "|- Классические единоборства");
            caps.Categories.AddCategoryMapping(257, TorznabCatType.TVSport, "|- Смешанные единоборства и K-1");
            caps.Categories.AddCategoryMapping(875, TorznabCatType.TVSport, "|- Американский футбол");
            caps.Categories.AddCategoryMapping(263, TorznabCatType.TVSport, "|- Регби");
            caps.Categories.AddCategoryMapping(2073, TorznabCatType.TVSport, "|- Бейсбол");
            caps.Categories.AddCategoryMapping(550, TorznabCatType.TVSport, "|- Теннис");
            caps.Categories.AddCategoryMapping(2124, TorznabCatType.TVSport, "|- Бадминтон/Настольный теннис");
            caps.Categories.AddCategoryMapping(1470, TorznabCatType.TVSport, "|- Гимнастика/Соревнования по танцам");
            caps.Categories.AddCategoryMapping(528, TorznabCatType.TVSport, "|- Лёгкая атлетика/Водные виды спорта");
            caps.Categories.AddCategoryMapping(486, TorznabCatType.TVSport, "|- Зимние виды спорта");
            caps.Categories.AddCategoryMapping(854, TorznabCatType.TVSport, "|- Фигурное катание");
            caps.Categories.AddCategoryMapping(2079, TorznabCatType.TVSport, "|- Биатлон");
            caps.Categories.AddCategoryMapping(260, TorznabCatType.TVSport, "|- Экстрим");
            caps.Categories.AddCategoryMapping(1319, TorznabCatType.TVSport, "|- Спорт (видео)");
            caps.Categories.AddCategoryMapping(1608, TorznabCatType.TVSport, "⚽ Футбол");
            caps.Categories.AddCategoryMapping(2294, TorznabCatType.TVSport, "|- UHDTV");
            caps.Categories.AddCategoryMapping(1693, TorznabCatType.TVSport, "|- Чемпионат Мира 2026 (отбор)");
            caps.Categories.AddCategoryMapping(136, TorznabCatType.TVSport, "|- Чемпионат Европы 2024 (отбор)");
            caps.Categories.AddCategoryMapping(2532, TorznabCatType.TVSport, "|- Чемпионат Европы 2020 [2021] (финальный турнир)");
            caps.Categories.AddCategoryMapping(592, TorznabCatType.TVSport, "|- Лига Наций");
            caps.Categories.AddCategoryMapping(1229, TorznabCatType.TVSport, "|- Чемпионат Мира 2022");
            caps.Categories.AddCategoryMapping(2533, TorznabCatType.TVSport, "|- Чемпионат Мира 2018 (игры)");
            caps.Categories.AddCategoryMapping(1952, TorznabCatType.TVSport, "|- Чемпионат Мира 2018 (обзорные передачи, документалистика)");
            caps.Categories.AddCategoryMapping(1621, TorznabCatType.TVSport, "|- Чемпионаты Мира");
            caps.Categories.AddCategoryMapping(2075, TorznabCatType.TVSport, "|- Россия 2024-2025");
            caps.Categories.AddCategoryMapping(1668, TorznabCatType.TVSport, "|- Россия 2023-2024");
            caps.Categories.AddCategoryMapping(1613, TorznabCatType.TVSport, "|- Россия/СССР");
            caps.Categories.AddCategoryMapping(1614, TorznabCatType.TVSport, "|- Англия");
            caps.Categories.AddCategoryMapping(1623, TorznabCatType.TVSport, "|- Испания");
            caps.Categories.AddCategoryMapping(1615, TorznabCatType.TVSport, "|- Италия");
            caps.Categories.AddCategoryMapping(1630, TorznabCatType.TVSport, "|- Германия");
            caps.Categories.AddCategoryMapping(2425, TorznabCatType.TVSport, "|- Франция");
            caps.Categories.AddCategoryMapping(2514, TorznabCatType.TVSport, "|- Украина");
            caps.Categories.AddCategoryMapping(1616, TorznabCatType.TVSport, "|- Другие национальные чемпионаты и кубки");
            caps.Categories.AddCategoryMapping(2014, TorznabCatType.TVSport, "|- Международные турниры");
            caps.Categories.AddCategoryMapping(1442, TorznabCatType.TVSport, "|- Еврокубки 2024-2025");
            caps.Categories.AddCategoryMapping(1491, TorznabCatType.TVSport, "|- Еврокубки 2023-2024");
            caps.Categories.AddCategoryMapping(1987, TorznabCatType.TVSport, "|- Еврокубки 2011-2023");
            caps.Categories.AddCategoryMapping(1617, TorznabCatType.TVSport, "|- Еврокубки");
            caps.Categories.AddCategoryMapping(1620, TorznabCatType.TVSport, "|- Чемпионаты Европы");
            caps.Categories.AddCategoryMapping(1998, TorznabCatType.TVSport, "|- Товарищеские турниры и матчи");
            caps.Categories.AddCategoryMapping(1343, TorznabCatType.TVSport, "|- Обзорные и аналитические передачи 2018-2023");
            caps.Categories.AddCategoryMapping(751, TorznabCatType.TVSport, "|- Обзорные и аналитические передачи");
            caps.Categories.AddCategoryMapping(497, TorznabCatType.TVSport, "|- Документальные фильмы (футбол)");
            caps.Categories.AddCategoryMapping(1697, TorznabCatType.TVSport, "|- Мини-футбол/Пляжный футбол");
            caps.Categories.AddCategoryMapping(2004, TorznabCatType.TVSport, "🏀 Баскетбол");
            caps.Categories.AddCategoryMapping(2001, TorznabCatType.TVSport, "|- Международные соревнования");
            caps.Categories.AddCategoryMapping(2002, TorznabCatType.TVSport, "|- NBA / NCAA (до 2000 г.)");
            caps.Categories.AddCategoryMapping(283, TorznabCatType.TVSport, "|- NBA / NCAA (2000-2010 гг.)");
            caps.Categories.AddCategoryMapping(1997, TorznabCatType.TVSport, "|- NBA / NCAA (2010-2024 гг.)");
            caps.Categories.AddCategoryMapping(2003, TorznabCatType.TVSport, "|- Европейский клубный баскетбол");
            caps.Categories.AddCategoryMapping(2009, TorznabCatType.TVSport, "🏒 Хоккей");
            caps.Categories.AddCategoryMapping(2010, TorznabCatType.TVSport, "|- Хоккей с мячом / Бенди");
            caps.Categories.AddCategoryMapping(2006, TorznabCatType.TVSport, "|- Международные турниры");
            caps.Categories.AddCategoryMapping(2007, TorznabCatType.TVSport, "|- КХЛ");
            caps.Categories.AddCategoryMapping(2005, TorznabCatType.TVSport, "|- НХЛ (до 2011/12)");
            caps.Categories.AddCategoryMapping(259, TorznabCatType.TVSport, "|- НХЛ (с 2013)");
            caps.Categories.AddCategoryMapping(2008, TorznabCatType.TVSport, "|- СССР - Канада");
            caps.Categories.AddCategoryMapping(126, TorznabCatType.TVSport, "|- Документальные фильмы и аналитика");
            caps.Categories.AddCategoryMapping(845, TorznabCatType.TVSport, "Рестлинг");
            caps.Categories.AddCategoryMapping(343, TorznabCatType.TVSport, "|- Professional Wrestling");
            caps.Categories.AddCategoryMapping(2111, TorznabCatType.TVSport, "|- Independent Wrestling");
            caps.Categories.AddCategoryMapping(1527, TorznabCatType.TVSport, "|- International Wrestling");
            caps.Categories.AddCategoryMapping(2069, TorznabCatType.TVSport, "|- Oldschool Wrestling");
            caps.Categories.AddCategoryMapping(1323, TorznabCatType.TVSport, "|- Documentary Wrestling");
            caps.Categories.AddCategoryMapping(1411, TorznabCatType.Books, "|- Сканирование, обработка сканов");
            caps.Categories.AddCategoryMapping(21, TorznabCatType.Books, "Книги и журналы (общий раздел)");
            caps.Categories.AddCategoryMapping(2157, TorznabCatType.Books, "|- Кино, театр, ТВ, мультипликация, цирк");
            caps.Categories.AddCategoryMapping(765, TorznabCatType.Books, "|- Рисунок, графический дизайн");
            caps.Categories.AddCategoryMapping(2019, TorznabCatType.Books, "|- Фото и видеосъемка");
            caps.Categories.AddCategoryMapping(31, TorznabCatType.BooksMags, "|- Журналы и газеты (общий раздел)");
            caps.Categories.AddCategoryMapping(1427, TorznabCatType.Books, "|- Эзотерика, гадания, магия, фен-шуй");
            caps.Categories.AddCategoryMapping(2422, TorznabCatType.Books, "|- Астрология");
            caps.Categories.AddCategoryMapping(2195, TorznabCatType.Books, "|- Красота. Уход. Домоводство");
            caps.Categories.AddCategoryMapping(2521, TorznabCatType.Books, "|- Мода. Стиль. Этикет");
            caps.Categories.AddCategoryMapping(2223, TorznabCatType.Books, "|- Путешествия и туризм");
            caps.Categories.AddCategoryMapping(2447, TorznabCatType.Books, "|- Знаменитости и кумиры");
            caps.Categories.AddCategoryMapping(39, TorznabCatType.Books, "|- Разное (книги)");
            caps.Categories.AddCategoryMapping(2086, TorznabCatType.Books, "|- Самиздат, статьи из журналов, фрагменты книг");
            caps.Categories.AddCategoryMapping(1101, TorznabCatType.Books, "Для детей, родителей и учителей");
            caps.Categories.AddCategoryMapping(745, TorznabCatType.Books, "|- Учебная литература для детского сада и начальной школы (до 4 класса)");
            caps.Categories.AddCategoryMapping(1689, TorznabCatType.Books, "|- Учебная литература для старших классов (5-11 класс)");
            caps.Categories.AddCategoryMapping(2336, TorznabCatType.Books, "|- Учителям и педагогам");
            caps.Categories.AddCategoryMapping(2337, TorznabCatType.Books, "|- Научно-популярная и познавательная литература (для детей)");
            caps.Categories.AddCategoryMapping(1353, TorznabCatType.Books, "|- Досуг и творчество");
            caps.Categories.AddCategoryMapping(1400, TorznabCatType.Books, "|- Воспитание и развитие");
            caps.Categories.AddCategoryMapping(1415, TorznabCatType.Books, "|- Худ. лит-ра для дошкольников и младших классов");
            caps.Categories.AddCategoryMapping(2046, TorznabCatType.Books, "|- Худ. лит-ра для средних и старших классов");
            caps.Categories.AddCategoryMapping(1802, TorznabCatType.Books, "Спорт, физическая культура, боевые искусства");
            caps.Categories.AddCategoryMapping(2189, TorznabCatType.Books, "|- Футбол (книги и журналы)");
            caps.Categories.AddCategoryMapping(2190, TorznabCatType.Books, "|- Хоккей (книги и журналы)");
            caps.Categories.AddCategoryMapping(2443, TorznabCatType.Books, "|- Игровые виды спорта");
            caps.Categories.AddCategoryMapping(1477, TorznabCatType.Books, "|- Легкая атлетика. Плавание. Гимнастика. Тяжелая атлетика. Гребля");
            caps.Categories.AddCategoryMapping(669, TorznabCatType.Books, "|- Автоспорт. Мотоспорт. Велоспорт");
            caps.Categories.AddCategoryMapping(2196, TorznabCatType.Books, "|- Шахматы. Шашки");
            caps.Categories.AddCategoryMapping(2056, TorznabCatType.Books, "|- Боевые искусства, единоборства");
            caps.Categories.AddCategoryMapping(1436, TorznabCatType.Books, "|- Экстрим (книги)");
            caps.Categories.AddCategoryMapping(2191, TorznabCatType.Books, "|- Физкультура, фитнес, бодибилдинг");
            caps.Categories.AddCategoryMapping(2477, TorznabCatType.Books, "|- Спортивная пресса");
            caps.Categories.AddCategoryMapping(1680, TorznabCatType.Books, "Гуманитарные науки");
            caps.Categories.AddCategoryMapping(1684, TorznabCatType.Books, "|- Искусствоведение. Культурология");
            caps.Categories.AddCategoryMapping(2446, TorznabCatType.Books, "|- Фольклор. Эпос. Мифология");
            caps.Categories.AddCategoryMapping(2524, TorznabCatType.Books, "|- Литературоведение");
            caps.Categories.AddCategoryMapping(2525, TorznabCatType.Books, "|- Лингвистика");
            caps.Categories.AddCategoryMapping(995, TorznabCatType.Books, "|- Философия");
            caps.Categories.AddCategoryMapping(2022, TorznabCatType.Books, "|- Политология");
            caps.Categories.AddCategoryMapping(2471, TorznabCatType.Books, "|- Социология");
            caps.Categories.AddCategoryMapping(2375, TorznabCatType.Books, "|- Публицистика, журналистика");
            caps.Categories.AddCategoryMapping(764, TorznabCatType.Books, "|- Бизнес, менеджмент");
            caps.Categories.AddCategoryMapping(1685, TorznabCatType.Books, "|- Маркетинг");
            caps.Categories.AddCategoryMapping(1688, TorznabCatType.Books, "|- Экономика");
            caps.Categories.AddCategoryMapping(2472, TorznabCatType.Books, "|- Финансы");
            caps.Categories.AddCategoryMapping(1687, TorznabCatType.Books, "|- Юридические науки. Право. Криминалистика");
            caps.Categories.AddCategoryMapping(2020, TorznabCatType.Books, "Исторические науки");
            caps.Categories.AddCategoryMapping(1349, TorznabCatType.Books, "|- Методология и философия исторической науки");
            caps.Categories.AddCategoryMapping(1967, TorznabCatType.Books, "|- Исторические источники (книги, периодика)");
            caps.Categories.AddCategoryMapping(1341, TorznabCatType.Books, "|- Исторические источники (документы)");
            caps.Categories.AddCategoryMapping(2049, TorznabCatType.Books, "|- Исторические персоны");
            caps.Categories.AddCategoryMapping(1681, TorznabCatType.Books, "|- Альтернативные исторические теории");
            caps.Categories.AddCategoryMapping(2319, TorznabCatType.Books, "|- Археология");
            caps.Categories.AddCategoryMapping(2434, TorznabCatType.Books, "|- Древний мир. Античность");
            caps.Categories.AddCategoryMapping(1683, TorznabCatType.Books, "|- Средние века");
            caps.Categories.AddCategoryMapping(2444, TorznabCatType.Books, "|- История Нового и Новейшего времени");
            caps.Categories.AddCategoryMapping(2427, TorznabCatType.Books, "|- История Европы");
            caps.Categories.AddCategoryMapping(2452, TorznabCatType.Books, "|- История Азии и Африки");
            caps.Categories.AddCategoryMapping(2445, TorznabCatType.Books, "|- История Америки, Австралии, Океании");
            caps.Categories.AddCategoryMapping(2435, TorznabCatType.Books, "|- История России");
            caps.Categories.AddCategoryMapping(667, TorznabCatType.Books, "|- История России до 1917 года");
            caps.Categories.AddCategoryMapping(2436, TorznabCatType.Books, "|- Эпоха СССР");
            caps.Categories.AddCategoryMapping(1335, TorznabCatType.Books, "|- История России после 1991 года");
            caps.Categories.AddCategoryMapping(2453, TorznabCatType.Books, "|- История стран бывшего СССР");
            caps.Categories.AddCategoryMapping(2320, TorznabCatType.Books, "|- Этнография, антропология");
            caps.Categories.AddCategoryMapping(1801, TorznabCatType.Books, "|- Международные отношения. Дипломатия");
            caps.Categories.AddCategoryMapping(2023, TorznabCatType.BooksTechnical, "Точные, естественные и инженерные науки");
            caps.Categories.AddCategoryMapping(2024, TorznabCatType.BooksTechnical, "|- Авиация / Космонавтика");
            caps.Categories.AddCategoryMapping(2026, TorznabCatType.BooksTechnical, "|- Физика");
            caps.Categories.AddCategoryMapping(2192, TorznabCatType.BooksTechnical, "|- Астрономия");
            caps.Categories.AddCategoryMapping(2027, TorznabCatType.BooksTechnical, "|- Биология / Экология");
            caps.Categories.AddCategoryMapping(295, TorznabCatType.BooksTechnical, "|- Химия / Биохимия");
            caps.Categories.AddCategoryMapping(2028, TorznabCatType.BooksTechnical, "|- Математика");
            caps.Categories.AddCategoryMapping(2029, TorznabCatType.BooksTechnical, "|- География / Геология / Геодезия");
            caps.Categories.AddCategoryMapping(1325, TorznabCatType.BooksTechnical, "|- Электроника / Радио");
            caps.Categories.AddCategoryMapping(2386, TorznabCatType.BooksTechnical, "|- Схемы и сервис-мануалы (оригинальная документация)");
            caps.Categories.AddCategoryMapping(2031, TorznabCatType.BooksTechnical, "|- Архитектура / Строительство / Инженерные сети / Ландшафтный дизайн");
            caps.Categories.AddCategoryMapping(2030, TorznabCatType.BooksTechnical, "|- Машиностроение");
            caps.Categories.AddCategoryMapping(2526, TorznabCatType.BooksTechnical, "|- Сварка / Пайка / Неразрушающий контроль");
            caps.Categories.AddCategoryMapping(2527, TorznabCatType.BooksTechnical, "|- Автоматизация / Робототехника");
            caps.Categories.AddCategoryMapping(2254, TorznabCatType.BooksTechnical, "|- Металлургия / Материаловедение");
            caps.Categories.AddCategoryMapping(2376, TorznabCatType.BooksTechnical, "|- Механика, сопротивление материалов");
            caps.Categories.AddCategoryMapping(2054, TorznabCatType.BooksTechnical, "|- Энергетика / электротехника");
            caps.Categories.AddCategoryMapping(770, TorznabCatType.BooksTechnical, "|- Нефтяная, газовая и химическая промышленность");
            caps.Categories.AddCategoryMapping(2476, TorznabCatType.BooksTechnical, "|- Сельское хозяйство и пищевая промышленность");
            caps.Categories.AddCategoryMapping(2494, TorznabCatType.BooksTechnical, "|- Железнодорожное дело");
            caps.Categories.AddCategoryMapping(1528, TorznabCatType.BooksTechnical, "|- Нормативная документация");
            caps.Categories.AddCategoryMapping(2032, TorznabCatType.BooksTechnical, "|- Журналы: научные, научно-популярные, радио и др.");
            caps.Categories.AddCategoryMapping(919, TorznabCatType.Books, "Ноты и Музыкальная литература");
            caps.Categories.AddCategoryMapping(944, TorznabCatType.Books, "|- Академическая музыка (Ноты и Media CD)");
            caps.Categories.AddCategoryMapping(980, TorznabCatType.Books, "|- Другие направления (Ноты, табулатуры)");
            caps.Categories.AddCategoryMapping(946, TorznabCatType.Books, "|- Самоучители и Школы");
            caps.Categories.AddCategoryMapping(977, TorznabCatType.Books, "|- Песенники (Songbooks)");
            caps.Categories.AddCategoryMapping(2074, TorznabCatType.Books, "|- Музыкальная литература и Теория");
            caps.Categories.AddCategoryMapping(2349, TorznabCatType.Books, "|- Музыкальные журналы");
            caps.Categories.AddCategoryMapping(768, TorznabCatType.Books, "Военное дело");
            caps.Categories.AddCategoryMapping(2099, TorznabCatType.Books, "|- Милитария");
            caps.Categories.AddCategoryMapping(2021, TorznabCatType.Books, "|- Военная история");
            caps.Categories.AddCategoryMapping(2437, TorznabCatType.Books, "|- История Второй мировой войны");
            caps.Categories.AddCategoryMapping(1337, TorznabCatType.Books, "|- Биографии и мемуары военных деятелей");
            caps.Categories.AddCategoryMapping(1447, TorznabCatType.Books, "|- Военная техника");
            caps.Categories.AddCategoryMapping(2468, TorznabCatType.Books, "|- Стрелковое оружие");
            caps.Categories.AddCategoryMapping(2469, TorznabCatType.Books, "|- Учебно-методическая литература");
            caps.Categories.AddCategoryMapping(2470, TorznabCatType.Books, "|- Спецслужбы мира");
            caps.Categories.AddCategoryMapping(1686, TorznabCatType.Books, "Вера и религия");
            caps.Categories.AddCategoryMapping(2215, TorznabCatType.Books, "|- Христианство");
            caps.Categories.AddCategoryMapping(2216, TorznabCatType.Books, "|- Ислам");
            caps.Categories.AddCategoryMapping(2217, TorznabCatType.Books, "|- Религии Индии, Тибета и Восточной Азии / Иудаизм");
            caps.Categories.AddCategoryMapping(2218, TorznabCatType.Books, "|- Нетрадиционные религиозные, духовные и мистические учения");
            caps.Categories.AddCategoryMapping(2252, TorznabCatType.Books, "|- Религиоведение. История Религии");
            caps.Categories.AddCategoryMapping(2543, TorznabCatType.Books, "|- Атеизм. Научный атеизм");
            caps.Categories.AddCategoryMapping(767, TorznabCatType.Books, "Психология");
            caps.Categories.AddCategoryMapping(2515, TorznabCatType.Books, "|- Общая и прикладная психология");
            caps.Categories.AddCategoryMapping(2516, TorznabCatType.Books, "|- Психотерапия и консультирование");
            caps.Categories.AddCategoryMapping(2517, TorznabCatType.Books, "|- Психодиагностика и психокоррекция");
            caps.Categories.AddCategoryMapping(2518, TorznabCatType.Books, "|- Социальная психология и психология отношений");
            caps.Categories.AddCategoryMapping(2519, TorznabCatType.Books, "|- Тренинг и коучинг");
            caps.Categories.AddCategoryMapping(2520, TorznabCatType.Books, "|- Саморазвитие и самосовершенствование");
            caps.Categories.AddCategoryMapping(1696, TorznabCatType.Books, "|- Популярная психология");
            caps.Categories.AddCategoryMapping(2253, TorznabCatType.Books, "|- Сексология. Взаимоотношения полов (18+)");
            caps.Categories.AddCategoryMapping(2033, TorznabCatType.Books, "Коллекционирование, увлечения и хобби");
            caps.Categories.AddCategoryMapping(1412, TorznabCatType.Books, "|- Коллекционирование и вспомогательные ист. дисциплины");
            caps.Categories.AddCategoryMapping(1446, TorznabCatType.Books, "|- Вышивание");
            caps.Categories.AddCategoryMapping(753, TorznabCatType.Books, "|- Вязание");
            caps.Categories.AddCategoryMapping(2037, TorznabCatType.Books, "|- Шитье, пэчворк");
            caps.Categories.AddCategoryMapping(2224, TorznabCatType.Books, "|- Кружевоплетение");
            caps.Categories.AddCategoryMapping(2194, TorznabCatType.Books, "|- Бисероплетение. Ювелирика. Украшения из проволоки.");
            caps.Categories.AddCategoryMapping(2418, TorznabCatType.Books, "|- Бумажный арт");
            caps.Categories.AddCategoryMapping(1410, TorznabCatType.Books, "|- Другие виды декоративно-прикладного искусства");
            caps.Categories.AddCategoryMapping(2034, TorznabCatType.Books, "|- Домашние питомцы и аквариумистика");
            caps.Categories.AddCategoryMapping(2433, TorznabCatType.Books, "|- Охота и рыбалка");
            caps.Categories.AddCategoryMapping(1961, TorznabCatType.Books, "|- Кулинария (книги)");
            caps.Categories.AddCategoryMapping(2432, TorznabCatType.Books, "|- Кулинария (газеты и журналы)");
            caps.Categories.AddCategoryMapping(565, TorznabCatType.Books, "|- Моделизм");
            caps.Categories.AddCategoryMapping(1523, TorznabCatType.Books, "|- Приусадебное хозяйство / Цветоводство");
            caps.Categories.AddCategoryMapping(1575, TorznabCatType.Books, "|- Ремонт, частное строительство, дизайн интерьеров");
            caps.Categories.AddCategoryMapping(1520, TorznabCatType.Books, "|- Деревообработка");
            caps.Categories.AddCategoryMapping(2424, TorznabCatType.Books, "|- Настольные игры");
            caps.Categories.AddCategoryMapping(769, TorznabCatType.Books, "|- Прочие хобби и игры");
            caps.Categories.AddCategoryMapping(2038, TorznabCatType.Books, "Художественная литература");
            caps.Categories.AddCategoryMapping(2043, TorznabCatType.Books, "|- Русская литература");
            caps.Categories.AddCategoryMapping(2042, TorznabCatType.Books, "|- Зарубежная литература (до 1900 г.)");
            caps.Categories.AddCategoryMapping(2041, TorznabCatType.Books, "|- Зарубежная литература (XX и XXI век)");
            caps.Categories.AddCategoryMapping(2044, TorznabCatType.Books, "|- Детектив, боевик");
            caps.Categories.AddCategoryMapping(2039, TorznabCatType.Books, "|- Женский роман");
            caps.Categories.AddCategoryMapping(2045, TorznabCatType.Books, "|- Отечественная фантастика / фэнтези / мистика");
            caps.Categories.AddCategoryMapping(2080, TorznabCatType.Books, "|- Зарубежная фантастика / фэнтези / мистика");
            caps.Categories.AddCategoryMapping(2047, TorznabCatType.Books, "|- Приключения");
            caps.Categories.AddCategoryMapping(2193, TorznabCatType.Books, "|- Литературные журналы");
            caps.Categories.AddCategoryMapping(1037, TorznabCatType.Books, "|- Самиздат и книги, изданные за счет авторов");
            caps.Categories.AddCategoryMapping(1418, TorznabCatType.BooksTechnical, "Компьютерная литература");
            caps.Categories.AddCategoryMapping(1422, TorznabCatType.BooksTechnical, "|- Программы от Microsoft");
            caps.Categories.AddCategoryMapping(1423, TorznabCatType.BooksTechnical, "|- Другие программы");
            caps.Categories.AddCategoryMapping(1424, TorznabCatType.BooksTechnical, "|- Mac OS; Linux, FreeBSD и прочие *NIX");
            caps.Categories.AddCategoryMapping(1445, TorznabCatType.BooksTechnical, "|- СУБД");
            caps.Categories.AddCategoryMapping(1425, TorznabCatType.BooksTechnical, "|- Веб-дизайн и программирование");
            caps.Categories.AddCategoryMapping(1426, TorznabCatType.BooksTechnical, "|- Программирование (книги)");
            caps.Categories.AddCategoryMapping(1428, TorznabCatType.BooksTechnical, "|- Графика, обработка видео");
            caps.Categories.AddCategoryMapping(1429, TorznabCatType.BooksTechnical, "|- Сети / VoIP");
            caps.Categories.AddCategoryMapping(1430, TorznabCatType.BooksTechnical, "|- Хакинг и безопасность");
            caps.Categories.AddCategoryMapping(1431, TorznabCatType.BooksTechnical, "|- Железо (книги о ПК)");
            caps.Categories.AddCategoryMapping(1433, TorznabCatType.BooksTechnical, "|- Инженерные и научные программы (книги)");
            caps.Categories.AddCategoryMapping(1432, TorznabCatType.BooksTechnical, "|- Компьютерные журналы и приложения к ним");
            caps.Categories.AddCategoryMapping(2202, TorznabCatType.BooksTechnical, "|- Дисковые приложения к игровым журналам");
            caps.Categories.AddCategoryMapping(862, TorznabCatType.BooksComics, "Комиксы, манга, ранобэ");
            caps.Categories.AddCategoryMapping(2461, TorznabCatType.BooksComics, "|- Комиксы на русском языке");
            caps.Categories.AddCategoryMapping(2462, TorznabCatType.BooksComics, "|- Комиксы издательства Marvel");
            caps.Categories.AddCategoryMapping(2463, TorznabCatType.BooksComics, "|- Комиксы издательства DC");
            caps.Categories.AddCategoryMapping(2464, TorznabCatType.BooksComics, "|- Комиксы других издательств");
            caps.Categories.AddCategoryMapping(2473, TorznabCatType.BooksComics, "|- Комиксы на других языках");
            caps.Categories.AddCategoryMapping(281, TorznabCatType.BooksComics, "|- Манга (на русском языке)");
            caps.Categories.AddCategoryMapping(2465, TorznabCatType.BooksComics, "|- Манга (на иностранных языках)");
            caps.Categories.AddCategoryMapping(2458, TorznabCatType.BooksComics, "|- Ранобэ");
            caps.Categories.AddCategoryMapping(2048, TorznabCatType.BooksOther, "Коллекции книг и библиотеки");
            caps.Categories.AddCategoryMapping(1238, TorznabCatType.BooksOther, "|- Библиотеки (зеркала сетевых библиотек/коллекций)");
            caps.Categories.AddCategoryMapping(2055, TorznabCatType.BooksOther, "|- Тематические коллекции (подборки)");
            caps.Categories.AddCategoryMapping(754, TorznabCatType.BooksOther, "|- Многопредметные коллекции (подборки)");
            caps.Categories.AddCategoryMapping(2114, TorznabCatType.BooksEBook, "Мультимедийные и интерактивные издания");
            caps.Categories.AddCategoryMapping(2438, TorznabCatType.BooksEBook, "|- Мультимедийные энциклопедии");
            caps.Categories.AddCategoryMapping(2439, TorznabCatType.BooksEBook, "|- Интерактивные обучающие и развивающие материалы");
            caps.Categories.AddCategoryMapping(2440, TorznabCatType.BooksEBook, "|- Обучающие издания для детей");
            caps.Categories.AddCategoryMapping(2441, TorznabCatType.BooksEBook, "|- Кулинария. Цветоводство. Домоводство");
            caps.Categories.AddCategoryMapping(2442, TorznabCatType.BooksEBook, "|- Культура. Искусство. История");
            caps.Categories.AddCategoryMapping(2125, TorznabCatType.Books, "Медицина и здоровье");
            caps.Categories.AddCategoryMapping(2133, TorznabCatType.Books, "|- Клиническая медицина до 1980 год");
            caps.Categories.AddCategoryMapping(2130, TorznabCatType.Books, "|- Клиническая медицина с 1980 по 2000 год");
            caps.Categories.AddCategoryMapping(2313, TorznabCatType.Books, "|- Клиническая медицина после 2000 год");
            caps.Categories.AddCategoryMapping(2528, TorznabCatType.Books, "|- Научная медицинская периодика (газеты и журналы)");
            caps.Categories.AddCategoryMapping(2129, TorznabCatType.Books, "|- Медико-биологические науки");
            caps.Categories.AddCategoryMapping(2141, TorznabCatType.Books, "|- Фармация и фармакология");
            caps.Categories.AddCategoryMapping(2314, TorznabCatType.Books, "|- Популярная медицинская периодика (газеты и журналы)");
            caps.Categories.AddCategoryMapping(2132, TorznabCatType.Books, "|- Нетрадиционная, народная медицина и популярные книги о здоровье");
            caps.Categories.AddCategoryMapping(2131, TorznabCatType.Books, "|- Ветеринария, разное");
            caps.Categories.AddCategoryMapping(2315, TorznabCatType.Books, "|- Тематические коллекции книг");
            caps.Categories.AddCategoryMapping(2362, TorznabCatType.BooksEBook, "Иностранные языки для взрослых");
            caps.Categories.AddCategoryMapping(1265, TorznabCatType.BooksEBook, "|- Английский язык (для взрослых)");
            caps.Categories.AddCategoryMapping(1266, TorznabCatType.BooksEBook, "|- Немецкий язык");
            caps.Categories.AddCategoryMapping(1267, TorznabCatType.BooksEBook, "|- Французский язык");
            caps.Categories.AddCategoryMapping(1358, TorznabCatType.BooksEBook, "|- Испанский язык");
            caps.Categories.AddCategoryMapping(2363, TorznabCatType.BooksEBook, "|- Итальянский язык");
            caps.Categories.AddCategoryMapping(734, TorznabCatType.BooksEBook, "|- Финский язык");
            caps.Categories.AddCategoryMapping(1268, TorznabCatType.BooksEBook, "|- Другие европейские языки");
            caps.Categories.AddCategoryMapping(1673, TorznabCatType.BooksEBook, "|- Арабский язык");
            caps.Categories.AddCategoryMapping(1269, TorznabCatType.BooksEBook, "|- Китайский язык");
            caps.Categories.AddCategoryMapping(1270, TorznabCatType.BooksEBook, "|- Японский язык");
            caps.Categories.AddCategoryMapping(1275, TorznabCatType.BooksEBook, "|- Другие восточные языки");
            caps.Categories.AddCategoryMapping(2364, TorznabCatType.BooksEBook, "|- Русский язык как иностранный");
            caps.Categories.AddCategoryMapping(1276, TorznabCatType.BooksEBook, "|- Мультиязычные сборники и курсы");
            caps.Categories.AddCategoryMapping(2094, TorznabCatType.BooksEBook, "|- LIM-курсы");
            caps.Categories.AddCategoryMapping(1274, TorznabCatType.BooksEBook, "|- Разное (иностранные языки)");
            caps.Categories.AddCategoryMapping(1264, TorznabCatType.BooksEBook, "Иностранные языки для детей");
            caps.Categories.AddCategoryMapping(2358, TorznabCatType.BooksEBook, "|- Английский язык (для детей)");
            caps.Categories.AddCategoryMapping(2359, TorznabCatType.BooksEBook, "|- Другие европейские языки (для детей)");
            caps.Categories.AddCategoryMapping(2360, TorznabCatType.BooksEBook, "|- Восточные языки (для детей)");
            caps.Categories.AddCategoryMapping(2361, TorznabCatType.BooksEBook, "|- Школьные учебники, ЕГЭ, ОГЭ");
            caps.Categories.AddCategoryMapping(2057, TorznabCatType.BooksEBook, "Художественная литература (ин.языки)");
            caps.Categories.AddCategoryMapping(2355, TorznabCatType.BooksEBook, "|- Художественная литература на английском языке");
            caps.Categories.AddCategoryMapping(2474, TorznabCatType.BooksEBook, "|- Художественная литература на французском языке");
            caps.Categories.AddCategoryMapping(2356, TorznabCatType.BooksEBook, "|- Художественная литература на других европейских языках");
            caps.Categories.AddCategoryMapping(2357, TorznabCatType.BooksEBook, "|- Художественная литература на восточных языках");
            caps.Categories.AddCategoryMapping(2413, TorznabCatType.AudioAudiobook, "Аудиокниги на иностранных языках");
            caps.Categories.AddCategoryMapping(1501, TorznabCatType.AudioAudiobook, "|- Аудиокниги на английском языке");
            caps.Categories.AddCategoryMapping(1580, TorznabCatType.AudioAudiobook, "|- Аудиокниги на немецком языке");
            caps.Categories.AddCategoryMapping(525, TorznabCatType.AudioAudiobook, "|- Аудиокниги на других иностранных языках");
            caps.Categories.AddCategoryMapping(610, TorznabCatType.BooksOther, "Видеоуроки и обучающие интерактивные DVD");
            caps.Categories.AddCategoryMapping(1568, TorznabCatType.BooksOther, "|- Кулинария");
            caps.Categories.AddCategoryMapping(1542, TorznabCatType.BooksOther, "|- Спорт");
            caps.Categories.AddCategoryMapping(2335, TorznabCatType.BooksOther, "|- Фитнес - Кардио-Силовые Тренировки");
            caps.Categories.AddCategoryMapping(1544, TorznabCatType.BooksOther, "|- Фитнес - Разум и Тело");
            caps.Categories.AddCategoryMapping(1546, TorznabCatType.BooksOther, "|- Бодибилдинг");
            caps.Categories.AddCategoryMapping(1549, TorznabCatType.BooksOther, "|- Оздоровительные практики");
            caps.Categories.AddCategoryMapping(1597, TorznabCatType.BooksOther, "|- Йога");
            caps.Categories.AddCategoryMapping(1552, TorznabCatType.BooksOther, "|- Видео- и фотосъёмка");
            caps.Categories.AddCategoryMapping(1550, TorznabCatType.BooksOther, "|- Уход за собой");
            caps.Categories.AddCategoryMapping(1553, TorznabCatType.BooksOther, "|- Рисование");
            caps.Categories.AddCategoryMapping(1554, TorznabCatType.BooksOther, "|- Игра на гитаре");
            caps.Categories.AddCategoryMapping(617, TorznabCatType.BooksOther, "|- Ударные инструменты");
            caps.Categories.AddCategoryMapping(1555, TorznabCatType.BooksOther, "|- Другие музыкальные инструменты");
            caps.Categories.AddCategoryMapping(2017, TorznabCatType.BooksOther, "|- Игра на бас-гитаре");
            caps.Categories.AddCategoryMapping(1257, TorznabCatType.BooksOther, "|- Бальные танцы");
            caps.Categories.AddCategoryMapping(1258, TorznabCatType.BooksOther, "|- Танец живота");
            caps.Categories.AddCategoryMapping(2208, TorznabCatType.BooksOther, "|- Уличные и клубные танцы");
            caps.Categories.AddCategoryMapping(677, TorznabCatType.BooksOther, "|- Танцы, разное");
            caps.Categories.AddCategoryMapping(1255, TorznabCatType.BooksOther, "|- Охота");
            caps.Categories.AddCategoryMapping(1479, TorznabCatType.BooksOther, "|- Рыболовство и подводная охота");
            caps.Categories.AddCategoryMapping(1261, TorznabCatType.BooksOther, "|- Фокусы и трюки");
            caps.Categories.AddCategoryMapping(614, TorznabCatType.BooksOther, "|- Образование");
            caps.Categories.AddCategoryMapping(1583, TorznabCatType.BooksOther, "|- Финансы");
            caps.Categories.AddCategoryMapping(1259, TorznabCatType.BooksOther, "|- Продажи, бизнес");
            caps.Categories.AddCategoryMapping(2065, TorznabCatType.BooksOther, "|- Беременность, роды, материнство");
            caps.Categories.AddCategoryMapping(1254, TorznabCatType.BooksOther, "|- Учебные видео для детей");
            caps.Categories.AddCategoryMapping(1260, TorznabCatType.BooksOther, "|- Психология");
            caps.Categories.AddCategoryMapping(2209, TorznabCatType.BooksOther, "|- Эзотерика, саморазвитие");
            caps.Categories.AddCategoryMapping(2210, TorznabCatType.BooksOther, "|- Пикап, знакомства");
            caps.Categories.AddCategoryMapping(1547, TorznabCatType.BooksOther, "|- Строительство, ремонт и дизайн");
            caps.Categories.AddCategoryMapping(1548, TorznabCatType.BooksOther, "|- Дерево- и металлообработка");
            caps.Categories.AddCategoryMapping(2211, TorznabCatType.BooksOther, "|- Растения и животные");
            caps.Categories.AddCategoryMapping(1596, TorznabCatType.BooksOther, "|- Хобби и рукоделие");
            caps.Categories.AddCategoryMapping(2135, TorznabCatType.BooksOther, "|- Медицина и стоматология");
            caps.Categories.AddCategoryMapping(2140, TorznabCatType.BooksOther, "|- Психотерапия и клиническая психология");
            caps.Categories.AddCategoryMapping(2136, TorznabCatType.BooksOther, "|- Массаж");
            caps.Categories.AddCategoryMapping(2138, TorznabCatType.BooksOther, "|- Здоровье");
            caps.Categories.AddCategoryMapping(615, TorznabCatType.BooksOther, "|- Разное");
            caps.Categories.AddCategoryMapping(1581, TorznabCatType.BooksOther, "Боевые искусства (Видеоуроки)");
            caps.Categories.AddCategoryMapping(1590, TorznabCatType.BooksOther, "|- Айкидо и айки-дзюцу");
            caps.Categories.AddCategoryMapping(1587, TorznabCatType.BooksOther, "|- Вин чун");
            caps.Categories.AddCategoryMapping(1594, TorznabCatType.BooksOther, "|- Джиу-джитсу");
            caps.Categories.AddCategoryMapping(1591, TorznabCatType.BooksOther, "|- Дзюдо и самбо");
            caps.Categories.AddCategoryMapping(1588, TorznabCatType.BooksOther, "|- Каратэ");
            caps.Categories.AddCategoryMapping(1585, TorznabCatType.BooksOther, "|- Работа с оружием");
            caps.Categories.AddCategoryMapping(1586, TorznabCatType.BooksOther, "|- Русский стиль");
            caps.Categories.AddCategoryMapping(2078, TorznabCatType.BooksOther, "|- Рукопашный бой");
            caps.Categories.AddCategoryMapping(1929, TorznabCatType.BooksOther, "|- Смешанные стили");
            caps.Categories.AddCategoryMapping(1593, TorznabCatType.BooksOther, "|- Ударные стили");
            caps.Categories.AddCategoryMapping(1592, TorznabCatType.BooksOther, "|- Ушу");
            caps.Categories.AddCategoryMapping(1595, TorznabCatType.BooksOther, "|- Разное");
            caps.Categories.AddCategoryMapping(1556, TorznabCatType.BooksTechnical, "Компьютерные видеоуроки и обучающие интерактивные DVD");
            caps.Categories.AddCategoryMapping(1560, TorznabCatType.BooksTechnical, "|- Компьютерные сети и безопасность");
            caps.Categories.AddCategoryMapping(1991, TorznabCatType.BooksTechnical, "|- Devops");
            caps.Categories.AddCategoryMapping(1561, TorznabCatType.BooksTechnical, "|- ОС и серверные программы Microsoft");
            caps.Categories.AddCategoryMapping(1653, TorznabCatType.BooksTechnical, "|- Офисные программы Microsoft");
            caps.Categories.AddCategoryMapping(1570, TorznabCatType.BooksTechnical, "|- ОС и программы семейства UNIX");
            caps.Categories.AddCategoryMapping(1654, TorznabCatType.BooksTechnical, "|- Adobe Photoshop");
            caps.Categories.AddCategoryMapping(1655, TorznabCatType.BooksTechnical, "|- Autodesk Maya");
            caps.Categories.AddCategoryMapping(1656, TorznabCatType.BooksTechnical, "|- Autodesk 3ds Max");
            caps.Categories.AddCategoryMapping(1930, TorznabCatType.BooksTechnical, "|- Autodesk Softimage (XSI)");
            caps.Categories.AddCategoryMapping(1931, TorznabCatType.BooksTechnical, "|- ZBrush");
            caps.Categories.AddCategoryMapping(1932, TorznabCatType.BooksTechnical, "|- Flash, Flex и ActionScript");
            caps.Categories.AddCategoryMapping(1562, TorznabCatType.BooksTechnical, "|- 2D-графика");
            caps.Categories.AddCategoryMapping(1563, TorznabCatType.BooksTechnical, "|- 3D-графика");
            caps.Categories.AddCategoryMapping(1626, TorznabCatType.BooksTechnical, "|- Инженерные и научные программы (видеоуроки)");
            caps.Categories.AddCategoryMapping(1564, TorznabCatType.BooksTechnical, "|- Web-дизайн");
            caps.Categories.AddCategoryMapping(1545, TorznabCatType.BooksTechnical, "|- WEB, SMM, SEO, интернет-маркетинг");
            caps.Categories.AddCategoryMapping(1565, TorznabCatType.BooksTechnical, "|- Программирование (видеоуроки)");
            caps.Categories.AddCategoryMapping(1559, TorznabCatType.BooksTechnical, "|- Программы для Mac OS");
            caps.Categories.AddCategoryMapping(1566, TorznabCatType.BooksTechnical, "|- Работа с видео");
            caps.Categories.AddCategoryMapping(1573, TorznabCatType.BooksTechnical, "|- Работа со звуком");
            caps.Categories.AddCategoryMapping(1567, TorznabCatType.BooksTechnical, "|- Разное (Компьютерные видеоуроки)");
            caps.Categories.AddCategoryMapping(2326, TorznabCatType.AudioAudiobook, "Радиоспектакли, история, мемуары");
            caps.Categories.AddCategoryMapping(574, TorznabCatType.AudioAudiobook, "|- [Аудио] Радиоспектакли и литературные чтения");
            caps.Categories.AddCategoryMapping(1036, TorznabCatType.AudioAudiobook, "|- [Аудио] Биографии и мемуары");
            caps.Categories.AddCategoryMapping(400, TorznabCatType.AudioAudiobook, "|- [Аудио] История, культурология, философия");
            caps.Categories.AddCategoryMapping(2389, TorznabCatType.AudioAudiobook, "Фантастика, фэнтези, мистика, ужасы, фанфики");
            caps.Categories.AddCategoryMapping(2388, TorznabCatType.AudioAudiobook, "|- [Аудио] Зарубежная фантастика, фэнтези, мистика, ужасы, фанфики");
            caps.Categories.AddCategoryMapping(2387, TorznabCatType.AudioAudiobook, "|- [Аудио] Российская фантастика, фэнтези, мистика, ужасы, фанфики");
            caps.Categories.AddCategoryMapping(661, TorznabCatType.AudioAudiobook, "|- [Аудио] Любовно-фантастический роман");
            caps.Categories.AddCategoryMapping(2348, TorznabCatType.AudioAudiobook, "|- [Аудио] Сборники/разное Фантастика, фэнтези, мистика, ужасы, фанфики");
            caps.Categories.AddCategoryMapping(2327, TorznabCatType.AudioAudiobook, "Художественная литература");
            caps.Categories.AddCategoryMapping(695, TorznabCatType.AudioAudiobook, "|- [Аудио] Поэзия");
            caps.Categories.AddCategoryMapping(399, TorznabCatType.AudioAudiobook, "|- [Аудио] Зарубежная литература");
            caps.Categories.AddCategoryMapping(402, TorznabCatType.AudioAudiobook, "|- [Аудио] Русская литература");
            caps.Categories.AddCategoryMapping(467, TorznabCatType.AudioAudiobook, "|- [Аудио] Современные любовные романы");
            caps.Categories.AddCategoryMapping(490, TorznabCatType.AudioAudiobook, "|- [Аудио] Детская литература");
            caps.Categories.AddCategoryMapping(499, TorznabCatType.AudioAudiobook, "|- [Аудио] Зарубежные детективы, приключения, триллеры, боевики");
            caps.Categories.AddCategoryMapping(2137, TorznabCatType.AudioAudiobook, "|- [Аудио] Российские детективы, приключения, триллеры, боевики");
            caps.Categories.AddCategoryMapping(2127, TorznabCatType.AudioAudiobook, "|- [Аудио] Азиатская подростковая литература, ранобэ, веб-новеллы");
            caps.Categories.AddCategoryMapping(2324, TorznabCatType.AudioAudiobook, "Религии");
            caps.Categories.AddCategoryMapping(2325, TorznabCatType.AudioAudiobook, "|- [Аудио] Православие");
            caps.Categories.AddCategoryMapping(2342, TorznabCatType.AudioAudiobook, "|- [Аудио] Ислам");
            caps.Categories.AddCategoryMapping(530, TorznabCatType.AudioAudiobook, "|- [Аудио] Другие традиционные религии");
            caps.Categories.AddCategoryMapping(2152, TorznabCatType.AudioAudiobook, "|- [Аудио] Нетрадиционные религиозно-философские учения");
            caps.Categories.AddCategoryMapping(2328, TorznabCatType.AudioAudiobook, "Прочая литература");
            caps.Categories.AddCategoryMapping(1350, TorznabCatType.AudioAudiobook, "|- [Аудио] Книги по медицине");
            caps.Categories.AddCategoryMapping(403, TorznabCatType.AudioAudiobook, "|- [Аудио] Учебная и научно-популярная литература");
            caps.Categories.AddCategoryMapping(1279, TorznabCatType.AudioAudiobook, "|- [Аудио] lossless-аудиокниги");
            caps.Categories.AddCategoryMapping(716, TorznabCatType.AudioAudiobook, "|- [Аудио] Бизнес");
            caps.Categories.AddCategoryMapping(2165, TorznabCatType.AudioAudiobook, "|- [Аудио] Разное");
            caps.Categories.AddCategoryMapping(401, TorznabCatType.AudioAudiobook, "|- [Аудио] Некондиционные раздачи");
            caps.Categories.AddCategoryMapping(1964, TorznabCatType.Books, "Ремонт и эксплуатация транспортных средств");
            caps.Categories.AddCategoryMapping(1973, TorznabCatType.Books, "|- Оригинальные каталоги по подбору запчастей");
            caps.Categories.AddCategoryMapping(1974, TorznabCatType.Books, "|- Неоригинальные каталоги по подбору запчастей");
            caps.Categories.AddCategoryMapping(1975, TorznabCatType.Books, "|- Программы по диагностике и ремонту");
            caps.Categories.AddCategoryMapping(1976, TorznabCatType.Books, "|- Тюнинг, чиптюнинг, настройка");
            caps.Categories.AddCategoryMapping(1977, TorznabCatType.Books, "|- Книги по ремонту/обслуживанию/эксплуатации ТС");
            caps.Categories.AddCategoryMapping(1203, TorznabCatType.Books, "|- Мультимедийки по ремонту/обслуживанию/эксплуатации ТС");
            caps.Categories.AddCategoryMapping(1978, TorznabCatType.Books, "|- Учет, утилиты и прочее");
            caps.Categories.AddCategoryMapping(1979, TorznabCatType.Books, "|- Виртуальная автошкола");
            caps.Categories.AddCategoryMapping(1980, TorznabCatType.Books, "|- Видеоуроки по вождению транспортных средств");
            caps.Categories.AddCategoryMapping(1981, TorznabCatType.Books, "|- Видеоуроки по ремонту транспортных средств");
            caps.Categories.AddCategoryMapping(1970, TorznabCatType.Books, "|- Журналы по авто/мото");
            caps.Categories.AddCategoryMapping(334, TorznabCatType.Books, "|- Водный транспорт");
            caps.Categories.AddCategoryMapping(1202, TorznabCatType.TVDocumentary, "Фильмы и передачи по авто/мото");
            caps.Categories.AddCategoryMapping(1985, TorznabCatType.TVDocumentary, "|- Документальные/познавательные фильмы");
            caps.Categories.AddCategoryMapping(1982, TorznabCatType.TVOther, "|- Развлекательные передачи");
            caps.Categories.AddCategoryMapping(2151, TorznabCatType.TVDocumentary, "|- Top Gear/Топ Гир");
            caps.Categories.AddCategoryMapping(1983, TorznabCatType.TVDocumentary, "|- Тест драйв/Обзоры/Автосалоны");
            caps.Categories.AddCategoryMapping(1984, TorznabCatType.TVDocumentary, "|- Тюнинг/форсаж");
            caps.Categories.AddCategoryMapping(409, TorznabCatType.Audio, "Классическая и современная академическая музыка");
            caps.Categories.AddCategoryMapping(560, TorznabCatType.AudioLossless, "|- Полные собрания сочинений и многодисковые издания (lossless)");
            caps.Categories.AddCategoryMapping(794, TorznabCatType.AudioLossless, "|- Опера (lossless)");
            caps.Categories.AddCategoryMapping(556, TorznabCatType.AudioLossless, "|- Вокальная музыка (lossless)");
            caps.Categories.AddCategoryMapping(2307, TorznabCatType.AudioLossless, "|- Хоровая музыка (lossless)");
            caps.Categories.AddCategoryMapping(557, TorznabCatType.AudioLossless, "|- Оркестровая музыка (lossless)");
            caps.Categories.AddCategoryMapping(2308, TorznabCatType.AudioLossless, "|- Концерт для инструмента с оркестром (lossless)");
            caps.Categories.AddCategoryMapping(558, TorznabCatType.AudioLossless, "|- Камерная инструментальная музыка (lossless)");
            caps.Categories.AddCategoryMapping(793, TorznabCatType.AudioLossless, "|- Сольная инструментальная музыка (lossless)");
            caps.Categories.AddCategoryMapping(1395, TorznabCatType.AudioLossless, "|- Духовные песнопения и музыка (lossless)");
            caps.Categories.AddCategoryMapping(1396, TorznabCatType.AudioMP3, "|- Духовные песнопения и музыка (lossy)");
            caps.Categories.AddCategoryMapping(436, TorznabCatType.AudioMP3, "|- Полные собрания сочинений и многодисковые издания (lossy)");
            caps.Categories.AddCategoryMapping(2309, TorznabCatType.AudioMP3, "|- Вокальная и хоровая музыка (lossy)");
            caps.Categories.AddCategoryMapping(2310, TorznabCatType.AudioMP3, "|- Оркестровая музыка (lossy)");
            caps.Categories.AddCategoryMapping(2311, TorznabCatType.AudioMP3, "|- Камерная и сольная инструментальная музыка (lossy)");
            caps.Categories.AddCategoryMapping(969, TorznabCatType.Audio, "|- Классика в современной обработке, Classical Crossover (lossy и lossless)");
            caps.Categories.AddCategoryMapping(1125, TorznabCatType.Audio, "Фольклор, Народная и Этническая музыка");
            caps.Categories.AddCategoryMapping(1130, TorznabCatType.AudioMP3, "|- Восточноевропейский фолк (lossy)");
            caps.Categories.AddCategoryMapping(1131, TorznabCatType.AudioLossless, "|- Восточноевропейский фолк (lossless)");
            caps.Categories.AddCategoryMapping(1132, TorznabCatType.AudioMP3, "|- Западноевропейский фолк (lossy)");
            caps.Categories.AddCategoryMapping(1133, TorznabCatType.AudioLossless, "|- Западноевропейский фолк (lossless)");
            caps.Categories.AddCategoryMapping(2084, TorznabCatType.Audio, "|- Klezmer и Еврейский фольклор (lossy и lossless)");
            caps.Categories.AddCategoryMapping(1128, TorznabCatType.AudioMP3, "|- Этническая музыка Сибири, Средней и Восточной Азии (lossy)");
            caps.Categories.AddCategoryMapping(1129, TorznabCatType.AudioLossless, "|- Этническая музыка Сибири, Средней и Восточной Азии (lossless)");
            caps.Categories.AddCategoryMapping(1856, TorznabCatType.AudioMP3, "|- Этническая музыка Индии (lossy)");
            caps.Categories.AddCategoryMapping(2430, TorznabCatType.AudioLossless, "|- Этническая музыка Индии (lossless)");
            caps.Categories.AddCategoryMapping(1283, TorznabCatType.AudioMP3, "|- Этническая музыка Африки и Ближнего Востока (lossy)");
            caps.Categories.AddCategoryMapping(2085, TorznabCatType.AudioLossless, "|- Этническая музыка Африки и Ближнего Востока (lossless)");
            caps.Categories.AddCategoryMapping(1282, TorznabCatType.Audio, "|- Фольклорная, Народная, Эстрадная музыка Кавказа и Закавказья (lossy и lossless)");
            caps.Categories.AddCategoryMapping(1284, TorznabCatType.AudioMP3, "|- Этническая музыка Северной и Южной Америки (lossy)");
            caps.Categories.AddCategoryMapping(1285, TorznabCatType.AudioLossless, "|- Этническая музыка Северной и Южной Америки (lossless)");
            caps.Categories.AddCategoryMapping(1138, TorznabCatType.Audio, "|- Этническая музыка Австралии, Тихого и Индийского океанов (lossy и lossless)");
            caps.Categories.AddCategoryMapping(1136, TorznabCatType.AudioMP3, "|- Country, Bluegrass (lossy)");
            caps.Categories.AddCategoryMapping(1137, TorznabCatType.AudioLossless, "|- Country, Bluegrass (lossless)");
            caps.Categories.AddCategoryMapping(1849, TorznabCatType.Audio, "New Age, Relax, Meditative & Flamenco");
            caps.Categories.AddCategoryMapping(1126, TorznabCatType.AudioMP3, "|- New Age & Meditative (lossy)");
            caps.Categories.AddCategoryMapping(1127, TorznabCatType.AudioLossless, "|- New Age & Meditative (lossless)");
            caps.Categories.AddCategoryMapping(1134, TorznabCatType.AudioMP3, "|- Фламенко и акустическая гитара (lossy)");
            caps.Categories.AddCategoryMapping(1135, TorznabCatType.AudioLossless, "|- Фламенко и акустическая гитара (lossless)");
            caps.Categories.AddCategoryMapping(2018, TorznabCatType.Audio, "|- Музыка для бальных танцев (lossy и lossless)");
            caps.Categories.AddCategoryMapping(855, TorznabCatType.Audio, "|- Звуки природы");
            caps.Categories.AddCategoryMapping(408, TorznabCatType.Audio, "Рэп, Хип-Хоп, R'n'B");
            caps.Categories.AddCategoryMapping(441, TorznabCatType.AudioMP3, "|- Отечественный Рэп, Хип-Хоп (lossy)");
            caps.Categories.AddCategoryMapping(1173, TorznabCatType.AudioMP3, "|- Отечественный R'n'B (lossy)");
            caps.Categories.AddCategoryMapping(1486, TorznabCatType.AudioLossless, "|- Отечественный Рэп, Хип-Хоп, R'n'B (lossless)");
            caps.Categories.AddCategoryMapping(1172, TorznabCatType.AudioMP3, "|- Зарубежный R'n'B (lossy)");
            caps.Categories.AddCategoryMapping(446, TorznabCatType.AudioMP3, "|- Зарубежный Рэп, Хип-Хоп (lossy)");
            caps.Categories.AddCategoryMapping(909, TorznabCatType.AudioLossless, "|- Зарубежный Рэп, Хип-Хоп (lossless)");
            caps.Categories.AddCategoryMapping(1665, TorznabCatType.AudioLossless, "|- Зарубежный R'n'B (lossless)");
            caps.Categories.AddCategoryMapping(1760, TorznabCatType.Audio, "Reggae, Ska, Dub");
            caps.Categories.AddCategoryMapping(1764, TorznabCatType.Audio, "|- Rocksteady, Early Reggae, Ska-Jazz, Trad.Ska (lossy и lossless)");
            caps.Categories.AddCategoryMapping(1767, TorznabCatType.AudioMP3, "|- 3rd Wave Ska (lossy)");
            caps.Categories.AddCategoryMapping(1769, TorznabCatType.AudioMP3, "|- Ska-Punk, Ska-Core (lossy)");
            caps.Categories.AddCategoryMapping(1765, TorznabCatType.AudioMP3, "|- Reggae (lossy)");
            caps.Categories.AddCategoryMapping(1771, TorznabCatType.AudioMP3, "|- Dub (lossy)");
            caps.Categories.AddCategoryMapping(1770, TorznabCatType.AudioMP3, "|- Dancehall, Raggamuffin (lossy)");
            caps.Categories.AddCategoryMapping(1768, TorznabCatType.AudioLossless, "|- Reggae, Dancehall, Dub (lossless)");
            caps.Categories.AddCategoryMapping(1774, TorznabCatType.AudioLossless, "|- Ska, Ska-Punk, Ska-Jazz (lossless)");
            caps.Categories.AddCategoryMapping(1772, TorznabCatType.Audio, "|- Отечественный Reggae, Dub (lossy и lossless)");
            caps.Categories.AddCategoryMapping(2233, TorznabCatType.Audio, "|- Reggae, Ska, Dub (компиляции) (lossy и lossless)");
            caps.Categories.AddCategoryMapping(416, TorznabCatType.Audio, "Саундтреки, караоке и мюзиклы");
            caps.Categories.AddCategoryMapping(2377, TorznabCatType.AudioVideo, "|- Караоке");
            caps.Categories.AddCategoryMapping(468, TorznabCatType.Audio, "|- Минусовки (lossy и lossless)");
            caps.Categories.AddCategoryMapping(691, TorznabCatType.AudioLossless, "|- Саундтреки к отечественным фильмам (lossless)");
            caps.Categories.AddCategoryMapping(469, TorznabCatType.AudioMP3, "|- Саундтреки к отечественным фильмам (lossy)");
            caps.Categories.AddCategoryMapping(786, TorznabCatType.AudioLossless, "|- Саундтреки к зарубежным фильмам (lossless)");
            caps.Categories.AddCategoryMapping(785, TorznabCatType.AudioMP3, "|- Саундтреки к зарубежным фильмам (lossy)");
            caps.Categories.AddCategoryMapping(1631, TorznabCatType.AudioLossless, "|- Саундтреки к сериалам (lossless)");
            caps.Categories.AddCategoryMapping(1499, TorznabCatType.AudioMP3, "|- Саундтреки к сериалам (lossy)");
            caps.Categories.AddCategoryMapping(715, TorznabCatType.Audio, "|- Саундтреки к мультфильмам (lossy и lossless)");
            caps.Categories.AddCategoryMapping(1388, TorznabCatType.AudioLossless, "|- Саундтреки к аниме (lossless)");
            caps.Categories.AddCategoryMapping(282, TorznabCatType.AudioMP3, "|- Саундтреки к аниме (lossy)");
            caps.Categories.AddCategoryMapping(796, TorznabCatType.AudioMP3, "|- Неофициальные саундтреки к фильмам и сериалам (lossy)");
            caps.Categories.AddCategoryMapping(784, TorznabCatType.AudioLossless, "|- Саундтреки к играм (lossless)");
            caps.Categories.AddCategoryMapping(783, TorznabCatType.AudioMP3, "|- Саундтреки к играм (lossy)");
            caps.Categories.AddCategoryMapping(2331, TorznabCatType.AudioMP3, "|- Неофициальные саундтреки к играм (lossy)");
            caps.Categories.AddCategoryMapping(2431, TorznabCatType.Audio, "|- Аранжировки музыки из игр (lossy и lossless)");
            caps.Categories.AddCategoryMapping(880, TorznabCatType.Audio, "|- Мюзикл (lossy и lossless)");
            caps.Categories.AddCategoryMapping(1215, TorznabCatType.Audio, "Шансон, Авторская и Военная песня");
            caps.Categories.AddCategoryMapping(1220, TorznabCatType.AudioLossless, "|- Отечественный шансон (lossless)");
            caps.Categories.AddCategoryMapping(1221, TorznabCatType.AudioMP3, "|- Отечественный шансон (lossy)");
            caps.Categories.AddCategoryMapping(1334, TorznabCatType.AudioMP3, "|- Сборники отечественного шансона (lossy)");
            caps.Categories.AddCategoryMapping(1216, TorznabCatType.AudioLossless, "|- Военная песня, марши (lossless)");
            caps.Categories.AddCategoryMapping(1223, TorznabCatType.AudioMP3, "|- Военная песня, марши (lossy)");
            caps.Categories.AddCategoryMapping(1224, TorznabCatType.AudioLossless, "|- Авторская песня (lossless)");
            caps.Categories.AddCategoryMapping(1225, TorznabCatType.AudioMP3, "|- Авторская песня (lossy)");
            caps.Categories.AddCategoryMapping(1226, TorznabCatType.Audio, "|- Менестрели и ролевики (lossy и lossless)");
            caps.Categories.AddCategoryMapping(1842, TorznabCatType.AudioLossless, "Label Packs (lossless)");
            caps.Categories.AddCategoryMapping(1648, TorznabCatType.AudioMP3, "Label packs, Scene packs (lossy)");
            caps.Categories.AddCategoryMapping(134, TorznabCatType.AudioLossless, "|- Неофициальные сборники и ремастеринги (lossless)");
            caps.Categories.AddCategoryMapping(965, TorznabCatType.AudioMP3, "|- Неофициальные сборники (lossy)");
            caps.Categories.AddCategoryMapping(2495, TorznabCatType.AudioMP3, "Отечественная поп-музыка ");
            caps.Categories.AddCategoryMapping(424, TorznabCatType.AudioMP3, "|- Популярная музыка России и стран бывшего СССР (lossy)");
            caps.Categories.AddCategoryMapping(1361, TorznabCatType.AudioMP3, "|- Популярная музыка России и стран бывшего СССР (сборники) (lossy)");
            caps.Categories.AddCategoryMapping(425, TorznabCatType.AudioLossless, "|- Популярная музыка России и стран бывшего СССР (lossless)");
            caps.Categories.AddCategoryMapping(1635, TorznabCatType.AudioMP3, "|- Советская эстрада, ретро, романсы (lossy)");
            caps.Categories.AddCategoryMapping(1634, TorznabCatType.AudioLossless, "|- Советская эстрада, ретро, романсы (lossless)");
            caps.Categories.AddCategoryMapping(2497, TorznabCatType.Audio, "Зарубежная поп-музыка");
            caps.Categories.AddCategoryMapping(428, TorznabCatType.AudioMP3, "|- Зарубежная поп-музыка (lossy)");
            caps.Categories.AddCategoryMapping(1362, TorznabCatType.AudioMP3, "|- Зарубежная поп-музыка (сборники) (lossy)");
            caps.Categories.AddCategoryMapping(429, TorznabCatType.AudioLossless, "|- Зарубежная поп-музыка (lossless)");
            caps.Categories.AddCategoryMapping(735, TorznabCatType.AudioMP3, "|- Итальянская поп-музыка (lossy)");
            caps.Categories.AddCategoryMapping(1753, TorznabCatType.AudioLossless, "|- Итальянская поп-музыка (lossless)");
            caps.Categories.AddCategoryMapping(2232, TorznabCatType.AudioMP3, "|- Латиноамериканская поп-музыка (lossy)");
            caps.Categories.AddCategoryMapping(714, TorznabCatType.AudioLossless, "|- Латиноамериканская поп-музыка (lossless)");
            caps.Categories.AddCategoryMapping(1331, TorznabCatType.AudioMP3, "|- Восточноазиатская поп-музыка (lossy)");
            caps.Categories.AddCategoryMapping(1330, TorznabCatType.AudioLossless, "|- Восточноазиатская поп-музыка (lossless)");
            caps.Categories.AddCategoryMapping(1219, TorznabCatType.AudioMP3, "|- Зарубежный шансон (lossy)");
            caps.Categories.AddCategoryMapping(1452, TorznabCatType.AudioLossless, "|- Зарубежный шансон (lossless)");
            caps.Categories.AddCategoryMapping(2275, TorznabCatType.AudioMP3, "|- Easy Listening, Instrumental Pop (lossy)");
            caps.Categories.AddCategoryMapping(2270, TorznabCatType.AudioLossless, "|- Easy Listening, Instrumental Pop (lossless)");
            caps.Categories.AddCategoryMapping(1351, TorznabCatType.Audio, "|- Сборники песен для детей (lossy и lossless)");
            caps.Categories.AddCategoryMapping(2499, TorznabCatType.Audio, "Eurodance, Disco, Hi-NRG");
            caps.Categories.AddCategoryMapping(2503, TorznabCatType.AudioMP3, "|- Eurodance, Euro-House, Technopop (lossy)");
            caps.Categories.AddCategoryMapping(2504, TorznabCatType.AudioMP3, "|- Eurodance, Euro-House, Technopop (сборники) (lossy)");
            caps.Categories.AddCategoryMapping(2502, TorznabCatType.AudioLossless, "|- Eurodance, Euro-House, Technopop (lossless)");
            caps.Categories.AddCategoryMapping(2501, TorznabCatType.AudioMP3, "|- Disco, Italo-Disco, Euro-Disco, Hi-NRG (lossy)");
            caps.Categories.AddCategoryMapping(2505, TorznabCatType.AudioMP3, "|- Disco, Italo-Disco, Euro-Disco, Hi-NRG (сборники) (lossy)");
            caps.Categories.AddCategoryMapping(2500, TorznabCatType.AudioLossless, "|- Disco, Italo-Disco, Euro-Disco, Hi-NRG (lossless)");
            caps.Categories.AddCategoryMapping(2267, TorznabCatType.Audio, "Зарубежный джаз");
            caps.Categories.AddCategoryMapping(2277, TorznabCatType.AudioLossless, "|- Early Jazz, Swing, Gypsy (lossless)");
            caps.Categories.AddCategoryMapping(2278, TorznabCatType.AudioLossless, "|- Bop (lossless)");
            caps.Categories.AddCategoryMapping(2279, TorznabCatType.AudioLossless, "|- Mainstream Jazz, Cool (lossless)");
            caps.Categories.AddCategoryMapping(2280, TorznabCatType.AudioLossless, "|- Jazz Fusion (lossless)");
            caps.Categories.AddCategoryMapping(2281, TorznabCatType.AudioLossless, "|- World Fusion, Ethnic Jazz (lossless)");
            caps.Categories.AddCategoryMapping(2282, TorznabCatType.AudioLossless, "|- Avant-Garde Jazz, Free Improvisation (lossless)");
            caps.Categories.AddCategoryMapping(2353, TorznabCatType.AudioLossless, "|- Modern Creative, Third Stream (lossless)");
            caps.Categories.AddCategoryMapping(2284, TorznabCatType.AudioLossless, "|- Smooth, Jazz-Pop (lossless)");
            caps.Categories.AddCategoryMapping(2285, TorznabCatType.AudioLossless, "|- Vocal Jazz (lossless)");
            caps.Categories.AddCategoryMapping(2283, TorznabCatType.AudioLossless, "|- Funk, Soul, R&B (lossless)");
            caps.Categories.AddCategoryMapping(2286, TorznabCatType.AudioLossless, "|- Сборники зарубежного джаза (lossless)");
            caps.Categories.AddCategoryMapping(2287, TorznabCatType.AudioMP3, "|- Зарубежный джаз (lossy)");
            caps.Categories.AddCategoryMapping(2268, TorznabCatType.Audio, "Зарубежный блюз");
            caps.Categories.AddCategoryMapping(2293, TorznabCatType.AudioLossless, "|- Blues (Texas, Chicago, Modern and Others) (lossless)");
            caps.Categories.AddCategoryMapping(2292, TorznabCatType.AudioLossless, "|- Blues-rock (lossless)");
            caps.Categories.AddCategoryMapping(2290, TorznabCatType.AudioLossless, "|- Roots, Pre-War Blues, Early R&B, Gospel (lossless)");
            caps.Categories.AddCategoryMapping(2289, TorznabCatType.AudioLossless, "|- Зарубежный блюз (сборники; Tribute VA) (lossless)");
            caps.Categories.AddCategoryMapping(2288, TorznabCatType.AudioMP3, "|- Зарубежный блюз (lossy)");
            caps.Categories.AddCategoryMapping(2269, TorznabCatType.Audio, "Отечественный джаз и блюз");
            caps.Categories.AddCategoryMapping(2297, TorznabCatType.AudioLossless, "|- Отечественный джаз (lossless)");
            caps.Categories.AddCategoryMapping(2295, TorznabCatType.AudioMP3, "|- Отечественный джаз (lossy)");
            caps.Categories.AddCategoryMapping(2296, TorznabCatType.AudioLossless, "|- Отечественный блюз (lossless)");
            caps.Categories.AddCategoryMapping(2298, TorznabCatType.AudioMP3, "|- Отечественный блюз (lossy)");
            caps.Categories.AddCategoryMapping(1698, TorznabCatType.Audio, "Зарубежный Rock");
            caps.Categories.AddCategoryMapping(1702, TorznabCatType.AudioLossless, "|- Classic Rock & Hard Rock (lossless)");
            caps.Categories.AddCategoryMapping(1703, TorznabCatType.AudioMP3, "|- Classic Rock & Hard Rock (lossy)");
            caps.Categories.AddCategoryMapping(1704, TorznabCatType.AudioLossless, "|- Progressive & Art-Rock (lossless)");
            caps.Categories.AddCategoryMapping(1705, TorznabCatType.AudioMP3, "|- Progressive & Art-Rock (lossy)");
            caps.Categories.AddCategoryMapping(1706, TorznabCatType.AudioLossless, "|- Folk-Rock (lossless)");
            caps.Categories.AddCategoryMapping(1707, TorznabCatType.AudioMP3, "|- Folk-Rock (lossy)");
            caps.Categories.AddCategoryMapping(2329, TorznabCatType.AudioLossless, "|- AOR (Melodic Hard Rock, Arena rock) (lossless)");
            caps.Categories.AddCategoryMapping(2330, TorznabCatType.AudioMP3, "|- AOR (Melodic Hard Rock, Arena rock) (lossy)");
            caps.Categories.AddCategoryMapping(1708, TorznabCatType.AudioLossless, "|- Pop-Rock & Soft Rock (lossless)");
            caps.Categories.AddCategoryMapping(1709, TorznabCatType.AudioMP3, "|- Pop-Rock & Soft Rock (lossy)");
            caps.Categories.AddCategoryMapping(1710, TorznabCatType.AudioLossless, "|- Instrumental Guitar Rock (lossless)");
            caps.Categories.AddCategoryMapping(1711, TorznabCatType.AudioMP3, "|- Instrumental Guitar Rock (lossy)");
            caps.Categories.AddCategoryMapping(1712, TorznabCatType.AudioLossless, "|- Rockabilly, Psychobilly, Rock'n'Roll (lossless)");
            caps.Categories.AddCategoryMapping(1713, TorznabCatType.AudioMP3, "|- Rockabilly, Psychobilly, Rock'n'Roll (lossy)");
            caps.Categories.AddCategoryMapping(731, TorznabCatType.AudioLossless, "|- Сборники зарубежного рока (lossless)");
            caps.Categories.AddCategoryMapping(1799, TorznabCatType.AudioMP3, "|- Сборники зарубежного рока (lossy)");
            caps.Categories.AddCategoryMapping(1714, TorznabCatType.AudioLossless, "|- Восточноазиатский рок (lossless)");
            caps.Categories.AddCategoryMapping(1715, TorznabCatType.AudioMP3, "|- Восточноазиатский рок (lossy)");
            caps.Categories.AddCategoryMapping(1716, TorznabCatType.Audio, "Зарубежный Metal");
            caps.Categories.AddCategoryMapping(1796, TorznabCatType.AudioLossless, "|- Avant-garde, Experimental Metal (lossless)");
            caps.Categories.AddCategoryMapping(1797, TorznabCatType.AudioMP3, "|- Avant-garde, Experimental Metal (lossy)");
            caps.Categories.AddCategoryMapping(1719, TorznabCatType.AudioLossless, "|- Black (lossless)");
            caps.Categories.AddCategoryMapping(1778, TorznabCatType.AudioMP3, "|- Black (lossy)");
            caps.Categories.AddCategoryMapping(1779, TorznabCatType.AudioLossless, "|- Death, Doom (lossless)");
            caps.Categories.AddCategoryMapping(1780, TorznabCatType.AudioMP3, "|- Death, Doom (lossy)");
            caps.Categories.AddCategoryMapping(1720, TorznabCatType.AudioLossless, "|- Folk, Pagan, Viking (lossless)");
            caps.Categories.AddCategoryMapping(798, TorznabCatType.AudioMP3, "|- Folk, Pagan, Viking (lossy)");
            caps.Categories.AddCategoryMapping(1724, TorznabCatType.AudioLossless, "|- Gothic Metal (lossless)");
            caps.Categories.AddCategoryMapping(1725, TorznabCatType.AudioMP3, "|- Gothic Metal (lossy)");
            caps.Categories.AddCategoryMapping(1730, TorznabCatType.AudioLossless, "|- Grind, Brutal Death (lossless)");
            caps.Categories.AddCategoryMapping(1731, TorznabCatType.AudioMP3, "|- Grind, Brutal Death (lossy)");
            caps.Categories.AddCategoryMapping(1726, TorznabCatType.AudioLossless, "|- Heavy, Power, Progressive (lossless)");
            caps.Categories.AddCategoryMapping(1727, TorznabCatType.AudioMP3, "|- Heavy, Power, Progressive (lossy)");
            caps.Categories.AddCategoryMapping(1815, TorznabCatType.AudioLossless, "|- Sludge, Stoner, Post-Metal (lossless)");
            caps.Categories.AddCategoryMapping(1816, TorznabCatType.AudioMP3, "|- Sludge, Stoner, Post-Metal (lossy)");
            caps.Categories.AddCategoryMapping(1728, TorznabCatType.AudioLossless, "|- Thrash, Speed (lossless)");
            caps.Categories.AddCategoryMapping(1729, TorznabCatType.AudioMP3, "|- Thrash, Speed (lossy)");
            caps.Categories.AddCategoryMapping(2230, TorznabCatType.AudioLossless, "|- Сборники (lossless)");
            caps.Categories.AddCategoryMapping(2231, TorznabCatType.AudioMP3, "|- Сборники (lossy)");
            caps.Categories.AddCategoryMapping(1732, TorznabCatType.Audio, "Зарубежные Alternative, Punk, Independent");
            caps.Categories.AddCategoryMapping(1736, TorznabCatType.AudioLossless, "|- Alternative & Nu-metal (lossless)");
            caps.Categories.AddCategoryMapping(1737, TorznabCatType.AudioMP3, "|- Alternative & Nu-metal (lossy)");
            caps.Categories.AddCategoryMapping(1738, TorznabCatType.AudioLossless, "|- Punk (lossless)");
            caps.Categories.AddCategoryMapping(1739, TorznabCatType.AudioMP3, "|- Punk (lossy)");
            caps.Categories.AddCategoryMapping(1740, TorznabCatType.AudioLossless, "|- Hardcore (lossless)");
            caps.Categories.AddCategoryMapping(1741, TorznabCatType.AudioMP3, "|- Hardcore (lossy)");
            caps.Categories.AddCategoryMapping(1773, TorznabCatType.AudioLossless, "|- Indie Rock, Indie Pop, Dream Pop, Brit-Pop (lossless)");
            caps.Categories.AddCategoryMapping(202, TorznabCatType.AudioMP3, "|- Indie Rock, Indie Pop, Dream Pop, Brit-Pop (lossy)");
            caps.Categories.AddCategoryMapping(172, TorznabCatType.AudioLossless, "|- Post-Punk, Shoegaze, Garage Rock, Noise Rock (lossless)");
            caps.Categories.AddCategoryMapping(236, TorznabCatType.AudioMP3, "|- Post-Punk, Shoegaze, Garage Rock, Noise Rock (lossy)");
            caps.Categories.AddCategoryMapping(1742, TorznabCatType.AudioLossless, "|- Post-Rock (lossless)");
            caps.Categories.AddCategoryMapping(1743, TorznabCatType.AudioMP3, "|- Post-Rock (lossy)");
            caps.Categories.AddCategoryMapping(1744, TorznabCatType.AudioLossless, "|- Industrial & Post-industrial (lossless)");
            caps.Categories.AddCategoryMapping(1745, TorznabCatType.AudioMP3, "|- Industrial & Post-industrial (lossy)");
            caps.Categories.AddCategoryMapping(1746, TorznabCatType.AudioLossless, "|- Emocore, Post-hardcore, Metalcore (lossless)");
            caps.Categories.AddCategoryMapping(1747, TorznabCatType.AudioMP3, "|- Emocore, Post-hardcore, Metalcore (lossy)");
            caps.Categories.AddCategoryMapping(1748, TorznabCatType.AudioLossless, "|- Gothic Rock & Dark Folk (lossless)");
            caps.Categories.AddCategoryMapping(1749, TorznabCatType.AudioMP3, "|- Gothic Rock & Dark Folk (lossy)");
            caps.Categories.AddCategoryMapping(2175, TorznabCatType.AudioLossless, "|- Avant-garde, Experimental Rock (lossless)");
            caps.Categories.AddCategoryMapping(2174, TorznabCatType.AudioMP3, "|- Avant-garde, Experimental Rock (lossy)");
            caps.Categories.AddCategoryMapping(722, TorznabCatType.Audio, "Отечественный Rock, Metal");
            caps.Categories.AddCategoryMapping(737, TorznabCatType.AudioLossless, "|- Rock (lossless)");
            caps.Categories.AddCategoryMapping(738, TorznabCatType.AudioMP3, "|- Rock (lossy)");
            caps.Categories.AddCategoryMapping(464, TorznabCatType.AudioLossless, "|- Alternative, Punk, Independent (lossless)");
            caps.Categories.AddCategoryMapping(463, TorznabCatType.AudioMP3, "|- Alternative, Punk, Independent (lossy)");
            caps.Categories.AddCategoryMapping(739, TorznabCatType.AudioLossless, "|- Metal (lossless)");
            caps.Categories.AddCategoryMapping(740, TorznabCatType.AudioMP3, "|- Metal (lossy)");
            caps.Categories.AddCategoryMapping(951, TorznabCatType.AudioLossless, "|- Rock на языках народов xUSSR (lossless)");
            caps.Categories.AddCategoryMapping(952, TorznabCatType.AudioMP3, "|- Rock на языках народов xUSSR (lossy)");
            caps.Categories.AddCategoryMapping(1821, TorznabCatType.Audio, "Trance, Goa Trance, Psy-Trance, PsyChill, Ambient, Dub");
            caps.Categories.AddCategoryMapping(1844, TorznabCatType.AudioLossless, "|- Goa Trance, Psy-Trance (lossless)");
            caps.Categories.AddCategoryMapping(1822, TorznabCatType.AudioMP3, "|- Goa Trance, Psy-Trance (lossy)");
            caps.Categories.AddCategoryMapping(1894, TorznabCatType.AudioLossless, "|- PsyChill, Ambient, Dub (lossless)");
            caps.Categories.AddCategoryMapping(1895, TorznabCatType.AudioMP3, "|- PsyChill, Ambient, Dub (lossy)");
            caps.Categories.AddCategoryMapping(460, TorznabCatType.AudioMP3, "|- Goa Trance, Psy-Trance, PsyChill, Ambient, Dub (Live Sets, Mixes) (lossy)");
            caps.Categories.AddCategoryMapping(1818, TorznabCatType.AudioLossless, "|- Trance (lossless)");
            caps.Categories.AddCategoryMapping(1819, TorznabCatType.AudioMP3, "|- Trance (lossy)");
            caps.Categories.AddCategoryMapping(1847, TorznabCatType.AudioMP3, "|- Trance (Singles, EPs) (lossy)");
            caps.Categories.AddCategoryMapping(1824, TorznabCatType.AudioMP3, "|- Trance (Radioshows, Podcasts, Live Sets, Mixes) (lossy)");
            caps.Categories.AddCategoryMapping(1807, TorznabCatType.Audio, "House, Techno, Hardcore, Hardstyle, Jumpstyle");
            caps.Categories.AddCategoryMapping(1829, TorznabCatType.AudioLossless, "|- Hardcore, Hardstyle, Jumpstyle (lossless)");
            caps.Categories.AddCategoryMapping(1830, TorznabCatType.AudioMP3, "|- Hardcore, Hardstyle, Jumpstyle (lossy)");
            caps.Categories.AddCategoryMapping(1831, TorznabCatType.AudioMP3, "|- Hardcore, Hardstyle, Jumpstyle (vinyl, web)");
            caps.Categories.AddCategoryMapping(1857, TorznabCatType.AudioLossless, "|- House (lossless)");
            caps.Categories.AddCategoryMapping(1859, TorznabCatType.AudioMP3, "|- House (Radioshow, Podcast, Liveset, Mixes)");
            caps.Categories.AddCategoryMapping(1858, TorznabCatType.AudioMP3, "|- House (lossy)");
            caps.Categories.AddCategoryMapping(840, TorznabCatType.AudioMP3, "|- House (Проморелизы, сборники) (lossy)");
            caps.Categories.AddCategoryMapping(1860, TorznabCatType.AudioMP3, "|- House (Singles, EPs) (lossy)");
            caps.Categories.AddCategoryMapping(1825, TorznabCatType.AudioLossless, "|- Techno (lossless)");
            caps.Categories.AddCategoryMapping(1826, TorznabCatType.AudioMP3, "|- Techno (lossy)");
            caps.Categories.AddCategoryMapping(1827, TorznabCatType.AudioMP3, "|- Techno (Radioshows, Podcasts, Livesets, Mixes)");
            caps.Categories.AddCategoryMapping(1828, TorznabCatType.AudioMP3, "|- Techno (Singles, EPs) (lossy)");
            caps.Categories.AddCategoryMapping(1808, TorznabCatType.Audio, "Drum & Bass, Jungle, Breakbeat, Dubstep, IDM, Electro");
            caps.Categories.AddCategoryMapping(797, TorznabCatType.AudioLossless, "|- Electro, Electro-Freestyle, Nu Electro (lossless)");
            caps.Categories.AddCategoryMapping(1805, TorznabCatType.AudioMP3, "|- Electro, Electro-Freestyle, Nu Electro (lossy)");
            caps.Categories.AddCategoryMapping(1832, TorznabCatType.AudioLossless, "|- Drum & Bass, Jungle (lossless)");
            caps.Categories.AddCategoryMapping(1833, TorznabCatType.AudioMP3, "|- Drum & Bass, Jungle (lossy)");
            caps.Categories.AddCategoryMapping(1834, TorznabCatType.AudioMP3, "|- Drum & Bass, Jungle (Radioshows, Podcasts, Livesets, Mixes)");
            caps.Categories.AddCategoryMapping(1836, TorznabCatType.AudioLossless, "|- Breakbeat (lossless)");
            caps.Categories.AddCategoryMapping(1837, TorznabCatType.AudioMP3, "|- Breakbeat (lossy)");
            caps.Categories.AddCategoryMapping(1839, TorznabCatType.AudioLossless, "|- Dubstep (lossless)");
            caps.Categories.AddCategoryMapping(454, TorznabCatType.AudioMP3, "|- Dubstep (lossy)");
            caps.Categories.AddCategoryMapping(1838, TorznabCatType.AudioMP3, "|- Breakbeat, Dubstep (Radioshows, Podcasts, Livesets, Mixes)");
            caps.Categories.AddCategoryMapping(1840, TorznabCatType.AudioLossless, "|- IDM (lossless)");
            caps.Categories.AddCategoryMapping(1841, TorznabCatType.AudioMP3, "|- IDM (lossy)");
            caps.Categories.AddCategoryMapping(2229, TorznabCatType.AudioMP3, "|- IDM Discography & Collections (lossy)");
            caps.Categories.AddCategoryMapping(1809, TorznabCatType.Audio, "Chillout, Lounge, Downtempo, Trip-Hop");
            caps.Categories.AddCategoryMapping(1861, TorznabCatType.AudioLossless, "|- Chillout, Lounge, Downtempo (lossless)");
            caps.Categories.AddCategoryMapping(1862, TorznabCatType.AudioMP3, "|- Chillout, Lounge, Downtempo (lossy)");
            caps.Categories.AddCategoryMapping(1947, TorznabCatType.AudioLossless, "|- Nu Jazz, Acid Jazz, Future Jazz (lossless)");
            caps.Categories.AddCategoryMapping(1946, TorznabCatType.AudioMP3, "|- Nu Jazz, Acid Jazz, Future Jazz (lossy)");
            caps.Categories.AddCategoryMapping(1945, TorznabCatType.AudioLossless, "|- Trip Hop, Abstract Hip-Hop (lossless)");
            caps.Categories.AddCategoryMapping(1944, TorznabCatType.AudioMP3, "|- Trip Hop, Abstract Hip-Hop (lossy)");
            caps.Categories.AddCategoryMapping(1810, TorznabCatType.Audio, "Traditional Electronic, Ambient, Modern Classical, Electroacoustic, Experimental");
            caps.Categories.AddCategoryMapping(1864, TorznabCatType.AudioLossless, "|- Traditional Electronic, Ambient (lossless)");
            caps.Categories.AddCategoryMapping(1865, TorznabCatType.AudioMP3, "|- Traditional Electronic, Ambient (lossy)");
            caps.Categories.AddCategoryMapping(1871, TorznabCatType.AudioLossless, "|- Modern Classical, Electroacoustic (lossless)");
            caps.Categories.AddCategoryMapping(1867, TorznabCatType.AudioMP3, "|- Modern Classical, Electroacoustic (lossy)");
            caps.Categories.AddCategoryMapping(1869, TorznabCatType.AudioLossless, "|- Experimental (lossless)");
            caps.Categories.AddCategoryMapping(1873, TorznabCatType.AudioMP3, "|- Experimental (lossy)");
            caps.Categories.AddCategoryMapping(1811, TorznabCatType.Audio, "Industrial, Noise, EBM, Dark Electro, Aggrotech, Cyberpunk, Synthpop, New Wave");
            caps.Categories.AddCategoryMapping(1868, TorznabCatType.AudioLossless, "|- EBM, Dark Electro, Aggrotech (lossless)");
            caps.Categories.AddCategoryMapping(1875, TorznabCatType.AudioMP3, "|- EBM, Dark Electro, Aggrotech (lossy)");
            caps.Categories.AddCategoryMapping(1877, TorznabCatType.AudioLossless, "|- Industrial, Noise (lossless)");
            caps.Categories.AddCategoryMapping(1878, TorznabCatType.AudioMP3, "|- Industrial, Noise (lossy)");
            caps.Categories.AddCategoryMapping(1907, TorznabCatType.Audio, "|- Cyberpunk, 8-bit, Chiptune (lossy & lossless)");
            caps.Categories.AddCategoryMapping(1880, TorznabCatType.AudioLossless, "|- Synthpop, Futurepop, New Wave, Electropop (lossless)");
            caps.Categories.AddCategoryMapping(1881, TorznabCatType.AudioMP3, "|- Synthpop, Futurepop, New Wave, Electropop (lossy)");
            caps.Categories.AddCategoryMapping(466, TorznabCatType.AudioLossless, "|- Synthwave, Spacesynth, Dreamwave, Retrowave, Outrun (lossless)");
            caps.Categories.AddCategoryMapping(465, TorznabCatType.AudioMP3, "|- Synthwave, Spacesynth, Dreamwave, Retrowave, Outrun (lossy)");
            caps.Categories.AddCategoryMapping(1866, TorznabCatType.AudioLossless, "|- Darkwave, Neoclassical, Ethereal, Dungeon Synth (lossless)");
            caps.Categories.AddCategoryMapping(406, TorznabCatType.AudioMP3, "|- Darkwave, Neoclassical, Ethereal, Dungeon Synth (lossy)");
            caps.Categories.AddCategoryMapping(1299, TorznabCatType.Audio, "Hi-Res stereo и многоканальная музыка");
            caps.Categories.AddCategoryMapping(1884, TorznabCatType.Audio, "|- Классика и классика в современной обработке (Hi-Res stereo)");
            caps.Categories.AddCategoryMapping(1164, TorznabCatType.Audio, "|- Классика и классика в современной обработке (многоканальная музыка)");
            caps.Categories.AddCategoryMapping(2513, TorznabCatType.Audio, "|- New Age, Relax, Meditative & Flamenco (Hi-Res stereo и многоканальная музыка)");
            caps.Categories.AddCategoryMapping(1397, TorznabCatType.Audio, "|- Саундтреки (Hi-Res stereo и многоканальная музыка)");
            caps.Categories.AddCategoryMapping(2512, TorznabCatType.Audio, "|- Музыка разных жанров (Hi-Res stereo и многоканальная музыка)");
            caps.Categories.AddCategoryMapping(1885, TorznabCatType.Audio, "|- Поп-музыка (Hi-Res stereo)");
            caps.Categories.AddCategoryMapping(1163, TorznabCatType.Audio, "|- Поп-музыка (многоканальная музыка)");
            caps.Categories.AddCategoryMapping(2302, TorznabCatType.Audio, "|- Джаз и Блюз (Hi-Res stereo)");
            caps.Categories.AddCategoryMapping(2303, TorznabCatType.Audio, "|- Джаз и Блюз (многоканальная музыка)");
            caps.Categories.AddCategoryMapping(1755, TorznabCatType.Audio, "|- Рок-музыка (Hi-Res stereo)");
            caps.Categories.AddCategoryMapping(1757, TorznabCatType.Audio, "|- Рок-музыка (многоканальная музыка)");
            caps.Categories.AddCategoryMapping(1893, TorznabCatType.Audio, "|- Электронная музыка (Hi-Res stereo)");
            caps.Categories.AddCategoryMapping(1890, TorznabCatType.Audio, "|- Электронная музыка (многоканальная музыка)");
            caps.Categories.AddCategoryMapping(2219, TorznabCatType.Audio, "Оцифровки с аналоговых носителей");
            caps.Categories.AddCategoryMapping(1660, TorznabCatType.Audio, "|- Классика и классика в современной обработке (оцифровки)");
            caps.Categories.AddCategoryMapping(506, TorznabCatType.Audio, "|- Фольклор, народная и этническая музыка (оцифровки)");
            caps.Categories.AddCategoryMapping(1835, TorznabCatType.Audio, "|- Rap, Hip-Hop, R'n'B, Reggae, Ska, Dub (оцифровки)");
            caps.Categories.AddCategoryMapping(1625, TorznabCatType.Audio, "|- Саундтреки и мюзиклы (оцифровки)");
            caps.Categories.AddCategoryMapping(1217, TorznabCatType.Audio, "|- Шансон, авторские, военные песни и марши (оцифровки)");
            caps.Categories.AddCategoryMapping(974, TorznabCatType.Audio, "|- Музыка других жанров (оцифровки)");
            caps.Categories.AddCategoryMapping(1444, TorznabCatType.Audio, "|- Зарубежная поп-музыка (оцифровки)");
            caps.Categories.AddCategoryMapping(2401, TorznabCatType.Audio, "|- Советская эстрада, ретро, романсы (оцифровки)");
            caps.Categories.AddCategoryMapping(239, TorznabCatType.Audio, "|- Отечественная поп-музыка (оцифровки)");
            caps.Categories.AddCategoryMapping(450, TorznabCatType.Audio, "|- Инструментальная поп-музыка (оцифровки)");
            caps.Categories.AddCategoryMapping(2301, TorznabCatType.Audio, "|- Джаз и блюз (оцифровки)");
            caps.Categories.AddCategoryMapping(123, TorznabCatType.Audio, "|- Alternative, Punk, Independent (оцифровки)");
            caps.Categories.AddCategoryMapping(1756, TorznabCatType.Audio, "|- Зарубежная рок-музыка (оцифровки)");
            caps.Categories.AddCategoryMapping(1758, TorznabCatType.Audio, "|- Отечественная рок-музыка (оцифровки)");
            caps.Categories.AddCategoryMapping(1766, TorznabCatType.Audio, "|- Зарубежный и Отечественный Metal (оцифровки)");
            caps.Categories.AddCategoryMapping(1754, TorznabCatType.Audio, "|- Электронная музыка (оцифровки)");
            caps.Categories.AddCategoryMapping(860, TorznabCatType.Audio, "Неофициальные конверсии цифровых форматов");
            caps.Categories.AddCategoryMapping(453, TorznabCatType.Audio, "|- Конверсии Quadraphonic");
            caps.Categories.AddCategoryMapping(1170, TorznabCatType.Audio, "|- Конверсии SACD");
            caps.Categories.AddCategoryMapping(1759, TorznabCatType.Audio, "|- Конверсии Blu-Ray, ADVD и DVD-Audio");
            caps.Categories.AddCategoryMapping(1852, TorznabCatType.Audio, "|- Апмиксы-Upmixes/Даунмиксы-Downmix");
            caps.Categories.AddCategoryMapping(413, TorznabCatType.AudioVideo, "Музыкальное SD видео");
            caps.Categories.AddCategoryMapping(445, TorznabCatType.AudioVideo, "|- Классическая и современная академическая музыка (Видео)");
            caps.Categories.AddCategoryMapping(702, TorznabCatType.AudioVideo, "|- Опера, Оперетта и Мюзикл (Видео)");
            caps.Categories.AddCategoryMapping(1990, TorznabCatType.AudioVideo, "|- Балет и современная хореография (Видео)");
            caps.Categories.AddCategoryMapping(1793, TorznabCatType.AudioVideo, "|- Классика в современной обработке, Classical Crossover (Видео)");
            caps.Categories.AddCategoryMapping(1141, TorznabCatType.AudioVideo, "|- Фольклор, Народная и Этническая музыка и фламенко (Видео)");
            caps.Categories.AddCategoryMapping(1775, TorznabCatType.AudioVideo, "|- New Age, Relax, Meditative, Рэп, Хип-Хоп, R'n'B, Reggae, Ska, Dub (Видео)");
            caps.Categories.AddCategoryMapping(1227, TorznabCatType.AudioVideo, "|- Зарубежный и Отечественный Шансон, Авторская и Военная песня (Видео)");
            caps.Categories.AddCategoryMapping(475, TorznabCatType.AudioVideo, "|- Музыка других жанров, Советская эстрада, ретро, романсы (Видео)");
            caps.Categories.AddCategoryMapping(1121, TorznabCatType.AudioVideo, "|- Отечественная поп-музыка (Видео)");
            caps.Categories.AddCategoryMapping(431, TorznabCatType.AudioVideo, "|- Зарубежная Поп-музыка, Eurodance, Disco (Видео)");
            caps.Categories.AddCategoryMapping(2378, TorznabCatType.AudioVideo, "|- Восточноазиатская поп-музыка (Видео)");
            caps.Categories.AddCategoryMapping(2383, TorznabCatType.AudioVideo, "|- Разножанровые сборные концерты и сборники видеоклипов (Видео)");
            caps.Categories.AddCategoryMapping(2305, TorznabCatType.AudioVideo, "|- Джаз и Блюз (Видео)");
            caps.Categories.AddCategoryMapping(1782, TorznabCatType.AudioVideo, "|- Rock (Видео)");
            caps.Categories.AddCategoryMapping(1787, TorznabCatType.AudioVideo, "|- Metal (Видео)");
            caps.Categories.AddCategoryMapping(1789, TorznabCatType.AudioVideo, "|- Зарубежный Alternative, Punk, Independent (Видео)");
            caps.Categories.AddCategoryMapping(1791, TorznabCatType.AudioVideo, "|- Отечественный Рок, Панк, Альтернатива (Видео)");
            caps.Categories.AddCategoryMapping(1912, TorznabCatType.AudioVideo, "|- Электронная музыка (Видео)");
            caps.Categories.AddCategoryMapping(1189, TorznabCatType.AudioVideo, "|- Документальные фильмы о музыке и музыкантах (Видео)");
            caps.Categories.AddCategoryMapping(2403, TorznabCatType.AudioVideo, "Музыкальное DVD видео");
            caps.Categories.AddCategoryMapping(984, TorznabCatType.AudioVideo, "|- Классическая и современная академическая музыка (DVD Видео)");
            caps.Categories.AddCategoryMapping(983, TorznabCatType.AudioVideo, "|- Опера, Оперетта и Мюзикл (DVD Видео)");
            caps.Categories.AddCategoryMapping(2352, TorznabCatType.AudioVideo, "|- Балет и современная хореография (DVD Видео)");
            caps.Categories.AddCategoryMapping(2384, TorznabCatType.AudioVideo, "|- Классика в современной обработке, Classical Crossover (DVD Видео)");
            caps.Categories.AddCategoryMapping(1142, TorznabCatType.AudioVideo, "|- Фольклор, Народная и Этническая музыка и Flamenco (DVD Видео)");
            caps.Categories.AddCategoryMapping(1107, TorznabCatType.AudioVideo, "|- New Age, Relax, Meditative, Рэп, Хип-Хоп, R'n'B, Reggae, Ska, Dub (DVD Видео)");
            caps.Categories.AddCategoryMapping(1228, TorznabCatType.AudioVideo, "|- Зарубежный и Отечественный Шансон, Авторская и Военная песня (DVD Видео)");
            caps.Categories.AddCategoryMapping(988, TorznabCatType.AudioVideo, "|- Музыка других жанров, Советская эстрада, ретро, романсы (DVD Видео)");
            caps.Categories.AddCategoryMapping(1122, TorznabCatType.AudioVideo, "|- Отечественная поп-музыка (DVD Видео)");
            caps.Categories.AddCategoryMapping(986, TorznabCatType.AudioVideo, "|- Зарубежная Поп-музыка, Eurodance, Disco (DVD Видео)");
            caps.Categories.AddCategoryMapping(2379, TorznabCatType.AudioVideo, "|- Восточноазиатская поп-музыка (DVD Видео)");
            caps.Categories.AddCategoryMapping(2088, TorznabCatType.AudioVideo, "|- Разножанровые сборные концерты и сборники видеоклипов (DVD Видео)");
            caps.Categories.AddCategoryMapping(2304, TorznabCatType.AudioVideo, "|- Джаз и Блюз (DVD Видео)");
            caps.Categories.AddCategoryMapping(1783, TorznabCatType.AudioVideo, "|- Зарубежный Rock (DVD Видео)");
            caps.Categories.AddCategoryMapping(1788, TorznabCatType.AudioVideo, "|- Зарубежный Metal (DVD Видео)");
            caps.Categories.AddCategoryMapping(1790, TorznabCatType.AudioVideo, "|- Зарубежный Alternative, Punk, Independent (DVD Видео)");
            caps.Categories.AddCategoryMapping(1792, TorznabCatType.AudioVideo, "|- Отечественный Рок, Метал, Панк, Альтернатива (DVD Видео)");
            caps.Categories.AddCategoryMapping(1886, TorznabCatType.AudioVideo, "|- Электронная музыка (DVD Видео)");
            caps.Categories.AddCategoryMapping(2509, TorznabCatType.AudioVideo, "|- Документальные фильмы о музыке и музыкантах (DVD Видео)");
            caps.Categories.AddCategoryMapping(2507, TorznabCatType.AudioVideo, "Неофициальные DVD видео");
            caps.Categories.AddCategoryMapping(2263, TorznabCatType.AudioVideo, "|- Классическая музыка, Опера, Балет, Мюзикл (Неофициальные DVD Видео)");
            caps.Categories.AddCategoryMapping(2511, TorznabCatType.AudioVideo, "|- Шансон, Авторская песня, Сборные концерты, МДЖ (Неофициальные DVD Видео)");
            caps.Categories.AddCategoryMapping(2264, TorznabCatType.AudioVideo, "|- Зарубежная и Отечественная Поп-музыка (Неофициальные DVD Видео)");
            caps.Categories.AddCategoryMapping(2262, TorznabCatType.AudioVideo, "|- Джаз и Блюз (Неофициальные DVD Видео)");
            caps.Categories.AddCategoryMapping(2261, TorznabCatType.AudioVideo, "|- Зарубежная и Отечественная Рок-музыка (Неофициальные DVD Видео)");
            caps.Categories.AddCategoryMapping(1887, TorznabCatType.AudioVideo, "|- Электронная музыка (Неофициальные DVD Видео)");
            caps.Categories.AddCategoryMapping(2531, TorznabCatType.AudioVideo, "|- Прочие жанры (Неофициальные DVD Видео)");
            caps.Categories.AddCategoryMapping(2400, TorznabCatType.AudioVideo, "Музыкальное HD видео");
            caps.Categories.AddCategoryMapping(1812, TorznabCatType.AudioVideo, "|- Классическая и современная академическая музыка (HD Видео)");
            caps.Categories.AddCategoryMapping(655, TorznabCatType.AudioVideo, "|- Опера, Оперетта и Мюзикл (HD Видео)");
            caps.Categories.AddCategoryMapping(1777, TorznabCatType.AudioVideo, "|- Балет и современная хореография (HD Видео)");
            caps.Categories.AddCategoryMapping(2530, TorznabCatType.AudioVideo, "|- Фольклор, Народная, Этническая музыка и Flamenco (HD Видео)");
            caps.Categories.AddCategoryMapping(2529, TorznabCatType.AudioVideo, "|- New Age, Relax, Meditative, Рэп, Хип-Хоп, R'n'B, Reggae, Ska, Dub (HD Видео)");
            caps.Categories.AddCategoryMapping(1781, TorznabCatType.AudioVideo, "|- Музыка других жанров, Разножанровые сборные концерты (HD видео)");
            caps.Categories.AddCategoryMapping(2508, TorznabCatType.AudioVideo, "|- Зарубежная поп-музыка (HD Видео)");
            caps.Categories.AddCategoryMapping(2426, TorznabCatType.AudioVideo, "|- Отечественная поп-музыка (HD видео)");
            caps.Categories.AddCategoryMapping(2351, TorznabCatType.AudioVideo, "|- Восточноазиатская Поп-музыка (HD Видео)");
            caps.Categories.AddCategoryMapping(2306, TorznabCatType.AudioVideo, "|- Джаз и Блюз (HD Видео)");
            caps.Categories.AddCategoryMapping(1795, TorznabCatType.AudioVideo, "|- Зарубежный рок (HD Видео)");
            caps.Categories.AddCategoryMapping(2271, TorznabCatType.AudioVideo, "|- Отечественный рок (HD видео)");
            caps.Categories.AddCategoryMapping(1913, TorznabCatType.AudioVideo, "|- Электронная музыка (HD Видео)");
            caps.Categories.AddCategoryMapping(1784, TorznabCatType.AudioVideo, "|- UHD музыкальное видео");
            caps.Categories.AddCategoryMapping(1892, TorznabCatType.AudioVideo, "|- Документальные фильмы о музыке и музыкантах (HD Видео)");
            caps.Categories.AddCategoryMapping(2266, TorznabCatType.AudioVideo, "|- Официальные апскейлы (Blu-ray, HDTV, WEB-DL)");
            caps.Categories.AddCategoryMapping(518, TorznabCatType.AudioVideo, "Некондиционное музыкальное видео (Видео, DVD видео, HD видео)");
            caps.Categories.AddCategoryMapping(5, TorznabCatType.PCGames, "Игры для Windows");
            caps.Categories.AddCategoryMapping(635, TorznabCatType.PCGames, "|- Горячие Новинки");
            caps.Categories.AddCategoryMapping(127, TorznabCatType.PCGames, "|- Аркады");
            caps.Categories.AddCategoryMapping(2203, TorznabCatType.PCGames, "|- Файтинги");
            caps.Categories.AddCategoryMapping(647, TorznabCatType.PCGames, "|- Экшены от первого лица");
            caps.Categories.AddCategoryMapping(646, TorznabCatType.PCGames, "|- Экшены от третьего лица");
            caps.Categories.AddCategoryMapping(50, TorznabCatType.PCGames, "|- Хорроры");
            caps.Categories.AddCategoryMapping(53, TorznabCatType.PCGames, "|- Приключения и квесты");
            caps.Categories.AddCategoryMapping(1008, TorznabCatType.PCGames, "|- Квесты в стиле \"Поиск предметов\"");
            caps.Categories.AddCategoryMapping(900, TorznabCatType.PCGames, "|- Визуальные новеллы");
            caps.Categories.AddCategoryMapping(128, TorznabCatType.PCGames, "|- Для самых маленьких");
            caps.Categories.AddCategoryMapping(2204, TorznabCatType.PCGames, "|- Логические игры");
            caps.Categories.AddCategoryMapping(278, TorznabCatType.PCGames, "|- Шахматы");
            caps.Categories.AddCategoryMapping(52, TorznabCatType.PCGames, "|- Ролевые игры");
            caps.Categories.AddCategoryMapping(54, TorznabCatType.PCGames, "|- Симуляторы");
            caps.Categories.AddCategoryMapping(51, TorznabCatType.PCGames, "|- Стратегии в реальном времени");
            caps.Categories.AddCategoryMapping(2226, TorznabCatType.PCGames, "|- Пошаговые стратегии");
            caps.Categories.AddCategoryMapping(2118, TorznabCatType.PCGames, "|- Антологии и сборники игр");
            caps.Categories.AddCategoryMapping(1310, TorznabCatType.PCGames, "|- Старые игры (Экшены)");
            caps.Categories.AddCategoryMapping(2410, TorznabCatType.PCGames, "|- Старые игры (Ролевые игры)");
            caps.Categories.AddCategoryMapping(2205, TorznabCatType.PCGames, "|- Старые игры (Стратегии)");
            caps.Categories.AddCategoryMapping(2225, TorznabCatType.PCGames, "|- Старые игры (Приключения и квесты)");
            caps.Categories.AddCategoryMapping(2206, TorznabCatType.PCGames, "|- Старые игры (Симуляторы)");
            caps.Categories.AddCategoryMapping(2228, TorznabCatType.PCGames, "|- IBM-PC-несовместимые компьютеры");
            caps.Categories.AddCategoryMapping(139, TorznabCatType.PCGames, "Прочее для Windows-игр");
            caps.Categories.AddCategoryMapping(2478, TorznabCatType.PCGames, "|- Официальные патчи, моды, плагины, дополнения");
            caps.Categories.AddCategoryMapping(2480, TorznabCatType.PCGames, "|- Неофициальные модификации, плагины, дополнения");
            caps.Categories.AddCategoryMapping(2481, TorznabCatType.PCGames, "|- Русификаторы");
            caps.Categories.AddCategoryMapping(2142, TorznabCatType.PCGames, "Прочее для Microsoft Flight Simulator, Prepar3D, X-Plane");
            caps.Categories.AddCategoryMapping(2060, TorznabCatType.PCGames, "|- Сценарии, меши и аэропорты для FS2004, FSX, P3D");
            caps.Categories.AddCategoryMapping(2145, TorznabCatType.PCGames, "|- Самолёты и вертолёты для FS2004, FSX, P3D");
            caps.Categories.AddCategoryMapping(2146, TorznabCatType.PCGames, "|- Миссии, трафик, звуки, паки и утилиты для FS2004, FSX, P3D");
            caps.Categories.AddCategoryMapping(2143, TorznabCatType.PCGames, "|- Сценарии, миссии, трафик, звуки, паки и утилиты для X-Plane");
            caps.Categories.AddCategoryMapping(2012, TorznabCatType.PCGames, "|- Самолёты и вертолёты для X-Plane");
            caps.Categories.AddCategoryMapping(960, TorznabCatType.PCMac, "Игры для Apple Macintosh");
            caps.Categories.AddCategoryMapping(537, TorznabCatType.PCMac, "|- Нативные игры для Mac");
            caps.Categories.AddCategoryMapping(637, TorznabCatType.PCMac, "|- Игры для Mac с Wineskin, DOSBox, Cider и другими");
            caps.Categories.AddCategoryMapping(899, TorznabCatType.PCGames, "Игры для Linux");
            caps.Categories.AddCategoryMapping(1992, TorznabCatType.PCGames, "|- Нативные игры для Linux");
            caps.Categories.AddCategoryMapping(2059, TorznabCatType.PCGames, "|- Игры для Linux с Wine, DOSBox и другими");
            caps.Categories.AddCategoryMapping(548, TorznabCatType.Console, "Игры для консолей");
            caps.Categories.AddCategoryMapping(908, TorznabCatType.Console, "|- PS");
            caps.Categories.AddCategoryMapping(357, TorznabCatType.ConsoleOther, "|- PS2");
            caps.Categories.AddCategoryMapping(886, TorznabCatType.ConsolePS3, "|- PS3");
            caps.Categories.AddCategoryMapping(973, TorznabCatType.ConsolePS4, "|- PS4");
            caps.Categories.AddCategoryMapping(546, TorznabCatType.ConsoleOther, "|- PS5");
            caps.Categories.AddCategoryMapping(1352, TorznabCatType.ConsolePSP, "|- PSP");
            caps.Categories.AddCategoryMapping(1116, TorznabCatType.ConsolePSP, "|- Игры PS1 для PSP");
            caps.Categories.AddCategoryMapping(595, TorznabCatType.ConsolePSVita, "|- PS Vita");
            caps.Categories.AddCategoryMapping(887, TorznabCatType.ConsoleXBox, "|- Original Xbox");
            caps.Categories.AddCategoryMapping(510, TorznabCatType.ConsoleXBox360, "|- Xbox 360");
            caps.Categories.AddCategoryMapping(773, TorznabCatType.ConsoleWii, "|- Wii/WiiU");
            caps.Categories.AddCategoryMapping(774, TorznabCatType.ConsoleNDS, "|- NDS/3DS");
            caps.Categories.AddCategoryMapping(1605, TorznabCatType.Console, "|- Switch");
            caps.Categories.AddCategoryMapping(968, TorznabCatType.Console, "|- Dreamcast");
            caps.Categories.AddCategoryMapping(129, TorznabCatType.Console, "|- Остальные платформы");
            caps.Categories.AddCategoryMapping(2185, TorznabCatType.ConsoleOther, "Видео для консолей");
            caps.Categories.AddCategoryMapping(2487, TorznabCatType.ConsoleOther, "|- Видео для PS Vita");
            caps.Categories.AddCategoryMapping(2182, TorznabCatType.ConsoleOther, "|- Фильмы для PSP");
            caps.Categories.AddCategoryMapping(2181, TorznabCatType.ConsoleOther, "|- Сериалы для PSP");
            caps.Categories.AddCategoryMapping(2180, TorznabCatType.ConsoleOther, "|- Мультфильмы для PSP");
            caps.Categories.AddCategoryMapping(2179, TorznabCatType.ConsoleOther, "|- Дорамы для PSP");
            caps.Categories.AddCategoryMapping(2186, TorznabCatType.ConsoleOther, "|- Аниме для PSP");
            caps.Categories.AddCategoryMapping(700, TorznabCatType.ConsoleOther, "|- Видео для PSP");
            caps.Categories.AddCategoryMapping(1926, TorznabCatType.ConsoleOther, "|- Видео для PS3 и других консолей");
            caps.Categories.AddCategoryMapping(650, TorznabCatType.PCMobileOther, "Игры для мобильных устройств");
            caps.Categories.AddCategoryMapping(2149, TorznabCatType.PCMobileAndroid, "|- Игры для Android");
            caps.Categories.AddCategoryMapping(2420, TorznabCatType.ConsoleOther, "|- Игры для Oculus Quest");
            caps.Categories.AddCategoryMapping(1001, TorznabCatType.PC, "|- Игры для Java");
            caps.Categories.AddCategoryMapping(1004, TorznabCatType.PCMobileOther, "|- Игры для Symbian");
            caps.Categories.AddCategoryMapping(1002, TorznabCatType.PCMobileOther, "|- Игры для Windows Mobile");
            caps.Categories.AddCategoryMapping(240, TorznabCatType.OtherMisc, "Игровое видео");
            caps.Categories.AddCategoryMapping(2415, TorznabCatType.OtherMisc, "|- Видеопрохождения игр");
            caps.Categories.AddCategoryMapping(1012, TorznabCatType.PC, "Операционные системы от Microsoft");
            caps.Categories.AddCategoryMapping(2489, TorznabCatType.PC, "|- Оригинальные образы Windows");
            caps.Categories.AddCategoryMapping(2523, TorznabCatType.PC, "|- Сборки Windows 8 и далее");
            caps.Categories.AddCategoryMapping(2153, TorznabCatType.PC, "|- Сборки Windows XP - Windows 7");
            caps.Categories.AddCategoryMapping(1019, TorznabCatType.PC, "|- Операционные системы выпущенные до Windows XP");
            caps.Categories.AddCategoryMapping(1021, TorznabCatType.PC, "|- Серверные ОС (оригинальные + сборки)");
            caps.Categories.AddCategoryMapping(1025, TorznabCatType.PC, "|- Разное (сборки All-in-One, пакеты обновлений, утилиты, и прочее)");
            caps.Categories.AddCategoryMapping(1376, TorznabCatType.PC, "Linux, Unix и другие ОС");
            caps.Categories.AddCategoryMapping(1379, TorznabCatType.PC, "|- Операционные системы (Linux, Unix)");
            caps.Categories.AddCategoryMapping(1381, TorznabCatType.PC, "|- Программное обеспечение (Linux, Unix)");
            caps.Categories.AddCategoryMapping(1473, TorznabCatType.PC, "|- Другие ОС и ПО под них");
            caps.Categories.AddCategoryMapping(1195, TorznabCatType.PC, "Тестовые диски для настройки аудио/видео аппаратуры");
            caps.Categories.AddCategoryMapping(1013, TorznabCatType.PC, "Системные программы");
            caps.Categories.AddCategoryMapping(1028, TorznabCatType.PC, "|- Работа с жёстким диском");
            caps.Categories.AddCategoryMapping(1029, TorznabCatType.PC, "|- Резервное копирование");
            caps.Categories.AddCategoryMapping(1030, TorznabCatType.PC, "|- Архиваторы и файловые менеджеры");
            caps.Categories.AddCategoryMapping(1031, TorznabCatType.PC, "|- Программы для настройки и оптимизации ОС");
            caps.Categories.AddCategoryMapping(1032, TorznabCatType.PC, "|- Сервисное обслуживание компьютера");
            caps.Categories.AddCategoryMapping(1033, TorznabCatType.PC, "|- Работа с носителями информации");
            caps.Categories.AddCategoryMapping(1034, TorznabCatType.PC, "|- Информация и диагностика");
            caps.Categories.AddCategoryMapping(1066, TorznabCatType.PC, "|- Программы для интернет и сетей");
            caps.Categories.AddCategoryMapping(1035, TorznabCatType.PC, "|- ПО для защиты компьютера (Антивирусное ПО, Фаерволлы)");
            caps.Categories.AddCategoryMapping(1536, TorznabCatType.PC, "|- Драйверы и прошивки");
            caps.Categories.AddCategoryMapping(1051, TorznabCatType.PC, "|- Оригинальные диски к компьютерам и комплектующим");
            caps.Categories.AddCategoryMapping(1040, TorznabCatType.PC, "|- Серверное ПО для Windows");
            caps.Categories.AddCategoryMapping(1041, TorznabCatType.PC, "|- Изменение интерфейса ОС Windows");
            caps.Categories.AddCategoryMapping(1636, TorznabCatType.PC, "|- Скринсейверы");
            caps.Categories.AddCategoryMapping(1042, TorznabCatType.PC, "|- Разное (Системные программы под Windows)");
            caps.Categories.AddCategoryMapping(1014, TorznabCatType.PC, "Системы для бизнеса, офиса, научной и проектной работы");
            caps.Categories.AddCategoryMapping(2134, TorznabCatType.PC, "|- Медицина - интерактивный софт");
            caps.Categories.AddCategoryMapping(1060, TorznabCatType.PC, "|- Всё для дома: кройка, шитьё, кулинария");
            caps.Categories.AddCategoryMapping(1061, TorznabCatType.PC, "|- Офисные системы");
            caps.Categories.AddCategoryMapping(1062, TorznabCatType.PC, "|- Системы для бизнеса");
            caps.Categories.AddCategoryMapping(1067, TorznabCatType.PC, "|- Распознавание текста, звука и синтез речи");
            caps.Categories.AddCategoryMapping(1086, TorznabCatType.PC, "|- Работа с PDF и DjVu");
            caps.Categories.AddCategoryMapping(1068, TorznabCatType.PC, "|- Словари, переводчики");
            caps.Categories.AddCategoryMapping(1063, TorznabCatType.PC, "|- Системы для научной работы");
            caps.Categories.AddCategoryMapping(1087, TorznabCatType.PC, "|- САПР (общие и машиностроительные)");
            caps.Categories.AddCategoryMapping(1192, TorznabCatType.PC, "|- САПР (электроника, автоматика, ГАП)");
            caps.Categories.AddCategoryMapping(1088, TorznabCatType.PC, "|- Программы для архитекторов и строителей");
            caps.Categories.AddCategoryMapping(1193, TorznabCatType.PC, "|- Библиотеки и проекты для архитекторов и дизайнеров интерьеров");
            caps.Categories.AddCategoryMapping(1071, TorznabCatType.PC, "|- Прочие справочные системы");
            caps.Categories.AddCategoryMapping(1073, TorznabCatType.PC, "|- Разное (Системы для бизнеса, офиса, научной и проектной работы)");
            caps.Categories.AddCategoryMapping(1052, TorznabCatType.PC, "Веб-разработка и Программирование");
            caps.Categories.AddCategoryMapping(1053, TorznabCatType.PC, "|- WYSIWYG Редакторы для веб-диза");
            caps.Categories.AddCategoryMapping(1054, TorznabCatType.PC, "|- Текстовые редакторы с подсветкой");
            caps.Categories.AddCategoryMapping(1055, TorznabCatType.PC, "|- Среды программирования, компиляторы и вспомогательные программы");
            caps.Categories.AddCategoryMapping(1056, TorznabCatType.PC, "|- Компоненты для сред программирования");
            caps.Categories.AddCategoryMapping(2077, TorznabCatType.PC, "|- Системы управления базами данных");
            caps.Categories.AddCategoryMapping(1057, TorznabCatType.PC, "|- Скрипты и движки сайтов, CMS а также расширения к ним");
            caps.Categories.AddCategoryMapping(1018, TorznabCatType.PC, "|- Шаблоны для сайтов и CMS");
            caps.Categories.AddCategoryMapping(1058, TorznabCatType.PC, "|- Разное (Веб-разработка и программирование)");
            caps.Categories.AddCategoryMapping(1016, TorznabCatType.PC, "Программы для работы с мультимедиа и 3D");
            caps.Categories.AddCategoryMapping(1079, TorznabCatType.PC, "|- Программные комплекты");
            caps.Categories.AddCategoryMapping(1080, TorznabCatType.PC, "|- Плагины для программ компании Adobe");
            caps.Categories.AddCategoryMapping(1081, TorznabCatType.PC, "|- Графические редакторы");
            caps.Categories.AddCategoryMapping(1082, TorznabCatType.PC, "|- Программы для верстки, печати и работы со шрифтами");
            caps.Categories.AddCategoryMapping(1083, TorznabCatType.PC, "|- 3D моделирование, рендеринг и плагины для них");
            caps.Categories.AddCategoryMapping(1084, TorznabCatType.PC, "|- Анимация");
            caps.Categories.AddCategoryMapping(1085, TorznabCatType.PC, "|- Создание BD/HD/DVD-видео");
            caps.Categories.AddCategoryMapping(1089, TorznabCatType.PC, "|- Редакторы видео");
            caps.Categories.AddCategoryMapping(1090, TorznabCatType.PC, "|- Видео- Аудио- конверторы");
            caps.Categories.AddCategoryMapping(1065, TorznabCatType.PC, "|- Аудио- и видео-, CD- проигрыватели и каталогизаторы");
            caps.Categories.AddCategoryMapping(1064, TorznabCatType.PC, "|- Каталогизаторы и просмотрщики графики");
            caps.Categories.AddCategoryMapping(1092, TorznabCatType.PC, "|- Разное (Программы для работы с мультимедиа и 3D)");
            caps.Categories.AddCategoryMapping(1204, TorznabCatType.PC, "|- Виртуальные студии, секвенсоры и аудиоредакторы");
            caps.Categories.AddCategoryMapping(1027, TorznabCatType.PC, "|- Виртуальные инструменты и синтезаторы");
            caps.Categories.AddCategoryMapping(1199, TorznabCatType.PC, "|- Плагины для обработки звука");
            caps.Categories.AddCategoryMapping(1091, TorznabCatType.PC, "|- Разное (Программы для работы со звуком)");
            caps.Categories.AddCategoryMapping(828, TorznabCatType.OtherMisc, "Материалы для мультимедиа и дизайна");
            caps.Categories.AddCategoryMapping(1357, TorznabCatType.OtherMisc, "|- Авторские работы");
            caps.Categories.AddCategoryMapping(890, TorznabCatType.OtherMisc, "|- Официальные сборники векторных клипартов");
            caps.Categories.AddCategoryMapping(830, TorznabCatType.OtherMisc, "|- Прочие векторные клипарты");
            caps.Categories.AddCategoryMapping(1290, TorznabCatType.OtherMisc, "|- Photostocks");
            caps.Categories.AddCategoryMapping(1962, TorznabCatType.OtherMisc, "|- Дополнения для программ компоузинга и постобработки");
            caps.Categories.AddCategoryMapping(831, TorznabCatType.OtherMisc, "|- Рамки, шаблоны, текстуры и фоны");
            caps.Categories.AddCategoryMapping(829, TorznabCatType.OtherMisc, "|- Прочие растровые клипарты");
            caps.Categories.AddCategoryMapping(633, TorznabCatType.OtherMisc, "|- 3D модели, сцены и материалы");
            caps.Categories.AddCategoryMapping(1009, TorznabCatType.OtherMisc, "|- Футажи");
            caps.Categories.AddCategoryMapping(1963, TorznabCatType.OtherMisc, "|- Прочие сборники футажей");
            caps.Categories.AddCategoryMapping(1954, TorznabCatType.OtherMisc, "|- Музыкальные библиотеки");
            caps.Categories.AddCategoryMapping(1010, TorznabCatType.OtherMisc, "|- Звуковые эффекты");
            caps.Categories.AddCategoryMapping(1674, TorznabCatType.OtherMisc, "|- Библиотеки сэмплов");
            caps.Categories.AddCategoryMapping(2421, TorznabCatType.OtherMisc, "|- Библиотеки и саундбанки для сэмплеров, пресеты для синтезаторов");
            caps.Categories.AddCategoryMapping(2492, TorznabCatType.OtherMisc, "|- Multitracks");
            caps.Categories.AddCategoryMapping(839, TorznabCatType.OtherMisc, "|- Материалы для создания меню и обложек DVD");
            caps.Categories.AddCategoryMapping(1679, TorznabCatType.OtherMisc, "|- Дополнения, стили, кисти, формы, узоры для программ Adobe");
            caps.Categories.AddCategoryMapping(1011, TorznabCatType.OtherMisc, "|- Шрифты");
            caps.Categories.AddCategoryMapping(835, TorznabCatType.OtherMisc, "|- Разное (Материалы для мультимедиа и дизайна)");
            caps.Categories.AddCategoryMapping(1503, TorznabCatType.OtherMisc, "ГИС, системы навигации и карты");
            caps.Categories.AddCategoryMapping(1507, TorznabCatType.OtherMisc, "|- ГИС (Геоинформационные системы)");
            caps.Categories.AddCategoryMapping(1526, TorznabCatType.OtherMisc, "|- Карты, снабженные программной оболочкой");
            caps.Categories.AddCategoryMapping(1508, TorznabCatType.OtherMisc, "|- Атласы и карты современные (после 1950 г.)");
            caps.Categories.AddCategoryMapping(1509, TorznabCatType.OtherMisc, "|- Атласы и карты старинные (до 1950 г.)");
            caps.Categories.AddCategoryMapping(1510, TorznabCatType.OtherMisc, "|- Карты прочие (астрономические, исторические, тематические)");
            caps.Categories.AddCategoryMapping(1511, TorznabCatType.OtherMisc, "|- Встроенная автомобильная навигация");
            caps.Categories.AddCategoryMapping(1512, TorznabCatType.OtherMisc, "|- Garmin");
            caps.Categories.AddCategoryMapping(1513, TorznabCatType.OtherMisc, "|- Ozi");
            caps.Categories.AddCategoryMapping(1514, TorznabCatType.OtherMisc, "|- TomTom");
            caps.Categories.AddCategoryMapping(1515, TorznabCatType.OtherMisc, "|- Navigon / Navitel");
            caps.Categories.AddCategoryMapping(1516, TorznabCatType.OtherMisc, "|- Igo");
            caps.Categories.AddCategoryMapping(1517, TorznabCatType.OtherMisc, "|- Разное - системы навигации и карты");
            caps.Categories.AddCategoryMapping(285, TorznabCatType.PCMobileOther, "Приложения для мобильных устройств");
            caps.Categories.AddCategoryMapping(2154, TorznabCatType.PCMobileAndroid, "|- Приложения для Android");
            caps.Categories.AddCategoryMapping(1005, TorznabCatType.PCMobileOther, "|- Приложения для Java");
            caps.Categories.AddCategoryMapping(289, TorznabCatType.PCMobileOther, "|- Приложения для Symbian");
            caps.Categories.AddCategoryMapping(290, TorznabCatType.PCMobileOther, "|- Приложения для Windows Mobile");
            caps.Categories.AddCategoryMapping(288, TorznabCatType.PCMobileOther, "|- Софт для работы с телефоном");
            caps.Categories.AddCategoryMapping(292, TorznabCatType.PCMobileOther, "|- Прошивки для телефонов");
            caps.Categories.AddCategoryMapping(291, TorznabCatType.PCMobileOther, "|- Обои и темы");
            caps.Categories.AddCategoryMapping(957, TorznabCatType.PCMobileOther, "Видео для мобильных устройств");
            caps.Categories.AddCategoryMapping(287, TorznabCatType.PCMobileOther, "|- Видео для смартфонов и КПК");
            caps.Categories.AddCategoryMapping(286, TorznabCatType.PCMobileOther, "|- Видео в формате 3GP для мобильных");
            caps.Categories.AddCategoryMapping(1366, TorznabCatType.PCMac, "Apple Macintosh");
            caps.Categories.AddCategoryMapping(1368, TorznabCatType.PCMac, "|- Mac OS (для Macintosh)");
            caps.Categories.AddCategoryMapping(1383, TorznabCatType.PCMac, "|- Mac OS (для РС-Хакинтош)");
            caps.Categories.AddCategoryMapping(1394, TorznabCatType.PCMac, "|- Программы для просмотра и обработки видео (Mac OS)");
            caps.Categories.AddCategoryMapping(1370, TorznabCatType.PCMac, "|- Программы для создания и обработки графики (Mac OS)");
            caps.Categories.AddCategoryMapping(2237, TorznabCatType.PCMac, "|- Плагины для программ компании Adobe (Mac OS)");
            caps.Categories.AddCategoryMapping(1372, TorznabCatType.PCMac, "|- Аудио редакторы и конвертеры (Mac OS)");
            caps.Categories.AddCategoryMapping(1373, TorznabCatType.PCMac, "|- Системные программы (Mac OS)");
            caps.Categories.AddCategoryMapping(1375, TorznabCatType.PCMac, "|- Офисные программы (Mac OS)");
            caps.Categories.AddCategoryMapping(1371, TorznabCatType.PCMac, "|- Программы для интернета и сетей (Mac OS)");
            caps.Categories.AddCategoryMapping(1374, TorznabCatType.PCMac, "|- Другие программы (Mac OS)");
            caps.Categories.AddCategoryMapping(1933, TorznabCatType.PCMobileiOS, "iOS");
            caps.Categories.AddCategoryMapping(1935, TorznabCatType.PCMobileiOS, "|- Программы для iOS");
            caps.Categories.AddCategoryMapping(1003, TorznabCatType.PCMobileiOS, "|- Игры для iOS");
            caps.Categories.AddCategoryMapping(1937, TorznabCatType.PCMobileiOS, "|- Разное для iOS");
            caps.Categories.AddCategoryMapping(2235, TorznabCatType.PCMobileiOS, "Видео");
            caps.Categories.AddCategoryMapping(1908, TorznabCatType.PCMobileiOS, "|- Фильмы для iPod, iPhone, iPad");
            caps.Categories.AddCategoryMapping(864, TorznabCatType.PCMobileiOS, "|- Сериалы для iPod, iPhone, iPad");
            caps.Categories.AddCategoryMapping(863, TorznabCatType.PCMobileiOS, "|- Мультфильмы для iPod, iPhone, iPad");
            caps.Categories.AddCategoryMapping(2535, TorznabCatType.PCMobileiOS, "|- Аниме для iPod, iPhone, iPad");
            caps.Categories.AddCategoryMapping(2534, TorznabCatType.PCMobileiOS, "|- Музыкальное видео для iPod, iPhone, iPad");
            caps.Categories.AddCategoryMapping(2238, TorznabCatType.PCMac, "Видео HD");
            caps.Categories.AddCategoryMapping(1936, TorznabCatType.PCMac, "|- Фильмы HD для Apple TV");
            caps.Categories.AddCategoryMapping(315, TorznabCatType.PCMac, "|- Сериалы HD для Apple TV");
            caps.Categories.AddCategoryMapping(1363, TorznabCatType.PCMac, "|- Мультфильмы HD для Apple TV");
            caps.Categories.AddCategoryMapping(2082, TorznabCatType.PCMac, "|- Документальное видео HD для Apple TV");
            caps.Categories.AddCategoryMapping(2241, TorznabCatType.PCMac, "|- Музыкальное видео HD для Apple TV");
            caps.Categories.AddCategoryMapping(2236, TorznabCatType.Audio, "Аудио");
            caps.Categories.AddCategoryMapping(1909, TorznabCatType.AudioAudiobook, "|- Аудиокниги (AAC, ALAC)");
            caps.Categories.AddCategoryMapping(1927, TorznabCatType.AudioLossless, "|- Музыка lossless (ALAC)");
            caps.Categories.AddCategoryMapping(2240, TorznabCatType.Audio, "|- Музыка Lossy (AAC-iTunes)");
            caps.Categories.AddCategoryMapping(2248, TorznabCatType.Audio, "|- Музыка Lossy (AAC)");
            caps.Categories.AddCategoryMapping(2244, TorznabCatType.Audio, "|- Музыка Lossy (AAC) (Singles, EPs)");
            caps.Categories.AddCategoryMapping(10, TorznabCatType.OtherMisc, "Разное (раздачи)");
            caps.Categories.AddCategoryMapping(865, TorznabCatType.OtherMisc, "|- Психоактивные аудиопрограммы");
            caps.Categories.AddCategoryMapping(1100, TorznabCatType.OtherMisc, "|- Аватары, Иконки, Смайлы");
            caps.Categories.AddCategoryMapping(1643, TorznabCatType.OtherMisc, "|- Живопись, Графика, Скульптура, Digital Art");
            caps.Categories.AddCategoryMapping(848, TorznabCatType.OtherMisc, "|- Картинки");
            caps.Categories.AddCategoryMapping(808, TorznabCatType.OtherMisc, "|- Любительские фотографии");
            caps.Categories.AddCategoryMapping(630, TorznabCatType.OtherMisc, "|- Обои");
            caps.Categories.AddCategoryMapping(1664, TorznabCatType.OtherMisc, "|- Фото знаменитостей");
            caps.Categories.AddCategoryMapping(148, TorznabCatType.Audio, "|- Аудио");
            caps.Categories.AddCategoryMapping(807, TorznabCatType.TVOther, "|- Видео");
            caps.Categories.AddCategoryMapping(147, TorznabCatType.Books, "|- Публикации и учебные материалы (тексты)");
            caps.Categories.AddCategoryMapping(847, TorznabCatType.MoviesOther, "|- Трейлеры и дополнительные материалы к фильмам");
            caps.Categories.AddCategoryMapping(1167, TorznabCatType.TVOther, "|- Любительские видеоклипы");
            caps.Categories.AddCategoryMapping(321, TorznabCatType.Other, "|- Отчеты о встречах");

            return caps;
        }

        public override async Task<ConfigurationData> GetConfigurationForSetup()
        {
            try
            {
                configData.CookieHeader.Value = null;
                var response = await RequestWithCookiesAsync(LoginUrl);
                var parser = new HtmlParser();
                using var doc = parser.ParseDocument(response.ContentString);
                var captchaimg = doc.QuerySelector("img[src^=\"https://static.rutracker.cc/captcha/\"]");

                if (captchaimg != null)
                {
                    var captchaImage = await RequestWithCookiesAsync(captchaimg.GetAttribute("src"));
                    configData.CaptchaImage.Value = captchaImage.ContentBytes;

                    _capCodeField = doc.QuerySelector("input[name^=\"cap_code_\"]")?.GetAttribute("name");
                    _capSid = doc.QuerySelector("input[name=\"cap_sid\"]")?.GetAttribute("value");
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
                var parser = new HtmlParser();
                using var doc = parser.ParseDocument(result.ContentString);
                var errorMessage = doc.QuerySelector("h4.warnColor1.tCenter.mrg_16, div.msg-main")?.TextContent.Trim();

                throw new ExceptionWithConfigData(errorMessage ?? "RuTracker authentication failed", configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var searchUrls = CreateSearchUrlsForQuery(query);

            var releases = new List<ReleaseInfo>();

            foreach (var searchUrl in searchUrls)
            {
                Console.WriteLine(searchUrl);

                var results = await RequestWithCookiesAsync(searchUrl);
                if (!results.ContentString.Contains("id=\"logged-in-username\""))
                {
                    // re login
                    await ApplyConfiguration(null);
                    results = await RequestWithCookiesAsync(searchUrl);
                }

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
            }

            return releases.OrderByDescending(o => o.PublishDate).ToArray();
        }

        public override async Task<byte[]> Download(Uri link)
        {
            if (configData.UseMagnetLinks.Value && link.PathAndQuery.Contains("viewtopic.php?t="))
            {
                var response = await RequestWithCookiesAsync(link.ToString());

                var parser = new HtmlParser();
                using var dom = parser.ParseDocument(response.ContentString);
                var magnetLink = dom.QuerySelector("table.attach a.magnet-link[href^=\"magnet:?\"]")?.GetAttribute("href");

                if (magnetLink == null)
                    throw new Exception($"Failed to fetch magnet link from {link}");

                link = new Uri(magnetLink);
            }

            return await base.Download(link);
        }

        private IEnumerable<string> CreateSearchUrlsForQuery(TorznabQuery query)
        {
            var queryCollection = new NameValueCollection();

            var searchString = query.SearchTerm;
            //  replace any space, special char, etc. with % (wildcard)
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = new Regex("[^a-zA-Zа-яА-ЯёЁ0-9]+").Replace(searchString, "%");
            }

            // if the search string is empty use the getnew view
            if (string.IsNullOrWhiteSpace(searchString))
            {
                queryCollection.Set("nm", searchString);
            }
            else // use the normal search
            {
                searchString = searchString.Replace("-", " ");

                if (query.Season is > 0)
                {
                    searchString += " Сезон: " + query.Season;
                }

                if (query.Episode.IsNotNullOrWhiteSpace())
                {
                    searchString += " Серии: " + query.Episode;
                }
                queryCollection.Set("nm", searchString);
            }

            if (query.HasSpecifiedCategories)
            {
                var trackerCategories = MapTorznabCapsToTrackers(query).Distinct().ToList();

                foreach (var trackerCategoriesChunk in trackerCategories.ChunkBy(200))
                {
                    queryCollection.Set("f", string.Join(",", trackerCategoriesChunk));

                    yield return SearchUrl + "?" + queryCollection.GetQueryString();
                }
            }
            else
            {
                yield return SearchUrl + "?" + queryCollection.GetQueryString();
            }
        }

        private IHtmlCollection<IElement> GetReleaseRows(WebResult results)
        {
            var parser = new HtmlParser();
            using var doc = parser.ParseDocument(results.ContentString);
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

                var title = qDetailsLink.TextContent.Trim();
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
                    Title = _titleParser.Parse(
                        title,
                        category,
                        configData.StripRussianLetters.Value,
                        configData.MoveAllTagsToEndOfReleaseTitle.Value,
                        configData.MoveFirstTagsToEndOfReleaseTitle.Value,
                        configData.AddRussianToTitle.Value
                    ),
                    Description = title,
                    Details = details,
                    Link = configData.UseMagnetLinks.Value ? details : link,
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
                var seedersString = qSeeders.QuerySelector("b")?.TextContent.Trim();
                if (!string.IsNullOrWhiteSpace(seedersString))
                    seeders = ParseUtil.CoerceInt(seedersString);
            }
            return seeders;
        }

        private ICollection<int> GetCategoryOfRelease(in IElement row)
        {
            var forum = row.QuerySelector("td.f-name-col > div.f-name > a")?.GetAttribute("href");
            var cat = ParseUtil.GetArgumentFromQueryString(forum, "f");

            return MapTrackerCatToNewznab(cat);
        }

        private long GetSizeOfRelease(in IElement row) => ParseUtil.GetBytes(row.QuerySelector("td.tor-size")?.GetAttribute("data-ts_text"));

        private DateTime GetPublishDateOfRelease(in IElement row) => DateTimeUtil.UnixTimestampToDateTime(long.Parse(row.QuerySelector("td:nth-child(10)")?.GetAttribute("data-ts_text")));

        public class TitleParser
        {
            private static readonly List<Regex> _FindTagsInTitlesRegexList = new List<Regex>
            {
                new Regex(@"\((?>\((?<c>)|[^()]+|\)(?<-c>))*(?(c)(?!))\)"),
                new Regex(@"\[(?>\[(?<c>)|[^\[\]]+|\](?<-c>))*(?(c)(?!))\]")
            };

            private readonly Regex _stripCyrillicRegex = new Regex(@"(\([\p{IsCyrillic}\W]+\))|(^[\p{IsCyrillic}\W\d]+\/ )|([\p{IsCyrillic} \-]+,+)|([\p{IsCyrillic}]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            private readonly Regex _tvTitleCommaRegex = new Regex(@"\s(\d+),(\d+)", RegexOptions.Compiled);
            private readonly Regex _tvTitleCyrillicXRegex = new Regex(@"([\s-])Х+([\s\)\]])", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            private readonly Regex _tvTitleRusSeasonEpisodeOfRegex = new Regex(@"Сезон\s*[:]*\s+(\d+).+(?:Серии|Эпизод|Выпуски)+\s*[:]*\s+(\d+(?:-\d+)?)\s*из\s*([\w?])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            private readonly Regex _tvTitleRusSeasonEpisodeRegex = new Regex(@"Сезон\s*[:]*\s+(\d+).+(?:Серии|Эпизод|Выпуски)+\s*[:]*\s+(\d+(?:-\d+)?)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            private readonly Regex _tvTitleRusSeasonRegex = new Regex(@"Сезон\s*[:]*\s+(\d+(?:-\d+)?)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            private readonly Regex _tvTitleRusEpisodeOfRegex = new Regex(@"(?:Серии|Эпизод|Выпуски)+\s*[:]*\s+(\d+(?:-\d+)?)\s*из\s*([\w?])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            private readonly Regex _tvTitleRusEpisodeRegex = new Regex(@"(?:Серии|Эпизод|Выпуски)+\s*[:]*\s+(\d+(?:-\d+)?)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            public string Parse(string title,
                                ICollection<int> category,
                                bool stripCyrillicLetters = true,
                                bool moveAllTagsToEndOfReleaseTitle = false,
                                bool moveFirstTagsToEndOfReleaseTitle = false,
                                bool addRussianToTitle = false)
            {
                // https://www.fileformat.info/info/unicode/category/Pd/list.htm
                title = Regex.Replace(title, @"\p{Pd}", "-", RegexOptions.Compiled | RegexOptions.IgnoreCase);

                // replace double 4K quality in title
                title = Regex.Replace(title, @"\b(2160p), 4K\b", "$1", RegexOptions.Compiled | RegexOptions.IgnoreCase);

                if (IsAnyTvCategory(category))
                {
                    title = _tvTitleCommaRegex.Replace(title, " $1-$2");
                    title = _tvTitleCyrillicXRegex.Replace(title, "$1XX$2");

                    title = _tvTitleRusSeasonEpisodeOfRegex.Replace(title, "S$1E$2 of $3");
                    title = _tvTitleRusSeasonEpisodeRegex.Replace(title, "S$1E$2");
                    title = _tvTitleRusSeasonRegex.Replace(title, "S$1");
                    title = _tvTitleRusEpisodeOfRegex.Replace(title, "E$1 of $2");
                    title = _tvTitleRusEpisodeRegex.Replace(title, "E$1");
                }
                else if (IsAnyMovieCategory(category))
                {
                    // remove director's name from title
                    // rutracker movies titles look like: russian name / english name (russian director / english director) other stuff
                    // Ирландец / The Irishman (Мартин Скорсезе / Martin Scorsese) [2019, США, криминал, драма, биография, WEB-DL 1080p] Dub (Пифагор) + MVO (Jaskier) + AVO (Юрий Сербин) + Sub Rus, Eng + Original Eng
                    // this part should be removed: (Мартин Скорсезе / Martin Scorsese)
                    title = Regex.Replace(title, @"(\([\p{IsCyrillic}\W]+)\s/\s(.+?)\)", string.Empty, RegexOptions.Compiled | RegexOptions.IgnoreCase);

                    // Bluray quality fix: radarr parse Blu-ray Disc as Bluray-1080p but should be BR-DISK
                    title = Regex.Replace(title, @"\bBlu-ray Disc\b", "BR-DISK", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                }

                // language fix: all rutracker releases contains russian track
                if (addRussianToTitle && (IsAnyTvCategory(category) || IsAnyMovieCategory(category)) && !Regex.Match(title, "\bRUS\b", RegexOptions.IgnoreCase).Success)
                    title += " RUS";

                if (stripCyrillicLetters)
                    title = _stripCyrillicRegex.Replace(title, string.Empty).Trim(' ', '-');

                if (moveAllTagsToEndOfReleaseTitle)
                    title = MoveAllTagsToEndOfReleaseTitle(title);
                else if (moveFirstTagsToEndOfReleaseTitle)
                    title = MoveFirstTagsToEndOfReleaseTitle(title);

                if (IsAnyAudioCategory(category))
                    title = DetectRereleaseInReleaseTitle(title);

                title = Regex.Replace(title, @"\b-Rip\b", "Rip", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                title = Regex.Replace(title, @"\bHDTVRip\b", "HDTV", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                title = Regex.Replace(title, @"\bWEB-DLRip\b", "WEB-DL", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                title = Regex.Replace(title, @"\bWEBDLRip\b", "WEB-DL", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                title = Regex.Replace(title, @"\bWEBDL\b", "WEB-DL", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                title = Regex.Replace(title, @"\bКураж-Бамбей\b", "kurazh", RegexOptions.Compiled | RegexOptions.IgnoreCase);

                title = Regex.Replace(title, @"\(\s*\/\s*", "(", RegexOptions.Compiled);
                title = Regex.Replace(title, @"\s*\/\s*\)", ")", RegexOptions.Compiled);

                title = Regex.Replace(title, @"[\[\(]\s*[\)\]]", "", RegexOptions.Compiled);

                title = title.Trim(' ', '&', ',', '.', '!', '?', '+', '-', '_', '|', '/', '\\', ':');

                // replace multiple spaces with a single space
                title = Regex.Replace(title, @"\s+", " ");

                return title.Trim();
            }

            private static bool IsAnyTvCategory(ICollection<int> category) => category.Contains(TorznabCatType.TV.ID) || TorznabCatType.TV.SubCategories.Any(subCat => category.Contains(subCat.ID));

            private static bool IsAnyMovieCategory(ICollection<int> category) => category.Contains(TorznabCatType.Movies.ID) || TorznabCatType.Movies.SubCategories.Any(subCat => category.Contains(subCat.ID));

            private static bool IsAnyAudioCategory(ICollection<int> category) => category.Contains(TorznabCatType.Audio.ID) || TorznabCatType.Audio.SubCategories.Any(subCat => category.Contains(subCat.ID));

            private static string MoveAllTagsToEndOfReleaseTitle(string input)
            {
                var output = input;
                foreach (var findTagsRegex in _FindTagsInTitlesRegexList)
                {
                    foreach (Match match in findTagsRegex.Matches(input))
                    {
                        var tag = match.ToString();
                        output = $"{output.Replace(tag, "")} {tag}".Trim();
                    }
                }

                return output.Trim();
            }

            private static string MoveFirstTagsToEndOfReleaseTitle(string input)
            {
                var output = input;
                foreach (var findTagsRegex in _FindTagsInTitlesRegexList)
                {
                    var expectedIndex = 0;
                    foreach (Match match in findTagsRegex.Matches(output))
                    {
                        if (match.Index > expectedIndex)
                        {
                            var substring = output.Substring(expectedIndex, match.Index - expectedIndex);
                            if (string.IsNullOrWhiteSpace(substring))
                                expectedIndex = match.Index;
                            else
                                break;
                        }

                        var tag = match.ToString();
                        var regex = new Regex(Regex.Escape(tag));
                        output = $"{regex.Replace(output, string.Empty, 1)} {tag}".Trim();
                        expectedIndex += tag.Length;
                    }
                }

                return output.Trim();
            }

            /// <summary>
            /// Searches the release title to find a 'year1/year2' pattern that would indicate that this is a re-release of an old music album.
            /// If the release is found to be a re-release, this is added to the title as a new tag.
            /// Not to be confused with discographies; they mostly follow the 'year1-year2' pattern.
            /// </summary>
            private static string DetectRereleaseInReleaseTitle(string input)
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
}
