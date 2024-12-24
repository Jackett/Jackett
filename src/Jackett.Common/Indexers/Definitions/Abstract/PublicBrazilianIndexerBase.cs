using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using AngleSharp.Dom;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;
using NLog;
using static System.Linq.Enumerable;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers.Definitions.Abstract
{
    public abstract class PublicBrazilianIndexerBase : IndexerBase
    {
        public PublicBrazilianIndexerBase(IIndexerConfigurationService configService, WebClient wc, Logger l,
                                          IProtectionService ps, ICacheService cs) : base(
            configService: configService, client: wc, logger: l, p: ps, cacheService: cs,
            configData: new ConfigurationData())
        {
            webclient.requestDelay = .5;
        }

        public override string Description =>
            $"{Name} is a Public Torrent Tracker for Movies and TV Shows dubbed in Brazilian Portuguese";

        public override string Language => "pt-BR";
        public override string Type => "public";
        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                MovieSearchParams = new List<MovieSearchParam> { MovieSearchParam.Q },
                TvSearchParams = new List<TvSearchParam> { TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep }
            };
            caps.Categories.AddCategoryMapping("filmes", TorznabCatType.Movies);
            caps.Categories.AddCategoryMapping("series", TorznabCatType.TV);
            return caps;
        }

        public override IIndexerRequestGenerator GetRequestGenerator() => new SimpleRequestGenerator(SiteLink);

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            await ConfigureIfOK(string.Empty, true, () => throw new Exception("Could not find releases from this URL"));
            return IndexerConfigurationStatus.Completed;
        }
        public class FileInfo
        {
            public string[] Genres { get; set; }
            public string[] Audio { get; set; }
            public string Subtitle { get; set; }
            public string Format { get; set; }
            public string Quality { get; set; }
            public string Size { get; set; }
            public string ReleaseYear { get; set; }
            public string Duration { get; set; }
            public string AudioQuality { get; set; }
            public string VideoQuality { get; set; }
            public string TitleTranslated { get; set; }
            public string TitleOriginal { get; set; }
            public string IMDb { get; set; }

            public static FileInfo FromDictionary(Dictionary<string, string> dict)
            {
                return new FileInfo
                {
                    Genres = dict.TryGetValue("Gênero", out var genres) ? genres?.Split(',').Select(g => g.Trim()).ToArray() : null,
                    Audio = dict.TryGetValue("Áudio", out var audio) ? audio?.Split(',').Select(a => a.Trim()).ToArray() : (
                        dict.TryGetValue("Idioma", out var lang) ? new[] { lang } : null),
                    Subtitle = dict.TryGetValue("Legenda", out var subtitle) ? subtitle : null,
                    Format = dict.TryGetValue("Formato", out var format) ? format : null,
                    Quality = dict.TryGetValue("Qualidade", out var quality) ? quality : null,
                    Size = dict.TryGetValue("Tamanho", out var size) ? size : null,
                    ReleaseYear = dict.TryGetValue("Ano de Lançamento", out var releaseYear) ? releaseYear : (dict.TryGetValue("Lançamento", out var year) ? year : null),
                    Duration = dict.TryGetValue("Duração", out var duration) ? duration : null,
                    AudioQuality = dict.TryGetValue("Qualidade de Áudio", out var audioQuality) ? audioQuality : null,
                    VideoQuality = dict.TryGetValue("Qualidade de Vídeo", out var videoQuality) ? videoQuality : null,
                    TitleTranslated = dict.TryGetValue("Título Traduzido", out var titleTr) ? titleTr : null,
                    TitleOriginal = dict.TryGetValue("Título Original", out var titleOr) ? titleOr : (dict.TryGetValue("Título", out var title) ? title : null),
                    IMDb = dict.TryGetValue("IMDb", out var imdb) ? imdb : null
                };
            }
        }
    }

    public class SimpleRequestGenerator : IIndexerRequestGenerator
    {
        private readonly string _siteLink;
        private string SearchQueryParamsKey { get; }

        public SimpleRequestGenerator(string siteLink, string searchQueryParamsKey = "?s=")
        {
            _siteLink = siteLink;
            SearchQueryParamsKey = searchQueryParamsKey;
        }

        public IndexerPageableRequestChain GetSearchRequests(TorznabQuery query)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            var searchUrl = $"{_siteLink}{SearchQueryParamsKey}";
            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                searchUrl += WebUtility.UrlEncode(query.SearchTerm);
                if (query.Season is { } value)
                {
                    searchUrl += WebUtility.UrlEncode($" {value}");
                }
            }
            else
            {
                searchUrl = _siteLink;
            }

            pageableRequests.Add(new[] { new IndexerRequest(searchUrl) });

            return pageableRequests;
        }
    }

    public static class RowParsingExtensions
    {
        public static Uri ExtractMagnet(this IElement downloadButton)
        {
            var magnetLink = downloadButton.GetAttribute("href");
            var magnet = string.IsNullOrEmpty(magnetLink) ? null : new Uri(magnetLink);
            return magnet;
        }

        public static List<string> ExtractGenres(this IElement row)
        {
            var genres = new List<string>();
            row.ExtractFromRow(
                "span:contains(\"Gênero:\")", genreText =>
                {
                    ExtractPattern(
                        genreText, @"Gênero:\s*(.+)", genre => ExtractMultiValuesFromField(values: out genres, field: genre));
                });
            return genres;
        }

        public static List<int> ExtractCategory(this IElement row, string title = null)
        {
            var releaseCategory = new List<int>();
            var category = TorznabCatType.Movies;
            row.ExtractFromRow(
                "div.title > a", categoryText =>
                {
                    category = ExtractCategory(categoryText);
                });
            if (!category.Equals(TorznabCatType.TV) && !string.IsNullOrWhiteSpace(title))
            {
                category = ExtractCategory(title);
            }
            releaseCategory.Add(category.ID);
            return releaseCategory;
        }

        private static TorznabCategory ExtractCategory(string text)
        {
            var hasSeasonInfo = text.IndexOf("temporada", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                text.IndexOf("season", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                Regex.IsMatch(text, @"\bS\d{1,2}(?:E\d{1,2})?\b", RegexOptions.IgnoreCase);
            var category = hasSeasonInfo ? TorznabCatType.TV : TorznabCatType.Movies;
            return category;
        }

        public static DateTime ExtractReleaseDate(this IElement row)
        {
            var result = DateTime.Today;
            row.ExtractFromRow(
                "span:contains(\"Lançamento:\")", releaseDateText =>
                {
                    ExtractPattern(
                        releaseDateText, @"Lançamento:\s*(.+)", releaseDate =>
                        {
                            DateTime.TryParseExact(
                                releaseDate, "yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
                        });
                });
            return result;
        }

        public static List<string> ExtractSubtitles(this IElement row)
        {
            var subtitles = new List<string>();
            row.ExtractFromRow(
                "span:contains(\"Legenda:\")", subtitleText =>
                {
                    ExtractPattern(
                        subtitleText, @"Legenda:\s*(.+)", subtitle => ExtractMultiValuesFromField(values: out subtitles, field: subtitle));
                });
            return subtitles;
        }

        public static long ExtractSize(this IElement row)
        {
            long result = 0;
            row.ExtractFromRow(
                "span:contains(\"Tamanho:\")", sizeText =>
                {
                    ExtractPattern(
                        sizeText, @"Tamanho:\s*(.+)", size =>
                        {
                            result = GetBytes(size);
                        });
                });
            return result;
        }

        public static long GetBytes(string text)
        {
            if (Regex.Matches(text, @"\b[GTKP]?B\b", RegexOptions.IgnoreCase).Count > 1)
            {
                var match = Regex.Match(text, @"[GTKP]?B([.,| \d]+[GTKP]?B)", RegexOptions.RightToLeft);
                if (match.Success)
                {
                    text = match.Groups[1].Value;
                }
            }

            return ParseUtil.GetBytes(text);
        }

        public static List<string> ExtractLanguages(this IElement row)
        {
            var languages = new List<string>();
            row.ExtractFromRow(
                "span:contains(\"Áudio:\")", audioText =>
                {
                    ExtractPattern(
                        audioText, @"Áudio:\s*(.+)", language => ExtractMultiValuesFromField(values: out languages, field: language));
                });
            if (languages.Count == 0)
            {
                row.ExtractFromRow(
                    "span:contains(\"Idioma:\")", languageText =>
                    {
                        ExtractPattern(
                            languageText, @"Idioma:\s*(.+)", language => ExtractMultiValuesFromField(values: out languages, field: language));
                    });
            }
            return languages;
        }
        private static void ExtractMultiValuesFromField(out List<string> values, in string field)
        {
            if (field.Contains("|"))
            {
                values = field.Split('|').Select(token => token.Trim()).ToList();
            }
            else if (field.Contains(","))
            {
                values = field.Split(',').Select(token => token.Trim()).ToList();
            }
            else
            {
                values = new List<string> { field };
            }
        }

        public static void ExtractFromRow(this IElement row, string selector, Action<string> extraction)
        {
            var element = row.QuerySelector(selector);
            if (element != null)
            {
                extraction(element.TextContent);
            }
        }

        public static void ExtractPattern(string text, string pattern, Action<string> extraction)
        {
            var match = Regex.Match(text, pattern);
            if (match.Success)
            {
                extraction(match.Groups[1].Value.Trim());
            }
        }
    }
    public abstract class PublicBrazilianParser : IParseIndexerResponse
    {
        public abstract IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse);



        public string ExtractTitleOrDefault(IElement downloadButton, string defaultTitle)
        {
            var magnetTitle = "";
            RowParsingExtensions.ExtractPattern(downloadButton?.GetAttribute("href"),
                                                @"&dn=(.+?)&|&dn=(.+?)$",
                                                mt => magnetTitle = HttpUtility.UrlDecode(mt));
            if (!string.IsNullOrWhiteSpace(magnetTitle))
                return FormatTitle(CleanTitle(magnetTitle), ExtractResolution(magnetTitle));
            var description = GetTitleElementOrNull(downloadButton);
            var resolution = description?.TextContent switch
            {
                string text when !string.IsNullOrWhiteSpace(text) => ExtractResolution(text),
                _ => ExtractResolution(defaultTitle)
            };
            var title = (defaultTitle, description?.TextContent) switch
            {
                (string defTitle, _) when !string.IsNullOrWhiteSpace(defTitle) => CleanTitle(defTitle),
                (_, string text) when !string.IsNullOrWhiteSpace(text) => CleanTitle(text),
                _ => defaultTitle
            };
            return FormatTitle(title, resolution);
        }

        private string ExtractResolution(string text)
        {
            var resolution = "";
            RowParsingExtensions.ExtractPattern(text, @"\b(\d{3,4}p)\b", res => resolution = res);
            return resolution;
        }

        private string FormatTitle(string title, string resolution = null)
        {
            return string.IsNullOrWhiteSpace(resolution)
                ? $"{title}"
                : $"{title} {resolution}";
        }

        public long ExtractSizeByResolution(string title)
        {
            var resolution = "Other";
            RowParsingExtensions.ExtractPattern(
                title, @"\b(\d{3,4}p)\b", res =>
                {
                    resolution = res;
                });

            var size = resolution switch
            {
                "720p" => "1GB",
                "1080p" => "2.5GB",
                "2160p" => "5GB",
                _ => "512MB"
            };

            return RowParsingExtensions.GetBytes(size);
        }

        protected static string CleanTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return null;

            // Remove size info in parentheses
            title = Regex.Replace(title, @"\(\d+(?:\.\d+)?\s*(?:GB|MB)\)", "", RegexOptions.IgnoreCase);

            // Remove quality info
            title = Regex.Replace(title, @"\b(?:720p|1080p|2160p|4K)\b", "", RegexOptions.IgnoreCase);

            // Remove source info
            title = Regex.Replace(title, @"\b(?:WEB-DL|BRRip|HDRip|WEBRip|BluRay|Torrent|Download)\b", "", RegexOptions.IgnoreCase);

            // Remove language info
            title = Regex.Replace(title, @"\b(?:Legendado|Leg|Dublado|Dub|[AÁ]udio)\b", "", RegexOptions.IgnoreCase);

            // Clean up torrent group names
            title = Regex.Replace(title, @"HIDRATORRENTS\.ORG|\[?Erai-raws\]?|\[?Anime Time\]?|COMANDO4K\.COM|COMANDO\.TO|VEMTORRENT\.COM|VACATORRENT\.COM", "", RegexOptions.IgnoreCase);

            // Remove brackets/parentheses content
            title = Regex.Replace(title, @"\[(?:.*?)\]|\((?:.*?)\)", "", RegexOptions.IgnoreCase);

            // Remove dangling punctuation and separators
            title = Regex.Replace(title, @"[\\/,|~_-]+\s*|\s*[\\/,|~_-]+", " ", RegexOptions.IgnoreCase);

            // Clean up multiple spaces
            title = Regex.Replace(title, @"\s+", " ");

            // Remove file extension from the beginning of title
            title = Regex.Replace(title, @"MKV|MP4", "", RegexOptions.IgnoreCase);

            // Remove dots between words but keep dots in version numbers
            title = Regex.Replace(title, @"(?<!\d)\.(?!\d)", " ", RegexOptions.IgnoreCase);

            // Remove any remaining punctuation at start/end
            title = title.Trim(' ', '.', ',', '-', '_', '~', '/', '\\', '|');
            return title.Trim();
        }

        protected abstract INode GetTitleElementOrNull(IElement downloadButton);

        protected static bool NotSpanTag(INode description) =>
            (description.NodeType != NodeType.Element || ((Element)description).TagName != "SPAN");
    }
}
