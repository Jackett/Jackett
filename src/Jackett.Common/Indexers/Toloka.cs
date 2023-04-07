using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class Toloka : IndexerBase
    {
        public override string Id => "toloka";
        public override string Name => "Toloka.to";
        public override string Description => "Toloka is a Semi-Private Ukrainian torrent site with a thriving file-sharing community";
        public override string SiteLink { get; protected set; } = "https://toloka.to/";
        public override string Language => "uk-UA";
        public override string Type => "semi-private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private new ConfigurationDataToloka configData
        {
            get => (ConfigurationDataToloka)base.configData;
            set => base.configData = value;
        }

        private readonly TitleParser _titleParser = new TitleParser();
        private string LoginUrl => SiteLink + "login.php";
        private string SearchUrl => SiteLink + "tracker.php";

        public Toloka(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps, ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataToloka())
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
                }
            };

            caps.Categories.AddCategoryMapping(117, TorznabCatType.Movies, "Українське кіно");
            caps.Categories.AddCategoryMapping(84, TorznabCatType.Movies, "|-Мультфільми і казки");
            caps.Categories.AddCategoryMapping(42, TorznabCatType.Movies, "|-Художні фільми");
            caps.Categories.AddCategoryMapping(124, TorznabCatType.TV, "|-Телесеріали");
            caps.Categories.AddCategoryMapping(125, TorznabCatType.TV, "|-Мультсеріали");
            caps.Categories.AddCategoryMapping(129, TorznabCatType.Movies, "|-АртХаус");
            caps.Categories.AddCategoryMapping(219, TorznabCatType.Movies, "|-Аматорське відео");
            caps.Categories.AddCategoryMapping(118, TorznabCatType.Movies, "Українське озвучення");
            caps.Categories.AddCategoryMapping(16, TorznabCatType.Movies, "|-Фільми");
            caps.Categories.AddCategoryMapping(32, TorznabCatType.TV, "|-Телесеріали");
            caps.Categories.AddCategoryMapping(19, TorznabCatType.Movies, "|-Мультфільми");
            caps.Categories.AddCategoryMapping(44, TorznabCatType.TV, "|-Мультсеріали");
            caps.Categories.AddCategoryMapping(127, TorznabCatType.TVAnime, "|-Аніме");
            caps.Categories.AddCategoryMapping(55, TorznabCatType.Movies, "|-АртХаус");
            caps.Categories.AddCategoryMapping(94, TorznabCatType.MoviesOther, "|-Трейлери");
            caps.Categories.AddCategoryMapping(144, TorznabCatType.Movies, "|-Короткометражні");

            caps.Categories.AddCategoryMapping(190, TorznabCatType.Movies, "Українські субтитри");
            caps.Categories.AddCategoryMapping(70, TorznabCatType.Movies, "|-Фільми");
            caps.Categories.AddCategoryMapping(192, TorznabCatType.TV, "|-Телесеріали");
            caps.Categories.AddCategoryMapping(193, TorznabCatType.Movies, "|-Мультфільми");
            caps.Categories.AddCategoryMapping(195, TorznabCatType.TV, "|-Мультсеріали");
            caps.Categories.AddCategoryMapping(194, TorznabCatType.TVAnime, "|-Аніме");
            caps.Categories.AddCategoryMapping(196, TorznabCatType.Movies, "|-АртХаус");
            caps.Categories.AddCategoryMapping(197, TorznabCatType.Movies, "|-Короткометражні");

            caps.Categories.AddCategoryMapping(225, TorznabCatType.TVDocumentary, "Документальні фільми українською");
            caps.Categories.AddCategoryMapping(21, TorznabCatType.TVDocumentary, "|-Українські наукові документальні фільми");
            caps.Categories.AddCategoryMapping(131, TorznabCatType.TVDocumentary, "|-Українські історичні документальні фільми");
            caps.Categories.AddCategoryMapping(226, TorznabCatType.TVDocumentary, "|-BBC");
            caps.Categories.AddCategoryMapping(227, TorznabCatType.TVDocumentary, "|-Discovery");
            caps.Categories.AddCategoryMapping(228, TorznabCatType.TVDocumentary, "|-National Geographic");
            caps.Categories.AddCategoryMapping(229, TorznabCatType.TVDocumentary, "|-History Channel");
            caps.Categories.AddCategoryMapping(230, TorznabCatType.TVDocumentary, "|-Інші іноземні документальні фільми");

            caps.Categories.AddCategoryMapping(119, TorznabCatType.TVOther, "Телепередачі українською");
            caps.Categories.AddCategoryMapping(18, TorznabCatType.TVOther, "|-Музичне відео");
            caps.Categories.AddCategoryMapping(132, TorznabCatType.TVOther, "|-Телевізійні шоу та програми");

            caps.Categories.AddCategoryMapping(157, TorznabCatType.TVSport, "Український спорт");
            caps.Categories.AddCategoryMapping(235, TorznabCatType.TVSport, "|-Олімпіада");
            caps.Categories.AddCategoryMapping(170, TorznabCatType.TVSport, "|-Чемпіонати Європи з футболу");
            caps.Categories.AddCategoryMapping(162, TorznabCatType.TVSport, "|-Чемпіонати світу з футболу");
            caps.Categories.AddCategoryMapping(166, TorznabCatType.TVSport, "|-Чемпіонат та Кубок України з футболу");
            caps.Categories.AddCategoryMapping(167, TorznabCatType.TVSport, "|-Єврокубки");
            caps.Categories.AddCategoryMapping(168, TorznabCatType.TVSport, "|-Збірна України");
            caps.Categories.AddCategoryMapping(169, TorznabCatType.TVSport, "|-Закордонні чемпіонати");
            caps.Categories.AddCategoryMapping(54, TorznabCatType.TVSport, "|-Футбольне відео");
            caps.Categories.AddCategoryMapping(158, TorznabCatType.TVSport, "|-Баскетбол, хоккей, волейбол, гандбол, футзал");
            caps.Categories.AddCategoryMapping(159, TorznabCatType.TVSport, "|-Бокс, реслінг, бойові мистецтва");
            caps.Categories.AddCategoryMapping(160, TorznabCatType.TVSport, "|-Авто, мото");
            caps.Categories.AddCategoryMapping(161, TorznabCatType.TVSport, "|-Інший спорт, активний відпочинок");

            // caps.Categories.AddCategoryMapping(136, TorznabCatType.Other, "HD українською");
            caps.Categories.AddCategoryMapping(96, TorznabCatType.MoviesHD, "|-Фільми в HD");
            caps.Categories.AddCategoryMapping(173, TorznabCatType.TVHD, "|-Серіали в HD");
            caps.Categories.AddCategoryMapping(139, TorznabCatType.MoviesHD, "|-Мультфільми в HD");
            caps.Categories.AddCategoryMapping(174, TorznabCatType.TVHD, "|-Мультсеріали в HD");
            caps.Categories.AddCategoryMapping(140, TorznabCatType.TVDocumentary, "|-Документальні фільми в HD");
            caps.Categories.AddCategoryMapping(120, TorznabCatType.MoviesDVD, "DVD українською");
            caps.Categories.AddCategoryMapping(66, TorznabCatType.MoviesDVD, "|-Художні фільми та серіали в DVD");
            caps.Categories.AddCategoryMapping(137, TorznabCatType.MoviesDVD, "|-Мультфільми та мультсеріали в DVD");
            caps.Categories.AddCategoryMapping(137, TorznabCatType.TV, "|-Мультфільми та мультсеріали в DVD");
            caps.Categories.AddCategoryMapping(138, TorznabCatType.MoviesDVD, "|-Документальні фільми в DVD");

            caps.Categories.AddCategoryMapping(237, TorznabCatType.Movies, "Відео для мобільних (iOS, Android, Windows Phone)");

            caps.Categories.AddCategoryMapping(33, TorznabCatType.AudioVideo, "Звукові доріжки та субтитри");

            caps.Categories.AddCategoryMapping(8, TorznabCatType.Audio, "Українська музика (lossy)");
            caps.Categories.AddCategoryMapping(23, TorznabCatType.Audio, "|-Поп, Естрада");
            caps.Categories.AddCategoryMapping(24, TorznabCatType.Audio, "|-Джаз, Блюз");
            caps.Categories.AddCategoryMapping(43, TorznabCatType.Audio, "|-Етно, Фольклор, Народна, Бардівська");
            caps.Categories.AddCategoryMapping(35, TorznabCatType.Audio, "|-Інструментальна, Класична та неокласична");
            caps.Categories.AddCategoryMapping(37, TorznabCatType.Audio, "|-Рок, Метал, Альтернатива, Панк, СКА");
            caps.Categories.AddCategoryMapping(36, TorznabCatType.Audio, "|-Реп, Хіп-хоп, РнБ");
            caps.Categories.AddCategoryMapping(38, TorznabCatType.Audio, "|-Електронна музика");
            caps.Categories.AddCategoryMapping(56, TorznabCatType.Audio, "|-Невидане");

            caps.Categories.AddCategoryMapping(98, TorznabCatType.AudioLossless, "Українська музика (lossless)");
            caps.Categories.AddCategoryMapping(100, TorznabCatType.AudioLossless, "|-Поп, Естрада");
            caps.Categories.AddCategoryMapping(101, TorznabCatType.AudioLossless, "|-Джаз, Блюз");
            caps.Categories.AddCategoryMapping(102, TorznabCatType.AudioLossless, "|-Етно, Фольклор, Народна, Бардівська");
            caps.Categories.AddCategoryMapping(103, TorznabCatType.AudioLossless, "|-Інструментальна, Класична та неокласична");
            caps.Categories.AddCategoryMapping(104, TorznabCatType.AudioLossless, "|-Рок, Метал, Альтернатива, Панк, СКА");
            caps.Categories.AddCategoryMapping(105, TorznabCatType.AudioLossless, "|-Реп, Хіп-хоп, РнБ");
            caps.Categories.AddCategoryMapping(106, TorznabCatType.AudioLossless, "|-Електронна музика");

            caps.Categories.AddCategoryMapping(11, TorznabCatType.Books, "Друкована література");
            caps.Categories.AddCategoryMapping(134, TorznabCatType.Books, "|-Українська художня література (до 1991 р.)");
            caps.Categories.AddCategoryMapping(177, TorznabCatType.Books, "|-Українська художня література (після 1991 р.)");
            caps.Categories.AddCategoryMapping(178, TorznabCatType.Books, "|-Зарубіжна художня література");
            caps.Categories.AddCategoryMapping(179, TorznabCatType.Books, "|-Наукова література (гуманітарні дисципліни)");
            caps.Categories.AddCategoryMapping(180, TorznabCatType.Books, "|-Наукова література (природничі дисципліни)");
            caps.Categories.AddCategoryMapping(183, TorznabCatType.Books, "|-Навчальна та довідкова");
            caps.Categories.AddCategoryMapping(181, TorznabCatType.BooksMags, "|-Періодика");
            caps.Categories.AddCategoryMapping(182, TorznabCatType.Books, "|-Батькам та малятам");
            caps.Categories.AddCategoryMapping(184, TorznabCatType.BooksComics, "|-Графіка (комікси, манґа, BD та інше)");

            caps.Categories.AddCategoryMapping(185, TorznabCatType.AudioAudiobook, "Аудіокниги українською");
            caps.Categories.AddCategoryMapping(135, TorznabCatType.AudioAudiobook, "|-Українська художня література");
            caps.Categories.AddCategoryMapping(186, TorznabCatType.AudioAudiobook, "|-Зарубіжна художня література");
            caps.Categories.AddCategoryMapping(187, TorznabCatType.AudioAudiobook, "|-Історія, біографістика, спогади");
            caps.Categories.AddCategoryMapping(189, TorznabCatType.AudioAudiobook, "|-Сирий матеріал");

            caps.Categories.AddCategoryMapping(9, TorznabCatType.PC, "Windows");
            caps.Categories.AddCategoryMapping(25, TorznabCatType.PC, "|-Windows");
            caps.Categories.AddCategoryMapping(199, TorznabCatType.PC, "|-Офіс");
            caps.Categories.AddCategoryMapping(200, TorznabCatType.PC, "|-Антивіруси та безпека");
            caps.Categories.AddCategoryMapping(201, TorznabCatType.PC, "|-Мультимедія");
            caps.Categories.AddCategoryMapping(202, TorznabCatType.PC, "|-Утиліти, обслуговування, мережа");
            caps.Categories.AddCategoryMapping(239, TorznabCatType.PC, "Linux, Mac OS");
            caps.Categories.AddCategoryMapping(26, TorznabCatType.PC, "|-Linux");
            caps.Categories.AddCategoryMapping(27, TorznabCatType.PCMac, "|-Mac OS");
            // caps.Categories.AddCategoryMapping(240, TorznabCatType.PC, "Інші OS");
            caps.Categories.AddCategoryMapping(211, TorznabCatType.PCMobileAndroid, "|-Android");
            caps.Categories.AddCategoryMapping(122, TorznabCatType.PCMobileiOS, "|-iOS");
            caps.Categories.AddCategoryMapping(40, TorznabCatType.PCMobileOther, "|-Інші мобільні платформи");

            // caps.Categories.AddCategoryMapping(241, TorznabCatType.Other, "Інше");
            // caps.Categories.AddCategoryMapping(203, TorznabCatType.Other, "|-Інфодиски, електронні підручники, відеоуроки");
            // caps.Categories.AddCategoryMapping(12, TorznabCatType.Other, "|-Шпалери, фотографії та зображення");
            // caps.Categories.AddCategoryMapping(249, TorznabCatType.Other, "|-Веб-скрипти");
            caps.Categories.AddCategoryMapping(10, TorznabCatType.PCGames, "Ігри українською");
            caps.Categories.AddCategoryMapping(28, TorznabCatType.PCGames, "|-PC ігри");
            caps.Categories.AddCategoryMapping(259, TorznabCatType.PCGames, "|-Mac ігри");
            caps.Categories.AddCategoryMapping(29, TorznabCatType.PCGames, "|-Українізації, доповнення, патчі...");
            caps.Categories.AddCategoryMapping(30, TorznabCatType.PCGames, "|-Мобільні та консольні ігри");
            caps.Categories.AddCategoryMapping(41, TorznabCatType.PCMobileiOS, "|-iOS");
            caps.Categories.AddCategoryMapping(212, TorznabCatType.PCMobileAndroid, "|-Android");
            caps.Categories.AddCategoryMapping(205, TorznabCatType.PCGames, "Переклад ігор українською");

            return caps;
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
                { "redirect", "" },
                { "login", "Вхід" }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, CookieHeader, true, null, LoginUrl, true);
            await ConfigureIfOK(result.Cookies, result.ContentString != null && result.ContentString.Contains("logout=true"), () =>
            {
                var loginResultParser = new HtmlParser();
                var loginResultDocument = loginResultParser.ParseDocument(result.ContentString);
                var errorMessage = loginResultDocument.QuerySelector("table.forumline table span.gen")?.FirstChild?.TextContent;

                throw new ExceptionWithConfigData(errorMessage ?? "Unknown error message, please report.", configData);
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.SanitizedSearchTerm;

            var qc = new List<KeyValuePair<string, string>> // NameValueCollection don't support cat[]=19&cat[]=6
            {
                { "o", "1" },
                { "s", "2" }
            };

            if (configData.FreeleechOnly.Value)
            {
                qc.Add("sds", "1");
            }

            // if the search string is empty use the getnew view
            if (string.IsNullOrWhiteSpace(searchString))
            {
                qc.Add("nm", searchString);
            }
            else // use the normal search
            {
                searchString = searchString.Replace("-", " ");
                if (query.Season != 0)
                {
                    searchString += " Сезон " + query.Season;
                }

                qc.Add("nm", searchString);
            }

            foreach (var cat in MapTorznabCapsToTrackers(query))
            {
                qc.Add("f[]", cat);
            }

            var searchUrl = SearchUrl + "?" + qc.GetQueryString();
            var results = await RequestWithCookiesAsync(searchUrl);

            if (!results.ContentString.Contains("logout=true"))
            {
                // re login
                await ApplyConfiguration(null);
                results = await RequestWithCookiesAsync(searchUrl);
            }

            try
            {
                var searchResultParser = new HtmlParser();
                var searchResultDocument = searchResultParser.ParseDocument(results.ContentString);
                var rows = searchResultDocument.QuerySelectorAll("table.forumline > tbody > tr[class*=\"prow\"]");

                foreach (var row in rows)
                {
                    try
                    {
                        var qDownloadLink = row.QuerySelector("td:nth-child(6) > a");

                        if (qDownloadLink == null) // Expects moderation
                        {
                            continue;
                        }

                        var qDetailsLink = row.QuerySelector("td:nth-child(3) > a");
                        var details = new Uri(SiteLink + qDetailsLink.GetAttribute("href"));
                        var title = qDetailsLink.TextContent.Trim();
                        var link = new Uri(SiteLink + qDownloadLink.GetAttribute("href"));
                        var forumLink = row.QuerySelector("td:nth-child(2) > a").GetAttribute("href");
                        var forumId = ParseUtil.GetArgumentFromQueryString(forumLink, "f");
                        var category = MapTrackerCatToNewznab(forumId);
                        var seedersStr = row.QuerySelector("td:nth-child(10) > b").TextContent;
                        var seeders = string.IsNullOrWhiteSpace(seedersStr) ? 0 : ParseUtil.CoerceInt(seedersStr);
                        var leechers = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(11) > b").TextContent);

                        var release = new ReleaseInfo
                        {
                            Guid = details,
                            Details = details,
                            Link = link,
                            Title = _titleParser.Parse(title, category, configData.StripCyrillicLetters.Value),
                            Description = title,
                            Category = category,
                            Size = ParseUtil.GetBytes(row.QuerySelector("td:nth-child(7)").TextContent),
                            Seeders = seeders,
                            Peers = leechers + seeders,
                            Grabs = 0, //ParseUtil.CoerceLong(Row.QuerySelector("td:nth-child(9)").TextContent);
                            PublishDate = DateTimeUtil.FromFuzzyTime(row.QuerySelector("td:nth-child(13)").TextContent),
                            DownloadVolumeFactor = 1,
                            UploadVolumeFactor = 1,
                            MinimumRatio = 1,
                            MinimumSeedTime = 0
                        };

                        if (row.QuerySelector("img[src=\"images/gold.gif\"], img[src=\"images/authors.gif\"]") != null)
                        {
                            release.DownloadVolumeFactor = 0;
                        }
                        else if (row.QuerySelector("img[src=\"images/silver.gif\"]") != null)
                        {
                            release.DownloadVolumeFactor = 0.5;
                        }
                        else if (row.QuerySelector("img[src=\"images/bronze.gif\"]") != null)
                        {
                            release.DownloadVolumeFactor = 0.75;
                        }

                        releases.Add(release);
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"{Id}: Error while parsing row '{row.OuterHtml}':\n\n{ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.ContentString, ex);
            }

            return releases;
        }

        public class TitleParser
        {
            private static readonly List<Regex> _FindTagsInTitlesRegexList = new List<Regex>
            {
                new Regex(@"\((?>\((?<c>)|[^()]+|\)(?<-c>))*(?(c)(?!))\)"),
                new Regex(@"\[(?>\[(?<c>)|[^\[\]]+|\](?<-c>))*(?(c)(?!))\]")
            };

            private readonly Regex _tvTitleCommaRegex = new Regex(@"\s(\d+),(\d+)", RegexOptions.Compiled);
            private readonly Regex _tvTitleCyrillicXRegex = new Regex(@"([\s-])Х+([\)\]])", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            private readonly Regex _tvTitleMultipleSeasonsRegex = new Regex(@"(?:Сезон|Seasons?)\s*[:]*\s+(\d+-\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            private readonly Regex _tvTitleUkrSeasonEpisodeOfRegex = new Regex(@"Сезон\s*[:]*\s+(\d+).+(?:Серії|Серія|Серій|Епізод)+\s*[:]*\s+(\d+(?:-\d+)?)\s*з\s*([\w?])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            private readonly Regex _tvTitleUkrSeasonEpisodeRegex = new Regex(@"Сезон\s*[:]*\s+(\d+).+(?:Серії|Серія|Серій|Епізод)+\s*[:]*\s+(\d+(?:-\d+)?)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            private readonly Regex _tvTitleUkrSeasonRegex = new Regex(@"Сезон\s*[:]*\s+(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            private readonly Regex _tvTitleUkrEpisodeOfRegex = new Regex(@"(?:Серії|Серія|Серій|Епізод)+\s*[:]*\s+(\d+(?:-\d+)?)\s*з\s*([\w?])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            private readonly Regex _tvTitleUkrEpisodeRegex = new Regex(@"(?:Серії|Серія|Серій|Епізод)+\s*[:]*\s+(\d+(?:-\d+)?)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            private readonly Regex _tvTitleEngSeasonEpisodeOfRegex = new Regex(@"Season\s*[:]*\s+(\d+).+(?:Episodes?)+\s*[:]*\s+(\d+(?:-\d+)?)\s*of\s*([\w?])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            private readonly Regex _tvTitleEngSeasonEpisodeRegex = new Regex(@"Season\s*[:]*\s+(\d+).+(?:Episodes?)+\s*[:]*\s+(\d+(?:-\d+)?)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            private readonly Regex _tvTitleEngSeasonRegex = new Regex(@"Season\s*[:]*\s+(\d+(?:-\d+)?)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            private readonly Regex _tvTitleEngEpisodeOfRegex = new Regex(@"(?:Episodes?)+\s*[:]*\s+(\d+(?:-\d+)?)\s*of\s*([\w?])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            private readonly Regex _tvTitleEngEpisodeRegex = new Regex(@"(?:Episodes?)+\s*[:]+\s*[:]*\s+(\d+(?:-\d+)?)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            private readonly Regex _stripCyrillicRegex = new Regex(@"(\([\p{IsCyrillic}\W]+\))|(^[\p{IsCyrillic}\W\d]+\/ )|([\p{IsCyrillic} \-]+,+)|([\p{IsCyrillic}]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            public string Parse(string title, ICollection<int> category, bool stripCyrillicLetters = true)
            {
                // https://www.fileformat.info/info/unicode/category/Pd/list.htm
                title = Regex.Replace(title, @"\p{Pd}", "-", RegexOptions.Compiled | RegexOptions.IgnoreCase);

                if (IsAnyTvCategory(category))
                {
                    title = _tvTitleCommaRegex.Replace(title, " $1-$2");
                    title = _tvTitleCyrillicXRegex.Replace(title, "$1XX$2");

                    // special case for multiple seasons
                    title = _tvTitleMultipleSeasonsRegex.Replace(title, "S$1");

                    title = _tvTitleUkrSeasonEpisodeOfRegex.Replace(title, "S$1E$2 of $3");
                    title = _tvTitleUkrSeasonEpisodeRegex.Replace(title, "S$1E$2");
                    title = _tvTitleUkrSeasonRegex.Replace(title, "S$1");
                    title = _tvTitleUkrEpisodeOfRegex.Replace(title, "E$1 of $2");
                    title = _tvTitleUkrEpisodeRegex.Replace(title, "E$1");

                    title = _tvTitleEngSeasonEpisodeOfRegex.Replace(title, "S$1E$2 of $3");
                    title = _tvTitleEngSeasonEpisodeRegex.Replace(title, "S$1E$2");
                    title = _tvTitleEngSeasonRegex.Replace(title, "S$1");
                    title = _tvTitleEngEpisodeOfRegex.Replace(title, "E$1 of $2");
                    title = _tvTitleEngEpisodeRegex.Replace(title, "E$1");
                }

                if (stripCyrillicLetters)
                {
                    title = _stripCyrillicRegex.Replace(title, string.Empty).Trim(' ', '-');
                }

                title = Regex.Replace(title, @"\b-Rip\b", "Rip", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                title = Regex.Replace(title, @"\bHDTVRip\b", "HDTV", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                title = Regex.Replace(title, @"\bWEB-DLRip\b", "WEB-DL", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                title = Regex.Replace(title, @"\bWEBDLRip\b", "WEB-DL", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                title = Regex.Replace(title, @"\bWEBDL\b", "WEB-DL", RegexOptions.Compiled | RegexOptions.IgnoreCase);

                title = MoveFirstTagsToEndOfReleaseTitle(title);

                title = Regex.Replace(title, @"\(\s*\/\s*", "(", RegexOptions.Compiled);
                title = Regex.Replace(title, @"\s*\/\s*\)", ")", RegexOptions.Compiled);

                title = Regex.Replace(title, @"[\[\(]\s*[\)\]]", "", RegexOptions.Compiled);

                title = title.Trim(' ', '&', ',', '.', '!', '?', '+', '-', '_', '|', '/', '\\', ':');

                // replace multiple spaces with a single space
                title = Regex.Replace(title, @"\s+", " ");

                return title.Trim();
            }

            private static bool IsAnyTvCategory(ICollection<int> category) => category.Contains(TorznabCatType.TV.ID) || TorznabCatType.TV.SubCategories.Any(subCat => category.Contains(subCat.ID));

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
                            {
                                expectedIndex = match.Index;
                            }
                            else
                            {
                                break;
                            }
                        }

                        var tag = match.ToString();
                        var regex = new Regex(Regex.Escape(tag));
                        output = $"{regex.Replace(output, string.Empty, 1)} {tag}".Trim();
                        expectedIndex += tag.Length;
                    }
                }

                return output.Trim();
            }
        }
    }
}
