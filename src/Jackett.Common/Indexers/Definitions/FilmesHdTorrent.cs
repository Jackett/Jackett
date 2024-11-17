using System;
using System.Collections.Generic;
using System.Linq;
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
    public class BaixarFilmesTorrents : FilmesHdTorrent
    {
        public override string Id => "baixarfilmestorrents";

        public override string Name => "BaixarFilmesTorrents";

        public override string SiteLink { get; protected set; } = "https://baixarfilmestorrents.net/";

        public BaixarFilmesTorrents(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps, ICacheService cs) : base(configService, wc, l, ps, cs)
        {
        }
    }
    public class FilmesHdTorrent : ComandoFilmes
    {
        public override string Id => "filmeshdtorrent";
        public override string Name => "Filmes HD Torrent";
        public override string SiteLink { get; protected set; } = "https://www.filmeshdtorrent.vip/";

        public FilmesHdTorrent(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps, ICacheService cs) : base(configService, wc, l, ps, cs)
        {
        }

        public override IParseIndexerResponse GetParser() => new FilmesHdTorrentParser(webclient, Name);
    }
    public class FilmesHdTorrentParser : PublicBrazilianParser
    {
        private readonly WebClient _webclient;

        public FilmesHdTorrentParser(WebClient webclient, string name) : base(name)
        {
            _webclient = webclient;
        }

        private Dictionary<string, string> ExtractFileInfo(IDocument detailsDom)
        {
            var fileInfo = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var content = detailsDom.QuerySelector("div.content");
            if (content == null)
                return fileInfo;

            var infoParagraph = content.QuerySelector("p");
            if (infoParagraph == null)
                return fileInfo;

            var lines = infoParagraph.InnerHtml.Split(new[] { "<br>" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.Contains("<strong>") && line.Contains("</strong>") && line.Contains(":"))
                {
                    var parts = line.Split(new[] { ':' }, 2);
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Replace("<strong>", "").Replace("</strong>", "").Trim();
                        var value = parts[1].Replace("<strong>", "").Replace("</strong>", "").Replace("<span style=\"12px arial,verdana,tahoma;\">", "").Replace("</span>", "").Replace("<span class=\"entry-date\">", "").Trim();
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
                    Category = row.ExtractCategory(),
                    PublishDate = row.ExtractReleaseDate()
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
                    release.Languages = fileInfo.Audio?.ToList() ?? release.Languages;
                    release.Genres = fileInfo.Genres?.ToList() ?? release.Genres;
                    release.Subs = string.IsNullOrEmpty(fileInfo.Subtitle) ? release.Subs : new[] { fileInfo.Subtitle };
                    release.Size = string.IsNullOrEmpty(fileInfo.Size) ? release.Size : ParseUtil.GetBytes(fileInfo.Size);
                    release.Link = release.Guid = release.MagnetUri = magnet;
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
