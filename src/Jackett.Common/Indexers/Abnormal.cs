using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers
{
    /// <summary>
    /// Provider for Abnormal Private French Tracker
    /// gazelle based but the ajax.php API seems to be broken (always returning failure)
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Abnormal : BaseCachingWebIndexer
    {
        private string LoginUrl => SiteLink + "login.php";
        private string SearchUrl => SiteLink + "torrents.php";
        private string DetailsUrl => SiteLink + "torrents.php?id=";
        private string ReplaceMulti => ConfigData.ReplaceMulti.Value;

        private ConfigurationDataAbnormal ConfigData
        {
            get => (ConfigurationDataAbnormal)configData;
            set => configData = value;
        }

        public Abnormal(IIndexerConfigurationService configService, WebClient w, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "abnormal",
                   name: "Abnormal",
                   description: "General French Private Tracker",
                   link: "https://abnormal.ws/",
                   caps: new TorznabCapabilities {
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
                   client: w,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   downloadBase: "https://abnormal.ws/torrents.php?action=download&id=",
                   configData: new ConfigurationDataAbnormal())
        {
            Language = "fr-fr";
            Encoding = Encoding.UTF8;
            Type = "private";

            AddCategoryMapping("MOVIE|DVDR", TorznabCatType.MoviesDVD, "DVDR");
            AddCategoryMapping("MOVIE|DVDRIP", TorznabCatType.MoviesSD, "DVDRIP");
            AddCategoryMapping("MOVIE|BDRIP", TorznabCatType.MoviesSD, "BDRIP");
            AddCategoryMapping("MOVIE|VOSTFR", TorznabCatType.MoviesOther, "VOSTFR");
            AddCategoryMapping("MOVIE|HD|720p", TorznabCatType.MoviesHD, "HD 720P");
            AddCategoryMapping("MOVIE|HD|1080p", TorznabCatType.MoviesHD, "HD 1080P");
            AddCategoryMapping("MOVIE|REMUXBR", TorznabCatType.MoviesBluRay, "REMUX BLURAY");
            AddCategoryMapping("MOVIE|FULLBR", TorznabCatType.MoviesBluRay, "FULL BLURAY");
            AddCategoryMapping("TV|SD|VOSTFR", TorznabCatType.TV, "TV SD VOSTFR");
            AddCategoryMapping("TV|HD|VOSTFR", TorznabCatType.TVHD, "TV HD VOSTFR");
            AddCategoryMapping("TV|SD|VF", TorznabCatType.TVSD, "TV SD VF");
            AddCategoryMapping("TV|HD|VF", TorznabCatType.TVHD, "TV HD VF");
            AddCategoryMapping("TV|PACK|FR", TorznabCatType.TVOther, "TV PACK FR");
            AddCategoryMapping("TV|PACK|VOSTFR", TorznabCatType.TVOther, "TV PACK VOSTFR");
            AddCategoryMapping("TV|EMISSIONS", TorznabCatType.TVOther, "TV EMISSIONS");
            AddCategoryMapping("ANIME", TorznabCatType.TVAnime, "ANIME");
            AddCategoryMapping("DOCS", TorznabCatType.TVDocumentary, "TV DOCS");
            AddCategoryMapping("MUSIC|FLAC", TorznabCatType.AudioLossless, "FLAC");
            AddCategoryMapping("MUSIC|MP3", TorznabCatType.AudioMP3, "MP3");
            AddCategoryMapping("MUSIC|CONCERT", TorznabCatType.AudioVideo, "CONCERT");
            AddCategoryMapping("PC|APP", TorznabCatType.PC, "PC");
            AddCategoryMapping("PC|GAMES", TorznabCatType.PCGames, "GAMES");
            AddCategoryMapping("EBOOKS", TorznabCatType.BooksEBook, "EBOOKS");
        }

        /// <summary>
        /// Configure our WiHD Provider
        /// </summary>
        /// <param name="configJson">Our params in Json</param>
        /// <returns>Configuration state</returns>
        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            // Retrieve config values set by Jackett's user
            LoadValuesFromJson(configJson);

            // Check & Validate Config
            ValidateConfig();

            // Building login form data
            var pairs = new Dictionary<string, string> {
                { "username", ConfigData.Username.Value },
                { "password", ConfigData.Password.Value },
                { "keeplogged", "1" },
                { "login", "Connexion" }
            };

            // Perform loggin
            logger.Info("\nAbnormal - Perform loggin.. with " + LoginUrl);
            var response = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, LoginUrl, true);

            // Test if we are logged in
            await ConfigureIfOK(response.Cookies, response.Cookies.Contains("session="), () =>
            {
                // Parse error page
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(response.ContentString);
                var message = dom.QuerySelector(".warning").TextContent.Split('.').Reverse().Skip(1).First();

                // Try left
                var left = dom.QuerySelector(".info").TextContent.Trim();

                // Oops, unable to login
                logger.Info("Abnormal - Login failed: \"" + message + "\" and " + left + " tries left before being banned for 6 hours !", "error");
                throw new ExceptionWithConfigData("Abnormal - Login failed: " + message, configData);
            });

            logger.Info("-> Login Success");

            return IndexerConfigurationStatus.RequiresTesting;
        }

        /// <summary>
        /// Execute our search query
        /// </summary>
        /// <param name="query">Query</param>
        /// <returns>Releases</returns>
        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var startTransition = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 3, 0, 0), 3, 5, DayOfWeek.Sunday);
            var endTransition = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 4, 0, 0), 10, 5, DayOfWeek.Sunday);
            var delta = new TimeSpan(1, 0, 0);
            var adjustment = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(1999, 10, 1), DateTime.MaxValue.Date, delta, startTransition, endTransition);
            TimeZoneInfo.AdjustmentRule[] adjustments = { adjustment };
            var FranceTz = TimeZoneInfo.CreateCustomTimeZone("W. Europe Standard Time", new TimeSpan(1, 0, 0), "(GMT+01:00) W. Europe Standard Time", "W. Europe Standard Time", "W. Europe DST Time", adjustments);

            var releases = new List<ReleaseInfo>();
            var qRowList = new List<IElement>();
            var searchTerm = query.GetQueryString();
            var searchUrl = SearchUrl;

            // Build our query
            var request = BuildQuery(searchTerm, query, searchUrl);

            // Getting results & Store content
            var parser = new HtmlParser();
            var dom = parser.ParseDocument(await QueryExecAsync(request));

            try
            {
                // Find torrent rows
                var firstPageRows = FindTorrentRows(dom);

                // Add them to torrents list
                qRowList.AddRange(firstPageRows);

                // Check if there are pagination links at bottom
                var qPagination = dom.QuerySelectorAll(".linkbox > a");
                int pageLinkCount;
                int nbResults;
                if (qPagination.Length > 0)
                {
                    // Calculate numbers of pages available for this search query (Based on number results and number of torrents on first page)
                    pageLinkCount = ParseUtil.CoerceInt(Regex.Match(qPagination.Last().GetAttribute("href").ToString(), @"\d+").Value);

                    // Calculate average number of results (based on torrents rows lenght on first page)
                    nbResults = firstPageRows.Length * pageLinkCount;
                }
                else
                {
                    // Check if we have a minimum of one result
                    if (firstPageRows.Length >= 1)
                    {
                        // Retrieve total count on our alone page
                        nbResults = firstPageRows.Length;
                        pageLinkCount = 1;
                    }
                    else
                    {
                        logger.Info("\nAbnormal - No result found for your query, please try another search term ...\n", "info");
                        // No result found for this query
                        return releases;
                    }
                }
                logger.Info("\nAbnormal - Found " + nbResults + " result(s) (+/- " + firstPageRows.Length + ") in " + pageLinkCount + " page(s) for this query !");
                logger.Info("\nAbnormal - There are " + firstPageRows.Length + " results on the first page !");

                // If we have a term used for search and pagination result superior to one
                if (!string.IsNullOrWhiteSpace(query.GetQueryString()) && pageLinkCount > 1)
                {
                    // Starting with page #2
                    for (var i = 2; i <= Math.Min(int.Parse(ConfigData.Pages.Value), pageLinkCount); i++)
                    {
                        logger.Info("\nAbnormal - Processing page #" + i);

                        // Build our query
                        var pageRequest = BuildQuery(searchTerm, query, searchUrl, i);

                        // Getting results & Store content
                        parser = new HtmlParser();
                        dom = parser.ParseDocument(await QueryExecAsync(pageRequest));

                        // Process page results
                        var additionalPageRows = FindTorrentRows(dom);

                        // Add them to torrents list
                        qRowList.AddRange(additionalPageRows);
                    }
                }

                // Loop on results
                foreach (var row in qRowList)
                {
                    // ID
                    var id = ParseUtil.CoerceInt(Regex.Match(row.QuerySelector("td:nth-of-type(2) > a").GetAttribute("href"), @"\d+").Value);

                    // Release Name
                    var name = row.QuerySelector("td:nth-of-type(2) > a").TextContent;
                    //issue #3847 replace multi keyword
                    if (!string.IsNullOrEmpty(ReplaceMulti))
                    {
                        var regex = new Regex("(?i)([\\.\\- ])MULTI([\\.\\- ])");
                        name = regex.Replace(name, "$1" + ReplaceMulti + "$2");
                    }

                    var categoryId = row.QuerySelector("td:nth-of-type(1) > a").GetAttribute("href").Replace("torrents.php?cat[]=", string.Empty);  // Category
                    var newznab = MapTrackerCatToNewznab(categoryId);                                                                               // Newznab Category
                    var seeders = ParseUtil.CoerceInt(Regex.Match(row.QuerySelector("td:nth-of-type(6)").TextContent, @"\d+").Value);               // Seeders
                    var leechers = ParseUtil.CoerceInt(Regex.Match(row.QuerySelector("td:nth-of-type(7)").TextContent, @"\d+").Value);              // Leechers
                    var completed = ParseUtil.CoerceInt(Regex.Match(row.QuerySelector("td:nth-of-type(6)").TextContent, @"\d+").Value);             // Completed
                    var sizeStr = row.QuerySelector("td:nth-of-type(5)").TextContent.Replace("Go", "gb").Replace("Mo", "mb").Replace("Ko", "kb");   // Size
                    var size = ReleaseInfo.GetBytes(sizeStr);                                                                                       // Size in bytes

                    // Publish DateToString
                    var datestr = row.QuerySelector("span.time").GetAttribute("title");
                    var dateLocal = DateTime.SpecifyKind(DateTime.ParseExact(datestr, "MMM dd yyyy, HH:mm", CultureInfo.InvariantCulture), DateTimeKind.Unspecified);
                    var date = TimeZoneInfo.ConvertTimeToUtc(dateLocal, FranceTz);

                    // Torrent Details URL
                    var details = new Uri(DetailsUrl + id);

                    // Torrent Download URL
                    Uri downloadLink = null;
                    var link = row.QuerySelector("td:nth-of-type(4) > a").GetAttribute("href");
                    if (!string.IsNullOrEmpty(link))
                    {
                        // Download link available
                        downloadLink = new Uri(SiteLink + link);
                    }
                    else
                    {
                        // No download link available -- Must be on pending ( can't be downloaded now...)
                        logger.Info("Abnormal - Download Link: Not available, torrent pending ? Skipping ...");
                        continue;
                    }

                    // Freeleech
                    var downloadVolumeFactor = 1;
                    if (row.QuerySelector("img[alt=\"Freeleech\"]") != null)
                    {
                        downloadVolumeFactor = 0;
                    }

                    // Building release infos
                    var release = new ReleaseInfo
                    {
                        Category = MapTrackerCatToNewznab(categoryId),
                        Title = name,
                        Seeders = seeders,
                        Peers = seeders + leechers,
                        PublishDate = date,
                        Size = size,
                        Guid = details,
                        Details = details,
                        Link = downloadLink,
                        MinimumRatio = 1,
                        MinimumSeedTime = 172800, // 48 hours
                        UploadVolumeFactor = 1,
                        DownloadVolumeFactor = downloadVolumeFactor
                    };
                    releases.Add(release);
                    logger.Info("Abnormal - Found Release: " + release.Title + "(" + id + ")");
                }
            }
            catch (Exception ex)
            {
                OnParseError("Error, unable to parse result \n" + ex.StackTrace, ex);
            }

            // Return found releases
            return releases;
        }

        /// <summary>
        /// Build query to process
        /// </summary>
        /// <param name="term">Term to search</param>
        /// <param name="query">Torznab Query for categories mapping</param>
        /// <param name="url">Search url for provider</param>
        /// <param name="page">Page number to request</param>
        /// <returns>URL to query for parsing and processing results</returns>
        private string BuildQuery(string term, TorznabQuery query, string url, int page = 0)
        {
            var parameters = new NameValueCollection();
            var categoriesList = MapTorznabCapsToTrackers(query);
            string categories = null;

            // Check if we are processing a new page
            if (page > 0)
            {
                // Adding page number to query
                parameters.Add("page", page.ToString());
            }

            // Loop on Categories needed
            foreach (var category in categoriesList)
            {
                // If last, build !
                if (categoriesList.Last() == category)
                {
                    // Adding previous categories to URL with latest category
                    parameters.Add(Uri.EscapeDataString("cat[]"), WebUtility.UrlEncode(category) + categories);
                }
                else
                {
                    // Build categories parameter
                    categories += "&" + Uri.EscapeDataString("cat[]") + "=" + WebUtility.UrlEncode(category);
                }
            }

            // If search term provided
            if (!string.IsNullOrWhiteSpace(term))
            {
                // Add search term
                parameters.Add("search", WebUtility.UrlEncode(term));
            }
            else
            {
                parameters.Add("search", WebUtility.UrlEncode("%"));
                // Showing all torrents (just for output function)
                term = "all";
            }

            // Building our query -- Cannot use GetQueryString due to UrlEncode (generating wrong cat[] param)
            url += "?" + string.Join("&", parameters.AllKeys.Select(a => a + "=" + parameters[a]));

            logger.Info("\nAbnormal - Builded query for \"" + term + "\"... " + url);

            // Return our search url
            return url;
        }

        /// <summary>
        /// Switch Method for Querying
        /// </summary>
        /// <param name="request">URL created by Query Builder</param>
        /// <returns>Results from query</returns>
        private async Task<string> QueryExecAsync(string request)
        {
            string results = null;

            // Querying tracker directly
            results = await QueryTrackerAsync(request);

            return results;
        }

        /// <summary>
        /// Get Torrents Page from Tracker by Query Provided
        /// </summary>
        /// <param name="request">URL created by Query Builder</param>
        /// <returns>Results from query</returns>
        private async Task<string> QueryTrackerAsync(string request)
        {
            // Cache mode not enabled or cached file didn't exist for our query
            logger.Info("\nAbnormal - Querying tracker for results....");

            // Request our first page
            var results = await RequestWithCookiesAndRetryAsync(request);

            // Return results from tracker
            return results.ContentString;
        }

        /// <summary>
        /// Find torrent rows in search pages
        /// </summary>
        /// <returns>List of rows</returns>
        private IHtmlCollection<IElement> FindTorrentRows(IHtmlDocument dom) =>
            dom.QuerySelectorAll(".torrent_table > tbody > tr:not(.colhead)");

        /// <summary>
        /// Validate Config entered by user on Jackett
        /// </summary>
        private void ValidateConfig()
        {
            logger.Info("\nAbnormal - Validating Settings ... \n");

            // Check Username Setting
            if (string.IsNullOrEmpty(ConfigData.Username.Value))
            {
                throw new ExceptionWithConfigData("You must provide a username for this tracker to login !", ConfigData);
            }
            else
            {
                logger.Info("Abnormal - Validated Setting -- Username (auth) => " + ConfigData.Username.Value.ToString());
            }

            // Check Password Setting
            if (string.IsNullOrEmpty(ConfigData.Password.Value))
            {
                throw new ExceptionWithConfigData("You must provide a password with your username for this tracker to login !", ConfigData);
            }
            else
            {
                logger.Info("Abnormal - Validated Setting -- Password (auth) => " + ConfigData.Password.Value.ToString());
            }

            // Check Max Page Setting
            if (!string.IsNullOrEmpty(ConfigData.Pages.Value))
            {
                try
                {
                    logger.Info("Abnormal - Validated Setting -- Max Pages => " + Convert.ToInt32(ConfigData.Pages.Value));
                }
                catch (Exception)
                {
                    throw new ExceptionWithConfigData("Please enter a numeric maximum number of pages to crawl !", ConfigData);
                }
            }
            else
            {
                throw new ExceptionWithConfigData("Please enter a maximum number of pages to crawl !", ConfigData);
            }
        }
    }
}
