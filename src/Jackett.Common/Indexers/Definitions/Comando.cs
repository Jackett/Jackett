using System;
using System.Collections.Generic;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers.Definitions
{
    public class Comando : PublicBrazilianIndexerBase
    {
        public override string Id => "comandola";
        public override string Name => "Comando.La";
        public override string SiteLink { get; protected set; } = "https://comando.la/";

        public Comando(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
                       ICacheService cs) : base(configService, wc, l, ps, cs)
        {
        }

        public override IParseIndexerResponse GetParser() => new ComandoParser(webclient, Name);
    }

    public class ComandoParser : PublicBrazilianParser
    {
        private readonly WebClient _webclient;

        public ComandoParser(WebClient webclient, string name) : base(name)
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
                var releaseCommonInfo = new ReleaseInfo
                {
                    Title = row.QuerySelector("div.title > a")?.TextContent.Trim(),
                    Genres = row.ExtractGenres(),
                    Subs = row.ExtractSubtitles(),
                    Size = row.ExtractSize(),
                    Languages = row.ExtractLanguages(),
                    Details = detailUrl,
                    Guid = detailUrl,
                    Category = row.ExtractCategory(),
                    PublishDate = row.ExtractReleaseDate()
                };

                var detailsResponse = _webclient.GetResultAsync(new WebRequest(detailUrl.ToString())).Result;
                var detailsDom = detailsParser.ParseDocument(detailsResponse.ContentString);
                var magnetLink = detailsDom.QuerySelector("a[href^='magnet:?xt=urn:btih:']")?.GetAttribute("href");
                if (magnetLink != null)
                {
                    releaseCommonInfo.MagnetUri = new Uri(magnetLink);
                    releases.Add(releaseCommonInfo);
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
