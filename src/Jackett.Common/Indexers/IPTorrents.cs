using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CsQuery;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    public class IPTorrents : BaseWebIndexer
    {
        private string LoginUrl { get { return SiteLink + "login.php"; } }
        private string TakeLoginUrl { get { return SiteLink + "take_login.php"; } }
        private string BrowseUrl { get { return SiteLink + "t"; } }

        public override string[] AlternativeSiteLinks { get; protected set; } = new string[] {
            "https://iptorrents.com/",
            "https://ipt-update.com/",
            "https://iptorrents.eu/",
            "https://nemo.iptorrents.com/",
            "https://ipt.rocks/",
            "http://ipt.read-books.org/",
            "http://alien.eating-organic.net/",
            "http://kong.net-freaks.com/",
            "http://ghost.cable-modem.org/",
            "http://logan.unusualperson.com/",
            "http://baywatch.workisboring.com/",
        };

        private new ConfigurationDataRecaptchaLogin configData
        {
            get { return (ConfigurationDataRecaptchaLogin)base.configData; }
            set { base.configData = value; }
        }

        public IPTorrents(IIndexerConfigurationService configService, Utils.Clients.WebClient wc, Logger l, IProtectionService ps)
            : base(name: "IPTorrents",
                description: "Always a step ahead.",
                link: "https://iptorrents.com/",
                caps: new TorznabCapabilities(),
                configService: configService,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataRecaptchaLogin())
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";

            TorznabCaps.SupportsImdbSearch = true;

            AddCategoryMapping(72, TorznabCatType.Movies, "Movies");
            AddCategoryMapping(87, TorznabCatType.Movies3D, "Movie/3D");
            AddCategoryMapping(77, TorznabCatType.MoviesSD, "Movie/480p");
            AddCategoryMapping(101, TorznabCatType.MoviesHD, "Movie/4K");
            AddCategoryMapping(89, TorznabCatType.MoviesHD, "Movie/BD-R");
            AddCategoryMapping(90, TorznabCatType.MoviesSD, "Movie/BD-Rip");
            AddCategoryMapping(96, TorznabCatType.MoviesSD, "Movie/Cam");
            AddCategoryMapping(6, TorznabCatType.MoviesDVD, "Movie/DVD-R");
            AddCategoryMapping(48, TorznabCatType.MoviesBluRay, "Movie/HD/Bluray");
            AddCategoryMapping(54, TorznabCatType.Movies, "Movie/Kids");
            AddCategoryMapping(62, TorznabCatType.MoviesSD, "Movie/MP4");
            AddCategoryMapping(38, TorznabCatType.MoviesForeign, "Movie/Non-English");
            AddCategoryMapping(68, TorznabCatType.Movies, "Movie/Packs");
            AddCategoryMapping(20, TorznabCatType.MoviesHD, "Movie/Web-DL");
            AddCategoryMapping(7, TorznabCatType.MoviesSD, "Movie/Xvid");
            AddCategoryMapping(100, TorznabCatType.Movies, "Movie/x265");

            AddCategoryMapping(73, TorznabCatType.TV, "TV");
            AddCategoryMapping(26, TorznabCatType.TVDocumentary, "Documentaries");
            AddCategoryMapping(55, TorznabCatType.TVSport, "Sports");
            AddCategoryMapping(78, TorznabCatType.TVSD, "TV/480p");
            AddCategoryMapping(23, TorznabCatType.TVHD, "TV/BD");
            AddCategoryMapping(24, TorznabCatType.TVSD, "TV/DVD-R");
            AddCategoryMapping(25, TorznabCatType.TVSD, "TV/DVD-Rip");
            AddCategoryMapping(66, TorznabCatType.TVSD, "TV/Mobile");
            AddCategoryMapping(82, TorznabCatType.TVFOREIGN, "TV/Non-English");
            AddCategoryMapping(65, TorznabCatType.TV, "TV/Packs");
            AddCategoryMapping(83, TorznabCatType.TVFOREIGN, "TV/Packs/Non-English");
            AddCategoryMapping(79, TorznabCatType.TVSD, "TV/SD/x264");
            AddCategoryMapping(22, TorznabCatType.TVWEBDL, "TV/Web-DL");
            AddCategoryMapping(5, TorznabCatType.TVHD, "TV/x264");
            AddCategoryMapping(99, TorznabCatType.TVHD, "TV/x265");
            AddCategoryMapping(4, TorznabCatType.TVSD, "TV/Xvid");

            AddCategoryMapping(74, TorznabCatType.Console, "Games");
            AddCategoryMapping(2, TorznabCatType.ConsoleOther, "Games/Mixed");
            AddCategoryMapping(47, TorznabCatType.ConsoleNDS, "Games/Nintendo DS");
            AddCategoryMapping(43, TorznabCatType.PCISO, "Games/PC-ISO");
            AddCategoryMapping(45, TorznabCatType.PCGames, "Games/PC-Rip");
            AddCategoryMapping(39, TorznabCatType.ConsolePS3, "Games/PS2");
            AddCategoryMapping(71, TorznabCatType.ConsolePS3, "Games/PS3");
            AddCategoryMapping(40, TorznabCatType.ConsolePSP, "Games/PSP");
            AddCategoryMapping(50, TorznabCatType.ConsoleWii, "Games/Wii");
            AddCategoryMapping(44, TorznabCatType.ConsoleXbox360, "Games/Xbox-360");

            AddCategoryMapping(75, TorznabCatType.Audio, "Music");
            AddCategoryMapping(3, TorznabCatType.AudioMP3, "Music/Audio");
            AddCategoryMapping(80, TorznabCatType.AudioLossless, "Music/Flac");
            AddCategoryMapping(93, TorznabCatType.Audio, "Music/Packs");
            AddCategoryMapping(37, TorznabCatType.AudioVideo, "Music/Video");
            AddCategoryMapping(21, TorznabCatType.AudioVideo, "Podcast");

            AddCategoryMapping(76, TorznabCatType.Other, "Miscellaneous");
            AddCategoryMapping(60, TorznabCatType.TVAnime, "Anime");
            AddCategoryMapping(1, TorznabCatType.PC0day, "Appz");
            AddCategoryMapping(86, TorznabCatType.PC0day, "Appz/Non-English");
            AddCategoryMapping(64, TorznabCatType.AudioAudiobook, "AudioBook");
            AddCategoryMapping(35, TorznabCatType.Books, "Books");
            AddCategoryMapping(94, TorznabCatType.BooksComics, "Comics");
            AddCategoryMapping(95, TorznabCatType.BooksOther, "Educational");
            AddCategoryMapping(98, TorznabCatType.Other, "Fonts");
            AddCategoryMapping(69, TorznabCatType.PCMac, "Mac");
            AddCategoryMapping(92, TorznabCatType.BooksMagazines, "Magazines / Newspapers");
            AddCategoryMapping(58, TorznabCatType.PCPhoneOther, "Mobile");
            AddCategoryMapping(36, TorznabCatType.Other, "Pics/Wallpapers");

            AddCategoryMapping(88, TorznabCatType.XXX, "XXX");
            AddCategoryMapping(85, TorznabCatType.XXXOther, "XXX/Magazines");
            AddCategoryMapping(8, TorznabCatType.XXX, "XXX/Movie");
            AddCategoryMapping(81, TorznabCatType.XXX, "XXX/Movie/0Day");
            AddCategoryMapping(91, TorznabCatType.XXXPacks, "XXX/Packs");
            AddCategoryMapping(84, TorznabCatType.XXXImageset, "XXX/Pics/Wallpapers");
        }

        public override async Task<ConfigurationData> GetConfigurationForSetup()
        {
            var loginPage = await RequestStringWithCookies(LoginUrl, string.Empty);
            CQ cq = loginPage.Content;
            var captcha = cq.Find(".g-recaptcha");
            if (captcha.Any())
            {
                var result = this.configData;
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

            var request = new Utils.Clients.WebRequest()
            {
                Url = TakeLoginUrl,
                Type = RequestType.POST,
                Referer = SiteLink,
                Encoding = Encoding,
                PostData = pairs
            };
            var response = await webclient.GetString(request);
            var firstCallCookies = response.Cookies;
            // Redirect to ? then to /t
            await FollowIfRedirect(response, request.Url, null, firstCallCookies);

            await ConfigureIfOK(firstCallCookies, response.Content.Contains("/my.php"), () =>
            {
                CQ dom = response.Content;
                var messageEl = dom["body > div"].First();
                var errorMessage = messageEl.Text().Trim();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();
            var searchUrl = BrowseUrl;
            var queryCollection = new NameValueCollection();

            if (!string.IsNullOrWhiteSpace(query.ImdbID))
            {
                queryCollection.Add("q", query.ImdbID);
            }
            else if (!string.IsNullOrWhiteSpace(searchString))
            {
                queryCollection.Add("q", searchString);
            }

            foreach (var cat in MapTorznabCapsToTrackers(query))
            {
                queryCollection.Add(cat, string.Empty);
            }

            if (queryCollection.Count > 0)
            {
                searchUrl += "?" + queryCollection.GetQueryString();
            }

            var response = await RequestStringWithCookiesAndRetry(searchUrl, null, BrowseUrl);

            var results = response.Content;
            try
            {
                CQ dom = results;

                var rows = dom["table[id='torrents'] > tbody > tr"];
                foreach (var row in rows.Skip(1))
                {
                    var release = new ReleaseInfo();
                    var qRow = row.Cq();
                    var qTitleLink = qRow.Find("a[href^=\"/details.php?id=\"]").First();
                    release.Title = qTitleLink.Text().Trim();

                    // If we search an get no results, we still get a table just with no info.
                    if (string.IsNullOrWhiteSpace(release.Title))
                    {
                        break;
                    }

                    release.Guid = new Uri(SiteLink + qTitleLink.Attr("href").Substring(1));
                    release.Comments = release.Guid;

                    var descString = qRow.Find(".t_ctime").Text();
                    var dateString = descString.Split('|').Last().Trim();
                    dateString = dateString.Split(new string[] { " by " }, StringSplitOptions.None)[0];
                    release.PublishDate = DateTimeUtil.FromTimeAgo(dateString);

                    var qLink = row.ChildElements.ElementAt(3).Cq().Children("a");
                    release.Link = new Uri(SiteLink + WebUtility.UrlEncode(qLink.Attr("href").TrimStart('/')));

                    var sizeStr = row.ChildElements.ElementAt(5).Cq().Text();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(qRow.Find(".t_seeders").Text().Trim());
                    release.Peers = ParseUtil.CoerceInt(qRow.Find(".t_leechers").Text().Trim()) + release.Seeders;

                    var catIcon = row.Cq().Find("td:eq(0) a");
                    if (catIcon.Length >= 1) // Torrents - Category column == Icons
                        release.Category = MapTrackerCatToNewznab(catIcon.First().Attr("href").Substring(1));
                    else // Torrents - Category column == Text or Code
                        throw new Exception("Please go to " + SiteLink + "settings.php and change the \"Torrents - Category column\" option to \"Icons\". Wait a minute (cache) and then try again.");
                        //release.Category = MapTrackerCatDescToNewznab(row.Cq().Find("td:eq(0)").Text()); // Works for "Text" but only contains the parent category

                    var filesElement = row.Cq().Find("a[href*=\"/files\"]"); // optional
                    if (filesElement.Length == 1)
                        release.Files = ParseUtil.CoerceLong(filesElement.Text());

                    var grabs = row.Cq().Find("td:nth-last-child(3)").Text();
                    release.Grabs = ParseUtil.CoerceInt(grabs);

                    if (row.Cq().Find("span.t_tag_free_leech").Any())
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
