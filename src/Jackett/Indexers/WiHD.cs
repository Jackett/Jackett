using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
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
using System.IO;

namespace Jackett.Indexers
{
    /// <summary>
    /// Provider for WiHD Private French Tracker
    /// </summary>
    public class WiHD : BaseCachingWebIndexer
    {
        private string LoginUrl { get { return SiteLink + "login"; } }
        private string LoginCheckUrl { get { return SiteLink + "login_check"; } }
        private string SearchUrl { get { return SiteLink + "torrent/ajaxfiltertorrent/"; } }
        private bool Latency { get { return ConfigData.Latency.Value; } }
        private bool DevMode { get { return ConfigData.DevMode.Value; } }
        private bool CacheMode { get { return ConfigData.HardDriveCache.Value; } }
        private static string Directory => Path.Combine(Path.GetTempPath(), Assembly.GetExecutingAssembly().GetName().Name.ToLower(), MethodBase.GetCurrentMethod().DeclaringType?.Name.ToLower());

        private Dictionary<string, string> emulatedBrowserHeaders = new Dictionary<string, string>();
        private CQ fDom = null;

        private ConfigurationDataWiHD ConfigData
        {
            get { return (ConfigurationDataWiHD)configData; }
            set { base.configData = value; }
        }

        public WiHD(IIndexerConfigurationService configService, IWebClient w, Logger l, IProtectionService ps)
            : base(
                name: "WiHD",
                description: "Your World in High Definition",
                link: "https://world-in-hd.net/",
                caps: new TorznabCapabilities(),
                configService: configService,
                client: w,
                logger: l,
                p: ps,
                downloadBase: "https://world-in-hd.net/torrents/download/",
                configData: new ConfigurationDataWiHD())
        {
            Encoding = Encoding.UTF8;
            Language = "fr-fr";
            Type = "private";

            // Clean capabilities
            TorznabCaps.Categories.Clear();

            // Movies
            AddCategoryMapping("565af82b1fd35761568b4572", TorznabCatType.MoviesHD);        // 1080P
            AddCategoryMapping("565af82b1fd35761568b4574", TorznabCatType.MoviesHD);        // 720P
            AddCategoryMapping("565af82b1fd35761568b4576", TorznabCatType.MoviesHD);        // HDTV
            AddCategoryMapping("565af82b1fd35761568b4578", TorznabCatType.MoviesBluRay);    // Bluray
            AddCategoryMapping("565af82b1fd35761568b457a", TorznabCatType.MoviesBluRay);    // Bluray Remux
            AddCategoryMapping("565af82b1fd35761568b457c", TorznabCatType.Movies3D);        // Bluray 3D

            // TV
            AddCategoryMapping("565af82d1fd35761568b4587", TorznabCatType.TVHD);            // 1080P
            AddCategoryMapping("565af82d1fd35761568b4589", TorznabCatType.TVHD);            // 720P
            AddCategoryMapping("565af82d1fd35761568b458b", TorznabCatType.TVHD);            // HDTV
            AddCategoryMapping("565af82d1fd35761568b458d", TorznabCatType.TVHD);            // Bluray
            AddCategoryMapping("565af82d1fd35761568b458f", TorznabCatType.TVHD);            // Bluray Remux
            AddCategoryMapping("565af82d1fd35761568b4591", TorznabCatType.TVHD);            // Bluray 3D

            // Anime
            AddCategoryMapping("565af82d1fd35761568b459c", TorznabCatType.TVAnime);         // 1080P
            AddCategoryMapping("565af82d1fd35761568b459e", TorznabCatType.TVAnime);         // 720P
            AddCategoryMapping("565af82d1fd35761568b45a0", TorznabCatType.TVAnime);         // HDTV
            AddCategoryMapping("565af82d1fd35761568b45a2", TorznabCatType.TVAnime);         // Bluray
            AddCategoryMapping("565af82d1fd35761568b45a4", TorznabCatType.TVAnime);         // Bluray Remux
            AddCategoryMapping("565af82d1fd35761568b45a6", TorznabCatType.TVAnime);         // Bluray 3D

            // Other
            AddCategoryMapping("565af82d1fd35761568b45af", TorznabCatType.PC);              // Apps
            AddCategoryMapping("565af82d1fd35761568b45b1", TorznabCatType.AudioVideo);      // Clips
            AddCategoryMapping("565af82d1fd35761568b45b3", TorznabCatType.AudioOther);      // Audios Tracks of Movies/TV/Anime
            AddCategoryMapping("565af82d1fd35761568b45b5", TorznabCatType.TVDocumentary);   // Documentary
            AddCategoryMapping("565af82d1fd35761568b45b7", TorznabCatType.MoviesBluRay);    // Bluray (ALL)
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


            // Getting login form to retrieve CSRF token
            var myRequest = new Utils.Clients.WebRequest()
            {
                Url = LoginUrl
            };

            // Add our headers to request
            myRequest.Headers = emulatedBrowserHeaders;

            // Get login page
            var loginPage = await webclient.GetString(myRequest);

            // Retrieving our CSRF token
            CQ loginPageDom = loginPage.Content;
            var csrfToken = loginPageDom["input[name=\"_csrf_token\"]"].Last();

            // Building login form data
            var pairs = new Dictionary<string, string> {
                { "_csrf_token", csrfToken.Attr("value") },
                { "_username", ConfigData.Username.Value },
                { "_password", ConfigData.Password.Value },
                { "_remember_me", "on" },
                { "_submit", "" }
            };

            // Do the login
            var request = new Utils.Clients.WebRequest()
            {
                Cookies = loginPage.Cookies,
                PostData = pairs,
                Referer = LoginUrl,
                Type = RequestType.POST,
                Url = LoginUrl,
                Headers = emulatedBrowserHeaders
            };

            // Perform loggin
            latencyNow();
            output("\nPerform loggin.. with " + LoginCheckUrl);
            var response = await RequestLoginAndFollowRedirect(LoginCheckUrl, pairs, loginPage.Cookies, true, null, null);

            // Test if we are logged in
            await ConfigureIfOK(response.Cookies, response.Content != null && response.Content.Contains("/logout"), () =>
            {
                // Oops, unable to login
                output("-> Login failed", "error");
                throw new ExceptionWithConfigData("Failed to login", configData);
            });

            output("-> Login Success");

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

            // Add emulated XHR request
            emulatedBrowserHeaders.Add("X-Requested-With", "XMLHttpRequest");

            // Build our query
            var request = buildQuery(searchTerm, query, searchUrl);

            // Getting results & Store content
            fDom = await queryExec(request);

            try
            {
                // Find number of results
                nbResults = ParseUtil.CoerceInt(Regex.Match(fDom["div.ajaxtotaltorrentcount"].Text(), @"\d+").Value);

                // Find torrent rows
                var firstPageRows = findTorrentRows();

                // Add them to torrents list
                torrentRowList.AddRange(firstPageRows.Select(fRow => fRow.Cq()));

                // Check if there are pagination links at bottom
                Boolean pagination = (nbResults != 0);

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
                }

                // Loop on results
                foreach (CQ tRow in torrentRowList)
                {
                    output("\n=>> Torrent #" + (releases.Count + 1));

                    // Release Name
                    string name = tRow.Find(".torrent-h3 > h3 > a").Attr("title").ToString();
                    output("Release: " + name);

                    // Category
                    string categoryID = tRow.Find(".category > img").Attr("src").Split('/').Last().ToString();
                    string categoryName = tRow.Find(".category > img").Attr("title").ToString();
                    output("Category: " + MapTrackerCatToNewznab(mediaToCategory(categoryID, categoryName)).First().ToString() + " (" + categoryName + ")");

                    // Uploader
                    string uploader = tRow.Find(".uploader > span > a").Attr("title").ToString();
                    output("Uploader: " + uploader);

                    // Seeders
                    int seeders = ParseUtil.CoerceInt(Regex.Match(tRow.Find(".seeders")[0].LastChild.ToString(), @"\d+").Value);
                    output("Seeders: " + seeders);

                    // Leechers
                    int leechers = ParseUtil.CoerceInt(Regex.Match(tRow.Find(".leechers")[0].LastChild.ToString(), @"\d+").Value);
                    output("Leechers: " + leechers);

                    // Completed
                    int completed = ParseUtil.CoerceInt(Regex.Match(tRow.Find(".completed")[0].LastChild.ToString(), @"\d+").Value);
                    output("Completed: " + completed);

                    // Comments
                    int comments = ParseUtil.CoerceInt(Regex.Match(tRow.Find(".comments")[0].LastChild.ToString(), @"\d+").Value);
                    output("Comments: " + comments);

                    // Size & Publish Date
                    string infosData = tRow.Find(".torrent-h3 > span")[0].LastChild.ToString().Trim();
                    IList<string> infosList = infosData.Split('-').Select(s => s.Trim()).Where(s => s != String.Empty).ToList();

                    // --> Size
                    var size = ReleaseInfo.GetBytes(infosList[1].Replace("Go", "gb").Replace("Mo", "mb").Replace("Ko", "kb"));
                    output("Size: " + infosList[1] + " (" + size + " bytes)");

                    // --> Publish Date
                    IList<string> clockList = infosList[0].Replace("Il y a", "").Split(',').Select(s => s.Trim()).Where(s => s != String.Empty).ToList();
                    var clock = agoToDate(clockList);
                    output("Released on: " + clock.ToString());

                    // Torrent Details URL
                    string details = tRow.Find(".torrent-h3 > h3 > a").Attr("href").ToString().TrimStart('/');
                    Uri detailsLink = new Uri(SiteLink + details);
                    output("Details: " + detailsLink.AbsoluteUri);

                    // Torrent Comments URL
                    Uri commentsLink = new Uri(SiteLink + details + "#tab_2");
                    output("Comments Link: " + commentsLink.AbsoluteUri);

                    // Torrent Download URL
                    string download = tRow.Find(".download-item > a").Attr("href").ToString().TrimStart('/');
                    Uri downloadLink = new Uri(SiteLink + download);
                    output("Download Link: " + downloadLink.AbsoluteUri);

                    // Freeleech
                    int downloadVolumeFactor = 1;
                    if (tRow.Find(".fl-item").Length >= 1)
                    {
                        downloadVolumeFactor = 0;
                        output("FreeLeech =)");
                    }

                    // Building release infos
                    var release = new ReleaseInfo()
                    {
                        Category = MapTrackerCatToNewznab(mediaToCategory(categoryID, categoryName)),
                        Title = name,
                        Seeders = seeders,
                        Peers = seeders + leechers,
                        MinimumRatio = 1,
                        MinimumSeedTime = 345600,
                        PublishDate = clock,
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
            finally
            {
                // Remove our XHR request header
                emulatedBrowserHeaders.Remove("X-Requested-With");
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
            var parameters = new NameValueCollection();
            List<string> categoriesList = MapTorznabCapsToTrackers(query);
            string categories = null;

            // If search term not provided
            if (string.IsNullOrWhiteSpace(term))
            {
                // Showing all torrents (just for output function)
                term = "null";
            }

            // Encode & Add search term to URL
            url += Uri.EscapeDataString(term);

            // Check if we are processing a new page
            if (page > 1)
            {
                // Adding page number to query
                url += "/" + page.ToString();
            }

            // Adding interrogation point
            url += "?";

            // Building our tracker query
            parameters.Add("exclu", Convert.ToInt32(ConfigData.Exclusive.Value).ToString());
            parameters.Add("freeleech", Convert.ToInt32(ConfigData.Freeleech.Value).ToString());
            parameters.Add("reseed", Convert.ToInt32(ConfigData.Reseed.Value).ToString());

            // Loop on Categories needed
            foreach (string category in categoriesList)
            {
                // If last, build !
                if (categoriesList.Last() == category)
                {
                    // Adding previous categories to URL with latest category
                    parameters.Add(Uri.EscapeDataString("subcat[]"), category + categories);
                }
                else
                {
                    // Build categories parameter
                    categories += "&" + Uri.EscapeDataString("subcat[]") + "=" + category;
                }
            }

            // Add timestamp as a query param (for no caching)
            parameters.Add("_", UnixTimeNow().ToString());

            // Building our query -- Cannot use GetQueryString due to UrlEncode (generating wrong subcat[] param)
            url += string.Join("&", parameters.AllKeys.Select(a => a + "=" + parameters[a]));

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
        /// Generate an UTC Unix TimeStamp
        /// </summary>
        /// <returns>Unix TimeStamp</returns>
        private long UnixTimeNow()
        {
            var timeSpan = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
            return (long)timeSpan.TotalSeconds;
        }

        /// <summary>
        /// Find torrent rows in search pages
        /// </summary>
        /// <returns>JQuery Object</returns>
        private CQ findTorrentRows()
        {
            // Return all occurencis of torrents found
            return fDom[".torrent-item"];
        }

        /// <summary>
        /// Convert Ago date to DateTime
        /// </summary>
        /// <param name="clockList"></param>
        /// <returns>A DateTime</returns>
        private DateTime agoToDate(IList<string> clockList)
        {
            DateTime release = DateTime.Now;
            foreach (var ago in clockList)
            {
                // Check for years
                if (ago.Contains("Années") || ago.Contains("Année"))
                {
                    // Number of years to remove
                    int years = ParseUtil.CoerceInt(Regex.Match(ago.ToString(), @"\d+").Value);
                    // Removing
                    release = release.AddYears(-years);

                    continue;
                }
                // Check for months
                else if (ago.Contains("Mois"))
                {
                    // Number of months to remove
                    int months = ParseUtil.CoerceInt(Regex.Match(ago.ToString(), @"\d+").Value);
                    // Removing
                    release = release.AddMonths(-months);

                    continue;
                }
                // Check for days
                else if (ago.Contains("Jours") || ago.Contains("Jour"))
                {
                    // Number of days to remove
                    int days = ParseUtil.CoerceInt(Regex.Match(ago.ToString(), @"\d+").Value);
                    // Removing
                    release = release.AddDays(-days);

                    continue;
                }
                // Check for hours
                else if (ago.Contains("Heures") || ago.Contains("Heure"))
                {
                    // Number of hours to remove
                    int hours = ParseUtil.CoerceInt(Regex.Match(ago.ToString(), @"\d+").Value);
                    // Removing
                    release = release.AddHours(-hours);

                    continue;
                }
                // Check for minutes
                else if (ago.Contains("Minutes") || ago.Contains("Minute"))
                {
                    // Number of minutes to remove
                    int minutes = ParseUtil.CoerceInt(Regex.Match(ago.ToString(), @"\d+").Value);
                    // Removing
                    release = release.AddMinutes(-minutes);

                    continue;
                }
                // Check for seconds
                else if (ago.Contains("Secondes") || ago.Contains("Seconde"))
                {
                    // Number of seconds to remove
                    int seconds = ParseUtil.CoerceInt(Regex.Match(ago.ToString(), @"\d+").Value);
                    // Removing
                    release = release.AddSeconds(-seconds);

                    continue;
                }
                else
                {
                    output("Unable to detect release date of torrent", "error");
                    //throw new Exception("Unable to detect release date of torrent");
                }
            }
            return release;
        }

        /// <summary>
        /// Retrieve category ID from media ID
        /// </summary>
        /// <param name="media">Media ID</param>
        /// <returns>Category ID</returns>
        private string mediaToCategory(string media, string name)
        {
            // Declare our Dictionnary -- Media ID (key) <-> Category ID (value)
            Dictionary<string, string> dictionary = new Dictionary<string, string>();

            // Movies
            dictionary.Add("565af82b1fd35761568b4573", "565af82b1fd35761568b4572");         // 1080P
            dictionary.Add("565af82b1fd35761568b4575", "565af82b1fd35761568b4574");         // 720P
            dictionary.Add("565af82b1fd35761568b4577", "565af82b1fd35761568b4576");         // HDTV
            dictionary.Add("565af82b1fd35761568b4579", "565af82b1fd35761568b4578");         // Bluray
            dictionary.Add("565af82b1fd35761568b457b", "565af82b1fd35761568b457a");         // Bluray Remux
            dictionary.Add("565af82b1fd35761568b457d", "565af82b1fd35761568b457c");         // Bluray 3D

            // TV
            dictionary.Add("565af82d1fd35761568b4588", "565af82d1fd35761568b4587");         // 1080P
            dictionary.Add("565af82d1fd35761568b458a", "565af82d1fd35761568b4589");         // 720P
            dictionary.Add("565af82d1fd35761568b458c", "565af82d1fd35761568b458b");         // HDTV
            dictionary.Add("565af82d1fd35761568b458e", "565af82d1fd35761568b458d");         // Bluray
            dictionary.Add("565af82d1fd35761568b4590", "565af82d1fd35761568b458f");         // Bluray Remux
            dictionary.Add("565af82d1fd35761568b4592", "565af82d1fd35761568b4591");         // Bluray 3D

            // Anime
            dictionary.Add("565af82d1fd35761568b459d", "565af82d1fd35761568b459c");         // 1080P
            dictionary.Add("565af82d1fd35761568b459f", "565af82d1fd35761568b459e");         // 720P
            dictionary.Add("565af82d1fd35761568b45a1", "565af82d1fd35761568b45a0");         // HDTV
            dictionary.Add("565af82d1fd35761568b45a3", "565af82d1fd35761568b45a2");         // Bluray
            dictionary.Add("565af82d1fd35761568b45a5", "565af82d1fd35761568b45a4");         // Bluray Remux
            // BUG ~~ Media ID for Anime BR 3D is same as TV BR 3D ~~
            //dictionary.Add("565af82d1fd35761568b4592", "565af82d1fd35761568b45a6");       // Bluray 3D

            // Other
            dictionary.Add("565af82d1fd35761568b45b0", "565af82d1fd35761568b45af");         // Apps
            dictionary.Add("565af82d1fd35761568b45b2", "565af82d1fd35761568b45b1");         // Clips
            dictionary.Add("565af82d1fd35761568b45b4", "565af82d1fd35761568b45b3");         // Audios Tracks of Movies/TV/Anime
            dictionary.Add("565af82d1fd35761568b45b6", "565af82d1fd35761568b45b5");         // Documentary
            dictionary.Add("565af82d1fd35761568b45b8", "565af82d1fd35761568b45b7");         // Bluray (ALL)

            // Check if we know this media ID
            if (dictionary.ContainsKey(media))
            {
                // Due to a bug on tracker side, check for a specific id/name as image is same for TV/Anime BR 3D
                if (media == "565af82d1fd35761568b4592" && name == "Animations - Bluray 3D")
                {
                    // If it's an Anime BR 3D
                    return "565af82d1fd35761568b45a6";
                }
                else
                {
                    // Return category ID for media ID
                    return dictionary[media];
                }
            }
            else
            {
                // Media ID unknown
                throw new Exception("Media ID Unknow !");
            }
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
