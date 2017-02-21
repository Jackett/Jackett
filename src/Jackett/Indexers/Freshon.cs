using CsQuery;
using Jackett.Indexers;
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
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI.WebControls;
using Jackett.Models.IndexerConfig;

namespace Jackett.Indexers
{
    public class Freshon : BaseIndexer, IIndexer
    {
        private string LoginUrl { get { return SiteLink + "login.php"; } }
        private string LoginPostUrl { get { return SiteLink + "login.php?action=makelogin"; } }
        private string SearchUrl { get { return SiteLink + "browse.php"; } }

        new ConfigurationDataBasicLogin configData
        {
            get { return (ConfigurationDataBasicLogin)base.configData; }
            set { base.configData = value; }
        }

        public Freshon(IIndexerManagerService i, Logger l, IWebClient c, IProtectionService ps)
            : base(name: "FreshOnTV",
                description: "Our goal is to provide the latest stuff in the TV show domain",
                link: "https://freshon.tv/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                client: c,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLogin())
        {
            Encoding = Encoding.GetEncoding("UTF-8");
            Language = "en-us";
            Type = "private";
        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value }
            };

            // Get inital cookies
            CookieHeader = string.Empty;
            var response = await RequestLoginAndFollowRedirect(LoginPostUrl, pairs, CookieHeader, true, null, LoginUrl);

            await ConfigureIfOK(response.Cookies, response.Content != null && response.Content.Contains("/logout.php"), () =>
            {
                CQ dom = response.Content;
                var messageEl = dom[".error_text"];
                var errorMessage = messageEl.Text().Trim();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            TimeZoneInfo.TransitionTime startTransition = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 3, 0, 0), 3, 5, DayOfWeek.Sunday);
            TimeZoneInfo.TransitionTime endTransition = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 4, 0, 0), 10, 5, DayOfWeek.Sunday);
            TimeSpan delta = new TimeSpan(1, 0, 0);
            TimeZoneInfo.AdjustmentRule adjustment = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(1999, 10, 1), DateTime.MaxValue.Date, delta, startTransition, endTransition);
            TimeZoneInfo.AdjustmentRule[] adjustments = { adjustment };
            TimeZoneInfo romaniaTz = TimeZoneInfo.CreateCustomTimeZone("Romania Time", new TimeSpan(2, 0, 0), "(GMT+02:00) Romania Time", "Romania Time", "Romania Daylight Time", adjustments);

            var releases = new List<ReleaseInfo>();
            string episodeSearchUrl;

            if (string.IsNullOrEmpty(query.GetQueryString()))
                episodeSearchUrl = SearchUrl;
            else
            {
                episodeSearchUrl = $"{SearchUrl}?search={HttpUtility.UrlEncode(query.GetQueryString())}&cat=0";
            }

            var results = await RequestStringWithCookiesAndRetry(episodeSearchUrl);
            try
            {
                CQ dom = results.Content;

                var rows = dom["#highlight > tbody > tr"];

                foreach (var row in rows.Skip(1))
                {
                    var release = new ReleaseInfo();

                    var qRow = row.Cq();
                    var qLink = qRow.Find("a.torrent_name_link").First();

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;
                    release.Title = qLink.Attr("title");
                    if (!query.MatchQueryStringAND(release.Title))
                        continue;

                    release.Description = release.Title;
                    release.Guid = new Uri(SiteLink + qLink.Attr("href").TrimStart('/'));
                    release.Comments = release.Guid;
                    release.Link = new Uri(SiteLink + qRow.Find("td.table_links > a").First().Attr("href").TrimStart('/'));
                    release.Category = new List<int> { TvCategoryParser.ParseTvShowQuality(release.Title) };

                    release.Seeders = ParseUtil.CoerceInt(qRow.Find("td.table_seeders").Text().Trim());
                    release.Peers = ParseUtil.CoerceInt(qRow.Find("td.table_leechers").Text().Trim()) + release.Seeders;

                    var sizeStr = qRow.Find("td.table_size")[0].Cq().Text();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    DateTime pubDateRomania;
                    var dateString = qRow.Find("td.table_added").Text().Trim();
                    if (dateString.StartsWith("Today "))
                    {   pubDateRomania = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified) + TimeSpan.Parse(dateString.Split(' ')[1]); }
                    else if (dateString.StartsWith("Yesterday "))
                    {   pubDateRomania = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified) + TimeSpan.Parse(dateString.Split(' ')[1]) - TimeSpan.FromDays(1); }
                    else
                    {   pubDateRomania = DateTime.SpecifyKind(DateTime.ParseExact(dateString, "d-MMM-yyyy HH:mm:ss", CultureInfo.InvariantCulture), DateTimeKind.Unspecified); }

                    DateTime pubDateUtc = TimeZoneInfo.ConvertTimeToUtc(pubDateRomania, romaniaTz);
                    release.PublishDate = pubDateUtc.ToLocalTime();

                    var grabs = row.Cq().Find("td.table_snatch").Get(0).FirstChild.ToString();
                    release.Grabs = ParseUtil.CoerceInt(grabs);

                    if (row.Cq().Find("img[alt=\"100% Free\"]").Any())
                        release.DownloadVolumeFactor = 0;
                    else if (row.Cq().Find("img[alt=\"50% Free\"]").Any())
                        release.DownloadVolumeFactor = 0.5;
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
