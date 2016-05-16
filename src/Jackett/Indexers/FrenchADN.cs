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
    public class FrenchAdn : BaseIndexer, IIndexer
    {
        private string LoginUrl => SiteLink + "login.php?";
        private string LoginCheckUrl => SiteLink + "takelogin.php";
        private string SearchUrl => SiteLink + "browse.php";
        private string TorrentCommentUrl => SiteLink + "details.php?id={id}#comments";
        private string TorrentDescriptionUrl => SiteLink + "details.php?id={id}";
        private string TorrentDownloadUrl => SiteLink + "download.php?id={id}";
        private string TorrentThanksUrl => SiteLink + "takethanks.php";
        private bool Latency => ConfigData.Latency.Value;
        private bool DevMode => ConfigData.DevMode.Value;
        private bool CacheMode => ConfigData.HardDriveCache.Value;
        private static string Directory => System.IO.Path.GetTempPath() + "Jackett\\" + MethodBase.GetCurrentMethod().DeclaringType?.Name + "\\";

        private readonly Dictionary<string, string> _emulatedBrowserHeaders = new Dictionary<string, string>();
        private CQ _fDom;
        private ConfigurationDataFrenchAdn ConfigData => (ConfigurationDataFrenchAdn)configData;

        public FrenchAdn(IIndexerManagerService i, IWebClient w, Logger l, IProtectionService ps)
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
                configData: new ConfigurationDataFrenchAdn())
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
            var myIndexRequest = new WebRequest()
            {
                Type = RequestType.GET,
                Url = SiteLink,
                Headers = _emulatedBrowserHeaders
            };

            // Get index page for cookies
            Output("\nGetting index page (for cookies).. with " + SiteLink);
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
                Headers = _emulatedBrowserHeaders,
                Cookies = indexPage.Cookies,
                Referer = SiteLink
            };

            // Get login page -- (not used, but simulation needed by tracker security's checks)
            LatencyNow();
            Output("\nGetting login page (user simulation).. with " + LoginUrl);
            await webclient.GetString(myRequestLogin);

            // Build WebRequest for submitting authentification
            var request = new WebRequest()
            {
                PostData = pairs,
                Referer = LoginUrl,
                Type = RequestType.POST,
                Url = LoginCheckUrl,
                Headers = _emulatedBrowserHeaders,
                Cookies = indexPage.Cookies,

            };

            // Perform loggin
            LatencyNow();
            Output("\nPerform loggin.. with " + LoginCheckUrl);
            var response = await webclient.GetString(request);

            // Test if we are logged in
            await ConfigureIfOK(response.Cookies, !string.IsNullOrEmpty(response.Cookies) && !response.IsRedirect, () =>
            {
                // Default error message
                var message = "Error during attempt !";

                // Parse redirect header
                var redirectTo = response.RedirectingTo;

                // Analyzer error code
                if (redirectTo.Contains("login.php?error=4"))
                {
                    // Set message
                    message = "Wrong username or password !";
                }

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
            var loggedInCheck = await RequestStringWithCookies(SearchUrl);
            if (!loggedInCheck.Content.Contains("/logout.php"))
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
        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var torrentRowList = new List<CQ>();
            var searchTerm = query.GetQueryString();
            var searchUrl = SearchUrl;

            // Check login before performing a query
           await CheckLogin();

            // Check cache first so we don't query the server (if search term used or not in dev mode)
            if (!DevMode && !string.IsNullOrEmpty(searchTerm))
            {
                lock (cache)
                {
                    // Remove old cache items
                    CleanCache();

                    // Search in cache
                    var cachedResult = cache.FirstOrDefault(i => i.Query == searchTerm);
                    if (cachedResult != null)
                        return cachedResult.Results.Select(s => (ReleaseInfo)s.Clone()).ToArray();
                }
            }

            // Build our query
            var request = BuildQuery(searchTerm, query, searchUrl);

            // Getting results & Store content
            var results = await QueryExec(request);
            _fDom = results.Content;

            try
            {
                // Find torrent rows
                var firstPageRows = FindTorrentRows();

                // Add them to torrents list
                torrentRowList.AddRange(firstPageRows.Select(fRow => fRow.Cq()));

                // Check if there are pagination links at bottom
                var pagination = (_fDom["#quicknavpage_menu"].Length != 0);

                // If pagination available
                int nbResults;
                int pageLinkCount;
                if (pagination)
                {
                    // Retrieve available pages (3 pages shown max)
                    pageLinkCount = _fDom["#navcontainer_f:first > ul"].Find("a").Not(".smalltext").Not("#quicknavpage").Length;

                    // Last button ? (So more than 3 page are available)
                    var more = _fDom["#navcontainer_f:first > ul"].Find("a.smalltext").Length > 1;

                    // More page than 3 pages ?
                    if (more)
                    {
                        // Get total page count from last link
                        pageLinkCount = ParseUtil.CoerceInt(Regex.Match(_fDom["#navcontainer_f:first > ul"].Find("a:eq(4)").Attr("href"), @"\d+").Value);
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
                            Output("\nNo result found for your query, please try another search term ...\n", "info");

                            // No result found for this query
                            return releases;
                        }
                    }
                }
                Output("\nFound " + nbResults + " result(s) (+/- " + firstPageRows.Length + ") in " + pageLinkCount + " page(s) for this query !");
                Output("\nThere are " + firstPageRows.Length + " results on the first page !");

                // If we have a term used for search and pagination result superior to one
                if (!string.IsNullOrWhiteSpace(query.GetQueryString()) && pageLinkCount > 1)
                {
                    // Starting with page #2
                    for (var i = 2; i <= Math.Min(int.Parse(ConfigData.Pages.Value), pageLinkCount); i++)
                    {
                        Output("\nProcessing page #" + i);

                        // Request our page
                        LatencyNow();

                        // Build our query -- Minus 1 to page due to strange pagination number on tracker side, starting with page 0...
                        var pageRequest = BuildQuery(searchTerm, query, searchUrl, i);

                        // Getting results & Store content
                        WebClientStringResult pageResults = await QueryExec(pageRequest);

                        // Assign response
                        _fDom = pageResults.Content;

                        // Process page results
                        var additionalPageRows = FindTorrentRows();

                        // Add them to torrents list
                        torrentRowList.AddRange(additionalPageRows.Select(fRow => fRow.Cq()));
                    }
                }

                // Loop on results
                foreach (var tRow in torrentRowList)
                {
                    Output("\n=>> Torrent #" + (releases.Count + 1));

                    // ID
                    var id = ParseUtil.CoerceInt(Regex.Match(tRow.Find("td:eq(1) > div:first > a").Attr("name"), @"\d+").Value);
                    Output("ID: " + id);

                    // Release Name -- Can be truncated ... Need FIX on tracker's side !
                    var name = tRow.Find("td:eq(1) > div > a:eq(2)").Text().Trim();
                    Output("Release: " + name);

                    // Category
                    var categoryId = ParseUtil.CoerceInt(Regex.Match(tRow.Find("td:eq(0) > a").Attr("href"), @"\d+").Value);
                    var categoryName = tRow.Find("td:eq(0) > a > img").Attr("title").Split(new[] { ':' }, 2)[1].Trim();
                    Output("Category: " + MapTrackerCatToNewznab(categoryId.ToString()) + " (" + categoryId + " - " + categoryName + ")");

                    // Seeders
                    var seeders = ParseUtil.CoerceInt(Regex.Match(tRow.Find("td:eq(4) > div > font > a").Text(), @"\d+").Value);
                    Output("Seeders: " + seeders);

                    // Leechers
                    var leechers = ParseUtil.CoerceInt(Regex.Match(tRow.Find("td:eq(5) > div > font").Text(), @"\d+").Value);
                    Output("Leechers: " + leechers);

                    // Files
                    var files = ParseUtil.CoerceInt(Regex.Match(tRow.Find("td:eq(2)").Text(), @"\d+").Value);
                    Output("Files: " + files);

                    // Comments
                    var comments = ParseUtil.CoerceInt(Regex.Match(tRow.Find("td:eq(3)").Text(), @"\d+").Value);
                    Output("Comments: " + files);

                    // Health
                    var percent = ParseUtil.CoerceInt(Regex.Match(tRow.Find("td:eq(6) > img").Attr("src"), @"\d+").Value) * 10;
                    Output("Health: " + percent + "%");

                    // Size
                    var humanSize = tRow.Find("td:eq(7)").Text().ToLowerInvariant();
                    var size = ReleaseInfo.GetBytes(humanSize);
                    Output("Size: " + humanSize + " (" + size + " bytes)");

                    // Date & Genre
                    var infosData = tRow.Find("td:eq(1) > div:last").Text();
                    var infosList = Regex.Split(infosData, "\\|").ToList();
                    var infosTorrent = infosList.Select(s => s.Split(new[] { ':' }, 2)[1].Trim()).ToList();

                    // --> Date
                    var date = FormatDate(infosTorrent.First());
                    Output("Released on: " + date.ToLocalTime());

                    // --> Genre
                    var genre = infosTorrent.Last();
                    Output("Genre: " + genre);

                    // Torrent Details URL
                    var detailsLink = new Uri(TorrentDescriptionUrl.Replace("{id}", id.ToString()));
                    Output("Details: " + detailsLink.AbsoluteUri);

                    // Torrent Comments URL
                    var commentsLink = new Uri(TorrentCommentUrl.Replace("{id}", id.ToString()));
                    Output("Comments Link: " + commentsLink.AbsoluteUri);

                    // Torrent Download URL
                    var downloadLink = new Uri(TorrentDownloadUrl.Replace("{id}", id.ToString()));
                    Output("Download Link: " + downloadLink.AbsoluteUri);

                    // Building release infos
                    var release = new ReleaseInfo
                    {
                        Category = MapTrackerCatToNewznab(categoryId.ToString()),
                        Title = name,
                        Seeders = seeders,
                        Peers = seeders + leechers,
                        MinimumRatio = 1,
                        MinimumSeedTime = 172800,
                        PublishDate = date,
                        Size = size,
                        Guid = detailsLink,
                        Comments = commentsLink,
                        Link = downloadLink
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
        private string BuildQuery(string term, TorznabQuery query, string url, int page = 0)
        {
            var parameters = new NameValueCollection();
            var categoriesList = MapTorznabCapsToTrackers(query);

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
            parameters.Add("category", categoriesList.Count > 0 ? string.Join(",", categoriesList) : "");

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

            Output("\nBuilded query for \"" + term + "\"... " + url);

            // Return our search url
            return url;
        }

        /// <summary>
        /// Switch Method for Querying
        /// </summary>
        /// <param name="request">URL created by Query Builder</param>
        /// <returns>Results from query</returns>
        private async Task<WebClientStringResult> QueryExec(string request)
        {
            WebClientStringResult results;

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
        private async Task<WebClientStringResult> QueryCache(string request)
        {
            WebClientStringResult results;

            // Create Directory if not exist
            System.IO.Directory.CreateDirectory(Directory);

            // Clean Storage Provider Directory from outdated cached queries
            CleanCacheStorage();

            // Create fingerprint for request
            var file = Directory + request.GetHashCode() + ".json";

            // Checking modes states
            if (System.IO.File.Exists(file))
            {
                // File exist... loading it right now !
                Output("Loading results from hard drive cache ..." + request.GetHashCode() + ".json");
                results = JsonConvert.DeserializeObject<WebClientStringResult>(System.IO.File.ReadAllText(file));
            }
            else
            {
                // No cached file found, querying tracker directly
                results = await QueryTracker(request);

                // Cached file didn't exist for our query, writing it right now !
                Output("Writing results to hard drive cache ..." + request.GetHashCode() + ".json");
                System.IO.File.WriteAllText(file, JsonConvert.SerializeObject(results));
            }
            return results;
        }

        /// <summary>
        /// Get Torrents Page from Tracker by Query Provided
        /// </summary>
        /// <param name="request">URL created by Query Builder</param>
        /// <returns>Results from query</returns>
        private async Task<WebClientStringResult> QueryTracker(string request)
        {
            // Cache mode not enabled or cached file didn't exist for our query
            Output("\nQuerying tracker for results....");

            // Request our first page
            LatencyNow();
            var results = await RequestStringWithCookiesAndRetry(request, ConfigData.CookieHeader.Value, SearchUrl, _emulatedBrowserHeaders);

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
                .Select(f => new System.IO.FileInfo(f))
                .Where(f => f.LastAccessTime < DateTime.Now.AddMilliseconds(-Convert.ToInt32(ConfigData.HardDriveCacheKeepTime.Value)))
                .ToList()
                .ForEach(f => {
                    Output("Deleting cached file << " + f.Name + " >> ... done.");
                    f.Delete();
                    i++;
                });

                // Inform on what was cleaned during process
                if (i > 0)
                {
                    Output("-> Deleted " + i + " cached files during cleaning.");
                }
                else {
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
        /// <returns>JQuery Object</returns>
        private CQ FindTorrentRows()
        {
            // Return all occurencis of torrents found
            return _fDom["#showcontents > table > tbody > tr:not(:first)"];
        }

        /// <summary>
        /// Format Date to DateTime
        /// </summary>
        /// <param name="clock"></param>
        /// <returns>A DateTime</returns>
        private static DateTime FormatDate(string clock)
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
                    // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
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
        public override async Task<byte[]> Download(Uri link)
        {
            // This tracker need to thanks Uploader before getting torrent file...
            Output("\nThis tracker needs you to thank uploader before downloading torrent!");

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

            // Build WebRequest for thanks
            var myRequestThanks = new WebRequest()
            {
                Type = RequestType.POST,
                PostData = pairs,
                Url = TorrentThanksUrl,
                Headers = _emulatedBrowserHeaders,
                Cookies = ConfigData.CookieHeader.Value,
                Referer = TorrentDescriptionUrl.Replace("{id}", id.ToString())
            };

            // Get thanks page -- (not used, just for doing a request)
            LatencyNow();
            Output("Thanks user, to get download link for our torrent.. with " + TorrentThanksUrl);
            await webclient.GetString(myRequestThanks);

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