using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;
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
        private string TorrentDetailsUrl => SiteLink + "Torrent/Details?ReleaseId={id}";
        private string TorrentDownloadUrl => SiteLink + "Torrent/Download?ReleaseId={id}";
        private string LoginUrl => SiteLink + "Home/Login";
        private string SearchUrl => SiteLink + "Torrent";
        private string WebRequestDelay => ((SingleSelectConfigurationItem)configData.GetDynamic("webRequestDelay")).Value;
        private int MaxPages => Convert.ToInt32(((SingleSelectConfigurationItem)configData.GetDynamic("maxPages")).Value);
        private string MultiReplacement => ((StringConfigurationItem)configData.GetDynamic("multiReplacement")).Value;
        private bool SubReplacement => ((BoolConfigurationItem)configData.GetDynamic("subReplacement")).Value;
        private bool EnhancedAnimeSearch => ((BoolConfigurationItem)configData.GetDynamic("enhancedAnimeSearch")).Value;

        public override string[] LegacySiteLinks { get; protected set; } = {
            "https://abnormal.ws"
        };
        private ConfigurationDataBasicLogin ConfigData => (ConfigurationDataBasicLogin)configData;

        public Abnormal(IIndexerConfigurationService configService, WebClient w, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "abnormal",
                   name: "Abnormal",
                   description: "General French Private Tracker",
                   link: "https://abn.lol/",
                   caps: new TorznabCapabilities
                   {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                       },
                       MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q
                       },
                       BookSearchParams = new List<BookSearchParam>
                       {
                           BookSearchParam.Q
                       }
                   },
                   configService: configService,
                   client: w,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   downloadBase: "https://abn.lol/Torrent/Download?ReleaseId=",
                   configData: new ConfigurationDataBasicLogin()
                   )
        {
            Encoding = Encoding.UTF8;
            Language = "fr-FR";
            Type = "private";

            AddCategoryMapping(1, TorznabCatType.TV, "Series");
            AddCategoryMapping(2, TorznabCatType.Movies, "Movies");
            AddCategoryMapping(3, TorznabCatType.TVDocumentary, "Documentaries");
            AddCategoryMapping(4, TorznabCatType.TVAnime, "Anime");
            AddCategoryMapping(5, TorznabCatType.PCGames, "Games");
            AddCategoryMapping(6, TorznabCatType.PC, "Applications");
            AddCategoryMapping(7, TorznabCatType.BooksEBook, "Ebooks");
            AddCategoryMapping(9, TorznabCatType.TV, "Emissions");

            // Dynamic Configuration
            ConfigData.AddDynamic("advancedConfigurationWarning", new DisplayInfoConfigurationItem(string.Empty, "<center><b>Advanced Configuration</b></center>,<br /><br /> <center><b><u>WARNING !</u></b> <i>Be sure to read instructions before editing options bellow, you can <b>drastically reduce performance</b> of queries or have <b>non-accurate results</b>.</i></center><br/><br/><ul><li><b>Delay between Requests</b>: (<i>not recommended</i>) you can increase delay to requests made to the tracker, but a minimum of 2.1s is enforced as there is an anti-spam protection.</li><br /><li><b>Max Pages</b>: (<i>not recommended</i>) you can increase max pages to follow when making a request. But be aware that others apps can consider this indexer not working if jackett take too many times to return results. </li><br /><li><b>Enhanced Anime</b>: if you have \"Anime\", this will improve queries made to this tracker related to this type when making searches.</li><br /><li><b>Multi Replacement</b>: you can dynamically replace the word \"MULTI\" with another of your choice like \"MULTI.FRENCH\" for better analysis of 3rd party softwares.</li><br /><li><b>Sub Replacement</b>: you can dynamically replace the word \"VOSTFR\" or \"SUBFRENCH\" with the word \"ENGLISH\" for better analysis of 3rd party softwares.</li></ul>"));

            var ConfigWebRequestDelay = new SingleSelectConfigurationItem("Which delay do you want to apply between each requests made to tracker ?", new Dictionary<string, string>
            {
                {"0", "0s (disabled)"},
                {"0.1", "0.1s"},
                {"0.3", "0.3s"},
                {"0.5", "0.5s (default)" },
                {"0.7", "0.7s" },
                {"1.0", "1.0s"},
                {"1.25", "1.25s"},
                {"1.50", "1.50s"}
            })
            { Value = "0.5" };
            ConfigData.AddDynamic("webRequestDelay", ConfigWebRequestDelay);

            var ConfigMaxPages = new SingleSelectConfigurationItem("How many pages do you want to follow ?", new Dictionary<string, string>
            {
                {"1", "1 (50 results - default / best perf.)"},
                {"2", "2 (100 results)"},
                {"3", "3 (150 results)"},
                {"4", "4 (200 results - hard limit max)" },
            })
            { Value = "1" };
            ConfigData.AddDynamic("maxPages", ConfigMaxPages);

            var ConfigEnhancedAnimeSearch = new BoolConfigurationItem("Do you want to use enhanced ANIME search ?") { Value = false };
            ConfigData.AddDynamic("enhancedAnimeSearch", ConfigEnhancedAnimeSearch);

            var ConfigMultiReplacement = new StringConfigurationItem("Do you want to replace \"MULTI\" keyword in release title by another word ?") { Value = "MULTI.FRENCH" };
            ConfigData.AddDynamic("multiReplacement", ConfigMultiReplacement);

            var ConfigSubReplacement = new BoolConfigurationItem("Do you want to replace \"VOSTFR\" and \"SUBFRENCH\" with \"ENGLISH\" word ?") { Value = false };
            ConfigData.AddDynamic("subReplacement", ConfigSubReplacement);

            webclient.requestDelay = Convert.ToDouble(WebRequestDelay);
        }

        /// <summary>
        /// Configure our Provider
        /// </summary>
        /// <param name="configJson">Our params in Json</param>
        /// <returns>Configuration state</returns>
        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            // Provider not yet configured
            IsConfigured = false;

            // Retrieve config values set by Jackett's user
            LoadValuesFromJson(configJson);

            // Check & Validate Config
            logger.Debug("\nAbnormal - Validating Settings ...");

            // Check Username Setting
            if (string.IsNullOrEmpty(ConfigData.Username.Value))
            {
                throw new ExceptionWithConfigData("You must provide a username for this tracker to login !", ConfigData);
            }
            else
            {
                logger.Debug("\nAbnormal - Validated Setting -- Username (auth) => " + ConfigData.Username.Value.ToString());
            }

            // Check Password Setting
            if (string.IsNullOrEmpty(ConfigData.Password.Value))
            {
                throw new ExceptionWithConfigData("You must provide a password with your username for this tracker to login !", ConfigData);
            }
            else
            {
                logger.Debug("\nAbnormal - Validated Setting -- Password (auth) => " + ConfigData.Password.Value.ToString());
            }

            // Building login form data
            var pairs = new Dictionary<string, string> {
                { "Username", ConfigData.Username.Value },
                { "Password", ConfigData.Password.Value },
                { "RememberMe", "true" },
            };

            // Get CSRF Token
            logger.Debug("\nAbnormal - Getting CSRF token for " + LoginUrl);
            var response = await RequestWithCookiesAsync(LoginUrl);

            var loginResultParser = new HtmlParser();
            var loginResultDocument = loginResultParser.ParseDocument(response.ContentString);
            var csrfToken = loginResultDocument.QuerySelector("input[name=\"__RequestVerificationToken\"]").GetAttribute("value");
            pairs.Add("__RequestVerificationToken", csrfToken);

            // Perform loggin
            logger.Debug("\nAbnormal - Perform loggin.. with " + LoginUrl);
            response = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, LoginUrl, true);

            // Test if we are logged in
            await ConfigureIfOK(response.Cookies, response.Cookies.Contains(".AspNetCore.Identity.Application="), () =>
            {
                // Parse error page
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(response.ContentString);
                var message = dom.QuerySelector(".validation-summary-errors").TextContent.Split('.').Reverse().Skip(1).First();

                // Oops, unable to login
                logger.Debug("Abnormal - Login failed: \"" + message, "error");
                throw new ExceptionWithConfigData("\nAbnormal - Login failed: " + message, configData);
            });

            logger.Debug("\nAbnormal - Login Success");

            return IndexerConfigurationStatus.RequiresTesting;
        }

        /// <summary>
        /// Execute our search query
        /// </summary>
        /// <param name="query">Query</param>
        /// <returns>Releases</returns>
        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchTerm = query.SanitizedSearchTerm + " " + query.GetEpisodeSearchString();

            if (EnhancedAnimeSearch && query.HasSpecifiedCategories && (query.Categories.Contains(TorznabCatType.TVAnime.ID) || query.Categories.Contains(100032) || query.Categories.Contains(100101) || query.Categories.Contains(100110)))
            {
                var regex = new Regex(" ([0-9]+)");
                searchTerm = regex.Replace(searchTerm, " E$1");
            }

            searchTerm = searchTerm.Trim();
            searchTerm = searchTerm.ToLower();
            searchTerm = searchTerm.Replace(" ", ".");

            // Multiple page support
            var nextPage = 1;
            var followingPages = true;
            do
            {

                // Build our query
                var request = BuildQuery(searchTerm, query, SearchUrl, nextPage);

                // Getting results
                logger.Info("\nAbnormal - Querying API page " + nextPage);
                var dom = new HtmlParser().ParseDocument(await QueryExecAsync(request));
                var results = dom.QuerySelectorAll(".table-rows > tbody > tr:not(.mvc-grid-empty-row)");

                // Torrents Result Count
                var torrentsCount = results.Length;

                try
                {
                    // If contains torrents
                    if (torrentsCount > 0)
                    {
                        logger.Info("\nAbnormal - Found " + torrentsCount + " torrents on current page.");

                        // Adding each torrent row to releases
                        releases.AddRange(results.Select(torrent =>
                            {
                                // Selectors
                                var id = torrent.QuerySelector("td.grid-release-column > a").GetAttribute("href");      // ID
                                var name = torrent.QuerySelector("td.grid-release-column > a").TextContent;             // Release Name 
                                var categoryId = torrent.QuerySelector("td.grid-cat-column > a").GetAttribute("href");  // Category
                                var completed = torrent.QuerySelector("td:nth-of-type(3)").TextContent;                 // Completed
                                var seeders = torrent.QuerySelector("td.text-green").TextContent;                       // Seeders
                                var leechers = torrent.QuerySelector("td.text-red").TextContent;                        // Leechers
                                var size = torrent.QuerySelector("td:nth-of-type(5)").TextContent;                      // Size

                                var release = new ReleaseInfo
                                {
                                    // Mapping data
                                    Category = MapTrackerCatToNewznab(Regex.Match(categoryId, @"\d+").Value),
                                    Title = name,
                                    Seeders = int.Parse(Regex.Match(seeders, @"\d+").Value),
                                    Peers = int.Parse(Regex.Match(seeders, @"\d+").Value) + int.Parse(Regex.Match(leechers, @"\d+").Value),
                                    Grabs = int.Parse(Regex.Match(completed, @"\d+").Value) + int.Parse(Regex.Match(leechers, @"\d+").Value),
                                    MinimumRatio = 1,
                                    MinimumSeedTime = 172800,
                                    Size = ReleaseInfo.GetBytes(size.Replace("Go", "gb").Replace("Mo", "mb").Replace("Ko", "kb")),
                                    UploadVolumeFactor = 1,
                                    DownloadVolumeFactor = 1,
                                    PublishDate = DateTime.Now,
                                    Guid = new Uri(TorrentDetailsUrl.Replace("{id}", Regex.Match(id, @"\d+").Value)),
                                    Details = new Uri(TorrentDetailsUrl.Replace("{id}", Regex.Match(id, @"\d+").Value)),
                                    Link = new Uri(TorrentDownloadUrl.Replace("{id}", Regex.Match(id, @"\d+").Value))
                                };

                                // Multi Replacement
                                if (!string.IsNullOrEmpty(MultiReplacement))
                                {
                                    var regex = new Regex("(?i)([\\.\\- ])MULTI([\\.\\- ])");
                                    release.Title = regex.Replace(release.Title, "$1" + MultiReplacement + "$2");
                                }

                                // Sub Replacement
                                if (SubReplacement)
                                    release.Title = release.Title.Replace("VOSTFR", "ENGLISH").Replace("SUBFRENCH", "ENGLISH");

                                // Freeleech
                                if (torrent.QuerySelector("img[alt=\"Freeleech\"]") != null)
                                {
                                    release.DownloadVolumeFactor = 0;
                                }

                                return release;
                            }));
                        if (torrentsCount == 50)
                        {
                            // Is there more pages to follow ?
                            var morePages = dom.QuerySelectorAll("div.mvc-grid-pager > button").Last().GetAttribute("tabindex");
                            if (morePages == "-1")
                                followingPages = false;
                        }
                        nextPage++;
                    }
                    else
                    {
                        logger.Info("\nAbnormal - No results found on page  " + nextPage + ", stopping follow of next page.");
                        //  No results or no more results available
                        followingPages = false;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    OnParseError("Unable to parse result \n" + ex.StackTrace, ex);
                }

                // Stop ?
                if (torrentsCount < int.Parse(dom.QuerySelector(".mvc-grid-pager-rows").GetAttribute("value")))
                {
                    logger.Info("\nAbnormal - Stopping follow of next page " + nextPage + " due max available results reached.");
                    break;
                }
                else if (nextPage > MaxPages)
                {
                    logger.Info("\nAbnormal - Stopping follow of next page " + nextPage + " due to page limit reached.");
                    break;
                }
                else if (query.IsTest)
                {
                    logger.Info("\nAbnormal - Stopping follow of next page " + nextPage + " due to index test query.");
                    break;
                }

            } while (followingPages);

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

            // Pages handling
            if (page > 1 && !query.IsTest)
            {
                parameters.Add("page", page.ToString());
            }

            // Loop on Categories needed
            foreach (var category in categoriesList)
            {
                // If last, build !
                if (categoriesList.Last() == category)
                {
                    // Adding previous categories to URL with latest category
                    parameters.Add(Uri.EscapeDataString("SelectedCats="), WebUtility.UrlEncode(category) + categories);
                }
                else
                {
                    // Build categories parameter
                    categories += "&" + Uri.EscapeDataString("SelectedCats=") + "=" + WebUtility.UrlEncode(category);
                }
            }

            // If search term provided
            if (!string.IsNullOrWhiteSpace(term))
            {
                // Add search term
                parameters.Add("Search", WebUtility.UrlEncode(term));
                url += "?" + string.Join("&", parameters.AllKeys.Select(a => a + "=" + parameters[a]));
            }
            else
            {
                // Showing all torrents (just for output function)
                term = "all";
            }

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

            // Querying tracker directly
            var results = await QueryTrackerAsync(request);

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
    }
}
