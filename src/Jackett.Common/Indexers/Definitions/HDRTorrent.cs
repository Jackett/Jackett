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
    public class HDRTorrent : PublicBrazilianIndexerBase
    {
        public override string Id => "hdrtorrent";

        public override string Name => "HDRTorrent";

        public override string SiteLink { get; protected set; } = "https://hdrtorrent.com/";

        public HDRTorrent(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps, ICacheService cs)
            : base(configService, wc, l, ps, cs)
        {
        }

        public override IParseIndexerResponse GetParser() => new HDRTorrentParser(webclient);

        public override IIndexerRequestGenerator GetRequestGenerator() => new SimpleRequestGenerator(SiteLink, searchQueryParamsKey: "index.php?s=");
    }

    public class HDRTorrentParser : PublicBrazilianParser
    {
        private readonly WebClient _webclient;
        protected string Tracker;

        public HDRTorrentParser(WebClient webclient)
        {
            _webclient = webclient;
            Tracker = "HDRTorrent";
        }

        private Dictionary<string, string> ExtractFileInfo(IDocument detailsDom)
        {
            var fileInfo = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var infoSection = detailsDom.QuerySelector("div.infos p");
            if (infoSection == null)
                return fileInfo;

            var lines = infoSection.InnerHtml.Split(new[] { "<br>" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.Contains("<b>") && line.Contains(":"))
                {
                    var parts = line.Split(new[] { ':' }, 2);
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Replace("<b>", "").Replace("</b>", "").Trim();
                        var value = parts[1]
                            .Replace("<b>", "")
                            .Replace("</b>", "")
                            .Replace("<strong>", "")
                            .Replace("</strong>", "")
                            .Trim();

                        if (value.Contains("<"))
                        {
                            var tempDoc = new HtmlParser().ParseDocument(value);
                            value = tempDoc.Body.TextContent.Trim();
                        }

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
            var rows = dom.QuerySelectorAll("div.capa-img");

            foreach (var row in rows)
            {
                var h2Anchor = row.QuerySelector("h2 a");
                if (h2Anchor == null)
                    continue;

                var title = h2Anchor.TextContent.Trim();
                var detailUrlStr = h2Anchor.GetAttribute("href")?.Trim();
                if (string.IsNullOrEmpty(detailUrlStr))
                    continue;

                var detailUrl = new Uri(detailUrlStr);

                var releaseCommonInfo = new ReleaseInfo
                {
                    Title = CleanTitle(title),
                    Details = detailUrl,
                    Guid = detailUrl,
                    Seeders = 1
                };

                var detailsPage = _webclient.GetResultAsync(new WebRequest(detailUrl.ToString())).Result;
                var detailsDom = parser.ParseDocument(detailsPage.ContentString);

                var fileInfoDict = ExtractFileInfo(detailsDom);
                var fileInfo = PublicBrazilianIndexerBase.FileInfo.FromDictionary(fileInfoDict);

                var publishedMeta = detailsDom.QuerySelector("meta[property='article:published_time']")?.GetAttribute("content");
                if (!string.IsNullOrEmpty(publishedMeta) && DateTime.TryParse(publishedMeta, out var parsedDate))
                {
                    releaseCommonInfo.PublishDate = parsedDate;
                }
                else
                {
                    releaseCommonInfo.PublishDate = DateTime.Today;
                }

                var magnetLinks = detailsDom.QuerySelectorAll("a[href^='magnet:?']");
                foreach (var magnetLink in magnetLinks)
                {
                    var magnet = magnetLink.GetAttribute("href");
                    if (string.IsNullOrEmpty(magnet))
                        continue;

                    var release = releaseCommonInfo.Clone() as ReleaseInfo;
                    release.Guid = release.MagnetUri = new Uri(magnet);

                    var parentText = magnetLink.ParentElement?.TextContent?.Trim();
                    if (!string.IsNullOrEmpty(parentText))
                    {
                        parentText = Regex.Replace(parentText, "DOWNLOAD TORRENT", "", RegexOptions.IgnoreCase)
                                          .Replace("DUAL ÁUDIO", "Dual")
                                          .Replace("DUAL AUDIO", "Dual")
                                          .Replace("ÁUDIO", "Audio")
                                          .Replace("AUDIO", "Audio")
                                          .Replace("DUBLADO", "Dubbed")
                                          .Replace("LEGENDADO", "Subbed")
                                          .Replace("MKV", "")
                                          .Replace("MP4", "")
                                          .Replace("MAGNET", "")
                                          .Replace("TORRENT", "")
                                          .Replace("LINK", "")
                                          .Trim();

                        if (!string.IsNullOrEmpty(parentText))
                        {
                            release.Title = $"{release.Title} {parentText}".Trim();
                        }
                    }

                    var resolution = fileInfo.Quality ?? fileInfo.VideoQuality ?? string.Empty;
                    if (!string.IsNullOrEmpty(resolution))
                        release.Title = $"{release.Title} {resolution}".Trim();

                    release.Category = magnetLink.ExtractCategory(release.Title);
                    var size = RowParsingExtensions.GetBytes(fileInfo.Size ?? string.Empty);
                    release.Size = size > 0 ? size : ExtractSizeByResolution(release.Title);

                    release.DownloadVolumeFactor = 0;
                    release.UploadVolumeFactor = 1;

                    release.Languages = fileInfo.Audio?.ToList() ?? release.Languages;
                    release.Genres = fileInfo.Genres?.ToList() ?? release.Genres;
                    release.Subs = string.IsNullOrEmpty(fileInfo.Subtitle) ? release.Subs : new[] { fileInfo.Subtitle };

                    if (release.Title.IsNotNullOrWhiteSpace())
                        releases.Add(release);
                }
            }

            return releases;
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
