using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
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
    [ExcludeFromCodeCoverage]
    public class Pier720 : BaseWebIndexer
    {
        public Pier720(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps) :
            base(id: "720pier",
                 name: "720pier",
                 description: "720pier is a RUSSIAN Private Torrent Tracker for HD SPORTS",
                 link: "https://720pier.ru/",
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

        private new ConfigurationDataBasicLoginWithRSSAndDisplay configData => (ConfigurationDataBasicLoginWithRSSAndDisplay)base.configData;

        public override string[] LegacySiteLinks { get; protected set; } =
        {
            "http://720pier.ru/"
        };

        private string LoginUrl => SiteLink + "ucp.php?mode=login";
        private string SearchUrl => SiteLink + "search.php";

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string>
            {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "redirect", "/" },
                { "login", "Login" },
                { "autologin", "on" }
            };
            var htmlParser = new HtmlParser();
            var loginDocument = htmlParser.ParseDocument((await RequestStringWithCookies(LoginUrl)).Content);
            pairs["creation_time"] = loginDocument.GetElementsByName("creation_time")[0].GetAttribute("value");
            pairs["form_token"] = loginDocument.GetElementsByName("form_token")[0].GetAttribute("value");
            pairs["sid"] = loginDocument.GetElementsByName("sid")[0].GetAttribute("value");
            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, LoginUrl, true);
            await ConfigureIfOK(
                result.Cookies, result.Content?.Contains("ucp.php?mode=logout&") == true,
                () => throw new ExceptionWithConfigData(result.Content, configData));
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var searchString = query.GetQueryString();
            var keywordSearch = !string.IsNullOrWhiteSpace(searchString);
            var releases = new List<ReleaseInfo>();
            var queryCollection = !keywordSearch
                ? new NameValueCollection
                {
                    { "search_id", "active_topics" }
                }
                : new NameValueCollection
                {
                    { "sr", "posts" }, //Search all posts
                    { "ot", "1" }, //Search only in forums trackers (checked)
                    { "keywords", searchString },
                    { "sf", "titleonly" }
                };
            var searchUrl = SearchUrl + "?" + queryCollection.GetQueryString();
            var results = await RequestStringWithCookies(searchUrl);
            if (!results.Content.Contains("ucp.php?mode=logout"))
            {
                await ApplyConfiguration(null);
                results = await RequestStringWithCookies(searchUrl);
            }

            try
            {
                var resultParser = new HtmlParser();
                var searchResultDocument = resultParser.ParseDocument(results.Content);
                var rowSelector = keywordSearch
                    ? "div.search div.postbody > h3 > a"
                    : "ul.topics > li.row:has(i.fa-paperclip) a.topictitle"; // Torrent lines have paperclip icon. Chat topics don't
                var rows = searchResultDocument.QuerySelectorAll(rowSelector);
                foreach (var rowLink in rows)
                {
                    var detailLink = SiteLink + rowLink.GetAttribute("href");
                    var detailsResult = await RequestStringWithCookies(detailLink);
                    var detailsDocument = resultParser.ParseDocument(detailsResult.Content);
                    var detailRow = detailsDocument.QuerySelector("table.table2 > tbody > tr");
                    if (detailRow == null)
                        continue; //No torrents in result
                    var qDownloadLink = detailRow.QuerySelector("a[href^=\"/download/torrent\"]");
                    var link = new Uri(SiteLink + qDownloadLink.GetAttribute("href").TrimStart('/'));
                    var timestr = detailRow.Children[0].QuerySelector("ul.dropdown-contents span.my_tt").TextContent;
                    var publishDate = DateTimeUtil.FromUnknown(timestr, "UK");
                    var forumId = detailsDocument.QuerySelector("li.breadcrumbs").LastElementChild
                                                 .GetAttribute("data-forum-id");
                    var sizeString = detailRow.Children[4].QuerySelector("span.my_tt").GetAttribute("title");
                    var size = ParseUtil.CoerceLong(Regex.Replace(sizeString, @"[^0-9]", string.Empty));
                    var comments = new Uri(detailLink);
                    var grabs = ParseUtil.CoerceInt(detailRow.Children[0].QuerySelector("span.complet").TextContent);
                    var seeders = ParseUtil.CoerceInt(detailRow.Children[2].QuerySelector("span.seed").TextContent);
                    var leechers = ParseUtil.CoerceInt(detailRow.Children[3].QuerySelector("span.leech").TextContent);
                    var release = new ReleaseInfo
                    {
                        MinimumRatio = 1,
                        MinimumSeedTime = 0,
                        DownloadVolumeFactor = 1,
                        UploadVolumeFactor = 1,
                        Seeders = seeders,
                        Grabs = grabs,
                        Peers = leechers + seeders,
                        Title = rowLink.TextContent,
                        Comments = comments,
                        Guid = comments,
                        Link = link,
                        PublishDate = publishDate,
                        Category = MapTrackerCatToNewznab(forumId),
                        Size = size,
                    };
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
