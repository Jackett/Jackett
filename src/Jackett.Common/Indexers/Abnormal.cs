using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
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
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json;
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
        private bool Latency => ConfigData.Latency.Value;
        private bool DevMode => ConfigData.DevMode.Value;
        private bool CacheMode => ConfigData.HardDriveCache.Value;
        private static string Directory => Path.Combine(Path.GetTempPath(), Assembly.GetExecutingAssembly().GetName().Name.ToLower(), MethodBase.GetCurrentMethod().DeclaringType?.Name.ToLower());

        private readonly Dictionary<string, string> emulatedBrowserHeaders = new Dictionary<string, string>();

        private ConfigurationDataAbnormal ConfigData
        {
            get => (ConfigurationDataAbnormal)configData;
            set => configData = value;
        }

        public Abnormal(IIndexerConfigurationService configService, WebClient w, Logger l, IProtectionService ps)
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
                   downloadBase: "https://abnormal.ws/torrents.php?action=download&id=",
                   configData: new ConfigurationDataAbnormal())
        {
            Language = "fr-fr";
            Encoding = Encoding.UTF8;
            Type = "private";
            // NET::ERR_CERT_DATE_INVALID expired ‎29 ‎July ‎2020
            w.AddTrustedCertificate(new Uri(SiteLink).Host, "9cb32582b564256146616afddbdb8e7c94c428ed");

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
            var myRequest = new Utils.Clients.WebRequest
            {
                Url = LoginUrl
            };

            // Add our headers to request
            myRequest.Headers = emulatedBrowserHeaders;

            // Building login form data
            var pairs = new Dictionary<string, string> {
                { "username", ConfigData.Username.Value },
                { "password", ConfigData.Password.Value },
                { "keeplogged", "1" },
                { "login", "Connexion" }
            };

            // Do the login
            var request = new Utils.Clients.WebRequest
            {
                PostData = pairs,
                Referer = LoginUrl,
                Type = RequestType.POST,
                Url = LoginUrl,
                Headers = emulatedBrowserHeaders
            };

            // Perform loggin
            latencyNow();
            output("\nPerform loggin.. with " + LoginUrl);
            var response = await webclient.GetResultAsync(request);

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
                output("-> Login failed: \"" + message + "\" and " + left + " tries left before being banned for 6 hours !", "error");
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
            var nbResults = 0;
            var pageLinkCount = 0;

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
            var parser = new HtmlParser();
            var dom = parser.ParseDocument(await queryExec(request));

            try
            {
                // Find torrent rows
                var firstPageRows = findTorrentRows(dom);

                // Add them to torrents list
                qRowList.AddRange(firstPageRows);

                // Check if there are pagination links at bottom
                var qPagination = dom.QuerySelectorAll(".linkbox > a");
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
                    for (var i = 2; i <= Math.Min(int.Parse(ConfigData.Pages.Value), pageLinkCount); i++)
                    {
                        output("\nProcessing page #" + i);

                        // Request our page
                        latencyNow();

                        // Build our query
                        var pageRequest = buildQuery(searchTerm, query, searchUrl, i);

                        // Getting results & Store content
                        parser = new HtmlParser();
                        dom = parser.ParseDocument(await queryExec(pageRequest));

                        // Process page results
                        var additionalPageRows = findTorrentRows(dom);

                        // Add them to torrents list
                        qRowList.AddRange(additionalPageRows);
                    }
                }

                // Loop on results
                foreach (var row in qRowList)
                {
                    output("\n=>> Torrent #" + (releases.Count + 1));

                    // ID
                    var id = ParseUtil.CoerceInt(Regex.Match(row.QuerySelector("td:nth-of-type(2) > a").GetAttribute("href"), @"\d+").Value);
                    output("ID: " + id);

                    // Release Name
                    var name = row.QuerySelector("td:nth-of-type(2) > a").TextContent;
                    //issue #3847 replace multi keyword
                    if (!string.IsNullOrEmpty(ReplaceMulti))
                    {
                        var regex = new Regex("(?i)([\\.\\- ])MULTI([\\.\\- ])");
                        name = regex.Replace(name, "$1" + ReplaceMulti + "$2");
                    }
                    output("Release: " + name);

                    // Category
                    var categoryId = row.QuerySelector("td:nth-of-type(1) > a").GetAttribute("href").Replace("torrents.php?cat[]=", string.Empty);
                    var newznab = MapTrackerCatToNewznab(categoryId);
                    output("Category: " + MapTrackerCatToNewznab(categoryId).First().ToString() + " (" + categoryId + ")");

                    // Seeders
                    var seeders = ParseUtil.CoerceInt(Regex.Match(row.QuerySelector("td:nth-of-type(6)").TextContent, @"\d+").Value);
                    output("Seeders: " + seeders);

                    // Leechers
                    var leechers = ParseUtil.CoerceInt(Regex.Match(row.QuerySelector("td:nth-of-type(7)").TextContent, @"\d+").Value);
                    output("Leechers: " + leechers);

                    // Completed
                    var completed = ParseUtil.CoerceInt(Regex.Match(row.QuerySelector("td:nth-of-type(6)").TextContent, @"\d+").Value);
                    output("Completed: " + completed);

                    // Size
                    var sizeStr = row.QuerySelector("td:nth-of-type(5)").TextContent.Replace("Go", "gb").Replace("Mo", "mb").Replace("Ko", "kb");
                    var size = ReleaseInfo.GetBytes(sizeStr);
                    output("Size: " + sizeStr + " (" + size + " bytes)");

                    // Publish DateToString
                    var datestr = row.QuerySelector("span.time").GetAttribute("title");
                    var dateLocal = DateTime.SpecifyKind(DateTime.ParseExact(datestr, "MMM dd yyyy, HH:mm", CultureInfo.InvariantCulture), DateTimeKind.Unspecified);
                    var date = TimeZoneInfo.ConvertTimeToUtc(dateLocal, FranceTz);
                    output("Released on: " + date);

                    // Torrent Details URL
                    var details = new Uri(DetailsUrl + id);
                    output("Details: " + details.AbsoluteUri);

                    // Torrent Download URL
                    Uri downloadLink = null;
                    var link = row.QuerySelector("td:nth-of-type(4) > a").GetAttribute("href");
                    if (!string.IsNullOrEmpty(link))
                    {
                        // Download link available
                        downloadLink = new Uri(SiteLink + link);
                        output("Download Link: " + downloadLink.AbsoluteUri);
                    }
                    else
                    {
                        // No download link available -- Must be on pending ( can't be downloaded now...)
                        output("Download Link: Not available, torrent pending ? Skipping ...");
                        continue;
                    }

                    // Freeleech
                    var downloadVolumeFactor = 1;
                    if (row.QuerySelector("img[alt=\"Freeleech\"]") != null)
                    {
                        downloadVolumeFactor = 0;
                        output("FreeLeech =)");
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

            output("\nBuilded query for \"" + term + "\"... " + url);

            // Return our search url
            return url;
        }

        /// <summary>
        /// Switch Method for Querying
        /// </summary>
        /// <param name="request">URL created by Query Builder</param>
        /// <returns>Results from query</returns>
        private async Task<string> queryExec(string request)
        {
            string results = null;

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
        private async Task<string> queryCache(string request)
        {
            string results;

            // Create Directory if not exist
            System.IO.Directory.CreateDirectory(Directory);

            // Clean Storage Provider Directory from outdated cached queries
            cleanCacheStorage();

            // File Name
            var fileName = StringUtil.HashSHA1(request) + ".json";

            // Create fingerprint for request
            var file = Path.Combine(Directory, fileName);

            // Checking modes states
            if (File.Exists(file))
            {
                // File exist... loading it right now !
                output("Loading results from hard drive cache ..." + fileName);
                try
                {
                    using (var fileReader = File.OpenText(file))
                    {
                        var serializer = new JsonSerializer();
                        results = (string)serializer.Deserialize(fileReader, typeof(string));
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
                using (var fileWriter = File.CreateText(file))
                {
                    var serializer = new JsonSerializer();
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
        private async Task<string> queryTracker(string request)
        {
            // Cache mode not enabled or cached file didn't exist for our query
            output("\nQuerying tracker for results....");

            // Request our first page
            latencyNow();
            var results = await RequestWithCookiesAndRetryAsync(request, headers: emulatedBrowserHeaders);

            // Return results from tracker
            return results.ContentString;
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
                var waiting = random.Next(Convert.ToInt32(ConfigData.LatencyStart.Value), Convert.ToInt32(ConfigData.LatencyEnd.Value));
                output("\nLatency Faker => Sleeping for " + waiting + " ms...");

                // Sleep now...
                System.Threading.Thread.Sleep(waiting);
            }
        }

        /// <summary>
        /// Find torrent rows in search pages
        /// </summary>
        /// <returns>List of rows</returns>
        private IHtmlCollection<IElement> findTorrentRows(IHtmlDocument dom) =>
            dom.QuerySelectorAll(".torrent_table > tbody > tr:not(.colhead)");

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
