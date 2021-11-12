using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;

namespace Jackett.Common.Indexers
{
    // This tracker uses a hybrid Luminance (based on GazelleTracker) 
    [ExcludeFromCodeCoverage]
    public class MoreThanTV : BaseWebIndexer
    {
        public override string[] LegacySiteLinks { get; protected set; } = {
            "https://www.morethan.tv/"
        };

        private string LoginUrl => SiteLink + "login";
        private string BrowseUrl => SiteLink + "torrents.php";
        private string DetailsUrl => SiteLink + "details.php";

        private string _sort;
        private string _order;

        private ConfigurationDataBasicLogin ConfigData => (ConfigurationDataBasicLogin)configData;

        private readonly Dictionary<string, string> _emulatedBrowserHeaders = new Dictionary<string, string>();

        public MoreThanTV(IIndexerConfigurationService configService, WebClient c, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "morethantv",
                   name: "MoreThanTV",
                   description: "Private torrent tracker for TV / MOVIES, and the internal tracker for the release group DRACULA.",
                   link: "https://www.morethantv.me/",
                   caps: new TorznabCapabilities
                   {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                       },
                       MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q
                       }
                   },
                   configService: configService,
                   client: c,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataBasicLogin())
        {
            Encoding = Encoding.UTF8;
            Language = "en-US";
            Type = "private";

            var sort = new SingleSelectConfigurationItem("Sort requested from site", new Dictionary<string, string>
            {
                {"time", "time"},
                {"size", "size"},
                {"snatched", "snatched"},
                {"seeders", "seeders"},
                {"leechers", "leechers"},
            })
            { Value = "time" };
            configData.AddDynamic("sort", sort);

            var order = new SingleSelectConfigurationItem("Order requested from site", new Dictionary<string, string>
            {
                {"desc", "desc"},
                {"asc", "asc"}
            })
            { Value = "desc" };
            configData.AddDynamic("order", order);

            AddCategoryMapping(1, TorznabCatType.Movies);
            AddCategoryMapping(2, TorznabCatType.TV);
        }

        /// <summary>
        /// Parse and Return CSRF token
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private string GetToken(string content)
        {
            try
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(content.Trim());
                return dom.QuerySelector<IHtmlInputElement>("input[name=\"token\"]").Value;
            }
            catch (Exception e)
            {
                throw new Exception("Token Could not be parsed from Response, Error?", e);
            }
        }

        /// <summary>
        /// Emulate browser headers -- REQUIRED
        /// </summary>
        private void SetRequestHeaders()
        {
            _emulatedBrowserHeaders.Clear();

            _emulatedBrowserHeaders.Add("referer", SiteLink);
            _emulatedBrowserHeaders.Add("Upgrade-Insecure-Requests", "1");
            _emulatedBrowserHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.72 Safari/537.36");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string> {
                { "username", ConfigData.Username.Value },
                { "password", ConfigData.Password.Value },
                { "submit", "login" },
                { "keeplogged", "1" },
                { "cinfo", "3440|1440|24|360" }
            };

            SetRequestHeaders();

            // Fetch CSRF token
            // We need to clean the old cookies to avoid issues
            var preRequest = await RequestWithCookiesAndRetryAsync(LoginUrl, referer: SiteLink, headers: _emulatedBrowserHeaders, cookieOverride: "");
            // Check if user is logged in. /login redirects to / if so)
            if (preRequest.IsRedirect)
            {
                await FollowIfRedirect(preRequest, SiteLink, null, preRequest.Cookies, true);
            }

            // sid was not set after redirect, attempt to log in again
            if (!preRequest.Cookies.Contains("sid="))
            {
                string token = null;

                try
                {
                    token = GetToken(preRequest.ContentString);
                }
                catch (Exception)
                {
                    var errorMessage = ParseErrorMessage(preRequest);
                    throw new ExceptionWithConfigData(errorMessage, configData);
                }

                // Add CSRF Token to payload
                pairs.Add("token", token);

                var response = await RequestLoginAndFollowRedirect(LoginUrl, pairs, preRequest.Cookies, true, null, SiteLink, headers: _emulatedBrowserHeaders);

                await ConfigureIfOK(response.Cookies, response.Cookies.Contains("sid="), () =>
                {
                    // Couldn't find "sid" cookie, so check for error
                    var parser = new HtmlParser();
                    var dom = parser.ParseDocument(response.ContentString);
                    var errorMessage = dom.QuerySelector(".flash.error").TextContent.Trim();
                    throw new ExceptionWithConfigData(errorMessage, configData);
                });
            }

            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var searchQuery = query.GetQueryString();
            searchQuery = searchQuery.Replace("Marvels", "Marvel"); // strip 's for better results
            var newSearchQuery = Regex.Replace(searchQuery, @"(S\d{2})$", "$1*"); // If we're just seaching for a season (no episode) append an * to include all episodes of that season.
            await GetReleasesAsync(releases, query, newSearchQuery);

            // Always search for torrent groups (complete seasons) too
            var seasonMatch = new Regex(@".*\s[Ss]{1}\d{2}([Ee]{1}\d{2,3})?$").Match(searchQuery);
            if (seasonMatch.Success)
            {
                newSearchQuery = Regex.Replace(searchQuery, @"[Ss]{1}\d{2}([Ee]{1}\d{2,3})?", $"Season {query.Season}");
                await GetReleasesAsync(releases, query, newSearchQuery);
            }

            return releases;
        }

        public override void LoadValuesFromJson(JToken jsonConfig, bool useProtectionService = false)
        {
            base.LoadValuesFromJson(jsonConfig, useProtectionService);

            var sort = (SingleSelectConfigurationItem)configData.GetDynamic("sort");
            _sort = sort != null ? sort.Value : "time";

            var order = (SingleSelectConfigurationItem)configData.GetDynamic("order");
            _order = order != null && order.Value.Equals("asc") ? order.Value : "desc";
        }

        private string GetTorrentSearchUrl(TorznabQuery query, string searchQuery)
        {
            var qc = new NameValueCollection
            {
                { "order_by", _sort },
                { "order_way",  _order },
                { "action", "advanced" },
                { "sizetype", "gb" },
                { "sizerange", "0.01" },
                { "title", searchQuery }
            };

            if (query.Categories.Contains(TorznabCatType.Movies.ID))
            {
                qc.Add("filter_cat[1]", "1"); // HD Movies
                qc.Add("filter_cat[2]", "1"); // SD Movies
            }

            if (query.Categories.Contains(TorznabCatType.TV.ID))
            {
                qc.Add("filter_cat[3]", "1"); // HD EPISODE
                qc.Add("filter_cat[4]", "1"); // SD Episode
                qc.Add("filter_cat[5]", "1"); // HD Season
                qc.Add("filter_cat[6]", "1"); // SD Season
            }

            return BrowseUrl + "?" + qc.GetQueryString();
        }

        private async Task GetReleasesAsync(ICollection<ReleaseInfo> releases, TorznabQuery query, string searchQuery)
        {
            var searchUrl = GetTorrentSearchUrl(query, searchQuery);
            var response = await RequestWithCookiesAndRetryAsync(searchUrl);
            if (response.IsRedirect)
            {
                // re-login
                await ApplyConfiguration(null);
                response = await RequestWithCookiesAndRetryAsync(searchUrl);
            }

            try
            {
                var parser = new HtmlParser();
                var document = parser.ParseDocument(response.ContentString);
                var torrents = document.QuerySelectorAll("#torrent_table > tbody > tr.torrent");
                var movies = new[] { "movie" };
                var tv = new[] { "season", "episode" };

                // Loop through all torrents checking for groups
                foreach (var torrent in torrents)
                {
                    // Parse required data
                    var torrentGroup = torrent.QuerySelectorAll("table a[href^=\"/torrents.php?action=download\"]");
                    foreach (var downloadAnchor in torrentGroup)
                    {
                        var title = downloadAnchor.ParentElement.ParentElement.ParentElement.TextContent.Trim();
                        title = CleanUpTitle(title);

                        var category = torrent.QuerySelector(".cats_col div").GetAttribute("title");
                        // default to Other
                        var categoryId = TorznabCatType.Other.ID;

                        if (movies.Any(category.Contains))
                            categoryId = TorznabCatType.Movies.ID;
                        else if (tv.Any(category.Contains))
                            categoryId = TorznabCatType.TV.ID;

                        releases.Add(GetReleaseInfo(torrent, downloadAnchor, title, categoryId));
                    }
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.ContentString, ex);
            }
        }

        /// <summary>
        /// Gather Release info from torrent table. Target using css
        /// </summary>
        /// <param name="row"></param>
        /// <param name="downloadAnchor"></param>
        /// <param name="title"></param>
        /// <param name="category"></param>
        /// <returns></returns>
        private ReleaseInfo GetReleaseInfo(IElement row, IElement downloadAnchor, string title, int category)
        {

            // count from bottom
            const int FILES_COL = 8;
            /*const int COMMENTS_COL = 7;*/
            const int DATE_COL = 6;
            const int FILESIZE_COL = 5;
            const int SNATCHED_COL = 4;
            const int SEEDS_COL = 3;
            const int LEECHERS_COL = 2;
            /*const int USER_COL = 1;*/


            var downloadAnchorHref = (downloadAnchor as IHtmlAnchorElement).Href;
            var queryParams = HttpUtility.ParseQueryString(downloadAnchorHref, Encoding.UTF8);
            var torrentId = queryParams["id"];

            var qFiles = row.QuerySelector("td:nth-last-child(" + FILES_COL + ")").TextContent;

            var fileCount = ParseUtil.CoerceLong(qFiles);
            var qPublishDate = row.QuerySelector("td:nth-last-child(" + DATE_COL + ") .time").Attributes["title"].Value;
            var publishDate = DateTime.ParseExact(qPublishDate, "MMM dd yyyy, HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToLocalTime();
            var qPoster = row.QuerySelector("div.tp-banner img")?.GetAttribute("src");
            var poster = (qPoster != null && !qPoster.Contains("caticons")) ? new Uri(qPoster) : null;
            var description = row.QuerySelector("div.tags")?.TextContent.Trim();
            var fileSize = row.QuerySelector("td:nth-last-child(" + FILESIZE_COL + ")").TextContent.Trim();
            var snatched = row.QuerySelector("td:nth-last-child(" + SNATCHED_COL + ")").TextContent.Trim();
            var seeds = row.QuerySelector("td:nth-last-child(" + SEEDS_COL + ")").TextContent.Trim();
            var leechs = row.QuerySelector("td:nth-last-child(" + LEECHERS_COL + ")").TextContent.Trim();

            if (fileSize.Length <= 0 || snatched.Length <= 0 || seeds.Length <= 0 || leechs.Length <= 0)
            {
                // Size (xx.xx GB[ (Max)]) Snatches (xx) Seeders (xx) Leechers (xx)
                throw new Exception($"We expected 4 torrent datas.");
            }

            var size = ReleaseInfo.GetBytes(fileSize);
            var grabs = int.Parse(snatched, NumberStyles.AllowThousands, CultureInfo.InvariantCulture);
            var seeders = int.Parse(seeds, NumberStyles.AllowThousands, CultureInfo.InvariantCulture);
            var leechers = int.Parse(leechs, NumberStyles.AllowThousands, CultureInfo.InvariantCulture);
            var detailsUri = new Uri(DetailsUrl + "?torrentid=" + torrentId);
            var downloadLink = new Uri(BrowseUrl + "?action=download&id=" + torrentId);

            return new ReleaseInfo
            {
                Title = title,
                Category = new List<int> { category },
                Link = downloadLink,
                PublishDate = publishDate,
                Poster = poster,
                Description = description,
                Seeders = seeders,
                Peers = seeders + leechers,
                Files = fileCount,
                Size = size,
                Grabs = grabs,
                Guid = downloadLink,
                Details = detailsUri,
                DownloadVolumeFactor = 0, // ratioless tracker
                UploadVolumeFactor = 1
            };
        }

        /// <summary>
        /// Parse Error Messages from using CSS classes
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        private string ParseErrorMessage(WebResult response)
        {
            var parser = new HtmlParser();
            var dom = parser.ParseDocument(response.ContentString);
            var errorMessage = response.Status == System.Net.HttpStatusCode.Forbidden
                ? dom.QuerySelector(".time").Parent.TextContent.Trim()
                : dom.QuerySelector(".flash.error").TextContent.Trim();

            return errorMessage;
        }

        /// <summary>
        /// Clean Up any title stuff
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        private string CleanUpTitle(string title)
        {
            return title
                .Replace(".", " ")
                .Replace("4K", "2160p"); // sonarr cleanup
        }
    }
}
