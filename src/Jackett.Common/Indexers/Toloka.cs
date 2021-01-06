using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class Toloka : BaseWebIndexer
    {
        private string LoginUrl => SiteLink + "/login.php";
        private string SearchUrl => SiteLink + "/tracker.php";

        protected string cap_sid = null;
        protected string cap_code_field = null;

        private new ConfigurationDataToloka configData
        {
            get => (ConfigurationDataToloka)base.configData;
            set => base.configData = value;
        }

        public Toloka(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "toloka",
                   name: "Toloka.to",
                   description: "Toloka is a Semi-Private Ukrainian torrent site with a thriving file-sharing community",
                   link: "https://toloka.to/",
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
                   configData: new ConfigurationDataToloka())
        {
            Encoding = Encoding.UTF8;
            Language = "uk-ua";
            Type = "semi-private";

            AddCategoryMapping(117, TorznabCatType.Movies, "Українське кіно");
            AddCategoryMapping(84, TorznabCatType.Movies, "|-Мультфільми і казки");
            AddCategoryMapping(42, TorznabCatType.Movies, "|-Художні фільми");
            AddCategoryMapping(124, TorznabCatType.TV, "|-Телесеріали");
            AddCategoryMapping(125, TorznabCatType.TV, "|-Мультсеріали");
            AddCategoryMapping(129, TorznabCatType.Movies, "|-АртХаус");
            AddCategoryMapping(219, TorznabCatType.Movies, "|-Аматорське відео");
            AddCategoryMapping(118, TorznabCatType.Movies, "Українське озвучення");
            AddCategoryMapping(16, TorznabCatType.Movies, "|-Фільми");
            AddCategoryMapping(32, TorznabCatType.TV, "|-Телесеріали");
            AddCategoryMapping(19, TorznabCatType.Movies, "|-Мультфільми");
            AddCategoryMapping(44, TorznabCatType.TV, "|-Мультсеріали");
            AddCategoryMapping(127, TorznabCatType.TVAnime, "|-Аніме");
            AddCategoryMapping(55, TorznabCatType.Movies, "|-АртХаус");
            AddCategoryMapping(94, TorznabCatType.MoviesOther, "|-Трейлери");
            AddCategoryMapping(144, TorznabCatType.Movies, "|-Короткометражні");

            AddCategoryMapping(190, TorznabCatType.Movies, "Українські субтитри");
            AddCategoryMapping(70, TorznabCatType.Movies, "|-Фільми");
            AddCategoryMapping(192, TorznabCatType.TV, "|-Телесеріали");
            AddCategoryMapping(193, TorznabCatType.Movies, "|-Мультфільми");
            AddCategoryMapping(195, TorznabCatType.TV, "|-Мультсеріали");
            AddCategoryMapping(194, TorznabCatType.TVAnime, "|-Аніме");
            AddCategoryMapping(196, TorznabCatType.Movies, "|-АртХаус");
            AddCategoryMapping(197, TorznabCatType.Movies, "|-Короткометражні");

            AddCategoryMapping(225, TorznabCatType.TVDocumentary, "Документальні фільми українською");
            AddCategoryMapping(21, TorznabCatType.TVDocumentary, "|-Українські наукові документальні фільми");
            AddCategoryMapping(131, TorznabCatType.TVDocumentary, "|-Українські історичні документальні фільми");
            AddCategoryMapping(226, TorznabCatType.TVDocumentary, "|-BBC");
            AddCategoryMapping(227, TorznabCatType.TVDocumentary, "|-Discovery");
            AddCategoryMapping(228, TorznabCatType.TVDocumentary, "|-National Geographic");
            AddCategoryMapping(229, TorznabCatType.TVDocumentary, "|-History Channel");
            AddCategoryMapping(230, TorznabCatType.TVDocumentary, "|-Інші іноземні документальні фільми");

            AddCategoryMapping(119, TorznabCatType.TVOther, "Телепередачі українською");
            AddCategoryMapping(18, TorznabCatType.TVOther, "|-Музичне відео");
            AddCategoryMapping(132, TorznabCatType.TVOther, "|-Телевізійні шоу та програми");

            AddCategoryMapping(157, TorznabCatType.TVSport, "Український спорт");
            AddCategoryMapping(235, TorznabCatType.TVSport, "|-Олімпіада");
            AddCategoryMapping(170, TorznabCatType.TVSport, "|-Чемпіонати Європи з футболу");
            AddCategoryMapping(162, TorznabCatType.TVSport, "|-Чемпіонати світу з футболу");
            AddCategoryMapping(166, TorznabCatType.TVSport, "|-Чемпіонат та Кубок України з футболу");
            AddCategoryMapping(167, TorznabCatType.TVSport, "|-Єврокубки");
            AddCategoryMapping(168, TorznabCatType.TVSport, "|-Збірна України");
            AddCategoryMapping(169, TorznabCatType.TVSport, "|-Закордонні чемпіонати");
            AddCategoryMapping(54, TorznabCatType.TVSport, "|-Футбольне відео");
            AddCategoryMapping(158, TorznabCatType.TVSport, "|-Баскетбол, хоккей, волейбол, гандбол, футзал");
            AddCategoryMapping(159, TorznabCatType.TVSport, "|-Бокс, реслінг, бойові мистецтва");
            AddCategoryMapping(160, TorznabCatType.TVSport, "|-Авто, мото");
            AddCategoryMapping(161, TorznabCatType.TVSport, "|-Інший спорт, активний відпочинок");

            // AddCategoryMapping(136, TorznabCatType.Other, "HD українською");
            AddCategoryMapping(96, TorznabCatType.MoviesHD, "|-Фільми в HD");
            AddCategoryMapping(173, TorznabCatType.TVHD, "|-Серіали в HD");
            AddCategoryMapping(139, TorznabCatType.MoviesHD, "|-Мультфільми в HD");
            AddCategoryMapping(174, TorznabCatType.TVHD, "|-Мультсеріали в HD");
            AddCategoryMapping(140, TorznabCatType.TVDocumentary, "|-Документальні фільми в HD");
            AddCategoryMapping(120, TorznabCatType.MoviesDVD, "DVD українською");
            AddCategoryMapping(66, TorznabCatType.MoviesDVD, "|-Художні фільми та серіали в DVD");
            AddCategoryMapping(137, TorznabCatType.MoviesDVD, "|-Мультфільми та мультсеріали в DVD");
            AddCategoryMapping(137, TorznabCatType.TV, "|-Мультфільми та мультсеріали в DVD");
            AddCategoryMapping(138, TorznabCatType.MoviesDVD, "|-Документальні фільми в DVD");

            AddCategoryMapping(237, TorznabCatType.Movies, "Відео для мобільних (iOS, Android, Windows Phone)");

            AddCategoryMapping(33, TorznabCatType.AudioVideo, "Звукові доріжки та субтитри");

            AddCategoryMapping(8, TorznabCatType.Audio, "Українська музика (lossy)");
            AddCategoryMapping(23, TorznabCatType.Audio, "|-Поп, Естрада");
            AddCategoryMapping(24, TorznabCatType.Audio, "|-Джаз, Блюз");
            AddCategoryMapping(43, TorznabCatType.Audio, "|-Етно, Фольклор, Народна, Бардівська");
            AddCategoryMapping(35, TorznabCatType.Audio, "|-Інструментальна, Класична та неокласична");
            AddCategoryMapping(37, TorznabCatType.Audio, "|-Рок, Метал, Альтернатива, Панк, СКА");
            AddCategoryMapping(36, TorznabCatType.Audio, "|-Реп, Хіп-хоп, РнБ");
            AddCategoryMapping(38, TorznabCatType.Audio, "|-Електронна музика");
            AddCategoryMapping(56, TorznabCatType.Audio, "|-Невидане");

            AddCategoryMapping(98, TorznabCatType.AudioLossless, "Українська музика (lossless)");
            AddCategoryMapping(100, TorznabCatType.AudioLossless, "|-Поп, Естрада");
            AddCategoryMapping(101, TorznabCatType.AudioLossless, "|-Джаз, Блюз");
            AddCategoryMapping(102, TorznabCatType.AudioLossless, "|-Етно, Фольклор, Народна, Бардівська");
            AddCategoryMapping(103, TorznabCatType.AudioLossless, "|-Інструментальна, Класична та неокласична");
            AddCategoryMapping(104, TorznabCatType.AudioLossless, "|-Рок, Метал, Альтернатива, Панк, СКА");
            AddCategoryMapping(105, TorznabCatType.AudioLossless, "|-Реп, Хіп-хоп, РнБ");
            AddCategoryMapping(106, TorznabCatType.AudioLossless, "|-Електронна музика");

            AddCategoryMapping(11, TorznabCatType.Books, "Друкована література");
            AddCategoryMapping(134, TorznabCatType.Books, "|-Українська художня література (до 1991 р.)");
            AddCategoryMapping(177, TorznabCatType.Books, "|-Українська художня література (після 1991 р.)");
            AddCategoryMapping(178, TorznabCatType.Books, "|-Зарубіжна художня література");
            AddCategoryMapping(179, TorznabCatType.Books, "|-Наукова література (гуманітарні дисципліни)");
            AddCategoryMapping(180, TorznabCatType.Books, "|-Наукова література (природничі дисципліни)");
            AddCategoryMapping(183, TorznabCatType.Books, "|-Навчальна та довідкова");
            AddCategoryMapping(181, TorznabCatType.BooksMags, "|-Періодика");
            AddCategoryMapping(182, TorznabCatType.Books, "|-Батькам та малятам");
            AddCategoryMapping(184, TorznabCatType.BooksComics, "|-Графіка (комікси, манґа, BD та інше)");

            AddCategoryMapping(185, TorznabCatType.AudioAudiobook, "Аудіокниги українською");
            AddCategoryMapping(135, TorznabCatType.AudioAudiobook, "|-Українська художня література");
            AddCategoryMapping(186, TorznabCatType.AudioAudiobook, "|-Зарубіжна художня література");
            AddCategoryMapping(187, TorznabCatType.AudioAudiobook, "|-Історія, біографістика, спогади");
            AddCategoryMapping(189, TorznabCatType.AudioAudiobook, "|-Сирий матеріал");

            AddCategoryMapping(9, TorznabCatType.PC, "Windows");
            AddCategoryMapping(25, TorznabCatType.PC, "|-Windows");
            AddCategoryMapping(199, TorznabCatType.PC, "|-Офіс");
            AddCategoryMapping(200, TorznabCatType.PC, "|-Антивіруси та безпека");
            AddCategoryMapping(201, TorznabCatType.PC, "|-Мультимедія");
            AddCategoryMapping(202, TorznabCatType.PC, "|-Утиліти, обслуговування, мережа");
            AddCategoryMapping(239, TorznabCatType.PC, "Linux, Mac OS");
            AddCategoryMapping(26, TorznabCatType.PC, "|-Linux");
            AddCategoryMapping(27, TorznabCatType.PCMac, "|-Mac OS");
            // AddCategoryMapping(240, TorznabCatType.PC, "Інші OS");
            AddCategoryMapping(211, TorznabCatType.PCMobileAndroid, "|-Android");
            AddCategoryMapping(122, TorznabCatType.PCMobileiOS, "|-iOS");
            AddCategoryMapping(40, TorznabCatType.PCMobileOther, "|-Інші мобільні платформи");

            // AddCategoryMapping(241, TorznabCatType.Other, "Інше");
            // AddCategoryMapping(203, TorznabCatType.Other, "|-Інфодиски, електронні підручники, відеоуроки");
            // AddCategoryMapping(12, TorznabCatType.Other, "|-Шпалери, фотографії та зображення");
            // AddCategoryMapping(249, TorznabCatType.Other, "|-Веб-скрипти");
            AddCategoryMapping(10, TorznabCatType.PCGames, "Ігри українською");
            AddCategoryMapping(28, TorznabCatType.PCGames, "|-PC ігри");
            AddCategoryMapping(259, TorznabCatType.PCGames, "|-Mac ігри");
            AddCategoryMapping(29, TorznabCatType.PCGames, "|-Українізації, доповнення, патчі...");
            AddCategoryMapping(30, TorznabCatType.PCGames, "|-Мобільні та консольні ігри");
            AddCategoryMapping(41, TorznabCatType.PCMobileiOS, "|-iOS");
            AddCategoryMapping(212, TorznabCatType.PCMobileAndroid, "|-Android");
            AddCategoryMapping(205, TorznabCatType.PCGames, "Переклад ігор українською");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string>
            {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "autologin", "on" },
                { "ssl", "on" },
                { "login", "Вхід" }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, CookieHeader, true, null, LoginUrl, true);
            await ConfigureIfOK(result.Cookies, result.ContentString != null && result.ContentString.Contains("logout=true"), () =>
            {
                logger.Debug(result.ContentString);
                var errorMessage = "Unknown error message, please report";
                var LoginResultParser = new HtmlParser();
                var LoginResultDocument = LoginResultParser.ParseDocument(result.ContentString);
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
                    searchString += " Сезон " + query.Season;
                }
                queryCollection.Add("nm", searchString);
            }

            var searchUrl = SearchUrl + "?" + queryCollection.GetQueryString();
            var results = await RequestWithCookiesAsync(searchUrl);
            if (!results.ContentString.Contains("logout=true"))
            {
                // re login
                await ApplyConfiguration(null);
                results = await RequestWithCookiesAsync(searchUrl);
            }
            try
            {
                var RowsSelector = "table.forumline > tbody > tr[class*=prow]";

                var SearchResultParser = new HtmlParser();
                var SearchResultDocument = SearchResultParser.ParseDocument(results.ContentString);
                var Rows = SearchResultDocument.QuerySelectorAll(RowsSelector);
                foreach (var Row in Rows)
                {
                    try
                    {
                        var qDownloadLink = Row.QuerySelector("td:nth-child(6) > a");
                        if (qDownloadLink == null) // Expects moderation
                            continue;

                        var qDetailsLink = Row.QuerySelector("td:nth-child(3) > a");
                        var qSize = Row.QuerySelector("td:nth-child(7)");
                        var seedersStr = Row.QuerySelector("td:nth-child(10) > b").TextContent;
                        var seeders = string.IsNullOrWhiteSpace(seedersStr) ? 0 : ParseUtil.CoerceInt(seedersStr);
                        var timestr = Row.QuerySelector("td:nth-child(13)").TextContent;
                        var forum = Row.QuerySelector("td:nth-child(2) > a");
                        var forumid = forum.GetAttribute("href").Split('=')[1];
                        var details = new Uri(SiteLink + qDetailsLink.GetAttribute("href"));
                        var link = new Uri(SiteLink + qDownloadLink.GetAttribute("href"));
                        var size = ReleaseInfo.GetBytes(qSize.TextContent);
                        var leechers = ParseUtil.CoerceInt(Row.QuerySelector("td:nth-child(11) > b").TextContent);
                        var publishDate = DateTimeUtil.FromFuzzyTime(timestr);
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
                            Grabs = 0, //ParseUtil.CoerceLong(Row.QuerySelector("td:nth-child(9)").TextContent);
                            PublishDate = publishDate,
                            Category = MapTrackerCatToNewznab(forumid),
                            DownloadVolumeFactor = 1,
                            UploadVolumeFactor = 1
                        };

                        // TODO cleanup
                        if (release.Category.Contains(TorznabCatType.TV.ID))
                        {
                            // extract season and episodes
                            var regex = new Regex(".+\\/\\s([^а-яА-я\\/]+)\\s\\/.+Сезон\\s*[:]*\\s+(\\d+).+(?:Серії|Епізод)+\\s*[:]*\\s+(\\d+-*\\d*).+,\\s+(.+)\\]\\s(.+)");
                            var title = regex.Replace(release.Title, "$1 - S$2E$3 - rus $4 $5");
                            title = Regex.Replace(title, "-Rip", "Rip", RegexOptions.IgnoreCase);
                            title = Regex.Replace(title, "WEB-DLRip", "WEBDL", RegexOptions.IgnoreCase);
                            title = Regex.Replace(title, "WEB-DL", "WEBDL", RegexOptions.IgnoreCase);
                            title = Regex.Replace(title, "HDTVRip", "HDTV", RegexOptions.IgnoreCase);

                            release.Title = title;
                        }
                        else if (configData.StripCyrillicLetters.Value)
                        {
                            var regex = new Regex(@"(\([А-Яа-яіІєЄїЇ\W]+\))|(^[А-Яа-яіІєЄїЇ\W\d]+\/ )|([а-яА-ЯіІєЄїЇ \-]+,+)|([а-яА-ЯіІєЄїЇ]+)");
                            release.Title = regex.Replace(release.Title, "");
                        }

                        releases.Add(release);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(string.Format("{0}: Error while parsing row '{1}':\n\n{2}", Id, Row.OuterHtml, ex));
                    }
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.ContentString, ex);
            }

            return releases;
        }
    }
}
