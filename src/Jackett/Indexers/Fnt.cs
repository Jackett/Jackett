using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
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
    /// Provider for Fnt Private French Tracker
    /// </summary>
    public class Fnt : BaseIndexer, IIndexer
    {
        private string LoginUrl { get { return SiteLink + "Login/"; } }
        private string LoginCheckUrl { get { return SiteLink + "account-login.php"; } }
        private string SearchUrl { get { return SiteLink + "torrents-recherche.php"; } }
        private string TorrentCommentUrl { get { return SiteLink + "FnT/comm/torrent/"; } }
        private string TorrentDescriptionUrl { get { return SiteLink + "FnT/fiche_film/"; } }
        private string TorrentDownloadUrl { get { return SiteLink + "download.php?id={id}&dl=oui"; } }
        private bool Latency { get { return ConfigData.Latency.Value; } }
        private bool DevMode { get { return ConfigData.DevMode.Value; } }
        private bool CacheMode { get { return ConfigData.HardDriveCache.Value; } }
        private string directory { get { return System.IO.Path.GetTempPath() + "Jackett\\" + MethodBase.GetCurrentMethod().DeclaringType.Name + "\\"; } }

        private Dictionary<string, string> emulatedBrowserHeaders = new Dictionary<string, string>();
        private CQ fDom = null;

        private ConfigurationDataFnt ConfigData
        {
            get { return (ConfigurationDataFnt)configData; }
            set { base.configData = value; }
        }

        public Fnt(IIndexerManagerService i, IWebClient w, Logger l, IProtectionService ps)
            : base(
                name: "Fnt",
                description: "Your French Movies & Series Provider",
                link: "http://fnt.nu/",
                caps: new TorznabCapabilities(),
                manager: i,
                client: w,
                logger: l,
                p: ps,
                downloadBase: "http://fnt.nu/download.php?id={id}&dl=oui",
                configData: new ConfigurationDataFnt())
        {
            // Clean capabilities
            TorznabCaps.Categories.Clear();

            // Movies
            AddCategoryMapping("101", TorznabCatType.MoviesSD);         // BDRIP
            AddCategoryMapping("100", TorznabCatType.MoviesSD);         // DVDRIP
            AddCategoryMapping("104", TorznabCatType.MoviesDVD);        // DVD FULL
            AddCategoryMapping("103", TorznabCatType.MoviesDVD);        // DVD NTSC
            AddCategoryMapping("102", TorznabCatType.MoviesDVD);        // DVD PAL
            AddCategoryMapping("106", TorznabCatType.MoviesHD);         // M-HD 1080P
            AddCategoryMapping("105", TorznabCatType.MoviesHD);         // M-HD 720P
            AddCategoryMapping("108", TorznabCatType.MoviesHD);         // 1080P
            AddCategoryMapping("107", TorznabCatType.MoviesHD);         // 720P
            AddCategoryMapping("130", TorznabCatType.MoviesBluRay);     // BLURAY
            AddCategoryMapping("127", TorznabCatType.MoviesSD);         // VOSTFR

            // Series
            AddCategoryMapping("121", TorznabCatType.TVSD);             // DVDRIP
            AddCategoryMapping("126", TorznabCatType.TVSD);             // BDRIP
            AddCategoryMapping("146", TorznabCatType.TVHD);             // DVD R
            AddCategoryMapping("120", TorznabCatType.TVSD);             // HDTV
            AddCategoryMapping("119", TorznabCatType.TVHD);             // HDTV 720P
            AddCategoryMapping("137", TorznabCatType.TVHD);             // M-HD
            AddCategoryMapping("138", TorznabCatType.TVHD);             // BLURAY
            AddCategoryMapping("135", TorznabCatType.TV);               // PACK
            AddCategoryMapping("153", TorznabCatType.TVWEBDL);          // WEB-DL
            AddCategoryMapping("150", TorznabCatType.TVWEBDL);          // WEB-DL 1080P
            AddCategoryMapping("149", TorznabCatType.TVWEBDL);          // WEB-DL 720P
            AddCategoryMapping("154", TorznabCatType.TVSD);             // WEB-RIP
            AddCategoryMapping("156", TorznabCatType.TVHD);             // WEB-RIP 1080P
            AddCategoryMapping("155", TorznabCatType.TVHD);             // WEB-RIP 720P
            AddCategoryMapping("110", TorznabCatType.TVHD);             // VOSTFR HD
            AddCategoryMapping("109", TorznabCatType.TV);               // VOSTFR PACK
            AddCategoryMapping("122", TorznabCatType.TVSD);             // VOSTFR SD

            // TV
            AddCategoryMapping("118", TorznabCatType.TV);               // HDTV
            AddCategoryMapping("129", TorznabCatType.TV);               // HDTV 720P
            AddCategoryMapping("115", TorznabCatType.TV);               // SHOWS

            // Sport
            AddCategoryMapping("133", TorznabCatType.TVSport);          // SD
            AddCategoryMapping("134", TorznabCatType.TVSport);          // HD

            // Anime
            AddCategoryMapping("151", TorznabCatType.TVAnime);          // HD
            AddCategoryMapping("140", TorznabCatType.TVAnime);          // M-HD
            AddCategoryMapping("116", TorznabCatType.TVAnime);          // ALL
            AddCategoryMapping("148", TorznabCatType.TVAnime);          // SERIES

            // Manga
            AddCategoryMapping("152", TorznabCatType.TVAnime);          // HD
            AddCategoryMapping("141", TorznabCatType.TVAnime);          // M-HD
            AddCategoryMapping("117", TorznabCatType.TVAnime);          // ALL

            // Documentaries
            AddCategoryMapping("128", TorznabCatType.TVDocumentary);    // SD
            AddCategoryMapping("131", TorznabCatType.TVDocumentary);    // HD

            // Music
            AddCategoryMapping("125", TorznabCatType.AudioVideo);       // CLIP
            AddCategoryMapping("123", TorznabCatType.AudioVideo);       // DIVX
            AddCategoryMapping("124", TorznabCatType.AudioVideo);       // DVD
            AddCategoryMapping("136", TorznabCatType.AudioLossless);    // FLAC
            AddCategoryMapping("132", TorznabCatType.Audio);            // HD

            // Other
            AddCategoryMapping("114", TorznabCatType.Other);            // SUBPACK
            AddCategoryMapping("147", TorznabCatType.AudioOther);       // AUDIO TRACKS OF MOVIES/SERIES
        }

        /// <summary>
        /// Configure our WiHD Provider
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
            var myRequest = new Utils.Clients.WebRequest()
            {
                Url = LoginUrl
            };

            // Add our headers to request
            myRequest.Headers = emulatedBrowserHeaders;

            // Building login form data
            var pairs = new Dictionary<string, string> {
                { "username", ConfigData.Username.Value },
                { "password", ConfigData.Password.Value },
                { "submit", "Se loguer" },
                { "returnto", "/" }
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
            output("\nPerform loggin.. with " + LoginCheckUrl);
            var response = await RequestLoginAndFollowRedirect(LoginCheckUrl, pairs, string.Empty, true, SiteLink + "accueil");

            // Test if we are logged in
            await ConfigureIfOK(response.Cookies, !response.Cookies.Contains("deleted"), () =>
            {
                // Parse error page
                CQ dom = response.Content;
                string message = dom[".NB-fm > p > b"].Text().Trim();

                // Oops, unable to login
                output("-> Login failed: " + message, "error");
                throw new ExceptionWithConfigData("Login failed: " + message, configData);
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
            WebClientStringResult results = await queryExec(request);
            fDom = results.Content;

            try
            {
                // Find torrent rows
                var firstPageRows = findTorrentRows();

                // Add them to torrents list
                torrentRowList.AddRange(firstPageRows.Select(fRow => fRow.Cq()));

                // Check if there are pagination links at bottom
                Boolean pagination = (fDom[".NB-fm > p > a"].Length != 0);

                // If pagination available
                if (pagination) {
                    // Retrieve total count from last page
                    nbResults = ParseUtil.CoerceInt(Regex.Match(fDom[".NB-fm > p > a:eq(-2)"].Text().Split('-')[1].Trim(), @"\d+").Value);

                    // Calculate numbers of pages available for this search query (Based on number results and number of torrents on first page)
                    pageLinkCount = (int)Math.Ceiling((double)nbResults / firstPageRows.Length);
                }
                else {
                    // Check if we have a minimum of one result
                    if (firstPageRows.Length >= 1)
                    {
                        // Retrieve total count on our alone page
                        nbResults = ParseUtil.CoerceInt(Regex.Match(fDom[".NB-fm > p > b"].Text().Split('-')[1].Trim(), @"\d+").Value);
                        pageLinkCount = 1;
                    }
                    else
                    {
                        output("\nNo result found for your query, please try another search term ...", "info");
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

                        // Build our query -- Minus 1 to page due to strange pagination number on tracker side, starting with page 0...
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

                // Loop on results
                foreach (CQ tRow in torrentRowList)
                {
                    output("\n=>> Torrent #" + (releases.Count + 1));

                    // ID
                    int id = ParseUtil.CoerceInt(Regex.Match(tRow.Find("td:eq(1) > div:last > a").Attr("href").ToString(), @"\d+").Value);
                    output("ID: " + id);

                    // Release Name
                    string name = tRow.Find("td:eq(1) > div:last > a > b").Text().ToString();
                    output("Release: " + name);

                    // Category
                    int categoryID = ParseUtil.CoerceInt(Regex.Match(tRow.Find("td:eq(0) > a").Attr("href").ToString(), @"\d+").Value);
                    //string categoryName = tRow.Find(".category > img").Attr("title").ToString();
                    output("Category: " + MapTrackerCatToNewznab(categoryID.ToString()) + " (" + categoryID + ")");

                    // Uploader & Size & Publish Date & Seeders & Leechers & Completed
                    string infosData = tRow.Find("td:eq(1) > div:last > a").Attr("mtcontent").ToString();
                    IList<string> infosList = Regex.Split(infosData, "<br />").Select(s => Regex.Replace(s, "<.*?>", String.Empty)).Where(s => s != String.Empty).ToList();
                    IList<string> infosTorrent = infosList.Select(s => s.Split(new[] { ':' }, 2)[1].Trim()).ToList();

                    // --> Uploader
                    string uploader = infosTorrent[2];
                    output("Uploader: " + uploader);

                    // --> Seeders
                    int seeders = ParseUtil.CoerceInt(infosTorrent[4]);
                    output("Seeders: " + seeders);

                    // --> Leechers ~~ Some torrents are bugged with 4294967294 leechers (so, arbitrary set to 0)
                    int leechers = infosTorrent[5] == "4294967294" ? 0 : ParseUtil.CoerceInt(infosTorrent[5]);
                    output("Leechers: " + leechers);

                    // --> Completed
                    int completed = ParseUtil.CoerceInt(infosTorrent[6]);
                    output("Completed: " + completed);

                    // Comments
                    int nbComments = 0;
                    string comments = tRow.Find("td:last > div > span:nth-child(5)").Attr("mttitle").ToString();
                    if(comments != "Ajouter un commentaire") {
                        nbComments = ParseUtil.CoerceInt(Regex.Match(comments, @"\d+").Value);
                    }
                    output("Comments: " + nbComments);

                    // --> Size
                    var size = ReleaseInfo.GetBytes(infosTorrent[0].ToLowerInvariant());
                    output("Size: " + infosTorrent[0] + " (" + size + " bytes)");

                    // --> Publish Date
                    DateTime date = DateTime.ParseExact(infosTorrent[1], "dd-MM-yyyy HH:mm:ss", CultureInfo.GetCultureInfo("fr-FR"), DateTimeStyles.AssumeLocal).ToUniversalTime();
                    output("Released on: " + date.ToLocalTime().ToString());

                    // Torrent Details URL
                    Uri detailsLink = new Uri(TorrentDescriptionUrl + id + "/");
                    output("Details: " + detailsLink.AbsoluteUri);

                    // Torrent Comments URL
                    Uri commentsLink = new Uri(TorrentCommentUrl + id + "/");
                    output("Comments Link: " + commentsLink.AbsoluteUri);

                    // Torrent Download URL
                    Uri downloadLink = new Uri(TorrentDownloadUrl.Replace("{id}", id.ToString()));
                    output("Download Link: " + downloadLink.AbsoluteUri);

                    // Building release infos
                    var release = new ReleaseInfo();
                    release.Category = MapTrackerCatToNewznab(categoryID.ToString());
                    release.Title = name;
                    release.Seeders = seeders;
                    release.Peers = seeders + leechers;
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;
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

            // Building our tracker query
            parameters.Add("afficher", "1");

            // If search term provider
            if (!string.IsNullOrWhiteSpace(term))
            {
                // Add search term
                parameters.Add("recherche", term);
            }
            else
            {
                // Showing all torrents (just for output function)
                term = "all";
            }

            // Building our tracker query
            parameters.Add("visible", (ConfigData.Dead.Value == true) ? "0" : "1");
            parameters.Add("freeleech", (ConfigData.Freeleech.Value == true) ? "2" : "0");
            parameters.Add("nuke", (ConfigData.Nuke.Value == true) ? "0" : "1");
            parameters.Add("3D", (ConfigData.ThreeD.Value == true) ? "2" : "0");
            parameters.Add("langue", (ConfigData.Language.Value == true) ? "2" : "0");

            // Loop on Categories needed
            foreach (string category in categoriesList)
            {
                    parameters.Add("c" + category, Convert.ToString(1));
            }

            // Check if we are processing a new page
            if (page > 0)
            {
                // Adding page number to query
                parameters.Add("page", page.ToString());
            }

            // Building our query -- Cannot use GetQueryString due to UrlEncode (generating wrong subcat[] param)
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
            return fDom[".ligntorrent"];
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