using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom;
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
    public class DigitalHive : BaseWebIndexer
    {
        private string SearchUrl => SiteLink + "browse.php";
        private string LoginUrl => SiteLink + "login.php?returnto=%2F";
        private string AjaxLoginUrl => SiteLink + "takelogin.php";

        private new ConfigurationDataRecaptchaLogin configData
        {
            get => (ConfigurationDataRecaptchaLogin)base.configData;
            set => base.configData = value;
        }

        public DigitalHive(IIndexerConfigurationService configService, WebClient w, Logger l, IProtectionService ps)
            : base(name: "DigitalHive",
                description: "DigitalHive is one of the oldest general trackers",
                link: "https://www.digitalhive.org/",
                caps: new TorznabCapabilities(),
                configService: configService,
                client: w,
                logger: l,
                p: ps,
                configData: new ConfigurationDataRecaptchaLogin())
        {
            Encoding = Encoding.GetEncoding("iso-8859-1");
            Language = "en-us";
            Type = "private";

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
            var parser = new HtmlParser();
            var cq = parser.ParseDocument(loginPage.Content);
            var recaptchaSiteKey = cq.QuerySelector(".g-recaptcha")?.GetAttribute("data-sitekey");
            if (recaptchaSiteKey != null)
            {
                var result = configData;
                result.CookieHeader.Value = loginPage.Cookies;
                result.Captcha.SiteKey = recaptchaSiteKey;
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
                { "returnto" , "/" },
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

            var result = await RequestLoginAndFollowRedirect(AjaxLoginUrl, pairs, configData.CookieHeader.Value, true, SiteLink, LoginUrl);

            await ConfigureIfOK(result.Cookies, result.Content.Contains("logout.php"), () =>
            {
                var parser = new HtmlParser();
                var errorMessage = parser.ParseDocument(result.Content);
                throw new ExceptionWithConfigData(errorMessage.Text(), configData);
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var queryCollection = new NameValueCollection();
            var searchString = query.GetQueryString();
            var searchUrl = SearchUrl;

            foreach (var cat in MapTorznabCapsToTrackers(query))
            {
                queryCollection.Add("c" + cat, "1");
            }

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                queryCollection.Add("search", searchString);
            }

            queryCollection.Add("blah", "0");

            var results = await RequestStringWithCookiesAndRetry(searchUrl + "?" + queryCollection.GetQueryString());
            if (results.IsRedirect)
            {
                // re-login
                await ApplyConfiguration(null);
                results = await RequestStringWithCookiesAndRetry(searchUrl + "?" + queryCollection.GetQueryString());
            }
            try
            {
                releases.AddRange(contentToReleaseInfos(query, results.Content));
            }
            catch (Exception ex)
            {
                OnParseError(results.Content, ex);
            }

            return releases;
        }

        private IEnumerable<ReleaseInfo> contentToReleaseInfos(TorznabQuery query, string content)
        {
            var parser = new HtmlParser();
            var dom = parser.ParseDocument(content);
            var releases = new List<ReleaseInfo>();

            // Doesn't handle pagination yet...
            var rows = dom.QuerySelectorAll("div.panel-body > table.table > tbody > tr");
            foreach (var row in rows)
            {
                var release = new ReleaseInfo();
                release.MinimumRatio = 1;
                release.MinimumSeedTime = 259200;


                release.Title = row.QuerySelector("td:nth-child(2) > a").TextContent.Trim();

                if ((query.ImdbID == null || !TorznabCaps.SupportsImdbMovieSearch) && !query.MatchQueryStringAND(release.Title))
                    continue;

                release.Guid = new Uri(SiteLink + row.QuerySelector("td:nth-child(2) > a").GetAttribute("href"));
                release.Comments = release.Guid;
                release.Link = new Uri(SiteLink + row.QuerySelector("td:nth-child(3) > a").GetAttribute("href"));
                var pubDateElement = row.QuerySelector("td:nth-child(2) > span");
                // remove snatchinfo links (added after completing a torrent)
                foreach (var element in pubDateElement.QuerySelectorAll("a"))
                    element.Remove();
                var pubDate = pubDateElement.TextContent.Trim().Replace("Added: ", "");
                release.PublishDate = DateTime.Parse(pubDate).ToLocalTime();
                release.Category = MapTrackerCatToNewznab(row.QuerySelector("td:nth-child(1) > a").GetAttribute("href").Split('=')[1]);
                release.Size = ReleaseInfo.GetBytes(row.QuerySelector("td:nth-child(7)").TextContent);
                release.Seeders = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(9)").TextContent);
                release.Peers = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(10)").TextContent) + release.Seeders;

                var files = row.QuerySelector("td:nth-child(5)").TextContent;
                release.Files = ParseUtil.CoerceInt(files);

                var grabs = row.QuerySelector("td:nth-child(8)").TextContent;
                release.Grabs = ParseUtil.CoerceInt(grabs);

                if (row.QuerySelector("i.fa-star") != null)
                    release.DownloadVolumeFactor = 0;
                else
                    release.DownloadVolumeFactor = 1;

                release.UploadVolumeFactor = 1;

                releases.Add(release);
            }

            return releases;
        }
    }
}
