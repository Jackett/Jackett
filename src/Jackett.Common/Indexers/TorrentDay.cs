using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Jackett.Common.Helpers;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    public class TorrentDay : BaseWebIndexer
    {
        private string StartPageUrl => SiteLink + "login.php";
        private string LoginUrl => SiteLink + "tak3login.php";
        private string SearchUrl => SiteLink + "t.json";

        public override string[] LegacySiteLinks { get; protected set; } = {
            "https://torrentday.com/",
        };

        public override string[] AlternativeSiteLinks { get; protected set; } = {
            "https://tday.love/",
            "https://torrentday.cool/",
            "https://tdonline.org/",
            "https://secure.torrentday.com/",
            "https://torrentday.eu/",
            "https://classic.torrentday.com/",
            "https://www.torrentday.com/",
            "https://td-update.com/",
            "https://www.torrentday.me/",
            "https://www.torrentday.ru/",
            "https://www.td.af/",
            "https://torrentday.it/",
            "https://td.findnemo.net/",
            "https://td.getcrazy.me/",
            "https://td.venom.global/",
            "https://td.workisboring.net/",
        };

        private new ConfigurationDataRecaptchaLogin configData => (ConfigurationDataRecaptchaLogin)base.configData;

        public TorrentDay(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base("TorrentDay",
                   description: "TorrentDay (TD) is a Private site for TV / MOVIES / GENERAL",
                   link: "https://tday.love/",
                   caps: new TorznabCapabilities
                   {
                       SupportsImdbMovieSearch = true
                       // SupportsImdbTVSearch = true (supported by the site but disabled due to #8107)
                   },
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   configData: new ConfigurationDataRecaptchaLogin(
                       "Make sure you get the cookies from the same torrent day domain as configured above."))
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";

            wc.EmulateBrowser = false;

            AddCategoryMapping(29, TorznabCatType.TVAnime, "Anime");
            AddCategoryMapping(28, TorznabCatType.PC, "Appz/Packs");
            AddCategoryMapping(42, TorznabCatType.AudioAudiobook, "Audio Books");
            AddCategoryMapping(20, TorznabCatType.Books, "Books");
            AddCategoryMapping(30, TorznabCatType.TVDocumentary, "Documentary");
            AddCategoryMapping(47, TorznabCatType.Other, "Fonts");
            AddCategoryMapping(43, TorznabCatType.PCMac, "Mac");

            AddCategoryMapping(96, TorznabCatType.MoviesUHD, "Movie/4K");
            AddCategoryMapping(25, TorznabCatType.MoviesSD, "Movies/480p");
            AddCategoryMapping(11, TorznabCatType.MoviesBluRay, "Movies/Bluray");
            AddCategoryMapping(5, TorznabCatType.MoviesBluRay, "Movies/Bluray-Full");
            AddCategoryMapping(3, TorznabCatType.MoviesDVD, "Movies/DVD-R");
            AddCategoryMapping(21, TorznabCatType.MoviesSD, "Movies/MP4");
            AddCategoryMapping(22, TorznabCatType.MoviesForeign, "Movies/Non-English");
            AddCategoryMapping(13, TorznabCatType.Movies, "Movies/Packs");
            AddCategoryMapping(44, TorznabCatType.MoviesSD, "Movies/SD/x264");
            AddCategoryMapping(48, TorznabCatType.Movies, "Movies/x265");
            AddCategoryMapping(1, TorznabCatType.MoviesSD, "Movies/XviD");

            AddCategoryMapping(17, TorznabCatType.AudioMP3, "Music/Audio");
            AddCategoryMapping(23, TorznabCatType.AudioForeign, "Music/Non-English");
            AddCategoryMapping(41, TorznabCatType.Audio, "Music/Packs");
            AddCategoryMapping(16, TorznabCatType.AudioVideo, "Music/Video");
            AddCategoryMapping(27, TorznabCatType.Audio, "Music/Flac");

            AddCategoryMapping(45, TorznabCatType.AudioOther, "Podcast");

            AddCategoryMapping(4, TorznabCatType.PCGames, "PC/Games");
            AddCategoryMapping(18, TorznabCatType.ConsolePS3, "PS3");
            AddCategoryMapping(8, TorznabCatType.ConsolePSP, "PSP");
            AddCategoryMapping(10, TorznabCatType.ConsoleWii, "Wii");
            AddCategoryMapping(9, TorznabCatType.ConsoleXbox360, "Xbox-360");

            AddCategoryMapping(24, TorznabCatType.TVSD, "TV/480p");
            AddCategoryMapping(32, TorznabCatType.TVHD, "TV/Bluray");
            AddCategoryMapping(31, TorznabCatType.TVSD, "TV/DVD-R");
            AddCategoryMapping(33, TorznabCatType.TVSD, "TV/DVD-Rip");
            AddCategoryMapping(46, TorznabCatType.TVSD, "TV/Mobile");
            AddCategoryMapping(14, TorznabCatType.TV, "TV/Packs");
            AddCategoryMapping(26, TorznabCatType.TVSD, "TV/SD/x264");
            AddCategoryMapping(7, TorznabCatType.TVHD, "TV/x264");
            AddCategoryMapping(34, TorznabCatType.TVUHD, "TV/x265");
            AddCategoryMapping(2, TorznabCatType.TVSD, "TV/XviD");

            AddCategoryMapping(6, TorznabCatType.XXX, "XXX/Movies");
            AddCategoryMapping(15, TorznabCatType.XXXPacks, "XXX/Packs");
        }

        public override async Task<ConfigurationData> GetConfigurationForSetup()
        {
            var loginPage = await RequestStringWithCookies(StartPageUrl, string.Empty);
            if (loginPage.IsRedirect)
                loginPage = await RequestStringWithCookies(loginPage.RedirectingTo, string.Empty);
            if (loginPage.IsRedirect)
                loginPage = await RequestStringWithCookies(loginPage.RedirectingTo, string.Empty);

            var parser = new HtmlParser();
            var dom = parser.ParseDocument(loginPage.Content);
            var result = configData;

            //result.CookieHeader.Value = loginPage.Cookies;
            UpdateCookieHeader(loginPage.Cookies); // update cookies instead of replacing them, see #3717
            result.Captcha.SiteKey = dom.QuerySelector(".g-recaptcha")?.GetAttribute("data-sitekey");
            result.Captcha.Version = "2";
            return result;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "g-recaptcha-response", configData.Captcha.Value }
            };

            if (!string.IsNullOrWhiteSpace(configData.Captcha.Cookie))
            {
                // Cookie was manually supplied
                CookieHeader = configData.Captcha.Cookie;
                try
                {
                    var results = await PerformQuery(new TorznabQuery());
                    if (!results.Any())
                        throw new Exception("no results found, please report this bug");

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

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, configData.CookieHeader.Value, true, null, LoginUrl);
            await ConfigureIfOK(result.Cookies, result.Content?.Contains("logout.php") == true, () =>
            {
                var errorMessage = result.Content;

                var parser = new HtmlParser();
                var dom = parser.ParseDocument(result.Content);
                var messageEl = dom.QuerySelector("#login");
                if (messageEl != null)
                {
                    foreach (var child in messageEl.QuerySelectorAll("form"))
                        child.Remove();
                    errorMessage = messageEl.TextContent.Trim();
                }

                if (string.IsNullOrWhiteSpace(errorMessage) && result.IsRedirect)
                    errorMessage = $"Got a redirect to {result.RedirectingTo}, please adjust your the alternative link";

                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var cats = MapTorznabCapsToTrackers(query);
            if (cats.Count == 0)
                cats = GetAllTrackerCategories();
            var catStr = string.Join(";", cats);
            var searchUrl = SearchUrl + "?" + catStr;

            if (query.IsImdbQuery)
                searchUrl += ";q=" + query.ImdbID;
            else
                searchUrl += ";q=" + WebUtilityHelpers.UrlEncode(query.GetQueryString(), Encoding);

            var results = await RequestStringWithCookiesAndRetry(searchUrl);

            // Check for being logged out
            if (results.IsRedirect)
                if (results.RedirectingTo.Contains("login.php"))
                    throw new ExceptionWithConfigData("Login failed, please reconfigure the tracker to update the cookies", configData);
                else
                    throw new ExceptionWithConfigData($"Got a redirect to {results.RedirectingTo}, please adjust your the alternative link", configData);

            try
            {
                var rows = JsonConvert.DeserializeObject<dynamic>(results.Content);

                foreach (var row in rows)
                {
                    var title = (string)row.name;
                    if ((!query.IsImdbQuery || !TorznabCaps.SupportsImdbMovieSearch) && !query.MatchQueryStringAND(title))
                        continue;
                    var torrentId = (long)row.t;
                    var comments = new Uri(SiteLink + "details.php?id=" + torrentId);
                    var seeders = (int)row.seeders;
                    var imdbId = (string)row["imdb-id"];
                    var downloadMultiplier = (double?)row["download-multiplier"] ?? 1;
                    var link = new Uri(SiteLink + "download.php/" + torrentId + "/" + torrentId + ".torrent");
                    var publishDate = DateTimeUtil.UnixTimestampToDateTime((long)row.ctime).ToLocalTime();
                    var imdb = ParseUtil.GetImdbID(imdbId);

                    var release = new ReleaseInfo
                    {
                        Title = title,
                        Comments = comments,
                        Guid = comments,
                        Link = link,
                        PublishDate = publishDate,
                        Category = MapTrackerCatToNewznab(row.c.ToString()),
                        Size = (long)row.size,
                        Files = (long)row.files,
                        Grabs = (long)row.completed,
                        Seeders = seeders,
                        Peers = seeders + (int)row.leechers,
                        Imdb = imdb,
                        DownloadVolumeFactor = downloadMultiplier,
                        UploadVolumeFactor = 1,
                        MinimumRatio = 1,
                        MinimumSeedTime = 172800 // 48 hours
                    };

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.Content, ex);
            }
            return releases;
        }
    }
}
