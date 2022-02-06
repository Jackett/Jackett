using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class NewPCT : BaseCachingWebIndexer
    {
        private enum ReleaseType
        {
            Tv,
            Movie
        }

        private class NewpctRelease : ReleaseInfo
        {
            public ReleaseType NewpctReleaseType;
            public string SeriesName;
            public int? Season;
            public int? Episode;
            public int? EpisodeTo;
            public int Score;

            public NewpctRelease()
            {
            }

            public NewpctRelease(NewpctRelease copyFrom) :
                base(copyFrom)
            {
                NewpctReleaseType = copyFrom.NewpctReleaseType;
                SeriesName = copyFrom.SeriesName;
                Season = copyFrom.Season;
                Episode = copyFrom.Episode;
                EpisodeTo = copyFrom.EpisodeTo;
                Score = copyFrom.Score;
            }

            public override object Clone() => new NewpctRelease(this);
        }

        private readonly char[] _wordSeparators = { ' ', '.', ',', ';', '(', ')', '[', ']', '-', '_' };
        private readonly int _wordNotFoundScore = 100000;
        private readonly Regex _searchStringRegex = new Regex(@"(.+?)S(\d{2})(E(\d{2}))?$", RegexOptions.IgnoreCase);
        // Defending Jacob Temp. 1 Capitulo 1
        private readonly Regex _seriesChapterTitleRegex = new Regex(@"(.+)Temp. (\d+) Capitulo (\d+)", RegexOptions.IgnoreCase);
        // Love 101 - Temp. 1 Capitulos 1 al 8
        private readonly Regex _seriesChaptersTitleRegex = new Regex(@"(.+)Temp. (\d+) Capitulos (\d+) al (\d+)", RegexOptions.IgnoreCase);
        private readonly Regex _titleYearRegex = new Regex(@" *[\[\(]? *((19|20)\d{2}) *[\]\)]? *$");

        private readonly int _maxMoviesPages = 6;
        private readonly int[] _allTvCategories = (new[] { TorznabCatType.TV }).Concat(TorznabCatType.TV.SubCategories).Select(c => c.ID).ToArray();
        private readonly int[] _allMoviesCategories = (new[] { TorznabCatType.Movies }).Concat(TorznabCatType.Movies.SubCategories).Select(c => c.ID).ToArray();

        private bool _includeVo;
        private bool _filterMovies;
        private bool _removeMovieAccents;
        private bool _removeMovieYear;
        private DateTime _dailyNow;
        private int _dailyResultIdx;

        private readonly string _dailyUrl = "ultimas-descargas/";
        private readonly string _searchJsonUrl = "get/result/";
        private readonly string[] _seriesLetterUrls = { "series/letter/{0}", "series-hd/letter/{0}" };
        private readonly string[] _seriesVoLetterUrls = { "series-vo/letter/{0}" };
        private readonly string[] _voUrls = { "serie-vo", "serievo" };

        public override string[] AlternativeSiteLinks { get; protected set; } = {
            "https://atomixhq.art/",
            "https://pctmix1.unblockit.how/"
        };

        public override string[] LegacySiteLinks { get; protected set; } = {
            "https://pctnew.site",
            "https://descargas2020.site",
            "http://torrentrapid.com/",
            "http://tumejortorrent.com/",
            "http://pctnew.com/",
            "https://descargas2020.org/",
            "https://pctnew.org/",
            "https://pctreload.com/",
            "https://pctmix1.unblockit.ch/",
            "https://pctmix1.unblockit.ws/",
            "https://pctmix1.unblockit.li/",
            "https://pctmix.com/",
            "https://pctmix1.com/",
            "https://pctreload1.com/",
            "https://maxitorrent.com",
            "https://pctmix1.unblockit.kim/",
            "https://atomixhq.com/",
            "https://pctmix1.unblockit.bz/",
            "https://atomixhq.one/",
            "https://pctmix1.unblockit.tv/",
            "https://atomixhq.top/",
            "https://atomixhq.net/"
        };

        public NewPCT(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "newpct",
                   name: "NewPCT",
                   description: "NewPCT - Descargar peliculas, series y estrenos torrent gratis",
                   link: "https://atomixhq.art/",
                   caps: new TorznabCapabilities
                   {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                       },
                       MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q
                       }
                   },
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationData())
        {
            Encoding = Encoding.GetEncoding("windows-1252");
            Language = "es-ES";
            Type = "public";

            var voItem = new BoolConfigurationItem("Include original versions in search results") { Value = false };
            configData.AddDynamic("IncludeVo", voItem);

            var filterMoviesItem = new BoolConfigurationItem("Only full match movies") { Value = true };
            configData.AddDynamic("FilterMovies", filterMoviesItem);

            var removeMovieAccentsItem = new BoolConfigurationItem("Remove accents in movie searches") { Value = true };
            configData.AddDynamic("RemoveMovieAccents", removeMovieAccentsItem);

            var removeMovieYearItem = new BoolConfigurationItem("Remove year from movie results (enable for Radarr)") { Value = false };
            configData.AddDynamic("RemoveMovieYear", removeMovieYearItem);

            AddCategoryMapping(1, TorznabCatType.Movies);
            AddCategoryMapping(2, TorznabCatType.TV);
            AddCategoryMapping(3, TorznabCatType.TVSD);
            AddCategoryMapping(4, TorznabCatType.TVHD);
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var results = await PerformQuery(new TorznabQuery());
            if (!results.Any())
                throw new Exception("Found 0 releases!");

            IsConfigured = true;
            SaveConfig();
            return IndexerConfigurationStatus.Completed;
        }

        public override async Task<byte[]> Download(Uri linkParam)
        {
            Uri uriLink = null;
            var downloadLink = linkParam.AbsoluteUri.Replace("/descargar/", "/descargar/torrent/");
            var result = await RequestWithCookiesAndRetryAsync(downloadLink, referer: linkParam.AbsoluteUri);

            var downloadRegex = new Regex("/t_download/([0-9]+)/");
            var match = downloadRegex.Match(result.ContentString);
            if (match.Success)
            {
                const string downloadUrl = "https://atomtt.com/to.php";
                var headers = new Dictionary<string, string>
                {
                    {"X-Requested-With", "XMLHttpRequest"},
                    {"Content-Type", "application/x-www-form-urlencoded; charset=UTF-8"}
                };
                var body = "t=" + match.Groups[1].Value;
                result = await RequestWithCookiesAsync(downloadUrl, method: RequestType.POST, rawbody: body,
                                                       headers: headers, referer: downloadLink);
                if (result.Status == HttpStatusCode.OK)
                    uriLink = new Uri(SiteLink + "t_download/" + result.ContentString + ".torrent");
            }

            if (uriLink == null)
                throw new Exception("Download link not found!");

            return await base.Download(uriLink);
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            _includeVo = ((BoolConfigurationItem)configData.GetDynamic("IncludeVo")).Value;
            _filterMovies = ((BoolConfigurationItem)configData.GetDynamic("FilterMovies")).Value;
            _removeMovieAccents = ((BoolConfigurationItem)configData.GetDynamic("RemoveMovieAccents")).Value;
            _removeMovieYear = ((BoolConfigurationItem)configData.GetDynamic("RemoveMovieYear")).Value;
            _dailyNow = DateTime.Now;
            _dailyResultIdx = 0;
            var rssMode = string.IsNullOrEmpty(query.SanitizedSearchTerm);

            if (rssMode)
            {
                var results = await RequestWithCookiesAndRetryAsync(SiteLink + _dailyUrl, referer: SiteLink);
                var items = ParseDailyContent(results.ContentString);
                releases.AddRange(items);
            }
            else
            {
                var isTvSearch = query.Categories == null || query.Categories.Length == 0 ||
                    query.Categories.Any(c => _allTvCategories.Contains(c));
                if (isTvSearch)
                    releases.AddRange(await TvSearch(query));

                var isMovieSearch = query.Categories == null || query.Categories.Length == 0 ||
                    query.Categories.Any(c => _allMoviesCategories.Contains(c));
                if (isMovieSearch)
                    releases.AddRange(await MovieSearch(query));
            }

            // Database lost on 2018/04/05, all previous torrents don't have download links
            var failureDay = new DateTime(2018, 04, 05);
            releases = releases.Where(r => r.PublishDate > failureDay).ToList();

            return releases;
        }

        private async Task<IEnumerable<ReleaseInfo>> TvSearch(TorznabQuery query)
        {
            var seriesName = query.SanitizedSearchTerm;
            var season = query.Season > 0 ? (int?)query.Season : null;
            int? episode = null;
            if (!string.IsNullOrWhiteSpace(query.Episode) && int.TryParse(query.Episode, out var episodeTemp))
                episode = episodeTemp;

            //If query has no season/episode info, try to parse title
            if (season == null && episode == null)
            {
                var searchMatch = _searchStringRegex.Match(query.SanitizedSearchTerm);
                if (searchMatch.Success)
                {
                    seriesName = searchMatch.Groups[1].Value.Trim();
                    season = int.Parse(searchMatch.Groups[2].Value);
                    episode = searchMatch.Groups[4].Success ? (int?)int.Parse(searchMatch.Groups[4].Value) : null;
                }
            }

            var releases = new List<ReleaseInfo>();

            //Search series url
            foreach (var seriesListUrl in SeriesListUris(seriesName))
                releases.AddRange(await GetReleasesFromUri(seriesListUrl, seriesName));

            //Sonarr removes "the" from shows. If there is nothing try prepending "the"
            if (releases.Count == 0 && !(seriesName.ToLower().StartsWith("the")))
            {
                seriesName = "The " + seriesName;
                foreach (var seriesListUrl in SeriesListUris(seriesName))
                    releases.AddRange(await GetReleasesFromUri(seriesListUrl, seriesName));
            }

            // remove duplicates
            releases = releases.GroupBy(x => x.Guid).Select(y => y.First()).ToList();

            //Filter only episodes needed
            return releases.Where(r =>
            {
                var nr = r as NewpctRelease;
                return (
                    nr.Season.HasValue != season.HasValue || //Can't determine if same season
                    nr.Season.HasValue && season.Value == nr.Season.Value && //Same season and ...
                    (
                        nr.Episode.HasValue != episode.HasValue || //Can't determine if same episode
                        nr.Episode.HasValue &&
                        (
                            nr.Episode.Value == episode.Value || //Same episode
                            nr.EpisodeTo.HasValue && episode.Value >= nr.Episode.Value && episode.Value <= nr.EpisodeTo.Value //Episode in interval
                        )
                    )
                );
            });
        }

        private async Task<List<ReleaseInfo>> GetReleasesFromUri(Uri uri, string seriesName)
        {
            var releases = new List<ReleaseInfo>();

            // Episodes list
            var results = await RequestWithCookiesAndRetryAsync(uri.AbsoluteUri, referer: uri.AbsoluteUri);
            var seriesEpisodesUrl = ParseSeriesListContent(results.ContentString, seriesName);

            // TV serie list
            if (!string.IsNullOrEmpty(seriesEpisodesUrl))
            {
                results = await RequestWithCookiesAndRetryAsync(seriesEpisodesUrl, referer: seriesEpisodesUrl);
                var items = ParseEpisodesListContent(results.ContentString);
                if (items != null && items.Any())
                    releases.AddRange(items);
            }
            return releases;
        }

        private IEnumerable<Uri> SeriesListUris(string seriesName)
        {
            IEnumerable<string> lettersUrl;
            if (!_includeVo)
                lettersUrl = _seriesLetterUrls;
            else
                lettersUrl = _seriesLetterUrls.Concat(_seriesVoLetterUrls);
            var seriesLetter = !char.IsDigit(seriesName[0]) ? seriesName[0].ToString() : "0-9";
            return lettersUrl.Select(
                urlFormat => new Uri(SiteLink + string.Format(urlFormat, seriesLetter.ToLower())));
        }

        private List<NewpctRelease> ParseDailyContent(string content)
        {
            var parser = new HtmlParser();
            var doc = parser.ParseDocument(content);

            var releases = new List<NewpctRelease>();

            try
            {
                var rows = doc.QuerySelectorAll("div.page-box > ul > li");
                foreach (var row in rows)
                {
                    var qDiv = row.QuerySelector("div.info");
                    var title = qDiv.QuerySelector("h2").TextContent.Trim();
                    var detailsUrl = SiteLink + qDiv.QuerySelector("a").GetAttribute("href").TrimStart('/');

                    // TODO: move this check to GetReleaseFromData to apply all releases
                    if (!_includeVo && _voUrls.Any(vo => detailsUrl.ToLower().Contains(vo.ToLower())))
                        continue;

                    var span = qDiv.QuerySelector("span");
                    var quality = span.ChildNodes[0].TextContent.Trim();
                    var releaseType = ReleaseTypeFromQuality(quality);
                    var sizeString = span.ChildNodes[1].TextContent.Replace("Tama\u00F1o", "").Trim();
                    var size = ReleaseInfo.GetBytes(sizeString);

                    var language = qDiv.QuerySelector("div > strong").TextContent.Trim();

                    _dailyResultIdx++;
                    var publishDate = _dailyNow - TimeSpan.FromMilliseconds(_dailyResultIdx);

                    var poster = "https:" + row.QuerySelector("img").GetAttribute("src");

                    var release = GetReleaseFromData(releaseType, title, detailsUrl, quality, language, size, publishDate, poster);
                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(content, ex);
            }

            return releases;
        }

        private string ParseSeriesListContent(string content, string title)
        {
            var titleLower = title.Trim().ToLower();
            var parser = new HtmlParser();
            var doc = parser.ParseDocument(content);
            try
            {
                var rows = doc.QuerySelectorAll(".pelilist li a");
                foreach (var row in rows)
                    if (titleLower.Equals(row.QuerySelector("h2").TextContent.Trim().ToLower()))
                        return row.GetAttribute("href");
            }
            catch (Exception ex)
            {
                OnParseError(content, ex);
            }
            return null;
        }

        private List<NewpctRelease> ParseEpisodesListContent(string content)
        {
            var parser = new HtmlParser();
            var doc = parser.ParseDocument(content);

            var releases = new List<NewpctRelease>();

            try
            {
                var rows = doc.QuerySelectorAll("ul.buscar-list > li");
                foreach (var row in rows)
                {
                    var qDiv = row.QuerySelector("div.info");
                    var qTitle = qDiv.QuerySelector("h2");
                    if (qTitle.Children.Length == 0)
                        continue; // we skip episodes with old title (those torrents can't be downloaded anyway)
                    var title = qTitle.Children[0].TextContent.Trim();
                    var language = qTitle.Children[1].TextContent.Trim();
                    var quality = qTitle.Children[2].TextContent.Replace("[", "").Replace("]", "").Trim();

                    var detailsUrl = qDiv.QuerySelector("a").GetAttribute("href");

                    var publishDate = DateTime.ParseExact(qDiv.ChildNodes[3].TextContent.Trim(), "dd-MM-yyyy", null);
                    var size = ReleaseInfo.GetBytes(qDiv.ChildNodes[5].TextContent.Trim());

                    var poster = "https:" + row.QuerySelector("img").GetAttribute("src");

                    var release = GetReleaseFromData(ReleaseType.Tv, title, detailsUrl, quality, language, size, publishDate, poster);
                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(content, ex);
            }

            return releases;
        }

        private async Task<IEnumerable<ReleaseInfo>> MovieSearch(TorznabQuery query)
        {
            var releases = new List<NewpctRelease>();

            var searchStr = query.SanitizedSearchTerm;
            if (_removeMovieAccents)
                searchStr = RemoveDiacritics(searchStr);

            // we always remove the year in the search, even if _removeMovieYear is disabled
            // and we save the year to add it in the title if required
            var year = "";
            var matchYear = _titleYearRegex.Match(searchStr);
            if (matchYear.Success)
            {
                year = matchYear.Groups[1].Value;
                searchStr = _titleYearRegex.Replace(searchStr, "");
            }

            var searchJsonUrl = SiteLink + _searchJsonUrl;

            var pg = 1;
            while (pg <= _maxMoviesPages)
            {
                var queryCollection = new Dictionary<string, string>
                {
                    {"ordenar", "Lo+Ultimo"},
                    {"inon", "Descendente"},
                    {"s", searchStr},
                    {"pg", pg.ToString()}
                };

                var results = await RequestWithCookiesAsync(searchJsonUrl, method: RequestType.POST, data: queryCollection, referer: SiteLink);
                var items = ParseSearchJsonContent(results.ContentString, year);
                if (!items.Any())
                    break;

                releases.AddRange(items);
                pg++;
            }

            ScoreReleases(releases, searchStr);

            if (_filterMovies)
                releases = releases.Where(r => r.Score < _wordNotFoundScore).ToList();

            return releases;
        }

        private List<NewpctRelease> ParseSearchJsonContent(string content, string year)
        {
            var releases = new List<NewpctRelease>();
            if (string.IsNullOrWhiteSpace(content))
                return releases;

            try
            {
                var jo = JObject.Parse(content);

                var numItems = int.Parse(jo["data"]["items"].ToString());
                for (var i = 0; i < numItems; i++)
                {
                    var item = jo["data"]["torrents"]["0"][i.ToString()];

                    var title = item["torrentName"].ToString();
                    var detailsUrl = SiteLink + item["guid"];
                    var quality = item["calidad"].ToString();
                    var sizeString = item["torrentSize"].ToString();
                    var size = !sizeString.Contains("NAN") ? ReleaseInfo.GetBytes(sizeString) : 0;
                    DateTime.TryParseExact(item["torrentDateAdded"].ToString(), "dd/MM/yyyy", null, DateTimeStyles.None, out var publishDate);
                    var poster = SiteLink + item["imagen"].ToString().TrimStart('/');

                    // we have another search for series
                    var titleLower = title.ToLower();
                    var isSeries = (quality != null && quality.ToLower().Contains("hdtv")) ||
                                   detailsUrl.Contains("/serie-") || detailsUrl.Contains("/series/");
                    var isGame = titleLower.Contains("pcdvd");
                    if (isSeries || isGame)
                        continue;

                    // at this point we assume that this is a movie release, we need to parse the title. examples:
                    // Quien Es Harry Crumb (1989) [BluRay 720p X264 MKV][AC3 5.1 Castellano][www.descargas2020.ORG]
                    // Harry Potter y la orden del Fenix [4K UHDrip][2160p][HDR][AC3 5.1 Castellano DTS 5.1-Ingles+Subs][ES-EN]
                    // Harry Potter Y El Misterio Del Principe [DVDFULL][Spanish][2009]
                    // Harry Potter 2 Y La Camara Secreta [DVD9 FULL][Spanish_English][Inc Subs.]
                    // The Avengers [DVDRIP][VOSE English_Subs. EspaÃ±ol][2012]
                    // Harry Potter y las Reliquias de la Muerte Parte I.DVD5  [ DVDR] [AC3 5.1] [Multilenguaje] [2010]
                    // Joker (2019) 720p [Web Screener 720p ][Castellano][www.descargas2020.ORG][www.pctnew.ORG]

                    // remove quality and language from the title
                    var titleParts = title.Split('[');
                    title = titleParts[0].Replace("720p", "").Trim();

                    // quality in the field quality/calidad is wrong in many cases
                    if (!string.IsNullOrWhiteSpace(quality))
                    {
                        if (titleLower.Contains("720") && !quality.Contains("720"))
                            quality += " 720p";
                        if (titleLower.Contains("265") || titleLower.Contains("hevc"))
                            quality += " x265";
                        if (titleLower.Contains("dvdfull") || titleLower.Contains("dvd5") || titleLower.Contains("dvd9"))
                            quality = "DVDR";
                        if (titleLower.Contains("[web screener]") || titleLower.Contains("[hd-tc]"))
                            quality = "TS Screener";
                    }
                    else if (titleParts.Length > 2)
                        quality = titleParts[1].Replace("]", "").Replace("MKV", "").Trim();

                    // we have to guess the language (words DUAL or MULTI are not supported in Radarr)
                    var language = "spanish";
                    if (titleLower.Contains("latino"))
                        language += " latino";
                    if ((titleLower.Contains("castellano") && titleLower.Contains("ingles")) ||
                        (titleLower.Contains("spanish") && titleLower.Contains("english")) ||
                        titleLower.Contains("[es-en]") || titleLower.Contains("multilenguaje"))
                        language += " english";
                    else if (titleLower.Contains("vose"))
                        language = "english vose";

                    // remove the movie year if the user chooses (the year in the title is wrong in many cases)
                    if (_removeMovieYear)
                        title = _titleYearRegex.Replace(title, "");

                    // we add the year from search if it's not in the title
                    if (!string.IsNullOrWhiteSpace(year) && !_titleYearRegex.Match(title).Success)
                        title += " " + year;

                    var release = GetReleaseFromData(ReleaseType.Movie, title, detailsUrl, quality, language, size, publishDate, poster);
                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(content, ex);
            }

            return releases;
        }

        private void ScoreReleases(IEnumerable<NewpctRelease> releases, string searchTerm)
        {
            var searchWords = searchTerm.ToLower().Split(_wordSeparators, StringSplitOptions.None).
                Select(s => s.Trim()).
                Where(s => !string.IsNullOrEmpty(s)).ToArray();

            foreach (var release in releases)
            {
                release.Score = 0;
                var releaseWords = release.Title.ToLower().Split(_wordSeparators, StringSplitOptions.None).
                    Select(s => s.Trim()).
                    Where(s => !string.IsNullOrEmpty(s)).ToArray();

                foreach (var search in searchWords)
                {
                    var index = Array.IndexOf(releaseWords, search);
                    if (index >= 0)
                    {
                        release.Score += index;
                        releaseWords[index] = null;
                    }
                    else
                        release.Score += _wordNotFoundScore;
                }
            }
        }

        private static ReleaseType ReleaseTypeFromQuality(string quality) =>
            quality.Trim().ToLower().StartsWith("hdtv")
                ? ReleaseType.Tv
                : ReleaseType.Movie;

        private NewpctRelease GetReleaseFromData(ReleaseType releaseType, string title, string detailsUrl, string quality,
                                                 string language, long size, DateTime publishDate, string poster)
        {
            var result = new NewpctRelease
            {
                NewpctReleaseType = releaseType
            };

            //Sanitize
            title = title.Replace("-", "");
            title = Regex.Replace(title, @"\s+", " ");

            if (releaseType == ReleaseType.Tv)
            {
                var match = _seriesChapterTitleRegex.Match(title);
                if (match.Success)
                {
                    result.SeriesName = match.Groups[1].Value.Trim();
                    result.Season = int.Parse(match.Groups[2].Value);
                    result.Episode = int.Parse(match.Groups[3].Value);
                }
                else
                {
                    match = _seriesChaptersTitleRegex.Match(title);
                    if (match.Success)
                    {
                        result.SeriesName = match.Groups[1].Value.Trim();
                        result.Season = int.Parse(match.Groups[2].Value);
                        result.Episode = int.Parse(match.Groups[3].Value);
                        result.EpisodeTo = int.Parse(match.Groups[4].Value);
                    }
                }

                // tv series
                var episodeText = "S" + result.Season.ToString().PadLeft(2, '0');
                episodeText += "E" + result.Episode.ToString().PadLeft(2, '0');
                episodeText += result.EpisodeTo.HasValue ? "-" + result.EpisodeTo.ToString().PadLeft(2, '0') : "";
                result.Title = $"{result.SeriesName} {episodeText}";

                if (!string.IsNullOrWhiteSpace(quality) && (quality.Contains("720") || quality.Contains("1080")))
                    result.Category = new List<int> { TorznabCatType.TVHD.ID };
                else
                    result.Category = new List<int> { TorznabCatType.TV.ID };
            }
            else
            {
                // movie
                result.Title = title;
                result.Category = new List<int> { TorznabCatType.Movies.ID };
            }

            result.Title = FixedTitle(result, quality, language);
            result.Link = new Uri(detailsUrl);
            result.Guid = result.Link;
            result.Details = result.Link;
            result.PublishDate = publishDate;
            result.Poster = new Uri(poster);
            result.Seeders = 1;
            result.Peers = 2;
            result.Size = size;
            result.DownloadVolumeFactor = 0;
            result.UploadVolumeFactor = 1;

            return result;
        }

        private string FixedTitle(NewpctRelease release, string quality, string language)
        {
            var fixedLanguage = language.ToLower()
                                        .Replace("español", "spanish")
                                        .Replace("espanol", "spanish")
                                        .Replace("castellano", "spanish")
                                        .ToUpper();

            var qualityLower = quality.ToLower();
            var fixedQuality = quality.Replace("-", " ");
            if (qualityLower.Contains("full"))
                fixedQuality = qualityLower.Contains("4k") ? "BluRay 2160p COMPLETE x265" : "BluRay COMPLETE";
            else if (qualityLower.Contains("remux"))
                fixedQuality = qualityLower.Contains("4k") ? "BluRay 2160p REMUX x265" : "BluRay REMUX";
            else if (qualityLower.Contains("4k")) // filter full and remux before 4k (there are 4k full and remux)
                fixedQuality = "BluRay 2160p x265";
            else if (qualityLower.Contains("microhd"))
                fixedQuality = qualityLower.Contains("720") ? "BluRay 720p MicroHD" : "BluRay 1080p MicroHD";
            else if (qualityLower.Contains("blurayrip"))
                fixedQuality = "BluRay 720p";
            else if (qualityLower.Contains("dvdrip"))
                fixedQuality = "DVDRip";
            else if (qualityLower.Contains("htdv"))
                fixedQuality = "HDTV";
            // BluRay and DVD Screener are not supported in Radarr
            else if (qualityLower.Contains("screener") || qualityLower.Contains("screeener"))
            {
                if (qualityLower.Contains("720p") || qualityLower.Contains("dvd"))
                    fixedQuality = "Screener 720p";
                else if (qualityLower.Contains("bluray")) // there are bluray with 720p (condition after 720p)
                    fixedQuality = "Screener 1080p";
                else
                    fixedQuality = "TS Screener";
            }

            return $"{release.Title} {fixedLanguage} {fixedQuality}";
        }

        private string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);

            // https://stackoverflow.com/a/14812065/9719178
            // TODO Better performance version in .Net-Core:
            // return string.Concat(normalizedString.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark))
            //              .Normalize(NormalizationForm.FormC);

            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                    stringBuilder.Append(c);
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
