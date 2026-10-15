using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Jackett.Common.Indexers.Definitions.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;
using WebRequest = Jackett.Common.Utils.Clients.WebRequest;

namespace Jackett.Common.Indexers.Definitions
{
    public class PelisPanda : PublicSpanishIndexerBase
    {
        public override string Id => "pelispanda";
        public override string Name => "PelisPanda";
        public override string SiteLink { get; protected set; } = "https://pelispanda.org/";

        public PelisPanda(IIndexerConfigurationService configService, WebClient wc, Logger l,
                          IProtectionService ps, ICacheService cs)
            : base(configService, wc, l, ps, cs)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator() =>
            new PelisPandaRequestGenerator(SiteLink);

        public override IParseIndexerResponse GetParser() =>
            new PelisPandaParser(webclient, logger, SiteLink);
    }

    public class PelisPandaRequestGenerator : IIndexerRequestGenerator
    {
        private const int PostsPerPage = 500;
        private const int Page = 1;

        private readonly string _siteLink;

        public PelisPandaRequestGenerator(string siteLink)
        {
            _siteLink = siteLink;
        }

        public IndexerPageableRequestChain GetSearchRequests(TorznabQuery query)
        {
            var chain = new IndexerPageableRequestChain();

            var term = query.SearchTerm ?? string.Empty;
            if (string.IsNullOrWhiteSpace(term))
                return chain;

            if (query.Season is { } season)
                term = $"{term} {season}".Trim();
            if (!string.IsNullOrWhiteSpace(query.Episode))
                term = $"{term} {query.Episode}".Trim();

            var url = $"{_siteLink}wp-json/wpreact/v1/search" +
                      $"?query={WebUtility.UrlEncode(term)}" +
                      $"&posts_per_page={PostsPerPage}" +
                      $"&page={Page}";
            chain.Add(new[] { new IndexerRequest(url) });
            return chain;
        }
    }

    public class PelisPandaParser : IParseIndexerResponse
    {
        private const int MaxConcurrentRequests = 2;

        private const long EstimateBytes720p = 1L * 1024 * 1024 * 1024;
        private const long EstimateBytes1080p = (long)(2.5 * 1024 * 1024 * 1024);
        private const long EstimateBytes2160p = 5L * 1024 * 1024 * 1024;
        private const long EstimateBytesDefault = 512L * 1024 * 1024;

        private readonly WebClient _webclient;
        private readonly Logger _logger;
        private readonly string _siteLink;

        public PelisPandaParser(WebClient webclient, Logger logger, string siteLink)
        {
            _webclient = webclient;
            _logger = logger;
            _siteLink = siteLink;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            if (indexerResponse == null || string.IsNullOrWhiteSpace(indexerResponse.Content))
            {
                _logger?.Warn("PelisPanda: search response was empty or missing; returning no releases");
                return new List<ReleaseInfo>();
            }

            JObject json;
            try
            {
                json = JObject.Parse(indexerResponse.Content);
            }
            catch (Exception ex)
            {
                _logger?.Warn(ex, "PelisPanda: failed to parse search response as JSON; returning no releases");
                return new List<ReleaseInfo>();
            }
            var results = json["results"] as JArray ?? new JArray();

            var items = new List<(int Index, JObject Item, string DetailUrl, string Type)>();
            foreach (var raw in results.OfType<JObject>())
            {
                var type = (string)raw["type"];
                var slug = (string)raw["slug"];
                var detailUrl = BuildDetailUrl(type, slug);
                if (detailUrl == null)
                {
                    _logger?.Warn($"PelisPanda: skipping unknown type '{type}' for slug '{slug}'");
                    continue;
                }
                items.Add((items.Count, raw, detailUrl, type));
            }

            var details = FetchDetailsAsync(items.Select(i => i.DetailUrl).Distinct().ToList())
                .GetAwaiter().GetResult();

            var seenGuids = new HashSet<string>();
            var releases = new List<ReleaseInfo>();
            foreach (var entry in items)
            {
                if (!details.TryGetValue(entry.DetailUrl, out var detail) || detail == null)
                    continue;
                BuildReleasesForItem(entry.Item, entry.Type, detail, seenGuids, releases);
            }
            return releases;
        }

        private string BuildDetailUrl(string type, string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return null;
            return type switch
            {
                "pelicula" => $"{_siteLink}wp-json/wpreact/v1/movie/{slug}",
                "anime" => $"{_siteLink}wp-json/wpreact/v1/anime/{slug}",
                "serie" => $"{_siteLink}wp-json/wpreact/v1/serie/{slug}/related",
                _ => null
            };
        }

