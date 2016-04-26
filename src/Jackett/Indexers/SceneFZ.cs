using Jackett.Utils.Clients;
using NLog;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Models;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using CsQuery;
using System.Web;
using System;
using System.Globalization;
using Jackett.Models.IndexerConfig;

namespace Jackett.Indexers
{
    public class SceneFZ : BaseIndexer, IIndexer
    {
        string LoginUrl { get { return SiteLink + "takelogin.php"; } }

        string BrowseUrl { get { return SiteLink + "ajax_browse.php"; } }

        new ConfigurationDataBasicLogin configData
        {
            get { return (ConfigurationDataBasicLogin)base.configData; }
            set { base.configData = value; }
        }

        public SceneFZ(IIndexerManagerService i, IWebClient wc, Logger l, IProtectionService ps)
            : base(name: "SceneFZ",
                   description: "Torrent tracker. Tracking over 50.000 torrent files.",
                   link: "http://scenefz.net/",
                   caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                   manager: i,
                   client: wc,
                   logger: l,
                   p: ps,
                   configData: new ConfigurationDataBasicLogin())
        {
            AddCategoryMapping(32, TorznabCatType.Movies);
            AddCategoryMapping(33, TorznabCatType.TV);
        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string>
            {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, LoginUrl);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("Please wait..."), () =>
                {
                    CQ dom = result.Content;
                    var errorMessage = dom[".tableinborder:eq(1) td"].Text().Trim();
                    throw new ExceptionWithConfigData(errorMessage, configData);
                });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchUrl = BrowseUrl;
            var searchString = query.GetQueryString();

            var cats = MapTorznabCapsToTrackers(query);
            string cat = "0";

            if (cats.Count == 1)
            {
                cat = cats[0];
            }

            if (!string.IsNullOrWhiteSpace(searchString) || cat != "0")
                searchUrl += string.Format("?search={0}&param_val=0&complex_search=0&incldead=mc{1}&orderby=added&sort=desc", HttpUtility.UrlEncode(searchString), cat);

            var response = await RequestStringWithCookiesAndRetry(searchUrl, null, BrowseUrl);
            var results = response.Content;
            try
            {
                CQ dom = results;
                var rows = dom["td#browse-middle-td"];

                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();
                    var qRow = row.Cq();
                    var qTitleLink = qRow.Find("table tbody tr:eq(0) td a").First();
                    release.Title = qRow.Find("table tbody tr:eq(0) td a b").Text().Trim();
                    release.Description = release.Title;
                    release.Guid = new Uri(SiteLink + qTitleLink.Attr("href"));
                    release.Comments = release.Guid;

                    //24.04.2016 16:44:57
                    var dateStr = qRow.Find("table tbody tr:eq(1) td:eq(4)").Html().Replace("&nbsp;", " ").Trim();
                    release.PublishDate = DateTime.ParseExact(dateStr, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture).AddHours(-2);

                    var qLink = qRow.First().Next().Find("a");
                    release.Link = new Uri(SiteLink + qLink.Attr("href"));

                    var sizeStr = qRow.Find("table tbody tr:eq(1) td b").Text().Trim();
                    release.Size = ReleaseInfo.GetBytes(sizeStr.Replace(",", "."));

                    release.Seeders = ParseUtil.CoerceInt(qRow.Find("table tbody tr:eq(1) td:eq(1) b:eq(0) font").Text().Trim());
                    release.Peers = ParseUtil.CoerceInt(qRow.Find("table tbody tr:eq(1) td:eq(1) b:eq(1) font").Text().Trim()) + release.Seeders;

                    var catId = qRow.First().Prev().Find("a").Attr("onclick").Substring(21, 2);
                    release.Category = MapTrackerCatToNewznab(catId);

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results, ex);
            }

            return releases;
        }
    }
}

