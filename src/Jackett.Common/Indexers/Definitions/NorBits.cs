using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Jackett.Common.Helpers;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class NorBits : BaseCachingWebIndexer
    {
        public override string Id => "norbits";
        public override string Name => "NorBits";
        public override string Description => "NorBits is a Norwegian Private site for MOVIES / TV / GENERAL";
        public override string SiteLink { get; protected set; } = "https://norbits.net/";
        public override Encoding Encoding => Encoding.GetEncoding("iso-8859-1");
        public override string Language => "nb-NO";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private string LoginUrl => SiteLink + "login.php";
        private string LoginCheckUrl => SiteLink + "takelogin.php";
        private string SearchUrl => SiteLink + "browse.php";

        private ConfigurationDataNorbits ConfigData => (ConfigurationDataNorbits)configData;

        public NorBits(IIndexerConfigurationService configService, WebClient w, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(configService: configService,
                   client: w,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataNorbits())
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
                    MovieSearchParam.Q, MovieSearchParam.ImdbId
                },
                MusicSearchParams = new List<MusicSearchParam>
                {
                    MusicSearchParam.Q
                },
                BookSearchParams = new List<BookSearchParam>
                {
                    BookSearchParam.Q
                }
            };

            caps.Categories.AddCategoryMapping("main_cat[]=1&sub2_cat[]=49", TorznabCatType.MoviesUHD, "Filmer - UHD-2160p");
            caps.Categories.AddCategoryMapping("main_cat[]=1&sub2_cat[]=19", TorznabCatType.MoviesHD, "Filmer - HD-1080p/i");
            caps.Categories.AddCategoryMapping("main_cat[]=1&sub2_cat[]=20", TorznabCatType.MoviesHD, "Filmer - HD-720p");
            caps.Categories.AddCategoryMapping("main_cat[]=1&sub2_cat[]=22", TorznabCatType.MoviesSD, "Filmer - SD");
            caps.Categories.AddCategoryMapping("main_cat[]=2&sub2_cat[]=49", TorznabCatType.TVUHD, "TV - UHD-2160p");
            caps.Categories.AddCategoryMapping("main_cat[]=2&sub2_cat[]=19", TorznabCatType.TVHD, "TV - HD-1080p/i");
            caps.Categories.AddCategoryMapping("main_cat[]=2&sub2_cat[]=20", TorznabCatType.TVHD, "TV - HD-720p");
            caps.Categories.AddCategoryMapping("main_cat[]=2&sub2_cat[]=22", TorznabCatType.TVSD, "TV - SD");
            caps.Categories.AddCategoryMapping("main_cat[]=3", TorznabCatType.PC, "Programmer");
            caps.Categories.AddCategoryMapping("main_cat[]=4", TorznabCatType.Console, "Spill");
            caps.Categories.AddCategoryMapping("main_cat[]=5&sub2_cat[]=42", TorznabCatType.AudioMP3, "Musikk - 192");
            caps.Categories.AddCategoryMapping("main_cat[]=5&sub2_cat[]=43", TorznabCatType.AudioMP3, "Musikk - 256");
            caps.Categories.AddCategoryMapping("main_cat[]=5&sub2_cat[]=44", TorznabCatType.AudioMP3, "Musikk - 320");
            caps.Categories.AddCategoryMapping("main_cat[]=5&sub2_cat[]=45", TorznabCatType.AudioMP3, "Musikk - VBR");
            caps.Categories.AddCategoryMapping("main_cat[]=5&sub2_cat[]=46", TorznabCatType.AudioLossless, "Musikk - Lossless");
            caps.Categories.AddCategoryMapping("main_cat[]=6", TorznabCatType.Books, "Tidsskrift");
            caps.Categories.AddCategoryMapping("main_cat[]=7", TorznabCatType.AudioAudiobook, "Lydbøker");
            caps.Categories.AddCategoryMapping("main_cat[]=8&sub2_cat[]=19", TorznabCatType.AudioVideo, "Musikkvideoer - HD-1080p/i");
            caps.Categories.AddCategoryMapping("main_cat[]=8&sub2_cat[]=20", TorznabCatType.AudioVideo, "Musikkvideoer - HD-720p");
            caps.Categories.AddCategoryMapping("main_cat[]=8&sub2_cat[]=22", TorznabCatType.AudioVideo, "Musikkvideoer - SD");
            caps.Categories.AddCategoryMapping("main_cat[]=40", TorznabCatType.AudioOther, "Podcasts");

            return caps;
        }

        /// <summary>
        /// Configure our FADN Provider
        /// </summary>
        /// <param name="configJson">Our params in Json</param>
        /// <returns>Configuration state</returns>
        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            // Retrieve config values set by Jackett's user
            LoadValuesFromJson(configJson);

            // Check & Validate Config
            ValidateConfig();

            await DoLoginAsync();

            return IndexerConfigurationStatus.RequiresTesting;
        }

        /// <summary>
        /// Perform login to racker
        /// </summary>
        /// <returns></returns>
        private async Task DoLoginAsync()
        {
            // Build WebRequest for index
            var myIndexRequest = new WebRequest
            {
                Type = RequestType.GET,
                Url = SiteLink,
                Encoding = Encoding
            };

            // Get index page for cookies
            logger.Info("\nNorBits - Getting index page (for cookies).. with " + SiteLink);
            var indexPage = await webclient.GetResultAsync(myIndexRequest);

            // Building login form data
            var pairs = new Dictionary<string, string> {
                { "username", ConfigData.Username.Value },
                { "password", ConfigData.Password.Value },
                { "logout", "no" },
                { "returnto", "/" }
            };

            // Use 2FA code if defined
            if (!string.IsNullOrEmpty(ConfigData.TwoFactorAuth.Value))
            {
                pairs.Add("code", ConfigData.TwoFactorAuth.Value);
            }

            // Build WebRequest for login
            var myRequestLogin = new WebRequest
            {
                Type = RequestType.GET,
                Url = LoginUrl,
                Cookies = indexPage.Cookies,
                Referer = SiteLink,
                Encoding = Encoding
            };

            // Get login page -- (not used, but simulation needed by tracker security's checks)
            logger.Info("\nNorBits - Getting login page (user simulation).. with " + LoginUrl);
            await webclient.GetResultAsync(myRequestLogin);

            // Build WebRequest for submitting authentication
            var request = new WebRequest
            {
                PostData = pairs,
                Referer = LoginUrl,
                Type = RequestType.POST,
                Url = LoginCheckUrl,
                Cookies = indexPage.Cookies,
                Encoding = Encoding
            };

            logger.Info("\nPerform login with " + LoginCheckUrl);
            var response = await webclient.GetResultAsync(request);

            // Test if we are logged in
            await ConfigureIfOK(response.Cookies, response.Cookies != null && response.Cookies.Contains("uid="), () =>
            {
                // Default error message
                var message = "Error during attempt !";
                // Parse redirect header
                var redirectTo = response.RedirectingTo;

                // Oops, unable to login
                logger.Info("NorBits - Login failed: " + message, "error");
                throw new ExceptionWithConfigData("Login failed: " + message, configData);
            });

            logger.Info("\nNorBits - Cookies saved for future uses...");
            ConfigData.CookieHeader.Value = indexPage.Cookies + " " + response.Cookies + " ts_username=" + ConfigData.Username.Value;

            logger.Info("\nNorBits - Login Success\n");
        }

        /// <summary>
        /// Check logged-in state for provider
        /// </summary>
        /// <returns></returns>
        private async Task CheckLoginAsync()
        {
            // Checking ...
            logger.Info("\nNorBits -  Checking logged-in state....");
            var loggedInCheck = await RequestWithCookiesAsync(SearchUrl);
            if (!loggedInCheck.ContentString.Contains("logout.php"))
            {
                // Cookie expired, renew session on provider
                logger.Info("NorBits - Not logged, login now...\n");

                await DoLoginAsync();
            }
            else
            {
                // Already logged, session active
                logger.Info("NorBits - Already logged, continue...\n");
            }
        }

        /// <summary>
        /// Execute our search query
        /// </summary>
        /// <param name="query">Query</param>
        /// <returns>Releases</returns>
        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var exactSearchTerm = query.GetQueryString();
            var searchUrl = SearchUrl;

            // Check login before performing a query
            await CheckLoginAsync();

            var searchTerms = new List<string> { exactSearchTerm };

            // duplicate search without diacritics
            var baseSearchTerm = StringUtil.RemoveDiacritics(exactSearchTerm);
            if (baseSearchTerm != exactSearchTerm)
            {
                searchTerms.Add(baseSearchTerm);
            }

            foreach (var searchTerm in searchTerms)
            {
                // Build our query
                var request = BuildQuery(searchTerm, query, searchUrl);

                // Getting results & Store content
                var response = await RequestWithCookiesAndRetryAsync(request, ConfigData.CookieHeader.Value);
                var parser = new HtmlParser();
                using var dom = parser.ParseDocument(response.ContentString);

                try
                {
                    var firstPageRows = FindTorrentRows(dom);

                    // If pagination available
                    int nbResults;
                    var pageLinkCount = 1;

                    // Check if we have a minimum of one result
                    if (firstPageRows?.Length >= 1)
                    {
                        // Retrieve total count on our alone page
                        nbResults = firstPageRows.Count();
                    }
                    else
                    {
                        // No result found for this query
                        logger.Info("\nNorBits - No result found for your query, please try another search term ...\n", "info");
                        break;
                    }

                    logger.Info("\nNorBits - Found " + nbResults + " result(s) (+/- " + firstPageRows.Length + ") in " + pageLinkCount + " page(s) for this query !");
                    logger.Info("\nNorBits - There are " + firstPageRows.Length + " results on the first page !");

                    foreach (var row in firstPageRows)
                    {
                        var link = new Uri(SiteLink + row.QuerySelector("td:nth-of-type(2) > a[href*=\"download.php?id=\"]")?.GetAttribute("href").TrimStart('/'));
                        var qDetails = row.QuerySelector("td:nth-of-type(2) > a[href*=\"details.php?id=\"]");

                        var title = qDetails?.GetAttribute("title").Trim();
                        var details = new Uri(SiteLink + qDetails?.GetAttribute("href").TrimStart('/'));

                        var mainCategory = row.QuerySelector("td:nth-of-type(1) > div > a[href*=\"main_cat[]\"]")?.GetAttribute("href")?.Split('?').Last();
                        var secondCategory = row.QuerySelector("td:nth-of-type(1) > div > a[href*=\"sub2_cat[]\"]")?.GetAttribute("href")?.Split('?').Last();

                        var categoryList = new[] { mainCategory, secondCategory };
                        var cat = string.Join("&", categoryList.Where(c => !string.IsNullOrWhiteSpace(c)));

                        var seeders = ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(9)").TextContent);
                        var leechers = ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(10)").TextContent);

                        var release = new ReleaseInfo
                        {
                            Guid = details,
                            Details = details,
                            Link = link,
                            Title = title,
                            Category = MapTrackerCatToNewznab(cat),
                            Size = ParseUtil.GetBytes(row.QuerySelector("td:nth-of-type(7)").TextContent),
                            Files = ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(3) > a")?.TextContent.Trim()),
                            Grabs = ParseUtil.CoerceLong(row.QuerySelector("td:nth-of-type(8)")?.FirstChild?.TextContent.Trim()),
                            Seeders = seeders,
                            Peers = seeders + leechers,
                            PublishDate = DateTime.ParseExact(row.QuerySelector("td:nth-of-type(5)")?.TextContent.Trim(), "yyyy-MM-ddHH:mm:ss", CultureInfo.InvariantCulture),
                            DownloadVolumeFactor = 1,
                            UploadVolumeFactor = 1,
                            MinimumRatio = 1,
                            MinimumSeedTime = 172800 // 48 hours
                        };

                        var genres = row.QuerySelector("span.genres")?.TextContent;
                        if (!string.IsNullOrEmpty(genres))
                        {
                            genres = genres.Trim().Replace("\xA0", " ").Replace("(", "").Replace(")", "").Replace(" | ", ",");
                            release.Description = genres;
                            release.Genres ??= new List<string>();
                            release.Genres = release.Genres.Union(genres.Split(',')).ToList();
                        }

                        // IMDB
                        var imdbLink = row.QuerySelector("a[href*=\"imdb.com/title/tt\"]")?.GetAttribute("href");
                        release.Imdb = ParseUtil.GetLongFromString(imdbLink);

                        if (row.QuerySelector("img[title=\"100% freeleech\"]") != null)
                        {
                            release.DownloadVolumeFactor = 0;
                        }
                        else if (row.QuerySelector("img[title=\"Halfleech\"]") != null)
                        {
                            release.DownloadVolumeFactor = 0.5;
                        }
                        else if (row.QuerySelector("img[title=\"90% Freeleech\"]") != null)
                        {
                            release.DownloadVolumeFactor = 0.1;
                        }

                        releases.Add(release);
                    }
                }
                catch (Exception ex)
                {
                    OnParseError("Error, unable to parse result \n" + ex.StackTrace, ex);
                }
            }
            // Return found releases
            return releases;
        }

        /// <summary>
        /// Build query to process
        /// </summary>
        /// <param name="term">Term to search</param>
        /// <param name="query">Torznab Query for categories mapping</param>
        /// <param name="searchUrl">Search url for provider</param>
        /// <param name="page">Page number to request</param>
        /// <returns>URL to query for parsing and processing results</returns>
        private string BuildQuery(string term, TorznabQuery query, string searchUrl, int page = 0)
        {
            var searchterm = term;

            // Building our tracker query
            var parameters = new NameValueCollection
            {
                { "incldead", "1" },
                { "fullsearch", ConfigData.UseFullSearch.Value ? "1" : "0" },
                { "scenerelease", "0" }
            };

            // If search term provided
            if (!string.IsNullOrWhiteSpace(query.ImdbID))
            {
                searchterm = "imdbsearch=" + query.ImdbID;
            }
            else if (!string.IsNullOrWhiteSpace(term))
            {
                searchterm = "search=" + WebUtilityHelpers.UrlEncode(term, Encoding.GetEncoding(28591));
            }
            else
            {
                // Showing all torrents (just for output function)
                searchterm = "search=";
                term = "all";
            }

            if (ConfigData.freeleech.Value)
            {
                parameters.Add("FL", "1");
            }

            // Building our query
            searchUrl += "?" + searchterm + "&" + parameters.GetQueryString();

            var categoriesList = MapTorznabCapsToTrackers(query);
            if (categoriesList.Any())
            {
                searchUrl += "&" + string.Join("&", categoriesList);
            }

            logger.Info("\nBuilded query for \"" + term + "\"... " + searchUrl);

            return searchUrl;
        }

        /// <summary>
        /// Switch Method for Querying
        /// </summary>
        /// <param name="request">URL created by Query Builder</param>
        /// <returns>Results from query</returns>
        private async Task<WebResult> QueryExecAsync(string request)
        {
            WebResult results;
            results = await QueryTrackerAsync(request);
            return results;
        }

        /// <summary>
        /// Get Torrents Page from Tracker by Query Provided
        /// </summary>
        /// <param name="request">URL created by Query Builder</param>
        /// <returns>Results from query</returns>
        private async Task<WebResult> QueryTrackerAsync(string request)
        {
            // Cache mode not enabled or cached file didn't exist for our query
            logger.Info("\nNorBits - Querying tracker for results....");

            // Request our first page
            var results = await RequestWithCookiesAndRetryAsync(request, ConfigData.CookieHeader.Value, RequestType.GET, SearchUrl, null);

            // Return results from tracker
            return results;
        }

        /// <summary>
        /// Find torrent rows in search pages
        /// </summary>
        /// <returns>List of rows</returns>
        private IHtmlCollection<IElement> FindTorrentRows(IHtmlDocument dom) =>
           dom.QuerySelectorAll("#torrentTable > tbody > tr").Skip(1).ToCollection();

        /// <summary>
        /// Download torrent file from tracker
        /// </summary>
        /// <param name="link">URL string</param>
        /// <returns></returns>
        public override async Task<byte[]> Download(Uri link)
        {
            // Retrieving ID from link provided
            var id = ParseUtil.CoerceInt(Regex.Match(link.AbsoluteUri, @"\d+").Value);
            logger.Info("NorBits - Torrent Requested ID: " + id);

            // Building login form data
            var pairs = new Dictionary<string, string> {
                { "torrentid", id.ToString() },
                { "_", string.Empty } // ~~ Strange, blank param...
            };

            // Get torrent file now
            var response = await base.Download(link);

            // Return content
            return response;
        }

        /// <summary>
        /// Validate Config entered by user on Jackett
        /// </summary>
        private void ValidateConfig()
        {
            logger.Info("\nNorBits - Validating Settings ... \n");

            // Check Username Setting
            if (string.IsNullOrEmpty(ConfigData.Username.Value))
            {
                throw new ExceptionWithConfigData("You must provide a username for this tracker to login !", ConfigData);
            }
            else
            {
                logger.Info("NorBits - Validated Setting -- Username (auth) => " + ConfigData.Username.Value);
            }

            // Check Password Setting
            if (string.IsNullOrEmpty(ConfigData.Password.Value))
            {
                throw new ExceptionWithConfigData("You must provide a password with your username for this tracker to login !", ConfigData);
            }
            else
            {
                logger.Info("NorBits - Validated Setting -- Password (auth) => " + ConfigData.Password.Value);
            }

            // Check Max Page Setting
            if (!string.IsNullOrEmpty(ConfigData.Pages.Value))
            {
                try
                {
                    logger.Info("NorBits - Validated Setting -- Max Pages => " + Convert.ToInt32(ConfigData.Pages.Value));
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
