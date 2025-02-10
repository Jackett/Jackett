using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Jackett.Common.Extensions;
using Jackett.Common.Indexers.Definitions.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class TorrentsMegaFilmes : PublicBrazilianIndexerBase
    {
        public override string Id => "torrentsmegafilmes";
        public override string Name => "Torrents Mega Filmes";
        public override string SiteLink { get; protected set; } = "https://torrentsmegafilmes.top/";

        public TorrentsMegaFilmes(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps, ICacheService cs) : base(configService, wc, l, ps, cs)
        {
        }

        public override IParseIndexerResponse GetParser() => new TorrentsMegaFilmesParser(webclient);
    }

    public class TorrentsMegaFilmesParser : PublicBrazilianParser
    {
        private readonly WebClient _webclient;
        public TorrentsMegaFilmesParser(WebClient webclient)
        {
            _webclient = webclient;
        }

        public override IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var releases = new List<ReleaseInfo>();

            var parser = new HtmlParser();
            var dom = parser.ParseDocument(indexerResponse.Content);
            var detailAnchors = dom.QuerySelectorAll("div.title > a");
            foreach (var detailAnchor in detailAnchors)
            {
                var detailUrl = new Uri(detailAnchor?.GetAttribute("href") ?? string.Empty);
                var title = detailAnchor?.TextContent.Trim();
                var detailsPage = _webclient.GetResultAsync(new WebRequest(detailUrl.ToString())).Result;
                var detailsDom = parser.ParseDocument(detailsPage.ContentString);
                var detailsInfo = detailsDom.QuerySelector("div.info");
                var releaseCommonInfo = new ReleaseInfo
                {
                    Title = CleanTitle(title),
                    Genres = detailsInfo.ExtractGenres(),
                    Subs = detailsInfo.ExtractSubtitles(),
                    Size = detailsInfo.ExtractSize(),
                    Languages = detailsInfo.ExtractLanguages(),
                    Details = detailUrl,
                    Guid = detailUrl,
                    PublishDate = detailsInfo.ExtractReleaseDate(),
                    Seeders = 1
                };
                foreach (var downloadButton in detailsDom.QuerySelectorAll("ul.buttons a[href]"))
                {
                    var magnet = downloadButton.ExtractMagnet();
                    var release = releaseCommonInfo.Clone() as ReleaseInfo;
                    release.Guid = release.MagnetUri = magnet;
                    release.Title = ExtractTitleOrDefault(downloadButton, release.Title + " " + downloadButton.TextContent);
                    release.Category = downloadButton.ExtractCategory(release.Title);
                    release.DownloadVolumeFactor = 0; // Free
                    release.UploadVolumeFactor = 1;

                    if (release.Title.IsNotNullOrWhiteSpace())
                    {
                        releases.Add(release);
                    }
                }
            }
            return releases;
        }

        /**
         * Return null to concatenate titles rather than ranking, i.e., button only contains resolution, season, and episode.
         */
        protected override INode GetTitleElementOrNull(IElement downloadButton) => null;
    }
}
