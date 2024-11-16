using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
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

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new BluDVRequestGenerator(SiteLink);
        }

        public override IParseIndexerResponse GetParser()
        {
            return new BluDVParser(SiteLink, webclient);
        }

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
                var detailsPage = _webclient.GetResultAsync(new WebRequest(detailUrl.ToString())).Result;
                var detailsDom = detailsParser.ParseDocument(detailsPage.ContentString);
                foreach (var downloadButton in detailsDom.QuerySelectorAll("a.customButton[href^=\"magnet:\"]"))
                {
                    var release = new ReleaseInfo
                    {
                        Title = row.QuerySelector("div.title > a")?.TextContent.Trim(),
                        Details = detailUrl,
                        Guid = detailUrl,
                        Link = null // Will be set after getting magnet link
                    };
                    var description = downloadButton.PreviousSibling;
                    while (description != null && description.NodeType == NodeType.Element && ((Element) description).TagName != "SPAN")
                    {
                        description = description.PreviousSibling;
                    }

                    if (description != null)
                    {
                        var descriptionText = description.TextContent;
                        var resolution = Regex.Match(descriptionText, @"(\d{3,4}p)");
                        if (resolution.Success)
                        {
                            release.Title = "[BluDV] " + CleanTitle(release.Title) + resolution.Value;
                        }
                    }
                    var genreElement = row.QuerySelector("span:contains(\"Gênero:\")");
                    if (genreElement != null)
                    {
                        var genreText = genreElement.TextContent;
                        var genreMatch = Regex.Match(genreText, @"Gênero:\s*(.+)");
                        if (genreMatch.Success)
                        {
                            var genre = genreMatch.Groups[1].Value.Trim();
                            release.Genres = new List<string>();
                            foreach (var token  in genre.Split('|'))
                            {
                                release.Genres.Add(token.Trim());
                            }
                        }
                    }
                    var categoryElement = dom.QuerySelector("div.title > a");
                    if (categoryElement != null)
                    {
                        var categoryText = categoryElement.TextContent;
                        if (categoryText.IndexOf("temporada", StringComparison.OrdinalIgnoreCase) >= 0)
                            release.Category = new List<int> { TorznabCatType.TV.ID };
                        else
                            release.Category = new List<int> { TorznabCatType.Movies.ID };
                    }

                    var releaseDateElement = dom.QuerySelector("span:contains(\"Lançamento:\")");
                    if (releaseDateElement != null)
                    {
                        var releaseDateText = releaseDateElement.TextContent;
                        var releaseDateMatch = Regex.Match(releaseDateText, @"Lançamento:\s*(\d{4})");
                        if (releaseDateMatch.Success)
                        {
                            if(DateTime.TryParseExact(
                                releaseDateMatch.Groups[1].Value.Trim(),
                                "yyyy",
                                CultureInfo.InvariantCulture,
                                DateTimeStyles.None,
                                out var date
                                ))
                                release.PublishDate = date;
                        }
                    }

                    var subtitleElement = dom.QuerySelector("span:contains(\"Legenda:\")");
                    if (subtitleElement != null)
                    {
                        var subtitleText = subtitleElement.TextContent;
                        var subtitleMatch = Regex.Match(subtitleText, @"Legenda:\s*(.+)");
                        if (subtitleMatch.Success)
                        {
                            var subtitle = subtitleMatch.Groups[1].Value.Trim();
                            release.Subs = new [] { subtitle };
                        }
                    }

                    var sizeElement = dom.QuerySelector("span:contains(\"Tamanho:\")");
                    if (sizeElement != null)
                    {
                        var sizeText = sizeElement.TextContent;
                        var sizeMatch = Regex.Match(sizeText, @"Tamanho:\s*(.+)");
                        if (sizeMatch.Success)
                        {
                            var size = sizeMatch.Groups[1].Value.Trim();

                            release.Size = ParseUtil.GetBytes(size);
                        }
                    }

                    var audioElement = dom.QuerySelector("span:contains(\"Áudio:\")");
                    if (audioElement != null)
                    {
                        var audioText = audioElement.TextContent;
                        var audioMatch = Regex.Match(audioText, @"Áudio:\s*(.+)");
                        if (audioMatch.Success)
                        {
                            var audio = audioMatch.Groups[1].Value.Trim();
                            release.Languages = new List<string>();
                            foreach (var token in audio.Split('|'))
                            {
                                release.Languages.Add(token.Trim());
                            }
                        }
                    }

                    var magnetLink = downloadButton?.GetAttribute("href");
                    if (!string.IsNullOrEmpty(magnetLink))
                    {
                        release.MagnetUri = new Uri(magnetLink);
                        release.Guid = release.MagnetUri;
                    }

                    // Set category based on title
                    if (release.Title.IndexOf("temporada", StringComparison.OrdinalIgnoreCase) >= 0)
                        release.Category = new List<int> { TorznabCatType.TV.ID };
                    else
                        release.Category = new List<int> { TorznabCatType.Movies.ID };

                    release.DownloadVolumeFactor = 0; // Free
                    release.UploadVolumeFactor = 1;

                    if (release.Title.IsNotNullOrWhiteSpace())
                        releases.Add(release);
                }
            }

            return releases;
        }
    }
}
