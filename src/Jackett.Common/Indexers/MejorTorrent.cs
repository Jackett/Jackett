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
        public static Uri WebUri = new Uri("http://www.mejortorrent.com/");

        public MejorTorrent(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(name: "MejorTorrent",
                description: "MejorTorrent - Hay veces que un torrent viene mejor! :)",
                link: WebUri.AbsoluteUri,
                caps: new TorznabCapabilities(TorznabCatType.TV,
                                              TorznabCatType.TVSD,
                                              TorznabCatType.TVHD),
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
            var requester = new MejorTorrentRequester(this);
            var tvShowScraper = new TvShowScraper();
            var seasonScraper = new SeasonScraper();
            var downloadScraper = new DownloadScraper();
            var tvShowPerformer = new TvShowPerformer(requester, tvShowScraper, seasonScraper, downloadScraper);
            var releases = await tvShowPerformer.PerformQuery(query);
            return releases;
        }

        // IF THERE IS NOT QUERY AT ALL IT IS LOOKING FOR NEW RELEASES
        // THIS IS USED TOO IN ORDER TO CHECK ALIVE
        // bool rssMode = string.IsNullOrEmpty(query.SanitizedSearchTerm);

        interface IScraper<T>
        {
            T Extract(IHtmlDocument html);
        }

        class RssReleases : IScraper<IEnumerable<ReleaseInfo>>
        {
            public IEnumerable<ReleaseInfo> Extract(IHtmlDocument html)
            {
                var tvSelector = "tr > td > a[href*=\"/serie\"]";
                var newTvShowsElements = html.QuerySelectorAll(tvSelector);
                var newTvShows = new List<ReleaseInfo>();
                foreach (var n in newTvShowsElements)
                {
                    newTvShows.Add(new ReleaseInfo
                    {
                        Title = "By the moment..."
                    });
                }
                throw new NotImplementedException();
            }
        }

        class TvShowScraper : IScraper<IEnumerable<Season>>
        {
            public IEnumerable<Season> Extract(IHtmlDocument html)
            {
                var tvSelector = "a[href*=\"/serie-\"]";
                var seasonsElements = html.QuerySelectorAll(tvSelector).Select(e => e.ParentElement);
                
                var newTvShows = new List<Season>();

                // EXAMPLES:
                // Stranger Things - 1ª Temporada (HDTV)
                // Stranger Things - 1ª Temporada [720p] (HDTV-720p)
                var regex = new Regex(@"(.+) - ([0-9]+).*\((.*)\)");
                foreach (var seasonElement in seasonsElements)
                {
                    var link = seasonElement.QuerySelector("a[href*=\"/serie-\"]").Attributes["href"].Value;
                    var info = seasonElement.TextContent; // Stranger Things - 1 ...
                    var searchMatch = regex.Match(info);
                    if (!searchMatch.Success)
                    {
                        continue;
                    }
                    int seasonNumber;
                    if (!Int32.TryParse(searchMatch.Groups[2].Value, out seasonNumber))
                    {
                        seasonNumber = 0;
                    }
                    var season = new Season
                    {
                        Title = searchMatch.Groups[1].Value,
                        Number = seasonNumber,
                        Type = searchMatch.Groups[3].Value,
                        Link = new Uri(WebUri, link)
                    };
                    var originalTitleRegex = new Regex(@".+\((.+)\)");
                    var originalTitleMath = originalTitleRegex.Match(season.Title);
                    if (originalTitleMath.Success)
                    {
                        season.Title = originalTitleMath.Groups[1].Value;
                    }
                    newTvShows.Add(season);
                }
                return newTvShows;
            }
        }

        class SeasonScraper : IScraper<IEnumerable<MejorTorrentReleaseInfo>>
        {
            public IEnumerable<MejorTorrentReleaseInfo> Extract(IHtmlDocument html)
            {
                var episodesLinks = html.QuerySelectorAll("a[href*=\"/serie-episodio-descargar-torrent\"]")
                    .Select(e => e.Attributes["href"].Value)
                    .Select(link => ExtractInfo(link));
                return episodesLinks;
            }

            private MejorTorrentReleaseInfo ExtractInfo(String link)
            {
                // LINK FORMAT: /serie-episodio-descargar-torrent-${ID}-${TITLE}-${SEASON_NUMBER}x${EPISODE_NUMBER}.html
                var regex = new Regex(@"\/serie-episodio-descargar-torrent-([0-9]+)-(.*)-([0-9]+)x([0-9]+)\.html");
                var linkMatch = regex.Match(link);

                if (!linkMatch.Success)
                {
                    return null;
                }
                var e = new MejorTorrentReleaseInfo();
                e.MejorTorrentID = linkMatch.Groups[1].Value;
                e.Title = linkMatch.Groups[2].Value;
                e.Season = Int32.Parse(linkMatch.Groups[3].Value);
                e.EpisodeNumber = Int32.Parse(linkMatch.Groups[4].Value);
                return e;
            }
        }

        class DownloadScraper : IScraper<IEnumerable<Uri>>
        {
            public IEnumerable<Uri> Extract(IHtmlDocument html)
            {
                return html.QuerySelectorAll("a[href*=\".torrent\"]")
                    .Select(e => e.Attributes["href"].Value)
                    .Select(link => new Uri(WebUri, link));
            }
        }

        class Season
        {
            public String Title;
            public int Number;
            public Uri Link;
            public TorznabCategory Category; // HDTV or HDTV-720
            private string _type;
            public string Type
            {
                get { return _type; }
                set
                {
                    switch(value)
                    {
                        case "HDTV":
                            Category = TorznabCatType.TVSD;
                            break;
                        case "HDTV-720p":
                            Category = TorznabCatType.TVHD;
                            break;
                        default:
                            Category = TorznabCatType.TV;
                            break;
                    }
                    _type = value;
                }
            }
        }

        class MejorTorrentReleaseInfo : ReleaseInfo
        {
            public string MejorTorrentID;
            public int Season;
            public int EpisodeNumber;
            public string CategoryText;

            public MejorTorrentReleaseInfo()
            {
                this.Description = "This is a short description of the chapter.";
                this.Grabs = 5;
                this.Files = 1;
                this.PublishDate = new DateTime();
                this.Peers = 10;
                this.Seeders = 10;
                this.Size = ReleaseInfo.BytesFromGB(1);
            }
        }

        interface IRequester
        {
            Task<IHtmlDocument> MakeRequest(
                Uri uri,
                RequestType method = RequestType.GET,
                IEnumerable<KeyValuePair<string, string>> data = null,
                Dictionary<string, string> headers = null);
        }

        class MejorTorrentRequester : IRequester
        {
            private MejorTorrent mt;

            public MejorTorrentRequester(MejorTorrent mt)
            {
                this.mt = mt;
            }

            public async Task<IHtmlDocument> MakeRequest(
                Uri uri,
                RequestType method = RequestType.GET,
                IEnumerable<KeyValuePair<string, string>> data = null,
                Dictionary<string, string> headers = null)
            {
                var result = await mt.RequestBytesWithCookies(uri.AbsoluteUri, null, method, null, data, headers);
                var SearchResultParser = new HtmlParser();
                var doc = SearchResultParser.Parse(Encoding.UTF8.GetString(result.Content));
                return doc;
            }
        }

        interface IPerformer
        {
            Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query);
        }

        class TvShowPerformer : IPerformer
        {
            private IRequester requester;
            private IScraper<IEnumerable<Season>> tvShowScraper;
            private IScraper<IEnumerable<MejorTorrentReleaseInfo>> seasonScraper;
            private IScraper<IEnumerable<Uri>> downloadScraper;

            public TvShowPerformer(
                IRequester requester,
                IScraper<IEnumerable<Season>> tvShowScraper,
                IScraper<IEnumerable<MejorTorrentReleaseInfo>> seasonScraper,
                IScraper<IEnumerable<Uri>> downloadScraper)
            {
                this.requester = requester;
                this.tvShowScraper = tvShowScraper;
                this.seasonScraper = seasonScraper;
                this.downloadScraper = downloadScraper;
            }

            public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
            {
                var seasonHtml = await requester.MakeRequest(new Uri(WebUri, "secciones.php?sec=buscador&valor=" + query.SanitizedSearchTerm));

                // TEMP
                if (query.SearchTerm == null || query.SearchTerm == "")
                {
                    seasonHtml = await requester.MakeRequest(new Uri(WebUri, "secciones.php?sec=buscador&valor=stranger things"));
                }
                // TEMP
                var seasons = tvShowScraper.Extract(seasonHtml);
                seasons.Where(season => new List<int>(query.Categories).Contains(season.Category.ID));
                var episodesHtmlTasks = new Dictionary<Season, Task<IHtmlDocument>>();
                seasons.ToList().ForEach(season =>
                {
                    episodesHtmlTasks.Add(season, requester.MakeRequest(new Uri(WebUri, season.Link)));
                });
                var episodesHtml = await Task.WhenAll(episodesHtmlTasks.Values);
                var episodes = episodesHtmlTasks.SelectMany(seasonAndHtml =>
                {
                    var season = seasonAndHtml.Key;
                    var html = seasonAndHtml.Value.Result;
                    var eps = seasonScraper.Extract(html);
                    return eps.ToList().Select(e =>
                    {
                        e.CategoryText = season.Type;
                        e.Category = new List<int>{ season.Category.ID };
                        return e;
                    });
                }).ToList();
                var downloadHtmlTasks = new List<Task<IHtmlDocument>>();
                var formData = new List<KeyValuePair<string, string>>();
                int index = 1;
                episodes.ForEach(episode =>
                {
                    var episodeID = new KeyValuePair<string, string>("episodios[" + index + "]", episode.MejorTorrentID);
                    formData.Add(episodeID);
                    index++;
                });
                formData.Add(new KeyValuePair<string, string>("total_capis", index.ToString()));
                formData.Add(new KeyValuePair<string, string>("tabla", "series"));
                var downloadHtml = await requester.MakeRequest(new Uri(WebUri, "secciones.php?sec=descargas&ap=contar_varios"), RequestType.POST, formData);
                var downloads = downloadScraper.Extract(downloadHtml).ToList();

                
                for (var i = 0; i < downloads.Count; i++)
                {
                    var e = episodes.ElementAt(i);
                    episodes.ElementAt(i).Link = downloads.ElementAt(i);
                    episodes.ElementAt(i).Title = seasons.First().Title + " S" + e.Season.ToString("00") + "E" + e.EpisodeNumber.ToString("00") + " " + e.CategoryText + " - Spanish";
                }

                // TEMP //
                int episodeNumber;
                var isNumber = Int32.TryParse(query.Episode, out episodeNumber);
                var filteredEpisodes = episodes.Where(e => e.Season == query.Season);
                if (isNumber)
                {
                    filteredEpisodes = filteredEpisodes.Where(e => e.EpisodeNumber == episodeNumber);
                }
                if (filteredEpisodes.Count() == 0)
                {
                    return episodes;
                }
                // TEMP //
                return filteredEpisodes;
            }

        }

    }
}
