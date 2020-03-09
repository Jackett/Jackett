using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
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
    public class Pornolab : BaseWebIndexer
    {
        private string LoginUrl => SiteLink + "forum/login.php";
        private string SearchUrl => SiteLink + "forum/tracker.php";

        protected string cap_sid = null;
        protected string cap_code_field = null;

        private new ConfigurationDataPornolab configData
        {
            get => (ConfigurationDataPornolab)base.configData;
            set => base.configData = value;
        }

        public Pornolab(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(name: "Pornolab",
                   description: "Pornolab is a Semi-Private Russian site for Adult content",
                   link: "https://pornolab.net/",
                   caps: new TorznabCapabilities(),
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   configData: new ConfigurationDataPornolab())
        {
            Encoding = Encoding.GetEncoding("windows-1251");
            Language = "ru-ru";
            Type = "semi-private";

            // Clean capabilities
            TorznabCaps.Categories.Clear();

            AddCategoryMapping(1768, TorznabCatType.XXX, "Эротические фильмы / Erotic Movies");
            AddCategoryMapping(60, TorznabCatType.XXX, "Документальные фильмы / Documentary & Reality");
            AddCategoryMapping(1644, TorznabCatType.XXX, "Нудизм-Натуризм / Nudity");

            AddCategoryMapping(1111, TorznabCatType.XXXPacks, "Паки полных фильмов / Full Length Movies Packs");
            AddCategoryMapping(508, TorznabCatType.XXX, "Классические фильмы / Classic");
            AddCategoryMapping(555, TorznabCatType.XXX, "Фильмы с сюжетом / Feature & Vignettes");
            AddCategoryMapping(1673, TorznabCatType.XXX, "Гонзо-фильмы / Gonzo");
            AddCategoryMapping(1112, TorznabCatType.XXX, "Фильмы без сюжета 1991-2010 / All Sex & Amateur 1991-2010");
            AddCategoryMapping(1718, TorznabCatType.XXX, "Фильмы без сюжета 2011-2019 / All Sex & Amateur 2011-2019");
            AddCategoryMapping(553, TorznabCatType.XXX, "Лесбо-фильмы / All Girl & Solo");
            AddCategoryMapping(1143, TorznabCatType.XXX, "Этнические фильмы / Ethnic-Themed");
            AddCategoryMapping(1646, TorznabCatType.XXX, "Видео для телефонов и КПК / Pocket РС & Phone Video");

            AddCategoryMapping(1712, TorznabCatType.XXX, "Эротические и Документальные фильмы (DVD и HD) / Erotic, Documentary & Reality (DVD & HD)");
            AddCategoryMapping(1713, TorznabCatType.XXXDVD, "Фильмы с сюжетом, Классические (DVD) / Feature & Vignettes, Classic (DVD)");
            AddCategoryMapping(512, TorznabCatType.XXXDVD, "Гонзо, Лесбо и Фильмы без сюжета (DVD) / Gonzo, All Girl & Solo, All Sex (DVD)");
            AddCategoryMapping(1775, TorznabCatType.XXX, "Фильмы с сюжетом (HD Video) / Feature & Vignettes (HD Video)");
            AddCategoryMapping(1450, TorznabCatType.XXX, "Гонзо, Лесбо и Фильмы без сюжета (HD Video) / Gonzo, All Girl & Solo, All Sex (HD Video)");

            AddCategoryMapping(902, TorznabCatType.XXX, "Русские порнофильмы / Russian Full Length Movies");
            AddCategoryMapping(1675, TorznabCatType.XXXPacks, "Паки русских порнороликов / Russian Clips Packs");
            AddCategoryMapping(36, TorznabCatType.XXX, "Сайтрипы с русскими актрисами 1991-2015 / Russian SiteRip's 1991-2015");
            AddCategoryMapping(1830, TorznabCatType.XXX, "Сайтрипы с русскими актрисами 1991-2015 (HD Video) / Russian SiteRip's 1991-2015 (HD Video)");
            AddCategoryMapping(1803, TorznabCatType.XXX, "Сайтрипы с русскими актрисами 2016-2019 / Russian SiteRip's 2016-2019");
            AddCategoryMapping(1831, TorznabCatType.XXX, "Сайтрипы с русскими актрисами 2016-2019 (HD Video) / Russian SiteRip's 2016-2019 (HD Video)");
            AddCategoryMapping(1741, TorznabCatType.XXX, "Русские Порноролики Разное / Russian Clips (various)");
            AddCategoryMapping(1676, TorznabCatType.XXX, "Русское любительское видео / Russian Amateur Video");

            AddCategoryMapping(1780, TorznabCatType.XXXPacks, "Паки сайтрипов (HD Video) / SiteRip's Packs (HD Video)");
            AddCategoryMapping(1110, TorznabCatType.XXXPacks, "Паки сайтрипов / SiteRip's Packs");
            AddCategoryMapping(1678, TorznabCatType.XXXPacks, "Паки порнороликов по актрисам / Actresses Clips Packs");
            AddCategoryMapping(1124, TorznabCatType.XXX, "Сайтрипы 1991-2010 (HD Video) / SiteRip's 1991-2010 (HD Video)");
            AddCategoryMapping(1784, TorznabCatType.XXX, "Сайтрипы 2011-2012 (HD Video) / SiteRip's 2011-2012 (HD Video)");
            AddCategoryMapping(1769, TorznabCatType.XXX, "Сайтрипы 2013 (HD Video) / SiteRip's 2013 (HD Video)");
            AddCategoryMapping(1793, TorznabCatType.XXX, "Сайтрипы 2014 (HD Video) / SiteRip's 2014 (HD Video)");
            AddCategoryMapping(1797, TorznabCatType.XXX, "Сайтрипы 2015 (HD Video) / SiteRip's 2015 (HD Video)");
            AddCategoryMapping(1804, TorznabCatType.XXX, "Сайтрипы 2016 (HD Video) / SiteRip's 2016 (HD Video)");
            AddCategoryMapping(1819, TorznabCatType.XXX, "Сайтрипы 2017 (HD Video) / SiteRip's 2017 (HD Video)");
            AddCategoryMapping(1825, TorznabCatType.XXX, "Сайтрипы 2018 (HD Video) / SiteRip's 2018 (HD Video)");
            AddCategoryMapping(1836, TorznabCatType.XXX, "Сайтрипы 2019 (HD Video) / SiteRip's 2019 (HD Video)");
            AddCategoryMapping(1451, TorznabCatType.XXX, "Сайтрипы 1991-2010 / SiteRip's 1991-2010");
            AddCategoryMapping(1788, TorznabCatType.XXX, "Сайтрипы 2011-2012 / SiteRip's 2011-2012");
            AddCategoryMapping(1789, TorznabCatType.XXX, "Сайтрипы 2013 / SiteRip's 2013");
            AddCategoryMapping(1792, TorznabCatType.XXX, "Сайтрипы 2014 / SiteRip's 2014");
            AddCategoryMapping(1798, TorznabCatType.XXX, "Сайтрипы 2015 / SiteRip's 2015");
            AddCategoryMapping(1805, TorznabCatType.XXX, "Сайтрипы 2016 / SiteRip's 2016");
            AddCategoryMapping(1820, TorznabCatType.XXX, "Сайтрипы 2017 / SiteRip's 2017");
            AddCategoryMapping(1826, TorznabCatType.XXX, "Сайтрипы 2018 / SiteRip's 2018");
            AddCategoryMapping(1837, TorznabCatType.XXX, "Сайтрипы 2019 / SiteRip's 2019");
            AddCategoryMapping(1707, TorznabCatType.XXX, "Сцены из фильмов / Movie Scenes");
            AddCategoryMapping(284, TorznabCatType.XXX, "Порноролики Разное / Clips (various)");
            AddCategoryMapping(1823, TorznabCatType.XXX, "Порноролики в 3D и Virtual Reality (VR) / 3D & Virtual Reality Videos");

            AddCategoryMapping(1801, TorznabCatType.XXXPacks, "Паки японских фильмов и сайтрипов / Full Length Japanese Movies Packs & SiteRip's Packs");
            AddCategoryMapping(1719, TorznabCatType.XXX, "Японские фильмы и сайтрипы (DVD и HD Video) / Japanese Movies & SiteRip's (DVD & HD Video)");
            AddCategoryMapping(997, TorznabCatType.XXX, "Японские фильмы и сайтрипы 1991-2014 / Japanese Movies & SiteRip's 1991-2014");
            AddCategoryMapping(1818, TorznabCatType.XXX, "Японские фильмы и сайтрипы 2015-2019 / Japanese Movies & SiteRip's 2015-2019");

            AddCategoryMapping(1671, TorznabCatType.XXX, "Эротические студии (видео) / Erotic Video Library");
            AddCategoryMapping(1726, TorznabCatType.XXX, "Met-Art & MetModels");
            AddCategoryMapping(883, TorznabCatType.XXXImageset, "Эротические студии Разное / Erotic Picture Gallery (various)");
            AddCategoryMapping(1759, TorznabCatType.XXXImageset, "Паки сайтрипов эротических студий / Erotic Picture SiteRip's Packs");
            AddCategoryMapping(1728, TorznabCatType.XXXImageset, "Любительское фото / Amateur Picture Gallery");
            AddCategoryMapping(1729, TorznabCatType.XXXPacks, "Подборки по актрисам / Actresses Picture Packs");
            AddCategoryMapping(38, TorznabCatType.XXXImageset, "Подборки сайтрипов / SiteRip's Picture Packs");
            AddCategoryMapping(1757, TorznabCatType.XXXImageset, "Подборки сетов / Picture Sets Packs");
            AddCategoryMapping(1735, TorznabCatType.XXXImageset, "Тематическое и нетрадиционное фото / Misc & Special Interest Picture Packs");
            AddCategoryMapping(1731, TorznabCatType.XXXImageset, "Журналы / Magazines");

            AddCategoryMapping(1679, TorznabCatType.XXX, "Хентай: основной подраздел / Hentai: main subsection");
            AddCategoryMapping(1740, TorznabCatType.XXX, "Хентай в высоком качестве (DVD и HD) / Hentai DVD & HD");
            AddCategoryMapping(1834, TorznabCatType.XXX, "Хентай: ролики 2D / Hentai: 2D video");
            AddCategoryMapping(1752, TorznabCatType.XXX, "Хентай: ролики 3D / Hentai: 3D video");
            AddCategoryMapping(1760, TorznabCatType.XXX, "Хентай: Манга / Hentai: Manga");
            AddCategoryMapping(1781, TorznabCatType.XXX, "Хентай: Арт и HCG / Hentai: Artwork & HCG");
            AddCategoryMapping(1711, TorznabCatType.XXX, "Мультфильмы / Cartoons");
            AddCategoryMapping(1296, TorznabCatType.XXX, "Комиксы и рисунки / Comics & Artwork");

            AddCategoryMapping(1750, TorznabCatType.XXX, "Игры: основной подраздел / Games: main subsection");
            AddCategoryMapping(1756, TorznabCatType.XXX, "Игры: визуальные новеллы / Games: Visual Novels");
            AddCategoryMapping(1785, TorznabCatType.XXX, "Игры: ролевые / Games: role-playing (RPG Maker and WOLF RPG Editor)");
            AddCategoryMapping(1790, TorznabCatType.XXX, "Игры и Софт: Анимация / Software: Animation");
            AddCategoryMapping(1827, TorznabCatType.XXX, "Игры: В разработке и Демо (основной подраздел) / Games: In Progress and Demo (main subsection)");
            AddCategoryMapping(1828, TorznabCatType.XXX, "Игры: В разработке и Демо (ролевые) / Games: In Progress and Demo (role-playing - RPG Maker and WOLF RPG Editor)");

            AddCategoryMapping(1715, TorznabCatType.XXX, "Транссексуалы (DVD и HD) / Transsexual (DVD & HD)");
            AddCategoryMapping(1680, TorznabCatType.XXX, "Транссексуалы / Transsexual");

            AddCategoryMapping(1758, TorznabCatType.XXX, "Бисексуалы / Bisexual");
            AddCategoryMapping(1682, TorznabCatType.XXX, "БДСМ / BDSM");
            AddCategoryMapping(1733, TorznabCatType.XXX, "Женское доминирование и страпон / Femdom & Strapon");
            AddCategoryMapping(1754, TorznabCatType.XXX, "Подглядывание / Voyeur");
            AddCategoryMapping(1734, TorznabCatType.XXX, "Фистинг и дилдо / Fisting & Dildo");
            AddCategoryMapping(1791, TorznabCatType.XXX, "Беременные / Pregnant");
            AddCategoryMapping(509, TorznabCatType.XXX, "Буккаке / Bukkake");
            AddCategoryMapping(1685, TorznabCatType.XXX, "Мочеиспускание / Peeing");
            AddCategoryMapping(1762, TorznabCatType.XXX, "Фетиш / Fetish");

            AddCategoryMapping(903, TorznabCatType.XXX, "Полнометражные гей-фильмы / Full Length Movies (Gay)");
            AddCategoryMapping(1765, TorznabCatType.XXX, "Полнометражные азиатские гей-фильмы / Full-length Asian Films (Gay)");
            AddCategoryMapping(1767, TorznabCatType.XXX, "Классические гей-фильмы (до 1990 года) / Classic Gay Films (Pre-1990's)");
            AddCategoryMapping(1755, TorznabCatType.XXX, "Гей-фильмы в высоком качестве (DVD и HD) / High-Quality Full Length Movies (Gay DVD & HD)");
            AddCategoryMapping(1787, TorznabCatType.XXX, "Азиатские гей-фильмы в высоком качестве (DVD и HD) / High-Quality Full Length Asian Movies (Gay DVD & HD)");
            AddCategoryMapping(1763, TorznabCatType.XXXPacks, "ПАКи гей-роликов и сайтрипов / Clip's & SiteRip's Packs (Gay)");
            AddCategoryMapping(1777, TorznabCatType.XXX, "Гей-ролики в высоком качестве (HD Video) / Gay Clips (HD Video)");
            AddCategoryMapping(1691, TorznabCatType.XXX, "Ролики, SiteRip'ы и сцены из гей-фильмов / Clips & Movie Scenes (Gay)");
            AddCategoryMapping(1692, TorznabCatType.XXXImageset, "Гей-журналы, фото, разное / Magazines, Photo, Rest (Gay)");

            AddCategoryMapping(1817, TorznabCatType.XXX, "Обход блокировки");
            AddCategoryMapping(1670, TorznabCatType.XXX, "Эротическое видео / Erotic&Softcore");
            AddCategoryMapping(1672, TorznabCatType.XXX, "Зарубежные порнофильмы / Full Length Movies");
            AddCategoryMapping(1717, TorznabCatType.XXX, "Зарубежные фильмы в высоком качестве (DVD&HD) / Full Length ..");
            AddCategoryMapping(1674, TorznabCatType.XXX, "Русское порно / Russian Video");
            AddCategoryMapping(1677, TorznabCatType.XXX, "Зарубежные порноролики / Clips");
            AddCategoryMapping(1842, TorznabCatType.XXX, "Сайтрипы 2020 (HD Video) / SiteRip's 2020 (HD Video)");
            AddCategoryMapping(1843, TorznabCatType.XXX, "Сайтрипы 2020 / SiteRip's 2020");
            AddCategoryMapping(1800, TorznabCatType.XXX, "Японское порно / Japanese Adult Video (JAV)");
            AddCategoryMapping(1815, TorznabCatType.XXX, "Архив (Японское порно)");
            AddCategoryMapping(1723, TorznabCatType.XXX, "Эротические студии, фото и журналы / Erotic Picture Gallery ..");
            AddCategoryMapping(1802, TorznabCatType.XXX, "Архив (Фото)");
            AddCategoryMapping(1745, TorznabCatType.XXX, "Хентай и Манга, Мультфильмы и Комиксы, Рисунки / Hentai&Ma..");
            AddCategoryMapping(1838, TorznabCatType.XXX, "Игры / Games");
            AddCategoryMapping(1829, TorznabCatType.XXX, "Обсуждение игр / Games Discussion");
            AddCategoryMapping(11, TorznabCatType.XXX, "Нетрадиционное порно / Special Interest Movies&Clips");
            AddCategoryMapping(1681, TorznabCatType.XXX, "Дефекация / Scat");
            AddCategoryMapping(1683, TorznabCatType.XXX, "Архив (общий)");
            AddCategoryMapping(1688, TorznabCatType.XXX, "Гей-порно / Gay Forum");
            AddCategoryMapping(1720, TorznabCatType.XXX, "Архив (Гей-порно)");

        }

        public override async Task<ConfigurationData> GetConfigurationForSetup()
        {
            configData.CookieHeader.Value = null;
            var response = await RequestStringWithCookies(LoginUrl);
            var LoginResultParser = new HtmlParser();
            var LoginResultDocument = LoginResultParser.ParseDocument(response.Content);
            var captchaimg = LoginResultDocument.QuerySelector("img[src*=\"/captcha/\"]");
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
                { "login", "Login" }
            };

            if (!string.IsNullOrWhiteSpace(cap_sid))
            {
                pairs.Add("cap_sid", cap_sid);
                pairs.Add(cap_code_field, configData.CaptchaText.Value);

                cap_sid = null;
                cap_code_field = null;
            }

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, CookieHeader, true, null, LoginUrl, true);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("Вы зашли как:"), () =>
            {
                logger.Debug(result.Content);
                var errorMessage = "Unknown error message, please report";
                var LoginResultParser = new HtmlParser();
                var LoginResultDocument = LoginResultParser.ParseDocument(result.Content);
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
                queryCollection.Add("nm", searchString);
            }

            var searchUrl = SearchUrl + "?" + queryCollection.GetQueryString();
            var results = await RequestStringWithCookies(searchUrl);
            if (!results.Content.Contains("Вы зашли как:"))
            {
                // re login
                await ApplyConfiguration(null);
                results = await RequestStringWithCookies(searchUrl);
            }
            try
            {
                var RowsSelector = "table#tor-tbl > tbody > tr";

                var SearchResultParser = new HtmlParser();
                var SearchResultDocument = SearchResultParser.ParseDocument(results.Content);
                var Rows = SearchResultDocument.QuerySelectorAll(RowsSelector);
                foreach (var Row in Rows)
                {
                    try
                    {
                        var release = new ReleaseInfo();

                        release.MinimumRatio = 1;
                        release.MinimumSeedTime = 0;

                        var qDownloadLink = Row.QuerySelector("a.tr-dl");
                        if (qDownloadLink == null) // Expects moderation
                            continue;

                        var qForumLink = Row.QuerySelector("a.f");
                        var qDetailsLink = Row.QuerySelector("a.tLink");
                        var qSize = Row.QuerySelector("td:nth-child(6) u");

                        release.Title = qDetailsLink.TextContent;

                        release.Comments = new Uri(SiteLink + "forum/" + qDetailsLink.GetAttribute("href"));
                        release.Description = qForumLink.TextContent;
                        release.Link = release.Comments;
                        release.Guid = release.Link;
                        release.Size = ReleaseInfo.GetBytes(qSize.TextContent);

                        var seeders = Row.QuerySelector("td:nth-child(7) b").TextContent;
                        if (string.IsNullOrWhiteSpace(seeders))
                            seeders = "0";
                        release.Seeders = ParseUtil.CoerceInt(seeders);
                        release.Peers = ParseUtil.CoerceInt(Row.QuerySelector("td:nth-child(8)").TextContent) + release.Seeders;
                        release.Grabs = ParseUtil.CoerceLong(Row.QuerySelector("td:nth-child(9)").TextContent);

                        var timestr = Row.QuerySelector("td:nth-child(10) u").TextContent;
                        release.PublishDate = DateTimeUtil.UnixTimestampToDateTime(long.Parse(timestr));

                        var forum = qForumLink;
                        var forumid = forum.GetAttribute("href").Split('=')[1];
                        release.Category = MapTrackerCatToNewznab(forumid);

                        release.DownloadVolumeFactor = 1;
                        release.UploadVolumeFactor = 1;

                        if (configData.StripRussianLetters.Value)
                        {
                            var regex = new Regex(@"(\([А-Яа-яЁё\W]+\))|(^[А-Яа-яЁё\W\d]+\/ )|([а-яА-ЯЁё \-]+,+)|([а-яА-ЯЁё]+)");
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

        // referer link support
        public override async Task<byte[]> Download(Uri link)
        {
            var downloadlink = link;
            var response = await RequestStringWithCookies(link.ToString());
            var results = response.Content;
            var SearchResultParser = new HtmlParser();
            var SearchResultDocument = SearchResultParser.ParseDocument(results);
            var downloadSelector = "a[class=\"dl-stub dl-link\"]";
            var DlUri = SearchResultDocument.QuerySelector(downloadSelector);
            if (DlUri != null)
            {
                logger.Debug(string.Format("{0}: Download selector {1} matched:{2}", ID, downloadSelector, DlUri.OuterHtml));
                var href = DlUri.GetAttribute("href");
                downloadlink = new Uri(SiteLink + "forum/" + href);

            }
            else
            {
                logger.Error(string.Format("{0}: Download selector {1} didn't match:\n{2}", ID, downloadSelector, results));
                throw new Exception(string.Format("Download selector {0} didn't match", downloadSelector));
            }
            return await base.Download(downloadlink, RequestType.POST, link.ToString());
        }
    }
}
