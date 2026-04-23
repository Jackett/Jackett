using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
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
            var url = $"{_siteLink}busca.php?palavraPesquisa={System.Net.WebUtility.UrlEncode(term)}";
            chain.Add(new[] { new IndexerRequest(url) });
            return chain;
        }
    }

    public class BoiTorrentParser : PublicBrazilianParser
    {
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
            "desenho" => TorznabCatType.TVAnime.ID,
            "filme" => TorznabCatType.Movies.ID,
            "série" => TorznabCatType.TV.ID,
            "serie" => TorznabCatType.TV.ID,
            _ => 0
        };

        public override IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var releases = new List<ReleaseInfo>();
            var parser = new HtmlParser();
            var dom = parser.ParseDocument(indexerResponse.Content);
            var rows = dom.QuerySelectorAll("#caixaBusca > a");
            var baseUri = new Uri(indexerResponse.Request.Url);

            foreach (var row in rows)
            {
                var href = row.GetAttribute("href");
                if (string.IsNullOrWhiteSpace(href))
                    continue;

                var detailUrl = new Uri(baseUri, href);
                var categoryText = row.QuerySelector(".flutuaPesquisa > span.letraBusca:nth-of-type(2)")?.TextContent;
                var categoryId = MapSearchCategory(categoryText);
                var searchYearText = row.QuerySelector(".flutuaPesquisa > span.letraBusca:nth-of-type(4)")?.TextContent?.Trim();

                var detailsPage = _webclient.GetResultAsync(new WebRequest(detailUrl.ToString())).Result;
                var detailsDom = parser.ParseDocument(detailsPage.ContentString);

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
                        Title = ExtractTitleOrDefault(magnetLink, defaultTitle),
                        Languages = fileInfo.Audio?.ToList(),
                        Genres = fileInfo.Genres?.ToList(),
                        Subs = string.IsNullOrEmpty(fileInfo.Subtitle) ? null : new[] { fileInfo.Subtitle }
                    };
                    release.Category = categoryId > 0
                        ? new List<int> { categoryId }
                        : magnetLink.ExtractCategory(release.Title);
                    release.Size = ExtractSizeByResolution(release.Title);

                    if (release.Title.IsNotNullOrWhiteSpace())
                        releases.Add(release);
                }
            }
            return releases;
        }

        protected override INode GetTitleElementOrNull(IElement downloadButton) => downloadButton.FirstChild;
    }
}
