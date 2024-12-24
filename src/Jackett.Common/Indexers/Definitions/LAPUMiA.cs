using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Jackett.Common.Extensions;
using Jackett.Common.Indexers.Definitions.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using NLog;
using static System.Linq.Enumerable;
using WebClient = Jackett.Common.Utils.Clients.WebClient;
using WebRequest = Jackett.Common.Utils.Clients.WebRequest;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class LAPUMiA : PublicBrazilianIndexerBase
    {
        public override string Id => "lapumia";
        public override string Name => "LAPUMiA";
        public override string SiteLink { get; protected set; } = "https://lapumia.net/";

        public LAPUMiA(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
                       ICacheService cs) : base(configService: configService, wc, l, ps, cs)
        {
        }

        public override IParseIndexerResponse GetParser() => new LAPUMiAParser(webclient);
    }

    public class LAPUMiAParser : PublicBrazilianParser
    {
        private WebClient _webclient;

        public LAPUMiAParser(WebClient webclient)
        {
            _webclient = webclient;
        }

        private Dictionary<string, string> ExtractFileInfo(IDocument detailsDom)
        {
            var fileInfo = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var infoItems = detailsDom.QuerySelectorAll("div.info li");
            foreach (var item in infoItems)
            {
                var text = item.TextContent.Trim();
                var parts = text.Split(
                    new[]
                    {
                        ':'
                    }, 2);
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
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
            var rows = dom.QuerySelectorAll("div.item");
            foreach (var row in rows)
            {
                // Get the details page to extract the magnet link
                var detailsParser = new HtmlParser();
                var detailAnchor = row.QuerySelector("a[title]");
                var detailUrl = new Uri(detailAnchor?.GetAttribute("href"));
                var title = detailAnchor.GetAttribute("title");
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
                foreach (var downloadButton in detailsDom.QuerySelectorAll("ul.buttons a[href^=\"magnet:?xt\"]"))
                {
                    var release = releaseCommonInfo.Clone() as ReleaseInfo;
                    release.Title = ExtractTitleOrDefault(downloadButton, release.Title);
                    release.Category = downloadButton.ExtractCategory(release.Title);
                    var fileInfoDict = ExtractFileInfo(detailsDom);
                    var fileInfo = PublicBrazilianIndexerBase.FileInfo.FromDictionary(fileInfoDict);
                    release.Languages = fileInfo.Audio?.ToList() ?? release.Languages;
                    release.Genres = fileInfo.Genres?.ToList() ?? release.Genres;
                    release.Subs = string.IsNullOrEmpty(fileInfo.Subtitle)
                        ? release.Subs
                        : new[]
                        {
                            fileInfo.Subtitle
                        };
                    var size = RowParsingExtensions.GetBytes(fileInfo.Size ?? string.Empty);
                    release.Size = size > 0 ? size : ExtractSizeByResolution(release.Title);
                    var magnet = downloadButton.ExtractMagnet();
                    release.Guid = release.MagnetUri = magnet;
                    release.DownloadVolumeFactor = 0; // Free
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
            while (description != null && description.NodeType != NodeType.Text)
            {
                description = description.PreviousSibling;
            }

            return description;
        }
    }
}
