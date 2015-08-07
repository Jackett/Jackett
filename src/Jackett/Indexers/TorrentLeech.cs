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

namespace Jackett.Indexers
{
    public class TorrentLeech : BaseIndexer, IIndexer
    {
        private string LoginUrl { get { return SiteLink + "user/account/login/"; } }
        private string SearchUrl { get { return SiteLink + "torrents/browse/index/query/{0}/categories/2%2C26%2C27%2C32/orderby/added?"; } }

        new ConfigurationDataBasicLogin configData
        {
            get { return (ConfigurationDataBasicLogin)base.configData; }
            set { base.configData = value; }
        }

        public TorrentLeech(IIndexerManagerService i, Logger l, IWebClient wc, IProtectionService ps)
            : base(name: "TorrentLeech",
                description: "This is what happens when you seed",
                link: "http://www.torrentleech.org/",
                caps: TorznabCapsUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLogin())
        {
        }

        public async Task ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "remember_me", "on" },
                { "login", "submit" }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, LoginUrl);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("/user/account/logout"), () =>
            {
                CQ dom = result.Content;
                var messageEl = dom[".ui-state-error"].Last();
                var errorMessage = messageEl.Text().Trim();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.SanitizedSearchTerm + " " + query.GetEpisodeSearchString();
            var episodeSearchUrl = string.Format(SearchUrl, HttpUtility.UrlEncode(searchString));
            var results = await RequestStringWithCookiesAndRetry(episodeSearchUrl);
            try
            {
                CQ dom = results.Content;

                CQ qRows = dom["#torrenttable > tbody > tr"];

                foreach (var row in qRows)
                {
                    var release = new ReleaseInfo();

                    var qRow = row.Cq();

                    var debug = qRow.Html();

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;

                    CQ qLink = qRow.Find(".title > a").First();
                    release.Guid = new Uri(SiteLink + qLink.Attr("href"));
                    release.Comments = release.Guid;
                    release.Title = qLink.Text();
                    release.Description = release.Title;

                    release.Link = new Uri(SiteLink + qRow.Find(".quickdownload > a").Attr("href"));

                    var dateString = qRow.Find(".name")[0].InnerText.Trim().Replace(" ", string.Empty).Replace("Addedinon", string.Empty);
                    //"2015-04-25 23:38:12"
                    //"yyyy-MMM-dd hh:mm:ss"
                    release.PublishDate = DateTime.ParseExact(dateString, "yyyy-MM-ddHH:mm:ss", CultureInfo.InvariantCulture);

                    var sizeStr = qRow.Children().ElementAt(4).InnerText;
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(qRow.Find(".seeders").Text());
                    release.Peers = release.Seeders + ParseUtil.CoerceInt(qRow.Find(".leechers").Text());

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
