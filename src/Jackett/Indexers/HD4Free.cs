using CsQuery;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jackett.Models.IndexerConfig;

using AngleSharp.Parser.Html;

namespace Jackett.Indexers
{
    public class HD4Free : BaseIndexer, IIndexer
    {
        private string SearchUrl { get { return SiteLink + "ajax/initial_recall.php"; } }
        private string LoginUrl { get { return SiteLink + "login.php"; } }
        private string TakeLoginUrl { get { return SiteLink + "takelogin.php"; } }

        new ConfigurationDataRecaptchaLogin configData
        {
            get { return (ConfigurationDataRecaptchaLogin)base.configData; }
            set { base.configData = value; }
        }

        public HD4Free(IIndexerManagerService i, Logger l, IWebClient w, IProtectionService ps)
            : base(name: "HD4Free",
                description: "A HD trackers",
                link: "https://hd4free.xyz/",
                caps: new TorznabCapabilities(),
                manager: i,
                client: w,
                logger: l,
                p: ps,
                configData: new ConfigurationDataRecaptchaLogin())
        {
            AddCategoryMapping(0, TorznabCatType.Other);
            AddCategoryMapping(48, TorznabCatType.Other); // 0Day
            AddCategoryMapping(56, TorznabCatType.XXXImageset); // 0Day-Imagesets
            AddCategoryMapping(6, TorznabCatType.Audio); // 0Day-Music
            AddCategoryMapping(51, TorznabCatType.XXX); // 0Day-XXX
            AddCategoryMapping(2, TorznabCatType.TVAnime); // Anime
            AddCategoryMapping(59, TorznabCatType.MoviesBluRay); // BluRay
            AddCategoryMapping(40, TorznabCatType.TVDocumentary); // Documentary
            AddCategoryMapping(20, TorznabCatType.MoviesDVD); // DVD-R
            AddCategoryMapping(25, TorznabCatType.BooksEbook); // Ebooks
            AddCategoryMapping(38, TorznabCatType.PCPhoneIOS); // HandHeld
            AddCategoryMapping(38, TorznabCatType.PCPhoneAndroid); // HandHeld
            AddCategoryMapping(38, TorznabCatType.PCPhoneOther); // HandHeld
            AddCategoryMapping(37, TorznabCatType.Other); // Kids Stuff
            AddCategoryMapping(23, TorznabCatType.PC); // Linux
            AddCategoryMapping(24, TorznabCatType.PCMac); // Mac
            AddCategoryMapping(22, TorznabCatType.OtherMisc); // Misc
            AddCategoryMapping(35, TorznabCatType.MoviesOther); // Movie Pack
            AddCategoryMapping(36, TorznabCatType.MoviesHD); // Movie-HD
            AddCategoryMapping(19, TorznabCatType.MoviesSD); // Movie-SD
            AddCategoryMapping(50, TorznabCatType.Audio); // Music
            AddCategoryMapping(53, TorznabCatType.AudioLossless); // Music-FLAC
            AddCategoryMapping(49, TorznabCatType.AudioVideo); // MVID
            AddCategoryMapping(1, TorznabCatType.PC); // PC Apps
            AddCategoryMapping(4, TorznabCatType.PCGames); // PC Games
            AddCategoryMapping(17, TorznabCatType.ConsolePS3); // Playstation
            AddCategoryMapping(17, TorznabCatType.ConsolePS4); // Playstation
            AddCategoryMapping(17, TorznabCatType.ConsolePSVita); // Playstation
            AddCategoryMapping(17, TorznabCatType.ConsolePSP); // Playstation
            AddCategoryMapping(28, TorznabCatType.ConsolePSP); // PSP
            AddCategoryMapping(34, TorznabCatType.TVOTHER); // TV Pack
            AddCategoryMapping(32, TorznabCatType.TVHD); // TV-HD
            AddCategoryMapping(55, TorznabCatType.TVOTHER); // TV-HDRip
            AddCategoryMapping(7, TorznabCatType.TVSD); // TV-SD
            AddCategoryMapping(57, TorznabCatType.TVOTHER); // TV-SDRip
            AddCategoryMapping(33, TorznabCatType.ConsoleWii); // WII
            AddCategoryMapping(33, TorznabCatType.ConsoleWiiU); // WII
            AddCategoryMapping(45, TorznabCatType.ConsoleXbox); // XBox
            AddCategoryMapping(45, TorznabCatType.ConsoleXbox360); // XBox
            AddCategoryMapping(45, TorznabCatType.ConsoleXBOX360DLC); // XBox
            AddCategoryMapping(45, TorznabCatType.ConsoleXboxOne); // XBox
            AddCategoryMapping(9, TorznabCatType.XXX); // XXX
            AddCategoryMapping(52, TorznabCatType.XXXOther); // XXX-ISO
        }