        private async Task<Dictionary<string, JObject>> FetchDetailsAsync(IList<string> urls)
        {
            var result = new Dictionary<string, JObject>();
            if (urls.Count == 0)
                return result;

            using var semaphore = new SemaphoreSlim(MaxConcurrentRequests);
            var tasks = urls.Select(async url =>
            {
                await semaphore.WaitAsync().ConfigureAwait(false);
                try
                {
                    var resp = await _webclient.GetResultAsync(new WebRequest(url)).ConfigureAwait(false);
                    if (resp.Status != HttpStatusCode.OK)
                    {
                        _logger?.Warn($"PelisPanda: detail {url} returned HTTP {(int)resp.Status}; skipping");
                        return (url, (JObject)null);
                    }
                    try
                    {
                        return (url, JObject.Parse(resp.ContentString ?? string.Empty));
                    }
                    catch (Exception ex)
                    {
                        _logger?.Warn($"PelisPanda: detail {url} JSON parse failed: {ex.Message}; skipping");
                        return (url, (JObject)null);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.Warn($"PelisPanda: detail {url} fetch failed: {ex.Message}; skipping");
                    return (url, (JObject)null);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            foreach (var (url, doc) in await Task.WhenAll(tasks).ConfigureAwait(false))
            {
                result[url] = doc;
            }
            return result;
        }

        private void BuildReleasesForItem(JObject item, string type,
            JObject detail, HashSet<string> seenGuids, List<ReleaseInfo> releases)
        {
            var downloads = detail["downloads"] as JArray;
            if (downloads == null || downloads.Count == 0)
                return;

            var slug = (string)item["slug"] ?? string.Empty;
            var titleBase = FirstNonEmpty(
                (string)item["original_title"],
                (string)item["title"],
                slug);

            var year = item["year"]?.Type == JTokenType.Integer
                ? (int?)item["year"]
                : null;

            var category = MapCategory(type);
            var detailsUri = new Uri($"{_siteLink}{type}/{slug}");

            foreach (var dl in downloads.OfType<JObject>())
            {
                var rawLink = (string)dl["download_link"];
                if (string.IsNullOrWhiteSpace(rawLink))
                    continue;

                var quality = (string)dl["quality"];
                var language = (string)dl["language"];
                var sizeStr = (string)dl["size"];
                var subsFlag = (int?)dl["subs"] ?? 0;
                var dateStr = (string)dl["date"];
                var season = (int?)dl["season"];
                var episode = (int?)dl["episode"];

                Uri magnetUri = null;
                Uri linkUri = null;
                if (rawLink.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase))
                {
                    if (!Uri.TryCreate(rawLink, UriKind.Absolute, out magnetUri))
                    {
                        _logger?.Warn($"PelisPanda: malformed magnet link in '{slug}'; skipping row");
                        continue;
                    }
                }
                else if (!Uri.TryCreate(rawLink, UriKind.Absolute, out linkUri))
                {
                    _logger?.Warn($"PelisPanda: malformed download link in '{slug}'; skipping row");
                    continue;
                }
                var guidUri = magnetUri ?? linkUri;
                if (!seenGuids.Add(guidUri.AbsoluteUri))
                    continue;

                var release = new ReleaseInfo
                {
                    Title = FormatTitle(titleBase, year, quality, language, season, episode),
                    Category = new List<int> { category },
                    Size = ResolveSize(sizeStr, quality),
                    Languages = string.IsNullOrWhiteSpace(language) ? null : new[] { language },
                    Subs = subsFlag == 1 ? new[] { "Subtitulado" } : null,
                    MagnetUri = magnetUri,
                    Link = linkUri,
                    Guid = guidUri,
                    Details = detailsUri,
                    PublishDate = ResolvePublishDate(dateStr, year),
                    Seeders = 1,
                    DownloadVolumeFactor = 0,
                    UploadVolumeFactor = 1
                };
                releases.Add(release);
            }
        }

        internal static int MapCategory(string type) => type switch
        {
            "pelicula" => TorznabCatType.Movies.ID,
            "anime" => TorznabCatType.TVAnime.ID,
            "serie" => TorznabCatType.TV.ID,
            _ => 0
        };

        internal static string FirstNonEmpty(params string[] values) =>
            values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

        internal static string FormatTitle(string titleBase, int? year, string quality, string language,
            int? season, int? episode)
        {
            var parts = new List<string> { titleBase };
            if (season.HasValue && episode.HasValue)
                parts.Add($"S{season.Value:00}E{episode.Value:00}");
            if (year is int y && y > 0)
                parts.Add($"({y})");
            if (!string.IsNullOrWhiteSpace(quality))
                parts.Add(quality);
            if (!string.IsNullOrWhiteSpace(language))
                parts.Add(language);
            return System.Text.RegularExpressions.Regex.Replace(
                string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p))), @"\s+", " ").Trim();
        }

        internal static long ResolveSize(string sizeStr, string quality)
        {
            if (!string.IsNullOrWhiteSpace(sizeStr))
                return ParseUtil.GetBytes(sizeStr);
            return EstimateSizeFromQuality(quality);
        }

        internal static long EstimateSizeFromQuality(string quality) => quality switch
        {
            "720p" => EstimateBytes720p,
            "1080p" => EstimateBytes1080p,
            "2160p" => EstimateBytes2160p,
            _ => EstimateBytesDefault
        };

        internal static DateTime ResolvePublishDate(string dateStr, int? year)
        {
            if (!string.IsNullOrWhiteSpace(dateStr) &&
                DateTime.TryParseExact(dateStr, "yyyyMMdd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var parsed))
                return parsed;

            if (year is int y && y >= 1 && y <= 9999)
                return new DateTime(y, 1, 1);

            return DateTime.Today;
        }
    }
}
