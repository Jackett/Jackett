using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
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
            
            AddCategoryMapping(32, TorznabCatType.TVSport, "Basketball");
            AddCategoryMapping(34, TorznabCatType.TVSport, "Basketball - NBA");
            AddCategoryMapping(87, TorznabCatType.TVSport, "Basketball - NBA Playoffs");
            AddCategoryMapping(81, TorznabCatType.TVSport, "Basketball - NBA Playoffs - 2016");
            AddCategoryMapping(95, TorznabCatType.TVSport, "Basketball - NBA Playoffs - 2017");
            AddCategoryMapping(58, TorznabCatType.TVSport, "Basketball - NBA (до 2015 г.)");
            AddCategoryMapping(52, TorznabCatType.TVSport, "Basketball - NCAA");
            AddCategoryMapping(82, TorznabCatType.TVSport, "Basketball - WNBA");
            AddCategoryMapping(36, TorznabCatType.TVSport, "Basketball - European basketball");
            AddCategoryMapping(37, TorznabCatType.TVSport, "Basketball - World Championship");
            AddCategoryMapping(51, TorznabCatType.TVSport, "Basketball - Reviews and highlights");
            AddCategoryMapping(41, TorznabCatType.TVSport, "Basketball - Other");
            AddCategoryMapping(38, TorznabCatType.TVSport, "Basketball - Olympic Games");

            AddCategoryMapping(42, TorznabCatType.TVSport, "Football");
            AddCategoryMapping(43, TorznabCatType.TVSport, "Football - NFL");
            AddCategoryMapping(66, TorznabCatType.TVSport, "Football - Super Bowls");
            AddCategoryMapping(53, TorznabCatType.TVSport, "Football - NCAA");
            AddCategoryMapping(99, TorznabCatType.TVSport, "Football - CFL");
            AddCategoryMapping(101, TorznabCatType.TVSport, "Football - AAF");
            AddCategoryMapping(54, TorznabCatType.TVSport, "Football - Reviews and highlights");
            AddCategoryMapping(97, TorznabCatType.TVSport, "Football - Documentaries");
            AddCategoryMapping(44, TorznabCatType.TVSport, "Football - Other");

            AddCategoryMapping(46, TorznabCatType.TVSport, "Hockey");
            AddCategoryMapping(48, TorznabCatType.TVSport, "Hockey - NHL");
            AddCategoryMapping(88, TorznabCatType.TVSport, "Hockey - NHL Playoffs");
            AddCategoryMapping(93, TorznabCatType.TVSport, "Hockey - NHL Playoffs - 2017");
            AddCategoryMapping(80, TorznabCatType.TVSport, "Hockey - NHL Playoffs - 2016");
            AddCategoryMapping(65, TorznabCatType.TVSport, "Hockey - Stanley Cup Finals");
            AddCategoryMapping(69, TorznabCatType.TVSport, "Hockey - Stanley Cup Finals - 2005-2014");
            AddCategoryMapping(70, TorznabCatType.TVSport, "Hockey - Stanley Cup Finals - 2003");
            AddCategoryMapping(92, TorznabCatType.TVSport, "Hockey - NCAA");
            AddCategoryMapping(49, TorznabCatType.TVSport, "Hockey - World Championship");
            AddCategoryMapping(68, TorznabCatType.TVSport, "Hockey - Documentaries");
            AddCategoryMapping(64, TorznabCatType.TVSport, "Hockey - Reviews and highlights");
            AddCategoryMapping(50, TorznabCatType.TVSport, "Hockey - Other");

            AddCategoryMapping(55, TorznabCatType.TVSport, "Baseball");
            AddCategoryMapping(71, TorznabCatType.TVSport, "Baseball - MLB");
            AddCategoryMapping(72, TorznabCatType.TVSport, "Baseball - Other");
            AddCategoryMapping(85, TorznabCatType.TVSport, "Baseball - Reviews, highlights, documentaries");

            AddCategoryMapping(59, TorznabCatType.TVSport, "Soccer");
            AddCategoryMapping(61, TorznabCatType.TVSport, "Soccer - English soccer");
            AddCategoryMapping(86, TorznabCatType.TVSport, "Soccer - UEFA");
            AddCategoryMapping(100, TorznabCatType.TVSport, "Soccer - MLS");
            AddCategoryMapping(62, TorznabCatType.TVSport, "Soccer - Other tournaments, championships");
            AddCategoryMapping(63, TorznabCatType.TVSport, "Soccer - World Championships");
            AddCategoryMapping(98, TorznabCatType.TVSport, "Soccer - FIFA World Cup");

            AddCategoryMapping(45, TorznabCatType.TVSport, "Other sports");
            AddCategoryMapping(79, TorznabCatType.TVSport, "Other sports - Rugby");
            AddCategoryMapping(78, TorznabCatType.TVSport, "Other sports - Lacrosse");
            AddCategoryMapping(77, TorznabCatType.TVSport, "Other sports - Cricket");
            AddCategoryMapping(76, TorznabCatType.TVSport, "Other sports - Volleyball");
            AddCategoryMapping(75, TorznabCatType.TVSport, "Other sports - Tennis");
            AddCategoryMapping(74, TorznabCatType.TVSport, "Other sports - Fighting");
            AddCategoryMapping(73, TorznabCatType.TVSport, "Other sports - Auto, moto racing");
            AddCategoryMapping(91, TorznabCatType.TVSport, "Other sports - Olympic Games");
            AddCategoryMapping(94, TorznabCatType.TVSport, "Other sports - Misc");

            AddCategoryMapping(56, TorznabCatType.TVSport, "Sports on tv");
            AddCategoryMapping(30, TorznabCatType.TVSport, "Sports");
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
                var SearchResultDocument = ResultParser.ParseDocument(results.Content);
                var Rows = SearchResultDocument.QuerySelectorAll(RowsSelector);
                foreach (var Row in Rows)
                {
                    try
                    {    
                        var release = new ReleaseInfo();

                        release.MinimumRatio = 1;
                        release.MinimumSeedTime = 0;

                        var qDetailsLink = Row.QuerySelector("a.topictitle");

                        release.Title = qDetailsLink.TextContent;
                        release.Comments = new Uri(SiteLink + qDetailsLink.GetAttribute("href"));
                        release.Guid = release.Comments;

                        var detailsResult = await RequestStringWithCookies(SiteLink + qDetailsLink.GetAttribute("href"));
                        var DetailsResultDocument = ResultParser.ParseDocument(detailsResult.Content);
                        var qDownloadLink = DetailsResultDocument.QuerySelector("table.table2 > tbody > tr > td > a[href^=\"/download/torrent.php?id\"]");

                        release.Link = new Uri(SiteLink + qDownloadLink.GetAttribute("href").TrimStart('/'));

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

                        size = size.Replace("ГБ", "GB");
                        size = size.Replace("МБ", "MB");
                        size = size.Replace("КБ", "KB");

                        release.Size = ReleaseInfo.GetBytes(size);

                        release.DownloadVolumeFactor = 1;
                        release.UploadVolumeFactor = 1;

                        releases.Add(release);
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
