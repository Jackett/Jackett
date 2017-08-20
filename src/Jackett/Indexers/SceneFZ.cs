using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using CsQuery;
using Jackett.Models;
using Jackett.Models.IndexerConfig.Bespoke;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Indexers
{
    /// <summary>
    /// Provider for SceneFZ
    /// </summary>
    public class SceneFZ : BaseCachingWebIndexer
    {
        private string LoginUrl { get { return SiteLink + "login.php"; } }
        private string LoginCheckUrl { get { return SiteLink + "takelogin.php"; } }
        private string SearchUrl { get { return SiteLink + "torrents.php"; } }
        private bool Latency { get { return ConfigData.Latency.Value; } }
        private bool DevMode { get { return ConfigData.DevMode.Value; } }
        private bool CacheMode { get { return ConfigData.HardDriveCache.Value; } }
        private static string Directory => Path.Combine(Path.GetTempPath(), Assembly.GetExecutingAssembly().GetName().Name.ToLower(), MethodBase.GetCurrentMethod().DeclaringType?.Name.ToLower());

        private Dictionary<string, string> emulatedBrowserHeaders = new Dictionary<string, string>();
        private CQ fDom = null;

        private ConfigurationDataSceneFZ ConfigData
        {
            get { return (ConfigurationDataSceneFZ)configData; }
            set { base.configData = value; }
        }

        public SceneFZ(IIndexerConfigurationService configService, IWebClient w, Logger l, IProtectionService ps)
            : base(
                name: "SceneFZ",
                description: "Torrent tracker. Tracking over 50.000 torrent files.",
                link: "https://scenefz.me/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                configService: configService,
                client: w,
                logger: l,
                p: ps,
                configData: new ConfigurationDataSceneFZ())
        {
            Encoding = Encoding.UTF8;
            Language = "ro-ro";
            Type = "private";

            // Clean capabilities
            TorznabCaps.Categories.Clear();

            AddCategoryMapping(1, TorznabCatType.TVAnime);
            AddCategoryMapping(49, TorznabCatType.Other);
            AddCategoryMapping(43, TorznabCatType.TVDocumentary);
            AddCategoryMapping(35, TorznabCatType.Console);
            AddCategoryMapping(30, TorznabCatType.PC);
            AddCategoryMapping(13, TorznabCatType.TVHD);
            AddCategoryMapping(14, TorznabCatType.TVHD);
            AddCategoryMapping(44, TorznabCatType.Other);
            AddCategoryMapping(45, TorznabCatType.PC);
            AddCategoryMapping(15, TorznabCatType.PCPhoneOther);
            AddCategoryMapping(61, TorznabCatType.Movies3D);
            AddCategoryMapping(62, TorznabCatType.Movies3D);
            AddCategoryMapping(5, TorznabCatType.MoviesBluRay);
            AddCategoryMapping(6, TorznabCatType.MoviesBluRay);
            AddCategoryMapping(9, TorznabCatType.MoviesDVD);
            AddCategoryMapping(10, TorznabCatType.MoviesDVD);
            AddCategoryMapping(11, TorznabCatType.MoviesHD);
            AddCategoryMapping(12, TorznabCatType.MoviesHD);
            AddCategoryMapping(24, TorznabCatType.MoviesSD);
            AddCategoryMapping(25, TorznabCatType.MoviesSD);
            AddCategoryMapping(27, TorznabCatType.XXX);
            AddCategoryMapping(28, TorznabCatType.Audio);
            AddCategoryMapping(64, TorznabCatType.AudioLossless);
            AddCategoryMapping(29, TorznabCatType.AudioVideo);
            AddCategoryMapping(26, TorznabCatType.PCISO);
            AddCategoryMapping(22, TorznabCatType.TVSport);
            AddCategoryMapping(20, TorznabCatType.TVSD);
            AddCategoryMapping(21, TorznabCatType.TVSD);
        }

        /// <summary>
        /// Configure our SceneFZ Provider
        /// </summary>
        /// <param name="configJson">Our params in Json</param>
        /// <returns>Configuration state</returns>
        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            try
            {
                // Retrieve config values set by Jackett's user
                LoadValuesFromJson(configJson);

                // Check & Validate Config
                validateConfig();

                // Setting our data for a better emulated browser (maximum security)
                // TODO: Encoded Content not supported by Jackett at this time
                // emulatedBrowserHeaders.Add("Accept-Encoding", "gzip, deflate");

                // If we want to simulate a browser
                if (ConfigData.Browser.Value)
                {

                    // Clean headers
                    emulatedBrowserHeaders.Clear();

                    // Inject headers
                    emulatedBrowserHeaders.Add("Accept", ConfigData.HeaderAccept.Value);
                    emulatedBrowserHeaders.Add("Accept-Language", ConfigData.HeaderAcceptLang.Value);
                    emulatedBrowserHeaders.Add("DNT", Convert.ToInt32(ConfigData.HeaderDNT.Value).ToString());
                    emulatedBrowserHeaders.Add("Upgrade-Insecure-Requests", Convert.ToInt32(ConfigData.HeaderUpgradeInsecure.Value).ToString());
                    emulatedBrowserHeaders.Add("User-Agent", ConfigData.HeaderUserAgent.Value);
                }

                // Building login form data
                var pairs = new Dictionary<string, string> {
                { "username", ConfigData.Username.Value },
                { "password", ConfigData.Password.Value },
                { "submitme", "X" },
                { "returnto", "%2F" }
            };


                // Getting login form to retrieve PHPSESSID
                var myRequest = new WebRequest()
                {
                    Url = LoginUrl
                };

                // Add our headers to request
                myRequest.Headers = emulatedBrowserHeaders;

                // Get login page
                var loginPage = await webclient.GetString(myRequest);

                // Do the login
                var request = new Utils.Clients.WebRequest()
                {
                    Cookies = loginPage.Cookies,
                    PostData = pairs,
                    Referer = LoginUrl,
                    Type = RequestType.POST,
                    Url = LoginCheckUrl,
                    Headers = emulatedBrowserHeaders
                };

                // Perform loggin
                latencyNow();
                output("\nPerform login.. with " + LoginCheckUrl);
                // Get login page
                var response = await webclient.GetString(request);
                output("\nTesting if we are logged...");
                await ConfigureIfOK(response.Cookies, response.Cookies != null && response.Cookies.Contains("uid="), () =>
                {
                    output("-> Login Failed: Wrong username or Password");
                    throw new ExceptionWithConfigData("Wrong username or Password", configData);
                });

                output("-> Login Success");

                return IndexerConfigurationStatus.RequiresTesting;
            }
            catch (Exception e)
            {
                IsConfigured = false;
                Console.WriteLine("Exception thrown : {0}.", e.Message);
                Console.WriteLine(e.StackTrace);
                throw e;
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
            var torrentRowList = new List<CQ>();
            var searchTerm = query.GetQueryString();
            var searchUrl = SearchUrl;
            //int nbResults = 0;
            //int pageLinkCount = 0;

            // Check cache first so we don't query the server (if search term used or not in dev mode)
            if (!DevMode && !string.IsNullOrEmpty(searchTerm))
            {
                lock (cache)
                {
                    // Remove old cache items
                    CleanCache();

                    // Search in cache
                    var cachedResult = cache.Where(i => i.Query == searchTerm).FirstOrDefault();
                    if (cachedResult != null)
                        return cachedResult.Results.Select(s => (ReleaseInfo)s.Clone()).ToArray();
                }
            }

            // Build our query
            var request = buildQuery(searchTerm, query, searchUrl);

            // Getting results & Store content
            fDom = await queryExec(request);

            try
            {
                // Find number of results
                //nbResults = ParseUtil.CoerceInt(Regex.Match(fDom["div.ajaxtotaltorrentcount"].Text(), @"\d+").Value);

                // Find torrent rows
                var firstPageRows = findTorrentRows();

                // Add them to torrents list
                torrentRowList.AddRange(firstPageRows.Select(fRow => fRow.Cq()));

                // Check if there are pagination links at bottom
                /*Boolean pagination = (nbResults != 0);

                // If pagination available
                if (pagination)
                {
                    // Calculate numbers of pages available for this search query (Based on number results and number of torrents on first page)
                    pageLinkCount = (int)Math.Ceiling((double)nbResults / firstPageRows.Length);
                }
                else
                {
                    // Check if we have a minimum of one result
                    if (firstPageRows.Length >= 1)
                    {
                        // Set page count arbitrary to one
                        pageLinkCount = 1;
                    }
                    else
                    {
                        output("\nNo result found for your query, please try another search term ...\n", "info");
                        // No result found for this query
                        return releases;
                    }
                }
                output("\nFound " + nbResults + " result(s) in " + pageLinkCount + " page(s) for this query !");
                output("\nThere are " + firstPageRows.Length + " results on the first page !");

                // If we have a term used for search and pagination result superior to one
                if (!string.IsNullOrWhiteSpace(query.GetQueryString()) && pageLinkCount > 1)
                {
                    // Starting with page #2
                    for (int i = 2; i <= Math.Min(Int32.Parse(ConfigData.Pages.Value), pageLinkCount); i++)
                    {
                        output("\nProcessing page #" + i);

                        // Request our page
                        latencyNow();

                        // Build our query
                        var pageRequest = buildQuery(searchTerm, query, searchUrl, i);

                        // Getting results & Store content
                        fDom = await queryExec(pageRequest);

                        // Process page results
                        var additionalPageRows = findTorrentRows();

                        // Add them to torrents list
                        torrentRowList.AddRange(additionalPageRows.Select(fRow => fRow.Cq()));
                    }
                }*/

                // Loop on results
                foreach (CQ tRow in torrentRowList)
                {
                    output("\n=>> Torrent #" + (releases.Count + 1));

                    // Release Name
                    string name = tRow.Find("td:nth-child(2) > a > b").Text();
                    output("Release: " + name);

                    // Category
                    string categoryID = tRow.Find("td:nth-child(1) > a").Attr("href").Split('=').Last();
                    string categoryName = tRow.Find("td:nth-child(1) > a > img").Attr("alt");
                    output("Category: " + MapTrackerCatToNewznab(categoryID).First().ToString() + " (" + categoryName + ")");

                    // Uploader
                    string uploader = tRow.Find("td.tt_row > span").Text();
                    output("Uploader: " + uploader);

                    // Seeders
                    int seeders = ParseUtil.CoerceInt(Regex.Match(tRow.Find("td:nth-child(9)").Text().Trim(), @"\d+").Value);
                    output("Seeders: " + seeders);

                    // Leechers
                    int leechers = ParseUtil.CoerceInt(Regex.Match(tRow.Find("td:nth-child(10)").Text().Trim(), @"\d+").Value);
                    output("Leechers: " + leechers);

                    // Completed
                    int completed = ParseUtil.CoerceInt(Regex.Match(tRow.Find("td:nth-child(8)").Text().Trim(), @"\d+").Value);
                    output("Completed: " + completed);

                    // Comments
                    int comments = ParseUtil.CoerceInt(Regex.Match(tRow.Find("td:nth-child(5)").Text().Trim(), @"\d+").Value);
                    output("Comments: " + comments);

                    // Files
                    int files = ParseUtil.CoerceInt(Regex.Match(tRow.Find("td:nth-child(4)").Text().Trim(), @"\d+").Value);
                    output("Files: " + files);

                    // Size
                    string humanSize = tRow.Find("td:nth-child(7)").Text().Trim();
                    var size = ReleaseInfo.GetBytes(humanSize);
                    output("Size: " + humanSize + " (" + size + " bytes)");

                    // Publish Date
                    var clock = DateTime.ParseExact(tRow.Find("td:nth-child(6) > nobr").Text().Trim(), "dd-MM-yyyyHH:mm:ss", CultureInfo.InvariantCulture);
                    output("Released on: " + clock.ToShortDateString());

                    // Torrent Details URL
                    string details = tRow.Find("td:nth-child(2) > a").Attr("href").ToString().TrimStart('/');
                    Uri detailsLink = new Uri(SiteLink + details);
                    output("Details: " + detailsLink.AbsoluteUri);

                    // Torrent Comments URL
                    Uri commentsLink = new Uri(SiteLink + details + "&hit=1&tocomm=1");
                    output("Comments Link: " + commentsLink.AbsoluteUri);

                    // Torrent Download URL
                    string download = tRow.Find("td:nth-child(3) > a").Attr("href");
                    Uri downloadLink = new Uri(SiteLink + download);
                    output("Download Link: " + downloadLink.AbsoluteUri);

                    // Freeleech
                    int downloadVolumeFactor = 1;
                    if (tRow.Find("img[title^=\"FreeLeech\"]").Length >= 1)
                    {
                        downloadVolumeFactor = 0;
                        output("FreeLeech =)");
                    }

                    // Building release infos
                    var release = new ReleaseInfo()
                    {
                        Category = MapTrackerCatToNewznab(categoryID),
                        Title = name,
                        Seeders = seeders,
                        Peers = seeders + leechers,
                        Grabs = completed,
                        MinimumRatio = 1,
                        MinimumSeedTime = 345600,
                        PublishDate = clock,
                        Files = files,
                        Size = size,
                        Guid = detailsLink,
                        Comments = commentsLink,
                        Link = downloadLink,
                        UploadVolumeFactor = 1,
                        DownloadVolumeFactor = downloadVolumeFactor
                    };
                    releases.Add(release);
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
        private string buildQuery(string term, TorznabQuery query, string url, int page = 1)
        {
            var queryCollection = new NameValueCollection();

            // Check if we are processing a new page
            /*if (page > 1)
            {
                // Adding page number to query
                url += "/" + page.ToString();
            }*/

            // If search term provided
            if (!string.IsNullOrWhiteSpace(term))
            {
                // Add search term
                queryCollection.Add("search", HttpUtility.UrlEncode(term));
                queryCollection.Add("searchin", "0");
                queryCollection.Add("cat", "0");
                queryCollection.Add("incldead", "0");

                // Building our query 
                url += "?" + queryCollection.GetQueryString();
            }
            else
            {
                // Showing all torrents (just for output function)
                term = "all";
            }

            output("\nBuilded query for \"" + term + "\"... " + url);

            // Return our search url
            return url;
        }

        /// <summary>
        /// Switch Method for Querying
        /// </summary>
        /// <param name="request">URL created by Query Builder</param>
        /// <returns>Results from query</returns>
        private async Task<String> queryExec(string request)
        {
            String results = null;

            // Switch in we are in DEV mode with Hard Drive Cache or not
            if (DevMode && CacheMode)
            {
                // Check Cache before querying and load previous results if available
                results = await queryCache(request);
            }
            else
            {
                // Querying tracker directly
                results = await queryTracker(request);
            }
            return results;
        }

        /// <summary>
        /// Get Torrents Page from Cache by Query Provided
        /// </summary>
        /// <param name="request">URL created by Query Builder</param>
        /// <returns>Results from query</returns>
        private async Task<String> queryCache(string request)
        {
            String results;

            // Create Directory if not exist
            System.IO.Directory.CreateDirectory(Directory);

            // Clean Storage Provider Directory from outdated cached queries
            cleanCacheStorage();

            // File Name
            string fileName = StringUtil.HashSHA1(request) + ".json";

            // Create fingerprint for request
            string file = Path.Combine(Directory, fileName);

            // Checking modes states
            if (File.Exists(file))
            {
                // File exist... loading it right now !
                output("Loading results from hard drive cache ..." + fileName);
                try
                {
                    using (StreamReader fileReader = File.OpenText(file))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        results = (String)serializer.Deserialize(fileReader, typeof(String));
                    }
                }
                catch (Exception e)
                {
                    output("Error loading cached results ! " + e.Message, "error");
                    results = null;
                }
            }
            else
            {
                // No cached file found, querying tracker directly
                results = await queryTracker(request);

                // Cached file didn't exist for our query, writing it right now !
                output("Writing results to hard drive cache ..." + fileName);
                using (StreamWriter fileWriter = File.CreateText(file))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(fileWriter, results);
                }
                output("Writed to " + Directory + fileName);
            }
            return results;
        }

        /// <summary>
        /// Get Torrents Page from Tracker by Query Provided
        /// </summary>
        /// <param name="request">URL created by Query Builder</param>
        /// <returns>Results from query</returns>
        private async Task<String> queryTracker(string request)
        {
            WebClientStringResult results = null;

            // Cache mode not enabled or cached file didn't exist for our query
            output("\nQuerying tracker for results....");

            // Request our first page
            latencyNow();
            results = await RequestStringWithCookiesAndRetry(request, null, null, emulatedBrowserHeaders);

            // Return results from tracker
            return results.Content;
        }

        /// <summary>
        /// Clean Hard Drive Cache Storage
        /// </summary>
        /// <param name="force">Force Provider Folder deletion</param>
        private void cleanCacheStorage(bool force = false)
        {
            // Check cleaning method
            if (force)
            {
                // Deleting Provider Storage folder and all files recursively
                output("\nDeleting Provider Storage folder and all files recursively ...");

                // Check if directory exist
                if (System.IO.Directory.Exists(Directory))
                {
                    // Delete storage directory of provider
                    System.IO.Directory.Delete(Directory, true);
                    output("-> Storage folder deleted successfully.");
                }
                else
                {
                    // No directory, so nothing to do
                    output("-> No Storage folder found for this provider !");
                }
            }
            else
            {
                var i = 0;
                // Check if there is file older than ... and delete them
                output("\nCleaning Provider Storage folder... in progress.");
                System.IO.Directory.GetFiles(Directory)
                .Select(f => new FileInfo(f))
                .Where(f => f.LastAccessTime < DateTime.Now.AddMilliseconds(-Convert.ToInt32(ConfigData.HardDriveCacheKeepTime.Value)))
                .ToList()
                .ForEach(f =>
                {
                    output("Deleting cached file << " + f.Name + " >> ... done.");
                    f.Delete();
                    i++;
                });

                // Inform on what was cleaned during process
                if (i > 0)
                {
                    output("-> Deleted " + i + " cached files during cleaning.");
                }
                else
                {
                    output("-> Nothing deleted during cleaning.");
                }
            }
        }

        /// <summary>
        /// Generate a random fake latency to avoid detection on tracker side
        /// </summary>
        private void latencyNow()
        {
            // Need latency ?
            if (Latency)
            {
                // Generate a random value in our range
                var random = new Random(DateTime.Now.Millisecond);
                int waiting = random.Next(Convert.ToInt32(ConfigData.LatencyStart.Value), Convert.ToInt32(ConfigData.LatencyEnd.Value));
                output("\nLatency Faker => Sleeping for " + waiting + " ms...");

                // Sleep now...
                System.Threading.Thread.Sleep(waiting);
            }
        }

        /// <summary>
        /// Find torrent rows in search pages
        /// </summary>
        /// <returns>JQuery Object</returns>
        private CQ findTorrentRows()
        {
            // Return all occurencis of torrents found
            return fDom["#minions .browse,.sticky"];
        }

        /// <summary>
        /// Output message for logging or developpment (console)
        /// </summary>
        /// <param name="message">Message to output</param>
        /// <param name="level">Level for Logger</param>
        private void output(string message, string level = "debug")
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
                        if (Engine.Logger.IsDebugEnabled)
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
        private void validateConfig()
        {
            output("\nValidating Settings ... \n");

            // Check Username Setting
            if (string.IsNullOrEmpty(ConfigData.Username.Value))
            {
                throw new ExceptionWithConfigData("You must provide a username for this tracker to login !", ConfigData);
            }
            else
            {
                output("Validated Setting -- Username (auth) => " + ConfigData.Username.Value.ToString());
            }

            // Check Password Setting
            if (string.IsNullOrEmpty(ConfigData.Password.Value))
            {
                throw new ExceptionWithConfigData("You must provide a password with your username for this tracker to login !", ConfigData);
            }
            else
            {
                output("Validated Setting -- Password (auth) => " + ConfigData.Password.Value.ToString());
            }

            // Check Max Page Setting
            /*if (!string.IsNullOrEmpty(ConfigData.Pages.Value))
            {
                try
                {
                    output("Validated Setting -- Max Pages => " + Convert.ToInt32(ConfigData.Pages.Value));
                }
                catch (Exception)
                {
                    throw new ExceptionWithConfigData("Please enter a numeric maximum number of pages to crawl !", ConfigData);
                }
            }
            else
            {
                throw new ExceptionWithConfigData("Please enter a maximum number of pages to crawl !", ConfigData);
            }*/

            // Check Latency Setting
            if (ConfigData.Latency.Value)
            {
                output("\nValidated Setting -- Latency Simulation enabled");

                // Check Latency Start Setting
                if (!string.IsNullOrEmpty(ConfigData.LatencyStart.Value))
                {
                    try
                    {
                        output("Validated Setting -- Latency Start => " + Convert.ToInt32(ConfigData.LatencyStart.Value));
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
                        output("Validated Setting -- Latency End => " + Convert.ToInt32(ConfigData.LatencyEnd.Value));
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
                output("\nValidated Setting -- Browser Simulation enabled");

                // Check ACCEPT header Setting
                if (string.IsNullOrEmpty(ConfigData.HeaderAccept.Value))
                {
                    throw new ExceptionWithConfigData("Browser Simulation enabled, Please enter an ACCEPT header !", ConfigData);
                }
                else
                {
                    output("Validated Setting -- ACCEPT (header) => " + ConfigData.HeaderAccept.Value.ToString());
                }

                // Check ACCEPT-LANG header Setting
                if (string.IsNullOrEmpty(ConfigData.HeaderAcceptLang.Value))
                {
                    throw new ExceptionWithConfigData("Browser Simulation enabled, Please enter an ACCEPT-LANG header !", ConfigData);
                }
                else
                {
                    output("Validated Setting -- ACCEPT-LANG (header) => " + ConfigData.HeaderAcceptLang.Value.ToString());
                }

                // Check USER-AGENT header Setting
                if (string.IsNullOrEmpty(ConfigData.HeaderUserAgent.Value))
                {
                    throw new ExceptionWithConfigData("Browser Simulation enabled, Please enter an USER-AGENT header !", ConfigData);
                }
                else
                {
                    output("Validated Setting -- USER-AGENT (header) => " + ConfigData.HeaderUserAgent.Value.ToString());
                }
            }

            // Check Dev Cache Settings
            if (ConfigData.HardDriveCache.Value == true)
            {
                output("\nValidated Setting -- DEV Hard Drive Cache enabled");

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
                        output("Validated Setting -- Cache Keep Time (ms) => " + Convert.ToInt32(ConfigData.HardDriveCacheKeepTime.Value));
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
                cleanCacheStorage(true);
            }
        }
    }
}
