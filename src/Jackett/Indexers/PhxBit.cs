using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
    /// Provider for PhxBit Private French Tracker
    /// </summary>
    public class PhxBit : BaseIndexer, IIndexer
    {
        private string LoginUrl { get { return SiteLink + "connect.php"; } }
        private string SearchUrl { get { return SiteLink + "sphinx.php"; } }
        private string TorrentCommentUrl { get { return TorrentDescriptionUrl; } }
        private string TorrentDescriptionUrl { get { return SiteLink + "torrent.php?id="; } }
        private string TorrentDownloadUrl { get { return SiteLink + "get.php?action=private&id={id}&passkey={passkey}"; } }
        private bool Latency { get { return ConfigData.Latency.Value; } }
        private bool DevMode { get { return ConfigData.DevMode.Value; } }
        private bool CacheMode { get { return ConfigData.HardDriveCache.Value; } }
        private string directory { get { return System.IO.Path.GetTempPath() + "Jackett\\" + MethodBase.GetCurrentMethod().DeclaringType.Name + "\\"; } }

        private Dictionary<string, string> emulatedBrowserHeaders = new Dictionary<string, string>();
        private CQ fDom = null;

        private ConfigurationDataPhxBit ConfigData
        {
            get { return (ConfigurationDataPhxBit)configData; }
            set { base.configData = value; }
        }

        public PhxBit(IIndexerManagerService i, IWebClient w, Logger l, IProtectionService ps)
            : base(
                name: "PhxBit",
                description: "General French Private Tracker",
                link: "https://phxbit.com/",
                caps: new TorznabCapabilities(),
                manager: i,
                client: w,
                logger: l,
                p: ps,
                downloadBase: "https://phxbit.com/get.php?action=private",
                configData: new ConfigurationDataPhxBit())
        {
            // Clean capabilities
            TorznabCaps.Categories.Clear();

            // Movies
            AddCategoryMapping(3, TorznabCatType.MoviesSD);             // DVDRIP
            AddCategoryMapping(33, TorznabCatType.MoviesSD);            // WEBRIP
            AddCategoryMapping(4, TorznabCatType.MoviesSD);             // BRRIP/BDRIP
            AddCategoryMapping(6, TorznabCatType.MoviesHD);             // HD 720P
            AddCategoryMapping(2, TorznabCatType.MoviesHD);             // HD 1080P
            AddCategoryMapping(5, TorznabCatType.MoviesBluRay);         // FULL BLURAY
            AddCategoryMapping(32, TorznabCatType.MoviesBluRay);        // FULL BLURAY 3D
            AddCategoryMapping(7, TorznabCatType.MoviesDVD);            // FULL DVD

            // Series
            AddCategoryMapping(14, TorznabCatType.TVSD);                // SD VOSTFR
            AddCategoryMapping(16, TorznabCatType.TVHD);                // HD VOSTFR
            AddCategoryMapping(13, TorznabCatType.TVSD);                // SD VF
            AddCategoryMapping(15, TorznabCatType.TVHD);                // HD VF
            AddCategoryMapping(12, TorznabCatType.TVOTHER);             // PACK
            AddCategoryMapping(26, TorznabCatType.TVOTHER);             // PACK VOSTFR
            AddCategoryMapping(24, TorznabCatType.TVOTHER);             // EMISSIONS
            AddCategoryMapping(34, TorznabCatType.TVOTHER);             // EMISSIONS
            AddCategoryMapping(29, TorznabCatType.TVOTHER);             // BDRIP VOSTFR

            // Anime
            AddCategoryMapping(1, TorznabCatType.TVAnime);              // ANIME
            AddCategoryMapping(28, TorznabCatType.TVAnime);             // MANGA ANIME

            // Documentaries
            AddCategoryMapping(17, TorznabCatType.TVDocumentary);       // DOCS

            // Music
            AddCategoryMapping(10, TorznabCatType.AudioLossless);       // FLAC
            AddCategoryMapping(9, TorznabCatType.AudioMP3);             // MP3
            AddCategoryMapping(25, TorznabCatType.AudioVideo);          // CONCERT

            // Other
            AddCategoryMapping(27, TorznabCatType.PC);                  // PC
            AddCategoryMapping(20, TorznabCatType.PCMac);               // PC
            AddCategoryMapping(19, TorznabCatType.PCGames);             // GAMES
            AddCategoryMapping(21, TorznabCatType.ConsoleXbox360);      // GAMES
            AddCategoryMapping(22, TorznabCatType.ConsoleWii);          // GAMES
            AddCategoryMapping(22, TorznabCatType.ConsolePS3);          // GAMES
            AddCategoryMapping(30, TorznabCatType.ConsolePSP);          // GAMES
            AddCategoryMapping(31, TorznabCatType.ConsoleNDS);          // GAMES
            AddCategoryMapping(8, TorznabCatType.BooksEbook);           // EBOOKS
            AddCategoryMapping(11, TorznabCatType.BooksEbook);          // EBOOKS AUDIO
            AddCategoryMapping(35, TorznabCatType.PCPhoneAndroid);      // ANDROID
        }

        /// <summary>
        /// Configure our Provider
        /// </summary>
        /// <param name="configJson">Our params in Json</param>
        /// <returns>Configuration state</returns>
        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            // Retrieve config values set by Jackett's user
            ConfigData.LoadValuesFromJson(configJson);

            // Check & Validate Config
            validateConfig();

            // Setting our data for a better emulated browser (maximum security)
            // TODO: Encoded Content not supported by Jackett at this time
            // emulatedBrowserHeaders.Add("Accept-Encoding", "gzip, deflate");

            // If we want to simulate a browser
            if (ConfigData.Browser.Value) {

                // Clean headers
                emulatedBrowserHeaders.Clear();

                // Inject headers
                emulatedBrowserHeaders.Add("Accept", ConfigData.HeaderAccept.Value);
                emulatedBrowserHeaders.Add("Accept-Language", ConfigData.HeaderAcceptLang.Value);
                emulatedBrowserHeaders.Add("DNT", Convert.ToInt32(ConfigData.HeaderDNT.Value).ToString());
                emulatedBrowserHeaders.Add("Upgrade-Insecure-Requests", Convert.ToInt32(ConfigData.HeaderUpgradeInsecure.Value).ToString());
                emulatedBrowserHeaders.Add("User-Agent", ConfigData.HeaderUserAgent.Value);
            }


            // Getting login form to retrieve CSRF token
            /*var myRequest = new Utils.Clients.WebRequest()
            {
                Url = LoginUrl
            };*/

            // Add our headers to request
            //myRequest.Headers = emulatedBrowserHeaders;

            // Building login form data
            var pairs = new Dictionary<string, string> {
                { "username", ConfigData.Username.Value },
                { "password", ConfigData.Password.Value }
            };

            // Do the login
            var request = new Utils.Clients.WebRequest(){
                PostData = pairs,
                Referer = LoginUrl,
                Type = RequestType.POST,
                Url = LoginUrl,
                Headers = emulatedBrowserHeaders
            };

            // Perform loggin
            latencyNow();
            output("\nPerform loggin.. with " + LoginUrl);
            var response = await webclient.GetString(request);

            // Test if we are logged in
            await ConfigureIfOK(response.Cookies, !response.Cookies.Contains("deleted"), () =>
            {
                // Parse error page
                CQ dom = response.Content;
                string message = dom[".error"].Text().Trim().Replace("X\n\t\t", "").Replace("\n\t\tX", "");

                // Oops, unable to login
                output("-> Login failed: \"" + message + "\".", "error");
                throw new ExceptionWithConfigData("Login failed: << " + message + " >>", configData);
            });

            output("-> Login Success");

            return IndexerConfigurationStatus.RequiresTesting;
        }

        /// <summary>
        /// Execute our search query
        /// </summary>
        /// <param name="query">Query</param>
        /// <returns>Releases</returns>
        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var torrentRowList = new List<CQ>();
            var searchTerm = query.GetQueryString();
            var searchUrl = SearchUrl;
            int nbResults = 0;
            int pageLinkCount = 0;

            // Check cache first so we don't query the server (if search term used or not in dev mode)
            if(!DevMode && !string.IsNullOrEmpty(searchTerm))
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
            WebClientStringResult results = await queryExec(request);
            fDom = results.Content;

            try
            {
                // Find torrent rows
                var firstPageRows = findTorrentRows();

                // Add them to torrents list
                torrentRowList.AddRange(firstPageRows.Select(fRow => fRow.Cq()));

                // Check if there are pagination links at bottom
                Boolean pagination = (fDom[".pager_align > a"].Length != 0);

                // If pagination available
                if (pagination) {
                    // Calculate numbers of pages available for this search query (Based on number results and number of torrents on first page)
                    pageLinkCount = ParseUtil.CoerceInt(Regex.Match(fDom[".pager_align > a:not(:last-child)"].Last().Attr("href").ToString(), @"\d+").Value) + 1;

                    // Calculate average number of results (based on torrents rows lenght on first page)
                    nbResults = firstPageRows.Count() * pageLinkCount;
                }
                else {
                    // Check if we have a minimum of one result
                    if (firstPageRows.Length >= 1)
                    {
                        // Retrieve total count on our alone page
                        nbResults = firstPageRows.Count();
                        pageLinkCount = 1;
                    }
                    else
                    {
                        output("\nNo result found for your query, please try another search term ...\n", "info");
                        // No result found for this query
                        return releases;
                    }
                }
                output("\nFound " + nbResults + " result(s) (+/- " + firstPageRows.Length + ") in " + pageLinkCount + " page(s) for this query !");
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
                        var pageRequest = buildQuery(searchTerm, query, searchUrl, (i - 1));

                        // Getting results & Store content
                        WebClientStringResult pageResults = await queryExec(pageRequest);

                        // Assign response
                        fDom = pageResults.Content;

                        // Process page results
                        var additionalPageRows = findTorrentRows();

                        // Add them to torrents list
                        torrentRowList.AddRange(additionalPageRows.Select(fRow => fRow.Cq()));
                    }
                }
                else
                {
                    // No search term, maybe testing... so registring passkey for future uses
                    string infosData = firstPageRows.First().Find("td:eq(2) > a").Attr("href");
                    IList<string> infosList = infosData.Split('&').Select(s => s.Trim()).Where(s => s != String.Empty).ToList();
                    IList<string> infosTracker = infosList.Select(s => s.Split(new[] { '=' }, 2)[1].Trim()).ToList();

                    output("\nStoring Passkey for future uses... \"" + infosTracker[2] + "\"");
                    ConfigData.PassKey.Value = infosTracker[2];

                }

                // Loop on results
                foreach (CQ tRow in torrentRowList)
                {
                    output("\n=>> Torrent #" + (releases.Count + 1));

                    // ID
                    string row = tRow.Html().ToString();
                    int id = ParseUtil.CoerceInt(Regex.Match(tRow.Find("td:eq(1) > a").Attr("href").ToString(), @"\d+").Value);
                    output("ID: " + id);

                    // Release Name
                    string name = tRow.Find("td:eq(1) > a").Attr("title").ToString();
                    output("Release: " + name);

                    // Category
                    string infosDataCategory = firstPageRows.First().Find("td:eq(0) > a").Attr("href");
                    IList<string> infosListCategory = infosDataCategory.Split('&').Select(s => s.Trim()).Where(s => s != String.Empty).ToList();
                    IList<string> infosCategory = infosListCategory.Select(s => s.Split(new[] { '=' }, 2)[0].Trim()).ToList();
                    string categoryID = infosCategory.Last().TrimStart('c');
                    output("Category: " + MapTrackerCatToNewznab(categoryID) + " (" + categoryID + ")");

                    // Seeders
                    int seeders = ParseUtil.CoerceInt(Regex.Match(tRow.Find("td:eq(6)").Text(), @"\d+").Value);
                    output("Seeders: " + seeders);

                    // Leechers
                    int leechers = ParseUtil.CoerceInt(Regex.Match(tRow.Find("td:eq(7)").Text(), @"\d+").Value);
                    output("Leechers: " + leechers);

                    // Completed
                    int completed = ParseUtil.CoerceInt(Regex.Match(tRow.Find("td:eq(5)").Text(), @"\d+").Value);
                    output("Completed: " + completed);

                    // Size
                    string sizeStr = tRow.Find("td:eq(4)").Text().Trim().Replace("Go", "gb").Replace("Mo", "mb").Replace("Ko", "kb");
                    long size = ReleaseInfo.GetBytes(sizeStr);
                    output("Size: " + sizeStr + " (" + size + " bytes)");

                    // Health
                    int percent = ParseUtil.CoerceInt(Regex.Match(tRow.Find("td:eq(8) > img").Attr("alt").ToString(), @"\d+").Value);
                    output("Health: " + percent + "%");

                    // Publish DateToString
                    //var date = agoToDate(null);
                    int timestamp = ParseUtil.CoerceInt(Regex.Match(tRow.Find("td:eq(1)").Attr("data-added").ToString(), @"\d+").Value);
                    DateTime date = unixTimeStampToDateTime(timestamp);
                    output("Released on: " + date.ToLocalTime() + " (TS >> " + timestamp + ")");

                    // Torrent Details URL
                    Uri detailsLink = new Uri(TorrentDescriptionUrl + id);
                    output("Details: " + detailsLink.AbsoluteUri);

                    // Torrent Comments URL
                    Uri commentsLink = new Uri(TorrentCommentUrl + id);
                    output("Comments Link: " + commentsLink.AbsoluteUri);

                    // Torrent Download URL
                    Uri downloadLink = new Uri(TorrentDownloadUrl.Replace("{id}", id.ToString()).Replace("{passkey}", ConfigData.PassKey.Value));
                    output("Download Link: " + downloadLink.AbsoluteUri);

                    // Building release infos
                    var release = new ReleaseInfo();
                    release.Category = MapTrackerCatToNewznab(categoryID.ToString());
                    release.Title = name;
                    release.Seeders = seeders;
                    release.Peers = seeders + leechers;
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 345600;
                    release.PublishDate = date;
                    release.Size = size;
                    release.Guid = detailsLink;
                    release.Comments = commentsLink;
                    release.Link = downloadLink;
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
        private string buildQuery(string term, TorznabQuery query, string url, int page = 0)
        {
            var parameters = new NameValueCollection();
            List<string> categoriesList = MapTorznabCapsToTrackers(query);

            // If search term provided
            if (!string.IsNullOrWhiteSpace(term))
            {
                // Add search term
                parameters.Add("q", term);
            }
            else
            {
                parameters.Add("q", string.Empty);
                // Showing all torrents (just for output function)
                term = "all";
            }

            // Default parameters
            parameters.Add("exact", "0");
            parameters.Add("sort", "normal");
            parameters.Add("order", "desc");

            // Check if we are processing a new page
            if (page > 0)
            {
                // Adding page number to query
                parameters.Add("page", page.ToString());
            }

            // Loop on Categories needed
            foreach (string category in categoriesList)
            {
                // Add categories
                parameters.Add("c" + category, "1");
            }

            // Building our query 
            url += "?" + parameters.GetQueryString();

            output("\nBuilded query for \"" + term + "\"... " + url);

            // Return our search url
            return url;
        }

        /// <summary>
        /// Switch Method for Querying
        /// </summary>
        /// <param name="request">URL created by Query Builder</param>
        /// <returns>Results from query</returns>
        private async Task<WebClientStringResult> queryExec(string request)
        {
            WebClientStringResult results = null;

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
        private async Task<WebClientStringResult> queryCache(string request)
        {
            WebClientStringResult results = null;

            // Create Directory if not exist
            System.IO.Directory.CreateDirectory(directory);

            // Clean Storage Provider Directory from outdated cached queries
            cleanCacheStorage();

            // Create fingerprint for request
            string file = directory + request.GetHashCode() + ".json";

            // Checking modes states
            if (System.IO.File.Exists(file))
            {
                // File exist... loading it right now !
                output("Loading results from hard drive cache ..." + request.GetHashCode() + ".json");
                results = JsonConvert.DeserializeObject<WebClientStringResult>(System.IO.File.ReadAllText(file));
            }
            else
            {
                // No cached file found, querying tracker directly
                results = await queryTracker(request);

                // Cached file didn't exist for our query, writing it right now !
                output("Writing results to hard drive cache ..." + request.GetHashCode() + ".json");
                System.IO.File.WriteAllText(file, JsonConvert.SerializeObject(results));
            }
            return results;
        }

        /// <summary>
        /// Get Torrents Page from Tracker by Query Provided
        /// </summary>
        /// <param name="request">URL created by Query Builder</param>
        /// <returns>Results from query</returns>
        private async Task<WebClientStringResult> queryTracker(string request)
        {
            WebClientStringResult results = null;

            // Cache mode not enabled or cached file didn't exist for our query
            output("\nQuerying tracker for results....");

            // Request our first page
            latencyNow();
            results = await RequestStringWithCookiesAndRetry(request, null, null, emulatedBrowserHeaders);

            // Return results from tracker
            return results;
        }

        /// <summary>
        /// Clean Hard Drive Cache Storage
        /// </summary>
        /// <param name="force">Force Provider Folder deletion</param>
        private void cleanCacheStorage(Boolean force = false)
        {
            // Check cleaning method
            if(force)
            {
                // Deleting Provider Storage folder and all files recursively
                output("\nDeleting Provider Storage folder and all files recursively ...");
                
                // Check if directory exist
                if(System.IO.Directory.Exists(directory))
                {
                    // Delete storage directory of provider
                    System.IO.Directory.Delete(directory, true);
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
                int i = 0;
                // Check if there is file older than ... and delete them
                output("\nCleaning Provider Storage folder... in progress.");
                System.IO.Directory.GetFiles(directory)
                .Select(f => new System.IO.FileInfo(f))
                .Where(f => f.LastAccessTime < DateTime.Now.AddMilliseconds(-Convert.ToInt32(ConfigData.HardDriveCacheKeepTime.Value)))
                .ToList()
                .ForEach(f => {
                    output("Deleting cached file << " + f.Name + " >> ... done.");
                    f.Delete();
                    i++;
                    });

                // Inform on what was cleaned during process
                if(i > 0) {
                    output("-> Deleted " + i + " cached files during cleaning.");
                }
                else {
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
            if(Latency)
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
            return fDom["#torrent_list > tbody > tr"].Not(".head_torrent").Filter("#torrent_");

            // Dispatch Torrent Row and Torrent Infos
        }

        /// <summary>
        /// Convert Unix TimeStamp to DateTime
        /// </summary>
        /// <param name="unixTimeStamp"></param>
        /// <returns>A DateTime</returns>
        private DateTime unixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        /// <summary>
        /// Output message for logging or developpment (console)
        /// </summary>
        /// <param name="message">Message to output</param>
        /// <param name="level">Level for Logger</param>
        private void output(string message, string level = "debug")
        {
            // Check if we are in dev mode
            if(DevMode)
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
            if (!string.IsNullOrEmpty(ConfigData.Pages.Value))
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
            }

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