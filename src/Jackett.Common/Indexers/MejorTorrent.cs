using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using Jackett.Common.Helpers;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    class MejorTorrent : BaseWebIndexer
    {
        public static Uri WebUri = new Uri("http://www.mejortorrentt.org/");
        public static Uri DownloadUri = new Uri(WebUri, "secciones.php?sec=descargas&ap=contar_varios");
        private static Uri SearchUriBase = new Uri(WebUri, "secciones.php");
        public static Uri NewTorrentsUri = new Uri(WebUri, "secciones.php?sec=ultimos_torrents");
        public static Encoding MEEncoding = Encoding.GetEncoding("utf-8");

        public override string[] LegacySiteLinks { get; protected set; } = new string[] {
            "http://www.mejortorrent.org/",
            "http://www.mejortorrent.tv/",
            "http://www.mejortorrentt.com/",
            "https://www.mejortorrentt.org/",
        };

        public MejorTorrent(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(name: "MejorTorrent",
                description: "MejorTorrent - Hay veces que un torrent viene mejor! :)",
                link: WebUri.AbsoluteUri,
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
            Encoding = MEEncoding;
            Language = "es-es";
            Type = "public";
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);

            WebUri = new Uri(configData.SiteLink.Value);
            DownloadUri = new Uri(WebUri, "secciones.php?sec=descargas&ap=contar_varios");
            SearchUriBase = new Uri(WebUri, "secciones.php");
            NewTorrentsUri = new Uri(WebUri, "secciones.php?sec=ultimos_torrents");

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
            query = query.Clone();

            var originalSearchTerm = query.SearchTerm;
            if (query.SearchTerm == null)
            {
                query.SearchTerm = "";
            }
            query.SearchTerm = query.SearchTerm.Replace("'", "");

            var requester = new MejorTorrentRequester(this);
            var tvShowScraper = new TvShowScraper();
            var seasonScraper = new SeasonScraper();
            var downloadScraper = new DownloadScraper();
            var rssScraper = new RssScraper();
            var downloadGenerator = new DownloadGenerator(requester, downloadScraper);
            var tvShowPerformer = new TvShowPerformer(requester, tvShowScraper, seasonScraper, downloadGenerator);
            var rssPerformer = new RssPerformer(requester, rssScraper, seasonScraper, downloadGenerator);
            var movieSearchScraper = new MovieSearchScraper();
            var movieInfoScraper = new MovieInfoScraper();
            var movieDownloadScraper = new MovieDownloadScraper();
            var moviePerformer = new MoviePerformer(requester, movieSearchScraper, movieInfoScraper, movieDownloadScraper);

            var releases = new List<ReleaseInfo>();

            if (string.IsNullOrEmpty(query.SanitizedSearchTerm))
            {
                releases = (await rssPerformer.PerformQuery(query)).ToList();
                var movie = releases.First();
                movie.Category.Add(TorznabCatType.Movies.ID);
                releases.ToList().Add(movie);
                if (releases.Count() == 0)
                {
                    releases = (await AliveCheck(tvShowPerformer)).ToList();
                }
                return releases;
            }

            if (query.Categories.Contains(TorznabCatType.Movies.ID) || query.Categories.Count() == 0)
            {
                releases.AddRange(await moviePerformer.PerformQuery(query));
            }
            if (query.Categories.Contains(TorznabCatType.TV.ID) || 
                query.Categories.Contains(TorznabCatType.TVSD.ID) ||
                query.Categories.Contains(TorznabCatType.TVHD.ID) ||
                query.Categories.Count() == 0)
            {
                releases.AddRange(await tvShowPerformer.PerformQuery(query));
            }

            query.SearchTerm = originalSearchTerm;
            return releases;
        }

        private async Task<IEnumerable<ReleaseInfo>> AliveCheck(TvShowPerformer tvShowPerformer)
        {
            IEnumerable<ReleaseInfo> releases = new List<ReleaseInfo>();
            var tests = new Queue<string>(new[] { "stranger things", "westworld", "friends" });
            while (releases.Count() == 0 && tests.Count > 0)
            {
                var query = new TorznabQuery();
                query.SearchTerm = tests.Dequeue();
                releases = await tvShowPerformer.PerformQuery(query);
            }
            return releases;
        }

        public static Uri CreateSearchUri(string search)
        {
            var finalUri = SearchUriBase.AbsoluteUri;
            finalUri += "?sec=buscador&valor=" + WebUtilityHelpers.UrlEncode(search, MEEncoding);
            return new Uri(finalUri);
        }

        interface IScraper<T>
        {
            T Extract(IHtmlDocument html);
        }

        class RssScraper : IScraper<IEnumerable<KeyValuePair<MTReleaseInfo, Uri>>>
        {
            private readonly string LinkQuerySelector = "a[href*=\"/serie\"]";

            public IEnumerable<KeyValuePair<MTReleaseInfo, Uri>> Extract(IHtmlDocument html)
            {
                var episodes = GetNewEpisodesScratch(html);
                var links = GetLinks(html);
                var results = new List<KeyValuePair<MTReleaseInfo, Uri>>();
                for (var i = 0; i < episodes.Count(); i++)
                {
                    results.Add(new KeyValuePair<MTReleaseInfo, Uri>(episodes.ElementAt(i), links.ElementAt(i)));
                }
                return results;
            }

            private List<MTReleaseInfo> GetNewEpisodesScratch(IHtmlDocument html)
            {
                var tvShowsElements = html.QuerySelectorAll(LinkQuerySelector);
                var seasonLinks = tvShowsElements.Select(e => e.Attributes["href"].Value);
                var dates = GetDates(html);
                var titles = GetTitles(html);
                var qualities = GetQualities(html);
                var seasonsFirstEpisodesAndLast = GetSeasonsFirstEpisodesAndLast(html);

                var episodes = new List<MTReleaseInfo>();
                for(var i = 0; i < tvShowsElements.Count(); i++)
                {
                    var e = new MTReleaseInfo();
                    e.TitleOriginal = titles.ElementAt(i);
                    e.PublishDate = dates.ElementAt(i);
                    e.CategoryText = qualities.ElementAt(i);
                    var sfeal = seasonsFirstEpisodesAndLast.ElementAt(i);
                    e.Season = sfeal.Key;
                    e.EpisodeNumber = sfeal.Value.Key;
                    if (sfeal.Value.Value != null && sfeal.Value.Value > sfeal.Value.Key)
                    {
                        e.Files = sfeal.Value.Value - sfeal.Value.Key + 1;
                    }
                    else
                    {
                        e.Files = 1;
                    }
                    episodes.Add(e);
                }
                return episodes;
            }

            private List<Uri> GetLinks(IHtmlDocument html)
            {
                return html.QuerySelectorAll(LinkQuerySelector)
                    .Select(e => e.Attributes["href"].Value)
                    .Select(relativeLink => new Uri(WebUri, relativeLink))
                    .ToList();
            }

            private List<DateTime> GetDates(IHtmlDocument html)
            {
                return html.QuerySelectorAll(LinkQuerySelector)
                    .Select(e => e.PreviousElementSibling.TextContent)
                    .Select(dateString => dateString.Split('-'))
                    .Select(parts => new int[] { Int32.Parse(parts[0]), Int32.Parse(parts[1]), Int32.Parse(parts[2]) })
                    .Select(intParts => new DateTime(intParts[0], intParts[1], intParts[2]))
                    .ToList();
            }

            private List<string> GetTitles(IHtmlDocument html)
            {
                var texts = LinkTexts(html);
                var completeTitles = texts.Select(text => text.Substring(0, text.IndexOf('-') - 1));
                var regex = new Regex(@".+\((.+)\)");
                var finalTitles = completeTitles.Select(title =>
                {
                    var match = regex.Match(title);
                    if (!match.Success) return title;
                    return match.Groups[1].Value;
                });
                return finalTitles.ToList();
            }

            private List<string> GetQualities(IHtmlDocument html)
            {
                var texts = LinkTexts(html);
                var regex = new Regex(@".+\[(.*)\].+");
                var qualities = texts.Select(text =>
                {
                    var match = regex.Match(text);
                    if (!match.Success) return "HDTV";
                    var quality = match.Groups[1].Value;
                    switch(quality)
                    {
                        case "720p":
                            return "HDTV-720p";
                        case "1080p":
                            return "HDTV-1080p";
                        default:
                            return "HDTV";
                    }
                });
                return qualities.ToList();
            }

            private List<KeyValuePair<int, KeyValuePair<int,int?>>> GetSeasonsFirstEpisodesAndLast(IHtmlDocument html)
            {
                var texts = LinkTexts(html);
                // SEASON | START EPISODE | [END EPISODE]
                var regex = new Regex(@"(\d{1,2})x(\d{1,2})(?:.*\d{1,2}x(\d{1,2})?)?", RegexOptions.IgnoreCase);
                var seasonsFirstEpisodesAndLast = texts.Select(text =>
                {
                    var match = regex.Match(text);
                    int season = 0;
                    int episode = 0;
                    int? finalEpisode = null;
                    if (!match.Success) return new KeyValuePair<int, KeyValuePair<int, int?>>(season, new KeyValuePair<int, int?>(episode, finalEpisode));
                    season = Int32.Parse(match.Groups[1].Value);
                    episode = Int32.Parse(match.Groups[2].Value);
                    if (match.Groups[3].Success)
                    {
                        finalEpisode = Int32.Parse(match.Groups[3].Value);
                    }
                    return new KeyValuePair<int, KeyValuePair<int, int?>>(season, new KeyValuePair<int, int?>(episode, finalEpisode));
                });
                return seasonsFirstEpisodesAndLast.ToList();
            }

            private List<string> LinkTexts(IHtmlDocument html)
            {
                return html.QuerySelectorAll(LinkQuerySelector)
                    .Select(e => e.TextContent).ToList();
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

                    // EXAMPLE: El cuento de la criada (Handmaids Tale)
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

        class SeasonScraper : IScraper<IEnumerable<MTReleaseInfo>>
        {
            public IEnumerable<MTReleaseInfo> Extract(IHtmlDocument html)
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

                var episodes = episodesLinks.Select(e => new MTReleaseInfo()).ToList();

                for (var i = 0; i < episodes.Count(); i++)
                {
                    GuessEpisodes(episodes.ElementAt(i), episodesTexts.ElementAt(i));
                    ExtractLinkInfo(episodes.ElementAt(i), episodesLinks.ElementAt(i));
                    episodes.ElementAt(i).PublishDate = dates.ElementAt(i);
                }

                return episodes;
            }

            private void GuessEpisodes(MTReleaseInfo release, string episodeText)
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

            private void ExtractLinkInfo(MTReleaseInfo release, String link)
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
                        case "HDTV-1080p":
                            Category = TorznabCatType.TVHD;
                            _type = "HDTV-1080p";
                            break;
                        default:
                            Category = TorznabCatType.TV;
                            _type = "HDTV-720p";
                            break;
                    }
                }
            }
        }

        class MTReleaseInfo : ReleaseInfo
        {
            public string MejorTorrentID;
            public bool IsMovie;
            public int? Year;
            public int _season;
            public int _episodeNumber;
            private string _categoryText;
            private string _originalTitle;

            public MTReleaseInfo()
            {
                this.Category = new List<int>();
                this.Grabs = 5;
                this.Files = 1;
                this.PublishDate = new DateTime();
                this.Peers = 1;
                this.Seeders = 1;
                this.Size = ReleaseInfo.BytesFromGB(1);
                this._originalTitle = "";
                this.DownloadVolumeFactor = 0;
                this.UploadVolumeFactor = 1;
            }

            public int Season { get { return _season; } set { _season = value; TitleOriginal = _originalTitle; } }

            public int EpisodeNumber { get { return _episodeNumber; } set { _episodeNumber = value; TitleOriginal = _originalTitle; } }

            public string CategoryText {
                get { return _categoryText; }
                set
                {
                    if (IsMovie)
                    {
                        Category.Add(TorznabCatType.Movies.ID);
                        _categoryText = value;
                    }
                    else
                    {
                        switch (value)
                        {
                            case "SDTV":
                                Category.Add(TorznabCatType.TVSD.ID);
                                _categoryText = "SDTV";
                                break;
                            case "HDTV":
                                Category.Add(TorznabCatType.TVSD.ID);
                                _categoryText = "SDTV";
                                break;
                            case "HDTV-720p":
                                Category.Add(TorznabCatType.TVHD.ID);
                                _categoryText = "HDTV-720p";
                                break;
                            case "HDTV-1080p":
                                Category.Add(TorznabCatType.TVHD.ID);
                                _categoryText = "HDTV-1080p";
                                break;
                            default:
                                Category.Add(TorznabCatType.TV.ID);
                                _categoryText = "HDTV-720p";
                                break;
                        }
                    }
                    TitleOriginal = _originalTitle;
                }
            }
        
            public int FinalEpisodeNumber { get { return (int)(EpisodeNumber + Files - 1); } }

            public string TitleOriginal
            {
                get { return _originalTitle; }
                set
                {
                    _originalTitle = value;
                    if (_originalTitle != "")
                    {
                        Title = _originalTitle;
                        Title = char.ToUpper(Title[0]) + Title.Substring(1);
                    }
                    var seasonAndEpisode = "";
                    if (!Category.Contains(TorznabCatType.Movies.ID))
                    {
                        seasonAndEpisode = "S" + Season.ToString("00") + "E" + EpisodeNumber.ToString("00");
                        if (Files > 1)
                        {
                            seasonAndEpisode += "-" + FinalEpisodeNumber.ToString("00");
                        }
                    }
                    Title = String.Join(".", new List<string>() { Title, seasonAndEpisode, CategoryText, "Spanish" }.Where(s => s != ""));
                }
            }
        }

        class MoviePerformer : IPerformer
        {
            private IRequester requester;
            private IScraper<IEnumerable<Uri>> movieSearchScraper;
            private IScraper<MTReleaseInfo> movieInfoScraper;
            private IScraper<Uri> movieDownloadScraper;

            public MoviePerformer(
                IRequester requester,
                IScraper<IEnumerable<Uri>> movieSearchScraper,
                IScraper<MTReleaseInfo> movieInfoScraper,
                IScraper<Uri> movieDownloadScraper)
            {
                this.requester = requester;
                this.movieSearchScraper = movieSearchScraper;
                this.movieInfoScraper = movieInfoScraper;
                this.movieDownloadScraper = movieDownloadScraper;
            }

            public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
            {
                query = SanitizeQuery(query);
                var movies = await FetchMoviesBasedOnLongestWord(query);
                return movies;
            }
            
            private async Task<IEnumerable<MTReleaseInfo>> FetchMoviesBasedOnLongestWord(TorznabQuery query)
            {
                var originalSearch = query.SearchTerm;
                var regexStr = ".*" + originalSearch.Replace(" ", ".*") + ".*";
                var regex = new Regex(regexStr, RegexOptions.IgnoreCase);
                query.SearchTerm = LongestWord(query);
                var movies = await FetchMovies(query);
                return movies.Where(m => regex.Match(m.Title).Success);
            }

            private async Task<IEnumerable<MTReleaseInfo>> FetchMovies(TorznabQuery query)
            {
                var uriSearch = CreateSearchUri(query.SearchTerm);
                var htmlSearch = await requester.MakeRequest(uriSearch);
                var moviesInfoUris = movieSearchScraper.Extract(htmlSearch);
                var infoHtmlTasks = moviesInfoUris.Select(async u => await requester.MakeRequest(u));
                var infoHtmls = await Task.WhenAll(infoHtmlTasks);
                var movies = infoHtmls.Select(h => movieInfoScraper.Extract(h));

                var tasks = movies.Select(async m =>
                {
                    var html = await requester.MakeRequest(m.Link);
                    return new KeyValuePair<MTReleaseInfo, IHtmlDocument>(m, html);
                });
                var moviesWithHtml = await Task.WhenAll(tasks.ToArray());
                movies = moviesWithHtml.Select(movieWithHtml =>
                {
                    var movie = movieWithHtml.Key;
                    var html = movieWithHtml.Value;
                    movie.Link = movieDownloadScraper.Extract(html);
                    movie.Guid = movieWithHtml.Key.Link;
                    return movie;
                });

                if (query.Year != null)
                {
                    movies = movies
                        .Where(m => 
                            m.Year == query.Year ||
                            m.Year == query.Year + 1 ||
                            m.Year == query.Year - 1)
                        .Select(m => 
                        {
                            m.TitleOriginal = m.TitleOriginal.Replace("(" + m.Year + ")", "(" + query.Year + ")");
                            return m;
                        });
                }

                return movies;
            }

            private string LongestWord(TorznabQuery query)
            {
                var words = query.SearchTerm.Split(' ');
                if (words.Count() == 0) return null;
                var longestWord = words.First();
                foreach(var word in words)
                {
                    if (word.Length >= longestWord.Length)
                    {
                        longestWord = word;
                    }
                }
                return longestWord;
            }

            private TorznabQuery SanitizeQuery(TorznabQuery query)
            {
                var regex = new Regex(@"\d{4}$");
                var match = regex.Match(query.SanitizedSearchTerm);
                if (match.Success)
                {
                    var yearStr = match.Groups[0].Value;
                    query.Year = Int32.Parse(yearStr);
                    query.SearchTerm = query.SearchTerm.Replace(yearStr, "").Trim();
                }
                return query;
            }
        }

        class MovieSearchScraper : IScraper<IEnumerable<Uri>>
        {
            public IEnumerable<Uri> Extract(IHtmlDocument html)
            {
                return html.QuerySelectorAll("a[href*=\"/peli-\"]")
                    .Select(e => e.GetAttribute("href"))
                    .Select(relativeUri => new Uri(WebUri, relativeUri));
            }
        }

        class MovieInfoScraper : IScraper<MTReleaseInfo>
        {
            public MTReleaseInfo Extract(IHtmlDocument html)
            {
                var release = new MTReleaseInfo();
                release.IsMovie = true;
                var selectors = html.QuerySelectorAll("b");
                var titleSelector = html.QuerySelector("span>b");
                var titleSelector3do4k = html.QuerySelector("span:nth-child(4) > b:nth-child(1)");
                try
                {
                    var title = titleSelector.TextContent;
                    if (title.Contains("("))
                    {
                        title = title.Substring(0, title.IndexOf("(")).Trim();
                    }
                    release.TitleOriginal = title;
                }
                catch { }
                try
                {
                    var year = selectors.Where(s => s.TextContent.ToLower().Contains("año"))
                        .First().NextSibling.TextContent.Trim();
                    release.Year = Int32.Parse(year);
                    release.TitleOriginal += " (" + year + ")";
                } catch { }
                try
                {
                    var dateStr = selectors.Where(s => s.TextContent.ToLower().Contains("fecha"))
                        .First().NextSibling.TextContent.Trim();
                    var date = Convert.ToDateTime(dateStr);
                    release.PublishDate = date;
                } catch { }
                try
                {
                    var sizeStr = selectors.Where(s => s.TextContent.ToLower().Contains("tamaño"))
                        .First().NextSibling.TextContent.Trim();
                    Regex rgx = new Regex(@"[^0-9,.]");
                    long size;
                    if (sizeStr.ToLower().Trim().EndsWith("mb"))
                    {
                        size = ReleaseInfo.BytesFromMB(float.Parse(rgx.Replace(sizeStr, "")));
                    }
                    else
                    {
                        sizeStr = rgx.Replace(sizeStr, "").Replace(",", ".");
                        size = ReleaseInfo.BytesFromGB(float.Parse(rgx.Replace(sizeStr, "")));
                    }
                    release.Size = size;
                } catch { }
                try
                {
                    var category = selectors.Where(s => s.TextContent.ToLower().Contains("formato"))
                        .First().NextSibling.TextContent.Trim();
                    release.CategoryText = category;
                } catch { }
                try
                {
                    var title = titleSelector.TextContent;
                    if (title.Contains("(") && title.Contains(")") && title.Contains("3D"))
                    {
                        release.CategoryText = "3D";
                    }
                } catch { }
                try
                {
                    var title = titleSelector.TextContent;
                    if (title.Contains("(") && title.Contains(")") && title.Contains("4K"))
                    {
                        release.CategoryText = "4K";
                    }
                } catch { }
                try
                {
                    var title = titleSelector3do4k.TextContent;
                    if (title.Contains("[") && title.Contains("]") && title.Contains("3D"))
                    {
                        release.CategoryText = "3D";
                    }
                } catch { }
                try
                {
                    var title = titleSelector3do4k.TextContent;
                    if (title.Contains("[") && title.Contains("]") && title.Contains("4K"))
                    {
                        release.CategoryText = "4K";
                    }
                } catch { }
                try
                {
                    var link = html.QuerySelector("a[href*=\"sec=descargas\"]").GetAttribute("href");
                    release.Link = new Uri(WebUri, link);
                    release.Guid = release.Link;
                } catch { }
                return release;
            }
        }

        class MovieDownloadScraper : IScraper<Uri>
        {
            public Uri Extract(IHtmlDocument html)
            {
                try
                {
                    return new Uri(WebUri, html.QuerySelector("a[href*=\".torrent\"]").GetAttribute("href"));
                }
                catch
                {
                    return null;
                }
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
                var doc = SearchResultParser.ParseDocument(mt.Encoding.GetString(result.Content));
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
            private IScraper<IEnumerable<KeyValuePair<MTReleaseInfo, Uri>>> rssScraper;
            private IScraper<IEnumerable<MTReleaseInfo>> seasonScraper;
            private IDownloadGenerator downloadGenerator;

            public RssPerformer(
                IRequester requester,
                IScraper<IEnumerable<KeyValuePair<MTReleaseInfo, Uri>>> rssScraper,
                IScraper<IEnumerable<MTReleaseInfo>> seasonScraper,
                IDownloadGenerator downloadGenerator)
            {
                this.requester = requester;
                this.rssScraper = rssScraper;
                this.seasonScraper = seasonScraper;
                this.downloadGenerator = downloadGenerator;
            }

            public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
            {
                var html = await requester.MakeRequest(NewTorrentsUri);
                var episodesAndSeasonsUri = rssScraper.Extract(html);

                Task.WaitAll(episodesAndSeasonsUri.ToList().Select(async epAndSeasonUri =>
                {
                    var episode = epAndSeasonUri.Key;
                    var seasonUri = epAndSeasonUri.Value;
                    await AddMejorTorrentIDs(episode, seasonUri);
                }).ToArray());

                var episodes = episodesAndSeasonsUri.Select(epAndSeason => epAndSeason.Key).ToList();
                await downloadGenerator.AddDownloadLinks(episodes);
                return episodes;
            }

            private async Task AddMejorTorrentIDs(MTReleaseInfo episode, Uri seasonUri)
            {
                var html = await requester.MakeRequest(seasonUri);
                var newEpisodes = seasonScraper.Extract(html);
                // GET BY EPISODE NUMBER
                newEpisodes = newEpisodes.Where(e => e.EpisodeNumber == episode.EpisodeNumber);
                if (newEpisodes.Count() == 0)
                {
                    throw new Exception("Imposible to detect episode ID in RSS");
                }
                episode.MejorTorrentID = newEpisodes.First().MejorTorrentID;
            }

        }

        class TvShowPerformer : IPerformer
        {
            private IRequester requester;
            private IScraper<IEnumerable<Season>> tvShowScraper;
            private IScraper<IEnumerable<MTReleaseInfo>> seasonScraper;
            private IDownloadGenerator downloadGenerator;

            public TvShowPerformer(
                IRequester requester,
                IScraper<IEnumerable<Season>> tvShowScraper,
                IScraper<IEnumerable<MTReleaseInfo>> seasonScraper,
                IDownloadGenerator downloadGenerator)
            {
                this.requester = requester;
                this.tvShowScraper = tvShowScraper;
                this.seasonScraper = seasonScraper;
                this.downloadGenerator = downloadGenerator;
            }

            public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
            {
                query = FixQuery(query);
                var seasons = await GetSeasons(query);
                var episodes = await GetEpisodes(query, seasons);
                await downloadGenerator.AddDownloadLinks(episodes);
                if (seasons.Count() > 0)
                {
                    episodes.ForEach(e => e.TitleOriginal = seasons.First().Title);
                }
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
                if (query.Season != 0)
                {
                    seasons = seasons.Where(s => s.Number == query.Season);
                }
                if (query.Categories.Count() != 0)
                {
                    seasons = seasons.Where(s => new List<int>(query.Categories).Contains(s.Category.ID));
                }
                return seasons.ToList();
            }

            private async Task<List<MTReleaseInfo>> GetEpisodes(TorznabQuery query, IEnumerable<Season> seasons)
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
                        return e;
                    });
                });
                if (!string.IsNullOrEmpty(query.Episode))
                {
                    var episodeNumber = Int32.Parse(query.Episode);
                    episodes = episodes.Where(e => e.EpisodeNumber <= episodeNumber && episodeNumber <= e.FinalEpisodeNumber);
                }
                return episodes.ToList();
            }
        }

        interface IDownloadGenerator
        {
            Task AddDownloadLinks(IEnumerable<MTReleaseInfo> episodes);
        }

        class DownloadGenerator : IDownloadGenerator
        {
            private IRequester requester;
            private IScraper<IEnumerable<Uri>> downloadScraper;

            public DownloadGenerator(IRequester requester, IScraper<IEnumerable<Uri>> downloadScraper)
            {
                this.requester = requester;
                this.downloadScraper = downloadScraper;
            }

            public async Task AddDownloadLinks(IEnumerable<MTReleaseInfo> episodes)
            {
                var downloadRequester = new MejorTorrentDownloadRequesterDecorator(requester);
                var downloadHtml = await downloadRequester.MakeRequest(episodes.Select(e => e.MejorTorrentID));
                var downloads = downloadScraper.Extract(downloadHtml).ToList();

                for (var i = 0; i < downloads.Count; i++)
                {
                    var e = episodes.ElementAt(i);
                    episodes.ElementAt(i).Link = downloads.ElementAt(i);
                    episodes.ElementAt(i).Guid = downloads.ElementAt(i);
                }
            }
        }
    }
}
