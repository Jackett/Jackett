using Jackett.Utils.Clients;
using NLog;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Models;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using CsQuery;
using System;
using System.Globalization;
using Jackett.Models.IndexerConfig;
using System.Collections.Specialized;
using System.Text;

namespace Jackett.Indexers
{
    public class BestFriends : BaseWebIndexer
    {
        string LoginUrl { get { return SiteLink + "login.php"; } }
        string TakeLoginUrl { get { return SiteLink + "takelogin.php"; } }
        string BrowseUrl { get { return SiteLink + "browse.php"; } }

        new ConfigurationDataCaptchaLogin configData
        {
            get { return (ConfigurationDataCaptchaLogin)base.configData; }
            set { base.configData = value; }
        }

        public BestFriends(IIndexerConfigurationService configService, IWebClient wc, Logger l, IProtectionService ps)
            : base(name: "Best Friends",
                   description: "A German general tracker.",
                   link: "http://bf.mine.nu/",
                   caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   configData: new ConfigurationDataCaptchaLogin())
        {
            Encoding = Encoding.GetEncoding("iso-8859-1");
            Language = "de-de";
            Type = "private";

            AddCategoryMapping(18, TorznabCatType.TVAnime); // Anime
            AddCategoryMapping(8,  TorznabCatType.PCMac); // Appz MAC
            AddCategoryMapping(9,  TorznabCatType.PC); // Appz other
            AddCategoryMapping(7,  TorznabCatType.PC); // Appz Windows
            AddCategoryMapping(23, TorznabCatType.TVDocumentary); // Dokumentationen
            AddCategoryMapping(32, TorznabCatType.Movies3D); // DVD 3D
            AddCategoryMapping(15, TorznabCatType.Books); // eBooks
            AddCategoryMapping(12, TorznabCatType.PCGames); // Games PC
            AddCategoryMapping(37, TorznabCatType.PCPhoneOther); // Handy_Mobile 
            AddCategoryMapping(24, TorznabCatType.TVHD); // HDTV
            AddCategoryMapping(22, TorznabCatType.AudioAudiobook); // Hörbücher
            AddCategoryMapping(1,  TorznabCatType.MoviesHD); // Movies 1080p/1080i
            AddCategoryMapping(31, TorznabCatType.Movies3D); // Movies 3D
            AddCategoryMapping(2,  TorznabCatType.MoviesHD); // Movies 720p/720i
            AddCategoryMapping(21, TorznabCatType.MoviesBluRay); // Movies BluRay
            AddCategoryMapping(5,  TorznabCatType.MoviesDVD); // Movies DVD/HDDVD
            AddCategoryMapping(6,  TorznabCatType.MoviesSD); // Movies M/SVCD/Other
            AddCategoryMapping(4,  TorznabCatType.MoviesSD); // Movies XVID/DIVX/h.264
            AddCategoryMapping(10, TorznabCatType.Audio); // Music
            AddCategoryMapping(25, TorznabCatType.AudioVideo); // Musikvideo
            AddCategoryMapping(29, TorznabCatType.ConsoleNDS); // Nintendo DS
            AddCategoryMapping(16, TorznabCatType.Other); // other
            AddCategoryMapping(13, TorznabCatType.ConsolePS4); // Playstation
            AddCategoryMapping(28, TorznabCatType.TVHD); // Serien HD
            AddCategoryMapping(11, TorznabCatType.TVSD); // Serien XviD
            AddCategoryMapping(33, TorznabCatType.Other); // Specials
            AddCategoryMapping(30, TorznabCatType.TVSport); // Sport
            AddCategoryMapping(19, TorznabCatType.TVOTHER); // TVRip
            AddCategoryMapping(38, TorznabCatType.TVDocumentary); // US Dokus
            AddCategoryMapping(20, TorznabCatType.MoviesForeign); // US Movies
            AddCategoryMapping(14, TorznabCatType.TVFOREIGN); // US Serien
            AddCategoryMapping(36, TorznabCatType.Other); // Wallpaper 
            AddCategoryMapping(26, TorznabCatType.ConsoleWii); // Wii
            AddCategoryMapping(27, TorznabCatType.ConsoleXbox360); // Xbox 360
            AddCategoryMapping(3,  TorznabCatType.XXX); // XXX
        }

        public override async Task<ConfigurationData> GetConfigurationForSetup()
        {
            var loginPage = await RequestStringWithCookies(LoginUrl, string.Empty);
            CQ dom = loginPage.Content;
            CQ qCaptchaImg = dom.Find("td.tablea > img").First();

            var CaptchaUrl = SiteLink + qCaptchaImg.Attr("src");
            var captchaImage = await RequestBytesWithCookies(CaptchaUrl, loginPage.Cookies);
            configData.CaptchaImage.Value = captchaImage.Content;
            configData.CaptchaCookie.Value = loginPage.Cookies;
            return configData;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var pairs1 = new Dictionary<string, string>
            {
                { "proofcode", configData.CaptchaText.Value }
            };
            var cookies = configData.CaptchaCookie.Value;
            var result1 = await RequestLoginAndFollowRedirect(LoginUrl, pairs1, cookies, true, null, LoginUrl, true);
            if(result1.Content == null || !result1.Content.Contains("takelogin.php"))
            {
                CQ dom = result1.Content;
                var errorMessage = dom["#login_error"].Text().Trim();
                errorMessage = result1.Content;
                throw new ExceptionWithConfigData(errorMessage, configData);
            }

            var pairs2 = new Dictionary<string, string>
            {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value }
            };

