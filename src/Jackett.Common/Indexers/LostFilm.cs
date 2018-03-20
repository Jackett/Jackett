using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Parser.Html;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    class LostFilm : BaseWebIndexer
    {
        private static Regex parsePlayEpisodeRegex = new Regex("PlayEpisode\\('(?<id>\\d{1,3})(?<season>\\d{3})(?<episode>\\d{3})'\\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
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

                seriesId = match.Groups["id"].Value.TrimStart('0');
                season = match.Groups["season"].Value.TrimStart('0');
                episode = match.Groups["episode"].Value.TrimStart('0');
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

        public LostFilm(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
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
            logger.Debug("Applying configuration");
            LoadValuesFromJson(configJson);

            // Performing Logout is required to invalidate previous session otherwise the `{"error":1,"result":"ok"}` will be returned.
            await Logout();

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

        private async Task<Boolean> Logout()
        {
            logger.Info("Performing logout");

            var data = new Dictionary<string, string>
            {
                { "act", "users" },
                { "type", "logout" }
            };

            var response = await PostDataWithCookies(url: ApiUrl, data: data);
            logger.Debug("Logout result: " + response.Content);

            var isOK = response.Status == System.Net.HttpStatusCode.OK;
            if (!isOK)
            {
                logger.Error("Logout failed with response: " + response.Content);
            }

            return isOK;
        }

        #region Query

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            logger.Debug("PerformQuery: " + query.GetQueryString());

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
            logger.Debug("PerformSearch: " + query.SanitizedSearchTerm + " [" + query.QueryType + "]");
            var releases = new List<ReleaseInfo>();

            /*
            Torznab query for some series could contains sanitized title. E.g. "Star Wars: The Clone Wars" will become "Star Wars The Clone Wars".
            Search API on LostFilm.tv doesn't return anything on such search query so the query should be "morphed" even for "tvsearch" queries.
            The algorythm works in the following way:
                1. Search with the full SearchTerm. Just for example, let's search for episode by it's name
                    - {Star Wars The Clone Wars To Catch a Jedi}
                2. [loop] If none were found, repeat search with SearchTerm reduced by 1 word from the end. Fail search if no words left and no results were obtained
                    - {Star Wars The Clone Wars To Catch a} Jedi
                    - {Star Wars The Clone Wars To Catch} a Jedi
                    - ...
                    - {Star Wars} The Clone Wars To Catch a Jedi
                3. When we got few results, try to filter them with the words excluded before
                    - [Star Wars: The Clone Wars, Star Wars Rebels, Star Wars: Forces of Destiny]
                        .filterBy(The Clone Wars To Catch a Jedi)
                4. [loop] Reduce filterTerm by 1 word from the end. Fail search if no words left and no results were obtained
                        .filterBy(The Clone Wars To Catch a) / Jedi
                        .filterBy(The Clone Wars To Catch) / a Jedi
                        ...
                        .filterBy(The Clone Wars) / To Catch a Jedi
                5. Fetch series detail page for "Star Wars The Clone Wars" with a "To Catch a Jedi" filterTerm to find required episode
            */

            // Search query words. Consists of Series keywords that will be used for series search request, and Episode keywords that will be used for episode filtering.
            var keywords = query.SanitizedSearchTerm.Split(' ').ToList();
            // Keywords count related to Series Search.
            var searchKeywords = keywords.Count;
            // Keywords count related to Series Filter.
            var serieFilterKeywords = 0;
            // Overall (keywords.count - searchKeywords - serieFilterKeywords) are related to episode filter

            do
            {
                var searchString = string.Join(" ", keywords.Take(searchKeywords));
                var data = new Dictionary<string, string>
                {
                    { "act", "common" },
                    { "type", "search" },
                    { "val", searchString }
                };
                logger.Debug("> Searching: " + searchString);
                var response = await PostDataWithCookies(url: ApiUrl, data: data);

                try
                {
                    var json = JToken.Parse(response.Content);

                    // Protect from {"data":false,"result":"ok"}
                    var jsonData = json["data"];
                    if (jsonData.Type != JTokenType.Object)
                        continue; // Search loop

                    var jsonSeries = jsonData["series"];
                    if (jsonSeries == null || !jsonSeries.HasValues)
                        continue; // Search loop

                    var series = jsonSeries.ToList();
                    logger.Debug("> Found " + series.Count().ToString() + " series: [" + string.Join(", ", series.Select(s => s["title_orig"].Value<string>())) + "]");

                    // Filter found series

                    if (series.Count() > 1)
                    {
                        serieFilterKeywords = keywords.Count - searchKeywords;

                        do
                        {
                            var serieFilter = string.Join(" ", keywords.GetRange(searchKeywords, serieFilterKeywords));
                            logger.Debug("> Filtering: " + serieFilter);
                            var filteredSeries = series.Where(s => s["title_orig"].Value<string>().Contains(serieFilter)).ToList();

                            if (filteredSeries.Count() > 0)
                            {
                                logger.Debug("> Series filtered: [" + string.Join(", ", filteredSeries.Select(s => s["title_orig"].Value<string>())) + "]");
                                series = filteredSeries;
                                break; // Serie Filter loop
                            }
                        }
                        while (--serieFilterKeywords > 0);
                    }

                    foreach (var serie in series)
                    {
                        var link = serie["link"].ToString();
                        var season = query.Season == 0 ? "/seasons" : "/season_" + query.Season.ToString();
                        var url = SiteLink + link.TrimStart('/') + season;

                        if (!string.IsNullOrEmpty(query.Episode)) // Fetch single episode releases
                        {
                            // TODO: Add a togglable Quick Path via v_search.php in Indexer Settings
                            url += "/episode_" + query.Episode;
                            var taskReleases = await FetchEpisodeReleases(url);
                            releases.AddRange(taskReleases);
                        }
                        else // Fetch the whole series OR episode with filter applied
                        {
                            var filterKeywords = keywords.Skip(searchKeywords + serieFilterKeywords);
                            var filter = string.Join(" ", filterKeywords);

                            var taskReleases = await FetchSeriesReleases(url, query, filter);
                            releases.AddRange(taskReleases);
                        }
                    }

                    break; // Search loop
                }
                catch (Exception ex)
                {
                    OnParseError(response.Content, ex);
                }
            }
            while (--searchKeywords > 0);

            return releases;
        }

        #endregion
        #region Page parsing

        private async Task<List<ReleaseInfo>> FetchNewReleases()
        {
            var url = DiscoveryUrl;
            logger.Debug("FetchNewReleases: " + url);
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

                    var episodeReleases = await FetchEpisodeReleases(episodeUrl);
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
            logger.Debug("FetchEpisodeReleases: " + url);
            var results = await RequestStringAndRelogin(url);
            var releases = new List<ReleaseInfo>();

            try
            {
                var parser = new HtmlParser();
                var document = parser.Parse(results.Content);

                var playButton = document.QuerySelector("div.external-btn");
                if (playButton != null && !playButton.ClassList.Contains("inactive"))
                {
                    var comments = new Uri(url);

                    var dateString = document.QuerySelector("div.title-block > div.details-pane > div.left-box").TextContent;
                    dateString = TrimString(dateString, "eng: ", " г."); // '... Дата выхода eng: 09 марта 2012 г. ...' -> '09 марта 2012'
                    var date = DateTime.Parse(dateString, new CultureInfo(Language)); // dd mmmm yyyy

                    var urlDetails = new TrackerUrlDetails(playButton);
                    var episodeReleases = await FetchTrackerReleases(urlDetails);

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

        private async Task<List<ReleaseInfo>> FetchSeriesReleases(string url, TorznabQuery query, string filter)
        {
            logger.Debug("FetchSeriesReleases: " + url + " S: " + query.Season.ToString() + " E: " + query.Episode + " Filter: " + filter);

            var releases = new List<ReleaseInfo>();
            var results = await RequestStringWithCookies(url);

            try
            {
                var parser = new HtmlParser();
                var document = parser.Parse(results.Content);
                var seasons = document.QuerySelectorAll("div.serie-block");
                var rowSelector = "table.movie-parts-list > tbody > tr";

                foreach (var season in seasons)
                {
                    // Could ne null if serie-block is for Extras
                    var seasonButton = season.QuerySelector("div.movie-details-block > div.external-btn");

                    // Process only season we're searching for
                    if (seasonButton != null && query.Season > 0)
                    {
                        // If seasonButton in "inactive" it will not contain "onClick" handler. Better to parse element which always exists.
                        var watchedButton = season.QuerySelector("div.movie-details-block > div.haveseen-btn");
                        var buttonCode = watchedButton.GetAttribute("data-code");
                        var currentSeason = buttonCode.Substring(buttonCode.IndexOf('-') + 1);

                        if (currentSeason != query.Season.ToString())
                        {
                            continue; // Can't match season by regex OR season not matches to a searched one
                        }

                        // Stop parsing season episodes if season pack was required but it's not available yet.
                        if (seasonButton.ClassList.Contains("inactive"))
                        {
                            logger.Debug("> No season pack is found for S" + query.Season.ToString());
                            break;
                        }
                    }

                    // Fetch season pack releases if no episode filtering is required.
                    // If seasonButton implements "inactive" class there are no season pack available and each episode should be fetched separately.
                    if (string.IsNullOrEmpty(query.Episode) && string.IsNullOrEmpty(filter) && seasonButton != null && !seasonButton.ClassList.Contains("inactive"))
                    {
                        var lastEpisode = season.QuerySelector(rowSelector);
                        var dateColumn = lastEpisode.QuerySelector("td.delta");
                        var date = DateFromEpisodeColumn(dateColumn);

                        var comments = new Uri(url); // Current season(-s) page url

                        var urlDetails = new TrackerUrlDetails(seasonButton);
                        var seasonReleases = await FetchTrackerReleases(urlDetails);

                        foreach (var release in seasonReleases)
                        {
                            release.Comments = comments;
                            release.PublishDate = date;
                        }

                        releases.AddRange(seasonReleases);

                        if (query.Season > 0)
                        {
                            break; // Searched season was processed
                        }

                        // Skip parsing separate episodes if season pack was added
                        if (seasonReleases.Count() > 0)
                        {
                            continue;
                        }
                    }

                    // No season filtering was applied OR season pack in not available
                    var rows = season.QuerySelectorAll(rowSelector).Where(s => !s.ClassList.Contains("not-available"));

                    foreach (var row in rows)
                    {
                        var couldBreak = false; // Set to `true` if searched episode was found

                        try
                        {
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
                            var date = DateFromEpisodeColumn(dateColumn);

                            var link = dateColumn.GetAttribute("onclick"); // goTo('/series/Prison_Break/season_5/episode_9/',false)
                            link = TrimString(link, '\'', '\'');
                            var episodeUrl = SiteLink + link.TrimStart('/');
                            var comments = new Uri(episodeUrl);

                            var urlDetails = new TrackerUrlDetails(playButton);
                            var episodeReleases = await FetchTrackerReleases(urlDetails);

                            foreach (var release in episodeReleases)
                            {
                                release.Comments = comments;
                                release.PublishDate = date;
                            }
                            releases.AddRange(episodeReleases);
                        }
                        catch (Exception ex)
                        {
                            logger.Error(string.Format("{0}: Error while parsing row '{1}':\n\n{2}", ID, row.OuterHtml, ex));
                        }

                        if (couldBreak)
                        {
                            break;
                        }
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
        #region Tracker parsing

        private async Task<List<ReleaseInfo>> FetchTrackerReleases(TrackerUrlDetails details)
        {
            var queryCollection = new NameValueCollection();
            queryCollection.Add("c", details.seriesId);
            queryCollection.Add("s", details.season);
            queryCollection.Add("e", string.IsNullOrEmpty(details.episode) ? "999" : details.episode); // 999 is a synonym for the whole serie
            var url = ReleaseUrl + "?" + queryCollection.GetQueryString();

            logger.Debug("FetchTrackerReleases: " + url);

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
            logger.Debug("FollowTrackerRedirection: " + url);
            var results = await RequestStringWithCookies(url);
            var releases = new List<ReleaseInfo>();

            try
            {
                var parser = new HtmlParser();
                var document = parser.Parse(results.Content);
                var rows = document.QuerySelectorAll("div.inner-box--item");

                logger.Debug("> Parsing " + rows.Count().ToString() + " releases");

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

                        /*
                         * For supported qualities see:
                         *  - TvCategoryParser.cs
                         *  - https://github.com/SickRage/SickRage/wiki/Quality-Settings#quality-names-to-recognize-the-quality-of-a-file
                         */
                        var quality = releaseDetails.Groups["quality"].Value.Trim();
                        // Adapt shitty quality format for common algorythms
                        quality = Regex.Replace(quality, "-Rip", "Rip", RegexOptions.IgnoreCase);
                        quality = Regex.Replace(quality, "WEB-DLRip", "WEBDL", RegexOptions.IgnoreCase);
                        quality = Regex.Replace(quality, "WEB-DL", "WEBDL", RegexOptions.IgnoreCase);
                        quality = Regex.Replace(quality, "HDTVRip", "HDTV", RegexOptions.IgnoreCase);
                        // Fix forgotten p-Progressive suffix in resolution index
                        quality = Regex.Replace(quality, "1080 ", "1080p ", RegexOptions.IgnoreCase);
                        quality = Regex.Replace(quality, "720 ", "720p ", RegexOptions.IgnoreCase);

                        var techComponents = new string[] {
                            "rus", quality, "(LostFilm)"
                        };
                        var techInfo = string.Join(" ", techComponents.Where(s => !string.IsNullOrEmpty(s)));

                        // Ru title: downloadLink.TextContent.Replace("\n", "");
                        // En title should be manually constructed.
                        var titleComponents = new string[] {
                            serieTitle, details.GetEpisodeString(), episodeName, techInfo
                        };
                        release.Title = string.Join(" - ", titleComponents.Where(s => !string.IsNullOrEmpty(s)));

                        var downloadLink = row.QuerySelector("div.inner-box--link > a");
                        release.Link = new Uri(downloadLink.GetAttribute("href"));
                        release.Guid = release.Link;

                        var sizeString = releaseDetails.Groups["size"].Value.ToUpper();
                        sizeString = sizeString.Replace("ТБ", "TB"); // untested
                        sizeString = sizeString.Replace("ГБ", "GB");
                        sizeString = sizeString.Replace("МБ", "MB");
                        sizeString = sizeString.Replace("КБ", "KB"); // untested
                        release.Size = ReleaseInfo.GetBytes(sizeString);

                        logger.Debug("> Add: " + release.Title);
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
            return (start != -1 && end != -1) ? s.Substring(start + 1, end - start - 1) : null;
        }

        private string TrimString(string s, string startString, string endString)
        {
            var start = s.IndexOf(startString);
            var end = s.LastIndexOf(endString);
            return (start != -1 && end != -1) ? s.Substring(start + startString.Length, end - start - startString.Length) : null;
        }

        private DateTime DateFromEpisodeColumn(IElement dateColumn)
        {
            var dateString = dateColumn.QuerySelector("span").TextContent;
            dateString = dateString.Substring(dateString.IndexOf(":") + 2); // 'Eng: 23.05.2017' -> '23.05.2017'
            var date = DateTime.Parse(dateString, new CultureInfo(Language)); // dd.mm.yyyy
            return date;
        }

        #endregion
    }
}
