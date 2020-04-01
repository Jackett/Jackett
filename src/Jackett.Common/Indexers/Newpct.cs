using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

namespace Jackett.Common.Indexers
{
    public class Newpct : BaseCachingWebIndexer
    {
        private enum ReleaseType
        {
            Tv,
            Movie,
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

        private class DownloadMatcher
        {
            public Regex MatchRegex;
            public MatchEvaluator MatchEvaluator;
        }

        private readonly char[] _wordSeparators = new char[] { ' ', '.', ',', ';', '(', ')', '[', ']', '-', '_' };
        private readonly int _wordNotFoundScore = 100000;
        private readonly Regex _searchStringRegex = new Regex(@"(.+?)S0?(\d+)(E0?(\d+))?$", RegexOptions.IgnoreCase);
        private readonly Regex _titleListRegex = new Regex(@"Serie( *Descargar)?(.+?)(Temporada(.+?)(\d+)(.+?))?Capitulos?(.+?)(\d+)((.+?)(\d+))?(.+?)-(.+?)Calidad(.*)", RegexOptions.IgnoreCase);
        private readonly Regex _titleClassicRegex = new Regex(@"(\[[^\]]*\])?\[Cap\.(\d{1,2})(\d{2})([_-](\d{1,2})(\d{2}))?\]", RegexOptions.IgnoreCase);
        private readonly Regex _titleClassicTvQualityRegex = new Regex(@"\[([^\]]*HDTV[^\]]*)", RegexOptions.IgnoreCase);
        private readonly Regex _titleYearRegex = new Regex(@"[\[\(] *(\d{4}) *[\]\)]");
        private readonly DownloadMatcher[] _downloadMatchers = new DownloadMatcher[]
        {
            new DownloadMatcher()
            {
                MatchRegex = new Regex("(/descargar-torrent/[^\"]+)\"")
            },
            new DownloadMatcher()
            {
                MatchRegex = new Regex(@"nalt\s*=\s*'([^\/]*)"),
                MatchEvaluator = m => string.Format("/download/{0}.torrent", m.Groups[1])
            },
        };

        private readonly int _maxDailyPages = 4;
        private readonly int _maxMoviesPages = 10;
        private readonly int[] _allTvCategories = (new TorznabCategory[] { TorznabCatType.TV }).Concat(TorznabCatType.TV.SubCategories).Select(c => c.ID).ToArray();
        private readonly int[] _allMoviesCategories = (new TorznabCategory[] { TorznabCatType.Movies }).Concat(TorznabCatType.Movies.SubCategories).Select(c => c.ID).ToArray();
        private readonly int _firstYearAllowed = 1885;
        private readonly int _lastYearAllowedFromNow = 3;

        private bool _includeVo;
        private bool _filterMovies;
        private bool _removeMovieAccents;
        private bool _removeMovieYear;
        private DateTime _dailyNow;
        private int _dailyResultIdx;

        private readonly string _searchJsonUrl = "get/result/";
        private readonly string _dailyUrl = "ultimas-descargas/pg/{0}";
        private readonly string[] _seriesLetterUrls = { "series/letter/{0}", "series-hd/letter/{0}" };
        private readonly string[] _seriesVoLetterUrls = { "series-vo/letter/{0}" };
        private readonly string[] _voUrls = { "serie-vo", "serievo" };

        public override string[] AlternativeSiteLinks { get; protected set; } = {
            "https://descargas2020.org/",
            "https://pctnew.org/",
            "https://pctreload.com/"
        };

        public override string[] LegacySiteLinks { get; protected set; } = {
            "http://descargas2020.com/",
            "http://www.tvsinpagar.com/",
            "http://torrentlocura.com/",
            "https://pctnew.site",
            "https://descargas2020.site",
            "http://torrentrapid.com/",
            "http://tumejortorrent.com/",
            "http://pctnew.com/",
        };

        public Newpct(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base("Newpct",
                description: "Newpct - Descargar peliculas, series y estrenos torrent gratis",
                link: "https://descargas2020.org/",
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

            var filterMoviesItem = new BoolItem() { Name = "Only full match movies", Value = true };
            configData.AddDynamic("FilterMovies", filterMoviesItem);

            var removeMovieAccentsItem = new BoolItem() { Name = "Remove accents in movie searches", Value = true };
            configData.AddDynamic("RemoveMovieAccents", removeMovieAccentsItem);

            var removeMovieYearItem = new BoolItem() { Name = "Remove year from movie results", Value = false };
            configData.AddDynamic("RemoveMovieYear", removeMovieYearItem);
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);

            // TODO: must be a simpler way to set the configured SiteLink
            SiteLink = configData.SiteLink.Value;

            var releases = await PerformQuery(new TorznabQuery());

            await ConfigureIfOK(string.Empty, releases.Any(), () =>
                                    throw new Exception("Could not find releases from this URL"));

            return IndexerConfigurationStatus.Completed;
        }

