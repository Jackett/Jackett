using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CsQuery;
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
        private string SearchUrl { get { return SiteLink + "browse.php"; } }
        private string LoginUrl { get { return SiteLink + "login.php"; } }
        private const int MAXPAGES = 3;

        private new ConfigurationDataBasicLogin configData
        {
            get { return (ConfigurationDataBasicLogin)base.configData; }
            set { base.configData = value; }
        }

        public Fuzer(IIndexerConfigurationService configService, Utils.Clients.WebClient w, Logger l, IProtectionService ps)
            : base(name: "Fuzer",
                description: "Fuzer is a private torrent website with israeli torrents.",
                link: "https://fuzer.me/",
                configService: configService,
                client: w,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLogin())
        {
            Encoding = Encoding.GetEncoding("windows-1255");
            Language = "he-il";
            Type = "private";
            TorznabCaps.SupportsImdbSearch = true;
            TorznabCaps.Categories.Clear();

            // סרטים
            AddCategoryMapping(7, TorznabCatType.MoviesSD, "סרטים");
            AddCategoryMapping(9, TorznabCatType.MoviesHD, "סרטים HD");
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

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var loginPage = await RequestStringWithCookies(LoginUrl, string.Empty);

            var pairs = new Dictionary<string, string> {
                { "vb_login_username", configData.Username.Value },
                { "vb_login_password", "" },
                { "securitytoken", "guest" },
                { "do","login"},
                { "vb_login_md5password", StringUtil.Hash(configData.Password.Value).ToLower()},
                { "vb_login_md5password_utf", StringUtil.Hash(configData.Password.Value).ToLower()},
                { "cookieuser", "1" }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, loginPage.Cookies, true, null, LoginUrl);

            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("images/loading.gif"), () =>
            {
                var errorMessage = "Couldn't login";
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            Thread.Sleep(2);
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var results = await performRegularQuery(query);
            if (results.Count() == 0 && !query.IsImdbQuery)
            {
                return await performHebrewQuery(query);
            }

            return results;
        }

        private async Task<IEnumerable<ReleaseInfo>> performHebrewQuery(TorznabQuery query)
        {
            var name = await getHebName(query.SearchTerm);

            if (string.IsNullOrEmpty(name))
            {
                return new List<ReleaseInfo>();
            }
            else
            {
                return await performRegularQuery(query, name);
            }
        }

        private async Task<IEnumerable<ReleaseInfo>> performRegularQuery(TorznabQuery query, string hebName = null)
        {
            var releases = new List<ReleaseInfo>();
            var searchurls = new List<string>();
            var searchUrl = SearchUrl;
            var queryCollection = new NameValueCollection();
            var searchString = query.GetQueryString();
            if (query.IsImdbQuery)
                searchString = query.ImdbID;

            if (hebName != null)
            {
                searchString = hebName + " - עונה " + query.Season + " פרק " + query.Episode;
            }
            searchUrl += "?";
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var strEncoded = WebUtilityHelpers.UrlEncode(searchString, Encoding);
                searchUrl += "&query=" + strEncoded + "&matchquery=any";
            }

            foreach (var cat in MapTorznabCapsToTrackers(query))
            {
                searchUrl += "&c[]=" + cat;
            }

            var data = await RequestStringWithCookiesAndRetry(searchUrl);
            try
            {
                CQ dom = data.Content;
                var rows = dom["tr.box_torrent"];
                foreach (var row in rows)
                {
                    CQ qRow = row.Cq();

                    var release = new ReleaseInfo();
                    var main_title_link = qRow.Find("div.main_title > a");
                    release.Title = main_title_link.Attr("longtitle");
                    if (release.Title.IsNullOrEmptyOrWhitespace())
                        release.Title = main_title_link.Text();

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;

                    int seeders, peers;
                    if (ParseUtil.TryCoerceInt(qRow.Find("td:nth-child(7) > div").Text(), out seeders))
                    {
                        release.Seeders = seeders;
                        if (ParseUtil.TryCoerceInt(qRow.Find("td:nth-child(8) > div").Text(), out peers))
                        {
                            release.Peers = peers + release.Seeders;
                        }
                    }
                    release.Grabs = ParseUtil.CoerceLong(qRow.Find("td:nth-child(5)").Text().Replace(",", ""));
                    release.Seeders = ParseUtil.CoerceInt(qRow.Find("td:nth-child(6)").Text().Replace(",", ""));
                    release.Peers = ParseUtil.CoerceInt(qRow.Find("td:nth-child(7)").Text().Replace(",", "")) + release.Seeders;
                    string fullSize = qRow.Find("td:nth-child(4)").Text();
                    release.Size = ReleaseInfo.GetBytes(fullSize);

                    release.Comments = new Uri(SiteLink + qRow.Find("a.threadlink[href]").Attr("href"));
                    release.Link = new Uri(SiteLink + qRow.Find("a:has(div.dlimg)").Attr("href"));
                    release.Guid = release.Comments;
                    try
                    {
                        release.BannerUrl = new Uri(qRow.Find("a[imgsrc]").Attr("imgsrc"));
                    }
                    catch (Exception)
                    {
                        // do nothing, some releases have invalid banner URLs, ignore the banners in this case
                    }

                    var dateStringAll = qRow.Find("div.up_info2")[0].ChildNodes.Last().ToString();
                    var dateParts = dateStringAll.Split(' ');
                    string dateString = dateParts[dateParts.Length - 2] + " " + dateParts[dateParts.Length - 1];
                    release.PublishDate = DateTime.ParseExact(dateString, "dd/MM/yy HH:mm", CultureInfo.InvariantCulture);

                    string categoryLink = qRow.Find("a[href^=\"/browse.php?cat=\"]").Attr("href");
                    var catid = ParseUtil.GetArgumentFromQueryString(categoryLink, "cat");
                    release.Category = MapTrackerCatToNewznab(catid);

                    if (qRow.Find("a[href^=\"?freeleech=1\"]").Length >= 1)
                        release.DownloadVolumeFactor = 0;
                    else
                        release.DownloadVolumeFactor = 1;

                    release.UploadVolumeFactor = 1;

                    var sub_title = qRow.Find("div.sub_title");
                    var imdb_link = sub_title.Find("span.imdb-inline > a");
                    release.Imdb = ParseUtil.GetLongFromString(imdb_link.Attr("href"));
                    sub_title.Find("span.imdb-inline").Remove();
                    release.Description = sub_title.Text();

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(data.Content, ex);
            }

            return releases;
        }

        private async Task<string> getHebName(string searchTerm)
        {
            const string site = "http://thetvdb.com";
            var url = site + "/index.php?searchseriesid=&tab=listseries&function=Search&";
            url += "string=" + searchTerm; // eretz + nehedert

            var results = await RequestStringWithCookies(url);

            CQ dom = results.Content;

            int rowCount = 0;
            var rows = dom["#listtable > tbody > tr"];

            foreach (var row in rows)
            {
                if (rowCount < 1)
                {
                    rowCount++;
                    continue;
                }

                CQ qRow = row.Cq();
                CQ link = qRow.Find("td:nth-child(1) > a");
                if (link.Text().Trim().ToLower() == searchTerm.Trim().ToLower())
                {
                    var address = link.Attr("href");
                    if (string.IsNullOrEmpty(address)) { continue; }

                    var realAddress = site + address.Replace("lid=7", "lid=24");
                    var realData = await RequestStringWithCookies(realAddress);

                    CQ realDom = realData.Content;
                    return realDom["#content:nth-child(1) > h1"].Text();
                }
            }

            return string.Empty;
        }
    }
}
