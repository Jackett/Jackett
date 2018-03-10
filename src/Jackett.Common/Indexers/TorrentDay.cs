using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsQuery;
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
        private string StartPageUrl { get { return SiteLink + "login.php"; } }
        private string LoginUrl { get { return SiteLink + "tak3login.php"; } }
        private string SearchUrl { get { return SiteLink + "t.json"; } }

        public override string[] AlternativeSiteLinks { get; protected set; } = new string[] {
            "https://tdonline.org/",
            "https://secure.torrentday.com/",
            "https://torrentday.eu/",
            "https://torrentday.it/",
            "https://classic.torrentday.com/",
            "https://www.torrentday.com/",
            "https://td-update.com/",
            "https://www.torrentday.me/",
            "https://www.torrentday.ru/",
            "https://www.torrentday.com/",
            "https://www.td.af/",
        };

        private new ConfigurationDataRecaptchaLogin configData
        {
            get { return (ConfigurationDataRecaptchaLogin)base.configData; }
            set { base.configData = value; }
        }

        public TorrentDay(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(name: "TorrentDay",
                description: "TorrentDay (TD) is a Private site for TV / MOVIES / GENERAL",
                link: "https://torrentday.it/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                configService: configService,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataRecaptchaLogin())
        {
            wc.EmulateBrowser = false;
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";

            TorznabCaps.SupportsImdbSearch = true;

            AddCategoryMapping(29, TorznabCatType.TVAnime, "Anime");
            AddCategoryMapping(28, TorznabCatType.PC, "Appz/Packs");
            AddCategoryMapping(42, TorznabCatType.AudioAudiobook, "Audio Books");
            AddCategoryMapping(20, TorznabCatType.Books, "Books");
            AddCategoryMapping(30, TorznabCatType.TVDocumentary, "Documentary");
            AddCategoryMapping(47, TorznabCatType.Other, "Fonts");
            AddCategoryMapping(43, TorznabCatType.PCMac, "Mac");

            AddCategoryMapping(25, TorznabCatType.MoviesSD, "Movies/480p");
            AddCategoryMapping(11, TorznabCatType.MoviesBluRay, "Movies/Bluray");
            AddCategoryMapping(5, TorznabCatType.MoviesBluRay, "Movies/Bluray-Full");
            AddCategoryMapping(3, TorznabCatType.MoviesDVD, "Movies/DVD-R");
            AddCategoryMapping(21, TorznabCatType.MoviesSD, "Movies/MP4");
            AddCategoryMapping(22, TorznabCatType.MoviesForeign, "Movies/Non-English");
            AddCategoryMapping(13, TorznabCatType.Movies, "Movies/Packs");
            AddCategoryMapping(44, TorznabCatType.MoviesSD, "Movies/SD/x264");
            AddCategoryMapping(48, TorznabCatType.MoviesUHD, "Movies/x265");
            AddCategoryMapping(1, TorznabCatType.MoviesSD, "Movies/XviD");

            AddCategoryMapping(17, TorznabCatType.Audio, "Music/Audio");
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
            CQ cq = loginPage.Content;
            var result = this.configData;
            result.CookieHeader.Value = loginPage.Cookies;
            result.Captcha.SiteKey = cq.Find(".g-recaptcha").Attr("data-sitekey");
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
                    if (results.Count() == 0)
                    {
                        throw new Exception("no results found, please report this bug");
                    }

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
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("logout.php"), () =>
            {
                CQ dom = result.Content;
                var messageEl = dom["#login"];
                messageEl.Children("form").Remove();
                var errorMessage = messageEl.Text().Trim();

                if (string.IsNullOrWhiteSpace(errorMessage))
                {
                    errorMessage = dom.Text();
                }

                if (string.IsNullOrWhiteSpace(errorMessage) && result.IsRedirect)
                {
                    errorMessage = string.Format("Got a redirect to {0}, please adjust your the alternative link", result.RedirectingTo);
                }

                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();
            var queryUrl = SearchUrl;

            var cats = MapTorznabCapsToTrackers(query);
            if (cats.Count == 0)
                cats = GetAllTrackerCategories();

            var catStr = string.Join(";", cats);
            queryUrl += "?" + catStr;

            if (!string.IsNullOrWhiteSpace(query.ImdbID))
            {
                queryUrl += ";q=" + query.ImdbID;
            }
            else
            {
                queryUrl += ";q=" + WebUtilityHelpers.UrlEncode(searchString, Encoding);
            }

            var results = await RequestStringWithCookiesAndRetry(queryUrl);

            // Check for being logged out
            if (results.IsRedirect)
                if (results.RedirectingTo.Contains("login.php"))
                    throw new ExceptionWithConfigData("Login failed, please reconfigure the tracker to update the cookies", configData);
                else
                    throw new ExceptionWithConfigData(string.Format("Got a redirect to {0}, please adjust your the alternative link", results.RedirectingTo), configData);

            try
            {
                dynamic json = JsonConvert.DeserializeObject<dynamic>(results.Content);

                foreach (var torrent in json)
                {
                    var release = new ReleaseInfo();

                    release.Title = torrent.name;
                    if ((query.ImdbID == null || !TorznabCaps.SupportsImdbSearch) && !query.MatchQueryStringAND(release.Title))
                        continue;

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;
                    release.Category = MapTrackerCatToNewznab(torrent.c.ToString());
                    
                    var torrentID = (long)torrent.t;
                    release.Comments = new Uri(SiteLink + "details.php?id=" + torrentID);
                    release.Guid = release.Comments;
                    release.Link = new Uri(SiteLink + "download.php/" + torrentID + "/dummy.torrent");
                    release.PublishDate = DateTimeUtil.UnixTimestampToDateTime((long)torrent.ctime).ToLocalTime();

                    release.Size = (long)torrent.size;
                    release.Seeders = (int)torrent.seeders;
                    release.Peers = release.Seeders + (int)torrent.leechers;
                    release.Files = (long)torrent.files;
                    release.Grabs = (long)torrent.completed;
                    var imdbId = (string)torrent["imdb-id"];
                    release.Imdb = ParseUtil.GetImdbID(imdbId);
                    var downloadMultiplier = (double?)torrent["download-multiplier"];
                    release.DownloadVolumeFactor = downloadMultiplier ?? 1;
                    release.UploadVolumeFactor = 1;

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
