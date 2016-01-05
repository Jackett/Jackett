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
    /// Provider for French-ADN Private Tracker
    /// </summary>
    public class FrenchADN : BaseIndexer, IIndexer
    {
        private string LoginUrl { get { return SiteLink + "login.php?"; } }
        private string LoginCheckUrl { get { return SiteLink + "takelogin.php"; } }
        private string SearchUrl { get { return SiteLink + "browse.php"; } }
        private string TorrentCommentUrl { get { return SiteLink + "details.php?id={id}#comments"; } }
        private string TorrentDescriptionUrl { get { return SiteLink + "details.php?id={id}"; } }
        private string TorrentDownloadUrl { get { return SiteLink + "download.php?id={id}"; } }
        private string TorrentThanksUrl { get { return SiteLink + "takethanks.php"; } }
        private bool Latency { get { return ConfigData.Latency.Value; } }
        private bool DevMode { get { return ConfigData.DevMode.Value; } }
        private bool CacheMode { get { return ConfigData.HardDriveCache.Value; } }
        private string directory { get { return System.IO.Path.GetTempPath() + "Jackett\\" + MethodBase.GetCurrentMethod().DeclaringType.Name + "\\"; } }

        private Dictionary<string, string> emulatedBrowserHeaders = new Dictionary<string, string>();
        private CQ fDom = null;

        private ConfigurationDataFrenchADN ConfigData
        {
            get { return (ConfigurationDataFrenchADN)configData; }
            set { base.configData = value; }
        }

        public FrenchADN(IIndexerManagerService i, IWebClient w, Logger l, IProtectionService ps)
            : base(
                name: "French-ADN",
                description: "Your French Family Provider",
                link: "https://french-adn.com/",
                caps: new TorznabCapabilities(),
                manager: i,
                client: w,
                logger: l,
                p: ps,
                downloadBase: "https://french-adn.com/download.php?id=",
                configData: new ConfigurationDataFrenchADN())
        {
            // Clean capabilities
            TorznabCaps.Categories.Clear();

            // Movies
            AddCategoryMapping("15", TorznabCatType.Movies);            // ALL
            AddCategoryMapping("108", TorznabCatType.MoviesSD);         // TS CAM
            AddCategoryMapping("25", TorznabCatType.MoviesSD);          // BDRIP
            AddCategoryMapping("56", TorznabCatType.MoviesSD);          // BRRIP
            AddCategoryMapping("16", TorznabCatType.MoviesSD);          // DVDRIP
            AddCategoryMapping("49", TorznabCatType.MoviesDVD);         // TVRIP
            AddCategoryMapping("102", TorznabCatType.MoviesWEBDL);      // WEBRIP
            AddCategoryMapping("105", TorznabCatType.MoviesHD);         // 1080P
            AddCategoryMapping("104", TorznabCatType.MoviesHD);         // 720P
            AddCategoryMapping("17", TorznabCatType.MoviesDVD);         // DVD R
            AddCategoryMapping("21", TorznabCatType.MoviesDVD);         // DVD R5
            AddCategoryMapping("112", TorznabCatType.MoviesDVD);        // DVD REMUX
            AddCategoryMapping("107", TorznabCatType.Movies3D);         // 3D
            AddCategoryMapping("113", TorznabCatType.MoviesBluRay);     // BLURAY
            AddCategoryMapping("118", TorznabCatType.MoviesHD);         // MHD

            // Series
            AddCategoryMapping("41", TorznabCatType.TV);                // ALL
            AddCategoryMapping("43", TorznabCatType.TV);                // VF
            AddCategoryMapping("44", TorznabCatType.TV);                // VOSTFR
            AddCategoryMapping("42", TorznabCatType.TV);                // PACK

            // TV
            AddCategoryMapping("110", TorznabCatType.TV);               // SHOWS

            // Anime
            AddCategoryMapping("109", TorznabCatType.TVAnime);          // ANIME

            // Manga
            AddCategoryMapping("119", TorznabCatType.TVAnime);          // MANGA

            // Documentaries
            AddCategoryMapping("114", TorznabCatType.TVDocumentary);    // DOCUMENTARY

            // Music
            AddCategoryMapping("22", TorznabCatType.Audio);             // ALL
            AddCategoryMapping("24", TorznabCatType.AudioLossless);     // FLAC
            AddCategoryMapping("23", TorznabCatType.AudioMP3);          // MP3

            // Games
            AddCategoryMapping("33", TorznabCatType.PCGames);           // ALL
            AddCategoryMapping("45", TorznabCatType.PCGames);           // PC GAMES
            AddCategoryMapping("93", TorznabCatType.Console3DS);        // 3DS
            AddCategoryMapping("94", TorznabCatType.Console);           // PS2
            AddCategoryMapping("93", TorznabCatType.ConsolePS3);        // PS3
            AddCategoryMapping("95", TorznabCatType.ConsolePSP);        // PSP
            AddCategoryMapping("35", TorznabCatType.ConsolePS3);        // WII

            // Applications
            AddCategoryMapping("11", TorznabCatType.PC);                // ALL
            AddCategoryMapping("12", TorznabCatType.PC);                // APPS WINDOWS
            AddCategoryMapping("97", TorznabCatType.PCMac);             // APPS MAC
            AddCategoryMapping("98", TorznabCatType.PC);                // APPS LINUX

            // Books
            AddCategoryMapping("115", TorznabCatType.BooksEbook);       // EBOOK
            AddCategoryMapping("114", TorznabCatType.BooksComics);      // COMICS

            // Other
            AddCategoryMapping("103", TorznabCatType.Other);            // STAFF
        }

        /// <summary>
        /// Configure our FADN Provider
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

            // Build WebRequest for index
            var myIndexRequest = new WebRequest()
            {
                Type = RequestType.GET,
                Url = SiteLink,
                Headers = emulatedBrowserHeaders
            };

            // Get index page for cookies
            output("\nGetting index page (for cookies).. with " + SiteLink);
            var indexPage = await webclient.GetString(myIndexRequest);

             // Building login form data
             var pairs = new Dictionary<string, string> {
                { "username", ConfigData.Username.Value },
                { "password", ConfigData.Password.Value }
            };

            // Build WebRequest for login
            var myRequestLogin = new WebRequest()
            {
                Type = RequestType.GET,
                Url = LoginUrl,
                Headers = emulatedBrowserHeaders,
                Cookies = indexPage.Cookies,
                Referer = SiteLink
            };

            // Get login page -- (not used, but simulation needed by tracker security's checks)
            latencyNow();
            output("\nGetting login page (user simulation).. with " + LoginUrl);
            var loginPage = await webclient.GetString(myRequestLogin);

            // Build WebRequest for submitting authentification
            var request = new WebRequest()
            {
                PostData = pairs,
                Referer = LoginUrl,
                Type = RequestType.POST,
                Url = LoginCheckUrl,
                Headers = emulatedBrowserHeaders,
                Cookies = indexPage.Cookies,
   
            };

            // Perform loggin
            latencyNow();
            output("\nPerform loggin.. with " + LoginCheckUrl);
            var response = await webclient.GetString(request);

            // Test if we are logged in
            await ConfigureIfOK(response.Cookies, !string.IsNullOrEmpty(response.Cookies) && !response.IsRedirect, () =>
            {
                // Default error message
                string message = "Error during attempt !";

                // Parse redirect header
                string redirectTo = response.RedirectingTo;

                // Analyzer error code
                if(redirectTo.Contains("login.php?error=4"))
                {
                    // Set message
                    message = "Wrong username or password !";
                }

                // Oops, unable to login
                output("-> Login failed: " + message, "error");
                throw new ExceptionWithConfigData("Login failed: " + message, configData);
            });

            output("\nCookies saved for future uses...");
            ConfigData.CookieHeader.Value = indexPage.Cookies + " " + response.Cookies + " ts_username=" + ConfigData.Username.Value;

            output("\n-> Login Success\n");

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
                Boolean pagination = (fDom["#quicknavpage_menu"].Length != 0);

                // If pagination available
                if (pagination)
                {
                    // Retrieve available pages (3 pages shown max)
                    pageLinkCount = fDom["#navcontainer_f:first > ul"].Find("a").Not(".smalltext").Not("#quicknavpage").Length;

                    // Last button ? (So more than 3 page are available)
                    Boolean more = (fDom["#navcontainer_f:first > ul"].Find("a.smalltext").Length > 1); ;
                
                    // More page than 3 pages ?
                    if (more)
                    {
                        // Get total page count from last link
                        pageLinkCount = ParseUtil.CoerceInt(Regex.Match(fDom["#navcontainer_f:first > ul"].Find("a:eq(4)").Attr("href").ToString(), @"\d+").Value);
                    }

                    // Calculate average number of results (based on torrents rows lenght on first page)
                    nbResults = firstPageRows.Count() * pageLinkCount;
                }
                else {
                    nbResults = 1;
                    pageLinkCount = 1;

                    // Check if we have a minimum of one result
                    if (firstPageRows.Length > 1)
                    {
                        // Retrieve total count on our alone page
                        nbResults = firstPageRows.Count();
                    }
                    else
                    {
                        // Check if no result
                        if(torrentRowList.First().Find("td").Length == 1)
                        {
                            // No results found
                            output("\nNo result found for your query, please try another search term ...\n", "info");

                            // No result found for this query
                            return releases;
                        }
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

                        // Build our query -- Minus 1 to page due to strange pagination number on tracker side, starting with page 0...
                        var pageRequest = buildQuery(searchTerm, query, searchUrl, i);

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
                    int id = ParseUtil.CoerceInt(Regex.Match(tRow.Find("td:eq(1) > div:first > a").Attr("name").ToString(), @"\d+").Value);
                    output("ID: " + id);

                    // Check if torrent is not nuked by tracker or rulez, can't download it
                    if (tRow.Find("td:eq(2) > a").Length == 0)
                    {
                        // Next item
                        output("Torrent is nuked, we can't download it, going to next torrent...");
                        continue;
                    }

                    // Release Name
                    string name = tRow.Find("td:eq(2) > a").Attr("title").ToString().Substring(24).Trim();
                    output("Release: " + name);

                    // Category
                    int categoryID = ParseUtil.CoerceInt(Regex.Match(tRow.Find("td:eq(0) > a").Attr("href").ToString(), @"\d+").Value);
                    string categoryName = tRow.Find("td:eq(0) > a > img").Attr("title").Split(new[] { ':' }, 2)[1].Trim().ToString();
                    output("Category: " + MapTrackerCatToNewznab(categoryID.ToString()) + " (" + categoryID + " - " + categoryName + ")");

                    // Seeders
                    int seeders = ParseUtil.CoerceInt(Regex.Match(tRow.Find("td:eq(5) > div > font").Select(s => Regex.Replace(s.ToString(), "<.*?>", String.Empty)).ToString(), @"\d+").Value);
                    output("Seeders: " + seeders);

                    // Leechers
                    int leechers = ParseUtil.CoerceInt(Regex.Match(tRow.Find("td:eq(6) > div > font").Text().ToString(), @"\d+").Value);
                    output("Leechers: " + leechers);

                    // Completed
                    int completed = ParseUtil.CoerceInt(Regex.Match(tRow.Find("td:eq(4)").Text().ToString(), @"\d+").Value);
                    output("Completed: " + completed);

                    // Files
                    int files = 1;
                    if (tRow.Find("td:eq(3) > a").Length == 1)
                    {
                        files = ParseUtil.CoerceInt(Regex.Match(tRow.Find("td:eq(3) > a").Text().ToString(), @"\d+").Value);
                    }
                    output("Files: " + files);

                    // Health
                    int percent = ParseUtil.CoerceInt(Regex.Match(tRow.Find("td:eq(7) > img").Attr("src").ToString(), @"\d+").Value) * 10;
                    output("Health: " + percent + "%");

                    // Size
                    string humanSize = tRow.Find("td:eq(8)").Text().ToString().ToLowerInvariant();
                    long size = ReleaseInfo.GetBytes(humanSize);
                    output("Size: " + humanSize + " (" + size + " bytes)");

                    // Date & IMDB & Genre
                    string infosData = tRow.Find("td:eq(1) > div:last").Text().ToString();
                    IList<string> infosList = Regex.Split(infosData, "\\|").ToList();
                    IList<string> infosTorrent = infosList.Select(s => s.Split(new[] { ':' }, 2)[1].Trim()).ToList();

                    // --> Date
                    DateTime date = formatDate(infosTorrent.First());
                    output("Released on: " + date.ToLocalTime().ToString());

                    // --> Genre
                    string genre = infosTorrent.Last();
                    output("Genre: " + genre);

                    // Torrent Details URL
                    Uri detailsLink = new Uri(TorrentDescriptionUrl.Replace("{id}", id.ToString()));
                    output("Details: " + detailsLink.AbsoluteUri);

                    // Torrent Comments URL
                    Uri commentsLink = new Uri(TorrentCommentUrl.Replace("{id}", id.ToString()));
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
            parameters.Add("do", "search");

            // If search term provided
            if (!string.IsNullOrWhiteSpace(term))
            {
                // Add search term ~~ Strange search engine, need to replace space with dot for results !
                parameters.Add("keywords", term.Replace(' ', '.'));
            }
            else
            {
                // Showing all torrents (just for output function)
                parameters.Add("keywords", "");
                term = "all";
            }

            // Adding requested categories
            if(categoriesList.Count > 0)
            {
                // Add categories
                parameters.Add("category", String.Join(",", categoriesList));
            }
            else
            {
                // Add empty category parameter
                parameters.Add("category", "");
            }

            // Building our tracker query
            parameters.Add("search_type", "t_name");

            // Check if we are processing a new page
            if (page > 1)
            {
                // Adding page number to query
                parameters.Add("page", page.ToString());
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
            results = await RequestStringWithCookiesAndRetry(request, ConfigData.CookieHeader.Value, SearchUrl, emulatedBrowserHeaders);

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
            if (force)
            {
                // Deleting Provider Storage folder and all files recursively
                output("\nDeleting Provider Storage folder and all files recursively ...");

                // Check if directory exist
                if (System.IO.Directory.Exists(directory))
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
                if (i > 0)
                {
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
            return fDom["#showcontents > table > tbody > tr:not(:first)"];
        }

        /// <summary>
        /// Format Date to DateTime
        /// </summary>
        /// <param name="clock"></param>
        /// <returns>A DateTime</returns>
        private DateTime formatDate(string clock)
        {
            DateTime date;

            // Switch from date format
            if(clock.Contains("Aujourd'hui") || clock.Contains("Hier"))
            {
                // Get hours & minutes
                IList<int> infosClock = clock.Split(':').Select(s => ParseUtil.CoerceInt(Regex.Match(s, @"\d+").Value)).ToList();

                // Ago date with today
                date = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, Convert.ToInt32(infosClock[0]), Convert.ToInt32(infosClock[1]), DateTime.Now.Second);

                // Set yesterday if necessary
                if (clock.Contains("Hier"))
                {
                    // Remove one day from date
                    date.AddDays(-1);
                }
            }
            else
            {
                // Parse Date if full
                date = DateTime.ParseExact(clock, "MM-dd-yyyy HH:mm", CultureInfo.GetCultureInfo("fr-FR"), DateTimeStyles.AssumeLocal);
            }

            return date.ToUniversalTime();
        }

        /// <summary>
        /// Download torrent file from tracker
        /// </summary>
        /// <param name="link">URL string</param>
        /// <returns></returns>
        public async override Task<byte[]> Download(Uri link)
        {
            var dl = link.AbsoluteUri;
            // This tracker need to thanks Uploader before getting torrent file...
            output("\nThis tracker needs you to thank uploader before downloading torrent!");

            // Retrieving ID from link provided
            int id = ParseUtil.CoerceInt(Regex.Match(link.AbsoluteUri, @"\d+").Value);
            output("Torrent Requested ID: " + id);

            // Building login form data
            var pairs = new Dictionary<string, string> {
                { "torrentid", id.ToString() },
                { "_", string.Empty } // ~~ Strange, blank param...
            };

            // Add emulated XHR request
            emulatedBrowserHeaders.Add("X-Prototype-Version", "1.6.0.3");
            emulatedBrowserHeaders.Add("X-Requested-With", "XMLHttpRequest");

            // Build WebRequest for thanks
            var myRequestThanks = new WebRequest()
            {
                Type = RequestType.POST,
                PostData = pairs,
                Url = TorrentThanksUrl,
                Headers = emulatedBrowserHeaders,
                Cookies = ConfigData.CookieHeader.Value,
                Referer = TorrentDescriptionUrl.Replace("{id}", id.ToString())
            };

            // Get thanks page -- (not used, just for doing a request)
            latencyNow();
            output("Thanks user, to get download link for our torrent.. with " + TorrentThanksUrl);
            var thanksPage = await webclient.GetString(myRequestThanks);

            // Get torrent file now
            output("Getting torrent file now....");
            var response = await base.Download(link);

            // Remove our XHR request header
            emulatedBrowserHeaders.Remove("X-Prototype-Version");
            emulatedBrowserHeaders.Remove("X-Requested-With");

            // Return content
            return response;
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
            if (ConfigData.Browser.Value == true)
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
            else
            {
                // Browser simulation must be enabled (otherwhise, this provider will not work due to tracker's security)
                throw new ExceptionWithConfigData("Browser Simulation must be enabled for this provider to work, please enable it !", ConfigData);
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