        public override async Task<ConfigurationData> GetConfigurationForSetup()
        {
            var loginPage = await RequestStringWithCookies(LoginUrl, configData.CookieHeader.Value);
            CQ cq = loginPage.Content;
            string recaptchaSiteKey = cq.Find(".g-recaptcha").Attr("data-sitekey");
            var result = this.configData;
            result.CookieHeader.Value = loginPage.Cookies;
            result.Captcha.SiteKey = recaptchaSiteKey;
            result.Captcha.Version = "2";
            return result;
        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string> {
                { "returnto" , "/" },
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "g-recaptcha-response", configData.Captcha.Value },
                { "submitme", "Login" }
            };

            if (!string.IsNullOrWhiteSpace(configData.Captcha.Cookie))
            {
                // Cookie was manually supplied
                CookieHeader = configData.Captcha.Cookie;
                try
                {
                    var results = await PerformQuery(new TorznabQuery());
                    if (!results.Any())
                    {
                        throw new Exception("Your cookie did not work");
                    }

                    SaveConfig();
                    IsConfigured = true;
                    return IndexerConfigurationStatus.Completed;
                }
                catch (Exception e)
                {
                    IsConfigured = false;
                    throw new Exception("Your cookie did not work: " + e.Message);
                }
            }

            var result = await RequestLoginAndFollowRedirect(TakeLoginUrl, pairs, null, true, SiteLink, LoginUrl);

            await ConfigureIfOK(result.Cookies, result.Content.Contains("logout.php"), () =>
            {
                CQ dom = result.Content;
                var messageEl = dom["table.main > tbody > tr > td > table > tbody > tr > td"];
                var errorMessage = messageEl.Text().Trim();
                if (string.IsNullOrWhiteSpace(errorMessage))
                    errorMessage = result.Content;
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            
            return IndexerConfigurationStatus.RequiresTesting;
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            var pairs = new Dictionary<string, string>();
            var searchString = query.GetQueryString();
            var searchUrl = SearchUrl;

            pairs.Add("start", "0");
            pairs.Add("length", "100");
            pairs.Add("visible", "1");
            pairs.Add("uid", "-1");
            pairs.Add("order[0][column]", "9");
            pairs.Add("order[0][dir]", "desc");

            pairs.Add("cats", string.Join(",+", MapTorznabCapsToTrackers(query)));
            
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                pairs.Add("search[value]", searchString);
            }

            var results = await PostDataWithCookiesAndRetry(searchUrl, pairs);

            try
            {
                var json = JObject.Parse(results.Content);
                foreach (var row in json["data"])
                {
                    logger.Error(row.ToString(Newtonsoft.Json.Formatting.Indented));
                    var release = new ReleaseInfo();
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 259200;

                    var hParser = new HtmlParser();
                    var hName = hParser.Parse(row["name"].ToString());
                    var hComments = hParser.Parse(row["comments"].ToString());
                    var hNumfiles = hParser.Parse(row["numfiles"].ToString());
                    var hSeeders = hParser.Parse(row["seeders"].ToString());
                    var hLeechers = hParser.Parse(row["leechers"].ToString());

                    var hDetailsLink = hName.QuerySelector("a[href^=\"details.php?id=\"]");
                    var hCommentsLink = hComments.QuerySelector("a");
                    var hDownloadLink = hName.QuerySelector("a[title=\"Download Torrent\"]");

                    release.Title = hDetailsLink.TextContent;
                    release.Comments = new Uri(SiteLink + hCommentsLink.GetAttribute("href"));
                    release.Link = new Uri(SiteLink + hDownloadLink.GetAttribute("href"));
                    release.Guid = release.Link;

                    release.Description = row["genre"].ToString();

                    var poster = row["poster"].ToString();
                    if(!string.IsNullOrWhiteSpace(poster))
                    {
                        release.BannerUrl = new Uri(SiteLink + poster);
                    }

                    release.Size = ReleaseInfo.GetBytes(row["size"].ToString());
                    var imdbId = row["imdbid"].ToString();
                    if (imdbId.StartsWith("tt"))
                        release.Imdb = ParseUtil.CoerceLong(imdbId.Substring(2));

                    var added = row["added"].ToString().Replace("<br>", " ");
                    release.PublishDate = DateTimeUtil.FromUnknown(added);

                    release.Category = ParseUtil.CoerceInt(row["catid"].ToString());

                    release.Seeders = ParseUtil.CoerceInt(hSeeders.QuerySelector("a").TextContent);
                    release.Peers = ParseUtil.CoerceInt(hLeechers.QuerySelector("a").TextContent) + release.Seeders;

                    release.Files = ParseUtil.CoerceInt(hNumfiles.QuerySelector("a").TextContent);

                    release.DownloadVolumeFactor = 1;
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