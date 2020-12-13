using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using WebRequest = Jackett.Common.Utils.Clients.WebRequest;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class Xthor : BaseCachingWebIndexer
    {
        private static string ApiEndpoint => "https://api.xthor.tk/";

        public override string[] LegacySiteLinks { get; protected set; } = {
            "https://xthor.bz/",
            "https://xthor.to"
        };

        private string TorrentDetailsUrl => SiteLink + "details.php?id={id}";
        private string ReplaceMulti => ConfigData.ReplaceMulti.Value;
        private bool EnhancedAnime => ConfigData.EnhancedAnime.Value;
        private bool DevMode => ConfigData.DevMode.Value;
        private bool CacheMode => ConfigData.HardDriveCache.Value;
        private static string Directory => Path.Combine(Path.GetTempPath(), Assembly.GetExecutingAssembly().GetName().Name.ToLower(), MethodBase.GetCurrentMethod().DeclaringType?.Name.ToLower());
        public Dictionary<string, string> EmulatedBrowserHeaders { get; } = new Dictionary<string, string>();
        private ConfigurationDataXthor ConfigData => (ConfigurationDataXthor)configData;

        public Xthor(IIndexerConfigurationService configService, Utils.Clients.WebClient w, Logger l,
            IProtectionService ps, ICacheService cs)
            : base(id: "xthor",
                   name: "Xthor",
                   description: "General French Private Tracker",
                   link: "https://xthor.tk/",
                   caps: new TorznabCapabilities
                   {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                       },
                       MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q
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
                   cacheService: cs,
                   downloadBase: "https://xthor.tk/download.php?torrent=",
                   configData: new ConfigurationDataXthor())
        {
            Encoding = Encoding.UTF8;
            Language = "fr-fr";
            Type = "private";

            // Movies / Films
            AddCategoryMapping(118, TorznabCatType.MoviesBluRay, "Films 2160p/Bluray");
            AddCategoryMapping(119, TorznabCatType.MoviesBluRay, "Films 2160p/Remux");
            AddCategoryMapping(107, TorznabCatType.MoviesUHD, "Films 2160p/x265");
            AddCategoryMapping(1, TorznabCatType.MoviesBluRay, "Films 1080p/BluRay");
            AddCategoryMapping(2, TorznabCatType.MoviesBluRay, "Films 1080p/Remux");
            AddCategoryMapping(100, TorznabCatType.MoviesHD, "Films 1080p/x265");
            AddCategoryMapping(4, TorznabCatType.MoviesHD, "Films 1080p/x264");
            AddCategoryMapping(5, TorznabCatType.MoviesHD, "Films 720p/x264");
            AddCategoryMapping(7, TorznabCatType.MoviesSD, "Films SD/x264");
            AddCategoryMapping(3, TorznabCatType.Movies3D, "Films 3D");
            AddCategoryMapping(6, TorznabCatType.MoviesSD, "Films XviD");
            AddCategoryMapping(8, TorznabCatType.MoviesDVD, "Films DVD");
            AddCategoryMapping(122, TorznabCatType.MoviesHD, "Films HDTV");
            AddCategoryMapping(94, TorznabCatType.MoviesWEBDL, "Films WEBDL");
            AddCategoryMapping(95, TorznabCatType.MoviesWEBDL, "Films WEBRiP");
            AddCategoryMapping(12, TorznabCatType.TVDocumentary, "Films Documentaire");
            AddCategoryMapping(31, TorznabCatType.MoviesOther, "Films Animation");
            AddCategoryMapping(33, TorznabCatType.MoviesOther, "Films Spectacle");
            AddCategoryMapping(125, TorznabCatType.TVSport, "Films Sports");
            AddCategoryMapping(20, TorznabCatType.AudioVideo, "Films Concerts, Clips");
            AddCategoryMapping(9, TorznabCatType.MoviesOther, "Films VOSTFR");

            // TV / Series
            AddCategoryMapping(104, TorznabCatType.TVOther, "Series BluRay");
            AddCategoryMapping(13, TorznabCatType.TVOther, "Series Pack VF");
            AddCategoryMapping(15, TorznabCatType.TVHD, "Series HD VF");
            AddCategoryMapping(14, TorznabCatType.TVSD, "Series SD VF");
            AddCategoryMapping(98, TorznabCatType.TVOther, "Series Pack VOSTFR");
            AddCategoryMapping(17, TorznabCatType.TVHD, "Series HD VOSTFR");
            AddCategoryMapping(16, TorznabCatType.TVSD, "Series SD VOSTFR");
            AddCategoryMapping(101, TorznabCatType.TVAnime, "Series Packs Anime");
            AddCategoryMapping(32, TorznabCatType.TVAnime, "Series Animes");
            AddCategoryMapping(110, TorznabCatType.TVAnime, "Series Anime VOSTFR");
            AddCategoryMapping(123, TorznabCatType.TVOther, "Series Animation");
            AddCategoryMapping(109, TorznabCatType.TVDocumentary, "Series DOC");
            AddCategoryMapping(34, TorznabCatType.TVOther, "Series Sport");
            AddCategoryMapping(30, TorznabCatType.TVOther, "Series Emission TV");

            // XxX / MISC
            AddCategoryMapping(36, TorznabCatType.XXX, "MISC XxX/Films");
            AddCategoryMapping(105, TorznabCatType.XXX, "MISC XxX/Séries");
            AddCategoryMapping(114, TorznabCatType.XXX, "MISC XxX/Lesbiennes");
            AddCategoryMapping(115, TorznabCatType.XXX, "MISC XxX/Gays");
            AddCategoryMapping(113, TorznabCatType.XXX, "MISC XxX/Hentai");
            AddCategoryMapping(120, TorznabCatType.XXX, "MISC XxX/Magazines");

            // Books / Livres
            AddCategoryMapping(24, TorznabCatType.BooksEBook, "Livres Romans");
            AddCategoryMapping(124, TorznabCatType.AudioAudiobook, "Livres Audio Books");
            AddCategoryMapping(96, TorznabCatType.BooksMags, "Livres  Magazines");
            AddCategoryMapping(99, TorznabCatType.BooksOther, "Livres Bandes dessinées");
            AddCategoryMapping(116, TorznabCatType.BooksEBook, "Livres Romans Jeunesse");
            AddCategoryMapping(102, TorznabCatType.BooksComics, "Livres Comics");
            AddCategoryMapping(103, TorznabCatType.BooksOther, "Livres Mangas");

            // SOFTWARE / Logiciels
            AddCategoryMapping(25, TorznabCatType.PCGames, "Logiciels Jeux PC");
            AddCategoryMapping(27, TorznabCatType.ConsolePS3, "Logiciels Playstation");
            AddCategoryMapping(111, TorznabCatType.PCMac, "Logiciels Jeux MAC");
            AddCategoryMapping(26, TorznabCatType.ConsoleXBox360, "Logiciels XboX");
            AddCategoryMapping(112, TorznabCatType.PC, "Logiciels Jeux Linux");
            AddCategoryMapping(28, TorznabCatType.ConsoleWii, "Logiciels Nintendo");
            AddCategoryMapping(29, TorznabCatType.ConsoleNDS, "Logiciels NDS");
            AddCategoryMapping(117, TorznabCatType.PC, "Logiciels ROM");
            AddCategoryMapping(21, TorznabCatType.PC, "Logiciels Applis PC");
            AddCategoryMapping(22, TorznabCatType.PCMac, "Logiciels Applis Mac");
            AddCategoryMapping(23, TorznabCatType.PCMobileAndroid, "Logiciels Smartphone");
        }

        /// <summary>
        /// Configure our Provider
        /// </summary>
        /// <param name="configJson">Our params in Json</param>
        /// <returns>Configuration state</returns>

        // Warning 1998 is async method with no await calls inside
        // TODO: Remove pragma by wrapping return in Task.FromResult and removing async

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
            // EmulatedBrowserHeaders.Add("Accept-Encoding", "gzip, deflate");

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
            var searchTerm = query.GetEpisodeSearchString() + " " + query.SanitizedSearchTerm; // use episode search string first, see issue #1202
            searchTerm = searchTerm.Trim();
            searchTerm = searchTerm.ToLower();

            if (EnhancedAnime && query.HasSpecifiedCategories && (query.Categories.Contains(TorznabCatType.TVAnime.ID) || query.Categories.Contains(100032) || query.Categories.Contains(100101) || query.Categories.Contains(100110)))
            {
                var regex = new Regex(" ([0-9]+)");
                searchTerm = regex.Replace(searchTerm, " E$1");
            }

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
                var xthorResponse = JsonConvert.DeserializeObject<XthorResponse>(results);

                // Check Tracker's State
                CheckApiState(xthorResponse.error);

                // If contains torrents
                if (xthorResponse.torrents != null)
                {
                    // Adding each torrent row to releases
                    releases.AddRange(xthorResponse.torrents.Select(torrent =>
                    {
                        //issue #3847 replace multi keyword
                        if (!string.IsNullOrEmpty(ReplaceMulti))
                        {
                            var regex = new Regex("(?i)([\\.\\- ])MULTI([\\.\\- ])");
                            torrent.name = regex.Replace(torrent.name, "$1" + ReplaceMulti + "$2");
                        }

                        // issue #8759 replace vostfr and subfrench with English
                        if (ConfigData.Vostfr.Value) torrent.name = torrent.name.Replace("VOSTFR","ENGLISH").Replace("SUBFRENCH","ENGLISH");

                        var publishDate = DateTimeUtil.UnixTimestampToDateTime(torrent.added);
                        //TODO replace with download link?
                        var guid = new Uri(TorrentDetailsUrl.Replace("{id}", torrent.id.ToString()));
                        var details = new Uri(TorrentDetailsUrl.Replace("{id}", torrent.id.ToString()));
                        var link = new Uri(torrent.download_link);
                        var release = new ReleaseInfo
                        {
                            // Mapping data
                            Category = MapTrackerCatToNewznab(torrent.category.ToString()),
                            Title = torrent.name,
                            Seeders = torrent.seeders,
                            Peers = torrent.seeders + torrent.leechers,
                            MinimumRatio = 1,
                            MinimumSeedTime = 345600,
                            PublishDate = publishDate,
                            Size = torrent.size,
                            Grabs = torrent.times_completed,
                            Files = torrent.numfiles,
                            UploadVolumeFactor = 1,
                            DownloadVolumeFactor = (torrent.freeleech == 1 ? 0 : 1),
                            Guid = guid,
                            Details = details,
                            Link = link,
                            TMDb = torrent.tmdb_id
                        };

                        //TODO make consistent with other trackers
                        if (DevMode)
                        {
                            Output(release.ToString());
                        }

                        return release;
                    }));
                }
            }
            catch (Exception ex)
            {
                OnParseError("Unable to parse result \n" + ex.StackTrace, ex);
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
            public int tmdb_id { get; set; }

            public override string ToString() => string.Format("[XthorTorrent: id={0}, category={1}, seeders={2}, leechers={3}, name={4}, times_completed={5}, size={6}, added={7}, freeleech={8}, numfiles={9}, release_group={10}, download_link={11}, tmdb_id={12}]", id, category, seeders, leechers, name, times_completed, size, added, freeleech, numfiles, release_group, download_link, tmdb_id);
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
                parameters.Add("search", WebUtility.UrlEncode(term));
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
                parameters.Add("category", string.Join("+", categoriesList));
            }

            // If Only Freeleech Enabled
            if (ConfigData.Freeleech.Value)
            {
                parameters.Add("freeleech", "1");
            }

            if (!string.IsNullOrEmpty(ConfigData.Accent.Value))
            {
                parameters.Add("accent", ConfigData.Accent.Value);
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
        private async Task<string> QueryExec(string request)
        {
            string results;

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
        private async Task<string> QueryCache(string request)
        {
            string results;

            // Create Directory if not exist
            System.IO.Directory.CreateDirectory(Directory);

            // Clean Storage Provider Directory from outdated cached queries
            CleanCacheStorage();

            // File Name
            var fileName = StringUtil.HashSHA1(request) + ".json";

            // Create fingerprint for request
            var file = Path.Combine(Directory, fileName);

            // Checking modes states
            if (File.Exists(file))
            {
                // File exist... loading it right now !
                Output("Loading results from hard drive cache ..." + fileName);
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
                    Output("Error loading cached results ! " + e.Message, "error");
                    results = null;
                }
            }
            else
            {
                // No cached file found, querying tracker directly
                results = await QueryTracker(request);

                // Cached file didn't exist for our query, writing it right now !
                Output("Writing results to hard drive cache ..." + fileName);
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
        private async Task<string> QueryTracker(string request)
        {
            // Cache mode not enabled or cached file didn't exist for our query
            Output("\nQuerying tracker for results....");

            // Build WebRequest for index
            var myIndexRequest = new WebRequest
            {
                Type = RequestType.GET,
                Url = request,
                Encoding = Encoding,
                Headers = EmulatedBrowserHeaders
            };

            // Request our first page
            var results = await webclient.GetResultAsync(myIndexRequest);
            if (results.Status == HttpStatusCode.InternalServerError) // See issue #2110
                throw new Exception("Internal Server Error (" + results.ContentString + "), probably you reached the API limits, please reduce the number of queries");

            // Return results from tracker
            return results.ContentString;
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
                    throw new Exception("Passkey not found in tracker's database");
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
                    throw new Exception("Tracker is under DDOS attack, API disabled");
                default:
                    // Unknown state
                    Output("\nAPI State : Unknown state, aborting querying ... -> " + state.descr);
                    throw new Exception("Unknown state, aborting querying");
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

            // Check Passkey Setting
            if (string.IsNullOrEmpty(ConfigData.PassKey.Value))
            {
                throw new ExceptionWithConfigData("You must provide your passkey for this tracker to be allowed to use API !", ConfigData);
            }
            else
            {
                Output("Validated Setting -- PassKey (auth) => " + ConfigData.PassKey.Value);
            }

            if (!string.IsNullOrEmpty(ConfigData.Accent.Value) && !string.Equals(ConfigData.Accent.Value, "1") && !string.Equals(ConfigData.Accent.Value, "2"))
            {
                throw new ExceptionWithConfigData("Only '1' or '2' are available in the Accent parameter.", ConfigData);
            }
            else
            {
                Output("Validated Setting -- Accent (audio) => " + ConfigData.Accent.Value);
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
