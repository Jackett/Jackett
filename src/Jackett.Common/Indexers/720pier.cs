using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Parser.Html;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    public class Pier720 : BaseWebIndexer
    {
        private string LoginUrl { get { return SiteLink + "ucp.php?mode=login"; } }
        private string SearchUrl { get { return SiteLink + "search.php"; } }

        private new ConfigurationDataBasicLoginWithRSSAndDisplay configData
        {
            get { return (ConfigurationDataBasicLoginWithRSSAndDisplay)base.configData; }
            set { base.configData = value; }
        }

        public Pier720(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(name: "720pier",
                   description: "720pier is a RUSSIAN Private Torrent Tracker for HD SPORTS",
                   link: "http://720pier.ru/",
                   caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   configData: new ConfigurationDataBasicLoginWithRSSAndDisplay())
        {
            Encoding = Encoding.UTF8;
            Language = "ru-ru";
            Type = "private";

            AddCategoryMapping(30, TorznabCatType.Movies, "Video content");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string>
            {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "redirect", "/" },
                { "login", "Login" }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, LoginUrl, true);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("ucp.php?mode=logout&"), () =>
            {
                var errorMessage = result.Content;
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();

            WebClientStringResult results = null;
            var queryCollection = new NameValueCollection();

            queryCollection.Add("st", "0");
            queryCollection.Add("sd", "d");
            queryCollection.Add("sk", "t");
            queryCollection.Add("tracker_search", "torrent");
            queryCollection.Add("t", "0");
            queryCollection.Add("submit", "Search");
            queryCollection.Add("sr", "topics");
            //queryCollection.Add("sr", "posts");
            //queryCollection.Add("ch", "99999");

            // if the search string is empty use the getnew view
            if (string.IsNullOrWhiteSpace(searchString))
            {
                queryCollection.Add("search_id", "active_topics");
                queryCollection.Add("ot", "1");
            }
            else // use the normal search
            {
                searchString = searchString.Replace("-", " ");
                queryCollection.Add("keywords", searchString);
                queryCollection.Add("sf", "titleonly");
                queryCollection.Add("sr", "topics");
                queryCollection.Add("pt", "t");
                queryCollection.Add("ot", "1");
            }

            var searchUrl = SearchUrl + "?" + queryCollection.GetQueryString();
            results = await RequestStringWithCookies(searchUrl);
            if (!results.Content.Contains("ucp.php?mode=logout"))
            {
                await ApplyConfiguration(null);
                results = await RequestStringWithCookies(searchUrl);
            }
            try
            {
                string RowsSelector = "ul.topics > li.row";

                var ResultParser = new HtmlParser();
                var SearchResultDocument = ResultParser.Parse(results.Content);
                var Rows = SearchResultDocument.QuerySelectorAll(RowsSelector);
                foreach (var Row in Rows)
                {
                    try
                    {     
                        {
                            var release = new ReleaseInfo();

                            release.MinimumRatio = 1;
                            release.MinimumSeedTime = 0;

                            var qDetailsLink = Row.QuerySelector("a.topictitle");

                            var detailsResult = await RequestStringWithCookies(SiteLink + qDetailsLink.GetAttribute("href"));
                            var DetailsResultDocument = ResultParser.Parse(detailsResult.Content);

                            var qDownloadLink = DetailsResultDocument.QuerySelector("table.table2 > tbody > tr > td > a[href^=\"/download/torrent.php?id\"]");

                            release.Title = qDetailsLink.TextContent;
                            release.Comments = new Uri(SiteLink + qDetailsLink.GetAttribute("href"));
                            release.Link = new Uri(SiteLink + qDownloadLink.GetAttribute("href"));
                            release.Guid = release.Comments;

                            release.Seeders = ParseUtil.CoerceInt(Row.QuerySelector("span.seed").TextContent);
                            release.Peers = ParseUtil.CoerceInt(Row.QuerySelector("span.leech").TextContent) + release.Seeders;
                            release.Grabs = ParseUtil.CoerceLong(Row.QuerySelector("span.complet").TextContent);

                            var author = Row.QuerySelector("dd.lastpost > span");
                            var timestr = author.TextContent.Split('\n')[4].Trim();
        
                            release.PublishDate = DateTimeUtil.FromUnknown(timestr, "UK");

                            var forum = Row.QuerySelector("a[href^=\"./viewforum.php?f=\"]");
                            var forumid = forum.GetAttribute("href").Split('=')[1];
                            release.Category = MapTrackerCatToNewznab(forumid);

                            var size = Row.QuerySelector("dl.row-item > dt > div.list-inner > div[style^=\"float:right\"]").TextContent;
                            size = size.Replace("GiB", "GB");
                            size = size.Replace("MiB", "MB");
                            size = size.Replace("KiB", "KB");

                            release.Size = ReleaseInfo.GetBytes(size);

                            release.DownloadVolumeFactor = 1;
                            release.UploadVolumeFactor = 1;

                            releases.Add(release);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(string.Format("{0}: Error while parsing row '{1}':\n\n{2}", ID, Row.OuterHtml, ex));
                    }
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
