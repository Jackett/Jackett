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
using System.Threading.Tasks;
using System.Web;
using Jackett.Models.IndexerConfig;
using System.Text.RegularExpressions;

namespace Jackett.Indexers
{
    public class Hebits : BaseIndexer, IIndexer
    {
        private string LoginUrl { get { return SiteLink + "login.php"; } }
        private string LoginPostUrl { get { return SiteLink + "takeloginAjax.php"; } }
        private string SearchUrl { get { return SiteLink + "browse.php?sort=4&type=desc"; } }

        new ConfigurationDataBasicLogin configData
        {
            get { return (ConfigurationDataBasicLogin)base.configData; }
            set { base.configData = value; }
        }

        public Hebits(IIndexerManagerService i, Logger l, IWebClient wc, IProtectionService ps)
            : base(name: "Hebits",
                description: "The Israeli Tracker",
                link: "https://hebits.net/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                client: wc,
                logger: l,
                p: ps,
                downloadBase: "https://hebits.net/",
                configData: new ConfigurationDataBasicLogin())
        {

            AddCategoryMapping(19, TorznabCatType.MoviesSD);
            AddCategoryMapping(25, TorznabCatType.MoviesOther); // Israeli Content
            AddCategoryMapping(20, TorznabCatType.MoviesDVD);
            AddCategoryMapping(36, TorznabCatType.MoviesBluRay);
            AddCategoryMapping(27, TorznabCatType.MoviesHD);

            AddCategoryMapping(7, TorznabCatType.TVSD); // Israeli SDTV
            AddCategoryMapping(24, TorznabCatType.TVSD); // English SDTV
            AddCategoryMapping(1, TorznabCatType.TVHD); // Israel HDTV
            AddCategoryMapping(37, TorznabCatType.TVHD); // Israel HDTV
        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value }
            };

            // Get inital cookies
            CookieHeader = string.Empty;
            var result = await RequestLoginAndFollowRedirect(LoginPostUrl, pairs, CookieHeader, true, null, SiteLink);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("OK"), () =>
            {
                CQ dom = result.Content;
                var messageEl = dom["#errorMsg"].Last();
                var errorMessage = messageEl.Text().Trim();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();
            var searchUrl = SearchUrl;

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchUrl += "&search=" + HttpUtility.UrlEncode(searchString);
            }
            string.Format(SearchUrl, HttpUtility.UrlEncode(searchString));

            var cats = MapTorznabCapsToTrackers(query);
            if (cats.Count > 0)
            {
                foreach (var cat in cats)
                {
                    searchUrl += "&c" + cat + "=1";
                }
            }

            var results = await RequestStringWithCookiesAndRetry(searchUrl);
            try
            {
                CQ dom = results.Content;

                CQ qRows = dom[".browse > div > div"];

                foreach (var row in qRows)
                {
                    var release = new ReleaseInfo();

                    var qRow = row.Cq();

                    var debug = qRow.Html();

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;

                    release.Title = qRow.Find(".bTitle").Text().Split('/')[1].Trim();
                    release.Link = new Uri(SiteLink + qRow.Find("a").Attr("href"));
                    release.Guid = release.Link;

                    var dateString = qRow.Find("div:last-child").Text().Trim();
                    var pattern = "\\d{4}-\\d{2}-\\d{2} \\d{2}:\\d{2}:\\d{2}";
                    var match = Regex.Match(dateString, pattern);
                    if (match.Success)
                    {
                        release.PublishDate = DateTime.ParseExact(match.Value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    }

                    var sizeStr = qRow.Find(".bSize").Text();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);
                    release.Seeders = ParseUtil.CoerceInt(qRow.Find(".bUping").Text().Trim());
                    release.Peers = release.Seeders + ParseUtil.CoerceInt(qRow.Find(".bDowning").Text().Trim());

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
