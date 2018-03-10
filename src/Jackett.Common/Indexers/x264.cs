using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
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
    public class x264 : BaseWebIndexer
    {
        private string SearchUrl { get { return SiteLink + "browse.php"; } }
        private string LoginUrl { get { return SiteLink + "login.php"; } }
        private string SubmitLoginUrl { get { return SiteLink + "takelogin.php"; } }

        private new ConfigurationDataRecaptchaLogin configData
        {
            get { return (ConfigurationDataRecaptchaLogin)base.configData; }
            set { base.configData = value; }
        }

        public x264(IIndexerConfigurationService configService, WebClient w, Logger l, IProtectionService ps)
            : base(name: "x264",
                description: "A movie/TV tracker",
                link: "https://x264.me/",
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

            TorznabCaps.SupportsImdbSearch = true;

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

            var result = this.configData;
            var captcha = dom.Find(".g-recaptcha");
            result.CookieHeader.Value = loginPage.Cookies;
            result.Captcha.SiteKey = captcha.Attr("data-sitekey");
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

            var result = await RequestLoginAndFollowRedirect(SubmitLoginUrl, pairs, configData.CookieHeader.Value, true, null, LoginUrl);
            await ConfigureIfOK(result.Cookies, result.Content.Contains("logout.php"), () =>
            {
                var errorMessage = result.Content;
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            var searchString = query.GetQueryString();
            var searchUrl = SearchUrl;
            var queryCollection = new NameValueCollection();
            queryCollection.Add("incldead", "1");
            queryCollection.Add("xtype", "0");
            queryCollection.Add("stype", "0");

            if (query.ImdbID != null)
            {
                queryCollection.Add("search", query.ImdbID);
            }
            else if (!string.IsNullOrWhiteSpace(searchString))
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

                var sideWideFreeLeech = false;
                if (dom.Find("td > b > font[color=\"white\"]:contains(Free Leech)").Length >= 1)
                    sideWideFreeLeech = true;

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

                    if (qImdbLink.Length == 1)
                    {
                        var ImdbId = qImdbLink.Attr("href").Split('/').Last().Substring(2);
                        release.Imdb = ParseUtil.CoerceLong(ImdbId);
                    }

                    release.Seeders = ParseUtil.CoerceInt(qSeeders.Text());
                    release.Peers = ParseUtil.CoerceInt(qLeechers.Text()) + release.Seeders;

                    var files = qRow.Find("td:nth-child(3)").Text();
                    release.Files = ParseUtil.CoerceInt(files);

                    var grabs = qRow.Find("td:nth-child(8)").Get(0).FirstChild.ToString();
                    release.Grabs = ParseUtil.CoerceInt(grabs);

                    if (sideWideFreeLeech || qRow.Find("font[color=\"red\"]:contains(FREE)").Length >= 1)
                        release.DownloadVolumeFactor = 0;
                    else
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
