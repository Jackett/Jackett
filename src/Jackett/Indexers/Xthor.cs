using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
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
    /// Provider for Xthor Private French Tracker
    /// </summary>
    public class Xthor : BaseCachingWebIndexer
    {
        private static string ApiEndpoint => "https://api.xthor.bz/";
        private string TorrentCommentUrl => TorrentDescriptionUrl;
        private string TorrentDescriptionUrl => SiteLink + "details.php?id={id}";
        private bool DevMode => ConfigData.DevMode.Value;
        private bool CacheMode => ConfigData.HardDriveCache.Value;
        private static string Directory => System.IO.Path.GetTempPath() + "Jackett\\" + MethodBase.GetCurrentMethod().DeclaringType?.Name + "\\";
        public Dictionary<string, string> EmulatedBrowserHeaders { get; } = new Dictionary<string, string>();
        private ConfigurationDataXthor ConfigData => (ConfigurationDataXthor)configData;

        public Xthor(IIndexerConfigurationService configService, IWebClient w, Logger l, IProtectionService ps)
            : base(
                name: "Xthor",
                description: "General French Private Tracker",
                link: "https://xthor.bz/",
                caps: new TorznabCapabilities(),
                configService: configService,
                client: w,
                logger: l,
                p: ps,
                downloadBase: "https://xthor.bz/download.php?torrent=",
                configData: new ConfigurationDataXthor())
        {
            Encoding = Encoding.UTF8;
            Language = "fr-fr";
            Type = "private";

            // Clean capabilities
            TorznabCaps.Categories.Clear();

            // Movies
            AddCategoryMapping(6, TorznabCatType.MoviesSD);             // XVID
            AddCategoryMapping(7, TorznabCatType.MoviesSD);             // X264
            AddCategoryMapping(95, TorznabCatType.MoviesSD);            // WEBRIP
            AddCategoryMapping(5, TorznabCatType.MoviesHD);             // HD 720P
            AddCategoryMapping(4, TorznabCatType.MoviesHD);             // HD 1080P X264
            AddCategoryMapping(100, TorznabCatType.MoviesHD);           // HD 1080P X265
            AddCategoryMapping(94, TorznabCatType.MoviesHD);            // WEBDL
            AddCategoryMapping(1, TorznabCatType.MoviesBluRay);         // FULL BLURAY
            AddCategoryMapping(2, TorznabCatType.MoviesBluRay);         // BLURAY REMUX
            AddCategoryMapping(3, TorznabCatType.MoviesBluRay);         // FULL BLURAY 3D
            AddCategoryMapping(8, TorznabCatType.MoviesDVD);            // FULL DVD
            AddCategoryMapping(9, TorznabCatType.MoviesOther);          // VOSTFR
            AddCategoryMapping(36, TorznabCatType.XXX);                 // XXX

            // Series
            AddCategoryMapping(14, TorznabCatType.TVSD);                // SD VF
            AddCategoryMapping(16, TorznabCatType.TVSD);                // SD VF VOSTFR
            AddCategoryMapping(15, TorznabCatType.TVHD);                // HD VF
            AddCategoryMapping(17, TorznabCatType.TVHD);                // HD VF VOSTFR
            AddCategoryMapping(13, TorznabCatType.TVOTHER);             // PACK
            AddCategoryMapping(98, TorznabCatType.TVOTHER);             // PACK VOSTFR HD
            AddCategoryMapping(16, TorznabCatType.TVOTHER);             // PACK VOSTFR SD
            AddCategoryMapping(30, TorznabCatType.TVOTHER);             // EMISSIONS
            AddCategoryMapping(34, TorznabCatType.TVOTHER);             // EMISSIONS
            AddCategoryMapping(33, TorznabCatType.TVOTHER);             // SHOWS

            // Anime
            AddCategoryMapping(31, TorznabCatType.TVAnime);             // MOVIES ANIME
            AddCategoryMapping(32, TorznabCatType.TVAnime);             // SERIES ANIME
            AddCategoryMapping(110, TorznabCatType.TVAnime);            // ANIME VOSTFR
            AddCategoryMapping(101, TorznabCatType.TVAnime);            // PACK ANIME

            // Documentaries
            AddCategoryMapping(12, TorznabCatType.TVDocumentary);       // DOCS

            // Music
            AddCategoryMapping(20, TorznabCatType.AudioVideo);          // CONCERT

            // Other
            AddCategoryMapping(21, TorznabCatType.PC);                  // PC
            AddCategoryMapping(22, TorznabCatType.PCMac);               // PC
            AddCategoryMapping(25, TorznabCatType.PCGames);             // GAMES
            AddCategoryMapping(26, TorznabCatType.ConsoleXbox360);      // GAMES
            AddCategoryMapping(28, TorznabCatType.ConsoleWii);          // GAMES
            AddCategoryMapping(27, TorznabCatType.ConsolePS3);          // GAMES
            AddCategoryMapping(29, TorznabCatType.ConsoleNDS);          // GAMES
            AddCategoryMapping(24, TorznabCatType.BooksEbook);          // EBOOKS
            AddCategoryMapping(96, TorznabCatType.BooksEbook);          // EBOOKS MAGAZINES
            AddCategoryMapping(99, TorznabCatType.BooksEbook);          // EBOOKS ANIME
            AddCategoryMapping(23, TorznabCatType.PCPhoneAndroid);      // ANDROID

            AddCategoryMapping(36, TorznabCatType.XXX);                 // XxX / Films
            AddCategoryMapping(105, TorznabCatType.XXX);                // XxX / Séries
            AddCategoryMapping(114, TorznabCatType.XXX);                // XxX / Lesbiennes 
            AddCategoryMapping(115, TorznabCatType.XXX);                // XxX / Gays
            AddCategoryMapping(113, TorznabCatType.XXX);                // XxX / Hentai
        }

        /// <summary>
        /// Configure our Provider
        /// </summary>
        /// <param name="configJson">Our params in Json</param>
        /// <returns>Configuration state</returns>
#pragma warning disable 1998
        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
#pragma warning restore 1998
        {
            // Provider not yet configured
            IsConfigured = false;

            // Retrieve config values set by Jackett's user
            LoadValuesFromJson(configJson);

            // Check & Validate Config
            ValidateConfig();

            // Setting our data for a better emulated browser (maximum security)
            // TODO: Encoded Content not supported by Jackett at this time
            // emulatedBrowserHeaders.Add("Accept-Encoding", "gzip, deflate");

            // Clean headers
            EmulatedBrowserHeaders.Clear();

            // Inject headers
            EmulatedBrowserHeaders.Add("Accept", "application/json-rpc, application/json");
            EmulatedBrowserHeaders.Add("Content-Type", "application/json-rpc");

            // Tracker is now configured
            IsConfigured = true;

            // Saving data
            SaveConfig();

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
            var searchTerm = query.GetQueryString();
            searchTerm = searchTerm.ToLower();

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
            var request = BuildQuery(searchTerm, query, ApiEndpoint);

            // Getting results & Store content
            var results = await QueryExec(request);

            try
            {
                // Deserialize our Json Response
                var xthorResponse = JsonConvert.DeserializeObject<XthorResponse>(results.Content);

                // Check Tracker's State
                CheckApiState(xthorResponse.error);

                // If contains torrents
                if (xthorResponse.torrents != null)
                {
                    // Adding each torrent row to releases
                    releases.AddRange(xthorResponse.torrents.Select(torrent => new ReleaseInfo
                    {
                        // Mapping data
                        Category = MapTrackerCatToNewznab(torrent.category.ToString()),
                        Title = torrent.name,
                        Seeders = torrent.seeders,
                        Peers = torrent.seeders + torrent.leechers,
                        MinimumRatio = 1,
                        MinimumSeedTime = 345600,
                        PublishDate = DateTimeUtil.UnixTimestampToDateTime(torrent.added),
                        Size = torrent.size,
                        Grabs = torrent.times_completed,
                        Files = torrent.numfiles,
                        UploadVolumeFactor = 1,
                        DownloadVolumeFactor = (torrent.freeleech == 1 ? 0 : 1),
                        Guid = new Uri(TorrentDescriptionUrl.Replace("{id}", torrent.id.ToString())),
                        Comments = new Uri(TorrentCommentUrl.Replace("{id}", torrent.id.ToString())),
                        Link = new Uri(torrent.download_link)
                    }));
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
        /// Response from Tracker's API
        /// </summary>
        public class XthorResponse
        {
            public XthorError error { get; set; }
            public XthorUser user { get; set; }
            public List<XthorTorrent> torrents { get; set; }
        }

        /// <summary>
        /// State of API
        /// </summary>
        public class XthorError
        {
            public int code { get; set; }
            public string descr { get; set; }
        }

        /// <summary>
        /// User Informations
        /// </summary>
        public class XthorUser
        {
            public int id { get; set; }
            public string username { get; set; }
            public long uploaded { get; set; }
            public long downloaded { get; set; }
            public int uclass { get; set; } // Class is a reserved keyword.
            public decimal bonus_point { get; set; }
            public int hits_and_run { get; set; }
            public string avatar_url { get; set; }
        }

        /// <summary>
        /// Torrent Informations
        /// </summary>
        public class XthorTorrent
        {
            public int id { get; set; }
            public int category { get; set; }
            public int seeders { get; set; }
            public int leechers { get; set; }
            public string name { get; set; }
            public int times_completed { get; set; }
            public long size { get; set; }
            public int added { get; set; }
            public int freeleech { get; set; }
            public int numfiles { get; set; }
            public string release_group { get; set; }
            public string download_link { get; set; }
        }

        /// <summary>
        /// Build query to process
        /// </summary>
        /// <param name="term">Term to search</param>
        /// <param name="query">Torznab Query for categories mapping</param>
        /// <param name="url">Search url for provider</param>
        /// <returns>URL to query for parsing and processing results</returns>
        private string BuildQuery(string term, TorznabQuery query, string url)
        {
            var parameters = new NameValueCollection();
            var categoriesList = MapTorznabCapsToTrackers(query);

            // Passkey
            parameters.Add("passkey", ConfigData.PassKey.Value);

            // If search term provided
            if (!string.IsNullOrWhiteSpace(term))
            {
                // Add search term
                // ReSharper disable once AssignNullToNotNullAttribute
                parameters.Add("search", HttpUtility.UrlEncode(term));
            }
            else
            {
                parameters.Add("search", string.Empty);
                // Showing all torrents (just for output function)
                term = "all";
            }

            // Loop on Categories needed
            if (categoriesList.Count > 0)
            {
                // ignore categories for now, something changed or is buggy, needs investigation
                //parameters.Add("category", string.Join("+", categoriesList));
            }

            // If Only Freeleech Enabled
            if (ConfigData.Freeleech.Value)
            {
                parameters.Add("freeleech", "1");
            }

            // Building our query -- Cannot use GetQueryString due to UrlEncode (generating wrong category param)
            url += "?" + string.Join("&", parameters.AllKeys.Select(a => a + "=" + parameters[a]));

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
            string file = Directory + request.GetHashCode() + ".json";

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

            // Build WebRequest for index
            var myIndexRequest = new WebRequest()
            {
                Type = RequestType.GET,
                Url = request,
                Encoding = Encoding,
                Headers = EmulatedBrowserHeaders
            };

            // Request our first page
            var results = await webclient.GetString(myIndexRequest);

            // Return results from tracker
            return results;
        }

        /// <summary>
        /// Check API's state
        /// </summary>
        /// <param name="state">State of API</param>
        private void CheckApiState(XthorError state)
        {
            // Switch on state
            switch (state.code)
            {
                case 0:
                    // Everything OK
                    Output("\nAPI State : Everything OK ... -> " + state.descr);
                    break;
                case 1:
                    // Passkey not found
                    Output("\nAPI State : Error, Passkey not found in tracker's database, aborting... -> " + state.descr);
                    throw new Exception("API State : Error, Passkey not found in tracker's database, aborting... -> " + state.descr);
                case 2:
                    // No results
                    Output("\nAPI State : No results for query ... -> " + state.descr);
                    break;
                case 3:
                    // Power Saver
                    Output("\nAPI State : Power Saver mode, only cached query with no parameters available ... -> " + state.descr);
                    break;
                case 4:
                    // DDOS Attack, API disabled
                    Output("\nAPI State : Tracker is under DDOS attack, API disabled, aborting ... -> " + state.descr);
                    throw new Exception("\nAPI State : Tracker is under DDOS attack, API disabled, aborting ... -> " + state.descr);
                default:
                    // Unknown state
                    Output("\nAPI State : Unknown state, aborting querying ... -> " + state.descr);
                    throw new Exception("API State : Unknown state, aborting querying ... -> " + state.descr);
            }
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

            // Check Passkey Setting
            if (string.IsNullOrEmpty(ConfigData.PassKey.Value))
            {
                throw new ExceptionWithConfigData("You must provide your passkey for this tracker to be allowed to use API !", ConfigData);
            }
            else
            {
                Output("Validated Setting -- PassKey (auth) => " + ConfigData.PassKey.Value);
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