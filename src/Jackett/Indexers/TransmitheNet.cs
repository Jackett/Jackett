using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Web;
using Jackett.Models.IndexerConfig;
using AngleSharp.Parser.Html;
using System.Text.RegularExpressions;

namespace Jackett.Indexers
{
    public class TransmitheNet : BaseIndexer, IIndexer
    {
        private string LoginUrl { get { return SiteLink + "login.php"; } }
        private string SearchUrl { get { return SiteLink + "torrents.php?action=basic&order_by=time&order_way=desc&search_type=0&taglist=&tags_type=0"; } }

        new ConfigurationDataBasicLogin configData
        {
            get { return (ConfigurationDataBasicLogin)base.configData; }
            set { base.configData = value; }
        }

        public TransmitheNet(IIndexerManagerService i, Logger l, IWebClient c, IProtectionService ps)
            : base(name: "TransmitTheNet",
                description: " At Transmithe.net we will change the way you think about TV",
                link: "https://transmithe.net/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                client: c,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLogin("For best results, change the 'Torrents per page' setting to 100 in your profile on the TTN webpage."))
        {
        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);
            await DoLogin();
            return IndexerConfigurationStatus.RequiresTesting;
        }

        private async Task DoLogin()
        {
            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "keeplogged", "on" },
                { "login", "Login" }
            };

            CookieHeader = string.Empty;
            var response = await RequestLoginAndFollowRedirect(LoginUrl, pairs, CookieHeader, true, null, LoginUrl);

            await ConfigureIfOK(response.Cookies, response.Content != null && response.Content.Contains("logout.php"), () =>
            {
                var parser = new HtmlParser();
                var document = parser.Parse(response.Content);
                var messageEl = document.QuerySelector("form > span[class='warning']");
                var errorMessage = messageEl.TextContent.Trim();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var loggedInCheck = await RequestStringWithCookies(SearchUrl);
            if (!loggedInCheck.Content.Contains("logout.php"))
            {
                //Cookie appears to expire after a period of time or logging in to the site via browser
                await DoLogin();
            }

            string Url;
            if (string.IsNullOrEmpty(query.GetQueryString()))
                Url = SearchUrl;
            else
            {
                Url = $"{SearchUrl}&searchtext={HttpUtility.UrlEncode(query.GetQueryString())}";
            }

            var response = await RequestStringWithCookiesAndRetry(Url);
            List<ReleaseInfo> releases = ParseResponse(response.Content);

            return releases;
        }

        public List<ReleaseInfo> ParseResponse(string htmlResponse)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            try
            {
                var parser = new HtmlParser();
                var document = parser.Parse(htmlResponse);
                var rows = document.QuerySelectorAll(".torrent_table > tbody > tr[class^='torrent row']");

                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();

                    string title = row.QuerySelector("a[data-src]").GetAttribute("data-src");
                    if (string.IsNullOrEmpty(title) || title == "0")
                    {
                        title = row.QuerySelector("a[data-src]").TextContent;
                        title = Regex.Replace(title, @"[\[\]\/]", "");
                    }
                    else
                    {
                        if (title.Length > 5 && title.Substring(title.Length - 5).Contains("."))
                        {
                            title = title.Remove(title.LastIndexOf("."));
                        }
                    }

                    release.Title = title;
                    release.Description = release.Title;
                    release.Guid = new Uri(SiteLink + row.QuerySelector("a[data-src]").GetAttribute("href"));
                    release.Comments = release.Guid;
                    release.Link = new Uri(SiteLink + row.QuerySelector("a[href*='action=download']").GetAttribute("href"));
                    release.Category = TvCategoryParser.ParseTvShowQuality(release.Title);

                    var timeAnchor = row.QuerySelector("span[class='time']");
                    release.PublishDate = DateTime.ParseExact(timeAnchor.GetAttribute("title"), "MMM dd yyyy, HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
                    release.Seeders = ParseUtil.CoerceInt(timeAnchor.ParentElement.NextElementSibling.NextElementSibling.TextContent.Trim());
                    release.Peers = ParseUtil.CoerceInt(timeAnchor.ParentElement.NextElementSibling.NextElementSibling.NextElementSibling.TextContent.Trim()) + release.Seeders;
                    release.Size = ReleaseInfo.GetBytes(timeAnchor.ParentElement.PreviousElementSibling.TextContent);
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(htmlResponse, ex);
            }

            return releases;
        }
    }
}
