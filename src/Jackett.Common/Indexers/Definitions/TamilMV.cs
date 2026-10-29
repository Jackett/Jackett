using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Jackett.Common.Extensions;
using Jackett.Common.Helpers;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class TamilMV : IndexerBase
    {
        public override string Id => "tamilmv";
        public override string Name => "1TamilMV";
        public override string Description => "1TamilMV is a Public Indian torrent tracker for Tamil, Telugu, Hindi, Malayalam, and Kannada releases.";
        public override string SiteLink { get; protected set; } = "https://www.1tamilmv.style/";

        public override string[] AlternativeSiteLinks => new[]
        {
            "https://www.1tamilmv.style/",
        };

        public override string[] LegacySiteLinks => new[]
        {
            "https://www.1tamilmv.ac/",
            "https://www.1tamilmv.eu/",
            "https://www.1tamilmv.pizza/",
            "https://www.1tamilmv.wtf/",
            "https://www.1tamilmv.unblockninja.com/",
        };

        public override string Language => "ta-IN";
        public override string Type => "public";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private static readonly Regex TopicRegex = new Regex(@"/forums/topic/(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex BtihRegex = new Regex(@"urn:btih:([a-zA-Z0-9]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex SizeRegex = new Regex(@"(?:^|[^\w.])(\d+(?:\.\d+)?)\s*(GB|MB|KB)(?![a-zA-Z])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex SitePrefixRegex = new Regex(@"^(?:www\.)?(?:1?tamilmv|tamilblasters|tamilrockers)\.[a-z0-9]+\s*-\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex SkipRegex = new Regex(@"(?i)\b(?:trailer|sample|teaser|promo|audio\s*launch|song\s*promo|video\s*song)\b", RegexOptions.Compiled);
        private static readonly Regex TvRegex = new Regex(@"(?i)\b(?:s\d+|ep\s*\(?\d+|season\s*\d+|series|episode)\b", RegexOptions.Compiled);
        private static readonly Regex AudioRegex = new Regex(@"(?i)\b(?:ost|soundtrack|flac|mp3\s*songs?|audio\s*songs?)\b", RegexOptions.Compiled);
        private static readonly Regex UhdRegex = new Regex(@"(?i)\b(?:2160p|4k|uhd)\b", RegexOptions.Compiled);
        private static readonly Regex HdRegex = new Regex(@"(?i)\b(?:1080p|fhd|720p|hd)\b", RegexOptions.Compiled);
        private static readonly Regex SdRegex = new Regex(@"(?i)\b(?:480p|sd)\b", RegexOptions.Compiled);
        private static readonly Regex ExcludedForumRegex = new Regex(
            @"video[- ]songs?|mp3[- ]songs?|cinema[- ]news|general[- ]videos|watch[- ].*?[- ]online|post[- &]*share[- ]here|request[- ]section|members[- ]lounge|/forums?/forum/(?:15|16|18|28|30|32|40|41|43|51|52|54|62|63|65|73|74|75|76|89|92|93|95|102|103|104)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly ConcurrentDictionary<string, (DateTime expiresAt, List<ReleaseInfo> releases)> _topicCache = new();
        private const int MaxReleases = 100;
        private const int MaxTopics = 20;
        private const int MaxSearchPages = 2;

        public TamilMV(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps, ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationData())
        {
            // Avoid Cloudflare too many requests limiter / site thrashing
            webclient.requestDelay = 1.1;

            var stripPrefix = new BoolConfigurationItem("Strip website prefix from torrent titles") { Value = true };
            configData.AddDynamic("stripPrefix", stripPrefix);

            var filterSamples = new BoolConfigurationItem("Filter out trailers, samples, and promo releases") { Value = true };
            configData.AddDynamic("filterSamples", filterSamples);
        }

        private static TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q
                },
                MusicSearchParams = new List<MusicSearchParam>
                {
                    MusicSearchParam.Q
                }
            };

            caps.Categories.AddCategoryMapping(2000, TorznabCatType.Movies, "Movies");
            caps.Categories.AddCategoryMapping(2040, TorznabCatType.MoviesHD, "Movies/HD");
            caps.Categories.AddCategoryMapping(2045, TorznabCatType.MoviesUHD, "Movies/UHD");
            caps.Categories.AddCategoryMapping(2030, TorznabCatType.MoviesSD, "Movies/SD");
            caps.Categories.AddCategoryMapping(2010, TorznabCatType.MoviesForeign, "Movies/Foreign");

            caps.Categories.AddCategoryMapping(5000, TorznabCatType.TV, "TV");
            caps.Categories.AddCategoryMapping(5040, TorznabCatType.TVHD, "TV/HD");
            caps.Categories.AddCategoryMapping(5045, TorznabCatType.TVUHD, "TV/UHD");
            caps.Categories.AddCategoryMapping(5030, TorznabCatType.TVSD, "TV/SD");
            caps.Categories.AddCategoryMapping(5020, TorznabCatType.TVForeign, "TV/Foreign");

            caps.Categories.AddCategoryMapping(3000, TorznabCatType.Audio, "Audio");
            caps.Categories.AddCategoryMapping(3010, TorznabCatType.AudioMP3, "Audio/MP3");
            caps.Categories.AddCategoryMapping(3020, TorznabCatType.AudioLossless, "Audio/Lossless");
            caps.Categories.AddCategoryMapping(3040, TorznabCatType.AudioVideo, "Audio/Video");

            return caps;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var releases = await PerformQuery(new TorznabQuery());

            await ConfigureIfOK(string.Empty, releases.Any(), () =>
                throw new Exception("Could not find releases from 1TamilMV"));

            return IndexerConfigurationStatus.Completed;
        }

        public override async Task<byte[]> Download(Uri link)
        {
            var uncleanLink = UncleanLink(link);
            return await Download(uncleanLink, RequestType.GET, referer: SiteLink);
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var stripPrefix = ((BoolConfigurationItem)configData.GetDynamic("stripPrefix"))?.Value ?? true;
            var filterSamples = ((BoolConfigurationItem)configData.GetDynamic("filterSamples"))?.Value ?? true;

            var topicUrls = new List<string>();
            var searchTerm = query.GetQueryString();

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                topicUrls = await ExtractHomepageTopicsAsync(SiteLink, MaxTopics);
            }
            else
            {
                topicUrls = await SearchTopicsAsync(SiteLink, searchTerm);
            }

            var releases = new List<ReleaseInfo>();
            var parser = new HtmlParser();

            foreach (var topicUrl in topicUrls)
            {
                if (releases.Count >= MaxReleases)
                {
                    break;
                }

                if (IsExcludedForum(null, topicUrl))
                {
                    continue;
                }

                if (_topicCache.TryGetValue(topicUrl, out var cached) && DateTime.UtcNow < cached.expiresAt)
                {
                    var matchingCached = searchTerm.IsNotNullOrWhiteSpace()
                        ? cached.releases.Where(r => query.MatchQueryStringAND(r.Title))
                        : cached.releases;

                    releases.AddRange(matchingCached);
                    continue;
                }

                try
                {
                    var topicResponse = await RequestWithCookiesAndRetryAsync(topicUrl, referer: SiteLink);
                    if (topicResponse.Status != HttpStatusCode.OK)
                    {
                        logger.Warn($"Failed to fetch 1TamilMV topic {topicUrl}: HTTP {topicResponse.Status}");
                        continue;
                    }

                    using var doc = parser.ParseDocument(topicResponse.ContentString);
                    var topicReleases = ExtractTopicReleases(doc, topicUrl, SiteLink, stripPrefix, filterSamples);

                    _topicCache[topicUrl] = (DateTime.UtcNow.AddDays(1), topicReleases);

                    var matchingReleases = searchTerm.IsNotNullOrWhiteSpace()
                        ? topicReleases.Where(r => query.MatchQueryStringAND(r.Title))
                        : topicReleases;

                    releases.AddRange(matchingReleases);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"Error extracting releases from 1TamilMV topic {topicUrl}");
                }
            }

            if (_topicCache.Count > 500)
            {
                var now = DateTime.UtcNow;
                foreach (var kvp in _topicCache)
                {
                    if (kvp.Value.expiresAt < now)
                        _topicCache.TryRemove(kvp.Key, out _);
                }
            }

            return releases.OrderByDescending(r => r.PublishDate).Take(MaxReleases).ToList();
        }

        private async Task<List<string>> ExtractHomepageTopicsAsync(string siteLink, int limit)
        {
            var response = await RequestWithCookiesAndRetryAsync(siteLink);
            if (response.Status != HttpStatusCode.OK)
            {
                throw new Exception($"Failed to fetch 1TamilMV homepage ({siteLink}): HTTP {response.Status}");
            }

            var parser = new HtmlParser();
            using var doc = parser.ParseDocument(response.ContentString);

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var bangerContainers = doc.QuerySelectorAll(".banger-container");
            var topReleases = new List<string>();
            var recentlyAdded = new List<string>();

            foreach (var container in bangerContainers)
            {
                var headerText = container.QuerySelector(".banger-header")?.TextContent?.ToUpperInvariant() ?? string.Empty;
                var links = ExtractTopicLinksFromElement(container, siteLink);

                if (headerText.Contains("TOP RELEASES"))
                {
                    topReleases.AddRange(links);
                }
                else if (headerText.Contains("RECENTLY ADDED"))
                {
                    recentlyAdded.AddRange(links);
                }
            }

            var topicUrls = topReleases.Where(seen.Add).ToList();
            topicUrls.AddRange(recentlyAdded.Where(seen.Add));

            if (topicUrls.Count == 0)
            {
                var fallbackElements = doc.QuerySelectorAll(".banger-container, .ipsType_break, .ipsContained, .cTopicList, .ipsDataList");
                foreach (var el in fallbackElements)
                {
                    topicUrls.AddRange(ExtractTopicLinksFromElement(el, siteLink).Where(seen.Add));
                }

                if (topicUrls.Count == 0)
                {
                    foreach (var a in doc.QuerySelectorAll("a[href]"))
                    {
                        var href = a.GetAttribute("href");
                        var clean = NormalizeTopicUrl(href, siteLink);
                        if (!string.IsNullOrEmpty(clean) && seen.Add(clean))
                            topicUrls.Add(clean);
                    }
                }
            }

            return topicUrls.Take(limit).ToList();
        }

        private async Task<List<string>> SearchTopicsAsync(string siteLink, string searchTerm, int maxPages = MaxSearchPages)
        {
            var topicUrls = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var parser = new HtmlParser();

            for (var page = 1; page <= maxPages; page++)
            {
                var pageParam = page > 1 ? $"&page={page}" : string.Empty;
                var searchUrl = $"{siteLink}index.php?/search/&q={Uri.EscapeDataString(searchTerm)}&type=forums_topic{pageParam}";
                var response = await RequestWithCookiesAndRetryAsync(searchUrl, referer: siteLink);
                if (response.Status != HttpStatusCode.OK)
                {
                    if (page == 1)
                    {
                        logger.Warn($"Failed to search 1TamilMV: HTTP {response.Status}");
                    }
                    break;
                }

                using var doc = parser.ParseDocument(response.ContentString);
                var resultItems = doc.QuerySelectorAll(".ipsStreamItem_title a[href], .ipsStreamItem a[href*='/forums/topic/'], [data-role='searchResults'] a[href*='/forums/topic/']");
                if (resultItems.Length == 0)
                {
                    break;
                }

                var addedThisPage = 0;
                foreach (var a in resultItems)
                {
                    var container = a.Closest(".ipsStreamItem, [data-role='activityItem']");
                    if (container != null)
                    {
                        var hasExcludedForum = container.QuerySelectorAll("a[href]")
                            .Any(link => ExcludedForumRegex.IsMatch(link.GetAttribute("href") ?? string.Empty) ||
                                         ExcludedForumRegex.IsMatch(link.TextContent ?? string.Empty));
                        if (hasExcludedForum)
                        {
                            continue;
                        }
                    }

                    var href = a.GetAttribute("href");
                    var clean = NormalizeTopicUrl(href, siteLink);
                    if (!string.IsNullOrEmpty(clean) && seen.Add(clean))
                    {
                        topicUrls.Add(clean);
                        addedThisPage++;
                    }
                }

                if (addedThisPage == 0)
                {
                    break;
                }

                var nextItem = doc.QuerySelector(".ipsPagination_next");
                var hasNextPage = nextItem != null
                    ? !nextItem.ClassList.Contains("ipsPagination_inactive") && nextItem.QuerySelector("a") != null
                    : doc.QuerySelector("a[rel='next']") != null;

                if (!hasNextPage)
                {
                    break;
                }
            }

            return topicUrls;
        }

        private static List<string> ExtractTopicLinksFromElement(IElement element, string siteLink)
        {
            var list = new List<string>();
            foreach (var a in element.QuerySelectorAll("a[href]"))
            {
                var href = a.GetAttribute("href");
                var clean = NormalizeTopicUrl(href, siteLink);
                if (!string.IsNullOrEmpty(clean) && !list.Contains(clean))
                {
                    list.Add(clean);
                }
            }
            return list;
        }

        private static string NormalizeTopicUrl(string href, string siteLink)
        {
            if (string.IsNullOrWhiteSpace(href))
                return string.Empty;

            if (ExcludedForumRegex.IsMatch(href))
                return string.Empty;

            var m = TopicRegex.Match(href);
            if (!m.Success || !int.TryParse(m.Groups[1].Value, out var topicId) || topicId < 1000)
                return string.Empty;

            var url = href.Split('#')[0];
            if (url.Contains("&do=findComment"))
            {
                url = url.Split(new[] { "&do=" }, StringSplitOptions.None)[0];
            }
            if (url.Contains("?do=findComment"))
            {
                url = url.Split(new[] { "?do=" }, StringSplitOptions.None)[0];
            }

            if (url.StartsWith("/"))
            {
                return siteLink.TrimEnd('/') + url;
            }

            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return new Uri(new Uri(siteLink), url).ToString();
            }

            return url;
        }

        private List<ReleaseInfo> ExtractTopicReleases(
            IHtmlDocument doc,
            string topicUrl,
            string siteLink,
            bool stripPrefix,
            bool filterSamples)
        {
            var results = new List<ReleaseInfo>();

            if (IsExcludedForum(doc, topicUrl))
            {
                logger.Debug($"1TamilMV skipping topic in excluded forum: {topicUrl}");
                return results;
            }

            var magnetLinks = doc.QuerySelectorAll("a[href^='magnet:?']")
                .Select(a => a.GetAttribute("href")?.Trim())
                .Where(m => !string.IsNullOrEmpty(m))
                .ToList();

            var torrentLinks = doc.QuerySelectorAll("a[data-fileext='torrent'][href]").ToList();
            if (torrentLinks.Count == 0)
            {
                torrentLinks = doc.QuerySelectorAll("a[href]")
                    .Where(a =>
                    {
                        var h = (a.GetAttribute("href") ?? string.Empty).ToLowerInvariant();
                        var txt = (a.TextContent ?? string.Empty).Trim().ToLowerInvariant();
                        return h.Contains("attachment.php?id=") || h.EndsWith(".torrent") || txt.EndsWith(".torrent");
                    })
                    .ToList();
            }

            var dateStr = doc.QuerySelector("time[datetime]")?.GetAttribute("datetime");
            var publishDate = DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var parsedDate)
                ? parsedDate
                : DateTime.UtcNow;

            for (var i = 0; i < torrentLinks.Count; i++)
            {
                var a = torrentLinks[i];
                var href = a.GetAttribute("href")?.Trim();
                var rawTitle = a.TextContent?.Trim();
                if (string.IsNullOrEmpty(rawTitle))
                {
                    rawTitle = a.GetAttribute("title")?.Trim();
                }

                if (string.IsNullOrEmpty(href) || string.IsNullOrEmpty(rawTitle))
                    continue;

                if (!rawTitle.EndsWith(".torrent", StringComparison.OrdinalIgnoreCase) &&
                    href.IndexOf("attachment.php?id=", StringComparison.OrdinalIgnoreCase) < 0 &&
                    !href.EndsWith(".torrent", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (filterSamples && SkipRegex.IsMatch(rawTitle))
                {
                    logger.Debug($"1TamilMV skipping filtered title: {rawTitle}");
                    continue;
                }

                if (href.StartsWith("/"))
                {
                    href = siteLink.TrimEnd('/') + href;
                }
                else if (!href.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    href = new Uri(new Uri(siteLink), href).ToString();
                }

                var cleanTitle = CleanTitle(rawTitle, stripPrefix);
                var (sizeBytes, _) = ParseSize(cleanTitle);

                var magnetUrl = i < magnetLinks.Count ? magnetLinks[i] : (magnetLinks.Count > 0 ? magnetLinks[0] : string.Empty);
                string infoHash = null;
                if (!string.IsNullOrEmpty(magnetUrl))
                {
                    var mHash = BtihRegex.Match(magnetUrl);
                    if (mHash.Success)
                    {
                        infoHash = mHash.Groups[1].Value.ToLowerInvariant();
                    }
                }

                var categories = DetermineCategories(cleanTitle);

                var release = new ReleaseInfo
                {
                    Title = cleanTitle,
                    Details = new Uri(topicUrl),
                    Guid = new Uri(href),
                    Link = new Uri(href),
                    MagnetUri = !string.IsNullOrEmpty(magnetUrl) ? new Uri(magnetUrl) : null,
                    InfoHash = infoHash,
                    PublishDate = publishDate,
                    Category = categories,
                    Size = sizeBytes,
                    Seeders = 1,
                    Peers = 1,
                    DownloadVolumeFactor = 0,
                    UploadVolumeFactor = 1
                };

                results.Add(release);
            }

            return results;
        }

        public static string CleanTitle(string rawTitle, bool stripPrefix = true)
        {
            if (string.IsNullOrWhiteSpace(rawTitle))
                return string.Empty;

            var title = WebUtility.HtmlDecode(rawTitle).Trim();

            if (stripPrefix)
            {
                title = SitePrefixRegex.Replace(title, string.Empty);
            }

            if (title.EndsWith(".torrent", StringComparison.OrdinalIgnoreCase))
            {
                title = title.Substring(0, title.Length - ".torrent".Length).Trim();
            }

            title = Regex.Replace(title, @"\s+", " ").Trim();
            return title;
        }

        public static long ParseSize(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return 0;
            }

            var m = SizeRegex.Match(title);
            if (!m.Success)
            {
                return 0;
            }

            if (!double.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var sizeVal))
            {
                return 0;
            }

            var unit = m.Groups[2].Value.ToUpperInvariant();
            return unit switch
            {
                "GB" => sizeVal.Gigabytes(),
                "MB" => sizeVal.Megabytes(),
                "KB" => (long)(sizeVal * 1024L),
                _ => 0
            };
        }

        public static List<int> DetermineCategories(string title)
        {
            var isAudio = AudioRegex.IsMatch(title);
            var isTv = TvRegex.IsMatch(title);
            var isUhd = UhdRegex.IsMatch(title);
            var isHd = HdRegex.IsMatch(title);
            var isSd = SdRegex.IsMatch(title);

            var cats = new List<int>();

            if (isAudio)
            {
                cats.Add(TorznabCatType.Audio.ID);
                if (title.IndexOf("flac", StringComparison.OrdinalIgnoreCase) >= 0)
                    cats.Add(TorznabCatType.AudioLossless.ID);
                else
                    cats.Add(TorznabCatType.AudioMP3.ID);
                return cats;
            }

            if (isTv)
            {
                cats.Add(TorznabCatType.TV.ID);
                if (isUhd)
                    cats.Add(TorznabCatType.TVUHD.ID);
                else if (isHd)
                    cats.Add(TorznabCatType.TVHD.ID);
                else if (isSd)
                    cats.Add(TorznabCatType.TVSD.ID);

                cats.Add(TorznabCatType.TVForeign.ID);
            }
            else
            {
                cats.Add(TorznabCatType.Movies.ID);
                if (isUhd)
                    cats.Add(TorznabCatType.MoviesUHD.ID);
                else if (isHd)
                    cats.Add(TorznabCatType.MoviesHD.ID);
                else if (isSd)
                    cats.Add(TorznabCatType.MoviesSD.ID);

                cats.Add(TorznabCatType.MoviesForeign.ID);
            }

            return cats;
        }

        public static bool IsExcludedForum(IHtmlDocument doc, string topicUrl = null)
        {
            if (!string.IsNullOrEmpty(topicUrl) && ExcludedForumRegex.IsMatch(topicUrl))
            {
                return true;
            }

            if (doc != null)
            {
                var breadcrumbLinks = doc.QuerySelectorAll(".ipsBreadcrumb a[href], [data-role='breadcrumbList'] a[href], .ipsType_breadcrumb a[href]");
                foreach (var a in breadcrumbLinks)
                {
                    var href = a.GetAttribute("href") ?? string.Empty;
                    if (ExcludedForumRegex.IsMatch(href))
                    {
                        return true;
                    }

                    var text = a.TextContent ?? string.Empty;
                    if (ExcludedForumRegex.IsMatch(text))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
