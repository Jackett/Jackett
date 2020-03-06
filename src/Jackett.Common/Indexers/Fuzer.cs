using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Jackett.Common.Helpers;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    public class Fuzer : BaseWebIndexer
    {
        public override string[] LegacySiteLinks { get; protected set; } =
        {
            "https://fuzer.me/"
        };

        private string SearchUrl => SiteLink + "browse.php";
        private string LoginUrl => SiteLink + "login.php";

        private new ConfigurationDataRecaptchaLogin configData
        {
            get => (ConfigurationDataRecaptchaLogin)base.configData;
            set => base.configData = value;
        }

        public Fuzer(IIndexerConfigurationService configService, Utils.Clients.WebClient w, Logger l, IProtectionService ps)
            : base(name: "Fuzer",
                   description: "Fuzer is a private torrent website with israeli torrents.",
                   link: "https://www.fuzer.me/",
                   configService: configService,
                   client: w,
                   logger: l,
                   p: ps,
                   configData: new ConfigurationDataRecaptchaLogin())
        {
            Encoding = Encoding.GetEncoding("windows-1255");
            Language = "he-il";
            Type = "private";
            TorznabCaps.SupportsImdbMovieSearch = true;
            TorznabCaps.Categories.Clear();

            // סרטים
            AddCategoryMapping(7, TorznabCatType.MoviesSD, "סרטים");
            AddCategoryMapping(9, TorznabCatType.MoviesHD, "סרטים HD");
            AddCategoryMapping(97, TorznabCatType.MoviesUHD, "סרטים UHD");
            AddCategoryMapping(58, TorznabCatType.MoviesDVD, "סרטים DVD-R");
            AddCategoryMapping(59, TorznabCatType.MoviesSD, "סרטי BDRIP-BRRip");
            AddCategoryMapping(60, TorznabCatType.MoviesSD, "סרטים ישראליים");
            AddCategoryMapping(61, TorznabCatType.MoviesHD, "סרטים ישראליים HD");
            AddCategoryMapping(83, TorznabCatType.MoviesOther, "סרטים מדובבים");

            // סדרות
            AddCategoryMapping(8, TorznabCatType.TVSD, "סדרות");
            AddCategoryMapping(10, TorznabCatType.TVHD, "סדרות HD");
            AddCategoryMapping(62, TorznabCatType.TVSD, "סדרות ישראליות");
            AddCategoryMapping(63, TorznabCatType.TVHD, "סדרות ישראליות HD");
            AddCategoryMapping(84, TorznabCatType.TVOTHER, "סדרות מדובבות");

            // מוזיקה
            AddCategoryMapping(14, TorznabCatType.Audio, "מוזיקה עולמית");
            AddCategoryMapping(66, TorznabCatType.Audio, "מוזיקה ישראלית");
            AddCategoryMapping(67, TorznabCatType.AudioMP3, "FLAC");
            AddCategoryMapping(68, TorznabCatType.Audio, "פסקולים");

            // משחקים
            AddCategoryMapping(11, TorznabCatType.PCGames, "משחקים PC");
            AddCategoryMapping(12, TorznabCatType.ConsoleOther, "משחקים PS");
            AddCategoryMapping(55, TorznabCatType.ConsoleXbox, "משחקים XBOX");
            AddCategoryMapping(56, TorznabCatType.ConsoleWii, "משחקים WII");
            AddCategoryMapping(57, TorznabCatType.PCPhoneOther, "משחקי קונסולות ניידות");

            // תוכנה
            AddCategoryMapping(13, TorznabCatType.PCPhoneAndroid, "אפליקציות לאנדרואיד");
            AddCategoryMapping(15, TorznabCatType.PC0day, "תוכנות PC");
            AddCategoryMapping(70, TorznabCatType.PCPhoneIOS, "אפליקציות לאייפון");
            AddCategoryMapping(71, TorznabCatType.PCMac, "תוכנות MAC");

            // שונות
            AddCategoryMapping(16, TorznabCatType.XXX, "למבוגרים בלבד");
            AddCategoryMapping(17, TorznabCatType.Other, "שונות");
            AddCategoryMapping(64, TorznabCatType.Other, "ספורט");
            AddCategoryMapping(65, TorznabCatType.Other, "אנימה");
            AddCategoryMapping(69, TorznabCatType.Books, "Ebooks");

            // FuzePacks
            AddCategoryMapping(72, TorznabCatType.Console, "משחקים");
            AddCategoryMapping(73, TorznabCatType.Movies, "סרטים");
            AddCategoryMapping(74, TorznabCatType.PC, "תוכנות");
            AddCategoryMapping(75, TorznabCatType.Audio, "שירים");
            AddCategoryMapping(76, TorznabCatType.TV, "סדרות");
        }

        public override async Task<ConfigurationData> GetConfigurationForSetup()
        {
            var loginPage = await RequestStringWithCookies(LoginUrl, string.Empty);
            var parser = new HtmlParser();
            var cq = parser.ParseDocument(loginPage.Content);
            var captcha = cq.QuerySelector(".g-recaptcha"); // invisible recaptcha
            if (captcha != null)
            {
                var result = configData;
                result.CookieHeader.Value = loginPage.Cookies;
                result.Captcha.SiteKey = captcha.GetAttribute("data-sitekey");
                result.Captcha.Version = "2";
                return result;
            }
            else
            {
                var result = new ConfigurationDataBasicLogin();
                result.SiteLink.Value = configData.SiteLink.Value;
                result.Instructions.Value = configData.Instructions.Value;
                result.Username.Value = configData.Username.Value;
                result.Password.Value = configData.Password.Value;
                result.CookieHeader.Value = loginPage.Cookies;
                return result;
            }
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            if (!string.IsNullOrWhiteSpace(configData.Captcha.Cookie))
            {
                CookieHeader = configData.Captcha.Cookie;
                try
                {
                    var results = await PerformQuery(new TorznabQuery());
                    if (!results.Any())
                        throw new Exception("Your cookie did not work");
                    IsConfigured = true;
                    SaveConfig();
                    return IndexerConfigurationStatus.Completed;
                }
                catch (Exception e)
                {
                    IsConfigured = false;
                    throw new Exception("Your cookie did not work: " + e.Message);
                }
            }

            var loginPage = await RequestStringWithCookies(LoginUrl, string.Empty);
            var pairs = new Dictionary<string, string>
            {
                {"vb_login_username", configData.Username.Value},
                {"vb_login_password", ""},
                {"securitytoken", "guest"},
                {"do", "login"},
                {"vb_login_md5password", StringUtil.Hash(configData.Password.Value).ToLower()},
                {"vb_login_md5password_utf", StringUtil.Hash(configData.Password.Value).ToLower()},
                {"cookieuser", "1"}
            };
            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, loginPage.Cookies, true, null, LoginUrl);
            await ConfigureIfOK(result.Cookies, result.Content?.Contains("images/loading.gif") == true,
                                () => throw new ExceptionWithConfigData("Couldn't login", configData));
            Thread.Sleep(2);
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var results = await PerformRegularQueryAsync(query);
            if (!results.Any() && !query.IsImdbQuery)
                return await PerformHebrewQueryAsync(query);
            return results;
        }

        private async Task<IEnumerable<ReleaseInfo>> PerformHebrewQueryAsync(TorznabQuery query)
        {
            var name = await GetHebNameAsync(query.SearchTerm);
            if (string.IsNullOrEmpty(name))
                return new List<ReleaseInfo>();
            return await PerformRegularQueryAsync(query, name);
        }

        private async Task<IEnumerable<ReleaseInfo>> PerformRegularQueryAsync(TorznabQuery query, string hebName = null)
        {
            var releases = new List<ReleaseInfo>();
            var searchUrl = SearchUrl;
            var searchString = query.GetQueryString();
            if (query.IsImdbQuery)
                searchString = query.ImdbID;
            if (hebName != null)
                searchString = hebName + " - עונה " + query.Season + " פרק " + query.Episode;
            searchUrl += "?";
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var strEncoded = WebUtilityHelpers.UrlEncode(searchString, Encoding);
                searchUrl += "&query=" + strEncoded + "&matchquery=any";
            }

            searchUrl = MapTorznabCapsToTrackers(query).Aggregate(searchUrl, (current, cat) => $"{current}&c[]={cat}");
            var data = await RequestStringWithCookiesAndRetry(searchUrl);
            try
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(data.Content);
                var rows = dom.QuerySelectorAll("tr.box_torrent");
                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();
                    var mainTitleLink = row.QuerySelector("div.main_title > a");
                    release.Title = mainTitleLink.GetAttribute("longtitle");
                    if (string.IsNullOrWhiteSpace(release.Title))
                        release.Title = mainTitleLink.TextContent;
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800; // 48 hours
                    release.Grabs = ParseUtil.CoerceLong(row.QuerySelector("td:nth-child(5)").TextContent.Replace(",", ""));
                    release.Seeders = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(6)").TextContent.Replace(",", ""));
                    release.Peers = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(7)").TextContent.Replace(",", "")) +
                                    release.Seeders;
                    var fullSize = row.QuerySelector("td:nth-child(4)").TextContent;
                    release.Size = ReleaseInfo.GetBytes(fullSize);
                    release.Comments = new Uri(SiteLink + row.QuerySelector("a.threadlink[href]").GetAttribute("href"));
                    release.Link = new Uri(SiteLink + row.QuerySelector("a:has(div.dlimg)").GetAttribute("href"));
                    release.Guid = release.Comments;
                    //some releases have invalid banner URLs, ignore the banners in this case
                    if (Uri.TryCreate(row.QuerySelector("a[imgsrc]").GetAttribute("imgsrc"),
                                      UriKind.Absolute, out var banner))
                        release.BannerUrl = banner;
                    var dateStringAll = row.QuerySelector("div.up_info2").ChildNodes.Last().TextContent;
                    var dateParts = dateStringAll.Split(' ');
                    var dateString = dateParts[dateParts.Length - 2] + " " + dateParts[dateParts.Length - 1];
                    release.PublishDate = DateTime.ParseExact(dateString, "dd/MM/yy HH:mm", CultureInfo.InvariantCulture);
                    var categoryLink = row.QuerySelector("a[href^=\"/browse.php?cat=\"]").GetAttribute("href");
                    var catid = ParseUtil.GetArgumentFromQueryString(categoryLink, "cat");
                    release.Category = MapTrackerCatToNewznab(catid);
                    if (row.QuerySelector("a[href^=\"?freeleech=1\"]") != null)
                        release.DownloadVolumeFactor = 0;
                    else
                        release.DownloadVolumeFactor = 1;
                    release.UploadVolumeFactor = 1;
                    var subTitle = row.QuerySelector("div.sub_title");
                    var imdbLink = subTitle.QuerySelector("span.imdb-inline > a");
                    if (imdbLink != null)
                        release.Imdb = ParseUtil.GetLongFromString(imdbLink.GetAttribute("href"));
                    release.Description = subTitle.FirstChild.TextContent;
                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(data.Content, ex);
            }

            return releases;
        }

        private async Task<string> GetHebNameAsync(string searchTerm)
        {
            var queryString = new NameValueCollection
            {
                {"searchseriesid", ""},
                {"tab", "listseries"},
                {"function", "Search"},
                {"string", searchTerm}, // eretz + nehedert
            };
            var site = new UriBuilder
            {
                Scheme = "http",
                Host = "thetvdb.com",
                Path = "index.php",
                Query = queryString.GetQueryString()
            };
            var results = await RequestStringWithCookies(site.ToString());
            var parser = new HtmlParser();
            var dom = parser.ParseDocument(results.Content);
            var rows = dom.QuerySelectorAll("#listtable > tbody > tr");
            foreach (var row in rows.Skip(1))
            {
                var link = row.QuerySelector("td:nth-child(1) > a");
                if (string.Equals(link.TextContent.Trim(), searchTerm.Trim(), StringComparison.CurrentCultureIgnoreCase))
                {
                    var address = link.GetAttribute("href");
                    if (string.IsNullOrEmpty(address))
                        continue;
                    var realDom = parser.ParseDocument(results.Content);
                    return realDom.QuerySelector("#content:nth-child(1) > h1").TextContent;
                }
            }

            return string.Empty;
        }
    }
}
