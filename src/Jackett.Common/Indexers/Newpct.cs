using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Parser.Html;
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
        enum ReleaseType
        {
            TV,
            Movie,
        }

        class NewpctRelease : ReleaseInfo
        {
            public string SerieName;
            public int? Season;
            public int? Episode;
            public int? EpisodeTo;
        }

        private static Uri SiteLinkUri = new Uri("http://www.tvsinpagar.com/");
        private ReleaseInfo _mostRecentRelease;
        private Regex _searchStringRegex = new Regex(@"(.+?)S0?(\d+)(E0?(\d+))?$", RegexOptions.IgnoreCase);
        private Regex _titleListRegex = new Regex(@"Serie(.+?)(Temporada(.+?)(\d+)(.+?))?Capitulos?(.+?)(\d+)((.+?)(\d+))?(.+?)-(.+?)Calidad(.*)", RegexOptions.IgnoreCase);
        private Regex _titleClassicRegex = new Regex(@"(\[[^\]]*\])?\[Cap\.(\d{1,2})(\d{2})([_-](\d{1,2})(\d{2}))?\]", RegexOptions.IgnoreCase);
        private Regex _titleClassicTvQualityRegex = new Regex(@"\[([^\]]*HDTV[^\]]*)", RegexOptions.IgnoreCase);

        private int _maxDailyPages = 7;
        private int _maxEpisodesListPages = 100;
        private int[] _allTvCategories = TorznabCatType.TV.SubCategories.Select(c => c.ID).ToArray();

        private string _dailyUrl = "/ultimas-descargas/pg/{0}";
        private string[] _seriesLetterUrls = new string[] { "/series/letter/{0}", "/series-hd/letter/{0}" };
        private string[] _seriesVOLetterUrls = new string[] { "/series-vo/letter/{0}" };
        private string _seriesUrl = "{0}/pg/{1}";

        public Newpct(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(name: "Newpct",
                description: "Newpct - descargar torrent peliculas, series",
                link: SiteLinkUri.AbsoluteUri,
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
            SiteLinkUri = new Uri(configData.SiteLink.Value);

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

        public override async Task<byte[]> Download(Uri link)
        {
            var results = await RequestStringWithCookies(link.AbsoluteUri);
            var content = results.Content;

            Regex regex = new Regex("[^\"]*/descargar-torrent/\\d+_[^\"]*");
            Match match = regex.Match(content);
            if (match.Success)
                link = new Uri(match.Groups[0].Value);
            else
                this.logger.Warn("Newpct - download link not found in " + link);

            return await base.Download(link);
        }

        private async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query, int attempts)
        {
            var releases = new List<ReleaseInfo>();

            bool rssMode = string.IsNullOrEmpty(query.SanitizedSearchTerm);

            if (rssMode)
            {
                int pg = 1;
                while (pg <= _maxDailyPages)
                {
                    Uri url = new Uri(SiteLinkUri, string.Format(_dailyUrl, pg));
                    var results = await RequestStringWithCookies(url.AbsoluteUri);

                    var items = ParseDailyContent(results.Content);
                    if (items == null || !items.Any())
                        break;

                    releases.AddRange(items);

                    //Check if we need to go to next page
                    bool recentFound = _mostRecentRelease != null &&
                        items.Any(r => r.Title == _mostRecentRelease.Title && r.Link.AbsoluteUri == _mostRecentRelease.Link.AbsoluteUri);
                    if (pg == 1)
                        _mostRecentRelease = (ReleaseInfo)items.First().Clone();
                    if (recentFound)
                        break;

                    pg++;
                }
            }
            else
            {
                //Only tv search supported. (newpct web search is useless)
                bool isTvSearch = query.Categories == null || query.Categories.Length == 0 ||
                    query.Categories.Any(c => _allTvCategories.Contains(c));
                if (isTvSearch)
                {
                    return await TvSearch(query);
                }
            }

            return releases;
        }

        private async Task<IEnumerable<ReleaseInfo>> TvSearch(TorznabQuery query)
        {
            var newpctReleases = new List<ReleaseInfo>();

            string seriesName = query.SanitizedSearchTerm;
            int? season = query.Season > 0 ? (int?)query.Season : null;
            int? episode = null;
            if (!string.IsNullOrWhiteSpace(query.Episode) && int.TryParse(query.Episode, out int episodeTemp))
                episode = episodeTemp;

            //If query has no season/episode info, try to parse title
            if (season == null && episode == null)
            {
                Match searchMatch = _searchStringRegex.Match(query.SanitizedSearchTerm);
                if (searchMatch.Success)
                {
                    seriesName = searchMatch.Groups[1].Value.Trim();
                    season = int.Parse(searchMatch.Groups[2].Value);
                    episode = searchMatch.Groups[4].Success ? (int?)int.Parse(searchMatch.Groups[4].Value) : null;
                }
            }

            //Try to reuse cache
            bool cacheFound = false;
            lock (cache)
            {
                CleanCache();
                var cachedResult = cache.FirstOrDefault(i => i.Query == seriesName.ToLower());
                if (cachedResult != null && cachedResult.Results != null)
                {
                    cacheFound = true;
                    newpctReleases = cachedResult.Results.Where(r => (r as NewpctRelease) != null).ToList();
                    if (!newpctReleases.Any() && cachedResult.Results.Any())
                        cacheFound = false;
                }
            }

            if (!cacheFound)
            {
                //Search series url
                foreach (Uri seriesListUrl in SeriesListUris(seriesName))
                {
                    newpctReleases.AddRange(await GetReleasesFromUri(seriesListUrl, seriesName));
                }

                //Sonarr removes "the" from shows. If there is nothing try prepending "the"
                if (newpctReleases.Count == 0 && !(seriesName.ToLower().StartsWith("the")))
                {
                    seriesName = "The " + seriesName;
                    foreach (Uri seriesListUrl in SeriesListUris(seriesName))
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

            //Filter only episodes needed
            return newpctReleases.Where(r =>
            {
                NewpctRelease nr = r as NewpctRelease;
                return nr.Season.HasValue != season.HasValue || //Can't determine if same season
                    nr.Season.HasValue && season.Value == nr.Season.Value && //Same season and ...
                    (
                        nr.Episode.HasValue != episode.HasValue || //Can't determine if same episode
                        nr.Episode.HasValue &&
                        (
                            nr.Episode.Value == episode.Value || //Same episode
                            nr.EpisodeTo.HasValue && episode.Value >= nr.Episode.Value && episode.Value <= nr.EpisodeTo.Value //Episode in interval
                        )
                    );
            });
        }

        private async Task<IEnumerable<ReleaseInfo>> GetReleasesFromUri(Uri uri, string seriesName)
        {
            var newpctReleases = new List<ReleaseInfo>();
            var results = await RequestStringWithCookies(uri.AbsoluteUri);

            //Episodes list
            string seriesEpisodesUrl = ParseSeriesListContent(results.Content, seriesName);
            if (!string.IsNullOrEmpty(seriesEpisodesUrl))
            {
                int pg = 1;
                while (pg < _maxEpisodesListPages)
                {
                    Uri episodesListUrl = new Uri(string.Format(_seriesUrl, seriesEpisodesUrl, pg));
                    results = await RequestStringWithCookies(episodesListUrl.AbsoluteUri);

                    var items = ParseEpisodesListContent(results.Content);
                    if (items == null || !items.Any())
                        break;

                    newpctReleases.AddRange(items);

                    pg++;
                }
            }
            return newpctReleases;
        }

        private IEnumerable<Uri> SeriesListUris(string seriesName)
        {
            IEnumerable<string> lettersUrl;
            if (!((BoolItem)configData.GetDynamic("IncludeVo")).Value)
            {
                lettersUrl = _seriesLetterUrls;
            }
            else
            {
                lettersUrl = _seriesLetterUrls.Concat(_seriesVOLetterUrls);
            }
            string seriesLetter = !char.IsDigit(seriesName[0]) ? seriesName[0].ToString() : "0-9";
            return lettersUrl.Select(urlFormat =>
            {
                return new Uri(SiteLinkUri, string.Format(urlFormat, seriesLetter.ToLower()));
            });
        }

        private IEnumerable<NewpctRelease> ParseDailyContent(string content)
        {
            var SearchResultParser = new HtmlParser();
            var doc = SearchResultParser.Parse(content);

            List<NewpctRelease> releases = new List<NewpctRelease>();

            try
            {
                var rows = doc.QuerySelectorAll(".content .info");
                foreach (var row in rows)
                {
                    var anchor = row.QuerySelector("a");
                    var title = anchor.TextContent.Replace("\t", "").Trim();
                    var detailsUrl = anchor.GetAttribute("href");

                    var span = row.QuerySelector("span");
                    var quality = span.ChildNodes[0].TextContent.Trim();
                    ReleaseType releaseType = ReleaseTypeFromQuality(quality);
                    var sizeText = span.ChildNodes[1].TextContent.Replace("Tama\u00F1o", "").Trim();

                    var div = row.QuerySelector("div");
                    var language = div.ChildNodes[1].TextContent.Trim();

                    NewpctRelease newpctRelease;
                    if (releaseType == ReleaseType.TV)
                        newpctRelease = GetReleaseFromData(releaseType, 
                        string.Format("Serie {0} - {1} Calidad [{2}]", title, language, quality),
                        detailsUrl, quality, language, ReleaseInfo.GetBytes(sizeText), DateTime.Now);
                    else
                        newpctRelease = GetReleaseFromData(releaseType,
                        string.Format("{0} [{1}][{2}]", title, quality, language), 
                        detailsUrl, quality, language, ReleaseInfo.GetBytes(sizeText), DateTime.Now);

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
            var doc = SearchResultParser.Parse(content);

            Dictionary<string, string> results = new Dictionary<string, string>();

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
            var doc = SearchResultParser.Parse(content);

            List<NewpctRelease> releases = new List<NewpctRelease>();

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

                    long size = ReleaseInfo.GetBytes(sizeText);
                    DateTime publishDate = DateTime.ParseExact(pubDateText, "dd-MM-yyyy", null);
                    NewpctRelease newpctRelease = GetReleaseFromData(ReleaseType.TV, title, detailsUrl, null, null, size, publishDate);

                    releases.Add(newpctRelease);
                }
            }
            catch (Exception ex)
            {
                OnParseError(content, ex);
            }

            return releases;
        }

        ReleaseType ReleaseTypeFromQuality(string quality)
        {
            if (quality.Trim().ToLower().StartsWith("hdtv"))
                return ReleaseType.TV;
            else
                return ReleaseType.Movie;
        }

        NewpctRelease GetReleaseFromData(ReleaseType releaseType, string title, string detailsUrl, string quality, string language, long size, DateTime publishDate)
        {
            NewpctRelease result = new NewpctRelease();

            //Sanitize
            title = title.Replace("\t", "").Replace("\x2013", "-");

            Match match = _titleListRegex.Match(title);
            if (match.Success)
            {
                result.SerieName = match.Groups[1].Value.Trim(' ', '-');
                result.Season = int.Parse(match.Groups[4].Success ? match.Groups[4].Value.Trim() : "1");
                result.Episode = int.Parse(match.Groups[7].Value.Trim().PadLeft(2, '0'));
                result.EpisodeTo = match.Groups[10].Success ? (int?)int.Parse(match.Groups[10].Value.Trim()) : null;
                string audioQuality = match.Groups[12].Value.Trim(' ', '[', ']');
                quality = match.Groups[13].Value.Trim(' ', '[', ']');

                string seasonText = result.Season.ToString();
                string episodeText = seasonText + result.Episode.ToString().PadLeft(2, '0');
                string episodeToText = result.EpisodeTo.HasValue ? "_" + seasonText + result.EpisodeTo.ToString().PadLeft(2, '0') : "";

                result.Title = string.Format("{0} - Temporada {1} [{2}][Cap.{3}{4}][{5}]",
                    result.SerieName, seasonText, quality, episodeText, episodeToText, audioQuality);
            }
            else
            {
                Match matchClassic = _titleClassicRegex.Match(title);
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

            result.Size = size;
            result.Link = new Uri(detailsUrl);
            result.Guid = result.Link;
            result.PublishDate = publishDate;
            result.Seeders = 1;
            result.Peers = 1;

            result.Title = FixedTitle(result, quality);

            return result;
        }

        private string FixedTitle(NewpctRelease release, string quality)
        {
            if (String.IsNullOrEmpty(release.SerieName))
            {
                release.SerieName = release.Title;
                if (release.Title.Contains("-"))
                {
                    release.SerieName = release.Title.Substring(0, release.Title.IndexOf('-') - 1);
                }
            }
            if (String.IsNullOrEmpty(quality))
            {
                quality = "HDTV";
            }
            var seasonAndEpisode = "S" + release.Season.ToString().PadLeft(2, '0');
            seasonAndEpisode += "E" + release.Episode.ToString().PadLeft(2, '0');
            if (release.EpisodeTo != release.Episode && release.EpisodeTo != null && release.EpisodeTo != 0)
            {
                seasonAndEpisode += "-" + release.EpisodeTo.ToString().PadLeft(2, '0');
            }
            var titleParts = new List<string>();
            titleParts.Add(release.SerieName);
            titleParts.Add(seasonAndEpisode);
            titleParts.Add(quality.Replace("[", "").Replace("]", ""));
            if (release.Title.ToLower().Contains("esp") || release.Title.ToLower().Contains("cast"))
            {
                titleParts.Add("Spanish");
            }
            return String.Join(".", titleParts);
        }
    }
}
