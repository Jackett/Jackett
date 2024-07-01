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
        public override string Description => "Películas Full UHD/HD en Latino Dual.";
        public override string SiteLink { get; protected set; } = "https://www.cinecalidad.vg/";
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

            var templateUrl = SiteLink;
            templateUrl += "{0}?s="; // placeholder for page

            var maxPages = MaxLatestPageLimit; // we scrape only 3 pages for recent torrents

            var recent = !string.IsNullOrWhiteSpace(query.GetQueryString());
            if (recent)
            {
                templateUrl += WebUtilityHelpers.UrlEncode(query.GetQueryString(), Encoding.UTF8);
                maxPages = MaxSearchPageLimit;
            }

            var lastPublishDate = DateTime.Now;
            for (var page = 1; page <= maxPages; page++)
            {
                var pageParam = page > 1 ? $"page/{page}/" : "";
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

                if (pageReleases.Count < 1 && recent)
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

                var downloadLink = link.Query.Contains("type=4k")
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
                var magnetUrl = document.QuerySelector("a[href^=magnet]").GetAttribute("href");

                return await base.Download(new Uri(magnetUrl));
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

                var rows = dom.QuerySelectorAll("article:has(a.absolute):has(img.rounded)");

                foreach (var row in rows)
                {
                    if (row.QuerySelector("div.selt") != null)
                    {
                        // we only support movies
                        continue;
                    }

                    var qLink = row.QuerySelector("a.absolute");
                    var qImg = row.QuerySelector("img.rounded");
                    if (qLink == null || qImg == null)
                    {
                        // skip results without image
                        continue;
                    }

                    var title = qLink.TextContent.Trim();
                    if (!CheckTitleMatchWords(query.GetQueryString(), title))
                    {
                        // skip if it doesn't contain all words
                        continue;
                    }

                    var poster = new Uri(GetAbsoluteUrl(qImg.GetAttribute("data-src") ?? qImg.GetAttribute("src")));
                    var link = new Uri(qLink.GetAttribute("href"));

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
                        UploadVolumeFactor = 1
                    });

                    if (row.QuerySelector("a[aria-label=\"4K\"]") != null)
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
                            UploadVolumeFactor = 1
                        });
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

        private string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }

}
