using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class Shazbat : IndexerBase
    {
        public override string Id => "shazbat";
        public override string Name => "Shazbat";
        public override string Description => "Shazbat is a PRIVATE Torrent Tracker with highly curated TV content";
        public override string SiteLink { get; protected set; } = "https://www.shazbat.tv/";
        public override string Language => "en-US";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private string LoginUrl => SiteLink + "login";
        private string SearchUrl => SiteLink + "search";
        private string TorrentsUrl => SiteLink + "torrents";
        private string ShowUrl => SiteLink + "show";

        private new ConfigurationDataShazbat configData => (ConfigurationDataShazbat)base.configData;

        public Shazbat(IIndexerConfigurationService configService, WebClient c, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(configService: configService,
                   client: c,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataShazbat())
        {
            webclient.requestDelay = 5.1;
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                }
            };

            caps.Categories.AddCategoryMapping(1, TorznabCatType.TV);
            caps.Categories.AddCategoryMapping(2, TorznabCatType.TVSD);
            caps.Categories.AddCategoryMapping(3, TorznabCatType.TVHD);

            return caps;
        }

        private int ShowPagesFetchLimit => int.TryParse(configData.ShowPagesFetchLimit.Value, out var limit) && limit is > 0 and <= 5 ? limit : 2;

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string>
            {
                { "referer", "" },
                { "query", "" },
                { "tv_timezone", "0" },
                { "tv_login", configData.Username.Value },
                { "tv_password", configData.Password.Value }
            };

            // Get cookie
            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, LoginUrl);
            await ConfigureIfOK(result.Cookies, result.ContentString?.Contains("glyphicon-log-out") == true, () =>
            {
                throw new ExceptionWithConfigData("The username and password entered do not match.", configData);
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            WebResult response;

            var releases = new List<ReleaseInfo>();
            var searchUrls = new List<WebRequest>();

            var hasGlobalFreeleech = false;

            var searchTerm = query.SanitizedSearchTerm;
            var term = FixSearchTerm(searchTerm);

            var showTorrentsHeaders = new Dictionary<string, string>
            {
                { "Content-Type", "application/x-www-form-urlencoded" },
                { "X-Requested-With", "XMLHttpRequest" },
            };

            var showTorrentsBody = new Dictionary<string, string>
            {
                { "portlet", "true" },
                { "tab", "true" }
            };

            if (!string.IsNullOrWhiteSpace(term))
            {
                var searchBody = new Dictionary<string, string>
                {
                    { "search", term }
                };

                response = await RequestWithCookiesAndRetryAsync(SearchUrl, method: RequestType.POST, referer: TorrentsUrl, data: searchBody);
                response = await ReloginIfNecessaryAsync(response);

                var parser = new HtmlParser();
                using var dom = parser.ParseDocument(response.ContentString);

                hasGlobalFreeleech = dom.QuerySelector("span:contains(\"Freeleech until:\"):has(span.datetime)") != null;

                releases.AddRange(ParseResults(response, query, searchTerm, hasGlobalFreeleech));

                var shows = dom.QuerySelectorAll("div.show[data-id]");
                if (shows.Any())
                {
                    var showPagesFetchLimit = ShowPagesFetchLimit;

                    if (showPagesFetchLimit < 1 || showPagesFetchLimit > 5)
                        throw new Exception($"Value for Show Pages Fetch Limit should be between 1 and 5. Current value: {showPagesFetchLimit}.");

                    if (shows.Length > showPagesFetchLimit)
                        logger.Debug($"Your search returned {shows.Length} shows. Use a more specific search term for more relevant results.");

                    foreach (var show in shows.Take(showPagesFetchLimit))
                    {
                        var showTorrentsQueryParams = new Dictionary<string, string>
                        {
                            { "id", show.GetAttribute("data-id") },
                            { "show_mode", "torrents" }
                        };

                        searchUrls.Add(new WebRequest
                        {
                            Url = $"{ShowUrl}?{showTorrentsQueryParams.GetQueryString()}",
                            Type = RequestType.POST,
                            PostData = showTorrentsBody,
                            Headers = showTorrentsHeaders
                        });
                    }
                }
            }
            else
                searchUrls.Add(new WebRequest { Url = TorrentsUrl, Type = RequestType.GET });

            foreach (var searchUrl in searchUrls)
            {
                response = await RequestWithCookiesAsync(url: searchUrl.Url, method: searchUrl.Type, data: searchUrl.PostData, headers: searchUrl.Headers);
                response = await ReloginIfNecessaryAsync(response);

                try
                {
                    releases.AddRange(ParseResults(response, query, searchTerm, hasGlobalFreeleech));
                }
                catch (Exception ex)
                {
                    OnParseError(response.ContentString, ex);
                }
            }

            return releases;
        }

        private IList<ReleaseInfo> ParseResults(WebResult response, TorznabQuery query, string searchTerm, bool hasGlobalFreeleech = false)
        {
            var releases = new List<ReleaseInfo>();

            var parser = new HtmlParser();
            using var dom = parser.ParseDocument(response.ContentString);

            if (!hasGlobalFreeleech)
                hasGlobalFreeleech = dom.QuerySelector("span:contains(\"Freeleech until:\"):has(span.datetime)") != null;

            var publishDate = DateTime.Now;

            var rows = dom.QuerySelectorAll("#torrent-table tr.eprow, table tr.eprow");
            foreach (var row in rows)
            {
                var title = ParseTitle(row.QuerySelector("td:nth-of-type(3)"));

                if ((query.ImdbID == null || !TorznabCaps.MovieSearchImdbAvailable) && !query.MatchQueryStringAND(title, queryStringOverride: searchTerm))
                    continue;

                var link = new Uri(SiteLink + row.QuerySelector("td:nth-of-type(5) a[href^=\"load_torrent?\"]")?.GetAttribute("href"));
                var details = new Uri(SiteLink + row.QuerySelector("td:nth-of-type(5) [href^=\"torrent_info?\"]")?.GetAttribute("href"));

                var infoString = row.QuerySelector("td:nth-of-type(4)")?.TextContent.Trim() ?? string.Empty;
                var infoRegex = new Regex(@"\((?<size>\d+)\):(?<seeders>\d+) \/ :(?<leechers>\d+)$", RegexOptions.Compiled);
                var matchInfo = infoRegex.Match(infoString);
                var size = matchInfo.Groups["size"].Success && long.TryParse(matchInfo.Groups["size"].Value, out var outSize) ? outSize : 0;
                var seeders = matchInfo.Groups["seeders"].Success && int.TryParse(matchInfo.Groups["seeders"].Value, out var outSeeders) ? outSeeders : 0;
                var leechers = matchInfo.Groups["leechers"].Success && int.TryParse(matchInfo.Groups["leechers"].Value, out var outLeechers) ? outLeechers : 0;

                var dateTimestamp = row.QuerySelector(".datetime[data-timestamp]")?.GetAttribute("data-timestamp");
                publishDate = dateTimestamp != null && ParseUtil.TryCoerceDouble(dateTimestamp, out var timestamp) ? DateTimeUtil.UnixTimestampToDateTime(timestamp) : publishDate.AddMinutes(-1);

                var release = new ReleaseInfo
                {
                    Guid = link,
                    Link = link,
                    Details = details,
                    Title = title,
                    Category = ParseCategories(title),
                    Size = size,
                    Seeders = seeders,
                    Peers = seeders + leechers,
                    PublishDate = publishDate,
                    Genres = row.QuerySelectorAll("label.label-tag").Select(t => t.TextContent.Trim()).ToList(),
                    DownloadVolumeFactor = hasGlobalFreeleech ? 0 : 1,
                    UploadVolumeFactor = 1,
                    MinimumRatio = 1,
                    MinimumSeedTime = 172800 // 48 hours
                };

                var posterStyle = row.QuerySelector("div[style^=\"cursor: pointer; background-image:url\"]")?.GetAttribute("style");
                if (!string.IsNullOrEmpty(posterStyle))
                {
                    var posterStr = Regex.Match(posterStyle, @"url\('(?<poster>.*)'\);").Groups["poster"].Value;
                    release.Poster = new Uri(SiteLink + posterStr);
                }

                releases.Add(release);
            }

            return releases;
        }

        private static string ParseTitle(IElement titleRow)
        {
            var title = titleRow?.ChildNodes.First(n => n.NodeType == NodeType.Text && n.TextContent.Trim() != string.Empty);

            return title?.TextContent.Trim();
        }

        private static string FixSearchTerm(string term)
        {
            term = Regex.Replace(term, @"\b[S|E]\d+\b", string.Empty, RegexOptions.IgnoreCase);
            term = Regex.Replace(term, @"(.+)\b\d{4}(\.\d{2}\.\d{2})?\b", "$1");
            term = Regex.Replace(term, @"[\.\s\(\)\[\]]+", " ");

            return term.ToLower().Trim();
        }

        protected virtual List<int> ParseCategories(string title) => title.Contains("1080p") || title.Contains("1080i") || title.Contains("720p") ? new List<int> { TorznabCatType.TVHD.ID } : new List<int> { TorznabCatType.TVSD.ID };

        private async Task<WebResult> ReloginIfNecessaryAsync(WebResult response)
        {
            if (response.ContentString.IndexOf("sign in now", StringComparison.InvariantCultureIgnoreCase) == -1)
                return response;

            logger.Debug("Session expired. Relogin.");

            await ApplyConfiguration(null);
            response.Request.Cookies = CookieHeader;
            return await webclient.GetResultAsync(response.Request);
        }
    }
}
