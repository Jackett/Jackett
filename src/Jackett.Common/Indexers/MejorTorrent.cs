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
        public static Uri DownloadUri = new Uri(WebUri, "secciones.php?sec=descargas&ap=contar_varios");
        private static Uri SearchUriBase = new Uri(WebUri, "secciones.php");

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
            var rssPerformer = new RssPerformer(requester, tvShowScraper, seasonScraper, downloadScraper);
            if (query.SearchTerm == "" || query.SearchTerm == null)
            {
                return await rssPerformer.PerformQuery(query);
            }
            return await tvShowPerformer.PerformQuery(query);
        }

        // IF THERE IS NOT QUERY AT ALL IT IS LOOKING FOR NEW RELEASES
        // THIS IS USED TOO IN ORDER TO CHECK ALIVE
        // bool rssMode = string.IsNullOrEmpty(query.SanitizedSearchTerm);

        public static Uri CreateSearchUri(string search)
        {
            var finalUri = SearchUriBase.AbsoluteUri;
            finalUri += "?sec=buscador&valor=" + search;
            return new Uri(finalUri);
        }

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
                var episodesLinksHtml = html.QuerySelectorAll("a[href*=\"/serie-episodio-descargar-torrent\"]");
                var episodesTexts = episodesLinksHtml.Select(l => l.TextContent).ToList();
                var episodesLinks = episodesLinksHtml.Select(e => e.Attributes["href"].Value).ToList();
                var dates = episodesLinksHtml
                    .Select(e => e.ParentElement.ParentElement.QuerySelector("div").TextContent)
                    .Select(stringDate => stringDate.Replace("Fecha: ", ""))
                    .Select(stringDate => stringDate.Split('-'))
                    .Select(stringParts => new int[]{ Int32.Parse(stringParts[0]), Int32.Parse(stringParts[1]), Int32.Parse(stringParts[2]) })
                    .Select(intParts => new DateTime(intParts[0], intParts[1], intParts[2]));

                var episodes = episodesLinks.Select(e => new MejorTorrentReleaseInfo()).ToList();

                for (var i = 0; i < episodes.Count(); i++)
                {
                    GuessEpisodes(episodes.ElementAt(i), episodesTexts.ElementAt(i));
                    ExtractLinkInfo(episodes.ElementAt(i), episodesLinks.ElementAt(i));
                    episodes.ElementAt(i).PublishDate = dates.ElementAt(i);
                }

                return episodes;
            }

            private void GuessEpisodes(MejorTorrentReleaseInfo release, string episodeText)
            {
                var seasonEpisodeRegex = new Regex(@"(\d{1,2}).*?(\d{1,2})", RegexOptions.IgnoreCase);
                var matchSeasonEpisode = seasonEpisodeRegex.Match(episodeText);
                if (!matchSeasonEpisode.Success) return;
                release.Season = Int32.Parse(matchSeasonEpisode.Groups[1].Value);
                release.EpisodeNumber = Int32.Parse(matchSeasonEpisode.Groups[2].Value);

                char[] textArray = episodeText.ToCharArray();
                Array.Reverse(textArray);
                var reversedText = new string(textArray);
                var finalEpisodeRegex = new Regex(@"(\d{1,2})");
                var matchFinalEpisode = finalEpisodeRegex.Match(reversedText);
                if (!matchFinalEpisode.Success) return;
                var finalEpisodeArray = matchFinalEpisode.Groups[1].Value.ToCharArray();
                Array.Reverse(finalEpisodeArray);
                var finalEpisode = Int32.Parse(new string(finalEpisodeArray));
                if (finalEpisode > release.EpisodeNumber)
                {
                    release.Files = (finalEpisode + 1) - release.EpisodeNumber;
                    release.Size = release.Size * release.Files;
                }
            }

            private void ExtractLinkInfo(MejorTorrentReleaseInfo release, String link)
            {
                // LINK FORMAT: /serie-episodio-descargar-torrent-${ID}-${TITLE}-${SEASON_NUMBER}x${EPISODE_NUMBER}[range].html
                var regex = new Regex(@"\/serie-episodio-descargar-torrent-(\d+)-(.*)-(\d{1,2}).*(\d{1,2}).*\.html", RegexOptions.IgnoreCase);
                var linkMatch = regex.Match(link);

                if (!linkMatch.Success)
                {
                    return;
                }
                release.MejorTorrentID = linkMatch.Groups[1].Value;
                release.Title = linkMatch.Groups[2].Value;
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
                            _type = "SDTV";
                            break;
                        case "HDTV-720p":
                            Category = TorznabCatType.TVHD;
                            _type = "HDTV-720p";
                            break;
                        default:
                            Category = TorznabCatType.TV;
                            _type = "HDTV-720p";
                            break;
                    }
                }
            }
        }

        class MejorTorrentReleaseInfo : ReleaseInfo
        {
            public string MejorTorrentID;
            public int Season;
            public int EpisodeNumber;
            public string CategoryText;
            public int FinalEpisodeNumber { get { return (int)(EpisodeNumber + Files - 1); } }

            public MejorTorrentReleaseInfo()
            {
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

        class MejorTorrentDownloadRequesterDecorator
        {
            private IRequester r;

            public MejorTorrentDownloadRequesterDecorator(IRequester r)
            {
                this.r = r;
            }

            public async Task<IHtmlDocument> MakeRequest(IEnumerable<string> ids)
            {
                var downloadHtmlTasks = new List<Task<IHtmlDocument>>();
                var formData = new List<KeyValuePair<string, string>>();
                int index = 1;
                ids.ToList().ForEach(id =>
                {
                    var episodeID = new KeyValuePair<string, string>("episodios[" + index + "]", id);
                    formData.Add(episodeID);
                    index++;
                });
                formData.Add(new KeyValuePair<string, string>("total_capis", index.ToString()));
                formData.Add(new KeyValuePair<string, string>("tabla", "series"));
                //var downloadHtml = await requester.MakeRequest(new Uri(WebUri, "secciones.php?sec=descargas&ap=contar_varios"), RequestType.POST, formData);

                return await r.MakeRequest(DownloadUri, RequestType.POST, formData);
            }
        }

        interface IPerformer
        {
            Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query);
        }

        class RssPerformer : IPerformer
        {
            private IRequester requester;
            private IScraper<IEnumerable<Season>> tvShowScraper;
            private IScraper<IEnumerable<MejorTorrentReleaseInfo>> seasonScraper;
            private IScraper<IEnumerable<Uri>> downloadScraper;

            public RssPerformer(
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

            public Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
            {
                query.SearchTerm = "stranger things";
                return new TvShowPerformer(requester, tvShowScraper, seasonScraper, downloadScraper).PerformQuery(query);
            }
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
                query = FixQuery(query);
                var seasons = await GetSeasons(query);
                var episodes = await GetEpisodes(query, seasons);
                await AddDownloadLinks(seasons.First().Title, episodes);
                return episodes;
            }

            private TorznabQuery FixQuery(TorznabQuery query)
            {
                var seasonRegex = new Regex(@".*?(s\d{1,2})", RegexOptions.IgnoreCase);
                var episodeRegex = new Regex(@".*?(e\d{1,2})", RegexOptions.IgnoreCase);
                var seasonMatch = seasonRegex.Match(query.SearchTerm);
                var episodeMatch = episodeRegex.Match(query.SearchTerm);
                if (seasonMatch.Success)
                {
                    query.Season = Int32.Parse(seasonMatch.Groups[1].Value.Substring(1));
                    query.SearchTerm = query.SearchTerm.Replace(seasonMatch.Groups[1].Value, "");
                }
                if (episodeMatch.Success)
                {
                    query.Episode = episodeMatch.Groups[1].Value.Substring(1);
                    query.SearchTerm = query.SearchTerm.Replace(episodeMatch.Groups[1].Value, "");
                }
                query.SearchTerm = query.SearchTerm.Trim();
                return query;
            }

            private async Task<List<Season>> GetSeasons(TorznabQuery query)
            {
                var seasonHtml = await requester.MakeRequest(CreateSearchUri(query.SanitizedSearchTerm));
                var seasons = tvShowScraper.Extract(seasonHtml);
                seasons = seasons.Where(s => s.Number == query.Season);
                if (query.Categories.Count() != 0)
                {
                    seasons = seasons.Where(s => new List<int>(query.Categories).Contains(s.Category.ID));
                }
                return seasons.ToList();
            }

            private async Task<List<MejorTorrentReleaseInfo>> GetEpisodes(TorznabQuery query, IEnumerable<Season> seasons)
            {
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
                        e.Category = new List<int> { season.Category.ID };
                        return e;
                    });
                });
                var episodeNumber = Int32.Parse(query.Episode);
                episodes = episodes.Where(e => e.EpisodeNumber <= episodeNumber && episodeNumber <= e.FinalEpisodeNumber);
                return episodes.ToList();
            }

            private async Task AddDownloadLinks(string tvTitle, IEnumerable<MejorTorrentReleaseInfo> episodes)
            {
                var downloadRequester = new MejorTorrentDownloadRequesterDecorator(requester);
                var downloadHtml = await downloadRequester.MakeRequest(episodes.Select(e => e.MejorTorrentID));
                var downloads = downloadScraper.Extract(downloadHtml).ToList();

                for (var i = 0; i < downloads.Count; i++)
                {
                    var e = episodes.ElementAt(i);
                    episodes.ElementAt(i).Link = downloads.ElementAt(i);
                    episodes.ElementAt(i).Guid = downloads.ElementAt(i);
                    var title = tvTitle.Trim().Replace(' ', '.');
                    title = char.ToUpper(title[0]) + title.Substring(1);
                    var seasonAndEpisode = "S" + e.Season.ToString("00") + "E" + e.EpisodeNumber.ToString("00");
                    if (e.Files > 1)
                    {
                        seasonAndEpisode += "-" + e.FinalEpisodeNumber.ToString("00");
                    }
                    episodes.ElementAt(i).Title = String.Join(".", new List<string>() { title, seasonAndEpisode, e.CategoryText, "Spanish" });
                }
            }

        }

    }
}
