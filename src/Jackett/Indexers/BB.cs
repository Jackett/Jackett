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
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Jackett.Indexers
{
    // To comply with the rules for this tracker, only the acronym is used and no publicly displayed URLs to the site. 

    public class BB : BaseIndexer, IIndexer
    {
        private string BaseUrl {  get { return StringUtil.FromBase64("aHR0cHM6Ly9iYWNvbmJpdHMub3JnLw=="); } }
        private string LoginUrl { get { return SiteLink + "login.php"; } }
        private string SearchUrl { get { return SiteLink + "torrents.php?searchstr={0}&searchtags=&tags_type=0&order_by=s3&order_way=desc&disablegrouping=1&filter_cat%5B10%5D=1"; } }

        public BB(IIndexerManagerService i, Logger l, IWebClient w)
            : base(name: "bB",
                description: "bB",
                link: "http://www.reddit.com/r/baconbits/",
                caps: TorznabCapsUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                client: w,
                logger: l)
        {
        }

        public Task<ConfigurationData> GetConfigurationForSetup()
        {
            return Task.FromResult<ConfigurationData>(new ConfigurationDataBasicLogin());
        }

        public async Task ApplyConfiguration(JToken configJson)
        {
            var incomingConfig = new ConfigurationDataBasicLogin();
            incomingConfig.LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string> {
                { "username", incomingConfig.Username.Value },
                { "password", incomingConfig.Password.Value },
                { "keeplogged", "1" },
                { "login", "Log In!" }
            };

            var response = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, SiteLink);
            await ConfigureIfOK(response.Cookies, response.Content != null && response.Content.Contains("logout.php"), () =>
            {
                CQ dom = response.Content;
                var messageEl = dom["#loginform"];
                var messages = new List<string>();
                for (var i = 0; i < 13; i++)
                {
                    var child = messageEl[0].ChildNodes[i];
                    messages.Add(child.Cq().Text().Trim());
                }
                var message = string.Join(" ", messages);
                throw new ExceptionWithConfigData(message, (ConfigurationData)incomingConfig);

            });
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            var searchString = query.SanitizedSearchTerm + " " + query.GetEpisodeSearchString();
            var episodeSearchUrl = string.Format(SearchUrl, HttpUtility.UrlEncode(searchString));
            var results = await RequestStringWithCookiesAndRetry(episodeSearchUrl);

            try
            {
                CQ dom = results.Content;
                var rows = dom["#torrent_table > tbody > tr.torrent"];
                foreach (var row in rows)
                {
                    CQ qRow = row.Cq();
                    var release = new ReleaseInfo();

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;

                    var qLink = row.ChildElements.ElementAt(1).Cq().Children("a")[0].Cq();
                    var linkStr = qLink.Attr("href");
                    release.Title = qLink.Text();
                    release.Comments = new Uri(BaseUrl + "/" + linkStr);
                    release.Guid = release.Comments;

                    var qDownload = row.ChildElements.ElementAt(1).Cq().Find("a[title='Download']")[0].Cq();
                    release.Link = new Uri(BaseUrl + "/" + qDownload.Attr("href"));

                    var dateStr = row.ChildElements.ElementAt(3).Cq().Text().Trim().Replace(" and", "");
                    release.PublishDate = DateTimeUtil.FromTimeAgo(dateStr);

                    var sizeStr = row.ChildElements.ElementAt(4).Cq().Text();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(row.ChildElements.ElementAt(7).Cq().Text().Trim());
                    release.Peers = ParseUtil.CoerceInt(row.ChildElements.ElementAt(8).Cq().Text().Trim()) + release.Seeders;

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
