using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class TransmitheNet : BaseWebIndexer
    {
        private string LoginUrl => SiteLink + "login.php";
        private string SearchUrl => SiteLink + "torrents.php?action=basic&order_by=time&order_way=desc&search_type=0&taglist=&tags_type=0";

        private new ConfigurationDataBasicLogin configData
        {
            get => (ConfigurationDataBasicLogin)base.configData;
            set => base.configData = value;
        }

        public TransmitheNet(IIndexerConfigurationService configService, Utils.Clients.WebClient c, Logger l, IProtectionService ps)
            : base(name: "Nebulance",
                description: " At Nebulance we will change the way you think about TV",
                link: "https://nebulance.io/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                configService: configService,
                client: c,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLogin("For best results, change the 'Torrents per page' setting to 100 in your profile on the NBL webpage."))
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
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
                var document = parser.ParseDocument(response.Content);
                var messageEl = document.QuerySelector("form > span[class='warning']");
                var errorMessage = response.Content;
                if (messageEl != null)
                    errorMessage = messageEl.TextContent.Trim();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var loggedInCheck = await RequestStringWithCookies(SearchUrl);
            if (!loggedInCheck.Content.Contains("logout.php"))
            {
                //Cookie appears to expire after a period of time or logging in to the site via browser
                await DoLogin();
            }

            // #6413
            //string Url;
            //if (string.IsNullOrEmpty(query.GetQueryString()))
            //    Url = SearchUrl;
            //else
            //{
            var Url = $"{SearchUrl}&searchtext={WebUtility.UrlEncode(query.GetQueryString())}";
            //}

            var response = await RequestStringWithCookiesAndRetry(Url);
            var releases = ParseResponse(response.Content);

            return releases;
        }

        public List<ReleaseInfo> ParseResponse(string htmlResponse)
        {
            var releases = new List<ReleaseInfo>();

            try
            {
                var globalFreeleech = false;
                var parser = new HtmlParser();
                var document = parser.ParseDocument(htmlResponse);

                if (document.QuerySelector("div.nicebar > span:contains(\"Personal Freeleech\")") != null)
                    globalFreeleech = true;

                var rows = document.QuerySelectorAll(".torrent_table > tbody > tr[class^='torrent row']");

                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();

                    var title = row.QuerySelector("a[data-src]").GetAttribute("data-src");
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
                    release.Category = new List<int> { TvCategoryParser.ParseTvShowQuality(release.Title) };

                    var timeAnchor = row.QuerySelector("span[class='time']");
                    var publishdate = timeAnchor.GetAttribute("title");
                    release.PublishDate = !string.IsNullOrEmpty(publishdate) && publishdate.Contains(",")
                        ? DateTime.ParseExact(publishdate, "MMM dd yyyy, HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal)
                        : DateTime.ParseExact(timeAnchor.TextContent.Trim(), "MMM dd yyyy, HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
                    release.Seeders = ParseUtil.CoerceInt(timeAnchor.ParentElement.NextElementSibling.NextElementSibling.TextContent.Trim());
                    release.Peers = ParseUtil.CoerceInt(timeAnchor.ParentElement.NextElementSibling.NextElementSibling.NextElementSibling.TextContent.Trim()) + release.Seeders;
                    release.Size = ReleaseInfo.GetBytes(timeAnchor.ParentElement.PreviousElementSibling.TextContent);
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800; // 48 hours

                    release.Files = ParseUtil.CoerceLong(row.QuerySelector("td > div:contains(\"Files:\")").TextContent.Split(':')[1].Trim());
                    release.Grabs = ParseUtil.CoerceLong(row.QuerySelector("td:nth-last-child(3)").TextContent);

                    if (globalFreeleech)
                        release.DownloadVolumeFactor = 0;
                    else if (row.QuerySelector("img[alt=\"Freeleech\"]") != null)
                        release.DownloadVolumeFactor = 0;
                    else
                        release.DownloadVolumeFactor = 1;

                    release.UploadVolumeFactor = 1;

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
