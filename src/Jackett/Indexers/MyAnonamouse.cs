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
using System.Collections.Specialized;
using System.Globalization;

namespace Jackett.Indexers
{
    public class MyAnonamouse : BaseIndexer, IIndexer
    {
        private string LoginUrl { get { return SiteLink + "takelogin.php"; } }
        private string SearchUrl { get { return SiteLink + "tor/js/loadSearch.php"; } }
        private string SearchUrlReferer { get { return SiteLink + "tor/browse.php"; } }

        new ConfigurationDataBasicLogin configData
        {
            get { return (ConfigurationDataBasicLogin)base.configData; }
            set { base.configData = value; }
        }

        public MyAnonamouse(IIndexerManagerService i, HttpWebClient c, Logger l, IProtectionService ps)
            : base(name: "MyAnonamouse",
                description: "Friendliness, Warmth and Sharing (eBooks & Audio Books)",
                link: "http://www.myanonamouse.net/",
                caps: new TorznabCapabilities(),
                manager: i,
                client: c, // Forced HTTP client for custom headers
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLogin())
        {
            //AddCategoryMapping("ALL", TorznabCatType.AllCats);
            AddCategoryMapping(13, TorznabCatType.AudioAudiobook);
            AddCategoryMapping(14, TorznabCatType.BooksEbook);
        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string> {
                { "email", configData.Username.Value },
                { "password", configData.Password.Value },
                { "returnto", "/" }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, LoginUrl); //loginPage.Cookies
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("logout.php"), () =>
            {
                CQ dom = result.Content;
                var errorMessage = dom["td.embedded"].Text().Trim();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();
            var searchUrl = SearchUrl;
            var queryCollection = new NameValueCollection();

            queryCollection.Add("total", "146"); // Not sure what this is about but its required!

            var cat = "0";
            var queryCats = MapTorznabCapsToTrackers(query);
            if (queryCats.Count == 1)
            {
                cat = queryCats.First().ToString();
            }

            queryCollection.Add("tor[text]", searchString);
            queryCollection.Add("tor[cat][]", "m" + cat);
            queryCollection.Add("tor[srchIn]", "0");
            queryCollection.Add("tor[fullTextType]", "native");
            queryCollection.Add("tor[searchType]", "all");
            queryCollection.Add("tor[searchIn]", "torrents");
            queryCollection.Add("tor[browseFlags][]", "16");
            queryCollection.Add("tor[sortType]", "default");
            queryCollection.Add("tor[startNumber]", "0");

            searchUrl += "?" + queryCollection.GetQueryString();

            var extraHeaders = new Dictionary<string, string>()
            {
                { "X-Requested-With", "XMLHttpRequest" }
            };

            var response = await RequestStringWithCookiesAndRetry(searchUrl, null, SearchUrlReferer, extraHeaders);

            var results = response.Content;
            try
            {
                CQ dom = results;

                var rows = dom["tr"];
                foreach (var row in rows.Skip(1))
                {
                    var qRow = row.Cq();
                    var mLink = qRow.Find("td:eq(3) a:eq(0)");
                    if (qRow.Find("th").Count() > 0 || mLink.Count() == 0) {
                        continue;
                    }

                    var release = new ReleaseInfo();
                    var qTitleLink = qRow.Find("td:eq(2) a").First();
                    release.Title = qTitleLink.Text().Trim();
                    // If we search an get no results, we still get a table just with no info.
                    if (string.IsNullOrWhiteSpace(release.Title))
                    {
                        break;
                    }

                    release.Description = release.Title;
                    release.Guid = new Uri(SiteLink.TrimEnd('/') + qTitleLink.Attr("href"));
                    release.Comments = release.Guid;

                    var dateString = qRow.Find("td:eq(5)").Text().Split('[')[0];
                    release.PublishDate = DateTime.ParseExact(dateString, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

                    //var qLink = qRow.Find("td:eq(2) a");
                    //release.Link = new Uri(qLink.Attr("href"));

                    release.MagnetUri = new Uri(SiteLink.TrimEnd('/') + mLink.Attr("href"));

                    var sizeStr = qRow.Find("td:eq(4)").After("br").Text().TrimStart('[').TrimEnd(']').Trim();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    var connections = qRow.Find("td:eq(6) p"); //.Text().Trim().Split("/".ToCharArray(),StringSplitOptions.RemoveEmptyEntries);
                    release.Seeders = ParseUtil.CoerceInt(connections[0].InnerText.Trim());
                    release.Peers = ParseUtil.CoerceInt(connections[1].InnerText.Trim()) + release.Seeders;

                    var rCat = row.Cq().Find("td:eq(0) a").First().Attr("href");
                    var catString = "tor[cat][]]=";
                    var rCatIdx = rCat.IndexOf(catString);
                    if (rCatIdx > -1)
                    {
                        rCat = rCat.Substring(catString.Length + 4);
                    }

                    release.Category = MapTrackerCatToNewznab(rCat);
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