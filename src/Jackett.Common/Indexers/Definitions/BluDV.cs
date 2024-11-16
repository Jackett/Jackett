using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using static System.Linq.Enumerable;
using AngleSharp.Html.Parser;
using Jackett.Common.Extensions;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;
using WebRequest = Jackett.Common.Utils.Clients.WebRequest;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class BluDV : IndexerBase
    {
        public override string Id => "bludv";
        public override string Name => "BluDV";
        public override string Description => "BluDV is a Public Torrent Tracker for Movies and TV Shows dubbed in Portuguese";
        public override string SiteLink { get; protected set; } = "https://bludv.xyz/";
        public override string Language => "pt-BR";
        public override string Type => "public";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        public BluDV(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps, ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationData())
        {
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q
                },
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q
                }
            };

            caps.Categories.AddCategoryMapping("filmes", TorznabCatType.Movies);
            caps.Categories.AddCategoryMapping("series", TorznabCatType.TV);

            return caps;
        }

        public override IIndexerRequestGenerator GetRequestGenerator() => new BluDVRequestGenerator(SiteLink);

        public override IParseIndexerResponse GetParser() => new BluDVParser(SiteLink, webclient);

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            await ConfigureIfOK(string.Empty, true, () =>
                throw new Exception("Could not find releases from this URL"));

            return IndexerConfigurationStatus.Completed;
        }
    }

    public class BluDVRequestGenerator : IIndexerRequestGenerator
    {
        private readonly string _siteLink;

        public BluDVRequestGenerator(string siteLink)
        {
            _siteLink = siteLink;
        }

        public IndexerPageableRequestChain GetSearchRequests(TorznabQuery query)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            var searchUrl = $"{_siteLink}?s=";
            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
                searchUrl += WebUtility.UrlEncode(query.SearchTerm.Replace(" ", "+"));

            pageableRequests.Add(new [] {new IndexerRequest(searchUrl)});

            return pageableRequests;
        }
    }

    public class BluDVParser : IParseIndexerResponse
    {
        private readonly string _siteLink;
        private WebClient _webclient;

        public BluDVParser(string siteLink, WebClient webclient)
        {
            _webclient = webclient;
            _siteLink = siteLink;
        }

        private string CleanTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return null;

            // Remove size info in parentheses
            title = Regex.Replace(title, @"\(\d+(?:\.\d+)?\s*(?:GB|MB)\)", "", RegexOptions.IgnoreCase);

            // Remove quality info
            title = Regex.Replace(title, @"\b(?:720p|1080p|2160p|4K)\b", "", RegexOptions.IgnoreCase);

            // Remove source info
            title = Regex.Replace(title, @"\b(?:WEB-DL|BRRip|HDRip|WEBRip|BluRay)\b", "", RegexOptions.IgnoreCase);

            // Remove brackets/parentheses content
            title = Regex.Replace(title, @"\[(?:.*?)\]|\((?:.*?)\)", "", RegexOptions.IgnoreCase);

            // Remove dangling punctuation and separators
            title = Regex.Replace(title, @"[\\/,|~_-]+\s*|\s*[\\/,|~_-]+", " ", RegexOptions.IgnoreCase);

            // Clean up multiple spaces
            title = Regex.Replace(title, @"\s+", " ");

            // Remove dots between words but keep dots in version numbers
            title = Regex.Replace(title, @"(?<!\d)\.(?!\d)", " ", RegexOptions.IgnoreCase);

            // Remove any remaining punctuation at start/end
            title = title.Trim(' ', '.', ',', '-', '_', '~', '/', '\\', '|');

            return title;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var releases = new List<ReleaseInfo>();

            var parser = new HtmlParser();
            var dom = parser.ParseDocument(indexerResponse.Content);
            var rows = dom.QuerySelectorAll("div.post");

            foreach (var row in rows)
            {
                // Get the details page to extract the magnet link
                var detailsParser = new HtmlParser();
                var detailUrl = new Uri(row.QuerySelector("a.more-link")?.GetAttribute("href"));
                var detailTitle = row.QuerySelector("div.title > a")?.TextContent.Trim();
                var releaseCommonInfo = new ReleaseInfo{
                    Genres = ExtractGenres(row),
                    Category = ExtractCategory(row),
                    PublishDate = ExtractReleaseDate(row),
                    Subs = ExtractSubtitles(row),
                    Size = ExtractSize(row),
                    Languages = ExtractLanguages(row),
                    Details = detailUrl,
                    Guid = detailUrl
                };
                var detailsPage = _webclient.GetResultAsync(new WebRequest(detailUrl.ToString())).Result;
                var detailsDom = detailsParser.ParseDocument(detailsPage.ContentString);
                foreach (var downloadButton in detailsDom.QuerySelectorAll("a.customButton[href^=\"magnet:\"]"))
                {
                    var release = releaseCommonInfo.Clone() as ReleaseInfo;
                    var title = ExtractTitle(downloadButton, detailTitle);
                    release.Title = title;
                    release.Languages =  ExtractLanguages(row);

                    var magnetLink = downloadButton.GetAttribute("href");
                    var magnet = string.IsNullOrEmpty(magnetLink) ? null : new Uri(magnetLink);
                    release.Link = release.Guid = release.MagnetUri = magnet;

                    release.DownloadVolumeFactor = 0; // Free
                    release.UploadVolumeFactor = 1;

                    if (release.Title.IsNotNullOrWhiteSpace())
                        releases.Add(release);
                }
            }

            return releases;
        }

        private static List<string> ExtractLanguages(IElement row)
        {
            var languages = new List<string>();
            ExtractFromRow(row, "span:contains(\"Áudio:\")", audioText =>
            {
                ExtractPattern(audioText, @"Áudio:\s*(.+)", audio =>
                {
                    languages = audio.Split('|').Select(token => token.Trim()).ToList();
                });
            });
            return languages;
        }

        private static long? ExtractSize(IElement row)
        {
            long? result = null;
            ExtractFromRow(row, "span:contains(\"Tamanho:\")", sizeText =>
            {
                ExtractPattern(sizeText, @"Tamanho:\s*(.+)", size =>
                {
                    result = ParseUtil.GetBytes(size);
                });
            });
            return result;
        }

        private static List<string> ExtractSubtitles(IElement row)
        {
            var subtitles = new List<string>();
            ExtractFromRow(row, "span:contains(\"Legenda:\")", subtitleText =>
            {
                ExtractPattern(subtitleText, @"Legenda:\s*(.+)", subtitle =>
                {
                    subtitles.Add(subtitle);
                });
            });
            return subtitles;
        }

        private static List<string> ExtractGenres(IElement row)
        {
            var genres = new List<string>();
            ExtractFromRow(
                row,
                "span:contains(\"Gênero:\")",
                genreText =>
                {
                    ExtractPattern(genreText, @"Gênero:\s*(.+)", genre =>
                    {
                        genres = genre.Split('|').Select(token => token.Trim()).ToList();
                    });
                });
            return genres;
        }

        private static DateTime ExtractReleaseDate(IElement row)
        {
            var result = DateTime.MinValue;
            ExtractFromRow(row, "span:contains(\"Lançamento:\")", releaseDateText =>
            {
                ExtractPattern(releaseDateText, @"Lançamento:\s*(.+)", releaseDate =>
                {
                    DateTime.TryParseExact(
                        releaseDate, "yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None,
                        out result);
                });
            });
            return result;
        }

        private static void ExtractPattern(string text, string pattern, Action<string> extraction)
        {
            var match = Regex.Match(text, pattern);
            if (match.Success)
            {
                extraction(match.Groups[1].Value.Trim());
            }
        }

        private static List<int> ExtractCategory(IElement row)
        {
            var releaseCategory = new List<int>();
            ExtractFromRow(row, "div.title > a", categoryText =>
            {
                var hasSeasonInfo = categoryText.IndexOf("temporada", StringComparison.OrdinalIgnoreCase) >= 0;
                releaseCategory.Add(hasSeasonInfo ? TorznabCatType.TV.ID : TorznabCatType.Movies.ID);
            });
            return releaseCategory;
        }

        private static void ExtractFromRow(IElement row, string selector, Action<string> extraction)
        {
            var genreElement = row.QuerySelector(selector);
            if (genreElement != null)
            {
                extraction(genreElement.TextContent);
            }
        }

        private string ExtractTitle(IElement downloadButton, string title)
        {
            var description = GetSpanTagOrNull(downloadButton);
            if (description != null)
            {
                var descriptionText = description.TextContent;
                ExtractPattern(descriptionText, @"(.+?)\s*\d{3,4}p", resolution =>
                {
                    title = "[BluDV] " + CleanTitle(title) + resolution;
                });
            }
            return title;
        }

        private static INode GetSpanTagOrNull(IElement downloadButton)
        {
            var description = downloadButton.PreviousSibling;
            while (description != null && NotSpanTag(description))
            {
                description = description.PreviousSibling;
            }

            return description;
        }

        private static bool NotSpanTag(INode description) => (description.NodeType != NodeType.Element || ((Element)description).TagName != "SPAN");
    }
}
