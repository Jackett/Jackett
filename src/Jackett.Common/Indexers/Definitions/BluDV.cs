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
                var releaseCommonInfo = new ReleaseInfo {
                    Genres = row.ExtractGenres(),
                    Category = row.ExtractCategory(),
                    PublishDate = row.ExtractReleaseDate(),
                    Subs = row.ExtractSubtitles(),
                    Size = row.ExtractSize(),
                    Languages = row.ExtractLanguages(),
                    Details = detailUrl,
                    Guid = detailUrl
                };
                var detailsPage = _webclient.GetResultAsync(new WebRequest(detailUrl.ToString())).Result;
                var detailsDom = detailsParser.ParseDocument(detailsPage.ContentString);
                foreach (var downloadButton in detailsDom.QuerySelectorAll("a.customButton[href^=\"magnet:\"]"))
                {
                    var title = downloadButton.ExtractTitleOrDefault(detailTitle);
                    var magnet = downloadButton.ExtractMagnet();
                    var release = releaseCommonInfo.Clone() as ReleaseInfo;
                    release.Title = title;
                    release.Languages =  row.ExtractLanguages();
                    release.Link = release.Guid = release.MagnetUri = magnet;
                    release.DownloadVolumeFactor = 0; // Free
                    release.UploadVolumeFactor = 1;

                    if (release.Title.IsNotNullOrWhiteSpace())
                        releases.Add(release);
                }
            }

            return releases;
        }
    }

    public static class RowParsingExtensions
    {
        private static string CleanTitle(string title)
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
        private static bool NotSpanTag(INode description) => (description.NodeType != NodeType.Element || ((Element)description).TagName != "SPAN");


        public static Uri ExtractMagnet(this IElement downloadButton)
        {
            var magnetLink = downloadButton.GetAttribute("href");
            var magnet = string.IsNullOrEmpty(magnetLink) ? null : new Uri(magnetLink);
            return magnet;
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


        public static string ExtractTitleOrDefault(this IElement downloadButton, string title)
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

        public static List<string> ExtractGenres(this IElement row)
        {
            var genres = new List<string>();
            row.ExtractFromRow("span:contains(\"Gênero:\")", genreText =>
            {
                ExtractPattern(genreText, @"Gênero:\s*(.+)", genre =>
                {
                    genres = genre.Split('|').Select(token => token.Trim()).ToList();
                });
            });
            return genres;
        }

        public static List<int> ExtractCategory(this IElement row)
        {
            var releaseCategory = new List<int>();
            row.ExtractFromRow("div.title > a", categoryText =>
            {
                var hasSeasonInfo = categoryText.IndexOf("temporada", StringComparison.OrdinalIgnoreCase) >= 0;
                releaseCategory.Add(hasSeasonInfo ? TorznabCatType.TV.ID : TorznabCatType.Movies.ID);
            });
            return releaseCategory;
        }

        public static DateTime ExtractReleaseDate(this IElement row)
        {
            var result = DateTime.MinValue;
            row.ExtractFromRow("span:contains(\"Lançamento:\")", releaseDateText =>
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

        public static List<string> ExtractSubtitles(this IElement row)
        {
            var subtitles = new List<string>();
            row.ExtractFromRow("span:contains(\"Legenda:\")", subtitleText =>
            {
                ExtractPattern(subtitleText, @"Legenda:\s*(.+)", subtitle =>
                {
                    subtitles.Add(subtitle);
                });
            });
            return subtitles;
        }

        public static long? ExtractSize(this IElement row)
        {
            long? result = null;
            row.ExtractFromRow("span:contains(\"Tamanho:\")", sizeText =>
            {
                ExtractPattern(sizeText, @"Tamanho:\s*(.+)", size =>
                {
                    result = ParseUtil.GetBytes(size);
                });
            });
            return result;
        }

        public static List<string> ExtractLanguages(this IElement row)
        {
            var languages = new List<string>();
            row.ExtractFromRow("span:contains(\"Áudio:\")", audioText =>
            {
                ExtractPattern(audioText, @"Áudio:\s*(.+)", audio =>
                {
                    languages = audio.Split('|').Select(token => token.Trim()).ToList();
                });
            });
            return languages;
        }

        public static void ExtractFromRow(this IElement row, string selector, Action<string> extraction)
        {
            var element = row.QuerySelector(selector);
            if (element != null)
            {
                extraction(element.TextContent);
            }
        }

        private static void ExtractPattern(string text, string pattern, Action<string> extraction)
        {
            var match = Regex.Match(text, pattern);
            if (match.Success)
            {
                extraction(match.Groups[1].Value.Trim());
            }
        }
    }
}
