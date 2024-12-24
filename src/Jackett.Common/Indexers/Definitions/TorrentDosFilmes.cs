using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Jackett.Common.Extensions;
using Jackett.Common.Indexers.Definitions.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;
using WebRequest = Jackett.Common.Utils.Clients.WebRequest;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class TorrentDosFilmes : PublicBrazilianIndexerBase
    {
        public override string Id => "torrentdosfilmes";
        public override string Name => "TorrentDosFilmes";
        public override string SiteLink { get; protected set; } = "https://torrentsdosfilmes.to/";

        public override string[] AlternativeSiteLinks { get; protected set; } = {
            "https://torrentsdosfilmes.to/",
            "https://ComandoFilmes.xyz/"
        };

        public override string[] LegacySiteLinks { get; protected set; } = {
            "https://torrentdosfilmes.site/"
        };

        public TorrentDosFilmes(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
                                ICacheService cs) : base(configService, wc, l, ps, cs)
        {
            configData.AddDynamic(
                "flaresolverr",
                new ConfigurationData.DisplayInfoConfigurationItem("FlareSolverr",
                                                                   "This site may use Cloudflare DDoS Protection, therefore Jackett requires <a href=\"https://github.com/Jackett/Jackett#configuring-flaresolverr\" target=\"_blank\">FlareSolverr</a> to access it."));
        }

        public override IParseIndexerResponse GetParser() => new TorrentDosFilmesParser(webclient);
    }

    public class TorrentDosFilmesParser : PublicBrazilianParser
    {
        private readonly WebClient _webclient;

        public TorrentDosFilmesParser(WebClient webclient)
        {
            _webclient = webclient;
        }

        private Dictionary<string, string> ExtractFileInfo(IDocument detailsDom)
        {
            var fileInfo = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var infoSpans = detailsDom.QuerySelectorAll("span[style*='color: black']");

            foreach (var span in infoSpans)
            {
                var text = span.TextContent.Trim();
                var parts = text.Split(new[] { ':' }, 2);
                if (parts.Length == 2)
                {
                    var key = parts[0].Replace("<em>", "").Replace("</em>", "").Replace("<strong>", "").Replace("</strong>", "").Trim();
                    var value = parts[1].Trim();
                    fileInfo[key] = value;
                }
            }

            return fileInfo;
        }

        public override IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var releases = new List<ReleaseInfo>();

            var parser = new HtmlParser();
            var dom = parser.ParseDocument(indexerResponse.Content);
            var rows = dom.QuerySelectorAll("div.post");

            foreach (var row in rows)
            {
                var detailsParser = new HtmlParser();
                var detailAnchor = row.QuerySelector("div.title a[title]");
                var detailUrl = new Uri(detailAnchor?.GetAttribute("href") ?? string.Empty);
                var title = detailAnchor?.TextContent.Trim() ?? string.Empty;
                var releaseCommonInfo = new ReleaseInfo
                {
                    Title = CleanTitle(title),
                    Details = detailUrl,
                    Guid = detailUrl,
                    PublishDate = row.ExtractReleaseDate(),
                    Seeders = 1
                };

                var detailsPage = _webclient.GetResultAsync(new WebRequest(detailUrl.ToString())).Result;
                var detailsDom = detailsParser.ParseDocument(detailsPage.ContentString);

                var fileInfoDict = ExtractFileInfo(detailsDom);
                var fileInfo = PublicBrazilianIndexerBase.FileInfo.FromDictionary(fileInfoDict);

                foreach (var downloadButton in detailsDom.QuerySelectorAll("a.customButton[href^=\"magnet:\"]"))
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
