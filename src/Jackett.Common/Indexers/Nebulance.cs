using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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
    public class Nebulance : BaseWebIndexer
    {
        private string LoginUrl => SiteLink + "login.php";
        private string SearchUrl => SiteLink + "torrents.php";

        private new ConfigurationDataBasicLoginWith2FA configData => (ConfigurationDataBasicLoginWith2FA)base.configData;

        public Nebulance(IIndexerConfigurationService configService, Utils.Clients.WebClient c, Logger l, IProtectionService ps)
            : base(id: "nebulance",
                   name: "Nebulance",
                   description: "At Nebulance we will change the way you think about TV",
                   link: "https://nebulance.io/",
                   caps: new TorznabCapabilities
                   {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                       }
                   },
                   configService: configService,
                   client: c,
                   logger: l,
                   p: ps,
                   configData: new ConfigurationDataBasicLoginWith2FA(@"If 2FA is disabled, let the field empty.
 We recommend to disable 2FA because re-login will require manual actions.
<br/>For best results, change the 'Torrents per page' setting to 100 in your profile on the NBL webpage."))
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";

            AddCategoryMapping(1, TorznabCatType.TV);
            AddCategoryMapping(2, TorznabCatType.TVSD);
            AddCategoryMapping(3, TorznabCatType.TVHD);
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
                { "twofa", configData.TwoFactorAuth.Value },
                { "keeplogged", "on" },
                { "login", "Login" }
            };

            CookieHeader = string.Empty;
            var response = await RequestLoginAndFollowRedirect(LoginUrl, pairs, CookieHeader, true, null, LoginUrl);

            await ConfigureIfOK(response.Cookies, response.ContentString != null && response.ContentString.Contains("logout.php"), () =>
            {
                var parser = new HtmlParser();
                var document = parser.ParseDocument(response.ContentString);
                var messageEl = document.QuerySelector("form > span[class='warning']");
                var errorMessage = response.ContentString;
                if (messageEl != null)
                    errorMessage = messageEl.TextContent.Trim();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var qc = new NameValueCollection
            {
                {"action", "basic"},
                {"order_by", "time"},
                {"order_way", "desc"},
                {"searchtext", query.GetQueryString()}
            };

            var searchUrl = SearchUrl + "?" + qc.GetQueryString();
            var response = await RequestWithCookiesAsync(searchUrl);
            if (!response.ContentString.Contains("logout.php")) // re-login
            {
                await DoLogin();
                response = await RequestWithCookiesAsync(searchUrl);
            }

            try
            {
                var parser = new HtmlParser();
                var document = parser.ParseDocument(response.ContentString);
                var rows = document.QuerySelectorAll(".torrent_table > tbody > tr[class^='torrent row']");

                foreach (var row in rows)
                {
                    var title = row.QuerySelector("a[data-src]").GetAttribute("data-src");
                    if (string.IsNullOrEmpty(title) || title == "0")
                    {
                        title = row.QuerySelector("a[data-src]").TextContent;
                        title = Regex.Replace(title, @"[\[\]\/]", "");
                    }
                    else
                    {
                        if (title.Length > 5 && title.Substring(title.Length - 5).Contains("."))
                            title = title.Remove(title.LastIndexOf(".", StringComparison.Ordinal));
                    }

                    var posterStr = row.QuerySelector("img")?.GetAttribute("src");
                    var poster = !string.IsNullOrWhiteSpace(posterStr) ? new Uri(posterStr) : null;

                    var details = new Uri(SiteLink + row.QuerySelector("a[data-src]").GetAttribute("href"));
                    var link = new Uri(SiteLink + row.QuerySelector("a[href*='action=download']").GetAttribute("href"));

                    var qColSize = row.QuerySelector("td:nth-child(3)");
                    var size = ReleaseInfo.GetBytes(qColSize.Children[0].TextContent);
                    var files = ParseUtil.CoerceLong(qColSize.Children[1].TextContent.Split(':')[1].Trim());


                    var qPublishdate = row.QuerySelector("td:nth-child(4) span");
                    var publishDateStr = qPublishdate.GetAttribute("title");
                    var publishDate = !string.IsNullOrEmpty(publishDateStr) && publishDateStr.Contains(",")
                        ? DateTime.ParseExact(publishDateStr, "MMM dd yyyy, HH:mm", CultureInfo.InvariantCulture)
                        : DateTime.ParseExact(qPublishdate.TextContent.Trim(), "MMM dd yyyy, HH:mm", CultureInfo.InvariantCulture);

                    var grabs = ParseUtil.CoerceLong(row.QuerySelector("td:nth-child(5)").TextContent);
                    var seeds = ParseUtil.CoerceLong(row.QuerySelector("td:nth-child(6)").TextContent);
                    var leechers = ParseUtil.CoerceLong(row.QuerySelector("td:nth-child(7)").TextContent);

                    var release = new ReleaseInfo
                    {
                        Title = title,
                        Guid = details,
                        Details = details,
                        Link = link,
                        Category = new List<int> { TvCategoryParser.ParseTvShowQuality(title) },
                        Size = size,
                        Files = files,
                        PublishDate = publishDate,
                        Grabs = grabs,
                        Seeders = seeds,
                        Peers = seeds + leechers,
                        Poster = poster,
                        MinimumRatio = 0, // ratioless
                        MinimumSeedTime = 86400, // 24 hours
                        DownloadVolumeFactor = 0, // ratioless tracker
                        UploadVolumeFactor = 1
                    };

                    releases.Add(release);
                }
            }
            catch (Exception e)
            {
                OnParseError(response.ContentString, e);
            }

            return releases;
        }
    }
}
