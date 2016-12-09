using CsQuery;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Jackett.Models.IndexerConfig;
using System.Collections.Specialized;

namespace Jackett.Indexers
{
    //
    //     Quick and dirty indexer for GFTracker.  
    // 
    public class GFTracker : BaseIndexer, IIndexer
    {
        private string StartPageUrl { get { return SiteLink + "login.php?returnto=%2F"; } }
        private string LoginUrl { get { return SiteLink + "loginsite.php"; } }
        private string SearchUrl { get { return SiteLink + "browse.php"; } }

        new ConfigurationDataRecaptchaLogin configData
        {
            get { return (ConfigurationDataRecaptchaLogin)base.configData; }
            set { base.configData = value; }
        }

        public GFTracker(IIndexerManagerService i, Logger l, IWebClient w, IProtectionService ps)
            : base(name: "GFTracker",
                description: "Home of user happiness",
                link: "https://www.thegft.org/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                client: w,
                logger: l,
                p: ps,
                configData: new ConfigurationDataRecaptchaLogin())
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";

            AddCategoryMapping(4, TorznabCatType.TV);               // TV/XVID
            AddCategoryMapping(17, TorznabCatType.TVHD);            // TV/X264
            AddCategoryMapping(19, TorznabCatType.TV);              // TV/DVDRIP
            AddCategoryMapping(26, TorznabCatType.TVHD);            // TV/BLURAY
            AddCategoryMapping(37, TorznabCatType.TV);              // TV/DVDR
            AddCategoryMapping(47, TorznabCatType.TV);              // TV/SD

            AddCategoryMapping(7, TorznabCatType.Movies);           // Movies/XVID
            AddCategoryMapping(8, TorznabCatType.MoviesDVD);        // Movies/DVDR
            AddCategoryMapping(12, TorznabCatType.MoviesBluRay);    // Movies/BLURAY
            AddCategoryMapping(18, TorznabCatType.MoviesHD);        // Movies/X264-HD
            AddCategoryMapping(49, TorznabCatType.MoviesSD);        // Movies/X264-SD
        }

        public override async Task<ConfigurationData> GetConfigurationForSetup()
        {
            var loginPage = await RequestStringWithCookies(StartPageUrl, string.Empty);
            CQ cq = loginPage.Content;
            var result = this.configData;
            CQ recaptcha = cq.Find(".g-recaptcha").Attr("data-sitekey");
            if(recaptcha.Length != 0)   // recaptcha not always present in login form, perhaps based on cloudflare uid or just phase of the moon
            {
                result.CookieHeader.Value = loginPage.Cookies;
                result.Captcha.SiteKey = cq.Find(".g-recaptcha").Attr("data-sitekey");
                result.Captcha.Version = "2";
                return result;
            } else
            {
                var stdResult = new ConfigurationDataBasicLogin();
                stdResult.Username.Value = configData.Username.Value;
                stdResult.Password.Value = configData.Password.Value;
                stdResult.CookieHeader.Value = loginPage.Cookies;
                return stdResult;
            }           
        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);
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

            var cookieJar = configData.CookieHeader.Value;
            var response = await RequestLoginAndFollowRedirect(LoginUrl, pairs, configData.CookieHeader.Value, true, null, StartPageUrl);
            cookieJar += response.Cookies.ToString();
            response = await RequestStringWithCookiesAndRetry(SearchUrl, cookieJar);

            await ConfigureIfOK(cookieJar, response.Content != null && response.Content.Contains("logout.php"), () =>
            {
                CQ dom = response.Content;
                var messageEl = dom["h2"].Last();
                messageEl.Children("a").Remove();
                messageEl.Children("style").Remove();
                var errorMessage = messageEl.Text().Trim();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();
            var queryCollection = new NameValueCollection();

            queryCollection.Add("view", "0");
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                queryCollection.Add("search", searchString);
            }

            foreach (var cat in MapTorznabCapsToTrackers(query))
            {
                queryCollection.Add(string.Format("c{0}", cat), "1");
            }

            var searchUrl = SearchUrl + "?" + queryCollection.GetQueryString();

            var results = await RequestStringWithCookiesAndRetry(searchUrl, CookieHeader);
            try
            {
                CQ dom = results.Content;
                var rows = dom["#torrentBrowse > table > tbody > tr"];
                foreach (var row in rows.Skip(1))
                {
                    var release = new ReleaseInfo();
                    CQ qRow = row.Cq();

                    //CQ qLink = qRow.Children().ElementAt(1).Cq().Children("a").ElementAt(1).Cq();
                    CQ qLink;
                    CQ qTmp = qRow.Children().ElementAt(1).Cq().Find("a");                    
                    if (qTmp.Length < 2) {
                        qLink = qRow.Children().ElementAt(1).Cq().Find("a").ElementAt(0).Cq();
                    } else {
                        qLink = qRow.Children().ElementAt(1).Cq().Find("a").ElementAt(1).Cq();
                    }

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;
                    release.Title = qLink.Attr("title");
                    release.Description = release.Title;
                    release.Guid = new Uri(SiteLink + qLink.Attr("href").TrimStart('/'));
                    release.Comments = release.Guid;

                    qLink = qRow.Children().ElementAt(3).Cq().Children("a").First();
                    release.Link = new Uri(string.Format("{0}{1}", SiteLink, qLink.Attr("href")));

                    var catUrl = qRow.Children().ElementAt(0).FirstElementChild.Cq().Attr("href");
                    var catNum = catUrl.Split(new char[] { '=', '&' })[2].Replace("c", "");
                    release.Category = MapTrackerCatToNewznab(catNum);

                    var dateString = qRow.Children().ElementAt(6).Cq().Text().Trim();
                    //var pubDate = DateTime.ParseExact(dateString, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    release.PublishDate = Jackett.Utils.DateTimeUtil.FromTimeAgo(dateString);

                    var sizeStr = qRow.Children().ElementAt(7).Cq().Text().Split(new char[] { '/' })[0];
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(qRow.Children().ElementAt(8).Cq().Text().Split(new char[] { '/' })[0].Trim());
                    release.Peers = ParseUtil.CoerceInt(qRow.Children().ElementAt(8).Cq().Text().Split(new char[] { '/' })[1].Trim()) + release.Seeders;

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
