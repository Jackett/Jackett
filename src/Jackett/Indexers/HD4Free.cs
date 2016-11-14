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

            var result = await RequestLoginAndFollowRedirect(TakeLoginUrl, pairs, configData.CookieHeader.Value, true, SiteLink, LoginUrl);

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
            pairs.Add("visible", "2");

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

                    CQ qName = row["name"].ToString();
                    var qDetailsLink = qName.Find("a[href^=\"details.php?id=\"]");
                    qDetailsLink = qName.Find("div");
                    qDetailsLink = qName[0].Cq();
                    release.Title = qDetailsLink.Text();
                    //release.Description = release.Title;
                    release.Comments = new Uri(SiteLink + qDetailsLink.Attr("href"));
                    release.Guid = release.Comments;

                    release.Category = ParseUtil.CoerceInt(row["catid"].ToString());
                    /*release.Link = new Uri(SiteLink + qRow.Find("td:nth-child(3) > a").First().Attr("href"));
                    var pubDate = qRow.Find("td:nth-child(2) > span").First().Text().Trim().Replace("Added: ", "");
                    release.PublishDate = DateTime.Parse(pubDate).ToLocalTime();
                    
                    release.Size = ReleaseInfo.GetBytes(qRow.Find("td:nth-child(7)").First().Text());
                    release.Seeders = ParseUtil.CoerceInt(qRow.Find("td:nth-child(9)").First().Text());
                    release.Peers = ParseUtil.CoerceInt(qRow.Find("td:nth-child(10)").First().Text()) + release.Seeders;

                    var files = row.Cq().Find("td:nth-child(5)").Text();
                    release.Files = ParseUtil.CoerceInt(files);

                    var grabs = row.Cq().Find("td:nth-child(8)").Text();
                    release.Grabs = ParseUtil.CoerceInt(grabs);

                    if (row.Cq().Find("i.fa-star").Any())
                        release.DownloadVolumeFactor = 0;
                    else
                        release.DownloadVolumeFactor = 1;

                    release.UploadVolumeFactor = 1;
                    */
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