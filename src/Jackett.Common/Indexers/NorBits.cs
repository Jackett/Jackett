using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class NorBits : BaseCachingWebIndexer
    {
        private string LoginUrl => SiteLink + "login.php";
        private string LoginCheckUrl => SiteLink + "takelogin.php";
        private string SearchUrl => SiteLink + "browse.php";
        private string TorrentDetailsUrl => SiteLink + "details.php?id={id}";
        private string TorrentDownloadUrl => SiteLink + "download.php?id={id}&passkey={passkey}";
        private bool Latency => ConfigData.Latency.Value;
        private bool DevMode => ConfigData.DevMode.Value;
        private bool CacheMode => ConfigData.HardDriveCache.Value;
        private static string Directory => Path.Combine(Path.GetTempPath(), "Jackett", MethodBase.GetCurrentMethod().DeclaringType?.Name);

        private readonly Dictionary<string, string> _emulatedBrowserHeaders = new Dictionary<string, string>();

        private ConfigurationDataNorbits ConfigData => (ConfigurationDataNorbits)configData;

        public NorBits(IIndexerConfigurationService configService, WebClient w, Logger l, IProtectionService ps)
            : base(id: "norbits",
                   name: "NorBits",
                   description: "NorBits is a Norwegian Private site for MOVIES / TV / GENERAL",
                   link: "https://norbits.net/",
                   caps: new TorznabCapabilities
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
                   },
                   configService: configService,
                   client: w,
                   logger: l,
                   p: ps,
                   configData: new ConfigurationDataNorbits())
        {
            Encoding = Encoding.GetEncoding("iso-8859-1");
            Language = "nb-no";
            Type = "private";

            AddCategoryMapping("main_cat[]=1&sub2_cat[]=19", TorznabCatType.MoviesHD, "Filmer - HD-1080p/i");
            AddCategoryMapping("main_cat[]=1&sub2_cat[]=20", TorznabCatType.MoviesHD, "Filmer - HD-720p");
            AddCategoryMapping("main_cat[]=1&sub2_cat[]=22", TorznabCatType.MoviesSD, "Filmer - SD");
            AddCategoryMapping("main_cat[]=2&sub2_cat[]=19", TorznabCatType.TVHD, "TV - HD-1080p/i");
            AddCategoryMapping("main_cat[]=2&sub2_cat[]=20", TorznabCatType.TVHD, "TV - HD-720p");
            AddCategoryMapping("main_cat[]=2&sub2_cat[]=22", TorznabCatType.TVSD, "TV - SD");
            AddCategoryMapping("main_cat[]=3", TorznabCatType.PC, "Programmer");
            AddCategoryMapping("main_cat[]=4", TorznabCatType.Console, "Spill");
            AddCategoryMapping("main_cat[]=5&sub2_cat[]=42", TorznabCatType.AudioMP3, "Musikk - 192");
            AddCategoryMapping("main_cat[]=5&sub2_cat[]=43", TorznabCatType.AudioMP3, "Musikk - 256");
            AddCategoryMapping("main_cat[]=5&sub2_cat[]=44", TorznabCatType.AudioMP3, "Musikk - 320");
            AddCategoryMapping("main_cat[]=5&sub2_cat[]=45", TorznabCatType.AudioMP3, "Musikk - VBR");
            AddCategoryMapping("main_cat[]=5&sub2_cat[]=46", TorznabCatType.AudioLossless, "Musikk - Lossless");
            AddCategoryMapping("main_cat[]=6", TorznabCatType.Books, "Tidsskrift");
            AddCategoryMapping("main_cat[]=7", TorznabCatType.AudioAudiobook, "Lydbøker");
            AddCategoryMapping("main_cat[]=8&sub2_cat[]=19", TorznabCatType.AudioVideo, "Musikkvideoer - HD-1080p/i");
            AddCategoryMapping("main_cat[]=8&sub2_cat[]=20", TorznabCatType.AudioVideo, "Musikkvideoer - HD-720p");
            AddCategoryMapping("main_cat[]=8&sub2_cat[]=22", TorznabCatType.AudioVideo, "Musikkvideoer - SD");
            AddCategoryMapping("main_cat[]=40", TorznabCatType.AudioOther, "Podcasts");
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

            // Setting our data for a better emulated browser (maximum security)
            // TODO: Encoded Content not supported by Jackett at this time
            // emulatedBrowserHeaders.Add("Accept-Encoding", "gzip, deflate");

            // If we want to simulate a browser
            if (ConfigData.Browser.Value)
            {
                // Clean headers
                _emulatedBrowserHeaders.Clear();

                // Inject headers
                _emulatedBrowserHeaders.Add("Accept", ConfigData.HeaderAccept.Value);
                _emulatedBrowserHeaders.Add("Accept-Language", ConfigData.HeaderAcceptLang.Value);
                _emulatedBrowserHeaders.Add("DNT", Convert.ToInt32(ConfigData.HeaderDnt.Value).ToString());
                _emulatedBrowserHeaders.Add("Upgrade-Insecure-Requests", Convert.ToInt32(ConfigData.HeaderUpgradeInsecure.Value).ToString());
                _emulatedBrowserHeaders.Add("User-Agent", ConfigData.HeaderUserAgent.Value);
                _emulatedBrowserHeaders.Add("Referer", LoginUrl);
            }

            await DoLogin();

            return IndexerConfigurationStatus.RequiresTesting;
        }

        /// <summary>
        /// Perform login to racker
        /// </summary>
        /// <returns></returns>
        private async Task DoLogin()
        {
            // Build WebRequest for index
            var myIndexRequest = new WebRequest
            {
                Type = RequestType.GET,
                Url = SiteLink,
                Headers = _emulatedBrowserHeaders,
                Encoding = Encoding
            };

            // Get index page for cookies
            Output("\nGetting index page (for cookies).. with " + SiteLink);
            var indexPage = await webclient.GetResultAsync(myIndexRequest);

            // Building login form data
            var pairs = new Dictionary<string, string> {
                { "username", ConfigData.Username.Value },
                { "password", ConfigData.Password.Value }
            };

            // Build WebRequest for login
            var myRequestLogin = new WebRequest
            {
                Type = RequestType.GET,
                Url = LoginUrl,
                Headers = _emulatedBrowserHeaders,
                Cookies = indexPage.Cookies,
                Referer = SiteLink,
                Encoding = Encoding
            };

            // Get login page -- (not used, but simulation needed by tracker security's checks)
            LatencyNow();
            Output("\nGetting login page (user simulation).. with " + LoginUrl);
            await webclient.GetResultAsync(myRequestLogin);

            // Build WebRequest for submitting authentification
            var request = new WebRequest
            {
                PostData = pairs,
                Referer = LoginUrl,
                Type = RequestType.POST,
                Url = LoginCheckUrl,
                Headers = _emulatedBrowserHeaders,
                Cookies = indexPage.Cookies,
                Encoding = Encoding
            };

            // Perform loggin
            LatencyNow();
            Output("\nPerform loggin.. with " + LoginCheckUrl);
            var response = await webclient.GetResultAsync(request);

            // Test if we are logged in
            await ConfigureIfOK(response.Cookies, response.Cookies != null && response.Cookies.Contains("uid="), () =>
            {
                // Default error message
                var message = "Error during attempt !";
                // Parse redirect header
                var redirectTo = response.RedirectingTo;

                // Oops, unable to login
                Output("-> Login failed: " + message, "error");
                throw new ExceptionWithConfigData("Login failed: " + message, configData);
            });

            Output("\nCookies saved for future uses...");
            ConfigData.CookieHeader.Value = indexPage.Cookies + " " + response.Cookies + " ts_username=" + ConfigData.Username.Value;

            Output("\n-> Login Success\n");
        }

        /// <summary>
        /// Check logged-in state for provider
        /// </summary>
        /// <returns></returns>
        private async Task CheckLogin()
        {
            // Checking ...
            Output("\n-> Checking logged-in state....");
            var loggedInCheck = await RequestWithCookiesAsync(SearchUrl);
            if (!loggedInCheck.ContentString.Contains("logout.php"))
            {
                // Cookie expired, renew session on provider
                Output("-> Not logged, login now...\n");

                await DoLogin();
            }
            else
            {
                // Already logged, session active
                Output("-> Already logged, continue...\n");
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
            await CheckLogin();

            // Check cache first so we don't query the server (if search term used or not in dev mode)
            if (!DevMode && !string.IsNullOrEmpty(exactSearchTerm))
            {
                lock (cache)
                {
                    // Remove old cache items
                    CleanCache();

                    // Search in cache
                    var cachedResult = cache.FirstOrDefault(i => i.Query == exactSearchTerm);
                    if (cachedResult != null)
                        return cachedResult.Results.Select(s => (ReleaseInfo)s.Clone()).ToArray();
                }
            }

            var SearchTerms = new List<string> { exactSearchTerm };

            // duplicate search without diacritics
            var baseSearchTerm = StringUtil.RemoveDiacritics(exactSearchTerm);
            if (baseSearchTerm != exactSearchTerm)
                SearchTerms.Add(baseSearchTerm);

            foreach (var searchTerm in SearchTerms)
            {
                // Build our query
                var request = BuildQuery(searchTerm, query, searchUrl);

                // Getting results & Store content
                var response = await RequestWithCookiesAndRetryAsync(request, ConfigData.CookieHeader.Value);
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(response.ContentString);

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
                        Output("\nNo result found for your query, please try another search term ...\n", "info");
                        break;
                    }

                    Output("\nFound " + nbResults + " result(s) (+/- " + firstPageRows.Length + ") in " + pageLinkCount + " page(s) for this query !");
                    Output("\nThere are " + firstPageRows.Length + " results on the first page !");

                    // Loop on results

                    foreach (var row in firstPageRows)
                    {
                        Output("Torrent #" + (releases.Count + 1));

                        // ID
                        var id = row.QuerySelector("td:nth-of-type(2) > a:nth-of-type(1)").GetAttribute("href").Split('=').Last();
                        Output("ID: " + id);

                        // Release Name
                        var name = row.QuerySelector("td:nth-of-type(2) > a:nth-of-type(1)").GetAttribute("title");

                        // Category
                        var categoryName = row.QuerySelector("td:nth-of-type(1) > div > a:nth-of-type(1)").GetAttribute("title");
                        var mainCat = row.QuerySelector("td:nth-of-type(1) > div > a:nth-of-type(1)").GetAttribute("href").Split('?').Last();
                        var qSubCat2 = row.QuerySelector("td:nth-of-type(1) > div > a[href^=\"/browse.php?sub2_cat[]=\"]");

                        var cat = mainCat;
                        if (qSubCat2 != null)
                            cat += '&' + qSubCat2.GetAttribute("href").Split('?').Last();

                        Output("Category: " + cat + " - " + categoryName);

                        // Seeders
                        var seeders = ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(9)").TextContent);
                        Output("Seeders: " + seeders);

                        // Leechers
                        var leechers = ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(10)").TextContent);
                        Output("Leechers: " + leechers);

                        // Completed
                        var regexObj = new Regex(@"[^\d]");
                        var completed2 = row.QuerySelector("td:nth-of-type(8)").TextContent;
                        var completed = ParseUtil.CoerceLong(regexObj.Replace(completed2, ""));
                        Output("Completed: " + completed);

                        // Files
                        var qFiles = row.QuerySelector("td:nth-of-type(3) > a");
                        var files = qFiles != null ? ParseUtil.CoerceInt(Regex.Match(qFiles.TextContent, @"\d+").Value) : 1;
                        Output("Files: " + files);

                        // Size
                        var humanSize = row.QuerySelector("td:nth-of-type(7)").TextContent.ToLowerInvariant();
                        var size = ReleaseInfo.GetBytes(humanSize);
                        Output("Size: " + humanSize + " (" + size + " bytes)");

                        // --> Date
                        var dateTimeOrig = row.QuerySelector("td:nth-of-type(5)").TextContent;
                        var dateTime = Regex.Replace(dateTimeOrig, @"<[^>]+>|&nbsp;", "").Trim();
                        var date = DateTime.ParseExact(dateTime, "yyyy-MM-ddHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToLocalTime();
                        Output("Released on: " + date);

                        // Torrent Details URL
                        var details = new Uri(TorrentDetailsUrl.Replace("{id}", id.ToString()));
                        Output("Details: " + details.AbsoluteUri);

                        // Torrent Download URL
                        var passkey = row.QuerySelector("td:nth-of-type(2) > a:nth-of-type(2)").GetAttribute("href");
                        var key = Regex.Match(passkey, "(?<=passkey\\=)([a-zA-z0-9]*)");
                        var downloadLink = new Uri(TorrentDownloadUrl.Replace("{id}", id.ToString()).Replace("{passkey}", key.ToString()));
                        Output("Download Link: " + downloadLink.AbsoluteUri);

                        // Building release infos
                        var release = new ReleaseInfo
                        {
                            Category = MapTrackerCatToNewznab(cat),
                            Title = name,
                            Seeders = seeders,
                            Peers = seeders + leechers,
                            PublishDate = date,
                            Size = size,
                            Files = files,
                            Grabs = completed,
                            Guid = details,
                            Details = details,
                            Link = downloadLink,
                            MinimumRatio = 1,
                            MinimumSeedTime = 172800 // 48 hours
                        };

                        var genres = row.QuerySelector("span.genres")?.TextContent;
                        if (!string.IsNullOrEmpty(genres))
                            release.Description = genres;

                        // IMDB
                        var imdbLink = row.QuerySelector("a[href*=\"imdb.com/title/tt\"]")?.GetAttribute("href");
                        release.Imdb = ParseUtil.GetLongFromString(imdbLink);

                        if (row.QuerySelector("img[title=\"100% freeleech\"]") != null)
                            release.DownloadVolumeFactor = 0;
                        else if (row.QuerySelector("img[title=\"Halfleech\"]") != null)
                            release.DownloadVolumeFactor = 0.5;
                        else if (row.QuerySelector("img[title=\"90% Freeleech\"]") != null)
                            release.DownloadVolumeFactor = 0.1;
                        else
                            release.DownloadVolumeFactor = 1;

                        release.UploadVolumeFactor = 1;

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
        /// <param name="url">Search url for provider</param>
        /// <param name="page">Page number to request</param>
        /// <returns>URL to query for parsing and processing results</returns>
        private string BuildQuery(string term, TorznabQuery query, string url, int page = 0)
        {
            var parameters = new NameValueCollection();
            var categoriesList = MapTorznabCapsToTrackers(query);
            var searchterm = term;

            // Building our tracker query
            parameters.Add("incldead", "1");
            parameters.Add("fullsearch", ConfigData.UseFullSearch.Value ? "1" : "0");
            parameters.Add("scenerelease", "0");

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

            var CatQryStr = "";
            foreach (var cat in categoriesList)
                CatQryStr += "&" + cat;

            // Building our query
            url += "?" + searchterm + "&" + parameters.GetQueryString() + "&" + CatQryStr;

            Output("\nBuilded query for \"" + term + "\"... " + url);

            // Return our search url
            return url;
        }

        /// <summary>
        /// Switch Method for Querying
        /// </summary>
        /// <param name="request">URL created by Query Builder</param>
        /// <returns>Results from query</returns>
        private async Task<WebResult> QueryExec(string request)
        {
            WebResult results;

            // Switch in we are in DEV mode with Hard Drive Cache or not
            if (DevMode && CacheMode)
            {
                // Check Cache before querying and load previous results if available
                results = await QueryCache(request);
            }
            else
            {
                // Querying tracker directly
                results = await QueryTracker(request);
            }
            return results;
        }

        /// <summary>
        /// Get Torrents Page from Cache by Query Provided
        /// </summary>
        /// <param name="request">URL created by Query Builder</param>
        /// <returns>Results from query</returns>
        private async Task<WebResult> QueryCache(string request)
        {
            WebResult results;

            // Create Directory if not exist
            System.IO.Directory.CreateDirectory(Directory);

            // Clean Storage Provider Directory from outdated cached queries
            CleanCacheStorage();

            // Create fingerprint for request
            var file = Directory + request.GetHashCode() + ".json";

            // Checking modes states
            if (File.Exists(file))
            {
                // File exist... loading it right now !
                Output("Loading results from hard drive cache ..." + request.GetHashCode() + ".json");
                results = JsonConvert.DeserializeObject<WebResult>(File.ReadAllText(file));
            }
            else
            {
                // No cached file found, querying tracker directly
                results = await QueryTracker(request);

                // Cached file didn't exist for our query, writing it right now !
                Output("Writing results to hard drive cache ..." + request.GetHashCode() + ".json");
                File.WriteAllText(file, JsonConvert.SerializeObject(results));
            }
            return results;
        }

        /// <summary>
        /// Get Torrents Page from Tracker by Query Provided
        /// </summary>
        /// <param name="request">URL created by Query Builder</param>
        /// <returns>Results from query</returns>
        private async Task<WebResult> QueryTracker(string request)
        {
            // Cache mode not enabled or cached file didn't exist for our query
            Output("\nQuerying tracker for results....");

            // Request our first page
            LatencyNow();
            var results = await RequestWithCookiesAndRetryAsync(request, ConfigData.CookieHeader.Value, RequestType.GET, SearchUrl, null, _emulatedBrowserHeaders);

            // Return results from tracker
            return results;
        }

        /// <summary>
        /// Clean Hard Drive Cache Storage
        /// </summary>
        /// <param name="force">Force Provider Folder deletion</param>
        private void CleanCacheStorage(bool force = false)
        {
            // Check cleaning method
            if (force)
            {
                // Deleting Provider Storage folder and all files recursively
                Output("\nDeleting Provider Storage folder and all files recursively ...");

                // Check if directory exist
                if (System.IO.Directory.Exists(Directory))
                {
                    // Delete storage directory of provider
                    System.IO.Directory.Delete(Directory, true);
                    Output("-> Storage folder deleted successfully.");
                }
                else
                {
                    // No directory, so nothing to do
                    Output("-> No Storage folder found for this provider !");
                }
            }
            else
            {
                var i = 0;
                // Check if there is file older than ... and delete them
                Output("\nCleaning Provider Storage folder... in progress.");
                System.IO.Directory.GetFiles(Directory)
                .Select(f => new FileInfo(f))
                .Where(f => f.LastAccessTime < DateTime.Now.AddMilliseconds(-Convert.ToInt32(ConfigData.HardDriveCacheKeepTime.Value)))
                .ToList()
                .ForEach(f =>
                {
                    Output("Deleting cached file << " + f.Name + " >> ... done.");
                    f.Delete();
                    i++;
                });

                // Inform on what was cleaned during process
                if (i > 0)
                {
                    Output("-> Deleted " + i + " cached files during cleaning.");
                }
                else
                {
                    Output("-> Nothing deleted during cleaning.");
                }
            }
        }

        /// <summary>
        /// Generate a random fake latency to avoid detection on tracker side
        /// </summary>
        private void LatencyNow()
        {
            // Need latency ?
            if (Latency)
            {
                var random = new Random(DateTime.Now.Millisecond);
                var waiting = random.Next(Convert.ToInt32(ConfigData.LatencyStart.Value),
                    Convert.ToInt32(ConfigData.LatencyEnd.Value));
                Output("\nLatency Faker => Sleeping for " + waiting + " ms...");

                // Sleep now...
                System.Threading.Thread.Sleep(waiting);
            }
            // Generate a random value in our range
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
            Output("Torrent Requested ID: " + id);

            // Building login form data
            var pairs = new Dictionary<string, string> {
                { "torrentid", id.ToString() },
                { "_", string.Empty } // ~~ Strange, blank param...
            };

            // Add emulated XHR request
            _emulatedBrowserHeaders.Add("X-Prototype-Version", "1.6.0.3");
            _emulatedBrowserHeaders.Add("X-Requested-With", "XMLHttpRequest");

            // Get torrent file now
            Output("Getting torrent file now....");
            var response = await base.Download(link);

            // Remove our XHR request header
            _emulatedBrowserHeaders.Remove("X-Prototype-Version");
            _emulatedBrowserHeaders.Remove("X-Requested-With");

            // Return content
            return response;
        }

        /// <summary>
        /// Output message for logging or developpment (console)
        /// </summary>
        /// <param name="message">Message to output</param>
        /// <param name="level">Level for Logger</param>
        private void Output(string message, string level = "debug")
        {
            // Check if we are in dev mode
            if (DevMode)
            {
                // Output message to console
                Console.WriteLine(message);
            }
            else
            {
                // Send message to logger with level
                switch (level)
                {
                    default:
                        goto case "debug";
                    case "debug":
                        // Only if Debug Level Enabled on Jackett
                        if (logger.IsDebugEnabled)
                        {
                            logger.Debug(message);
                        }
                        break;

                    case "info":
                        logger.Info(message);
                        break;

                    case "error":
                        logger.Error(message);
                        break;
                }
            }
        }

        /// <summary>
        /// Validate Config entered by user on Jackett
        /// </summary>
        private void ValidateConfig()
        {
            Output("\nValidating Settings ... \n");

            // Check Username Setting
            if (string.IsNullOrEmpty(ConfigData.Username.Value))
            {
                throw new ExceptionWithConfigData("You must provide a username for this tracker to login !", ConfigData);
            }
            else
            {
                Output("Validated Setting -- Username (auth) => " + ConfigData.Username.Value);
            }

            // Check Password Setting
            if (string.IsNullOrEmpty(ConfigData.Password.Value))
            {
                throw new ExceptionWithConfigData("You must provide a password with your username for this tracker to login !", ConfigData);
            }
            else
            {
                Output("Validated Setting -- Password (auth) => " + ConfigData.Password.Value);
            }

            // Check Max Page Setting
            if (!string.IsNullOrEmpty(ConfigData.Pages.Value))
            {
                try
                {
                    Output("Validated Setting -- Max Pages => " + Convert.ToInt32(ConfigData.Pages.Value));
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

            // Check Latency Setting
            if (ConfigData.Latency.Value)
            {
                Output("\nValidated Setting -- Latency Simulation enabled");

                // Check Latency Start Setting
                if (!string.IsNullOrEmpty(ConfigData.LatencyStart.Value))
                {
                    try
                    {
                        Output("Validated Setting -- Latency Start => " + Convert.ToInt32(ConfigData.LatencyStart.Value));
                    }
                    catch (Exception)
                    {
                        throw new ExceptionWithConfigData("Please enter a numeric latency start in ms !", ConfigData);
                    }
                }
                else
                {
                    throw new ExceptionWithConfigData("Latency Simulation enabled, Please enter a start latency !", ConfigData);
                }

                // Check Latency End Setting
                if (!string.IsNullOrEmpty(ConfigData.LatencyEnd.Value))
                {
                    try
                    {
                        Output("Validated Setting -- Latency End => " + Convert.ToInt32(ConfigData.LatencyEnd.Value));
                    }
                    catch (Exception)
                    {
                        throw new ExceptionWithConfigData("Please enter a numeric latency end in ms !", ConfigData);
                    }
                }
                else
                {
                    throw new ExceptionWithConfigData("Latency Simulation enabled, Please enter a end latency !", ConfigData);
                }
            }

            // Check Browser Setting
            if (ConfigData.Browser.Value)
            {
                Output("\nValidated Setting -- Browser Simulation enabled");

                // Check ACCEPT header Setting
                if (string.IsNullOrEmpty(ConfigData.HeaderAccept.Value))
                {
                    throw new ExceptionWithConfigData("Browser Simulation enabled, Please enter an ACCEPT header !", ConfigData);
                }
                else
                {
                    Output("Validated Setting -- ACCEPT (header) => " + ConfigData.HeaderAccept.Value);
                }

                // Check ACCEPT-LANG header Setting
                if (string.IsNullOrEmpty(ConfigData.HeaderAcceptLang.Value))
                {
                    throw new ExceptionWithConfigData("Browser Simulation enabled, Please enter an ACCEPT-LANG header !", ConfigData);
                }
                else
                {
                    Output("Validated Setting -- ACCEPT-LANG (header) => " + ConfigData.HeaderAcceptLang.Value);
                }

                // Check USER-AGENT header Setting
                if (string.IsNullOrEmpty(ConfigData.HeaderUserAgent.Value))
                {
                    throw new ExceptionWithConfigData("Browser Simulation enabled, Please enter an USER-AGENT header !", ConfigData);
                }
                else
                {
                    Output("Validated Setting -- USER-AGENT (header) => " + ConfigData.HeaderUserAgent.Value);
                }
            }
            else
            {
                // Browser simulation must be enabled (otherwhise, this provider will not work due to tracker's security)
                throw new ExceptionWithConfigData("Browser Simulation must be enabled for this provider to work, please enable it !", ConfigData);
            }

            // Check Dev Cache Settings
            if (ConfigData.HardDriveCache.Value)
            {
                Output("\nValidated Setting -- DEV Hard Drive Cache enabled");

                // Check if Dev Mode enabled !
                if (!ConfigData.DevMode.Value)
                {
                    throw new ExceptionWithConfigData("Hard Drive is enabled but not in DEV MODE, Please enable DEV MODE !", ConfigData);
                }

                // Check Cache Keep Time Setting
                if (!string.IsNullOrEmpty(ConfigData.HardDriveCacheKeepTime.Value))
                {
                    try
                    {
                        Output("Validated Setting -- Cache Keep Time (ms) => " + Convert.ToInt32(ConfigData.HardDriveCacheKeepTime.Value));
                    }
                    catch (Exception)
                    {
                        throw new ExceptionWithConfigData("Please enter a numeric hard drive keep time in ms !", ConfigData);
                    }
                }
                else
                {
                    throw new ExceptionWithConfigData("Hard Drive Cache enabled, Please enter a maximum keep time for cache !", ConfigData);
                }
            }
            else
            {
                // Delete cache if previously existed
                CleanCacheStorage(true);
            }
        }
    }
}
