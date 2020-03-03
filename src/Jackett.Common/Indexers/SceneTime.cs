using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
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
    public class SceneTime : BaseWebIndexer
    {
        private string StartPageUrl => SiteLink + "login.php";
        private string LoginUrl => SiteLink + "takelogin.php";
        private string SearchUrl => SiteLink + "browse.php";
        private string DownloadUrl => SiteLink + "download.php/{0}/download.torrent";


        private new ConfigurationDataSceneTime configData
        {
            get => (ConfigurationDataSceneTime)base.configData;
            set => base.configData = value;
        }

        public SceneTime(IIndexerConfigurationService configService, WebClient w, Logger l, IProtectionService ps)
            : base(name: "SceneTime",
                description: "Always on time",
                link: "https://www.scenetime.com/",
                caps: new TorznabCapabilities(),
                configService: configService,
                client: w,
                logger: l,
                p: ps,
                configData: new ConfigurationDataSceneTime())
        {
            Encoding = Encoding.GetEncoding("iso-8859-1");
            Language = "en-us";
            Type = "private";

            //Movies
            AddCategoryMapping(1, TorznabCatType.MoviesSD, "Movies/XviD");
            AddCategoryMapping(3, TorznabCatType.MoviesDVD, "Movies/DVD-R");
            AddCategoryMapping(10, TorznabCatType.XXX, "Movies/XxX");
            AddCategoryMapping(47, TorznabCatType.Movies, "Movie/Packs");
            AddCategoryMapping(56, TorznabCatType.Movies, "Movies/Anime");
            AddCategoryMapping(57, TorznabCatType.MoviesSD, "Movies/SD");
            AddCategoryMapping(59, TorznabCatType.MoviesHD, "Movies/HD");
            AddCategoryMapping(61, TorznabCatType.Movies, "Movies/Classic");
            AddCategoryMapping(64, TorznabCatType.Movies3D, "Movies/3D");
            AddCategoryMapping(80, TorznabCatType.MoviesForeign, "Movies/Non-English");
            AddCategoryMapping(81, TorznabCatType.MoviesBluRay, "Movies/BluRay");
            AddCategoryMapping(82, TorznabCatType.MoviesOther, "Movies/CAM-TS");
            AddCategoryMapping(102, TorznabCatType.MoviesOther, "Movies/Remux");
            AddCategoryMapping(22, TorznabCatType.MoviesWEBDL, "Movies/Web-Rip/DL");
            AddCategoryMapping(105, TorznabCatType.Movies, "Movies/Kids");
            AddCategoryMapping(16, TorznabCatType.MoviesUHD, "Movies/4K");
            AddCategoryMapping(17, TorznabCatType.MoviesBluRay, "Movies/4K bluray");

            //TV
            AddCategoryMapping(2, TorznabCatType.TVSD, "TV/XviD");
            AddCategoryMapping(43, TorznabCatType.TV, "TV/Packs");
            AddCategoryMapping(9, TorznabCatType.TVHD, "TV-HD");
            AddCategoryMapping(63, TorznabCatType.TV, "TV/Classic");
            AddCategoryMapping(77, TorznabCatType.TVSD, "TV/SD");
            AddCategoryMapping(79, TorznabCatType.TVSport, "Sports");
            AddCategoryMapping(100, TorznabCatType.TVFOREIGN, "TV/Non-English");
            AddCategoryMapping(83, TorznabCatType.TVWEBDL, "TV/Web-Rip");
            AddCategoryMapping(8, TorznabCatType.TVOTHER, "TV-Mobile");
            AddCategoryMapping(18, TorznabCatType.TVAnime, "TV/Anime");
            AddCategoryMapping(19, TorznabCatType.TVHD, "TV-x265");

            // Games
            AddCategoryMapping(6, TorznabCatType.PCGames, "Games/PC ISO");
            AddCategoryMapping(48, TorznabCatType.ConsoleXbox, "Games/XBOX");
            AddCategoryMapping(49, TorznabCatType.ConsolePSP, "Games/PSP");
            AddCategoryMapping(50, TorznabCatType.ConsolePS3, "Games/PS3");
            AddCategoryMapping(51, TorznabCatType.ConsoleWii, "Games/Wii");
            AddCategoryMapping(55, TorznabCatType.ConsoleNDS, "Games/Nintendo DS");
            AddCategoryMapping(12, TorznabCatType.ConsolePS4, "Games/Ps4");
            AddCategoryMapping(13, TorznabCatType.ConsoleOther, "Games/PS1");
            AddCategoryMapping(14, TorznabCatType.ConsoleOther, "Games/PS2");
            AddCategoryMapping(15, TorznabCatType.ConsoleOther, "Games/Dreamcast");

            // Miscellaneous
            AddCategoryMapping(5, TorznabCatType.PC0day, "Apps/0DAY");
            AddCategoryMapping(7, TorznabCatType.Books, "Books-Mags");
            AddCategoryMapping(52, TorznabCatType.PCMac, "Mac");
            AddCategoryMapping(65, TorznabCatType.BooksComics, "Books/Comic");
            AddCategoryMapping(53, TorznabCatType.PC, "Appz");
            AddCategoryMapping(24, TorznabCatType.PCPhoneOther, "Mobile/Appz");

            // Music
            AddCategoryMapping(4, TorznabCatType.Audio, "Music/Audio");
            AddCategoryMapping(11, TorznabCatType.AudioVideo, "Music/Videos");
            AddCategoryMapping(116, TorznabCatType.Audio, "Music/Pack");
        }

        public override async Task<ConfigurationData> GetConfigurationForSetup()
        {
            var loginPage = await RequestStringWithCookies(StartPageUrl, string.Empty);
            var parser = new HtmlParser();
            var dom = parser.ParseDocument(loginPage.Content);
            var recaptcha = dom.QuerySelector(".g-recaptcha");
            if (recaptcha != null)
            {
                var result = configData;
                result.Captcha.Version = "2";
                result.CookieHeader.Value = loginPage.Cookies;
                result.Captcha.SiteKey = recaptcha.GetAttribute("data-sitekey");
                return result;
            }
            else
            {
                var stdResult = new ConfigurationDataBasicLogin
                {
                    SiteLink = {Value = configData.SiteLink.Value},
                    Username = {Value = configData.Username.Value},
                    Password = {Value = configData.Password.Value},
                    CookieHeader = {Value = loginPage.Cookies}
                };
                return stdResult;
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

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, LoginUrl);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("logout.php"), () =>
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(result.Content);
                var errorMessage = dom.QuerySelector("td.text").TextContent.Trim();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var qParams = new NameValueCollection
            {
                {"cata", "yes"},
                {"sec", "jax"}
            };

            var catList = MapTorznabCapsToTrackers(query);
            foreach (var cat in catList)
                qParams.Add("c" + cat, "1");

            if (!string.IsNullOrEmpty(query.SanitizedSearchTerm))
                qParams.Add("search", query.GetQueryString());

            // If Only Freeleech Enabled
            if (configData.Freeleech.Value)
                qParams.Add("freeleech", "on");

            var searchUrl = SearchUrl + "?" + qParams.GetQueryString();

            var results = await RequestStringWithCookies(searchUrl);
            var releases = ParseResponse(query, results.Content);

            return releases;
        }

        private List<ReleaseInfo> ParseResponse(TorznabQuery query, string htmlResponse)
        {
            var releases = new List<ReleaseInfo>();

            try
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(htmlResponse);

                var table = dom.QuerySelector("table.movehere");
                if (table == null)
                    return releases; // no results

                var headerColumns = table.QuerySelectorAll("tbody > tr > td.cat_Head")
                                         .Select(x => x.TextContent).ToList();
                var categoryIndex = headerColumns.FindIndex(x => x.Equals("Type"));
                var nameIndex = headerColumns.FindIndex(x => x.Equals("Name"));
                var sizeIndex = headerColumns.FindIndex(x => x.Equals("Size"));
                var seedersIndex = headerColumns.FindIndex(x => x.Equals("Seeders"));
                var leechersIndex = headerColumns.FindIndex(x => x.Equals("Leechers"));

                var rows = dom.QuerySelectorAll("tr.browse");
                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800; // 48 hours

                    var qCatLink = row.Children[categoryIndex].QuerySelector("a");
                    if (qCatLink != null)
                    {
                        var catId = new Regex(@"\?cat=(\d*)").Match(qCatLink.GetAttribute("href")).Groups[1].ToString().Trim();
                        release.Category = MapTrackerCatToNewznab(catId);
                    }

                    var qDescCol = row.Children[nameIndex];
                    var qLink = qDescCol.QuerySelector("a");
                    release.Title = qLink.TextContent;
                    if (!query.MatchQueryStringAND(release.Title))
                        continue;

                    release.Comments = new Uri(SiteLink + "/" + qLink.GetAttribute("href"));
                    release.Guid = release.Comments;

                    var torrentId = qLink.GetAttribute("href").Split('=')[1];
                    release.Link = new Uri(string.Format(DownloadUrl, torrentId));

                    release.PublishDate = DateTimeUtil.FromTimeAgo(qDescCol.ChildNodes.Last().TextContent);

                    var sizeStr = row.Children[sizeIndex].TextContent;
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(row.Children[seedersIndex].TextContent.Trim());
                    release.Peers = ParseUtil.CoerceInt(row.Children[leechersIndex].TextContent.Trim()) + release.Seeders;

                    release.DownloadVolumeFactor = row.QuerySelector("font > b:contains(Freeleech)") != null ? 0 : 1;
                    release.UploadVolumeFactor = 1;

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(htmlResponse, ex);
            }

            return releases;
        }
    }
}
