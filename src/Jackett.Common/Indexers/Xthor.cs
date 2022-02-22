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
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;
using WebRequest = Jackett.Common.Utils.Clients.WebRequest;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class Xthor : BaseCachingWebIndexer
    {
        private static string ApiEndpoint => "https://api.xthor.tk/";
        private int MaxPagesHardLimit => 4;
        private string TorrentDetailsUrl => SiteLink + "details.php?id={id}";
        private string WebRequestDelay => ((SingleSelectConfigurationItem)configData.GetDynamic("webRequestDelay")).Value;
        private int MaxPages => Convert.ToInt32(((SingleSelectConfigurationItem)configData.GetDynamic("maxPages")).Value);
        private bool MaxPagesBypassForTMDB => ((BoolConfigurationItem)configData.GetDynamic("maxPagesBypassForTMDB")).Value;
        private int DropCategories => Convert.ToInt32(((SingleSelectConfigurationItem)configData.GetDynamic("dropCategories")).Value);
        private string MultiReplacement => ((StringConfigurationItem)configData.GetDynamic("multiReplacement")).Value;
        private bool SubReplacement => ((BoolConfigurationItem)configData.GetDynamic("subReplacement")).Value;
        private bool EnhancedAnimeSearch => ((BoolConfigurationItem)configData.GetDynamic("enhancedAnimeSearch")).Value;
        private string SpecificLanguageAccent => ((SingleSelectConfigurationItem)configData.GetDynamic("specificLanguageAccent")).Value;
        private bool FreeleechOnly => ((BoolConfigurationItem)configData.GetDynamic("freeleechOnly")).Value;

        public override string[] LegacySiteLinks { get; protected set; } = {
            "https://xthor.bz/",
            "https://xthor.to"
        };
        private ConfigurationDataPasskey ConfigData => (ConfigurationDataPasskey)configData;

        public Xthor(IIndexerConfigurationService configService, Utils.Clients.WebClient w, Logger l,
            IProtectionService ps, ICacheService cs)
            : base(id: "xthor-api",
                   name: "Xthor API",
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
                           MovieSearchParam.Q, MovieSearchParam.TmdbId
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
                   configData: new ConfigurationDataPasskey()
                  )
        {
            Encoding = Encoding.UTF8;
            Language = "fr-FR";
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

            // Dynamic Configuration
            ConfigData.AddDynamic("optionsConfigurationWarning", new DisplayInfoConfigurationItem(string.Empty, "<center><b>Available Options</b></center>,<br /><br /> <ul><li><b>Freeleech Only</b>: (<i>Restrictive</i>) If you want to discover only freeleech torrents to not impact your ratio, check the related box. So only torrents marked as freeleech will be returned instead of all.</li><br /><li><b>Specific Language</b>: (<i>Restrictive</i>) You can scope your searches with a specific language / accent.</li></ul>"));

            var ConfigFreeleechOnly = new BoolConfigurationItem("Do you want to discover only freeleech tagged torrents ?");
            ConfigData.AddDynamic("freeleechOnly", ConfigFreeleechOnly);

            var ConfigSpecificLanguageAccent = new SingleSelectConfigurationItem("Do you want to scope your searches with a specific language ? (Accent)", new Dictionary<string, string>
            {
                {"0", "All Voices (default)"},
                {"1", "Françaises"},
                {"2", "Quebecoises"},
                {"47", "Françaises et Québécoises"},
                {"3", "Anglaises"},
                {"4", "Japonaises"},
                {"5", "Espagnoles"},
                {"6", "Allemandes"},
                {"7", "Chinoises"},
                {"8", "Italiennes"},
                {"9", "Coréennes"},
                {"10", "Danoises"},
                {"11", "Russes"},
                {"12", "Portugaises"},
                {"13", "Hindi"},
                {"14", "Hollandaises"},
                {"15", "Suédoises"},
                {"16", "Norvégiennes"},
                {"17", "Thaïlandaises"},
                {"18", "Hébreu"},
                {"19", "Persanes"},
                {"20", "Arabes"},
                {"21", "Turques"},
                {"22", "Hongroises"},
                {"23", "Polonaises"},
                {"24", "Finnoises"},
                {"25", "Indonésiennes"},
                {"26", "Roumaines"},
                {"27", "Malaisiennes"},
                {"28", "Estoniennes"},
                {"29", "Islandaises"},
                {"30", "Grecques"},
                {"31", "Serbes"},
                {"32", "Norvégiennes"},
                {"33", "Ukrainiennes"},
                {"34", "Bulgares"},
                {"35", "Tagalogues"},
                {"36", "Xhosa"},
                {"37", "Kurdes"},
                {"38", "Bengali"},
                {"39", "Amhariques"},
                {"40", "Bosniaques"},
                {"41", "Malayalam"},
                {"42", "Télougou"},
                {"43", "Bambara"},
                {"44", "Catalanes"},
                {"45", "Tchèques"},
                {"46", "Afrikaans"}
            })
            { Value = "0" };
            ConfigData.AddDynamic("specificLanguageAccent", ConfigSpecificLanguageAccent);

            ConfigData.AddDynamic("advancedConfigurationWarning", new DisplayInfoConfigurationItem(string.Empty, "<center><b>Advanced Configuration</b></center>,<br /><br /> <center><b><u>WARNING !</u></b> <i>Be sure to read instructions before editing options bellow, you can <b>drastically reduce performance</b> of queries or have <b>non-accurate results</b>.</i></center><br/><br/><ul><li><b>Delay betwwen Requests</b>: (<i>not recommended</i>) you can increase delay to requests made to the tracker, but a minimum of 2.1s is enforced as there is an anti-spam protection.</li><br /><li><b>Max Pages</b>: (<i>not recommended</i>) you can increase max pages to follow when making a request. But be aware that others apps can consider this indexer not working if jackett take too many times to return results. Another thing is that API is very buggy on tracker side, most of time, results of next pages are same ... as the first page. Even if we deduplicate rows, you will loose performance for the same results. You can check logs to see if an higher pages following is not benefical, you will see an error percentage (duplicates) with recommandations.</li><br /><li><b>Bypass for TMDB</b>: (<i>recommended</i>) this indexer is compatible with TMDB queries (<i>for movies only</i>), so when requesting content with an TMDB ID, we will search directly ID on API instead of name. Results will be more accurate, so you can enable a max pages bypass for this query type. You will be at least limited by the hard limit of 4 pages.</li><br /><li><b>Drop categories</b>: (<i>recommended</i>) this indexer has some problems when too many categories are requested for filtering, so you will have better results by dropping categories from TMDB queries or selecting fewer categories in 3rd apps.</li><br /><li><b>Enhanced Anime</b>: if you have \"Anime\", this will improve queries made to this tracker related to this type when making searches.</li><br /><li><b>Multi Replacement</b>: you can dynamically replace the word \"MULTI\" with another of your choice like \"MULTI.FRENCH\" for better analysis of 3rd party softwares.</li><br /><li><b>Sub Replacement</b>: you can dynamically replace the word \"VOSTFR\" or \"SUBFRENCH\" with the word \"ENGLISH\" for better analysis of 3rd party softwares.</li></ul>"));

            var ConfigWebRequestDelay = new SingleSelectConfigurationItem("Which delay do you want to apply between each requests made to tracker ?", new Dictionary<string, string>
            {
                {"2.1", "2.1s (minimum)"},
                {"2.2", "2.2s"},
                {"2.3", "2.3s"},
                {"2.4", "2.4s" },
                {"2.5", "2.5s"},
                {"2.6", "2.6s"}
            })
            { Value = "2.1" };
            ConfigData.AddDynamic("webRequestDelay", ConfigWebRequestDelay);

            var ConfigMaxPages = new SingleSelectConfigurationItem("How many pages do you want to follow ?", new Dictionary<string, string>
            {
                {"1", "1 (32 results - default / best perf.)"},
                {"2", "2 (64 results)"},
                {"3", "3 (96 results)"},
                {"4", "4 (128 results - hard limit max)" },
            })
            { Value = "1" };
            ConfigData.AddDynamic("maxPages", ConfigMaxPages);

            var ConfigMaxPagesBypassForTMDB = new BoolConfigurationItem("Do you want to bypass max pages for TMDB searches ? (Radarr) - Hard limit of 4") { Value = true };
            ConfigData.AddDynamic("maxPagesBypassForTMDB", ConfigMaxPagesBypassForTMDB);

            var ConfigDropCategories = new SingleSelectConfigurationItem("Drop requested categories", new Dictionary<string, string>
            {
                {"0", "Disabled"},
                {"1", "Yes, only for TMDB requests (default)"},
                {"2", "Yes, for all requests"},
            })
            { Value = "1" };
            ConfigData.AddDynamic("dropCategories", ConfigDropCategories);

            var ConfigEnhancedAnimeSearch = new BoolConfigurationItem("Do you want to use enhanced ANIME search ?") { Value = false };
            ConfigData.AddDynamic("enhancedAnimeSearch", ConfigEnhancedAnimeSearch);

            var ConfigMultiReplacement = new StringConfigurationItem("Do you want to replace \"MULTI\" keyword in release title by another word ?") { Value = "MULTI.FRENCH" };
            ConfigData.AddDynamic("multiReplacement", ConfigMultiReplacement);

            var ConfigSubReplacement = new BoolConfigurationItem("Do you want to replace \"VOSTFR\" and \"SUBFRENCH\" with \"ENGLISH\" word ?") { Value = false };
            ConfigData.AddDynamic("subReplacement", ConfigSubReplacement);

            // Api has 1req/2s limit (minimum)
            webclient.requestDelay = Convert.ToDouble(WebRequestDelay);

        }

        /// <summary>
        /// Configure our Provider
        /// </summary>
        /// <param name="configJson">Our params in Json</param>
        /// <returns>Configuration state</returns>

        // Warning 1998 is async method with no await calls inside
        // TODO: Remove pragma by wrapping return in Task.FromResult and removing async

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            // Provider not yet configured
            IsConfigured = false;

            // Retrieve config values set by Jackett's user
            LoadValuesFromJson(configJson);

            logger.Debug("\nXthor - Validating Settings ... \n");

            // Check Passkey Setting
            if (string.IsNullOrEmpty(ConfigData.Passkey.Value))
            {
                throw new ExceptionWithConfigData("You must provide your passkey for this tracker to be allowed to use API !", ConfigData);
            }
            else
            {
                logger.Debug("Xthor - Validated Setting -- PassKey (auth) => " + ConfigData.Passkey.Value);
            }


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

            if (EnhancedAnimeSearch && query.HasSpecifiedCategories && (query.Categories.Contains(TorznabCatType.TVAnime.ID) || query.Categories.Contains(100032) || query.Categories.Contains(100101) || query.Categories.Contains(100110)))
            {
                var regex = new Regex(" ([0-9]+)");
                searchTerm = regex.Replace(searchTerm, " E$1");
            }

            searchTerm = searchTerm.Trim();
            searchTerm = searchTerm.ToLower();
            searchTerm = searchTerm.Replace(" ", ".");

            // Multiple page support
            var nextPage = 1;
            var followingPages = true;
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
                                if (!string.IsNullOrEmpty(MultiReplacement))
                                {
                                    var regex = new Regex("(?i)([\\.\\- ])MULTI([\\.\\- ])");
                                    torrent.Name = regex.Replace(torrent.Name, "$1" + MultiReplacement + "$2");
                                }

                                // issue #8759 replace vostfr and subfrench with English
                                if (SubReplacement)
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
                        logger.Info("\nXthor - No results found on page  " + nextPage + ", stopping follow of next page.");
                        //  No results or no more results available
                        followingPages = false;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    OnParseError("Unable to parse result \n" + ex.StackTrace, ex);
                }

                // Stop ?
                if (query.IsTmdbQuery && MaxPagesBypassForTMDB)
                {
                    if (nextPage > MaxPagesHardLimit)
                    {
                        logger.Info("\nXthor - Stopping follow of next page " + nextPage + " due to page hard limit reached.");
                        break;
                    }
                    logger.Info("\nXthor - Continue to next page " + nextPage + " due to TMDB request and activated max page bypass for this type of query. Max page hard limit: 4.");
                    continue;
                }
                else
                {
                    if (torrentsCount < 32)
                    {
                        logger.Info("\nXthor - Stopping follow of next page " + nextPage + " due max available results reached.");
                        break;
                    }
                    else if (nextPage > MaxPages)
                    {
                        logger.Info("\nXthor - Stopping follow of next page " + nextPage + " due to page limit reached.");
                        break;
                    }
                    else if (query.IsTest)
                    {
                        logger.Info("\nXthor - Stopping follow of next page " + nextPage + " due to index test query.");
                        break;
                    }
                }

            } while (followingPages);

            // Check if there is duplicate and return unique rows - Xthor API can be very buggy !
            var uniqReleases = releases.GroupBy(x => x.Guid).Select(x => x.First()).ToList();
            var errorPercentage = 1 - ((double)uniqReleases.Count() / releases.Count());
            if (errorPercentage >= 0.25)
            {
                logger.Warn("\nXthor - High percentage error detected: " + string.Format("{0:0.0%}", errorPercentage) + "\nWe strongly recommend that you lower max page to 1, as there is no benefit to grab additionnals.\nTracker API sent us duplicated pages with same results, even if we deduplicate returned rows, please consider to lower as it's unnecessary and increase time used for query for the same result.");
            }
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
            parameters.Add("passkey", ConfigData.Passkey.Value);

            if (query.IsTmdbQuery)
            {
                logger.Info("\nXthor - Search requested for movie with TMDB ID n°" + query.TmdbID.ToString());
                parameters.Add("tmdbid", query.TmdbID.ToString());
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(term))
                {
                    // Add search term
                    logger.Info("\nXthor - Search requested for movie with title \"" + term + "\"");
                    parameters.Add("search", WebUtility.UrlEncode(term));
                }
            }

            // Loop on categories needed
            if (categoriesList.Count > 0)
            {
                switch (DropCategories)
                {
                    case 1:
                        // Drop categories for TMDB query only.
                        if (!query.IsTmdbQuery)
                        { goto default; }
                        break;
                    case 2:
                        // Drop categories enabled for all requests
                        break;
                    default:
                        // Default or disabled state (0 value of config switch)
                        parameters.Add("category", string.Join("+", categoriesList));
                        break;
                }
            }

            // If Only Freeleech Enabled
            if (FreeleechOnly)
            {
                parameters.Add("freeleech", "1");
            }

            // If Specific Language Accent Requested
            if (!string.IsNullOrEmpty(SpecificLanguageAccent) && SpecificLanguageAccent != "0")
            {
                parameters.Add("accent", SpecificLanguageAccent);
            }

            // Pages handling
            if (page > 1 && !query.IsTest)
            {
                parameters.Add("page", page.ToString());
            }

            // Building our query -- Cannot use GetQueryString due to UrlEncode (generating wrong category param)
            url += "?" + string.Join("&", parameters.AllKeys.Select(a => a + "=" + parameters[a]));

            logger.Info("\nXthor - Builded query: " + url);

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
                    logger.Debug("\nXthor - API State : Everything OK ... -> " + state.Descr);
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
    }
}
