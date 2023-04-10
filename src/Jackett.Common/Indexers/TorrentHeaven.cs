using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
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
    [ExcludeFromCodeCoverage]
    public class TorrentHeaven : IndexerBase
    {
        public override string Id => "torrentheaven";
        public override string Name => "TorrentHeaven";
        public override string Description => "A German general tracker.";
        public override string SiteLink { get; protected set; } = "https://newheaven.nl/";
        public override string[] LegacySiteLinks => new[]
        {
            "https://torrentheaven.myfqdn.info/"
        };
        public override Encoding Encoding => Encoding.GetEncoding("iso-8859-1");
        public override string Language => "de-DE";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private new ConfigurationDataCaptchaLogin configData => (ConfigurationDataCaptchaLogin)base.configData;

        private string IndexUrl => SiteLink + "index.php";
        private string LoginCompleteUrl => SiteLink + "index.php?strWebValue=account&strWebAction=login_complete&ancestry=verify";

        public TorrentHeaven(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps, ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataCaptchaLogin())
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

            caps.Categories.AddCategoryMapping(1, TorznabCatType.PCGames, "GAMES/PC");
            caps.Categories.AddCategoryMapping(3, TorznabCatType.Console, "GAMES/Sonstige");
            caps.Categories.AddCategoryMapping(59, TorznabCatType.ConsolePS4, "GAMES/PlayStation");
            caps.Categories.AddCategoryMapping(60, TorznabCatType.ConsolePSP, "GAMES/PSP");
            caps.Categories.AddCategoryMapping(63, TorznabCatType.ConsoleWii, "GAMES/Wii");
            caps.Categories.AddCategoryMapping(67, TorznabCatType.ConsoleXBox360, "GAMES/XBOX 360");
            caps.Categories.AddCategoryMapping(68, TorznabCatType.PCMobileOther, "GAMES/PDA / Handy");
            caps.Categories.AddCategoryMapping(72, TorznabCatType.ConsoleNDS, "GAMES/NDS");
            caps.Categories.AddCategoryMapping(7, TorznabCatType.MoviesDVD, "MOVIES/DVD");
            caps.Categories.AddCategoryMapping(8, TorznabCatType.MoviesSD, "MOVIES/SD");
            caps.Categories.AddCategoryMapping(37, TorznabCatType.MoviesDVD, "MOVIES/DVD Spezial");
            caps.Categories.AddCategoryMapping(41, TorznabCatType.MoviesForeign, "MOVIES/International");
            caps.Categories.AddCategoryMapping(101, TorznabCatType.MoviesHD, "MOVIES/720p");
            caps.Categories.AddCategoryMapping(102, TorznabCatType.MoviesHD, "MOVIES/1080p");
            caps.Categories.AddCategoryMapping(103, TorznabCatType.MoviesHD, "MOVIES/AVCHD");
            caps.Categories.AddCategoryMapping(104, TorznabCatType.MoviesBluRay, "MOVIES/Bluray");
            caps.Categories.AddCategoryMapping(106, TorznabCatType.Movies3D, "MOVIES/3D");
            caps.Categories.AddCategoryMapping(109, TorznabCatType.MoviesUHD, "MOVIES/4K");
            caps.Categories.AddCategoryMapping(14, TorznabCatType.Audio, "AUDIO/Musik");
            caps.Categories.AddCategoryMapping(15, TorznabCatType.AudioAudiobook, "AUDIO/Hörbücher");
            caps.Categories.AddCategoryMapping(16, TorznabCatType.AudioAudiobook, "AUDIO/Hörspiele");
            caps.Categories.AddCategoryMapping(36, TorznabCatType.AudioLossless, "AUDIO/Flac");
            caps.Categories.AddCategoryMapping(42, TorznabCatType.AudioOther, "AUDIO/Soundtracks");
            caps.Categories.AddCategoryMapping(58, TorznabCatType.AudioVideo, "AUDIO/Musikvideos");
            caps.Categories.AddCategoryMapping(18, TorznabCatType.TVSD, "TV/Serien SD");
            caps.Categories.AddCategoryMapping(19, TorznabCatType.TVHD, "TV/Serien HD 720p");
            caps.Categories.AddCategoryMapping(20, TorznabCatType.TVHD, "TV/Serien HD 1080p");
            caps.Categories.AddCategoryMapping(49, TorznabCatType.TVSD, "TV/Serien DVD");
            caps.Categories.AddCategoryMapping(51, TorznabCatType.TVDocumentary, "TV/Doku SD");
            caps.Categories.AddCategoryMapping(52, TorznabCatType.TVDocumentary, "TV/Doku HD");
            caps.Categories.AddCategoryMapping(53, TorznabCatType.TV, "TV/Serien Complete Packs");
            caps.Categories.AddCategoryMapping(54, TorznabCatType.TVSport, "TV/Sport");
            caps.Categories.AddCategoryMapping(66, TorznabCatType.TVForeign, "TV/International");
            caps.Categories.AddCategoryMapping(110, TorznabCatType.TVUHD, "TV/Serien - 4K");
            caps.Categories.AddCategoryMapping(22, TorznabCatType.Books, "MISC/EBooks");
            caps.Categories.AddCategoryMapping(24, TorznabCatType.Other, "MISC/Sonstiges");
            caps.Categories.AddCategoryMapping(25, TorznabCatType.Other, "MISC/Tonspuren");
            caps.Categories.AddCategoryMapping(108, TorznabCatType.TVAnime, "MISC/Anime");
            caps.Categories.AddCategoryMapping(28, TorznabCatType.PC, "APPLICATIONS/PC");
            caps.Categories.AddCategoryMapping(29, TorznabCatType.PCMobileOther, "APPLICATIONS/Mobile");
            caps.Categories.AddCategoryMapping(30, TorznabCatType.PC, "APPLICATIONS/Sonstige");
            caps.Categories.AddCategoryMapping(70, TorznabCatType.PC, "APPLICATIONS/Linux");
            caps.Categories.AddCategoryMapping(71, TorznabCatType.PCMac, "APPLICATIONS/Mac");

            return caps;
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
            var result = await RequestLoginAndFollowRedirect(
                IndexUrl, pairs, configData.CaptchaCookie.Value, true, referer: IndexUrl, accumulateCookies: true);
            if (result.ContentString == null || (!result.ContentString.Contains("login_complete") &&
                                           !result.ContentString.Contains("index.php?strWebValue=account&strWebAction=logout")))
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(result.ContentString);
                var errorMessageEl = dom.QuerySelector("table > tbody > tr > td[valign=top][width=100%]");
                var errorMessage = errorMessageEl != null ? errorMessageEl.InnerHtml : result.ContentString;
                throw new ExceptionWithConfigData(errorMessage, configData);
            }

            var result2 = await RequestWithCookiesAsync(LoginCompleteUrl, result.Cookies);
            await ConfigureIfOK(
                result2.Cookies, result2.Cookies?.Contains("pass") == true,
                () => throw new ExceptionWithConfigData("Didn't get a user/pass cookie", configData));
            return IndexerConfigurationStatus.RequiresTesting;
        }

        public override async Task<ConfigurationData> GetConfigurationForSetup()
        {
            var loginPage = await RequestWithCookiesAsync(IndexUrl, string.Empty);
            var parser = new HtmlParser();
            var dom = parser.ParseDocument(loginPage.ContentString);
            var qCaptchaImg = dom.QuerySelector("td.tablea > img");
            if (qCaptchaImg != null)
            {
                var captchaUrl = SiteLink + qCaptchaImg.GetAttribute("src");
                var captchaImage = await RequestWithCookiesAsync(captchaUrl, loginPage.Cookies);
                configData.CaptchaImage.Value = captchaImage.ContentBytes;
            }
            else
                configData.CaptchaImage.Value = Array.Empty<byte>();

            configData.CaptchaCookie.Value = loginPage.Cookies;
            return configData;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            // TODO verify this code is necessary for TZ data or if builtin exist
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
                {"type", "2"}, // 0 active, 1 inactive, 2 all
                {"do_search", "suchen"},
                {"time", "0"}, // 0 any, 1 1day, 2 1week, 3 30days, 4 90days
                {"details", "title"} // title, info, descr, all
            };
            if (!string.IsNullOrWhiteSpace(searchString))
                queryCollection.Add("searchstring", searchString);
            foreach (var cat in MapTorznabCapsToTrackers(query))
                queryCollection.Add("dirs" + cat, "1");
            searchUrl += "?" + queryCollection.GetQueryString();
            var response = await RequestWithCookiesAsync(searchUrl);
            var titleRegexp = new Regex(@"^return buildTable\('(.*?)',\s+");
            try
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(response.ContentString);
                var rows = dom.QuerySelectorAll("table.torrenttable > tbody > tr");
                foreach (var row in rows.Skip(1))
                {
                    var qColumn1 = row.QuerySelectorAll("td.column1");
                    var qColumn2 = row.QuerySelectorAll("td.column2");
                    var qDetailsLink = row.QuerySelector("a[href^=\"index.php?strWebValue=torrent&strWebAction=details\"]");
                    var qCatLink = row.QuerySelector("a[href^=\"index.php?strWebValue=torrent&strWebAction=search&dir=\"]");
                    var qDlLink = row.QuerySelector("a[href^=\"index.php?strWebValue=torrent&strWebAction=download&id=\"]");
                    var qDateStr = row.QuerySelector("font:has(a)");
                    var catStr = qCatLink.GetAttribute("href").Split('=')[3].Split('#')[0];
                    var link = new Uri(SiteLink + qDlLink.GetAttribute("href"));
                    var dateStr = qDateStr.TextContent;
                    var split = dateStr.IndexOf("Uploader", StringComparison.OrdinalIgnoreCase);
                    dateStr = dateStr.Substring(0, split > 0 ? split : dateStr.Length).Trim().Replace("Heute", "Today")
                                     .Replace("Gestern", "Yesterday");
                    var dateGerman = DateTimeUtil.FromUnknown(dateStr);
                    double downloadFactor;
                    if (row.QuerySelector("img[src=\"themes/images/freeleech.png\"]") != null ||
                        row.QuerySelector("img[src=\"themes/images/onlyup.png\"]") != null)
                        downloadFactor = 0;
                    else if (row.QuerySelector("img[src=\"themes/images/DL50.png\"]") != null)
                        downloadFactor = 0.5;
                    else
                        downloadFactor = 1;
                    var title = titleRegexp.Match(qDetailsLink.GetAttribute("onmouseover")).Groups[1].Value;
                    var details = new Uri(SiteLink + qDetailsLink.GetAttribute("href"));
                    var size = ParseUtil.GetBytes(qColumn2[1].TextContent);
                    var seeders = ParseUtil.CoerceInt(qColumn1[3].TextContent);
                    var leechers = ParseUtil.CoerceInt(qColumn2[3].TextContent);
                    var publishDate = TimeZoneInfo.ConvertTime(dateGerman, germanyTz, TimeZoneInfo.Local);

                    var release = new ReleaseInfo
                    {
                        MinimumRatio = 0.8,
                        MinimumSeedTime = 0,
                        Title = title,
                        Category = MapTrackerCatToNewznab(catStr),
                        Details = details,
                        Link = link,
                        Guid = link,
                        Size = size,
                        Seeders = seeders,
                        Peers = leechers + seeders,
                        PublishDate = publishDate,
                        DownloadVolumeFactor = downloadFactor,
                        UploadVolumeFactor = 1
                    };
                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.ContentString, ex);
            }

            return releases;
        }
    }
}
