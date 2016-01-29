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
using System.Threading.Tasks;
using System.Web;
using Jackett.Models.IndexerConfig;
using AngleSharp;

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
                configData: new ConfigurationDataBasicLogin("For best results, change the 'Torrents per page' setting to 100 in your profile on the FreshOn webpage."))
        {
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
            var response = await RequestLoginAndFollowRedirect(LoginPostUrl, pairs, CookieHeader, true, null, LoginUrl);

            await ConfigureIfOK(response.Cookies, response.Content != null && response.Content.Contains("/logout.php"), () =>
            {
                var parser = new AngleSharp.Parser.Html.HtmlParser();
                var document = parser.Parse(response.Content);
                var messageEl = document.QuerySelector(".error_text");
                var errorMessage = messageEl.TextContent.Trim();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            string Url;
            if (string.IsNullOrEmpty(query.GetQueryString()))
                Url = SearchUrl;
            else
            {
                Url = $"{SearchUrl}?search={HttpUtility.UrlEncode(query.GetQueryString())}&cat=0";
            }

            var response = await RequestStringWithCookiesAndRetry(Url);
            List<ReleaseInfo> releases = ParseResponse(response.Content);

            return releases;
        }

        private List<ReleaseInfo> ParseResponse(string htmlResponse)
        {
            TimeZoneInfo.TransitionTime startTransition = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 3, 0, 0), 3, 5, DayOfWeek.Sunday);
            TimeZoneInfo.TransitionTime endTransition = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 4, 0, 0), 10, 5, DayOfWeek.Sunday);
            TimeSpan delta = new TimeSpan(1, 0, 0);
            TimeZoneInfo.AdjustmentRule adjustment = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(1999, 10, 1), DateTime.MaxValue.Date, delta, startTransition, endTransition);
            TimeZoneInfo.AdjustmentRule[] adjustments = { adjustment };
            TimeZoneInfo romaniaTz = TimeZoneInfo.CreateCustomTimeZone("Romania Time", new TimeSpan(2, 0, 0), "(GMT+02:00) Romania Time", "Romania Time", "Romania Daylight Time", adjustments);

            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            try
            {
                var parser = new AngleSharp.Parser.Html.HtmlParser();
                var document = parser.Parse(htmlResponse);
                var rows = document.QuerySelectorAll("#highlight > tbody > tr:not(:First-child)");

                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();

                    var linkNameElement = row.QuerySelector("a.torrent_name_link");

                    release.Title = linkNameElement.GetAttribute("title");
                    release.Description = release.Title;
                    release.Guid = new Uri(SiteLink + linkNameElement.GetAttribute("href"));
                    release.Comments = release.Guid;
                    release.Link = new Uri(SiteLink + row.QuerySelector("td.table_links > a").GetAttribute("href"));
                    release.Category = TvCategoryParser.ParseTvShowQuality(release.Title);
                    release.Seeders = ParseUtil.CoerceInt(row.QuerySelector("td.table_seeders").TextContent.Trim());
                    release.Peers = ParseUtil.CoerceInt(row.QuerySelector("td.table_leechers").TextContent.Trim()) + release.Seeders;
                    release.Size = ReleaseInfo.GetBytes(row.QuerySelector("td.table_size").TextContent);
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;

                    DateTime pubDateRomania;
                    var dateString = row.QuerySelector("td.table_added").TextContent.Trim();
                    if (dateString.StartsWith("Today "))
                    { pubDateRomania = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified) + TimeSpan.Parse(dateString.Split(' ')[1]); }
                    else if (dateString.StartsWith("Yesterday "))
                    { pubDateRomania = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified) + TimeSpan.Parse(dateString.Split(' ')[1]) - TimeSpan.FromDays(1); }
                    else
                    { pubDateRomania = DateTime.SpecifyKind(DateTime.ParseExact(dateString, "d-MMM-yyyy HH:mm:ss", CultureInfo.InvariantCulture), DateTimeKind.Unspecified); }

                    DateTime pubDateUtc = TimeZoneInfo.ConvertTimeToUtc(pubDateRomania, romaniaTz);
                    release.PublishDate = pubDateUtc.ToLocalTime();

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(htmlResponse, ex);
            }

            return releases;
        }
    }
}
