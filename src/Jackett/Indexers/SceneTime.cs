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
using System.Text.RegularExpressions;
using System.Collections.Specialized;

namespace Jackett.Indexers
{
    public class SceneTime : BaseIndexer, IIndexer
    {
        private string LoginUrl { get { return SiteLink + "takelogin.php"; } }
        private string SearchUrl { get { return SiteLink + "browse_API.php"; } }
        private string DownloadUrl { get { return SiteLink + "download.php/{0}/download.torrent"; } }

        new ConfigurationDataBasicLogin configData
        {
            get { return (ConfigurationDataBasicLogin)base.configData; }
            set { base.configData = value; }
        }

        public SceneTime(IIndexerManagerService i, Logger l, IWebClient w, IProtectionService ps)
            : base(name: "SceneTime",
                description: "Always on time",
                link: "https://www.scenetime.com/",
                caps: new TorznabCapabilities(),
                manager: i,
                client: w,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLogin("For best results, change the 'Torrents per page' setting to the maximum in your profile on the SceneTime webpage."))
        {
            AddCategoryMapping(77, TorznabCatType.TVSD);
            AddCategoryMapping(9, TorznabCatType.TVHD);
        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, LoginUrl);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("logout.php"), () =>
            {
                CQ dom = result.Content;
                var errorMessage = dom["td.text"].Text().Trim();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }


        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            NameValueCollection qParams = new NameValueCollection();
            qParams.Add("cata", "yes");
            qParams.Add("sec", "jax");

            List<string> catList = MapTorznabCapsToTrackers(query);
            foreach (string cat in catList)
            {
                qParams.Add("c" + cat, "1");
            }

            if (!string.IsNullOrEmpty(query.SanitizedSearchTerm))
            {
                qParams.Add("search", query.GetQueryString());
            }

            string torrentSearchUrl = $"{SearchUrl}?{qParams.GetQueryString()}";

            var results = await RequestStringWithCookiesAndRetry(torrentSearchUrl);
            List<ReleaseInfo> releases = ParseResponse(results.Content);
            
            return releases;
        }

        public List<ReleaseInfo> ParseResponse(string htmlResponse)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            try
            {
                CQ dom = htmlResponse;

                List<string> headerColumns = dom["table[class*='movehere']"].First().Find("tbody > tr > td[class='cat_Head']").Select(x => x.Cq().Text()).ToList();
                int categoryIndex = headerColumns.FindIndex(x => x.Equals("Type"));
                int nameIndex = headerColumns.FindIndex(x => x.Equals("Name"));
                int sizeIndex = headerColumns.FindIndex(x => x.Equals("Size"));
                int seedersIndex = headerColumns.FindIndex(x => x.Equals("Seeders"));
                int leechersIndex = headerColumns.FindIndex(x => x.Equals("Leechers"));

                var rows = dom["tr.browse"];
                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;

                    var categoryCol = row.ChildElements.ElementAt(categoryIndex);
                    string catLink = categoryCol.Cq().Find("a").Attr("href");
                    string catId = new Regex(@"\?cat=(\d*)").Match(catLink).Groups[1].ToString().Trim();
                    release.Category = MapTrackerCatToNewznab(catId);

                    var descCol = row.ChildElements.ElementAt(nameIndex);
                    var qDescCol = descCol.Cq();
                    var qLink = qDescCol.Find("a");
                    release.Title = qLink.Text();
                    release.Description = release.Title;
                    release.Comments = new Uri(SiteLink + "/" + qLink.Attr("href"));
                    release.Guid = release.Comments;
                    var torrentId = qLink.Attr("href").Split('=')[1];
                    release.Link = new Uri(string.Format(DownloadUrl, torrentId));

                    release.PublishDate = DateTimeUtil.FromTimeAgo(descCol.ChildNodes.Last().InnerText);

                    var sizeStr = row.ChildElements.ElementAt(sizeIndex).Cq().Text();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(row.ChildElements.ElementAt(seedersIndex).Cq().Text().Trim());
                    release.Peers = ParseUtil.CoerceInt(row.ChildElements.ElementAt(leechersIndex).Cq().Text().Trim()) + release.Seeders;

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
