using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Jackett.Common.Extensions;
using Jackett.Common.Indexers.Definitions.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;
using WebRequest = Jackett.Common.Utils.Clients.WebRequest;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class BluDV : PublicBrazilianIndexerBase
    {
        public override string Id => "bludv";
        public override string Name => "BluDV";
        public override string SiteLink { get; protected set; } = "https://bludv.xyz/";

        public BluDV(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps, ICacheService cs)
            : base(configService, wc, l, ps, cs)
        {
            configData.AddDynamic(
                "flaresolverr",
                new ConfigurationData.DisplayInfoConfigurationItem("FlareSolverr",
                                                                   "This site may use Cloudflare DDoS Protection, therefore Jackett requires <a href=\"https://github.com/Jackett/Jackett#configuring-flaresolverr\" target=\"_blank\">FlareSolverr</a> to access it."));
        }

        public override IParseIndexerResponse GetParser() => new BluDVParser(webclient);
    }

    public class BluDVParser : PublicBrazilianParser
    {
        private readonly WebClient _webclient;

        public BluDVParser(WebClient webclient)
        {
            _webclient = webclient;
        }

        public override IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var releases = new List<ReleaseInfo>();

            var parser = new HtmlParser();
            var dom = parser.ParseDocument(indexerResponse.Content);
            var rows = dom.QuerySelectorAll("div.post");

            foreach (var row in rows)
            {
                // Get the details page to extract the magnet link
                var detailsParser = new HtmlParser();
                var detailAnchor = row.QuerySelector("a.more-link");
                var detailUrl = new Uri(detailAnchor?.GetAttribute("href") ?? string.Empty);
                var title = row.QuerySelector("div.title > a")?.TextContent.Trim();
                var releaseCommonInfo = new ReleaseInfo
                {
                    Title = CleanTitle(title),
                    Genres = row.ExtractGenres(),
                    Subs = row.ExtractSubtitles(),
                    Size = row.ExtractSize(),
                    Languages = row.ExtractLanguages(),
                    Details = detailUrl,
                    Guid = detailUrl,
                    PublishDate = row.ExtractReleaseDate(),
                    Seeders = 1
                };
                var detailsPage = _webclient.GetResultAsync(new WebRequest(detailUrl.ToString())).Result;
                var detailsDom = detailsParser.ParseDocument(detailsPage.ContentString);
                foreach (var downloadButton in detailsDom.QuerySelectorAll("a.customButton[href^=\"magnet:\"]"))
                {
                    var magnet = downloadButton.ExtractMagnet();
                    var release = releaseCommonInfo.Clone() as ReleaseInfo;
                    release.Title = ExtractTitleOrDefault(downloadButton, release.Title);
                    release.Category = downloadButton.ExtractCategory(release.Title);
                    release.Size = release.Size > 0 ? release.Size : ExtractSizeByResolution(release.Title);
                    release.Languages = row.ExtractLanguages();
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
            while (description != null && NotSpanTag(description))
            {
                description = description.PreviousSibling;
            }

            return description;
        }
    }


}
