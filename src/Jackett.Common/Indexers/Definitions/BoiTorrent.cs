using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Jackett.Common.Extensions;
using Jackett.Common.Indexers.Definitions.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers.Definitions
{
    public class BoiTorrent : PublicBrazilianIndexerBase
    {
        public override string Id => "boitorrent";
        public override string Name => "BoiTorrent";
        public override string SiteLink { get; protected set; } = "https://boitorrent.com/";

        public BoiTorrent(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps, ICacheService cs)
            : base(configService, wc, l, ps, cs)
        {
        }

        public override TorznabCapabilities TorznabCaps
        {
            get
            {
                var caps = base.TorznabCaps;
                caps.Categories.AddCategoryMapping("desenhos", TorznabCatType.TVAnime);
                return caps;
            }
        }

        public override IParseIndexerResponse GetParser() => new BoiTorrentParser(webclient);

        public override IIndexerRequestGenerator GetRequestGenerator() => new BoiTorrentRequestGenerator(SiteLink);
    }

    public class BoiTorrentRequestGenerator : IIndexerRequestGenerator
    {
        private readonly string _siteLink;

        public BoiTorrentRequestGenerator(string siteLink)
        {
            _siteLink = siteLink;
        }

        public IndexerPageableRequestChain GetSearchRequests(TorznabQuery query)
        {
            var chain = new IndexerPageableRequestChain();
            var term = query.SearchTerm ?? string.Empty;
            if (query.Season is { } season)
                term = $"{term} {season}".Trim();
            var url = $"{_siteLink}index.php?campo1={System.Net.WebUtility.UrlEncode(term)}&nome_campo1=pesquisa&categoria=lista&pagina=1";
            chain.Add(new[] { new IndexerRequest(url) });
            return chain;
        }
    }

    public class BoiTorrentParser : PublicBrazilianParser
    {
        private const int MaxConcurrentRequests = 2;

        private readonly WebClient _webclient;

        public BoiTorrentParser(WebClient webclient)
        {
            _webclient = webclient;
        }

        private static Dictionary<string, string> ExtractFileInfo(IDocument detailsDom)
        {
            var fileInfo = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var infoSection = detailsDom.QuerySelector("div.infos");
            if (infoSection == null)
                return fileInfo;

            var lines = infoSection.InnerHtml.Split(new[] { "<br>" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (!line.Contains("<strong>") || !line.Contains(":"))
                    continue;

                var cleanLine = Regex.Replace(line, @"<[^>]+>", string.Empty);
                cleanLine = System.Net.WebUtility.HtmlDecode(cleanLine);
                cleanLine = Regex.Replace(cleanLine, @"\s+", " ");
                var parts = cleanLine.Split(new[] { ':' }, 2);
                if (parts.Length != 2)
                    continue;

                var key = parts[0].Trim();
                var value = parts[1].Trim().Trim('/', ',', '|').Trim();
                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                    fileInfo[key] = value;
            }

            return NormalizeBoiTorrentKeys(fileInfo);
        }

        private static Dictionary<string, string> NormalizeBoiTorrentKeys(Dictionary<string, string> raw)
        {
            var canonical = new Dictionary<string, string>(raw, StringComparer.OrdinalIgnoreCase);

            AliasKey(canonical, "Titulo Original", "Título Original");
            AliasKey(canonical, "Titulo Traduzido", "Título Traduzido");
            AliasKey(canonical, "Titulo", "Título");

            if (canonical.TryGetValue("Gênero", out var genres))
                canonical["Gênero"] = JoinSlashSeparated(genres);

            if (canonical.TryGetValue("Idioma / Áudio", out var idiomaAudio) && !canonical.ContainsKey("Áudio"))
                canonical["Áudio"] = JoinSlashSeparated(idiomaAudio);
            canonical.Remove("Idioma / Áudio");

            if (canonical.TryGetValue("Legendas", out var subtitles) && !canonical.ContainsKey("Legenda"))
                canonical["Legenda"] = subtitles;
            canonical.Remove("Legendas");

            if (canonical.TryGetValue("Crítica Especializada", out var critics) && !canonical.ContainsKey("IMDb"))
            {
                var match = Regex.Match(critics, @"Imdb\s*:\s*([\d.,]+)", RegexOptions.IgnoreCase);
                if (match.Success)
                    canonical["IMDb"] = match.Groups[1].Value;
            }

            return canonical;
        }

        private static void AliasKey(Dictionary<string, string> dict, string aliasKey, string canonicalKey)
        {
            if (dict.TryGetValue(aliasKey, out var value) && !dict.ContainsKey(canonicalKey))
                dict[canonicalKey] = value;
            dict.Remove(aliasKey);
        }

        private static string JoinSlashSeparated(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || !value.Contains('/'))
                return value;
            return string.Join(", ",
                value.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => v.Trim())
                    .Where(v => !string.IsNullOrEmpty(v)));
        }

        private static int MapSearchCategory(string text) => text?.Trim().ToLowerInvariant() switch
        {
            "desenho" or "desenhos" or "anime" or "animes" => TorznabCatType.TVAnime.ID,
            "filme" or "filmes" => TorznabCatType.Movies.ID,
            "série" or "séries" or "serie" or "series" => TorznabCatType.TV.ID,
            _ => 0
        };

        private static (int CategoryId, string Year) ParseCategoryAndYear(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return (0, null);

            var match = Regex.Match(
                title,
                @"^(?<cat>filme|filmes|s[eé]rie|s[eé]ries|desenho|desenhos|anime|animes)\s+.+?\s+(?<year>\d{4})(?:\s+torrent)?\s*$",
                RegexOptions.IgnoreCase);
            if (!match.Success)
                return (0, null);

            return (MapSearchCategory(match.Groups["cat"].Value), match.Groups["year"].Value);
        }

        public override IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var firstPageUri = new Uri(indexerResponse.Request.Url);
            var firstDom = new HtmlParser().ParseDocument(indexerResponse.Content);

            var listingDocs = new List<(Uri PageUri, IDocument Document)> { (firstPageUri, firstDom) };

            var totalPages = GetTotalPages(firstDom);
            if (totalPages > 1)
            {
                var extraPageUris = Enumerable.Range(2, totalPages - 1)
                    .Select(p => BuildPageUri(firstPageUri, p))
                    .ToList();
                var extraDocs = FetchDocumentsAsync(extraPageUris).GetAwaiter().GetResult();
                listingDocs.AddRange(extraDocs.Select(t => (t.Uri, t.Document)));
            }

            var searchRows = new List<(IElement Row, Uri DetailUrl)>();
            foreach (var (pageUri, doc) in listingDocs)
            {
                foreach (var row in doc.QuerySelectorAll("div.row.semelhantes"))
                {
                    var href = row.QuerySelector("h2 a[href]")?.GetAttribute("href");
                    if (string.IsNullOrWhiteSpace(href))
                        continue;
                    searchRows.Add((row, new Uri(pageUri, href)));
                }
            }

            var detailUris = searchRows.Select(r => r.DetailUrl).Distinct().ToList();
            var detailDocs = FetchDocumentsAsync(detailUris).GetAwaiter().GetResult()
                .ToDictionary(t => t.Uri, t => t.Document);

            var releases = new List<ReleaseInfo>();
            foreach (var (row, detailUrl) in searchRows)
            {
                if (!detailDocs.TryGetValue(detailUrl, out var detailsDom))
                    continue;
                BuildReleases(row, detailUrl, detailsDom, releases);
            }
            return releases;
        }

        private void BuildReleases(IElement row, Uri detailUrl, IDocument detailsDom, List<ReleaseInfo> releases)
        {
            var titleAttr = row.QuerySelector("h2 a")?.GetAttribute("title")
                            ?? row.QuerySelector("img")?.GetAttribute("title");
            var (categoryId, searchYearText) = ParseCategoryAndYear(titleAttr);

            var fileInfoDict = ExtractFileInfo(detailsDom);
            var fileInfo = PublicBrazilianIndexerBase.FileInfo.FromDictionary(fileInfoDict);

            var publishDate = DateTime.Today;
            var yearCandidate = fileInfo.ReleaseYear ?? searchYearText;
            if (!string.IsNullOrEmpty(yearCandidate) &&
                DateTime.TryParseExact(yearCandidate, "yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedYear))
                publishDate = parsedYear;

            var defaultTitle = !string.IsNullOrWhiteSpace(fileInfo.TitleOriginal)
                ? fileInfo.TitleOriginal
                : CleanTitle(fileInfo.TitleTranslated ?? string.Empty);

            var magnetLinks = detailsDom.QuerySelectorAll("a.list-group-item.newdawn[href^=\"magnet:\"]");
            foreach (var magnetLink in magnetLinks)
            {
                var magnetHref = magnetLink.GetAttribute("href");
                if (string.IsNullOrWhiteSpace(magnetHref))
                    continue;

                var magnetUri = new Uri(magnetHref);
                var release = new ReleaseInfo
                {
                    Details = detailUrl,
                    Guid = magnetUri,
                    MagnetUri = magnetUri,
                    PublishDate = publishDate,
                    Seeders = 1,
                    DownloadVolumeFactor = 0,
                    UploadVolumeFactor = 1,
                    Languages = fileInfo.Audio?.ToList(),
                    Genres = fileInfo.Genres?.ToList(),
                    Subs = string.IsNullOrEmpty(fileInfo.Subtitle) ? null : new[] { fileInfo.Subtitle }
                };
                var resolution = fileInfo.Quality ?? fileInfo.VideoQuality ?? string.Empty;
                release.Title = ExtractTitleOrDefault(magnetLink, release.Title);

                release.Category = categoryId > 0
                    ? new List<int> { categoryId }
                    : magnetLink.ExtractCategory(release.Title);
                release.Size = ExtractSizeByResolution(release.Title);

                if (release.Title.IsNotNullOrWhiteSpace())
                    releases.Add(release);
            }
        }

        private static int GetTotalPages(IDocument dom)
        {
            var maxPage = 1;
            foreach (var link in dom.QuerySelectorAll(".paginacao a.page-link"))
            {
                var match = Regex.Match(link.GetAttribute("title") ?? string.Empty, @"Pagina\s+(\d+)", RegexOptions.IgnoreCase);
                if (match.Success && int.TryParse(match.Groups[1].Value, out var n) && n > maxPage)
                    maxPage = n;
            }
            return maxPage;
        }

        private static Uri BuildPageUri(Uri baseUri, int page)
        {
            var url = baseUri.AbsoluteUri;
            url = Regex.IsMatch(url, @"[?&]pagina=\d+", RegexOptions.IgnoreCase)
                ? Regex.Replace(url, @"([?&])pagina=\d+", $"$1pagina={page}", RegexOptions.IgnoreCase)
                : url + (url.Contains("?") ? "&" : "?") + "pagina=" + page;
            return new Uri(url);
        }

        private async Task<List<(Uri Uri, IDocument Document)>> FetchDocumentsAsync(IReadOnlyCollection<Uri> uris)
        {
            if (uris.Count == 0)
                return new List<(Uri, IDocument)>();

            using var semaphore = new SemaphoreSlim(MaxConcurrentRequests);
            var tasks = uris.Select(async uri =>
            {
                await semaphore.WaitAsync().ConfigureAwait(false);
                try
                {
                    var response = await _webclient.GetResultAsync(new WebRequest(uri.ToString())).ConfigureAwait(false);
                    IDocument document = new HtmlParser().ParseDocument(response.ContentString ?? string.Empty);
                    return (uri, document);
                }
                finally
                {
                    semaphore.Release();
                }
            });
            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            return results.ToList();
        }

        protected override INode GetTitleElementOrNull(IElement downloadButton) => downloadButton.FirstChild;
    }
}
