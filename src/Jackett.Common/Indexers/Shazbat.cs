using System;
using System.Collections.Generic;
using System.Linq;
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
    public class Shazbat : BaseWebIndexer
    {
        private string LoginUrl => SiteLink + "login";
        private string SearchUrl => SiteLink + "search";
        private string TorrentsUrl => SiteLink + "torrents";
        private string ShowUrl => SiteLink + "show?id=";
        private string RSSProfile => SiteLink + "rss_feeds";

        private new ConfigurationDataBasicLoginWithRSS configData
        {
            get => (ConfigurationDataBasicLoginWithRSS)base.configData;
            set => base.configData = value;
        }

        public Shazbat(IIndexerConfigurationService configService, WebClient c, Logger l, IProtectionService ps)
            : base(name: "Shazbat",
                   description: "Modern indexer",
                   link: "https://www.shazbat.tv/",
                   caps: new TorznabCapabilities(
                       TorznabCatType.TV,
                       TorznabCatType.TVHD,
                       TorznabCatType.TVSD),
                   configService: configService,
                   client: c,
                   logger: l,
                   p: ps,
                   configData: new ConfigurationDataBasicLoginWithRSS())
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string>
            {
                {"referer", "login"},
                {"query", ""},
                {"tv_login", configData.Username.Value},
                {"tv_password", configData.Password.Value},
                {"email", ""}
            };

            // Get cookie
            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, LoginUrl);
            await ConfigureIfOK(result.Cookies, result.Content?.Contains("glyphicon-log-out") == true,
                                () => throw new ExceptionWithConfigData("The username and password entered do not match.", configData));
            var rssProfile = await RequestStringWithCookiesAndRetry(RSSProfile);
            var parser = new HtmlParser();
            var rssDom = parser.ParseDocument(rssProfile.Content);
            configData.RSSKey.Value = rssDom.QuerySelector(".col-sm-9:nth-of-type(1)").TextContent.Trim();
            if (string.IsNullOrWhiteSpace(configData.RSSKey.Value))
                throw new ExceptionWithConfigData("Failed to find RSS key.", configData);
            SaveConfig();
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var queryString = query.GetQueryString();
            WebClientStringResult results = null;
            var searchUrls = new List<string>();
            if (!string.IsNullOrWhiteSpace(query.SanitizedSearchTerm))
            {
                var pairs = new Dictionary<string, string>
                {
                    {"search", query.SanitizedSearchTerm}
                };
                results = await PostDataWithCookiesAndRetry(SearchUrl, pairs, null, TorrentsUrl);
                results = await ReloginIfNecessary(results);
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(results.Content);
                var shows = dom.QuerySelectorAll("div.show[data-id]");
                foreach (var show in shows)
                {
                    var showUrl = ShowUrl + show.GetAttribute("data-id");
                    searchUrls.Add(showUrl);
                }
            }
            else
                searchUrls.Add(TorrentsUrl);

            try
            {
                foreach (var searchUrl in searchUrls)
                {
                    results = await RequestStringWithCookies(searchUrl);
                    results = await ReloginIfNecessary(results);
                    var parser = new HtmlParser();
                    var dom = parser.ParseDocument(results.Content);
                    var rows = dom.QuerySelectorAll(
                        string.IsNullOrWhiteSpace(queryString) ? "#torrent-table tr" : "table tr");
                    var globalFreeleech =
                        dom.QuerySelector("span:contains(\"Freeleech until:\"):has(span.datetime)") != null;
                    foreach (var row in rows.Skip(1))
                    {
                        var release = new ReleaseInfo();
                        var titleRow = row.QuerySelector("td:nth-of-type(3)");
                        foreach (var child in titleRow.Children)
                            child.Remove();
                        release.Title = titleRow.TextContent.Trim();
                        if ((query.ImdbID == null || !TorznabCaps.SupportsImdbMovieSearch) &&
                            !query.MatchQueryStringAND(release.Title))
                            continue;
                        var bannerStyle = row.QuerySelector("div[style^=\"cursor: pointer; background-image:url\"]")
                                             ?.GetAttribute("style");
                        if (!string.IsNullOrEmpty(bannerStyle))
                        {
                            var bannerImg = Regex.Match(bannerStyle, @"url\('(.*?)'\);").Groups[1].Value;
                            release.BannerUrl = new Uri(SiteLink + bannerImg);
                        }

                        var qLink = row.QuerySelector("td:nth-of-type(5) a");
                        release.Link = new Uri(SiteLink + qLink.GetAttribute("href"));
                        release.Guid = release.Link;
                        var qLinkComm = row.QuerySelector("td:nth-of-type(5) a.internal");
                        release.Comments = new Uri(SiteLink + qLinkComm.GetAttribute("href"));
                        var dateString = row.QuerySelector(".datetime")?.GetAttribute("data-timestamp");
                        if (dateString != null)
                            release.PublishDate = DateTimeUtil
                                                  .UnixTimestampToDateTime(ParseUtil.CoerceDouble(dateString)).ToLocalTime();
                        var infoString = row.QuerySelector("td:nth-of-type(4)").TextContent;
                        release.Size = ParseUtil.CoerceLong(
                            Regex.Match(infoString, "\\((\\d+)\\)").Value.Replace("(", "").Replace(")", ""));
                        var infosplit = infoString.Replace("/", string.Empty).Split(":".ToCharArray());
                        release.Seeders = ParseUtil.CoerceInt(infosplit[1]);
                        release.Peers = release.Seeders + ParseUtil.CoerceInt(infosplit[2]);
                        release.DownloadVolumeFactor = globalFreeleech ? 0 : 1;
                        release.UploadVolumeFactor = 1;
                        release.MinimumRatio = 1;
                        release.MinimumSeedTime = 172800; // 48 hours

                        // var tags = row.QuerySelector(".label-tag").TextContent; These don't see to parse - bad tags?
                        releases.Add(release);
                    }
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.Content, ex);
            }
            foreach (var release in releases)
                release.Category = release.Title.Contains("1080p") || release.Title.Contains("720p")
                    ? new List<int> {TorznabCatType.TVHD.ID}
                    : new List<int> {TorznabCatType.TVSD.ID};
            return releases;
        }

        private async Task<WebClientStringResult> ReloginIfNecessary(WebClientStringResult response)
        {
            if (response.Content.Contains("onclick=\"document.location='logout'\""))
                return response;

            await ApplyConfiguration(null);
            response.Request.Cookies = CookieHeader;
            return await webclient.GetString(response.Request);
        }
    }
}
