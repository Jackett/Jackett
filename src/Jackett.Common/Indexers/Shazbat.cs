using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CsQuery;
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
        private string LoginUrl { get { return SiteLink + "login"; } }
        private string SearchUrl { get { return SiteLink + "search"; } }
        private string TorrentsUrl { get { return SiteLink + "torrents"; } }
        private string ShowUrl { get { return SiteLink + "show?id="; } }
        private string RSSProfile { get { return SiteLink + "rss_feeds"; } }

        private new ConfigurationDataBasicLoginWithRSS configData
        {
            get { return (ConfigurationDataBasicLoginWithRSS)base.configData; }
            set { base.configData = value; }
        }

        public Shazbat(IIndexerConfigurationService configService, WebClient c, Logger l, IProtectionService ps)
            : base(name: "Shazbat",
                description: "Modern indexer",
                link: "https://www.shazbat.tv/",
                caps: new TorznabCapabilities(TorznabCatType.TV,
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

            var rssProfile = await RequestStringWithCookiesAndRetry(RSSProfile);
            CQ rssDom = rssProfile.Content;
            configData.RSSKey.Value = rssDom.Find(".col-sm-9:eq(0)").Text().Trim();
            if (string.IsNullOrWhiteSpace(configData.RSSKey.Value))
            {
                throw new ExceptionWithConfigData("Failed to find RSS key.", configData);
            }

            SaveConfig();
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected async Task<WebClientStringResult> ReloginIfNecessary(WebClientStringResult response)
        {
            if (!response.Content.Contains("onclick=\"document.location='logout'\""))
            {
                await ApplyConfiguration(null);
                response.Request.Cookies = CookieHeader;
                return await webclient.GetString(response.Request);
            }
            return response;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var queryString = query.GetQueryString();
            var url = TorrentsUrl;

            WebClientStringResult results = null;

            var searchUrls = new List<string>();
            if (!string.IsNullOrWhiteSpace(query.SanitizedSearchTerm))
            {
                var pairs = new Dictionary<string, string>();
                pairs.Add("search", query.SanitizedSearchTerm);

                results = await PostDataWithCookiesAndRetry(SearchUrl, pairs, null, TorrentsUrl);
                results = await ReloginIfNecessary(results);
                CQ dom = results.Content;
                var shows = dom.Find("div.show[data-id]");
                foreach (var show in shows)
                {
                    var showUrl = ShowUrl + show.GetAttribute("data-id");
                    searchUrls.Add(showUrl);
                }
            }
            else
            {
                searchUrls.Add(TorrentsUrl);
            }

            try
            {
                foreach (var searchUrl in searchUrls)
                {
                    results = await RequestStringWithCookies(searchUrl);
                    results = await ReloginIfNecessary(results);

                    CQ dom = results.Content;
                    var rows = dom["#torrent-table tr"];

                    if (!string.IsNullOrWhiteSpace(queryString))
                    {
                        rows = dom["table tr"];
                    }

                    var globalFreeleech = dom.Find("span:contains(\"Freeleech until:\"):has(span.datetime)").Any();

                    foreach (var row in rows.Skip(1))
                    {
                        var release = new ReleaseInfo();
                        var qRow = row.Cq();
                        var titleRow = qRow.Find("td:eq(2)").First();
                        titleRow.Children().Remove();
                        release.Title = titleRow.Text().Trim();
                        if ((query.ImdbID == null || !TorznabCaps.SupportsImdbSearch) && !query.MatchQueryStringAND(release.Title))
                            continue;

                        var qBanner = qRow.Find("div[style^=\"cursor: pointer; background-image:url\"]");
                        var qBannerStyle = qBanner.Attr("style");
                        if (!string.IsNullOrEmpty(qBannerStyle))
                        {
                            var bannerImg = Regex.Match(qBannerStyle, @"url\('(.*?)'\);").Groups[1].Value;
                            release.BannerUrl = new Uri(SiteLink + bannerImg);
                        }

                        var qLink = row.Cq().Find("td:eq(4) a:eq(0)");
                        release.Link = new Uri(SiteLink + qLink.Attr("href"));
                        release.Guid = release.Link;
                        var qLinkComm = row.Cq().Find("td:eq(4) a:eq(1)");
                        release.Comments = new Uri(SiteLink + qLinkComm.Attr("href"));

                        var dateString = qRow.Find(".datetime").Attr("data-timestamp");
                        if (dateString != null)
                            release.PublishDate = DateTimeUtil.UnixTimestampToDateTime(ParseUtil.CoerceDouble(dateString)).ToLocalTime();
                        var infoString = row.Cq().Find("td:eq(3)").Text();

                        release.Size = ParseUtil.CoerceLong(Regex.Match(infoString, "\\((\\d+)\\)").Value.Replace("(", "").Replace(")", ""));

                        var infosplit = infoString.Replace("/", string.Empty).Split(":".ToCharArray());
                        release.Seeders = ParseUtil.CoerceInt(infosplit[1]);
                        release.Peers = release.Seeders + ParseUtil.CoerceInt(infosplit[2]);

                        if (globalFreeleech)
                            release.DownloadVolumeFactor = 0;
                        else
                            release.DownloadVolumeFactor = 1;

                        release.UploadVolumeFactor = 1;

                        // var tags = row.Cq().Find(".label-tag").Text(); These don't see to parse - bad tags?

                        releases.Add(release);
                    }
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.Content, ex);
            }
            /* else
             {
                 var rssUrl = SiteLink + "rss/recent?passkey=" + configData.RSSKey.Value;

                 results = await RequestStringWithCookiesAndRetry(rssUrl);
                 try
                 {
                     var doc = XDocument.Parse(results.Content);
                     foreach (var result in doc.Descendants("item"))
                     {
                         var xTitle = result.Element("title").Value;
                         var xLink = result.Element("link").Value;
                         var xGUID = result.Element("guid").Value;
                         var xDesc = result.Element("description").Value;
                         var xDate = result.Element("pubDate").Value;
                         var release = new ReleaseInfo();
                         release.Guid  =release.Link =  new Uri(xLink);
                         release.MinimumRatio = 1;
                         release.Seeders = 1; // We are not supplied with peer info so just mark it as one.
                         foreach (var element in xDesc.Split(";".ToCharArray()))
                         {
                             var split = element.IndexOf(':');
                             if (split > -1)
                             {
                                 var key = element.Substring(0, split).Trim();
                                 var value = element.Substring(split+1).Trim();

                                 switch (key)
                                 {
                                     case "Filename":
                                         release.Title = release.Description = value;
                                         break;
                                 }
                             }
                         }

                         //"Thu, 24 Sep 2015 18:07:07 +0000"
                         release.PublishDate = DateTime.ParseExact(xDate, "ddd, dd MMM yyyy HH:mm:ss +0000", CultureInfo.InvariantCulture);

                         if (!string.IsNullOrWhiteSpace(release.Title))
                         {
                             releases.Add(release);
                         }
                     }
                 }
                 catch (Exception ex)
                 {
                     OnParseError(results.Content, ex);
                 }*/

            foreach (var release in releases)
            {
                if (release.Title.Contains("1080p") || release.Title.Contains("720p"))
                {
                    release.Category = new List<int> { TorznabCatType.TVHD.ID };
                }
                else
                {
                    release.Category = new List<int> { TorznabCatType.TVSD.ID };
                }
            }

            return releases;
        }
    }
}
