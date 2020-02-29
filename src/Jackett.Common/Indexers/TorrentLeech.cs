using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CsQuery;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    public class TorrentLeech : BaseWebIndexer
    {
        public override string[] LegacySiteLinks { get; protected set; } = new string[] {
            "https://v4.torrentleech.org/",
        };

        private string LoginUrl => SiteLink + "user/account/login/";
        private string SearchUrl => SiteLink + "torrents/browse/list/";

        private new ConfigurationDataRecaptchaLogin configData
        {
            get => (ConfigurationDataRecaptchaLogin)base.configData;
            set => base.configData = value;
        }

        public TorrentLeech(IIndexerConfigurationService configService, Utils.Clients.WebClient wc, Logger l, IProtectionService ps)
            : base(name: "TorrentLeech",
                description: "This is what happens when you seed",
                link: "https://www.torrentleech.org/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                configService: configService,
                client: wc,
                logger: l,
                p: ps,
                downloadBase: "https://www.torrentleech.org/download/",
                configData: new ConfigurationDataRecaptchaLogin("For best results, change the 'Default Number of Torrents per Page' setting to the maximum in your profile on the TorrentLeech webpage."))
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";
            TorznabCaps.SupportsImdbMovieSearch = true;
            TorznabCaps.SupportsImdbTVSearch = true;

            AddCategoryMapping(8, TorznabCatType.MoviesSD); // cam
            AddCategoryMapping(9, TorznabCatType.MoviesSD); //ts
            AddCategoryMapping(10, TorznabCatType.MoviesSD); // Sceener
            AddCategoryMapping(11, TorznabCatType.MoviesSD);
            AddCategoryMapping(12, TorznabCatType.MoviesSD);
            AddCategoryMapping(13, TorznabCatType.MoviesHD);
            AddCategoryMapping(14, TorznabCatType.MoviesHD);
            AddCategoryMapping(15, TorznabCatType.Movies); // Boxsets
            AddCategoryMapping(29, TorznabCatType.TVDocumentary);
            AddCategoryMapping(41, TorznabCatType.MoviesHD, "4K Upscaled");
            AddCategoryMapping(47, TorznabCatType.MoviesHD, "Real 4K UltraHD HDR");
            AddCategoryMapping(36, TorznabCatType.MoviesForeign);
            AddCategoryMapping(37, TorznabCatType.MoviesWEBDL);
            AddCategoryMapping(43, TorznabCatType.MoviesSD, "Movies/HDRip");

            AddCategoryMapping(26, TorznabCatType.TVSD);
            AddCategoryMapping(27, TorznabCatType.TV); // Boxsets
            AddCategoryMapping(32, TorznabCatType.TVHD);
            AddCategoryMapping(44, TorznabCatType.TVFOREIGN, "TV/Foreign");

            AddCategoryMapping(17, TorznabCatType.PCGames);
            AddCategoryMapping(18, TorznabCatType.ConsoleXbox);
            AddCategoryMapping(19, TorznabCatType.ConsoleXbox360);
            AddCategoryMapping(40, TorznabCatType.ConsoleXbox, "Games/XBOXONE");
            AddCategoryMapping(20, TorznabCatType.ConsolePS3); // PS2
            AddCategoryMapping(21, TorznabCatType.ConsolePS3);
            AddCategoryMapping(22, TorznabCatType.ConsolePSP);
            AddCategoryMapping(28, TorznabCatType.ConsoleWii);
            AddCategoryMapping(30, TorznabCatType.ConsoleNDS);
            AddCategoryMapping(39, TorznabCatType.ConsolePS4);
            AddCategoryMapping(42, TorznabCatType.PCMac, "Games/Mac");

            AddCategoryMapping(16, TorznabCatType.AudioVideo);
            AddCategoryMapping(31, TorznabCatType.Audio);

            AddCategoryMapping(34, TorznabCatType.TVAnime);
            AddCategoryMapping(35, TorznabCatType.TV); // Cartoons

            AddCategoryMapping(5, TorznabCatType.Books);
            AddCategoryMapping(45, TorznabCatType.BooksEbook, "Books/EBooks");
            AddCategoryMapping(46, TorznabCatType.BooksComics, "Books/Comics");

            AddCategoryMapping(23, TorznabCatType.PCISO);
            AddCategoryMapping(24, TorznabCatType.PCMac);
            AddCategoryMapping(25, TorznabCatType.PCPhoneOther);
            AddCategoryMapping(33, TorznabCatType.PC0day);

            AddCategoryMapping(38, TorznabCatType.Other, "Education");
        }

        public override async Task<ConfigurationData> GetConfigurationForSetup()
        {
            var loginPage = await RequestStringWithCookies(LoginUrl, string.Empty);
            CQ cq = loginPage.Content;
            var captcha = cq.Find(".g-recaptcha");
            if (captcha.Any())
            {
                var result = configData;
                result.CookieHeader.Value = loginPage.Cookies;
                result.Captcha.SiteKey = captcha.Attr("data-sitekey");
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
            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "g-recaptcha-response", configData.Captcha.Value }
            };

            if (!string.IsNullOrWhiteSpace(configData.Captcha.Cookie))
            {
                CookieHeader = configData.Captcha.Cookie;
                try
                {
                    var results = await PerformQuery(new TorznabQuery());
                    if (results.Count() == 0)
                    {
                        throw new Exception("Your cookie did not work");
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

            await DoLogin();
            return IndexerConfigurationStatus.RequiresTesting;
        }

        private async Task DoLogin()
        {
            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, LoginUrl);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("/user/account/logout"), () =>
            {
                CQ dom = result.Content;
                var errorMessage = dom["p.text-danger:contains(\"Error:\")"].Text().Trim();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();
            searchString = Regex.Replace(searchString, @"(^|\s)-", " "); // remove dashes at the beginning of keywords as they exclude search strings (see issue #3096)
            var searchUrl = SearchUrl;
            var imdbId = ParseUtil.GetFullImdbID(query.ImdbID);

            if (imdbId != null)
            {
                searchUrl += "imdbID/" + imdbId + "/";
            }
            else if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchUrl += "query/" + WebUtility.UrlEncode(searchString) + "/";
            }
            string.Format(SearchUrl, WebUtility.UrlEncode(searchString));

            var cats = MapTorznabCapsToTrackers(query);
            if (cats.Count > 0)
            {
                searchUrl += "categories/";
                foreach (var cat in cats)
                {
                    if (!searchUrl.EndsWith("/"))
                        searchUrl += ",";
                    searchUrl += cat;
                }
            }
            else
            {
                searchUrl += "newfilter/2"; // include 0day and music
            }

            var results = await RequestStringWithCookiesAndRetry(searchUrl);

            if (results.Content.Contains("/user/account/login"))
            {
                //Cookie appears to expire after a period of time or logging in to the site via browser
                await DoLogin();
                results = await RequestStringWithCookiesAndRetry(searchUrl);
            }

            try
            {
                dynamic jsonObj = JsonConvert.DeserializeObject(results.Content);

                foreach (var torrent in jsonObj.torrentList)
                {
                    var release = new ReleaseInfo();

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800; // 48 hours

                    release.Guid = new Uri(SiteLink + "torrent/" + torrent.fid);
                    release.Comments = release.Guid;
                    release.Title = torrent.name;

                    if (!query.MatchQueryStringAND(release.Title))
                        continue;

                    release.Link = new Uri(SiteLink + "download/" + torrent.fid + "/" + torrent.filename);

                    release.PublishDate = DateTime.ParseExact(torrent.addedTimestamp.ToString(), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);

                    release.Size = (long)torrent.size;

                    release.Seeders = ParseUtil.CoerceInt(torrent.seeders.ToString());
                    release.Peers = release.Seeders + ParseUtil.CoerceInt(torrent.leechers.ToString());

                    release.Category = MapTrackerCatToNewznab(torrent.categoryID.ToString());

                    release.Grabs = ParseUtil.CoerceInt(torrent.completed.ToString());

                    release.Imdb = ParseUtil.GetImdbID(torrent.imdbID.ToString());

                    release.UploadVolumeFactor = 1;

                    // freeleech #6579 #6624 #7367
                    release.DownloadVolumeFactor = string.IsNullOrEmpty(torrent.download_multiplier.ToString()) ?
                        1 :
                        ParseUtil.CoerceInt(torrent.download_multiplier.ToString());

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
