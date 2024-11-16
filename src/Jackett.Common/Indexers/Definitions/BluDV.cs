using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AngleSharp.Html.Parser;
using Jackett.Common.Extensions;
using Jackett.Common.Models;
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
        }

        public override IParseIndexerResponse GetParser() => new BluDVParser(SiteLink, webclient);
    }

    public class BluDVParser : IParseIndexerResponse
    {
        private readonly string _siteLink;
        private WebClient _webclient;

        public BluDVParser(string siteLink, WebClient webclient)
        {
            _webclient = webclient;
            _siteLink = siteLink;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var releases = new List<ReleaseInfo>();

            var parser = new HtmlParser();
            var dom = parser.ParseDocument(indexerResponse.Content);
            var rows = dom.QuerySelectorAll("div.post");

            foreach (var row in rows)
            {
                // Get the details page to extract the magnet link
                var detailsParser = new HtmlParser();
                var detailUrl = new Uri(row.QuerySelector("a.more-link")?.GetAttribute("href"));
                var detailTitle = row.QuerySelector("div.title > a")?.TextContent.Trim();
                var releaseCommonInfo = new ReleaseInfo {
                    Genres = row.ExtractGenres(),
                    Category = row.ExtractCategory(),
                    PublishDate = row.ExtractReleaseDate(),
                    Subs = row.ExtractSubtitles(),
                    Size = row.ExtractSize(),
                    Languages = row.ExtractLanguages(),
                    Details = detailUrl,
                    Guid = detailUrl
                };
                var detailsPage = _webclient.GetResultAsync(new WebRequest(detailUrl.ToString())).Result;
                var detailsDom = detailsParser.ParseDocument(detailsPage.ContentString);
                foreach (var downloadButton in detailsDom.QuerySelectorAll("a.customButton[href^=\"magnet:\"]"))
                {
                    var title = downloadButton.ExtractTitleOrDefault(detailTitle);
                    var magnet = downloadButton.ExtractMagnet();
                    var release = releaseCommonInfo.Clone() as ReleaseInfo;
                    release.Title = title;
                    release.Languages =  row.ExtractLanguages();
                    release.Link = release.Guid = release.MagnetUri = magnet;
                    release.DownloadVolumeFactor = 0; // Free
                    release.UploadVolumeFactor = 1;

                    if (release.Title.IsNotNullOrWhiteSpace())
                        releases.Add(release);
                }
            }

            return releases;
        }
    }

}
