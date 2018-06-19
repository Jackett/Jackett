using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;

namespace Jackett.Common.Indexers
{
    class MejorTorrent : BaseWebIndexer
    {
        public MejorTorrent(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(name: "MejorTorrent",
                description: "MejorTorrent - Hay veces que un torrent viene mejor! :)",
                link: "http://www.mejortorrent.com/",
                caps: new TorznabCapabilities(TorznabCatType.TV,
                                              TorznabCatType.TVSD,
                                              TorznabCatType.TVHD,
                                              TorznabCatType.Movies),
                configService: configService,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationData())
        {
            Encoding = Encoding.GetEncoding("windows-1252");
            Language = "es-es";
            Type = "public";

            var voItem = new BoolItem() { Name = "Include original versions in search results", Value = false };
            configData.AddDynamic("IncludeVo", voItem);
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);
            var releases = await PerformQuery(new TorznabQuery());

            await ConfigureIfOK(string.Empty, releases.Count() > 0, () =>
            {
                throw new Exception("Could not find releases from this URL");
            });

            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            return await PerformQuery(query, 0);
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query, int attempts)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();

            Uri siteUri = new Uri("http://www.mejortorrent.com");
            Uri getListUri = new Uri(siteUri, "secciones.php?sec=buscador&valor=homeland");

            var result = await RequestStringWithCookies(getListUri.AbsoluteUri);

            var SearchResultParser = new HtmlParser();
            var doc = SearchResultParser.Parse(result.Content);
            var seriesTr = doc.QuerySelectorAll("tr[height=\"22\"]");
            var seriesTr2 = doc.QuerySelectorAll("tr > td > a[href*=\"/serie\"]");
            var body = doc.QuerySelector("body").TextContent;

            // TESTING
            for (var i = 0; i < 20; i++)
            {
                var r = new ReleaseInfo();
                r.Peers = 1;
                r.Seeders = 1;
                r.Size = 10240;
                r.Description = "Random description";
                r.Category = new List<int>(TorznabCatType.TV.ID);
                r.Title = "Random title " + i;
                // MUST BE IN RSS
                //r.Link = new Uri("http://meloinvento.com");
                r.PublishDate = new DateTime();
                releases.Add(r);
            }

            return releases;
        }

        // IF THERE IS NOT QUERY AT ALL IT IS LOOKING FOR NEW RELEASES
        // THIS IS USED TOO IN ORDER TO CHECK ALIVE
        // bool rssMode = string.IsNullOrEmpty(query.SanitizedSearchTerm);

        //public IEnumerable<ReleaseInfo> Get

        private String tvSelector = "tr > td:has(a[href*=\"/serie\"])";
        private String movieSelector = "tr > td:has(a[href*=\"/peli\"])";

        interface ReleaseParser
        {
            IEnumerable<ReleaseInfo> GetReleases(IHtmlDocument html);
        }

        class RssReleases : ReleaseParser
        {
            IEnumerable<ReleaseInfo> ReleaseParser.GetReleases(IHtmlDocument html)
            {
                var tvSelector = "tr > td > a[href*=\"/serie\"]";
                var newTvShowsElements = html.QuerySelectorAll(tvSelector);
                var newTvShows = new List<ReleaseInfo>();
                foreach(var n in newTvShowsElements)
                {
                    newTvShows.Add(new ReleaseInfo
                    {
                        Title = "By the moment..."
                    });
                }
                throw new NotImplementedException();
            }
        }

        class TvReleases : ReleaseParser
        {
            IEnumerable<ReleaseInfo> ReleaseParser.GetReleases(IHtmlDocument html)
            {
                throw new NotImplementedException();
            }
        }

    }
}
