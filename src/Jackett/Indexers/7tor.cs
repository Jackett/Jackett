using Jackett.Utils.Clients;
using NLog;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Models;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System;
using System.Text;
using System.Globalization;
using Jackett.Models.IndexerConfig;
using System.Collections.Specialized;
using AngleSharp.Parser.Html;
using AngleSharp.Dom;
using System.Text.RegularExpressions;
using System.Web;

namespace Jackett.Indexers
{
    public class SevenTor : BaseIndexer, IIndexer
    {
        string LoginUrl { get { return SiteLink + "ucp.php?mode=login"; } }
        string SearchUrl { get { return SiteLink + "search.php"; } }

        new ConfigurationDataBasicLoginWithRSSAndDisplay configData
        {
            get { return (ConfigurationDataBasicLoginWithRSSAndDisplay)base.configData; }
            set { base.configData = value; }
        }

        public SevenTor(IIndexerManagerService i, IWebClient wc, Logger l, IProtectionService ps)
            : base(name: "7tor",
                   description: null,
                   link: "https://7tor.org/",
                   caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                   manager: i,
                   client: wc,
                   logger: l,
                   p: ps,
                   configData: new ConfigurationDataBasicLoginWithRSSAndDisplay())
        {
            Encoding = Encoding.UTF8;
            Language = "ru-ru";

            AddCategoryMapping(1, TorznabCatType.AudioMP3);
            AddCategoryMapping(2, TorznabCatType.AudioLossless);
            AddCategoryMapping(3, TorznabCatType.AudioOther);
        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string>
            {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "autologin", "on" },
                { "viewonline", "on" },
                { "login", "Вход" }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, LoginUrl, true);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("ucp.php?mode=logout&"), () =>
            {
                var errorMessage = result.Content;
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
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
                queryCollection.Add("search_id", "newposts");
            }
            else // use the normal search
            {
                searchString = searchString.Replace("-", " ");
                queryCollection.Add("terms", "all");
                queryCollection.Add("keywords", searchString);
                queryCollection.Add("author", "");
                queryCollection.Add("sc", "1");
                queryCollection.Add("sf", "titleonly");
            }

            var searchUrl = SearchUrl + "?" + queryCollection.GetQueryString();
            results = await RequestStringWithCookies(searchUrl);
            try
            {
                string RowsSelector = "ul.topics > li";

                var SearchResultParser = new HtmlParser();
                var SearchResultDocument = SearchResultParser.Parse(results.Content);
                var Rows = SearchResultDocument.QuerySelectorAll(RowsSelector);
                foreach (var Row in Rows)
                {
                    try
                    {
                        var release = new ReleaseInfo();

                        release.MinimumRatio = 1;
                        release.MinimumSeedTime = 0;

                        var qDetailsLink = Row.QuerySelector("a.topictitle");
                        var qDownloadLink = Row.QuerySelector("a[href^=\"./download/file.php?id=\"]");
                        
                        release.Title = qDetailsLink.TextContent;
                        release.Comments = new Uri(SiteLink + qDetailsLink.GetAttribute("href"));
                        release.Link = new Uri(SiteLink + qDownloadLink.GetAttribute("href"));
                        release.Guid = release.Comments;

                        release.Seeders = ParseUtil.CoerceInt(Row.QuerySelector("span.seed").TextContent);
                        release.Peers = ParseUtil.CoerceInt(Row.QuerySelector("span.leech").TextContent) + release.Seeders;
                        release.Grabs = ParseUtil.CoerceLong(Row.QuerySelector("span.complet").TextContent);

                        var author = Row.QuerySelector("a[href^=\"./memberlist.php?mode=viewprofile&\"]");
                        var timestr = author.NextSibling.NodeValue.Substring(3).Split('\n')[0].Trim();

                        timestr = timestr.Replace("менее минуты назад", "now");
                        timestr = timestr.Replace("назад", "ago");
                        timestr = timestr.Replace("минуту", "minute");
                        timestr = timestr.Replace("минуты", "minutes");
                        timestr = timestr.Replace("минут", "minutes");

                        timestr = timestr.Replace("Сегодня", "Today");
                        timestr = timestr.Replace("Вчера", "Yesterday"); // untested
                        

                        timestr = timestr.Replace("янв", "Jan");
                        timestr = timestr.Replace("фев", "Feb");
                        timestr = timestr.Replace("мар", "Mar");
                        timestr = timestr.Replace("апр", "Apr");
                        timestr = timestr.Replace("май", "May");
                        timestr = timestr.Replace("июн", "Jun");
                        timestr = timestr.Replace("июл", "Jul");
                        timestr = timestr.Replace("авг", "Aug");
                        timestr = timestr.Replace("сен", "Sep");
                        timestr = timestr.Replace("окт", "Oct");
                        timestr = timestr.Replace("ноя", "Nov");
                        timestr = timestr.Replace("дек", "Dec");
                        release.PublishDate = DateTimeUtil.FromUnknown(timestr, "UK");

                        var forum = Row.QuerySelector("a[href^=\"./viewforum.php?f=\"]");
                        var forumid = forum.GetAttribute("href").Split('=')[1];
                        release.Category = MapTrackerCatToNewznab(forumid);

                        var size = forum.NextElementSibling;
                        var sizestr = size.TextContent;
                        sizestr = sizestr.Replace("ГБ", "GB");
                        sizestr = sizestr.Replace("МБ", "MB");
                        sizestr = sizestr.Replace("КБ", "KB"); // untested
                        release.Size = ReleaseInfo.GetBytes(sizestr);

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

