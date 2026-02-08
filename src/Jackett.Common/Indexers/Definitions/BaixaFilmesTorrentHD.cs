using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Jackett.Common.Extensions;
using Jackett.Common.Indexers.Definitions.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;
using WebRequest = Jackett.Common.Utils.Clients.WebRequest;

namespace Jackett.Common.Indexers.Definitions
{
    public class BaixaFilmesTorrentHD : PublicBrazilianIndexerBase
    {
        public override string Id => "baixafilmestorrenthd";
        public override string Name => "Baixa Filmes Torrent HD";
        public override string SiteLink { get; protected set; } = "https://baixafilmestorrenthd.com/";

        public BaixaFilmesTorrentHD(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps, ICacheService cs)
            : base(configService, wc, l, ps, cs)
        {
        }

        public override IParseIndexerResponse GetParser() => new BaixaFilmesTorrentHDParser(webclient);
    }

    public class BaixaFilmesTorrentHDParser : PublicBrazilianParser
    {
        private readonly WebClient _webclient;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public BaixaFilmesTorrentHDParser(WebClient webclient)
        {
            _webclient = webclient;
        }

        private Dictionary<string, string> ExtractFileInfo(IDocument detailsDom)
        {
            var fileInfo = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var content = detailsDom.QuerySelector("div.content");
            if (content == null)
                return fileInfo;

            var lines = content.InnerHtml.Split(new[] { "<br>", "<br/>", "<br />" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.Contains("<strong>") && line.Contains("</strong>") && line.Contains(":"))
                {
                    var cleanLine = Regex.Replace(line, @"<[^>]+>", "");
                    var parts = cleanLine.Split(new[] { ':' }, 2);
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var value = parts[1].Trim();
                        if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                        {
                            fileInfo[key] = value;
                        }
                    }
                }
            }

            return fileInfo;
        }

        private static string NormalizeHost(string host)
        {
            if (string.IsNullOrEmpty(host))
                return host;
            var h = host.Trim();
            if (h.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
                h = h.Substring(4);
            return h.ToLowerInvariant();
        }

        public override IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var releases = new List<ReleaseInfo>();
            if (string.IsNullOrWhiteSpace(indexerResponse?.Content))
            {
                _logger.Warn("BaixaFilmesTorrentHD: indexerResponse.Content is null or empty");
                return releases;
            }
            var parser = new HtmlParser();
            var dom = parser.ParseDocument(indexerResponse.Content);
            var baseUri = new Uri(indexerResponse.Request.Url);
            var baseHost = NormalizeHost(baseUri.Host);
            var rows = dom.QuerySelectorAll("div.item:has(a[title])").ToList();
            var detailItems = new List<(Uri detailUrl, string title, IElement row)>();

            _logger.Debug($"BaixaFilmesTorrentHD: Found {rows.Count} items with div.item:has(a[title])");

            if (rows.Count > 0)
            {
                foreach (var row in rows)
                {
                    var detailAnchor = row.QuerySelector("a[title]");
                    var href = detailAnchor?.GetAttribute("href");
                    if (string.IsNullOrEmpty(href))
                        continue;
                    var detailUrl = href.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                        ? new Uri(href)
                        : new Uri(baseUri, href.TrimStart('/'));
                    var title = CleanTitle(row.QuerySelector("div.titulo span")?.TextContent.Trim()
                        ?? detailAnchor?.GetAttribute("title")?.Trim() ?? string.Empty);
                    detailItems.Add((detailUrl, title, row));
                }
            }
            else
            {
                var links = dom.QuerySelectorAll("a[href]");
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var a in links)
                {
                    var href = a.GetAttribute("href");
                    if (string.IsNullOrEmpty(href))
                        continue;
                    Uri detailUrl;
                    try
                    {
                        detailUrl = href.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                            ? new Uri(href)
                            : new Uri(baseUri, href.StartsWith("/") ? href : "/" + href);
                    }
                    catch (UriFormatException)
                    {
                        continue;
                    }
                    if (NormalizeHost(detailUrl.Host) != baseHost)
                        continue;
                    var pathSegment = detailUrl.AbsolutePath.Trim('/');
                    if (string.IsNullOrEmpty(pathSegment) || pathSegment.IndexOf('/') >= 0)
                        continue;
                    if (pathSegment.StartsWith("genero", StringComparison.OrdinalIgnoreCase) ||
                        pathSegment.StartsWith("categoria", StringComparison.OrdinalIgnoreCase) ||
                        pathSegment.StartsWith("page", StringComparison.OrdinalIgnoreCase) ||
                        pathSegment.StartsWith("tag", StringComparison.OrdinalIgnoreCase) ||
                        pathSegment.StartsWith("author", StringComparison.OrdinalIgnoreCase) ||
                        pathSegment.StartsWith("feed", StringComparison.OrdinalIgnoreCase) ||
                        pathSegment.StartsWith("wp-", StringComparison.OrdinalIgnoreCase))
                        continue;
                    var key = detailUrl.AbsoluteUri;
                    if (seen.Contains(key))
                        continue;
                    seen.Add(key);
                    var title = CleanTitle(a.TextContent?.Trim() ?? pathSegment);
                    detailItems.Add((detailUrl, title, null));
                }
                _logger.Debug($"BaixaFilmesTorrentHD: Fallback found {detailItems.Count} candidate detail URLs");
            }

