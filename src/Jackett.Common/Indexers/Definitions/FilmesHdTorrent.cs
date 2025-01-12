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

namespace Jackett.Common.Indexers.Definitions
{
    public class FilmesHdTorrent : PublicBrazilianIndexerBase
    {
        public override string Id => "filmeshdtorrent";
        public override string Name => "Filmes HD Torrent";
        public override string SiteLink { get; protected set; } = "https://www.filmeshdtorrent.vip/";

        public override string[] AlternativeSiteLinks { get; protected set; } = {
            "https://www.filmeshdtorrent.vip/",
            "https://filmetorrent.org/x/",
        };

        public override string[] LegacySiteLinks { get; protected set; } = {
            "https://baixarfilmestorrents.net/",
            "https://comandofilmes.life/",
            "https://torrentalerta.net/",
            "https://filmetorrent.org/baixar/",
        };

        public FilmesHdTorrent(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps, ICacheService cs) : base(configService, wc, l, ps, cs)
        {
        }

        public override IParseIndexerResponse GetParser() => new FilmesHdTorrentParser(webclient);
    }
    public class FilmesHdTorrentParser : PublicBrazilianParser
    {
        private readonly WebClient _webclient;

        public FilmesHdTorrentParser(WebClient webclient)
        {
            _webclient = webclient;
        }

        private Dictionary<string, string> ExtractFileInfo(IDocument detailsDom)
        {
            var fileInfo = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var content = detailsDom.QuerySelector("div.content");
            if (content == null)
                return fileInfo;

            var lines = content.InnerHtml.Split(new[] { "<br>" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.Contains("<strong>") && line.Contains("</strong>") && line.Contains(":"))
                {
                    var cleanLine = Regex.Replace(line, @"<[^>]+>", ""); // Remove HTML tags
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

        public override IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var releases = new List<ReleaseInfo>();

            var parser = new HtmlParser();
            var dom = parser.ParseDocument(indexerResponse.Content);
            var rows = dom.QuerySelectorAll("div.item");

            foreach (var row in rows)
            {
                var detailsParser = new HtmlParser();
                var detailAnchor = row.QuerySelector("a[title]");
                var detailUrl = new Uri(detailAnchor?.GetAttribute("href") ?? string.Empty);
                var releaseCommonInfo = new ReleaseInfo
                {
                    Title = CleanTitle(row.QuerySelector("div.titulo span")?.TextContent.Trim() ?? detailAnchor?.GetAttribute("title")?.Trim() ?? string.Empty),
                    Details = detailUrl,
                    Guid = detailUrl,
                    PublishDate = row.ExtractReleaseDate(),
                    Seeders = 1
                };

                var detailsPage = _webclient.GetResultAsync(new WebRequest(detailUrl.ToString())).Result;
                var detailsDom = detailsParser.ParseDocument(detailsPage.ContentString);

                var fileInfoDict = ExtractFileInfo(detailsDom);
                var fileInfo = PublicBrazilianIndexerBase.FileInfo.FromDictionary(fileInfoDict);
                var querySelectorAll = detailsDom.QuerySelectorAll("a[href^=\"magnet:?xt\"]");
                foreach (var downloadButton in querySelectorAll)
                {
                    var magnet = downloadButton.ExtractMagnet();
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
