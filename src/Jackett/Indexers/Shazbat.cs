using CsQuery;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Jackett.Models.IndexerConfig;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Jackett.Indexers
{
    public class Shazbat : BaseIndexer, IIndexer
    {
        private string LoginUrl { get { return SiteLink + "login"; } }
        private string SearchUrl { get { return SiteLink + "search"; } }
        private string TorrentsUrl { get { return SiteLink + "torrents"; } }

        new ConfigurationDataBasicLogin configData
        {
            get { return (ConfigurationDataBasicLogin)base.configData; }
            set { base.configData = value; }
        }

        public Shazbat(IIndexerManagerService i, IWebClient c, Logger l, IProtectionService ps)
            : base(name: "Shazbat",
                description: "Modern indexer",
                link: "http://www.shazbat.tv/",
                caps: new TorznabCapabilities(TorznabCatType.TV,
                                              TorznabCatType.TVHD,
                                              TorznabCatType.TVSD),
                manager: i,
                client: c,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLogin())
        {
        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string> {
                { "referer", "login"},
                { "query", ""},
                { "tv_login", configData.Username.Value },
                { "tv_password", configData.Password.Value },
                { "email", "" }
            };

            // Get cookie
            var firstRequest = await RequestStringWithCookiesAndRetry(LoginUrl);

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, LoginUrl);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("glyphicon-log-out"), () =>
            {
                throw new ExceptionWithConfigData("The username and password entered do not match.", configData);
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var queryString = query.GetQueryString();
            var url = TorrentsUrl;

            WebClientStringResult results = null;

            if (!string.IsNullOrWhiteSpace(queryString))
            {
                var pairs = new Dictionary<string, string> {
                    { "search", queryString},
                    { "portlet", "true"}
                };

                results = await PostDataWithCookiesAndRetry(SearchUrl, pairs, null, TorrentsUrl);
            }
            else
            {
                var pairs = new Dictionary<string, string> {
                    { "portlet", "true"}
                };

                results = await PostDataWithCookiesAndRetry(TorrentsUrl, pairs, null, TorrentsUrl);
            }

            try
            {
                CQ dom = results.Content;
                var rows = dom["#torrent-table tr"];
                foreach (var row in rows.Skip(1))
                {
                    var release = new ReleaseInfo();
                    var qRow = row.Cq();
                    var titleRow = qRow.Find("td:eq(2)").First();
                    titleRow.Children().Remove();
                    release.Title = titleRow.Text().Trim();
                    if (string.IsNullOrWhiteSpace(release.Title))
                        continue;
                    release.Description = release.Title;

                    var qLink = row.Cq().Find("td:eq(4) a:eq(0)");
                    release.Link = new Uri(SiteLink + qLink.Attr("href"));
                    release.Guid = release.Link;
                    var qLinkComm = row.Cq().Find("td:eq(4) a:eq(1)");
                    release.Comments = new Uri(SiteLink + qLinkComm.Attr("href"));

                    // 07-22-2015 11:08 AM
                    var dateString = qRow.Find(".datetime").Attr("data-timestamp");
                    release.PublishDate = DateTimeUtil.UnixTimestampToDateTime(ParseUtil.CoerceDouble(dateString));
                    var infoString = row.Cq().Find("td:eq(3)").Text();

                    release.Size = ParseUtil.CoerceLong(Regex.Match(infoString, "\\((\\d+)\\)").Value.Replace("(","").Replace(")", ""));

                    var infosplit = infoString.Replace("/", string.Empty).Split(":".ToCharArray());
                    release.Seeders = ParseUtil.CoerceInt(infosplit[1]);
                    release.Peers = release.Seeders +  ParseUtil.CoerceInt(infosplit[2]);

                   // var tags = row.Cq().Find(".label-tag").Text(); These don't see to parse - bad tags?

                    if(release.Title.Contains("1080p") || release.Title.Contains("720p"))
                    {
                        release.Category = TorznabCatType.TVHD.ID;
                    } else
                    {
                        release.Category = TorznabCatType.TVSD.ID;
                    }

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
