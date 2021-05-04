using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
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
        private string TorrentDetailsUrl => SiteLink + "details.php?id={id}";
        private string ReplaceMulti => ConfigData.ReplaceMulti.Value;
        private bool EnhancedAnime => ConfigData.EnhancedAnime.Value;
        private static int MaxPageLoads => 4;

        public override string[] LegacySiteLinks { get; protected set; } = {
            "https://xthor.bz/",
            "https://xthor.to"
        };
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

            // Api has 1req/2s limit
            webclient.requestDelay = 2.1;

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
            var searchTerm = query.SanitizedSearchTerm + " " + query.GetEpisodeSearchString();
            searchTerm = searchTerm.Trim();
            searchTerm = searchTerm.ToLower();
            searchTerm = searchTerm.Replace(" ", ".");

            if (EnhancedAnime && query.HasSpecifiedCategories && (query.Categories.Contains(TorznabCatType.TVAnime.ID) || query.Categories.Contains(100032) || query.Categories.Contains(100101) || query.Categories.Contains(100110)))
            {
                var regex = new Regex(" ([0-9]+)");
                searchTerm = regex.Replace(searchTerm, " E$1");
            }

            logger.Info("\nXthor - Search requested for \"" + searchTerm + "\"");

            // Multiple page support
            var nextPage = 1; var followingPages = true;
            do
            {

                // Build our query
                var request = BuildQuery(searchTerm, query, ApiEndpoint, nextPage);

                // Getting results
                logger.Info("\nXthor - Querying API page " + nextPage);
                var results = await QueryTrackerAsync(request);

                // Torrents Result Count
                var torrentsCount = 0;

                try
                {
                    // Deserialize our Json Response
                    var xthorResponse = JsonConvert.DeserializeObject<XthorResponse>(results);

                    // Check Tracker's State
                    CheckApiState(xthorResponse.Error);

                    // If contains torrents
                    if (xthorResponse.Torrents != null)
                    {
                        // Store torrents rows count result
                        torrentsCount = xthorResponse.Torrents.Count();
                        logger.Info("\nXthor - Found " + torrentsCount + " torrents on current page.");

                        // Adding each torrent row to releases
                        // Exclude hidden torrents (category 106, example => search 'yoda' in the API) #10407
                        releases.AddRange(xthorResponse.Torrents
                            .Where(torrent => torrent.Category != 106).Select(torrent =>
                            {
                                //issue #3847 replace multi keyword
                                if (!string.IsNullOrEmpty(ReplaceMulti))
                                {
                                    var regex = new Regex("(?i)([\\.\\- ])MULTI([\\.\\- ])");
                                    torrent.Name = regex.Replace(torrent.Name, "$1" + ReplaceMulti + "$2");
                                }

                                // issue #8759 replace vostfr and subfrench with English
                                if (ConfigData.Vostfr.Value)
                                    torrent.Name = torrent.Name.Replace("VOSTFR", "ENGLISH").Replace("SUBFRENCH", "ENGLISH");

                                var publishDate = DateTimeUtil.UnixTimestampToDateTime(torrent.Added);
                                //TODO replace with download link?
                                var guid = new Uri(TorrentDetailsUrl.Replace("{id}", torrent.Id.ToString()));
                                var details = new Uri(TorrentDetailsUrl.Replace("{id}", torrent.Id.ToString()));
                                var link = new Uri(torrent.Download_link);
                                var release = new ReleaseInfo
                                {
                                    // Mapping data
                                    Category = MapTrackerCatToNewznab(torrent.Category.ToString()),
                                    Title = torrent.Name,
                                    Seeders = torrent.Seeders,
                                    Peers = torrent.Seeders + torrent.Leechers,
                                    MinimumRatio = 1,
                                    MinimumSeedTime = 345600,
                                    PublishDate = publishDate,
                                    Size = torrent.Size,
                                    Grabs = torrent.Times_completed,
                                    Files = torrent.Numfiles,
                                    UploadVolumeFactor = 1,
                                    DownloadVolumeFactor = (torrent.Freeleech == 1 ? 0 : 1),
                                    Guid = guid,
                                    Details = details,
                                    Link = link,
                                    TMDb = torrent.Tmdb_id
                                };

                                return release;
                            }));
                            nextPage++;
                    }
                    else
                    {
                        logger.Info("\nXthor - No results found on page  " + (nextPage -1) + ", stopping follow of next page.");
                        //  No results or no more results available
                        followingPages = false;
                    }
                }
                catch (Exception ex)
                {
                    OnParseError("Unable to parse result \n" + ex.StackTrace, ex);
                }

                // Stop ?
                if(nextPage > MaxPageLoads | torrentsCount < 32 | string.IsNullOrWhiteSpace(searchTerm))
                {
                    logger.Info("\nXthor - Stopping follow of next page " + nextPage + " due to page limit or max available results reached or indexer test.");
                    followingPages = false;
                }

            } while (followingPages);

            // Check if there is duplicate and return unique rows - Xthor API can be very buggy !
            var uniqReleases = releases.GroupBy(x => x.Guid).Select(x => x.First()).ToList();
            // Return found releases
            return uniqReleases;
        }

        /// <summary>
        /// Response from Tracker's API
        /// </summary>
        public class XthorResponse
        {
            public XthorError Error { get; set; }
            public XthorUser User { get; set; }
            public List<XthorTorrent> Torrents { get; set; }
        }

        /// <summary>
        /// State of API
        /// </summary>
        public class XthorError
        {
            public int Code { get; set; }
            public string Descr { get; set; }
        }

        /// <summary>
        /// User Informations
        /// </summary>
        public class XthorUser
        {
            public int Id { get; set; }
            public string Username { get; set; }
            public long Uploaded { get; set; }
            public long Downloaded { get; set; }
            public int Uclass { get; set; } // Class is a reserved keyword.
            public decimal Bonus_point { get; set; }
            public int Hits_and_run { get; set; }
            public string Avatar_url { get; set; }
        }

        /// <summary>
        /// Torrent Informations
        /// </summary>
        public class XthorTorrent
        {
            public int Id { get; set; }
            public int Category { get; set; }
            public int Seeders { get; set; }
            public int Leechers { get; set; }
            public string Name { get; set; }
            public int Times_completed { get; set; }
            public long Size { get; set; }
            public int Added { get; set; }
            public int Freeleech { get; set; }
            public int Numfiles { get; set; }
            public string Release_group { get; set; }
            public string Download_link { get; set; }
            public int Tmdb_id { get; set; }

            public override string ToString() => string.Format("[XthorTorrent: id={0}, category={1}, seeders={2}, leechers={3}, name={4}, times_completed={5}, size={6}, added={7}, freeleech={8}, numfiles={9}, release_group={10}, download_link={11}, tmdb_id={12}]", Id, Category, Seeders, Leechers, Name, Times_completed, Size, Added, Freeleech, Numfiles, Release_group, Download_link, Tmdb_id);
        }

        /// <summary>
        /// Build query to process
        /// </summary>
        /// <param name="term">Term to search</param>
        /// <param name="query">Torznab Query for categories mapping</param>
        /// <param name="url">Search url for provider</param>
        /// <returns>URL to query for parsing and processing results</returns>
        private string BuildQuery(string term, TorznabQuery query, string url, int page = 1)
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
                // Showing all torrents
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

            // Pages handling
            if (page > 1 && !string.IsNullOrWhiteSpace(term))
            {
                parameters.Add("page", page.ToString());
            }

            // Building our query -- Cannot use GetQueryString due to UrlEncode (generating wrong category param)
            url += "?" + string.Join("&", parameters.AllKeys.Select(a => a + "=" + parameters[a]));

            logger.Info("\nXthor - Builded query for \"" + term + "\"... " + url);

            // Return our search url
            return url;
        }

        /// <summary>
        /// Get Torrents Page from Tracker by Query Provided
        /// </summary>
        /// <param name="request">URL created by Query Builder</param>
        /// <returns>Results from query</returns>
        private async Task<string> QueryTrackerAsync(string request)
        {
            // Cache mode not enabled or cached file didn't exist for our query
            logger.Debug("\nQuerying tracker for results....");

            // Build WebRequest for index
            var myIndexRequest = new WebRequest
            {
                Type = RequestType.GET,
                Url = request,
                Encoding = Encoding
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
            switch (state.Code)
            {
                case 0:
                    // Everything OK
                    logger.Info("\nXthor - API State : Everything OK ... -> " + state.Descr);
                    break;

                case 1:
                    // Passkey not found
                    logger.Error("\nXthor - API State : Error, Passkey not found in tracker's database, aborting... -> " + state.Descr);
                    throw new Exception("Passkey not found in tracker's database");
                case 2:
                    // No results
                    logger.Info("\nXthor - API State : No results for query ... -> " + state.Descr);
                    break;

                case 3:
                    // Power Saver
                    logger.Warn("\nXthor - API State : Power Saver mode, only cached query with no parameters available ... -> " + state.Descr);
                    break;

                case 4:
                    // DDOS Attack, API disabled
                    logger.Error("\nXthor - API State : Tracker is under DDOS attack, API disabled, aborting ... -> " + state.Descr);
                    throw new Exception("Tracker is under DDOS attack, API disabled");
                case 8:
                    // AntiSpam Protection
                    logger.Warn("\nXthor - API State : Triggered AntiSpam Protection -> " + state.Descr);
                    throw new Exception("Triggered AntiSpam Protection, please delay your requests !");
                default:
                    // Unknown state
                    logger.Error("\nXthor - API State : Unknown state, aborting querying ... -> " + state.Descr);
                    throw new Exception("Unknown state, aborting querying");
            }
        }

        /// <summary>
        /// Validate Config entered by user on Jackett
        /// </summary>
        private void ValidateConfig()
        {
            logger.Debug("\nXthor - Validating Settings ... \n");

            // Check Passkey Setting
            if (string.IsNullOrEmpty(ConfigData.PassKey.Value))
            {
                throw new ExceptionWithConfigData("You must provide your passkey for this tracker to be allowed to use API !", ConfigData);
            }
            else
            {
                logger.Debug("Xthor - Validated Setting -- PassKey (auth) => " + ConfigData.PassKey.Value);
            }

            if (!string.IsNullOrEmpty(ConfigData.Accent.Value) && !string.Equals(ConfigData.Accent.Value, "1") && !string.Equals(ConfigData.Accent.Value, "2"))
            {
                throw new ExceptionWithConfigData("Only '1' or '2' are available in the Accent parameter.", ConfigData);
            }
            else
            {
                logger.Debug("Xthor - Validated Setting -- Accent (audio) => " + ConfigData.Accent.Value);
            }
        }
    }
}
