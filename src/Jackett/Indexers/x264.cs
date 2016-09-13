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

namespace Jackett.Indexers
{
    public class x264 : BaseIndexer, IIndexer
    {
        private string SearchUrl { get { return SiteLink + "browse.php"; } }
        private string LoginUrl { get { return SiteLink + "login.php"; } }
        private string SubmitLoginUrl { get { return SiteLink + "takelogin.php"; } }

        new ConfigurationDataRecaptchaLogin configData
        {
            get { return (ConfigurationDataRecaptchaLogin)base.configData; }
            set { base.configData = value; }
        }

        public x264(IIndexerManagerService i, Logger l, IWebClient w, IProtectionService ps)
            : base(name: "x264",
                description: "A movie/TV tracker",
                link: "https://x264.me/",
                caps: new TorznabCapabilities(),
                manager: i,
                client: w,
                logger: l,
                p: ps,
                configData: new ConfigurationDataRecaptchaLogin())
        {
            AddCategoryMapping(20, TorznabCatType.Movies); // Movies&TV/Sources
            AddCategoryMapping(53, TorznabCatType.MoviesHD); // Movies/1080p
            AddCategoryMapping(30, TorznabCatType.MoviesHD); // Movies/576p
            AddCategoryMapping(50, TorznabCatType.MoviesHD); // Movies/720p
            AddCategoryMapping(33, TorznabCatType.MoviesSD); // Movies/SD
            AddCategoryMapping(54, TorznabCatType.TVHD); // TV/1080p
            AddCategoryMapping(31, TorznabCatType.TVHD); // TV/576p
            AddCategoryMapping(51, TorznabCatType.TVHD); // TV/720p
            AddCategoryMapping(25, TorznabCatType.TVSD); // TV/SD
        }

        public override async Task<ConfigurationData> GetConfigurationForSetup()
        {
            var loginPage = await RequestStringWithCookies(LoginUrl, string.Empty);
            CQ dom = loginPage.Content;
            CQ recaptchaScript = dom.Find("script").First();

            string recaptchaSiteKey = recaptchaScript.Attr("src").Split('=')[1];
            var result = new ConfigurationDataRecaptchaLogin();
            result.CookieHeader.Value = loginPage.Cookies;
            result.Captcha.SiteKey = recaptchaSiteKey;
            result.Captcha.Version = "1";
            return result;
        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "recaptcha_challenge_field", configData.Captcha.Challenge },
                { "recaptcha_response_field", configData.Captcha.Value },
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

            var result = await RequestLoginAndFollowRedirect(SubmitLoginUrl, pairs, configData.CookieHeader.Value, true, null, LoginUrl);
            await ConfigureIfOK(result.Cookies, result.Content.Contains("logout.php"), () =>
            {
                var errorMessage = result.Content;
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            var searchString = query.GetQueryString();
            var searchUrl = SearchUrl;
            var queryCollection = new NameValueCollection();
            queryCollection.Add("incldead", "1");
            queryCollection.Add("xtype", "0");
            queryCollection.Add("stype", "0");

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                queryCollection.Add("search", searchString);
            }

            foreach (var cat in MapTorznabCapsToTrackers(query))
            {
                queryCollection.Add("c" + cat, "1");
            }

            searchUrl += "?" + queryCollection.GetQueryString();

            var results = await RequestStringWithCookiesAndRetry(searchUrl);
            try
            {
                CQ dom = results.Content;
                var rows = dom["table > tbody > tr[height=36]"];
                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 7 * 24 * 60 * 60;
                    
                    var qRow = row.Cq();
                    var qCatLink = qRow.Find("a[href^=?cat]").First();
                    var qDetailsLink = qRow.Find("a[href^=details.php]").First();
                    var qSeeders = qRow.Find("td:eq(8)");
                    var qLeechers = qRow.Find("td:eq(9)");
                    var qDownloadLink = qRow.Find("a[href^=\"download.php\"]").First();
                    var qImdbLink = qRow.Find("a[href^=/redir.php?url=http://www.imdb.com]");
                    var qSize = qRow.Find("td:eq(6)");

                    var catStr = qCatLink.Attr("href").Split('=')[1];
                    release.Category = MapTrackerCatToNewznab(catStr);

                    release.Link = new Uri(SiteLink + qDownloadLink.Attr("href"));
                    release.Title = qDetailsLink.Text().Trim();
                    release.Comments = new Uri(SiteLink + qDetailsLink.Attr("href"));
                    release.Guid = release.Link;

                    var sizeStr = qSize.Text();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    if(qImdbLink.Length == 1) { 
                        var ImdbId = qImdbLink.Attr("href").Split('/').Last().Substring(2);
                        release.Imdb = ParseUtil.CoerceLong(ImdbId);
                    }

                    release.Seeders = ParseUtil.CoerceInt(qSeeders.Text());
                    release.Peers = ParseUtil.CoerceInt(qLeechers.Text()) + release.Seeders;

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
