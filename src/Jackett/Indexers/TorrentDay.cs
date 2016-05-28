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
    public class TorrentDay : BaseIndexer, IIndexer
    {
        private string StartPageUrl { get { return SiteLink + "login.php"; } }
        private string LoginUrl { get { return SiteLink + "tak3login.php"; } }
        private string SearchUrl { get { return SiteLink + "browse.php"; } }

        new ConfigurationDataRecaptchaLogin configData
        {
            get { return (ConfigurationDataRecaptchaLogin)base.configData; }
            set { base.configData = value; }
        }

        public TorrentDay(IIndexerManagerService i, Logger l, IWebClient wc, IProtectionService ps)
            : base(name: "TorrentDay",
                description: "TorrentDay",
                link: "https://tdonline.org/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataRecaptchaLogin())
        {

            AddCategoryMapping(29, TorznabCatType.TVAnime);
            AddCategoryMapping(28, TorznabCatType.PC);
            AddCategoryMapping(28, TorznabCatType.AudioAudiobook);
            AddCategoryMapping(20, TorznabCatType.Books);
            AddCategoryMapping(30, TorznabCatType.TVDocumentary);
            //Freelech
            //Mac

            AddCategoryMapping(25, TorznabCatType.MoviesSD);
            AddCategoryMapping(11, TorznabCatType.MoviesHD);
            AddCategoryMapping(5, TorznabCatType.MoviesHD);
            AddCategoryMapping(3, TorznabCatType.MoviesSD);
            AddCategoryMapping(21, TorznabCatType.MoviesSD);
            AddCategoryMapping(22, TorznabCatType.MoviesForeign);
            // Movie packs
            AddCategoryMapping(44, TorznabCatType.MoviesSD);
            AddCategoryMapping(1, TorznabCatType.MoviesSD);

            // Music
            AddCategoryMapping(17, TorznabCatType.AudioMP3);
            AddCategoryMapping(44, TorznabCatType.AudioLossless);
            AddCategoryMapping(23, TorznabCatType.AudioForeign);
            AddCategoryMapping(41, TorznabCatType.AudioOther);
            AddCategoryMapping(16, TorznabCatType.AudioVideo);

            AddCategoryMapping(4, TorznabCatType.PCGames);
            // ps3
            // psp
            // wii
            // 360

            AddCategoryMapping(24, TorznabCatType.TVSD);
            AddCategoryMapping(32, TorznabCatType.TVHD);
            AddCategoryMapping(31, TorznabCatType.TVSD);
            AddCategoryMapping(33, TorznabCatType.TVSD);
            AddCategoryMapping(14, TorznabCatType.TVHD);
            AddCategoryMapping(26, TorznabCatType.TVSD);
            AddCategoryMapping(7, TorznabCatType.TVHD);
            AddCategoryMapping(2, TorznabCatType.TVSD);

            AddCategoryMapping(6, TorznabCatType.XXX);
            AddCategoryMapping(15, TorznabCatType.XXX);
        }

        public override async Task<ConfigurationData> GetConfigurationForSetup()
        {
            var loginPage = await RequestStringWithCookies(StartPageUrl, string.Empty);
            CQ cq = loginPage.Content;
            var result = new ConfigurationDataRecaptchaLogin();
            result.CookieHeader.Value = loginPage.Cookies;
            result.Captcha.SiteKey = cq.Find(".g-recaptcha").Attr("data-sitekey");
            return result;
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
                // Cookie was manually supplied
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

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, configData.CookieHeader.Value, true, SiteLink, LoginUrl);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("logout.php"), () =>
            {
                CQ dom = result.Content;
                var messageEl = dom["#login"];
                messageEl.Children("form").Remove();
                var errorMessage = messageEl.Text().Trim();

                if (string.IsNullOrWhiteSpace(errorMessage))
                {
                    errorMessage = dom.Text();
                }

                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();
            var queryUrl = SearchUrl;
            var queryCollection = new NameValueCollection();

            if (!string.IsNullOrWhiteSpace(searchString))
                queryCollection.Add("search", searchString);

            foreach (var cat in MapTorznabCapsToTrackers(query))
                queryCollection.Add("c" + cat, "1");

            if (queryCollection.Count > 0)
                queryUrl += "?" + queryCollection.GetQueryString();

            var results = await RequestStringWithCookiesAndRetry(queryUrl);

            // Check for being logged out
            if (results.IsRedirect)
                throw new AuthenticationException();

            try
            {
                CQ dom = results.Content;
                var rows = dom["#torrentTable > tbody > tr.browse"];
                foreach (var row in rows)
                {
                    CQ qRow = row.Cq();
                    var release = new ReleaseInfo();

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;
                    release.Title = qRow.Find(".torrentName").Text();
                    release.Description = release.Title;
                    release.Guid = new Uri(SiteLink + qRow.Find(".torrentName").Attr("href"));
                    release.Comments = release.Guid;
                    release.Link = new Uri(SiteLink + qRow.Find(".dlLinksInfo > a").Attr("href"));

                    var sizeStr = qRow.Find(".sizeInfo").Text();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    var dateStr = qRow.Find(".ulInfo").Text().Split('|').Last().Trim();
                    var agoIdx = dateStr.IndexOf("ago");
                    if (agoIdx > -1)
                    {
                        dateStr = dateStr.Substring(0, agoIdx);
                    }
                    release.PublishDate = DateTimeUtil.FromTimeAgo(dateStr);

                    release.Seeders = ParseUtil.CoerceInt(qRow.Find(".seedersInfo").Text());
                    release.Peers = ParseUtil.CoerceInt(qRow.Find(".leechersInfo").Text()) + release.Seeders;

                    var cat = qRow.Find("td:eq(0) a").First().Attr("href").Substring(15);//browse.php?cat=24
                    release.Category = MapTrackerCatToNewznab(cat);

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
