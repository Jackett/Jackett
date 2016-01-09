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
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace Jackett.Indexers
{
    public class DigitalHive : BaseIndexer, IIndexer
    {
        private string SearchUrl { get { return SiteLink + "browse.php"; } }
        private string LoginUrl { get { return SiteLink + "login.php?returnto=%2F"; } }
        private string AjaxLoginUrl { get { return SiteLink + "takelogin.php"; } }

        new ConfigurationDataRecaptchaLogin configData
        {
            get { return (ConfigurationDataRecaptchaLogin)base.configData; }
            set { base.configData = value; }
        }

        public DigitalHive(IIndexerManagerService i, Logger l, IWebClient w, IProtectionService ps)
            : base(name: "DigitalHive",
                description: "DigitalHive is one of the oldest general trackers",
                link: "https://www.digitalhive.org/",
                caps: new TorznabCapabilities(),
                manager: i,
                client: w,
                logger: l,
                p: ps,
                configData: new ConfigurationDataRecaptchaLogin())
        {
            AddCategoryMapping(0, TorznabCatType.AllCats);
            AddCategoryMapping(48, TorznabCatType.AllCats); // 0Day
            AddCategoryMapping(56, TorznabCatType.XXXImageset); // 0Day-Imagesets
            AddCategoryMapping(6, TorznabCatType.Audio); // 0Day-Music
            AddCategoryMapping(51, TorznabCatType.XXX); // 0Day-XXX
            AddCategoryMapping(2, TorznabCatType.TVAnime); // Anime
            AddCategoryMapping(59, TorznabCatType.MoviesBluRay); // BluRay
            AddCategoryMapping(40, TorznabCatType.TVDocumentary); // Documentary
            AddCategoryMapping(20, TorznabCatType.MoviesDVD); // DVD-R
            AddCategoryMapping(25, TorznabCatType.BooksEbook); // Ebooks
            AddMultiCategoryMapping(38, TorznabCatType.PCPhoneIOS, TorznabCatType.PCPhoneAndroid, TorznabCatType.PCPhoneOther); // HandHeld
            AddMultiCategoryMapping(37, TorznabCatType.TVOTHER, TorznabCatType.OtherMisc); // Kids Stuff
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
            AddMultiCategoryMapping(17, TorznabCatType.ConsolePS3, TorznabCatType.ConsolePS4, TorznabCatType.ConsolePSVita, TorznabCatType.ConsolePSP); // Playstation
            AddCategoryMapping(28, TorznabCatType.ConsolePSP); // PSP
            AddCategoryMapping(34, TorznabCatType.TVOTHER); // TV Pack
            AddCategoryMapping(32, TorznabCatType.TVHD); // TV-HD
            AddCategoryMapping(55, TorznabCatType.TVOTHER); // TV-HDRip
            AddCategoryMapping(7, TorznabCatType.TVSD); // TV-SD
            AddCategoryMapping(57, TorznabCatType.TVOTHER); // TV-SDRip
            AddMultiCategoryMapping(33, TorznabCatType.ConsoleWii, TorznabCatType.ConsoleWiiU); // WII
            AddMultiCategoryMapping(45, TorznabCatType.ConsoleXbox, TorznabCatType.ConsoleXbox360, TorznabCatType.ConsoleXBOX360DLC, TorznabCatType.ConsoleXboxOne); // XBox
            AddCategoryMapping(9, TorznabCatType.XXX); // XXX
            AddCategoryMapping(52, TorznabCatType.XXXOther); // XXX-ISO
        }

        public override async Task<ConfigurationData> GetConfigurationForSetup()
        {
            var loginPage = await RequestStringWithCookies(LoginUrl, string.Empty);
            string recaptchaSiteKey = new Regex(@"<div class=""g-recaptcha"" data-sitekey=""([0-9A-Za-z]{5,60})"">").Match(loginPage.Content).Groups[1].ToString().Trim();
            var result = new ConfigurationDataRecaptchaLogin();
            result.CookieHeader.Value = loginPage.Cookies;
            result.Captcha.SiteKey = recaptchaSiteKey;
            return result;
        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);
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

            var result = await RequestLoginAndFollowRedirect(AjaxLoginUrl, pairs, configData.CookieHeader.Value, true, SiteLink, LoginUrl);

            // Not sure if I have to check for this since doing a search would also give a connected stated or not
//            if (result.RedirectingTo != "https://www.digitalhive.org/" || result.RedirectingTo != "/")
//            {
//                throw new ExceptionWithConfigData("Credentials incorrect", configData);
//            }
            return IndexerConfigurationStatus.RequiresTesting;
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            var searchString = query.GetQueryString();
            var searchUrl = SearchUrl;

            foreach (var cat in MapTorznabCapsToTrackers(query))
            {
                var queryCollection = new NameValueCollection();

                if (!string.IsNullOrWhiteSpace(searchString))
                {
                    queryCollection.Add("search", searchString);
                }

                queryCollection.Add("cat", cat);
                queryCollection.Add("blah", "0");

                releases.AddRange(await handleUrl(searchUrl + queryCollection.GetQueryString()));
            }

            return releases;
        }

        private async Task<IEnumerable<ReleaseInfo>> handleUrl(String url) 
        {
            var results = await RequestStringWithCookiesAndRetry(url);
            await FollowIfRedirect(results); // TODO Is this necessary? 
            try
            {
                return parsePage(results.Content);
            }
            catch (Exception ex)
            {
                OnParseError(results.Content, ex);
            }
        }

        private ReleaseInfo parseRow(CQ row)
        {
            var release = new ReleaseInfo();
            release.MinimumRatio = 1;
            release.MinimumSeedTime = 259200;

            release.Title = row["td:nth-child(2) > a"].First().Text().Trim();
            release.Description = release.Title;
            release.Guid = new Uri(SiteLink + row["td:nth-child(2) > a"].First().Attr("href"));
            release.Comments = release.Guid;
            release.Link = new Uri(SiteLink + row["td:nth-child(3) > a"].First().Attr("href"));
            release.PublishDate = DateTime.Parse(row["td:nth-child(2) > span"].First().Text().Trim());
            release.Category = MapTrackerCatToNewznab(row["td:nth-child(1) > a"].First().Attr("href").Split('=')[1]);
            release.Size = ReleaseInfo.GetBytes(row["td:nth-child(7)"].First().Text());
            release.Seeders = Int32.Parse(row["td:nth-child(9)"].First().Text());
            release.Peers = Int32.Parse(row["td:nth-child(10)"].First().Text()) + release.Seeders;

            return release;
        }

        private async Task<IEnumerable<ReleaseInfo>> parsePage(CQ dom) 
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            var siteLink = SiteLink;

            var rows = dom["div.panel-body > table > tbody > tr"];
            foreach (var row in rows)
            {
                releases.Add(parseRow(row.Cq()));
            }

            // Check if there is a next button
            if (dom["body > div.page-container > div.main-content > p:nth-child(5)"].Children().Last().Is("body > div.page-container > div.main-content > p:nth-child(5) > a:nth-child(10)")) 
            {
                // If so, handle and add it to the collection
                releases.AddRange(await handleUrl(siteLink + dom["body > div.page-container > div.main-content > p:nth-child(5) > a:nth-child(10)"].Attr("href")));
            }

            return releases;
        }
    }
}
