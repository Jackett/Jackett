using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Jackett.Common.Extensions;
using Jackett.Common.Helpers;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class Cinecalidad : IndexerBase
    {
        public override string Id => "cinecalidad";
        public override string Name => "Cinecalidad";
        public override string Description => "Cinecalidad is a Public site for Películas Full UHD/HD en Latino Dual.";
        public override string SiteLink { get; protected set; } = "https://www.cinecalidad.ec/";
        public override string[] LegacySiteLinks => new[]
        {
            "https://wv.cinecalidad.foo/",
            "https://vwv.cinecalidad.foo/",
            "https://wzw.cinecalidad.foo/",
            "https://v2.cinecalidad.foo/",
            "https://www.cinecalidad.so/",
            "https://wvw.cinecalidad.so/",
            "https://vww.cinecalidad.so/",
            "https://wwv.cinecalidad.so/",
            "https://vvv.cinecalidad.so/",
            "https://ww.cinecalidad.so/",
            "https://w.cinecalidad.so/",
            "https://wv.cinecalidad.so/",
            "https://vvvv.cinecalidad.so/",
            "https://wvvv.cinecalidad.so/",
            "https://cinecalidad.fi/",
            "https://www.cinecalidad.vg/",
        };
        public override string Language => "es-419";
        public override string Type => "public";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private const int MaxLatestPageLimit = 3; // 12 items per page * 3 pages = 36
        private const int MaxSearchPageLimit = 6;

        public Cinecalidad(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
                           ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationData())
        {
            configData.AddDynamic("flaresolverr", new DisplayInfoConfigurationItem("FlareSolverr", "This site may use Cloudflare DDoS Protection, therefore Jackett requires <a href=\"https://github.com/Jackett/Jackett#configuring-flaresolverr\" target=\"_blank\">FlareSolverr</a> to access it."));
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q
                }
            };

            caps.Categories.AddCategoryMapping(1, TorznabCatType.MoviesHD);
            caps.Categories.AddCategoryMapping(2, TorznabCatType.MoviesUHD);

            return caps;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var releases = await PerformQuery(new TorznabQuery());

            await ConfigureIfOK(string.Empty, releases.Any(), () =>
                                    throw new Exception("Could not find release from this URL."));

            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var templateUrl = $"{SiteLink}{{0}}?s="; // placeholder for page

            var maxPages = MaxLatestPageLimit; // we scrape only 3 pages for recent torrents

            var isSearch = !string.IsNullOrWhiteSpace(query.GetQueryString());
            if (isSearch)
            {
                templateUrl += WebUtilityHelpers.UrlEncode(query.GetQueryString(), Encoding.UTF8);
                maxPages = MaxSearchPageLimit;
            }

            var lastPublishDate = DateTime.Now;
            for (var page = 1; page <= maxPages; page++)
            {
                var pageParam = page > 1 ? $"page/{page}/" : string.Empty;
                var searchUrl = string.Format(templateUrl, pageParam);
                var response = await RequestWithCookiesAndRetryAsync(searchUrl);
                var pageReleases = ParseReleases(response, query);

                // publish date is not available in the torrent list, but we add a relative date so we can sort
                foreach (var release in pageReleases)
                {
                    release.PublishDate = lastPublishDate;
                    lastPublishDate = lastPublishDate.AddMinutes(-1);
                }
                releases.AddRange(pageReleases);

                if (pageReleases.Count < 1 && isSearch)
                {
                    // this is the last page
                    break;
                }
            }

            return releases;
        }

        public override async Task<byte[]> Download(Uri link)
        {
            var parser = new HtmlParser();

            var results = await RequestWithCookiesAsync(link.ToString());

            try
            {
                using var dom = await parser.ParseDocumentAsync(results.ContentString);

                var is4K = link.Query.Contains("type=4k");
                var hrefDownloadLinks = dom.QuerySelectorAll("#sbss > a:not([href*='acortalink'])");

                if (hrefDownloadLinks.Length > 0)
                {
                    var selected = hrefDownloadLinks.FirstOrDefault(a => a.TextContent.IndexOf("uTorrent", StringComparison.OrdinalIgnoreCase) >= 0);
                    selected ??= is4K
                        ? hrefDownloadLinks.FirstOrDefault(a => a.TextContent.IndexOf("4k", StringComparison.OrdinalIgnoreCase) >= 0 || a.TextContent.IndexOf("2160", StringComparison.OrdinalIgnoreCase) >= 0)
                        : hrefDownloadLinks.FirstOrDefault(a => a.TextContent.IndexOf("4k", StringComparison.OrdinalIgnoreCase) < 0 && a.TextContent.IndexOf("2160", StringComparison.OrdinalIgnoreCase) < 0);

                    selected ??= hrefDownloadLinks.FirstOrDefault();

                    var href = selected?.GetAttribute("href");
                    if (!href.IsNullOrWhiteSpace())
                    {
                        href = GetAbsoluteUrl(href);
                        var dlResult = await RequestWithCookiesAsync(href);
                        dlResult = await FollowIfRedirect(dlResult, referrer: link.ToString());

                        if (!dlResult.RedirectingTo.IsNullOrWhiteSpace() && new Uri(dlResult.RedirectingTo).Scheme == "magnet")
                        {
                            return await base.Download(new Uri(dlResult.RedirectingTo));
                        }

                        using var dlDom = parser.ParseDocument(dlResult.ContentString);
                        var magnetUrlFromDownloadPage = dlDom.QuerySelector("a.link[data-href^='magnet:']")?.GetAttribute("data-href");
                        if (magnetUrlFromDownloadPage.IsNullOrWhiteSpace())
                        {
                            throw new Exception($"Invalid magnet link for {link}");
                        }

                        return await base.Download(new Uri(magnetUrlFromDownloadPage));
                    }
                }

                var downloadLink = is4K
                    ? dom.QuerySelector("ul.links a:contains('Bittorrent 4K')")
                    : dom.QuerySelector("ul.links a:contains('Torrent')");

                var protectedLink = downloadLink?.GetAttribute("data-url");

                if (protectedLink.IsNullOrWhiteSpace())
                {
                    throw new Exception($"Invalid download link for {link}");
                }

                protectedLink = Base64Decode(protectedLink);
                // turn
                // link=https://cinecalidad.dev/pelicula/la-chica-salvaje/
                // and
                // protectedlink=https://cinecalidad.dev/links/MS8xMDA5NTIvMQ==
                // into
                // https://cinecalidad.dev/pelicula/la-chica-salvaje/?link=MS8xMDA5NTIvMQ==
                var protectedLinkSplit = protectedLink.Split('/');
                var key = protectedLinkSplit.Last();
                protectedLink = link.AddQueryParameter("link", key).ToString();
                protectedLink = GetAbsoluteUrl(protectedLink);

                results = await RequestWithCookiesAsync(protectedLink);

                using var document = parser.ParseDocument(results.ContentString);
                var magnetUrlFromProtectedPage = document.QuerySelector("a[href^=magnet]").GetAttribute("href");

                return await base.Download(new Uri(magnetUrlFromProtectedPage));
            }
            catch (Exception ex)
            {
                OnParseError(results.ContentString, ex);
            }

            return null;
        }

        private List<ReleaseInfo> ParseReleases(WebResult response, TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            try
            {
                var parser = new HtmlParser();
                using var dom = parser.ParseDocument(response.ContentString);

                // Get all movie items - updated selector to match the actual structure
                var movieItems = dom.QuerySelectorAll("article, div[class*='post-']");

                foreach (var item in movieItems)
                {
                    var seltText = item.QuerySelector("div.selt")?.TextContent?.Trim();
                    if (!string.IsNullOrEmpty(seltText) && seltText.IndexOf("Series", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        continue;
                    }

                    var titleElement = item.QuerySelector(".in_title") ?? item.QuerySelector("h3") ?? item.QuerySelector("h2") ?? item.QuerySelector(".entry-title");
                    var linkElement = item.QuerySelector("a[href*='/ver-pelicula/']") ??
                                     item.QuerySelector("a[href*='/pelicula/']") ??
                                     item.QuerySelector("h3 a[href]") ??
                                     item.QuerySelector("h2 a[href]") ??
                                     item.QuerySelector(".entry-title a[href]");

                    var imgElement = item.QuerySelector("img.lazy") ??
                                    item.QuerySelector("img[data-src]") ??
                                    item.QuerySelector("img[src]");

                    // Try to find year in various places
                    var yearElement = item.QuerySelector("span.year") ??
                                     item.QuerySelector(".year") ??
                                     item.QuerySelector(".date") ??
                                     item.QuerySelector("time") ??
                                     item.QuerySelector(".entry-meta");

                    if (titleElement == null || linkElement == null)
                    {
                        continue;
                    }

                    var title = titleElement.TextContent.Trim();
                    var year = yearElement?.TextContent.Trim();

                    // Extract year if it's in format like (2023) or [2023]
                    if (!string.IsNullOrEmpty(year))
                    {
                        var yearMatch = Regex.Match(year, @"\(?(\d{4})\)?");
                        if (yearMatch.Success)
                        {
                            year = yearMatch.Groups[1].Value;
                            title = $"{title.Trim()} ({year})";
                        }
                    }

                    if (!CheckTitleMatchWords(query.GetQueryString(), title))
                    {
                        continue;
                    }

                    var posterUrl = imgElement?.GetAttribute("data-src") ??
                                   imgElement?.GetAttribute("src");

                    var detailsUrl = linkElement.GetAttribute("href");

                    if (string.IsNullOrEmpty(detailsUrl) ||
                        detailsUrl.Contains("wp-json") ||
                        detailsUrl.Contains("wp-admin"))
                    {
                        continue;
                    }

                    // Skip if it's not a movie URL
                    if (!detailsUrl.Contains("/ver-pelicula/") && !detailsUrl.Contains("/pelicula/"))
                    {
                        continue;
                    }

                    try
                    {
                        var poster = !string.IsNullOrEmpty(posterUrl)
                            ? new Uri(GetAbsoluteUrl(posterUrl))
                            : null;
                        var link = new Uri(detailsUrl);

                        // Check for 4K version
                        var is4K = item.TextContent.IndexOf("4K", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   item.TextContent.IndexOf("2160p", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   detailsUrl.IndexOf("4k", StringComparison.OrdinalIgnoreCase) >= 0;

                        // Add HD version (1080p)
                        releases.Add(new ReleaseInfo
                        {
                            Guid = link,
                            Details = link,
                            Link = link,
                            Title = $"{title} MULTi/LATiN SPANiSH 1080p BDRip x264",
                            Category = new List<int> { TorznabCatType.MoviesHD.ID },
                            Poster = poster,
                            Size = 2147483648, // 2 GB
                            Files = 1,
                            Seeders = 1,
                            Peers = 2,
                            DownloadVolumeFactor = 0,
                            UploadVolumeFactor = 1,
                            PublishDate = DateTime.Today
                        });

                        // Add 4K version if available
                        if (is4K)
                        {
                            var link4K = link.AddQueryParameter("type", "4k");
                            releases.Add(new ReleaseInfo
                            {
                                Guid = link4K,
                                Details = link,
                                Link = link4K,
                                Title = $"{title} MULTi/LATiN SPANiSH 2160p BDRip x265",
                                Category = new List<int> { TorznabCatType.MoviesUHD.ID },
                                Poster = poster,
                                Size = 10737418240, // 10 GB
                                Files = 1,
                                Seeders = 1,
                                Peers = 2,
                                DownloadVolumeFactor = 0,
                                UploadVolumeFactor = 1,
                                PublishDate = DateTime.Today
                            });
                        }
                    }
                    catch (Exception)
                    {
                        // Skip this item if there's an error with URL parsing
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.ContentString, ex);
            }

            return releases;
        }

        // TODO: merge this method with query.MatchQueryStringAND
        private static bool CheckTitleMatchWords(string queryStr, string title)
        {
            // this code split the words, remove words with 2 letters or less, remove accents and lowercase
            var queryMatches = Regex.Matches(queryStr, @"\b[\w']*\b");
            var queryWords = from m in queryMatches.Cast<Match>()
                             where !string.IsNullOrEmpty(m.Value) && m.Value.Length > 2
                             select Encoding.UTF8.GetString(Encoding.GetEncoding("ISO-8859-8").GetBytes(m.Value.ToLower()));

            var titleMatches = Regex.Matches(title, @"\b[\w']*\b");
            var titleWords = from m in titleMatches.Cast<Match>()
                             where !string.IsNullOrEmpty(m.Value) && m.Value.Length > 2
                             select Encoding.UTF8.GetString(Encoding.GetEncoding("ISO-8859-8").GetBytes(m.Value.ToLower()));
            titleWords = titleWords.ToArray();

            return queryWords.All(word => titleWords.Contains(word));
        }

        private string GetAbsoluteUrl(string url)
        {
            url = url.Trim();

            if (!url.StartsWith("http"))
            {
                return SiteLink + url.TrimStart('/');
            }

            return url;
        }

        private static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}
