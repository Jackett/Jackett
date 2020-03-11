using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    public class TorrentHeaven : BaseWebIndexer
    {
        public override string[] LegacySiteLinks { get; protected set; } =
        {
            "https://torrentheaven.myfqdn.info/"
        };

        private string IndexUrl => SiteLink + "index.php";

        private string LoginCompleteUrl =>
            SiteLink + "index.php?strWebValue=account&strWebAction=login_complete&ancestry=verify";

        private new ConfigurationDataCaptchaLogin configData
        {
            get => (ConfigurationDataCaptchaLogin)base.configData;
            set => base.configData = value;
        }

        public TorrentHeaven(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps) :
            base(
                name: "TorrentHeaven",
                description: "A German general tracker.",
                link: "https://newheaven.nl/",
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
            // incomplete CA chain
            wc.AddTrustedCertificate(new Uri(SiteLink).Host, "cbf23ac75b07255ad7548a87567a839d23f31576"); 
            AddCategoryMapping(1, TorznabCatType.PCGames, "GAMES/PC");
            AddCategoryMapping(3, TorznabCatType.Console, "GAMES/Sonstige");
            AddCategoryMapping(59, TorznabCatType.ConsolePS4, "GAMES/PlayStation");
            AddCategoryMapping(60, TorznabCatType.ConsolePSP, "GAMES/PSP");
            AddCategoryMapping(63, TorznabCatType.ConsoleWii, "GAMES/Wii");
            AddCategoryMapping(67, TorznabCatType.ConsoleXbox360, "GAMES/XBOX 360");
            AddCategoryMapping(68, TorznabCatType.PCPhoneOther, "GAMES/PDA / Handy");
            AddCategoryMapping(72, TorznabCatType.ConsoleNDS, "GAMES/NDS");
            AddCategoryMapping(7, TorznabCatType.MoviesDVD, "MOVIES/DVD");
            AddCategoryMapping(8, TorznabCatType.MoviesSD, "MOVIES/SD");
            AddCategoryMapping(37, TorznabCatType.MoviesDVD, "MOVIES/DVD Spezial");
            AddCategoryMapping(41, TorznabCatType.MoviesForeign, "MOVIES/International");
            AddCategoryMapping(101, TorznabCatType.MoviesHD, "MOVIES/720p");
            AddCategoryMapping(102, TorznabCatType.MoviesHD, "MOVIES/1080p");
            AddCategoryMapping(103, TorznabCatType.MoviesHD, "MOVIES/AVCHD");
            AddCategoryMapping(104, TorznabCatType.MoviesBluRay, "MOVIES/Bluray");
            AddCategoryMapping(106, TorznabCatType.Movies3D, "MOVIES/3D");
            AddCategoryMapping(109, TorznabCatType.MoviesUHD, "MOVIES/4K");
            AddCategoryMapping(14, TorznabCatType.Audio, "AUDIO/Musik");
            AddCategoryMapping(15, TorznabCatType.AudioAudiobook, "AUDIO/Hörbücher");
            AddCategoryMapping(16, TorznabCatType.AudioAudiobook, "AUDIO/Hörspiele");
            AddCategoryMapping(36, TorznabCatType.AudioLossless, "AUDIO/Flac");
            AddCategoryMapping(42, TorznabCatType.AudioOther, "AUDIO/Soundtracks");
            AddCategoryMapping(58, TorznabCatType.AudioVideo, "AUDIO/Musikvideos");
            AddCategoryMapping(18, TorznabCatType.TVSD, "TV/Serien SD");
            AddCategoryMapping(19, TorznabCatType.TVHD, "TV/Serien HD 720p");
            AddCategoryMapping(20, TorznabCatType.TVHD, "TV/Serien HD 1080p");
            AddCategoryMapping(49, TorznabCatType.TVSD, "TV/Serien DVD");
            AddCategoryMapping(51, TorznabCatType.TVDocumentary, "TV/Doku SD");
            AddCategoryMapping(52, TorznabCatType.TVDocumentary, "TV/Doku HD");
            AddCategoryMapping(53, TorznabCatType.TV, "TV/Serien Complete Packs");
            AddCategoryMapping(54, TorznabCatType.TVSport, "TV/Sport");
            AddCategoryMapping(66, TorznabCatType.TVFOREIGN, "TV/International");
            AddCategoryMapping(22, TorznabCatType.Books, "MISC/EBooks");
            AddCategoryMapping(24, TorznabCatType.Other, "MISC/Sonstiges");
            AddCategoryMapping(25, TorznabCatType.Other, "MISC/Tonspuren");
            AddCategoryMapping(108, TorznabCatType.TVAnime, "MISC/Anime");
            AddCategoryMapping(28, TorznabCatType.PC, "APPLICATIONS/PC");
            AddCategoryMapping(29, TorznabCatType.PCPhoneOther, "APPLICATIONS/Mobile");
            AddCategoryMapping(30, TorznabCatType.PC, "APPLICATIONS/Sonstige");
            AddCategoryMapping(70, TorznabCatType.PC, "APPLICATIONS/Linux");
            AddCategoryMapping(71, TorznabCatType.PCMac, "APPLICATIONS/Mac");
        }

        public override async Task<ConfigurationData> GetConfigurationForSetup()
        {
            var loginPage = await RequestStringWithCookies(IndexUrl, string.Empty);
            var parser = new HtmlParser();
            var dom = parser.ParseDocument(loginPage.Content);
            var qCaptchaImg = dom.QuerySelector("td.tablea > img");
            if (qCaptchaImg != null)
            {
                var captchaUrl = SiteLink + qCaptchaImg.GetAttribute("src");
                var captchaImage = await RequestBytesWithCookies(captchaUrl, loginPage.Cookies);
                configData.CaptchaImage.Value = captchaImage.Content;
            }
            else
                configData.CaptchaImage.Value = Array.Empty<byte>();

            configData.CaptchaCookie.Value = loginPage.Cookies;
            return configData;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string>
            {
                {"strWebAction", "login"},
                {"strWebValue", "account"},
                {"jsenabled", "1"},
                {"screenwidth", "2560"},
                {"username", configData.Username.Value},
                {"password", configData.Password.Value}
            };
            if (!string.IsNullOrWhiteSpace(configData.CaptchaText.Value))
                pairs.Add("proofcode", configData.CaptchaText.Value);
            var result = await RequestLoginAndFollowRedirect(IndexUrl, pairs, configData.CaptchaCookie.Value, true, referer: IndexUrl, accumulateCookies: true);
            if (result.Content == null || (!result.Content.Contains("login_complete") &&
                                           !result.Content.Contains("index.php?strWebValue=account&strWebAction=logout")))
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(result.Content);
                var errorMessageEl = dom.QuerySelector("table > tbody > tr > td[valign=top][width=100%]");
                var errorMessage = errorMessageEl != null ? errorMessageEl.InnerHtml : result.Content;
                throw new ExceptionWithConfigData(errorMessage, configData);
            }

            var result2 = await RequestStringWithCookies(LoginCompleteUrl, result.Cookies);
            await ConfigureIfOK(
                result2.Cookies, result2.Cookies?.Contains("pass") == true,
                () => throw new ExceptionWithConfigData("Didn't get a user/pass cookie", configData));
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var startTransition = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(
                new DateTime(1, 1, 1, 3, 0, 0), 3, 5, DayOfWeek.Sunday);
            var endTransition = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(
                new DateTime(1, 1, 1, 4, 0, 0), 10, 5, DayOfWeek.Sunday);
            var delta = new TimeSpan(1, 0, 0);
            var adjustment = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
                new DateTime(1999, 10, 1), DateTime.MaxValue.Date, delta, startTransition,
                endTransition);
            TimeZoneInfo.AdjustmentRule[] adjustments =
            {
                adjustment
            };
            var germanyTz = TimeZoneInfo.CreateCustomTimeZone(
                "W. Europe Standard Time", new TimeSpan(1, 0, 0), "(GMT+01:00) W. Europe Standard Time",
                "W. Europe Standard Time", "W. Europe DST Time", adjustments);
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();
            var searchUrl = IndexUrl;
            var queryCollection = new NameValueCollection
            {
                {"strWebValue", "torrent"},
                {"strWebAction", "search"},
                {"sort", "torrent_added"},
                {"by", "d"},
                {"type", "0"},
                {"do_search", "suchen"},
                {"time", "0"},
                {"details", "title"}
            };
            if (!string.IsNullOrWhiteSpace(searchString))
                queryCollection.Add("searchstring", searchString);
            foreach (var cat in MapTorznabCapsToTrackers(query))
                queryCollection.Add("dirs" + cat, "1");
            searchUrl += "?" + queryCollection.GetQueryString();
            var response = await RequestStringWithCookies(searchUrl);
            var titleRegexp = new Regex(@"^return buildTable\('(.*?)',\s+");
            try
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(response.Content);
                var rows = dom.QuerySelectorAll("table.torrenttable > tbody > tr");
                foreach (var row in rows.Skip(1))
                {
                    var release = new ReleaseInfo();
                    release.MinimumRatio = 0.8;
                    release.MinimumSeedTime = 0;
                    var qDetailsLink = row.QuerySelector("a[href^=\"index.php?strWebValue=torrent&strWebAction=details\"]");
                    release.Title = titleRegexp.Match(qDetailsLink.GetAttribute("onmouseover")).Groups[1].Value;
                    var qCatLink = row.QuerySelector("a[href^=\"index.php?strWebValue=torrent&strWebAction=search&dir=\"]");
                    var qDlLink = row.QuerySelector("a[href^=\"index.php?strWebValue=torrent&strWebAction=download&id=\"]");
                    var qSeeders = row.QuerySelectorAll("td.column1")[3];
                    var qLeechers = row.QuerySelectorAll("td.column2")[3];
                    var qDateStr = row.QuerySelector("font:has(a)");
                    var qSize = row.QuerySelector("td.column2[align=center]");
                    var catStr = qCatLink.GetAttribute("href").Split('=')[3].Split('#')[0];
                    release.Category = MapTrackerCatToNewznab(catStr);
                    release.Link = new Uri(SiteLink + qDlLink.GetAttribute("href"));
                    release.Comments = new Uri(SiteLink + qDetailsLink.GetAttribute("href"));
                    release.Guid = release.Link;
                    var sizeStr = qSize.TextContent;
                    release.Size = ReleaseInfo.GetBytes(sizeStr);
                    release.Seeders = ParseUtil.CoerceInt(qSeeders.TextContent);
                    release.Peers = ParseUtil.CoerceInt(qLeechers.TextContent) + release.Seeders;
                    var dateStr = qDateStr.TextContent.Trim();
                    var dateStrParts = dateStr.Split();
                    var dateGerman = dateStrParts[0] switch
                    {
                        "Heute" => (DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified) + TimeSpan.Parse(dateStrParts[1])),
                        "Gestern" => (DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified) + TimeSpan.Parse(dateStrParts[1])
                                      - TimeSpan.FromDays(1)),
                        _ => DateTime.SpecifyKind(                            DateTime.ParseExact(
                                dateStrParts[0] + dateStrParts[1], "dd.MM.yyyyHH:mm", CultureInfo.InvariantCulture),
                            DateTimeKind.Unspecified)
                    };

                    release.PublishDate = TimeZoneInfo.ConvertTime(dateGerman, germanyTz, TimeZoneInfo.Local);
                    var grabs = row.QuerySelector("td:nth-child(7)").TextContent;
                    release.Grabs = ParseUtil.CoerceInt(grabs);
                    if (row.QuerySelector("img[src=\"themes/images/freeleech.png\"]") != null
                        || row.QuerySelector("img[src=\"themes/images/onlyup.png\"]") != null)
                        release.DownloadVolumeFactor = 0;
                    else if (row.QuerySelector("img[src=\"themes/images/DL50.png\"]") != null)
                        release.DownloadVolumeFactor = 0.5;
                    else
                        release.DownloadVolumeFactor = 1;
                    release.UploadVolumeFactor = 1;
                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.Content, ex);
            }

            return releases;
        }
    }
}