            var result = await RequestLoginAndFollowRedirect(TakeLoginUrl, pairs2, result1.Cookies, true, null, LoginUrl, true);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("logout.php"), () =>
                {
                    CQ dom = result.Content;
                    var errorMessage = dom["#login_error"].Text().Trim();
                    errorMessage = result.Content;
                    throw new ExceptionWithConfigData(errorMessage, configData);
                });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            TimeZoneInfo.TransitionTime startTransition = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 3, 0, 0), 3, 5, DayOfWeek.Sunday);
            TimeZoneInfo.TransitionTime endTransition = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 4, 0, 0), 10, 5, DayOfWeek.Sunday);
            TimeSpan delta = new TimeSpan(1, 0, 0);
            TimeZoneInfo.AdjustmentRule adjustment = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(1999, 10, 1), DateTime.MaxValue.Date, delta, startTransition, endTransition);
            TimeZoneInfo.AdjustmentRule[] adjustments = { adjustment };
            TimeZoneInfo germanyTz = TimeZoneInfo.CreateCustomTimeZone("W. Europe Standard Time", new TimeSpan(1, 0, 0), "(GMT+01:00) W. Europe Standard Time", "W. Europe Standard Time", "W. Europe DST Time", adjustments);

            var releases = new List<ReleaseInfo>();
            
            var searchString = query.GetQueryString();
            var searchUrl = BrowseUrl;
            var queryCollection = new NameValueCollection();
            queryCollection.Add("showsearch", "1");
            queryCollection.Add("incldead", "1");
            queryCollection.Add("blah", "0");
            queryCollection.Add("orderby", "added");
            queryCollection.Add("sort", "desc");

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                queryCollection.Add("search", searchString);
            }

            foreach (var cat in MapTorznabCapsToTrackers(query))
            {
                queryCollection.Add("c" + cat, "1");
            }
            searchUrl += "?" + queryCollection.GetQueryString();

            var response = await RequestStringWithCookiesAndRetry(searchUrl, null, BrowseUrl);
            var results = response.Content;
            try
            {
                CQ dom = results;
                var rows = dom["table.tableinborder > tbody > tr:has(td.tableb)"];

                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();
                    release.MinimumRatio = 0.75;
                    release.MinimumSeedTime = 0;
                    var qRow = row.Cq();

                    var qDetailsLink = qRow.Find("a[href^=details.php?id=]").First();
                    release.Title = qDetailsLink.Attr("title");

                    if (!query.MatchQueryStringAND(release.Title))
                        continue;

                    var qCatLink = qRow.Find("a[href^=browse.php?cat=]").First();

                    // use negative indexes as if a user has "Wartezeit" there's an extra column after the title
                    var qSeeders = qRow.Find("td:nth-last-child(4)");
                    var qLeechers = qRow.Find("td:nth-last-child(3)");
                    var qDateStr = qRow.Find("td:nth-last-child(7)");
                    var qSize = qRow.Find("td:nth-last-child(6)");

                    var torrentId = qDetailsLink.Attr("href").Replace("&hit=1", "").Split('=')[1];

                    var catStr = qCatLink.Attr("href").Split('=')[1];
                    release.Category = MapTrackerCatToNewznab(catStr);

                    release.Link = new Uri(SiteLink + "download.php?torrent="+torrentId);
                    release.Comments = new Uri(SiteLink + qDetailsLink.Attr("href"));
                    release.Guid = release.Link;

                    var sizeStr = qSize.Text();
                    release.Size = ReleaseInfo.GetBytes(sizeStr.Replace(",", "."));

                    release.Seeders = ParseUtil.CoerceInt(qSeeders.Text());
                    release.Peers = ParseUtil.CoerceInt(qLeechers.Text()) + release.Seeders;

                    var dateStr = qDateStr.Text();
                    var dateGerman = DateTime.SpecifyKind(DateTime.ParseExact(dateStr, "dd.MM.yyyyHH:mm:ss", CultureInfo.InvariantCulture), DateTimeKind.Unspecified);
                    DateTime pubDateUtc = TimeZoneInfo.ConvertTimeToUtc(dateGerman, germanyTz);
                    release.PublishDate = pubDateUtc;

                    var files = qRow.Find("td:nth-last-child(9)").Text();
                    release.Files = ParseUtil.CoerceInt(files);

                    var grabs = qRow.Find("td:nth-last-child(5)").Text();
                    release.Grabs = ParseUtil.CoerceInt(grabs);

                    if (qRow.Find("font[color=\"red\"]:contains(OnlyUp)").Length >= 1)
                        release.DownloadVolumeFactor = 0;
                    else
                        release.DownloadVolumeFactor = 1;

                    release.UploadVolumeFactor = 1;

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results, ex);
            }

            return releases;
        }
    }
}

