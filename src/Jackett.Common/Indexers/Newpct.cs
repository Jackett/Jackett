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
using Jackett.Common.Utils;
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
            TV,
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

        private static readonly Uri DefaultSiteLinkUri =
            new Uri("https://descargas2020.org");

        private static readonly Uri[] ExtraSiteLinkUris = new Uri[]
        {
            new Uri("https://pctnew.org"),
        };

        private static readonly Uri[] LegacySiteLinkUris = new Uri[]
        {
            new Uri("http://descargas2020.com/"),
            new Uri("http://www.tvsinpagar.com/"),
            new Uri("http://torrentlocura.com/"),
            new Uri("https://pctnew.site"),
            new Uri("https://descargas2020.site"),
            new Uri("http://torrentrapid.com/"),
            new Uri("http://tumejortorrent.com/"),
            new Uri("http://pctnew.com/"),
        };

        private NewpctRelease _mostRecentRelease;
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

        private readonly int _maxDailyPages = 7;
        private readonly int _maxMoviesPages = 30;
        private readonly int _maxEpisodesListPages = 100;
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

        private readonly string _searchUrl = "/buscar";
        private readonly string _searchJsonUrl = "/get/result/";
        private readonly string _dailyUrl = "/ultimas-descargas/pg/{0}";
        private readonly string[] _seriesLetterUrls = new string[] { "/series/letter/{0}", "/series-hd/letter/{0}" };
        private readonly string[] _seriesVOLetterUrls = new string[] { "/series-vo/letter/{0}" };
        private readonly string _seriesUrl = "{0}/pg/{1}";
        private readonly string[] _voUrls = new string[] { "serie-vo", "serievo" };

        public override string[] LegacySiteLinks { get; protected set; } = LegacySiteLinkUris.Select(u => u.AbsoluteUri).ToArray();

        public Newpct(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(name: "Newpct",
                description: "Newpct - descargar torrent peliculas, series",
                link: DefaultSiteLinkUri.AbsoluteUri,
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

            var removeMovieAccentsItem = new BoolItem() { Name = "Remove accents in movie searchs", Value = true };
            configData.AddDynamic("RemoveMovieAccents", removeMovieAccentsItem);

            var removeMovieYearItem = new BoolItem() { Name = "Remove year from movie results", Value = false };
            configData.AddDynamic("RemoveMovieYear", removeMovieYearItem);
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);
            var releases = await PerformQuery(new TorznabQuery());

            await ConfigureIfOK(string.Empty, releases.Any(), () =>
                                    throw new Exception("Could not find releases from this URL"));

            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var link = new Uri(configData.SiteLink.Value);

            lock (cache)
            {
                CleanCache();
            }

            return await PerformQuery(link, query, 0);
        }

        public override async Task<byte[]> Download(Uri linkParam)
        {
            var uris = GetLinkUris(linkParam);

            foreach (var uri in uris)
            {
                byte[] result = null;

                try
                {
                    var results = await RequestStringWithCookiesAndRetry(uri.AbsoluteUri);
                    await FollowIfRedirect(results);
                    var content = results.Content;

                    if (content != null)
                    {
                        var uriLink = ExtractDownloadUri(content, uri.AbsoluteUri);
                        if (uriLink != null)
                            result = await base.Download(uriLink);
                    }
                }
                catch
                {
                }

                if (result != null)
                    return result;
                else
                    logger.Warn("Newpct - download link not found in " + uri.LocalPath);
            }

            return null;
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

        private IEnumerable<Uri> GetLinkUris(Uri referenceLink)
        {
            var uris = new List<Uri>();
            uris.Add(referenceLink);
            if (DefaultSiteLinkUri.Scheme != referenceLink.Scheme && DefaultSiteLinkUri.Host != referenceLink.Host)
                uris.Add(DefaultSiteLinkUri);

            uris = uris.Concat(ExtraSiteLinkUris.
                Where(u =>
                    (u.Scheme != referenceLink.Scheme || u.Host != referenceLink.Host) &&
                    (u.Scheme != DefaultSiteLinkUri.Scheme || u.Host != DefaultSiteLinkUri.Host))).ToList();

            var result = new List<Uri>();

            foreach (var uri in uris)
            {
                var ub = new UriBuilder(uri);
                ub.Path = referenceLink.LocalPath;
                result.Add(ub.Uri);
            }

            return result;
        }

        private async Task<IEnumerable<ReleaseInfo>> PerformQuery(Uri siteLink, TorznabQuery query, int attempts)
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
                Uri validUri = null;
                while (pg <= _maxDailyPages)
                {
                    IEnumerable<NewpctRelease> items = null;
                    WebClientStringResult results = null;

                    if (validUri != null)
                    {
                        var uri = new Uri(validUri, string.Format(_dailyUrl, pg));
                        results = await RequestStringWithCookiesAndRetry(uri.AbsoluteUri);
                        if (results == null || string.IsNullOrEmpty(results.Content))
                            break;
                        await FollowIfRedirect(results);
                        items = ParseDailyContent(results.Content);
                    }
                    else
                    {
                        foreach (var uri in GetLinkUris(new Uri(siteLink, string.Format(_dailyUrl, pg))))
                        {
                            results = await RequestStringWithCookiesAndRetry(uri.AbsoluteUri);
                            if (results != null && !string.IsNullOrEmpty(results.Content))
                            {
                                await FollowIfRedirect(results);
                                items = ParseDailyContent(results.Content);
                                if (items != null && items.Any())
                                {
                                    validUri = uri;
                                    break;
                                }
                            }
                        }
                    }

                    if (items == null || !items.Any())
                        break;

                    releases.AddRange(items);

                    //Check if we need to go to next page
                    var recentFound = _mostRecentRelease != null &&
                        items.Any(r => r.Title == _mostRecentRelease.Title && r.Link.AbsoluteUri == _mostRecentRelease.Link.AbsoluteUri);
                    if (pg == 1)
                        _mostRecentRelease = (NewpctRelease)items.First().Clone();
                    if (recentFound)
                        break;

                    pg++;
                }
            }
            else
            {
                var isTvSearch = query.Categories == null || query.Categories.Length == 0 ||
                    query.Categories.Any(c => _allTvCategories.Contains(c));
                if (isTvSearch)
                {
                    releases.AddRange(await TvSearch(siteLink, query));
                }

                var isMovieSearch = query.Categories == null || query.Categories.Length == 0 ||
                    query.Categories.Any(c => _allMoviesCategories.Contains(c));
                if (isMovieSearch)
                {
                    releases.AddRange(await MovieSearch(siteLink, query));
                }
            }

            // Database lost on 2018/04/05, all previous torrents don't have download links
            var failureDay = new DateTime(2018, 04, 05);
            releases = releases.Where(r => r.PublishDate > failureDay).ToList();

            return releases;
        }

        private async Task<IEnumerable<ReleaseInfo>> TvSearch(Uri siteLink, TorznabQuery query)
        {
            List<ReleaseInfo> newpctReleases = null;

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

            //Try to reuse cache
            lock (cache)
            {
                var cachedResult = cache.FirstOrDefault(i => i.Query == seriesName.ToLower());
                if (cachedResult != null)
                    newpctReleases = cachedResult.Results.Select(r => (ReleaseInfo)r.Clone()).ToList();
            }

            if (newpctReleases == null)
            {
                newpctReleases = new List<ReleaseInfo>();

                //Search series url
                foreach (var seriesListUrl in SeriesListUris(siteLink, seriesName))
                {
                    newpctReleases.AddRange(await GetReleasesFromUri(seriesListUrl, seriesName));
                }

                //Sonarr removes "the" from shows. If there is nothing try prepending "the"
                if (newpctReleases.Count == 0 && !(seriesName.ToLower().StartsWith("the")))
                {
                    seriesName = "The " + seriesName;
                    foreach (var seriesListUrl in SeriesListUris(siteLink, seriesName))
                    {
                        newpctReleases.AddRange(await GetReleasesFromUri(seriesListUrl, seriesName));
                    }
                }

                //Cache ALL episodes
                lock (cache)
                {
                    cache.Add(new CachedQueryResult(seriesName.ToLower(), newpctReleases));
                }
            }

            // remove duplicates
            newpctReleases = newpctReleases.GroupBy(x => x.Guid).Select(y => y.First()).ToList();

            //Filter only episodes needed
            return newpctReleases.Where(r =>
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
            var newpctReleases = new List<ReleaseInfo>();
            var results = await RequestStringWithCookiesAndRetry(uri.AbsoluteUri);
            await FollowIfRedirect(results);

            //Episodes list
            var seriesEpisodesUrl = ParseSeriesListContent(results.Content, seriesName);
            if (!string.IsNullOrEmpty(seriesEpisodesUrl))
            {
                var pg = 1;
                while (pg < _maxEpisodesListPages)
                {
                    var episodesListUrl = new Uri(string.Format(_seriesUrl, seriesEpisodesUrl, pg));
                    results = await RequestStringWithCookiesAndRetry(episodesListUrl.AbsoluteUri);
                    await FollowIfRedirect(results);

                    var items = ParseEpisodesListContent(results.Content);
                    if (items == null || !items.Any())
                        break;

                    newpctReleases.AddRange(items);

                    pg++;
                }
            }
            return newpctReleases;
        }

        private IEnumerable<Uri> SeriesListUris(Uri siteLink, string seriesName)
        {
            IEnumerable<string> lettersUrl;
            if (!_includeVo)
            {
                lettersUrl = _seriesLetterUrls;
            }
            else
            {
                lettersUrl = _seriesLetterUrls.Concat(_seriesVOLetterUrls);
            }
            var seriesLetter = !char.IsDigit(seriesName[0]) ? seriesName[0].ToString() : "0-9";
            return lettersUrl.Select(urlFormat =>
            {
                return new Uri(siteLink, string.Format(urlFormat, seriesLetter.ToLower()));
            });
        }

        private IEnumerable<NewpctRelease> ParseDailyContent(string content)
        {
            var SearchResultParser = new HtmlParser();
            var doc = SearchResultParser.ParseDocument(content);

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

                    NewpctRelease newpctRelease;
                    if (releaseType == ReleaseType.TV)
                        newpctRelease = GetReleaseFromData(releaseType,
                        string.Format("Serie {0} - {1} Calidad [{2}]", title, language, quality),
                        detailsUrl, quality, language, ReleaseInfo.GetBytes(sizeText), _dailyNow - TimeSpan.FromMilliseconds(_dailyResultIdx));
                    else
                        newpctRelease = GetReleaseFromData(releaseType,
                        string.Format("{0} [{1}][{2}]", title, quality, language),
                        detailsUrl, quality, language, ReleaseInfo.GetBytes(sizeText), _dailyNow - TimeSpan.FromMilliseconds(_dailyResultIdx));

                    releases.Add(newpctRelease);
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
            var SearchResultParser = new HtmlParser();
            var doc = SearchResultParser.ParseDocument(content);

            var results = new Dictionary<string, string>();

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
            var SearchResultParser = new HtmlParser();
            var doc = SearchResultParser.ParseDocument(content);

            var releases = new List<NewpctRelease>();

            try
            {
                var rows = doc.QuerySelectorAll(".content .info");
                foreach (var row in rows)
                {
                    var anchor = row.QuerySelector("a");
                    var title = anchor.TextContent.Replace("\t", "").Trim();
                    var detailsUrl = anchor.GetAttribute("href");

                    var span = row.QuerySelector("span");
                    var pubDateText = row.ChildNodes[3].TextContent.Trim();
                    var sizeText = row.ChildNodes[5].TextContent.Trim();

                    var size = ReleaseInfo.GetBytes(sizeText);
                    var publishDate = DateTime.ParseExact(pubDateText, "dd-MM-yyyy", null);
                    var newpctRelease = GetReleaseFromData(ReleaseType.TV, title, detailsUrl, null, null, size, publishDate);

                    releases.Add(newpctRelease);
                }
            }
            catch (Exception ex)
            {
                OnParseError(content, ex);
            }

            return releases;
        }

        private async Task<IEnumerable<ReleaseInfo>> MovieSearch(Uri siteLink, TorznabQuery query)
        {
            var releases = new List<NewpctRelease>();

            var searchStr = query.SanitizedSearchTerm;
            if (_removeMovieAccents)
                searchStr = RemoveDiacritics(searchStr);

            Uri validUri = null;
            var validUriUsesJson = false;
            var pg = 1;
            while (pg <= _maxMoviesPages)
            {
                var queryCollection = new Dictionary<string, string>();
                queryCollection.Add("q", searchStr);
                queryCollection.Add("s", searchStr);
                queryCollection.Add("pg", pg.ToString());

                WebClientStringResult results = null;
                IEnumerable<NewpctRelease> items = null;

                if (validUri != null)
                {
                    if (validUriUsesJson)
                    {
                        var uri = new Uri(validUri, _searchJsonUrl);
                        results = await PostDataWithCookies(uri.AbsoluteUri, queryCollection);
                        if (results == null || string.IsNullOrEmpty(results.Content))
                            break;
                        items = ParseSearchJsonContent(uri, results.Content);
                    }
                    else
                    {
                        var uri = new Uri(validUri, _searchUrl);
                        results = await PostDataWithCookies(uri.AbsoluteUri, queryCollection);
                        if (results == null || string.IsNullOrEmpty(results.Content))
                            break;
                        items = ParseSearchContent(results.Content);
                    }
                }
                else
                {
                    using (var jsonUris = GetLinkUris(new Uri(siteLink, _searchJsonUrl)).GetEnumerator())
                    {
                        using (var uris = GetLinkUris(new Uri(siteLink, _searchUrl)).GetEnumerator())
                        {
                            var resultFound = false;
                            while (jsonUris.MoveNext() && uris.MoveNext() && !resultFound)
                            {
                                for (var i = 0; i < 2 && !resultFound; i++)
                                {
                                    var usingJson = i == 0;

                                    Uri uri;
                                    if (usingJson)
                                        uri = jsonUris.Current;
                                    else
                                        uri = uris.Current;

                                    try
                                    {
                                        results = await PostDataWithCookies(uri.AbsoluteUri, queryCollection);
                                    }
                                    catch
                                    {
                                        results = null;
                                    }

                                    if (results != null && !string.IsNullOrEmpty(results.Content))
                                    {
                                        if (usingJson)
                                            items = ParseSearchJsonContent(uri, results.Content);
                                        else
                                            items = ParseSearchContent(results.Content);

                                        if (items != null)
                                        {
                                            validUri = uri;
                                            validUriUsesJson = usingJson;
                                            resultFound = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

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

        private IEnumerable<NewpctRelease> ParseSearchContent(string content)
        {
            var someFound = false;
            var SearchResultParser = new HtmlParser();
            var doc = SearchResultParser.ParseDocument(content);

            var releases = new List<NewpctRelease>();

            try
            {
                var rows = doc.QuerySelectorAll(".content .info");
                if (rows == null || !rows.Any())
                    return null;
                foreach (var row in rows)
                {
                    var anchor = row.QuerySelector("a");
                    var h2 = anchor.QuerySelector("h2");
                    var title = Regex.Replace(h2.TextContent, @"\s+", " ").Trim();
                    var detailsUrl = anchor.GetAttribute("href");

                    someFound = true;

                    var isSeries = h2.QuerySelector("span") != null && h2.TextContent.ToLower().Contains("calidad");
                    var isGame = title.ToLower().Contains("pcdvd");
                    if (isSeries || isGame)
                        continue;

                    var span = row.QuerySelectorAll("span");

                    var pubDateText = span[1].TextContent.Trim();
                    var sizeText = span[2].TextContent.Trim();

                    long size = 0;
                    try
                    {
                        size = ReleaseInfo.GetBytes(sizeText);
                    }
                    catch
                    {
                    }
                    DateTime.TryParseExact(pubDateText, "dd-MM-yyyy", null, DateTimeStyles.None, out var publishDate);

                    var div = row.QuerySelector("div");

                    NewpctRelease newpctRelease;
                    newpctRelease = GetReleaseFromData(ReleaseType.Movie, title, detailsUrl, null, null, size, publishDate);

                    releases.Add(newpctRelease);
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

        private IEnumerable<NewpctRelease> ParseSearchJsonContent(Uri uri, string content)
        {
            var someFound = false;

            var releases = new List<NewpctRelease>();

            //Remove path from uri
            var ub = new UriBuilder(uri);
            ub.Path = string.Empty;
            uri = ub.Uri;

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

                    NewpctRelease newpctRelease;
                    var detailsUrl = new Uri(uri, url).AbsoluteUri;
                    newpctRelease = GetReleaseFromData(ReleaseType.Movie, title, detailsUrl, calidad, null, size, publishDate);

                    releases.Add(newpctRelease);

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
                ? ReleaseType.TV
                : ReleaseType.Movie;

        private NewpctRelease GetReleaseFromData(ReleaseType releaseType, string title, string detailsUrl, string quality, string language, long size, DateTime publishDate)
        {
            var result = new NewpctRelease();
            result.NewpctReleaseType = releaseType;

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

            if (releaseType == ReleaseType.TV)
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
                if (release.NewpctReleaseType == ReleaseType.TV && release.SeriesName.Contains("-"))
                    release.SeriesName = release.Title.Substring(0, release.SeriesName.IndexOf('-') - 1);
            }

            var titleParts = new List<string>();

            titleParts.Add(release.SeriesName);

            if (release.NewpctReleaseType == ReleaseType.TV)
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
