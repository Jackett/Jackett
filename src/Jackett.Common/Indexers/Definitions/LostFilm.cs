using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class LostFilm : IndexerBase
    {
        public override string Id => "lostfilm";
        public override string Name => "LostFilm.tv";
        public override string Description => "Unique portal about foreign series";
        public override string SiteLink { get; protected set; } = "https://www.lostfilm.tv/";
        public override string[] AlternativeSiteLinks => new[]
        {
            // Uptrends.com uptime checkpoints // Uptime.com availability locations
            "https://www.lostfilm.tv/", // 43/43 // 9/9
            "https://www.lostfilmtv.site/", // 43/43 // 9/9
            "https://www.lostfilmtv5.site/", // 43/43 // 9/9
            "https://www.lostfilmtv2.site/", // 43/43 // 9/9
            "https://www.lostfilmtv3.site/", // 43/43 // 9/9
            "https://www.lostfilm.today/", // 43/43 // 9/9
            "https://www.lostfilm.download/", // 43/43 // 9/9
            "https://www.lostfilm.life/", // 27/43 // 6/9
            "https://www.lostfilm.uno/", // 25/43 // 7/9
            "https://www.lostfilm.win/", // 25/43 // 7/9
            "https://www.lostfilm.tw/", // 25/43 // 7/9
        };
        public override string[] LegacySiteLinks => new[]
        {
            "https://lostfilm.site", // redirects to .tw
            "https://lostfilm.tw/", // redirects to www.
            "https://www.lostfilm.run/", // ERR_NAME_NOT_RESOLVED
        };
        public override string Language => "ru-RU";
        public override string Type => "semi-private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private static readonly Regex parsePlayEpisodeRegex = new Regex("PlayEpisode\\('(?<id>\\d{1,3})(?<season>\\d{3})(?<episode>\\d{3})'\\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex parseReleaseDetailsRegex = new Regex("Видео:\\ (?<quality>.+).\\ Размер:\\ (?<size>.+).\\ Перевод", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private string LoginUrl => SiteLink + "login";

        // http://www.lostfilm.tv/login
        private string ApiUrl => SiteLink + "ajaxik.php";

        // http://www.lostfilm.tv/new
        private string DiscoveryUrl => SiteLink + "new";

        // http://www.lostfilm.tv/search?q=breaking+bad
        private string SearchUrl => SiteLink + "search";

        // PlayEpisode function produce urls like this:
        // https://www.lostfilm.tv/v_search.php?c=119&s=5&e=16
        private string ReleaseUrl => SiteLink + "v_search.php";

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

            // TODO: see if query.GetEpisodeString() is sufficient
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

        private new ConfigurationDataCaptchaLogin configData
        {
            get => (ConfigurationDataCaptchaLogin)base.configData;
            set => base.configData = value;
        }

        public LostFilm(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataCaptchaLogin())
        {
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q
                }
            };

            // TODO: review if there is only this category (movie search is enabled)
            caps.Categories.AddCategoryMapping(1, TorznabCatType.TV);

            return caps;
        }

        public override void LoadValuesFromJson(JToken jsonConfig, bool useProtectionService = false)
        {
            base.LoadValuesFromJson(jsonConfig, useProtectionService);

            webclient?.AddTrustedCertificate(new Uri(SiteLink).Host, "34287FB53A58EC6AE590E7DD7E03C70C0263CADC"); // for *.tw  expired 01/Apr/21
        }

        public override async Task<ConfigurationData> GetConfigurationForSetup()
        {
            // looks like after some failed login attempts there's a captcha
            var loginPage = await RequestWithCookiesAsync(LoginUrl, string.Empty);
            var parser = new HtmlParser();
            using var document = parser.ParseDocument(loginPage.ContentString);
            var qCaptchaImg = document.QuerySelector("img#captcha_pictcha");
            if (qCaptchaImg != null)
            {
                var captchaUrl = SiteLink + qCaptchaImg.GetAttribute("src").TrimStart('/');
                var captchaImage = await RequestWithCookiesAsync(captchaUrl, loginPage.Cookies);
                configData.CaptchaImage.Value = captchaImage.ContentBytes;
            }
            else
            {
                configData.CaptchaImage.Value = Array.Empty<byte>();
            }
            configData.CaptchaCookie.Value = loginPage.Cookies;
            UpdateCookieHeader(loginPage.Cookies);
            return configData;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            logger.Debug("Applying configuration");
            LoadValuesFromJson(configJson);

            if (!configData.Username.Value.Contains("@"))
                throw new ExceptionWithConfigData("Username must be an e-mail address", configData);


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

            if (!string.IsNullOrWhiteSpace(configData.CaptchaText.Value))
            {
                data.Add("need_captcha", "1");
                data.Add("captcha", configData.CaptchaText.Value);
            }

            var result = await RequestLoginAndFollowRedirect(ApiUrl, data, CookieHeader, true, SiteLink, ApiUrl, true);
            await ConfigureIfOK(result.Cookies, result.ContentString != null && result.ContentString.Contains("\"success\":true"), () =>
            {
                var errorMessage = result.ContentString;
                if (errorMessage.Contains("\"error\":1") || errorMessage.Contains("\"error\":2") || errorMessage.Contains("\"error\":4"))
                    errorMessage = "Captcha is incorrect";
                if (errorMessage.Contains("\"error\":3"))
                    errorMessage = "E-mail or password is incorrect";
                throw new ExceptionWithConfigData(errorMessage, configData);
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        private async Task<bool> Logout()
        {
            logger.Info("Performing logout");

            var data = new Dictionary<string, string>
            {
                { "act", "users" },
                { "type", "logout" }
            };

            var response = await RequestWithCookiesAsync(ApiUrl, method: RequestType.POST, data: data);
            logger.Debug("Logout result: " + response.ContentString);

            var isOK = response.Status == System.Net.HttpStatusCode.OK;
            if (!isOK)
            {
                logger.Error("Logout failed with response: " + response.ContentString);
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

        private async Task<WebResult> RequestStringAndRelogin(string url)
        {
            var results = await RequestWithCookiesAsync(url);
            if (results.ContentString.Contains("503 Service"))
            {
                throw new ExceptionWithConfigData(results.ContentString, configData);
            }
            else if (results.ContentString.Contains("href=\"/login\""))
            {
                // Re-login
                await ApplyConfiguration(null);
                return await RequestWithCookiesAsync(url);
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
            Also the queries to Specials is a union of Series and Episode titles. E.g.: "Breaking Bad - El Camino: A Breaking Bad Movie".

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
                5. [loop] Now we know that series we're looking for is called "Star Wars The Clone Wars". Fetch series detail page for it and try to apply remaining words as episode filter, reducing filter by 1 word each time we get no results:
                    - .episodes().filteredBy(To Catch a Jedi)
                    - .episodes().filteredBy(To Catch a) / Jedi
                    - ...
                    - .episodes() / To Catch a Jedi

            Test queries:
                - "Star Wars The Clone Wars To Catch a Jedi"    -> S05E19
                - "Breaking Bad El Camino A Breaking Bad Movie" -> Special
                - "The Magicians (2015)"                        -> Year should be ignored
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
                var response = await RequestWithCookiesAsync(ApiUrl, method: RequestType.POST, data: data);
                if (response.ContentString == null)
                {
                    logger.Debug("> Empty series response for query: " + searchString);
                    continue;
                }

                try
                {
                    var json = JToken.Parse(response.ContentString);
                    if (json == null || json.Type == JTokenType.Array)
                    {
                        logger.Debug("> Invalid response for query: " + searchString);
                        continue; // Search loop
                    }

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
                        var season = query.Season is > 0 ? $"/season_{query.Season}" : "/seasons";
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
                            var episodeKeywords = keywords.Skip(searchKeywords + serieFilterKeywords);
                            var episodeFilterKeywords = episodeKeywords.Count();

                            // Search for episodes dropping 1 filter word each time when no results has found.
                            // Last search will be performed with empty filter
                            do
                            {
                                var filter = string.Join(" ", episodeKeywords.Take(episodeFilterKeywords));
                                logger.Debug("> Searching episodes with filter [" + filter + "]");
                                var taskReleases = await FetchSeriesReleases(url, query, filter);

                                if (taskReleases.Count() > 0)
                                {
                                    logger.Debug("> Found " + taskReleases.Count().ToString() + " episodes");
                                    releases.AddRange(taskReleases);
                                    break; // Episodes Filter loop
                                }
                            }
                            while (--episodeFilterKeywords >= 0);
                        }
                    }

                    break; // Search loop
                }
                catch (Exception ex)
                {
                    OnParseError(response.ContentString, ex);
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
            var results = await RequestWithCookiesAndRetryAsync(
                url, referer: SiteLink);
            var releases = new List<ReleaseInfo>();

            try
            {
                var parser = new HtmlParser();
                using var document = parser.ParseDocument(results.ContentString);
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
                OnParseError(results.ContentString, ex);
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
                using var document = parser.ParseDocument(results.ContentString);

                var playButton = document.QuerySelector("div.external-btn");
                if (playButton != null && !playButton.ClassList.Contains("inactive"))
                {
                    var details = new Uri(url);

                    var dateString = document.QuerySelector("div.title-block > div.details-pane > div.left-box").TextContent;
                    var key = (dateString.Contains("TBA")) ? "ru: " : "eng: ";
                    dateString = TrimString(dateString, key, " г."); // '... Дата выхода eng: 09 марта 2012 г. ...' -> '09 марта 2012'

                    DateTime date;
                    if (dateString.Length == 4)
                    {
                        // dateString might be just a year, e.g. https://www.lostfilm.tv/series/Ghosted/season_1/episode_14/
                        date = DateTime.TryParseExact(dateString, "yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsedDate) ? parsedDate : DateTime.Now;
                    }
                    else
                    {
                        // dd mmmm yyyy
                        date = DateTime.TryParse(dateString, new CultureInfo(Language), DateTimeStyles.AssumeLocal, out var parsedDate) ? parsedDate : DateTime.Now;
                    }

                    var urlDetails = new TrackerUrlDetails(playButton);
                    var episodeReleases = await FetchTrackerReleases(urlDetails);

                    foreach (var release in episodeReleases)
                    {
                        release.Details = details;
                        release.PublishDate = date;
                    }
                    releases.AddRange(episodeReleases);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.ContentString, ex);
            }

            return releases;
        }

        private async Task<List<ReleaseInfo>> FetchSeriesReleases(string url, TorznabQuery query, string filter)
        {
            logger.Debug("FetchSeriesReleases: " + url + " S: " + query.Season.ToString() + " E: " + query.Episode + " Filter: " + filter);

            var releases = new List<ReleaseInfo>();
            var results = await RequestWithCookiesAsync(url);

            try
            {
                var parser = new HtmlParser();
                using var document = parser.ParseDocument(results.ContentString);
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

                        var details = new Uri(url); // Current season(-s) page url

                        var urlDetails = new TrackerUrlDetails(seasonButton);
                        var seasonReleases = await FetchTrackerReleases(urlDetails);

                        foreach (var release in seasonReleases)
                        {
                            release.Details = details;
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
                            if (playButton == null) // #9725
                                continue;

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
                            var details = new Uri(episodeUrl);

                            var urlDetails = new TrackerUrlDetails(playButton);
                            var episodeReleases = await FetchTrackerReleases(urlDetails);

                            foreach (var release in episodeReleases)
                            {
                                release.Details = details;
                                release.PublishDate = date;
                            }
                            releases.AddRange(episodeReleases);
                        }
                        catch (Exception ex)
                        {
                            logger.Error(string.Format("{0}: Error while parsing row '{1}':\n\n{2}", Id, row.OuterHtml, ex));
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
                OnParseError(results.ContentString, ex);
            }

            return releases;
        }

        #endregion
        #region Tracker parsing

        private async Task<IReadOnlyList<ReleaseInfo>> FetchTrackerReleases(TrackerUrlDetails details)
        {
            var queryCollection = new NameValueCollection
            {
                { "c", details.seriesId },
                { "s", details.season },
                { "e", string.IsNullOrEmpty(details.episode) ? "999" : details.episode } // 999 is a synonym for the whole serie
            };
            var url = ReleaseUrl + "?" + queryCollection.GetQueryString();

            logger.Debug("FetchTrackerReleases: " + url);

            // Get redirection page with generated link on it. This link can't be constructed manually as it contains Hash field and hashing algo is unknown.
            var results = await RequestWithCookiesAsync(url);
            if (results.ContentString == null)
            {
                throw new ExceptionWithConfigData("Empty response from " + url, configData);
            }
            if (results.ContentString == "log in first")
            {
                throw new ExceptionWithConfigData(results.ContentString, configData);
            }

            try
            {
                var parser = new HtmlParser();
                using var document = parser.ParseDocument(results.ContentString);
                var meta = document.QuerySelector("meta");
                var metaContent = meta.GetAttribute("content");

                // Follow redirection defined by async url.replace
                var redirectionUrl = metaContent.Substring(metaContent.IndexOf("http"));
                return await FollowTrackerRedirection(redirectionUrl, details);
            }
            catch (Exception ex)
            {
                OnParseError(results.ContentString, ex);
            }

            // Failure path
            return Array.Empty<ReleaseInfo>();
        }

        private async Task<List<ReleaseInfo>> FollowTrackerRedirection(string url, TrackerUrlDetails details)
        {
            logger.Debug("FollowTrackerRedirection: " + url);
            var results = await RequestWithCookiesAsync(url);
            var releases = new List<ReleaseInfo>();

            try
            {
                var parser = new HtmlParser();
                using var document = parser.ParseDocument(results.ContentString);
                var rows = document.QuerySelectorAll("div.inner-box--item");

                if (rows.Count() > 0)
                {
                    logger.Debug("> Parsing " + rows.Count().ToString() + " releases");

                    var serieTitle = document.QuerySelector("div.inner-box--subtitle").TextContent;
                    serieTitle = serieTitle.Substring(0, serieTitle.LastIndexOf(','));

                    var episodeInfo = document.QuerySelector("div.inner-box--text").TextContent;
                    var episodeName = TrimString(episodeInfo, '(', ')');

                    foreach (var row in rows)
                    {
                        try
                        {

                            var detailsInfo = row.QuerySelector("div.inner-box--desc").TextContent;
                            var releaseDetails = parseReleaseDetailsRegex.Match(detailsInfo);

                            // ReSharper states "Expression is always false"
                            // TODO Refactor to get the intended operation
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

                            var techComponents = new[]
                            {
                            "rus",
                            quality,
                            "(LostFilm)"
                        };
                            var techInfo = string.Join(" ", techComponents.Where(s => !string.IsNullOrEmpty(s)));

                            // Ru title: downloadLink.TextContent.Replace("\n", "");
                            // En title should be manually constructed.
                            var titleComponents = new[] {
                            serieTitle, details.GetEpisodeString(), episodeName, techInfo
                        };
                            var downloadLink = row.QuerySelector("div.inner-box--link > a");
                            var sizeString = releaseDetails.Groups["size"].Value.ToUpper();
                            sizeString = sizeString.Replace("ТБ", "TB"); // untested
                            sizeString = sizeString.Replace("ГБ", "GB");
                            sizeString = sizeString.Replace("МБ", "MB");
                            sizeString = sizeString.Replace("КБ", "KB"); // untested
                            var link = new Uri(downloadLink.GetAttribute("href"));

                            // TODO this feels sparse compared to other trackers. Expand later
                            var release = new ReleaseInfo
                            {
                                Category = new[] { TorznabCatType.TV.ID },
                                Title = string.Join(" - ", titleComponents.Where(s => !string.IsNullOrEmpty(s))),
                                Link = link,
                                Guid = link,
                                Size = ParseUtil.GetBytes(sizeString),
                                // add missing torznab fields not available from results
                                Seeders = 1,
                                Peers = 2,
                                DownloadVolumeFactor = 0,
                                UploadVolumeFactor = 1,
                                MinimumRatio = 1,
                                MinimumSeedTime = 172800 // 48 hours
                            };

                            // TODO Other trackers don't have this log line. Remove or add to other trackers?
                            logger.Debug("> Add: " + release.Title);
                            releases.Add(release);
                        }
                        catch (Exception ex)
                        {
                            logger.Error(string.Format("{0}: Error while parsing row '{1}':\n\n{2}", Id, row.OuterHtml, ex));
                        }
                    }
                }
                else
                {
                    if (results.ContentString.Contains("Контент недоступен на территории Российской Федерации"))
                    {
                        logger.Debug($"> Content is not available in the Russian Federation");
                    }
                    else
                    {
                        var message = document.QuerySelector("p").TextContent;
                        logger.Debug($"> {message}");
                    }
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.ContentString, ex);
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
            var dateString = dateColumn.QuerySelector("span.small-text")?.TextContent;
            // 'Eng: 23.05.2017' -> '23.05.2017' OR '23.05.2017' -> '23.05.2017'
            dateString = (string.IsNullOrEmpty(dateString)) ? dateColumn.QuerySelector("span")?.TextContent : dateString.Substring(dateString.IndexOf(":") + 2);
            // dd.mm.yyyy
            return DateTime.TryParse(dateString, new CultureInfo(Language), DateTimeStyles.AssumeLocal, out var parsedDate) ? parsedDate : DateTime.Now;
        }

        #endregion
    }
}
