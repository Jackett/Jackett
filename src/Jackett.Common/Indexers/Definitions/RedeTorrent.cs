using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Jackett.Common.Extensions;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers.Definitions
{
    public class ApacheTorrent : RedeTorrent
    {
        public override string Id => "apachetorrent";

        public override string Name => "ApacheTorrent";

        public override string SiteLink { get; protected set; } = "https://apachetorrent.com/";

        public ApacheTorrent(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps, ICacheService cs) : base(configService, wc, l, ps, cs)
        {
        }

        public override IParseIndexerResponse GetParser() => new ApacheTorrentParser(webclient, Name, MapTrackerCatToNewznab);
    }
    public class RedeTorrent : FilmesHdTorrent
    {
        public override string Id => "redetorrent";

        public override string Name => "RedeTorrent";

        public override string SiteLink { get; protected set; } = "https://redetorrent.com/";

        public RedeTorrent(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps, ICacheService cs)
            : base(configService, wc, l, ps, cs)
        {
            // Parser = new RedeTorrentParser(wc, Name, MapTrackerCatToNewznab);
        }

        public override IParseIndexerResponse GetParser() => new RedeTorrentParser(webclient, Name, MapTrackerCatToNewznab);
    }

    public class ApacheTorrentParser : RedeTorrentParser
    {
        public ApacheTorrentParser(WebClient webclient, string name, Func<string, ICollection<int>> mapTrackerCatToNewznab) : base(webclient, name, mapTrackerCatToNewznab)
        {
            Tracker = "ApacheTorrent";
        }

        protected override IHtmlCollection<IElement> GetRows(IHtmlDocument dom)
        {
            return dom.QuerySelectorAll("div.capaname");
        }

        protected override IElement GetTitle(IElement row) => null;
    }
    public class RedeTorrentParser : FilmesHdTorrentParser
    {
        private readonly WebClient _webclient;
        private Func<string, ICollection<int> > _mapTrackerCatToNewznab;
        protected string Tracker;

        public RedeTorrentParser(WebClient webclient, string name, Func<string, ICollection<int> > mapTrackerCatToNewznab) : base(webclient, name)
        {
            _webclient = webclient;
            _mapTrackerCatToNewznab = mapTrackerCatToNewznab;
            Tracker = "RedeTorrent";
        }

        private Dictionary<string, string> ExtractFileInfo(IDocument detailsDom)
        {
            var fileInfo = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var infoSection = detailsDom.QuerySelector("#informacoes p");
            if (infoSection == null)
                return fileInfo;

            var lines = infoSection.InnerHtml.Split(new[] { "<br>" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.Contains("<strong>") && line.Contains(":"))
                {
                    var parts = line.Split(new[] { ':' }, 2);
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Replace("<strong>", "").Replace("</strong>", "").Trim();
                        var value = parts[1]
                            .Replace("<strong>", "")
                            .Replace("</strong>", "")
                            .Replace("<span style=\"12px arial,verdana,tahoma;\">", "")
                            .Replace("</span>", "")
                            .Replace("<span class=\"entry-date\">", "")
                            .Trim();
                        value = value switch
                        {
                            var v when v.Contains("Dual Áudio") => v.Replace("Dual Áudio", "Dual"),
                            var v when v.Contains("Dual Audio") => v.Replace("Dual Audio", "Dual"),
                            var v when v.Contains("Full HD") => v.Replace("Full HD", "1080p"),
                            var v when v.Contains("4K") => v.Replace("4K", "2160p"),
                            var v when v.Contains("SD") => v.Replace("SD", "480p"),
                            var v when v.Contains("WEB") => v.Replace("WEB", "WEB-DL"),
                            _ => value
                        };

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
            var rows = GetRows(dom);

            foreach (var row in rows)
            {
                var detailAnchor = row.QuerySelector("a[href^=\"https://\"]");
                if (detailAnchor == null)
                    continue;

                var detailUrl = new Uri(detailAnchor.GetAttribute("href") ?? string.Empty);
                var titleElement = GetTitle(row);
                var title = CleanTitle(titleElement?.TextContent.Trim() ?? detailAnchor.GetAttribute("title")?.Trim() ?? string.Empty);

                var releaseCommonInfo = new ReleaseInfo
                {
                    Title = title,
                    Details = detailUrl,
                    Guid = detailUrl,
                    Category = row.ExtractCategory()
                };

                var detailsPage = _webclient.GetResultAsync(new WebRequest(detailUrl.ToString())).Result;
                var detailsDom = parser.ParseDocument(detailsPage.ContentString);

                var fileInfoDict = ExtractFileInfo(detailsDom);
                var fileInfo = PublicBrazilianIndexerBase.FileInfo.FromDictionary(fileInfoDict);
                releaseCommonInfo.PublishDate = fileInfo.ReleaseYear != null ? DateTime.ParseExact(fileInfo.ReleaseYear, "yyyy", null) : DateTime.MinValue;

                var magnetLinks = detailsDom.QuerySelectorAll("a.btn[href^=\"magnet:?xt\"]");
                foreach (var magnetLink in magnetLinks)
                {
                    var magnet = magnetLink.GetAttribute("href");
                    var release = releaseCommonInfo.Clone() as ReleaseInfo;
                    release.MagnetUri = new Uri(magnet);
                    release.Link = new Uri(magnet);
                    release.DownloadVolumeFactor = 0;
                    release.UploadVolumeFactor = 1;

                    // Extract resolution from file info
                    var resolution = fileInfo.Quality ?? fileInfo.VideoQuality ?? string.Empty;

                    // Format the title
                    release.Title = $"[{Tracker}] {release.Title} {resolution}".Trim();

                    // Additional metadata
                    release.Languages = fileInfo.Audio?.ToList() ?? release.Languages;
                    release.Genres = fileInfo.Genres?.ToList() ?? release.Genres;
                    release.Subs = string.IsNullOrEmpty(fileInfo.Subtitle) ? release.Subs : new[] { fileInfo.Subtitle };
                    release.Size = string.IsNullOrEmpty(fileInfo.Size) ? release.Size : ParseUtil.GetBytes(fileInfo.Size);

                    if (release.Title.IsNotNullOrWhiteSpace())
                        releases.Add(release);
                }
            }

            return releases;
        }

        protected virtual IElement GetTitle(IElement row) => row.QuerySelector("h2[itemprop='headline']");

        protected virtual IHtmlCollection<IElement> GetRows(IHtmlDocument dom)
        {
            return dom.QuerySelectorAll("div.capa_lista");
        }

        protected override INode GetTitleElementOrNull(IElement downloadButton)
        {
            var description = downloadButton.PreviousSibling;
            while (description != null && description.NodeType != NodeType.Text)
            {
                description = description.PreviousSibling;
            }

            return description;
        }
    }
}