            const int maxItemsPerPage = 20;
            _logger.Debug($"BaixaFilmesTorrentHD: Processing {Math.Min(detailItems.Count, maxItemsPerPage)} items (out of {detailItems.Count} total)");
            foreach (var (detailUrl, titleFromList, row) in detailItems.Take(maxItemsPerPage))
            {
                var detailsParser = new HtmlParser();
                var detailsPage = _webclient.GetResultAsync(new WebRequest(detailUrl.ToString())).Result;
                if (detailsPage.HasHttpError || string.IsNullOrEmpty(detailsPage.ContentString))
                    continue;

                var detailsDom = detailsParser.ParseDocument(detailsPage.ContentString);
                var fileInfoDict = ExtractFileInfo(detailsDom);
                var fileInfo = PublicBrazilianIndexerBase.FileInfo.FromDictionary(fileInfoDict);
                var releaseCommonInfo = new ReleaseInfo
                {
                    Title = titleFromList,
                    Details = detailUrl,
                    Guid = detailUrl,
                    PublishDate = row != null ? row.ExtractReleaseDate() : DateTime.UtcNow,
                    Seeders = 1
                };
                var magnetLinks = detailsDom.QuerySelectorAll("a[href^=\"magnet:?xt\"]");

                foreach (var downloadButton in magnetLinks)
                {
                    var magnet = downloadButton.ExtractMagnet();
                    if (magnet == null)
                        continue;

                    var release = releaseCommonInfo.Clone() as ReleaseInfo;
                    release.Title = ExtractTitleOrDefault(downloadButton, release.Title);
                    release.Category = downloadButton.ExtractCategory(release.Title);
                    release.Languages = fileInfo.Audio?.ToList() ?? release.Languages;
                    release.Genres = fileInfo.Genres?.ToList() ?? release.Genres;
                    release.Subs = string.IsNullOrEmpty(fileInfo.Subtitle) ? release.Subs : new[] { fileInfo.Subtitle };
                    var size = RowParsingExtensions.GetBytes(fileInfo.Size ?? string.Empty);
                    release.Size = size > 0 ? size : ExtractSizeByResolution(release.Title);
                    release.Guid = release.MagnetUri = magnet;
                    release.DownloadVolumeFactor = 0;
                    release.UploadVolumeFactor = 1;

                    if (release.Title.IsNotNullOrWhiteSpace())
                        releases.Add(release);
                }
            }

            return releases;
        }

        protected override INode GetTitleElementOrNull(IElement downloadButton)
        {
            var description = downloadButton.PreviousSibling;
            while (description != null && NotSpanTag(description))
            {
                description = description.PreviousSibling;
            }

            return description;
        }
    }
}
