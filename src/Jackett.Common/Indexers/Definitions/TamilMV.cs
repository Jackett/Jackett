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
        public override string SiteLink { get; protected set; } = "https://www.1tamilmv.solar/";

        public override string[] AlternativeSiteLinks => new[]
        {
            "https://www.1tamilmv.solar/",
            "https://www.1tamilmv.fi/",
            "https://www.1tamilmv.pm/",
            "https://www.1tamilmv.vin/",
            "https://www.1tamilmv.yt/",
            "https://www.1tamilmv.lat/",
            "https://www.1tamilmv.wf/",
            "https://www.1tamilmv.gold/",
            "https://www.1tamilmv.cz/",
            "https://www.1tamilmv.unblockninja.com/"
        };

        public override string[] LegacySiteLinks => new[]
        {
            "https://www.1tamilmv.fi/",
            "https://www.1tamilmv.pm/",
            "https://www.1tamilmv.vin/",
            "https://www.1tamilmv.yt/",
            "https://www.1tamilmv.lat/",
            "https://www.1tamilmv.wf/",
            "https://www.1tamilmv.gold/",
            "https://www.1tamilmv.cz/",
            "https://www.1tamilmv.unblockninja.com/"
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

        private static string _cachedWorkingDomain;
        private static DateTime _domainCacheExpiresAt = DateTime.MinValue;
        private static readonly ConcurrentDictionary<string, (DateTime expiresAt, List<ReleaseInfo> releases)> _topicCache = new();

        public TamilMV(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps, ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationData())
        {
            var gatewayUrl = new StringConfigurationItem("Permanent Gateway URL") { Value = "https://www.1tamilmv.fi/" };
            configData.AddDynamic("gatewayUrl", gatewayUrl);

            var stripPrefix = new BoolConfigurationItem("Strip website prefix from torrent titles") { Value = true };
            configData.AddDynamic("stripPrefix", stripPrefix);

            var filterSamples = new BoolConfigurationItem("Filter out trailers, samples, and promo releases") { Value = true };
            configData.AddDynamic("filterSamples", filterSamples);

            var maxTopics = new SingleSelectConfigurationItem("Topics to scrape per query", new Dictionary<string, string>
            {
                { "10", "10 topics" },
                { "15", "15 topics (default)" },
                { "25", "25 topics" },
                { "40", "40 topics" }
            })
            { Value = "15" };
            configData.AddDynamic("maxTopics", maxTopics);
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

        private async Task<string> ResolveWorkingDomainAsync(bool forceRefresh = false)
        {
            var configuredLink = SiteLink;
            if (!string.IsNullOrWhiteSpace(configuredLink) &&
                configuredLink.IndexOf("1tamilmv.fi", StringComparison.OrdinalIgnoreCase) < 0 &&
                !forceRefresh)
            {
                return configuredLink.TrimEnd('/') + "/";
            }

            if (!forceRefresh && _cachedWorkingDomain != null && DateTime.UtcNow < _domainCacheExpiresAt)
            {
                return _cachedWorkingDomain;
            }

            var gatewayUrl = ((StringConfigurationItem)configData.GetDynamic("gatewayUrl"))?.Value;
            if (string.IsNullOrWhiteSpace(gatewayUrl))
            {
                gatewayUrl = "https://www.1tamilmv.fi/";
            }

            try
            {
                var response = await RequestWithCookiesAsync(gatewayUrl);
                if (response.Request?.Url != null)
                {
                    var effectiveUri = new Uri(response.Request.Url);
                    if (effectiveUri.Host.IndexOf("1tamilmv.fi", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        var domain = $"{effectiveUri.Scheme}://{effectiveUri.Authority}/";
                        _cachedWorkingDomain = domain;
                        _domainCacheExpiresAt = DateTime.UtcNow.AddMinutes(30);
                        SiteLink = domain;
                        logger.Info($"1TamilMV gateway redirected to active working domain: {domain}");
                        return domain;
                    }
                }

                if (response.IsRedirect && !string.IsNullOrWhiteSpace(response.RedirectingTo))
                {
                    var redirectUri = new Uri(response.RedirectingTo);
                    var domain = $"{redirectUri.Scheme}://{redirectUri.Authority}/";
                    _cachedWorkingDomain = domain;
                    _domainCacheExpiresAt = DateTime.UtcNow.AddMinutes(30);
                    SiteLink = domain;
                    logger.Info($"1TamilMV gateway redirected to active working domain: {domain}");
                    return domain;
                }
            }
            catch (Exception ex)
            {
                logger.Warn($"Failed to resolve gateway domain from {gatewayUrl}: {ex.Message}. Falling back to {configuredLink}");
            }

            return configuredLink.TrimEnd('/') + "/";
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var workingDomain = await ResolveWorkingDomainAsync();
            var maxTopicsVal = ((SingleSelectConfigurationItem)configData.GetDynamic("maxTopics"))?.Value;
            var maxTopics = int.TryParse(maxTopicsVal, out var mt) && mt > 0 ? mt : 15;
            var stripPrefix = ((BoolConfigurationItem)configData.GetDynamic("stripPrefix"))?.Value ?? true;
            var filterSamples = ((BoolConfigurationItem)configData.GetDynamic("filterSamples"))?.Value ?? true;

            var topicUrls = new List<string>();
            var searchTerm = query.GetQueryString();

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                topicUrls = await ExtractHomepageTopicsAsync(workingDomain, maxTopics);
            }
            else
            {
                topicUrls = await SearchTopicsAsync(workingDomain, searchTerm, maxTopics);
            }

            var releases = new List<ReleaseInfo>();
            var parser = new HtmlParser();

            foreach (var topicUrl in topicUrls)
            {
                if (_topicCache.TryGetValue(topicUrl, out var cached) && DateTime.UtcNow < cached.expiresAt)
                {
                    releases.AddRange(cached.releases);
                    continue;
                }

                try
                {
                    var topicResponse = await RequestWithCookiesAndRetryAsync(topicUrl, referer: workingDomain);
                    if (topicResponse.Status != HttpStatusCode.OK)
                    {
                        logger.Warn($"Failed to fetch 1TamilMV topic {topicUrl}: HTTP {topicResponse.Status}");
                        continue;
                    }

                    using var doc = parser.ParseDocument(topicResponse.ContentString);
                    var topicReleases = ExtractTopicReleases(doc, topicUrl, workingDomain, stripPrefix, filterSamples);

                    if (topicReleases.Count > 0)
                    {
                        _topicCache[topicUrl] = (DateTime.UtcNow.AddDays(1), topicReleases);
                        releases.AddRange(topicReleases);
                    }
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

            if (searchTerm.IsNotNullOrWhiteSpace())
            {
                releases = releases.Where(r => query.MatchQueryStringAND(r.Title)).ToList();
            }

            return releases;
        }

        private async Task<List<string>> ExtractHomepageTopicsAsync(string workingDomain, int limit)
        {
            var response = await RequestWithCookiesAndRetryAsync(workingDomain);
            if (response.Status != HttpStatusCode.OK)
            {
                throw new Exception($"Failed to fetch 1TamilMV homepage ({workingDomain}): HTTP {response.Status}");
            }

            var parser = new HtmlParser();
            using var doc = parser.ParseDocument(response.ContentString);

            var topicUrls = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var bangerContainers = doc.QuerySelectorAll(".banger-container");
            var topReleases = new List<string>();
            var recentlyAdded = new List<string>();

            foreach (var container in bangerContainers)
            {
                var headerText = container.QuerySelector(".banger-header")?.TextContent?.ToUpperInvariant() ?? string.Empty;
                var links = ExtractTopicLinksFromElement(container, workingDomain);

                if (headerText.Contains("TOP RELEASES"))
                {
                    topReleases.AddRange(links);
                }
                else if (headerText.Contains("RECENTLY ADDED"))
                {
                    recentlyAdded.AddRange(links);
                }
            }

            foreach (var u in topReleases)
            {
                if (seen.Add(u))
                    topicUrls.Add(u);
            }

            foreach (var u in recentlyAdded)
            {
                if (seen.Add(u))
                    topicUrls.Add(u);
            }

            if (topicUrls.Count == 0)
            {
                var fallbackElements = doc.QuerySelectorAll(".banger-container, .ipsType_break, .ipsContained, .cTopicList, .ipsDataList");
                foreach (var el in fallbackElements)
                {
                    foreach (var u in ExtractTopicLinksFromElement(el, workingDomain))
                    {
                        if (seen.Add(u))
                            topicUrls.Add(u);
                    }
                }

                if (topicUrls.Count == 0)
                {
                    foreach (var a in doc.QuerySelectorAll("a[href]"))
                    {
                        var href = a.GetAttribute("href");
                        var clean = NormalizeTopicUrl(href, workingDomain);
                        if (!string.IsNullOrEmpty(clean) && seen.Add(clean))
                            topicUrls.Add(clean);
                    }
                }
            }

            return topicUrls.Take(limit).ToList();
        }

        private async Task<List<string>> SearchTopicsAsync(string workingDomain, string searchTerm, int limit)
        {
            var searchUrl = $"{workingDomain}index.php?/search/&q={WebUtility.UrlEncode(searchTerm)}&type=forums_topic";
            var response = await RequestWithCookiesAndRetryAsync(searchUrl, referer: workingDomain);
            if (response.Status != HttpStatusCode.OK)
            {
                logger.Warn($"Failed to search 1TamilMV: HTTP {response.Status}");
                return new List<string>();
            }

            var parser = new HtmlParser();
            using var doc = parser.ParseDocument(response.ContentString);

            var topicUrls = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var resultItems = doc.QuerySelectorAll(".ipsStreamItem_title a[href], .ipsStreamItem a[href*='/forums/topic/'], [data-role='searchResults'] a[href*='/forums/topic/']");
            foreach (var a in resultItems)
            {
                var href = a.GetAttribute("href");
                var clean = NormalizeTopicUrl(href, workingDomain);
                if (!string.IsNullOrEmpty(clean) && seen.Add(clean))
                {
                    topicUrls.Add(clean);
                    if (topicUrls.Count >= limit)
                        break;
                }
            }

            return topicUrls;
        }

        private static List<string> ExtractTopicLinksFromElement(IElement element, string workingDomain)
        {
            var list = new List<string>();
            foreach (var a in element.QuerySelectorAll("a[href]"))
            {
                var href = a.GetAttribute("href");
                var clean = NormalizeTopicUrl(href, workingDomain);
                if (!string.IsNullOrEmpty(clean) && !list.Contains(clean))
                {
                    list.Add(clean);
                }
            }
            return list;
        }

        private static string NormalizeTopicUrl(string href, string workingDomain)
        {
            if (string.IsNullOrWhiteSpace(href))
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
                return workingDomain.TrimEnd('/') + url;
            }

            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return new Uri(new Uri(workingDomain), url).ToString();
            }

            return url;
        }

        private List<ReleaseInfo> ExtractTopicReleases(
            IHtmlDocument doc,
            string topicUrl,
            string workingDomain,
            bool stripPrefix,
            bool filterSamples)
        {
            var results = new List<ReleaseInfo>();

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
                    href = workingDomain.TrimEnd('/') + href;
                }
                else if (!href.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    href = new Uri(new Uri(workingDomain), href).ToString();
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

        public static (long bytes, string human) ParseSize(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return (0, "Unknown");

            var m = SizeRegex.Match(title);
            if (!m.Success)
                return (0, "Unknown");

            if (!double.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var sizeVal))
                return (0, "Unknown");

            var unit = m.Groups[2].Value.ToUpperInvariant();
            return unit switch
            {
                "GB" => ((long)(sizeVal * 1024L * 1024L * 1024L), $"{sizeVal:0.0} GB"),
                "MB" => ((long)(sizeVal * 1024L * 1024L), $"{(long)Math.Round(sizeVal)} MB"),
                "KB" => ((long)(sizeVal * 1024L), $"{(long)Math.Round(sizeVal)} KB"),
                _ => (0, "Unknown")
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
    }
}