        public override async Task<byte[]> Download(Uri linkParam)
        {
            var results = await RequestStringWithCookiesAndRetry(linkParam.AbsoluteUri);

            var uriLink = ExtractDownloadUri(results.Content, linkParam.AbsoluteUri);
            if (uriLink == null)
                throw new Exception("Download link not found!");

            return await base.Download(uriLink);
        }

        private Uri ExtractDownloadUri(string content, string baseLink)
        {
            foreach (var matcher in _downloadMatchers)
            {
                var match = matcher.MatchRegex.Match(content);
                if (match.Success)
                {
                    string linkText;

                    if (matcher.MatchEvaluator != null)
                        linkText = (string)matcher.MatchEvaluator.DynamicInvoke(match);
                    else
                        linkText = match.Groups[1].Value;

                    return new Uri(new Uri(baseLink), linkText);
                }
            }

            return null;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            _includeVo = ((BoolItem)configData.GetDynamic("IncludeVo")).Value;
            _filterMovies = ((BoolItem)configData.GetDynamic("FilterMovies")).Value;
            _removeMovieAccents = ((BoolItem)configData.GetDynamic("RemoveMovieAccents")).Value;
            _removeMovieYear = ((BoolItem)configData.GetDynamic("RemoveMovieYear")).Value;
            _dailyNow = DateTime.Now;
            _dailyResultIdx = 0;
            var rssMode = string.IsNullOrEmpty(query.SanitizedSearchTerm);

            if (rssMode)
            {
                var pg = 1;
                while (pg <= _maxDailyPages)
                {
                    var pageUrl = SiteLink + string.Format(_dailyUrl, pg);
                    var results = await RequestStringWithCookiesAndRetry(pageUrl);
                    if (results == null || string.IsNullOrEmpty(results.Content))
                        break;

                    var items = ParseDailyContent(results.Content);
                    if (items == null || !items.Any())
                        break;

                    releases.AddRange(items);
                    pg++;
                }
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

        private async Task<IEnumerable<ReleaseInfo>> GetReleasesFromUri(Uri uri, string seriesName)
        {
            var releases = new List<ReleaseInfo>();

            // Episodes list
            var results = await RequestStringWithCookiesAndRetry(uri.AbsoluteUri);
            var seriesEpisodesUrl = ParseSeriesListContent(results.Content, seriesName);

            // TV serie list
            if (!string.IsNullOrEmpty(seriesEpisodesUrl))
            {
                results = await RequestStringWithCookiesAndRetry(seriesEpisodesUrl);
                var items = ParseEpisodesListContent(results.Content);
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

        private IEnumerable<NewpctRelease> ParseDailyContent(string content)
        {
            var parser = new HtmlParser();
            var doc = parser.ParseDocument(content);

            var releases = new List<NewpctRelease>();

            try
            {
                var rows = doc.QuerySelectorAll(".content .info");
                foreach (var row in rows)
                {
                    var anchor = row.QuerySelector("a");
                    var title = Regex.Replace(anchor.TextContent, @"\s+", " ").Trim();
                    var title2 = Regex.Replace(anchor.GetAttribute("title"), @"\s+", " ").Trim();
                    if (title2.Length >= title.Length)
                        title = title2;

                    var detailsUrl = anchor.GetAttribute("href");
                    if (!_includeVo && _voUrls.Any(vo => detailsUrl.ToLower().Contains(vo.ToLower())))
                        continue;

                    var span = row.QuerySelector("span");
                    var quality = span.ChildNodes[0].TextContent.Trim();
                    var releaseType = ReleaseTypeFromQuality(quality);
                    var sizeText = span.ChildNodes[1].TextContent.Replace("Tama\u00F1o", "").Trim();

                    var div = row.QuerySelector("div");
                    var language = div.ChildNodes[1].TextContent.Trim();
                    _dailyResultIdx++;

                    var rSize = ReleaseInfo.GetBytes(sizeText);
                    var rPublishDate = _dailyNow - TimeSpan.FromMilliseconds(_dailyResultIdx);
                    var rTitle = releaseType == ReleaseType.Tv
                        ? $"Serie {title} - {language} Calidad [{quality}]"
                        : $"{title} [{quality}][{language}]";

                    var release = GetReleaseFromData(releaseType, rTitle, detailsUrl, quality, language, rSize, rPublishDate);
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
            var parser = new HtmlParser();
            var doc = parser.ParseDocument(content);

            try
            {
                var rows = doc.QuerySelectorAll(".pelilist li a");
                foreach (var anchor in rows)
                {
                    var h2 = anchor.QuerySelector("h2");
                    if (h2.TextContent.Trim().ToLower() == title.Trim().ToLower())
                        return anchor.GetAttribute("href");
                }
            }
            catch (Exception ex)
            {
                OnParseError(content, ex);
            }

            return null;
        }

        private IEnumerable<NewpctRelease> ParseEpisodesListContent(string content)
        {
            var parser = new HtmlParser();
            var doc = parser.ParseDocument(content);

            var releases = new List<NewpctRelease>();

            try
            {
                var rows = doc.QuerySelectorAll(".content .info");
                foreach (var row in rows)
                {
                    var anchor = row.QuerySelector("a");
                    var title = anchor.TextContent.Replace("\t", "").Trim();
                    var detailsUrl = anchor.GetAttribute("href");

                    var pubDateText = row.ChildNodes[3].TextContent.Trim();
                    var sizeText = row.ChildNodes[5].TextContent.Trim();

                    var size = ReleaseInfo.GetBytes(sizeText);
                    var publishDate = DateTime.ParseExact(pubDateText, "dd-MM-yyyy", null);

                    var release = GetReleaseFromData(ReleaseType.Tv, title, detailsUrl, null, null, size, publishDate);
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

            var searchJsonUrl = SiteLink + _searchJsonUrl;

            var pg = 1;
            while (pg <= _maxMoviesPages)
            {
                var queryCollection = new Dictionary<string, string>
                {
                    {"s", searchStr},
                    {"pg", pg.ToString()}
                };

                var results = await PostDataWithCookies(searchJsonUrl, queryCollection);
                if (results == null || string.IsNullOrEmpty(results.Content))
                    break;
                var items = ParseSearchJsonContent(results.Content);
                if (items == null)
                    break;

                releases.AddRange(items);
                pg++;
            }

            ScoreReleases(releases, searchStr);

            if (_filterMovies)
                releases = releases.Where(r => r.Score < _wordNotFoundScore).ToList();

            return releases;
        }

        private IEnumerable<NewpctRelease> ParseSearchJsonContent(string content)
        {
            var someFound = false;
            var releases = new List<NewpctRelease>();

            try
            {
                var jo = JObject.Parse(content);

                var numItems = int.Parse(jo["data"]["items"].ToString());
                for (var i = 0; i < numItems; i++)
                {
                    var item = jo["data"]["torrents"]["0"][i.ToString()];

                    var url = item["guid"].ToString();
                    var title = item["torrentName"].ToString();
                    var pubDateText = item["torrentDateAdded"].ToString();
                    var calidad = item["calidad"].ToString();
                    var sizeText = item["torrentSize"].ToString();

                    someFound = true;

                    var isSeries = calidad != null && calidad.ToLower().Contains("hdtv");
                    var isGame = title.ToLower().Contains("pcdvd");
                    if (isSeries || isGame)
                        continue;

                    long size = 0;
                    try
                    {
                        size = ReleaseInfo.GetBytes(sizeText);
                    }
                    catch
                    {
                    }
                    DateTime.TryParseExact(pubDateText, "dd/MM/yyyy", null, DateTimeStyles.None, out var publishDate);

                    var detailsUrl = SiteLink + url;

                    var release = GetReleaseFromData(ReleaseType.Movie, title, detailsUrl, calidad, null, size, publishDate);
                    releases.Add(release);
                }
            }
            catch (Exception)
            {
                return null;
            }

            if (!someFound)
                return null;

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
                    {
                        release.Score += _wordNotFoundScore;
                    }
                }
            }
        }

        private static ReleaseType ReleaseTypeFromQuality(string quality) =>
            quality.Trim().ToLower().StartsWith("hdtv")
                ? ReleaseType.Tv
                : ReleaseType.Movie;

        private NewpctRelease GetReleaseFromData(ReleaseType releaseType, string title, string detailsUrl, string quality,
                                                 string language, long size, DateTime publishDate)
        {
            var result = new NewpctRelease
            {
                NewpctReleaseType = releaseType
            };

            //Sanitize
            title = title.Replace("\t", "").Replace("\x2013", "-");

            var match = _titleListRegex.Match(title);
            if (match.Success)
            {
                result.SeriesName = match.Groups[2].Value.Trim(' ', '-');
                result.Season = int.Parse(match.Groups[5].Success ? match.Groups[5].Value.Trim() : "1");
                result.Episode = int.Parse(match.Groups[8].Value.Trim().PadLeft(2, '0'));
                result.EpisodeTo = match.Groups[11].Success ? (int?)int.Parse(match.Groups[11].Value.Trim()) : null;
                var audioQuality = match.Groups[13].Value.Trim(' ', '[', ']');
                if (string.IsNullOrEmpty(language))
                    language = audioQuality;
                quality = match.Groups[14].Value.Trim(' ', '[', ']');

                var seasonText = result.Season.ToString();
                var episodeText = seasonText + result.Episode.ToString().PadLeft(2, '0');
                var episodeToText = result.EpisodeTo.HasValue ? "_" + seasonText + result.EpisodeTo.ToString().PadLeft(2, '0') : "";

                result.Title = string.Format("{0} - Temporada {1} [{2}][Cap.{3}{4}][{5}]",
                    result.SeriesName, seasonText, quality, episodeText, episodeToText, audioQuality);
            }
            else
            {
                var matchClassic = _titleClassicRegex.Match(title);
                if (matchClassic.Success)
                {
                    result.Season = matchClassic.Groups[2].Success ? (int?)int.Parse(matchClassic.Groups[2].Value) : null;
                    result.Episode = matchClassic.Groups[3].Success ? (int?)int.Parse(matchClassic.Groups[3].Value) : null;
                    result.EpisodeTo = matchClassic.Groups[6].Success ? (int?)int.Parse(matchClassic.Groups[6].Value) : null;
                    if (matchClassic.Groups[1].Success)
                        quality = matchClassic.Groups[1].Value;
                }

                result.Title = title;
            }

            if (releaseType == ReleaseType.Tv)
            {
                if (!string.IsNullOrWhiteSpace(quality) && (quality.Contains("720") || quality.Contains("1080")))
                    result.Category = new List<int> { TorznabCatType.TVHD.ID };
                else
                    result.Category = new List<int> { TorznabCatType.TV.ID };
            }
            else
            {
                result.Title = title;
                result.Category = new List<int> { TorznabCatType.Movies.ID };
            }

            // TODO: add banner

            if (size > 0)
                result.Size = size;
            result.Link = new Uri(detailsUrl);
            result.Guid = result.Link;
            result.Comments = result.Link;
            result.PublishDate = publishDate;
            result.Seeders = 1;
            result.Peers = 1;

            result.Title = FixedTitle(result, quality, language);
            result.MinimumRatio = 1;
            result.MinimumSeedTime = 172800; // 48 hours
            result.DownloadVolumeFactor = 0;
            result.UploadVolumeFactor = 1;

            return result;
        }

        private string FixedTitle(NewpctRelease release, string quality, string language)
        {
            if (string.IsNullOrEmpty(release.SeriesName))
            {
                release.SeriesName = release.Title;
                if (release.NewpctReleaseType == ReleaseType.Tv && release.SeriesName.Contains("-"))
                    release.SeriesName = release.Title.Substring(0, release.SeriesName.IndexOf('-') - 1);
            }

            var titleParts = new List<string>
            {
                release.SeriesName
            };

            if (release.NewpctReleaseType == ReleaseType.Tv)
            {
                if (string.IsNullOrEmpty(quality))
                    quality = "HDTV";

                var seasonAndEpisode = "S" + release.Season.ToString().PadLeft(2, '0');
                seasonAndEpisode += "E" + release.Episode.ToString().PadLeft(2, '0');
                if (release.EpisodeTo != release.Episode && release.EpisodeTo != null && release.EpisodeTo != 0)
                {
                    seasonAndEpisode += "-" + release.EpisodeTo.ToString().PadLeft(2, '0');
                }
                titleParts.Add(seasonAndEpisode);
            }

            if (!string.IsNullOrEmpty(quality) && !release.SeriesName.Contains(quality))
            {
                titleParts.Add(quality);
            }

            if (!string.IsNullOrWhiteSpace(language) && !release.SeriesName.Contains(language))
            {
                titleParts.Add(language);
            }

            if (release.Title.ToLower().Contains("espa\u00F1ol") ||
                release.Title.ToLower().Contains("espanol") ||
                release.Title.ToLower().Contains("castellano") ||
                release.Title.ToLower().EndsWith("espa"))
            {
                titleParts.Add("Spanish");
            }

            var result = string.Join(".", titleParts);

            if (release.NewpctReleaseType == ReleaseType.Movie)
            {
                if (_removeMovieYear)
                {
                    Match match = _titleYearRegex.Match(result);
                    if (match.Success)
                    {
                        int year = int.Parse(match.Groups[1].Value);
                        if (year >= _firstYearAllowed && year <= DateTime.Now.Year + _lastYearAllowedFromNow)
                            result = result.Replace(match.Groups[0].Value, "");
                    }
                }
            }

            result = Regex.Replace(result, @"[\[\]]+", ".");
            result = Regex.Replace(result, @"\.[ \.]*\.", ".");

            return result;
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
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
