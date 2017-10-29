using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Jackett.Models;
using Jackett.Models.IndexerConfig;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Jackett.Services.Interfaces;

using AngleSharp.Dom;
using AngleSharp.Parser.Html;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Indexers
{
    class LostFilm : BaseWebIndexer
    {
        private static Regex parsePlayEpisodeRegex = new Regex("PlayEpisode\\('(?<id>\\d+)','(?<season>\\d+)','(?<episode>\\d+)'\\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Regex parseReleaseDetailsRegex = new Regex("Видео:\\ (?<quality>.+).\\ Размер:\\ (?<size>.+).\\ Перевод", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // http://www.lostfilm.tv/login
        string ApiUrl { get { return SiteLink + "ajaxik.php"; } }
        // http://www.lostfilm.tv/new
        string DiscoveryUrl { get { return SiteLink + "new"; } }
        // http://www.lostfilm.tv/search?q=breaking+bad
        string SearchUrl { get { return SiteLink + "search"; } }
        // PlayEpisode function produce urls like this:
        // https://www.lostfilm.tv/v_search.php?c=119&s=5&e=16
        string ReleaseUrl { get { return SiteLink + "v_search.php"; } }


        internal class TrackerUrlDetails
        {
            internal string seriesId { get; private set; }
            internal string season { get; private set; }
            internal string episode { get; private set; }

            internal TrackerUrlDetails(string seriesId, string season, string episode)
            {
                this.seriesId = seriesId;
                this.season = season;
                this.episode = episode;
            }

            internal TrackerUrlDetails(IElement button)
            {
                var trigger = button.GetAttribute("onclick");
                var match = parsePlayEpisodeRegex.Match(trigger);

                seriesId = match.Groups["id"].Value;
                season = match.Groups["season"].Value;
                episode = match.Groups["episode"].Value;
            }

            internal string GetEpisodeString()
            {
                var result = string.Empty;

                if (!string.IsNullOrEmpty(season) && season != "0" && season != "999")
                {
                    result += "S" + season;

                    if (!string.IsNullOrEmpty(episode) && episode != "0" && episode != "999")
                    {
                        result += "E" + episode;
                    }
                }

                return result;
            }
        }

        new ConfigurationDataBasicLogin configData
        {
            get { return (ConfigurationDataBasicLogin)base.configData; }
            set { base.configData = value; }
        }

        public LostFilm(IIndexerConfigurationService configService, IWebClient wc, Logger l, IProtectionService ps)
            : base(name: "LostFilm.tv",
                   description: "Unique portal about foreign series",
                   link: "https://www.lostfilm.tv/",
                   caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   // TODO: Provide optional instructions
                   configData: new ConfigurationDataBasicLogin())
        {
            Encoding = Encoding.UTF8;
            Language = "ru-ru";
            Type = "semi-private";
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            logger.Info("Applying configuration");
            LoadValuesFromJson(configJson);

            var data = new Dictionary<string, string>
            {
                { "act", "users" },
                { "type", "login" },
                { "mail", configData.Username.Value },
                { "pass", configData.Password.Value },
                { "rem", "1" }
            };

            var result = await RequestLoginAndFollowRedirect(ApiUrl, data, CookieHeader, true, SiteLink, ApiUrl, true);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("\"success\":true"), () =>
            {
                var errorMessage = result.Content;
                throw new ExceptionWithConfigData(errorMessage, configData);
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            logger.Info("PerformQuery: " + query.GetQueryString());

            // If the search string is empty use the latest releases
            if (query.IsTest || string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                return await FetchNewReleases();
            }
            else
            {
                return await PerformSearch(query);
            }
        }

        private async Task<WebClientStringResult> RequestStringAndRelogin(string url)
        {
            var results = await RequestStringWithCookies(url);
            if (results.Content.Contains("503 Service"))
            {
                throw new ExceptionWithConfigData(results.Content, configData);
            }
            else if (results.Content.Contains("href=\"/login\""))
            {
                // Re-login
                await ApplyConfiguration(null);
                return await RequestStringWithCookies(url);
            }
            else
            {
                return results;
            }
        }

        private async Task<List<ReleaseInfo>> PerformSearch(TorznabQuery query)
        {
            logger.Info("PerformSearch: " + query.SanitizedSearchTerm + " [" + query.QueryType + "]");
            var releases = new List<ReleaseInfo>();

            // If set to `true` search string will be reduced by word each time tracker returns zero results.
            // It's required for "search" queries because tracker search API could only return series, not episodes.
            // Set this variable to `false` within do/while loop when results were achieved.
            var shouldMorphQuery = query.IsSearch;
            // Search query words. Consists of Series keywords that will be used for series search request, and Episode keywords that will be used for episode filtering.
            var keywords = query.SanitizedSearchTerm.Split(' ').ToList();
            // Number of first keywords that relates to Series keywords.
            int searchKeywords = keywords.Count;

            do
            {
                var searchString = string.Join(" ", keywords.Take(searchKeywords));
                var data = new Dictionary<string, string>
                {
                    { "act", "common" },
                    { "type", "search" },
                    { "val", searchString }
                };
                logger.Info("> Searching: " + searchString);
                var response = await PostDataWithCookies(url: ApiUrl, data: data);

                try
                {
                    var json = JToken.Parse(response.Content);
                    var jsonData = json["data"];

                    // Protect from {"data":false,"result":"ok"}
                    if (jsonData.Type == JTokenType.Object)
                    {
                        var series = jsonData["series"];
                        if (series != null && series.HasValues)
                        {
                            logger.Info("> Found " + series.Count().ToString() + " series -> breaking the loop");
                            shouldMorphQuery = false;

                            foreach (var serie in series)
                            {
                                var link = serie["link"].ToString();
                                var season = query.Season == 0 ? "/seasons" : "/season_" + query.Season.ToString();
                                var url = SiteLink + link.TrimStart('/') + season;

                                if (!string.IsNullOrEmpty(query.Episode))
                                {
                                    // TODO: Add a Quick Path via v_search.php for "tvsearch" queries
                                    url += "/episode_" + query.Episode;
                                    var taskReleases = await FetchEpisodeReleases(url);
                                    releases.AddRange(taskReleases);
                                }
                                else
                                {
                                    var filterKeywords = keywords.Skip(searchKeywords);
                                    var filter = string.Join(" ", filterKeywords);

                                    var taskReleases = await FetchSeriesReleases(url, query, filter);
                                    releases.AddRange(taskReleases);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    OnParseError(response.Content, ex);
                }

                if (shouldMorphQuery)
                {
                    searchKeywords--;
                }
            } while (shouldMorphQuery && searchKeywords > 0);

            return releases;
        }

        #region Page parsing

        private async Task<List<ReleaseInfo>> FetchNewReleases()
        {
            var url = DiscoveryUrl;
            logger.Info("FetchNewReleases: " + url);
            var results = await RequestStringAndRelogin(url);
            var releases = new List<ReleaseInfo>();

            try
            {
                var parser = new HtmlParser();
                var document = parser.Parse(results.Content);
                var rows = document.QuerySelectorAll("div.row");

                foreach (var row in rows)
                {
                    var link = row.QuerySelector("a").GetAttribute("href");
                    var episodeUrl = SiteLink + link.TrimStart('/');
                    var comments = new Uri(episodeUrl);

                    var dateString = row.QuerySelector("div.beta:contains('Дата')").TextContent; // Release date: beta - ENG, alpha - RUS 
                    dateString = dateString.Substring(dateString.IndexOf(":") + 2); // 'Дата выхода Eng: 13.10.2017' -> '13.10.2017'
                    var date = DateTime.Parse(dateString, new CultureInfo(Language)); // dd.mm.yyyy

                    var episodeReleases = await FetchEpisodeReleases(episodeUrl);

                    foreach (var release in episodeReleases)
                    {
                        release.Comments = comments;
                        release.PublishDate = date;
                    }
                    releases.AddRange(episodeReleases);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.Content, ex);
            }

            return releases;
        }

        private async Task<List<ReleaseInfo>> FetchEpisodeReleases(string url)
        {
            logger.Info("FetchEpisodeReleases: " + url);
            var results = await RequestStringAndRelogin(url);
            var releases = new List<ReleaseInfo>();

            try
            {
                var parser = new HtmlParser();
                var document = parser.Parse(results.Content);
                var playButton = document.QuerySelector("div.external-btn");
                if (playButton != null)
                {
                    var urlDetails = new TrackerUrlDetails(playButton);
                    releases = await FetchTrackerReleases(urlDetails);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.Content, ex);
            }

            return releases;
        }

        // `detailsUrl` is a series details url provided in search JSON response.
        private async Task<List<ReleaseInfo>> FetchSeriesReleases(string url, TorznabQuery query, string filter)
        {
            logger.Info("FetchSeriesReleases: " + url + " S: " + query.Season.ToString() + " E: " + query.Episode + " Filter: " + filter);

            var releases = new List<ReleaseInfo>();
            var results = await RequestStringWithCookies(url);

            try
            {
                var parser = new HtmlParser();
                var document = parser.Parse(results.Content);
                var seasons = document.QuerySelectorAll("div.serie-block");

                if (string.IsNullOrEmpty(query.Episode) || string.IsNullOrEmpty(filter))
                {
                    var rows = seasons.SelectMany(s => s.QuerySelectorAll("table.movie-parts-list > tbody > tr"));

                    foreach (var row in rows)
                    {
                        var couldBreak = false; // Set to `true` if searched episode was found.

                        if (!string.IsNullOrEmpty(filter))
                        {
                            var titles = row.QuerySelector("td.gamma > div");
                            if (titles.TextContent.IndexOf(filter, StringComparison.OrdinalIgnoreCase) == -1)
                            {
                                continue;
                            }
                        }

                        var playButton = row.QuerySelector("td.zeta > div.external-btn");

                        if (!string.IsNullOrEmpty(query.Episode))
                        {
                            var match = parsePlayEpisodeRegex.Match(playButton.GetAttribute("onclick"));
                            var episode = match.Groups["episode"];

                            if (episode == null || episode.Value != query.Episode)
                            {
                                continue;
                            }

                            couldBreak = true;
                        }

                        var dateColumn = row.QuerySelector("td.delta"); // Contains both Date and EpisodeURL

                        var link = dateColumn.GetAttribute("onclick"); // goTo('/series/Prison_Break/season_5/episode_9/',false)
                        link = TrimString(link, '\'', '\'');
                        var episodeUrl = SiteLink + link.TrimStart('/');
                        var comments = new Uri(episodeUrl);

                        var dateString = dateColumn.QuerySelector("span").TextContent;
                        dateString = dateString.Substring(dateString.IndexOf(":") + 2); // 'Eng: 23.05.2017' -> '23.05.2017'
                        var date = DateTime.Parse(dateString, new CultureInfo(Language)); // dd.mm.yyyy

                        var urlDetails = new TrackerUrlDetails(playButton);
                        var episodeReleases = await FetchTrackerReleases(urlDetails);

                        foreach (var release in episodeReleases)
                        {
                            release.Comments = comments;
                            release.PublishDate = date;
                        }
                        releases.AddRange(episodeReleases);

                        if (couldBreak)
                        {
                            break;
                        }
                    }
                }
                else if (query.Season > 0)
                {
                    // Query for the whole season release. Strange query in terms of typical requests but doable.
                    var buttons = seasons.SelectMany(s => s.QuerySelectorAll("div.movie-details-block > div.external-btn"));

                    foreach (var playButton in buttons)
                    {
                        var match = parsePlayEpisodeRegex.Match(playButton.GetAttribute("onclick"));
                        var season = match.Groups["season"];
                        if (season != null && season.Value == query.Season.ToString())
                        {
                            var urlDetails = new TrackerUrlDetails(playButton);
                            // TODO: Set PublishDate = season.lastEpisode.publishDate for season releases.
                            releases = await FetchTrackerReleases(urlDetails);

                            // Skip other seasons
                            break;
                        }
                    }
                }
                else
                {
                    throw new ArgumentException("Impossible combination of arguments");
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.Content, ex);
            }

            return releases;
        }

        #endregion
        #region Tracker parsing

        private async Task<List<ReleaseInfo>> FetchTrackerReleases(TrackerUrlDetails details)
        {
            var queryCollection = new NameValueCollection();
            queryCollection.Add("c", details.seriesId);
            queryCollection.Add("s", details.season);
            queryCollection.Add("e", string.IsNullOrEmpty(details.episode) ? "999" : details.episode); // 999 is a synonym for the whole serie
            var url = ReleaseUrl + "?" + queryCollection.GetQueryString();

            logger.Info("FetchTrackerReleases: " + url);

            // Get redirection page with generated link on it. This link can't be constructed manually as it contains Hash field and hashing algo is unknown.
            var results = await RequestStringWithCookies(url);
            if (results.Content == "log in first")
            {
                throw new ExceptionWithConfigData(results.Content, configData);
            }

            try
            {
                var parser = new HtmlParser();
                var document = parser.Parse(results.Content);
                var meta = document.QuerySelector("meta");
                var metaContent = meta.GetAttribute("content");

                // Follow redirection defined by async url.replace
                var redirectionUrl = metaContent.Substring(metaContent.IndexOf("http"));
                return await FollowTrackerRedirection(redirectionUrl, details);
            }
            catch (Exception ex)
            {
                OnParseError(results.Content, ex);
            }

            // Failure path
            return new List<ReleaseInfo>();
        }

        private async Task<List<ReleaseInfo>> FollowTrackerRedirection(string url, TrackerUrlDetails details)
        {
            logger.Info("FollowTrackerRedirection: " + url);
            var results = await RequestStringWithCookies(url);
            var releases = new List<ReleaseInfo>();

            try
            {
                var parser = new HtmlParser();
                var document = parser.Parse(results.Content);
                var rows = document.QuerySelectorAll("div.inner-box--item");

                logger.Info("> Parsing " + rows.Count().ToString() + " releases");

                var serieTitle = document.QuerySelector("div.inner-box--subtitle").TextContent;
                serieTitle = serieTitle.Substring(0, serieTitle.LastIndexOf(','));

                var episodeInfo = document.QuerySelector("div.inner-box--text").TextContent;
                var episodeName = TrimString(episodeInfo, '(', ')');

                foreach (var row in rows)
                {
                    try
                    {
                        var release = new ReleaseInfo();

                        release.Category = new int[] { TorznabCatType.TV.ID };

                        var detailsInfo = row.QuerySelector("div.inner-box--desc").TextContent;
                        var releaseDetails = parseReleaseDetailsRegex.Match(detailsInfo);
                        if (releaseDetails == null)
                        {
                            throw new FormatException("Failed to map release details string: " + detailsInfo);
                        }

                        // Ru title: downloadLink.TextContent.Replace("\n", "");
                        // En title should be manually constructed.
                        var titleComponents = new string[] {
                            serieTitle, details.GetEpisodeString(), episodeName, releaseDetails.Groups["quality"].Value
                        };
                        release.Title = string.Join(" - ", titleComponents.Where(s => !string.IsNullOrEmpty(s)));

                        var downloadLink = row.QuerySelector("div.inner-box--link > a");
                        release.Link = new Uri(downloadLink.GetAttribute("href"));
                        release.Guid = release.Link;

                        var sizeString = releaseDetails.Groups["size"].Value;
                        sizeString = sizeString.Replace("ТБ", "TB"); // untested
                        sizeString = sizeString.Replace("ГБ", "GB");
                        sizeString = sizeString.Replace("МБ", "MB");
                        sizeString = sizeString.Replace("КБ", "KB"); // untested
                        release.Size = ReleaseInfo.GetBytes(sizeString);

                        logger.Info("> Add: " + release.Title);
                        releases.Add(release);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(string.Format("{0}: Error while parsing row '{1}':\n\n{2}", ID, row.OuterHtml, ex));
                    }
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.Content, ex);
            }

            return releases;
        }

        #endregion
        #region Helpers

        private string TrimString(string s, char startChar, char endChar)
        {
            var start = s.IndexOf(startChar);
            var end = s.LastIndexOf(endChar);
            return s.Substring(start + 1, end - start - 1);
        }

        #endregion
    }
}
