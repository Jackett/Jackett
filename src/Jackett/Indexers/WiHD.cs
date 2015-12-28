using CsQuery;
using Jackett.Models;
using Jackett.Models.IndexerConfig;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Jackett.Indexers
{
    /// <summary>
    /// Provider for WiHD Private French Tracker
    /// </summary>
    public class WiHD : BaseIndexer, IIndexer
    {
        private string LoginUrl { get { return SiteLink + "login"; } }
        private string LoginCheckUrl { get { return SiteLink + "login_check"; } }
        private string SearchUrl { get { return SiteLink + "torrent/ajaxfiltertorrent/"; } }
        private string DownloadUrl { get { return SiteLink + "torrents/download/"; } }
        private string GuidUrl { get { return SiteLink + "torrents/view/"; } }
        private Dictionary<string, string> emulatedBrowserHeaders = new Dictionary<string, string>();
        private bool Latency { get { return true; } }
        private bool DevMode { get { return true; } }

        public WiHD(IIndexerManagerService i, IWebClient w, Logger l, IProtectionService ps)
            : base(
                name: "WiHD",
                description: "Your World in High Definition",
                link: "http://world-in-hd.net/",
                caps: new TorznabCapabilities(),
                manager: i,
                client: w,
                logger: l,
                p: ps,
                downloadBase: "http://world-in-hd.net/torrents/download/",
                configData: new ConfigurationDataBasicLogin())
        {
            // Clean capabilities
            TorznabCaps.Categories.Clear();

            // Movies
            AddCategoryMapping("565af82b1fd35761568b4572", TorznabCatType.MoviesHD);        // 1080P
            AddCategoryMapping("565af82b1fd35761568b4574", TorznabCatType.MoviesHD);        // 720P
            AddCategoryMapping("565af82b1fd35761568b4576", TorznabCatType.MoviesHD);        // HDTV
            AddCategoryMapping("565af82b1fd35761568b4578", TorznabCatType.MoviesBluRay);    // Bluray
            AddCategoryMapping("565af82b1fd35761568b457a", TorznabCatType.MoviesHD);        // Bluray Remux
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
        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson) {
            var incomingConfig = new ConfigurationDataBasicLogin();
            incomingConfig.LoadValuesFromJson(configJson);

            // Setting our data for a better emulated browser (maximum security)
            // Get your default browser values here: https://www.whatismybrowser.com/detect/what-http-headers-is-my-browser-sending
            emulatedBrowserHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            //emulatedBrowserHeaders.Add("Accept-Encoding", "gzip, deflate");
            emulatedBrowserHeaders.Add("Accept-Language", "fr-FR,fr;q=0.8,en-US;q=0.6,en;q=0.4,es;q=0.2");
            emulatedBrowserHeaders.Add("DNT", "1");
            emulatedBrowserHeaders.Add("Upgrade-Insecure-Requests", "1");
            emulatedBrowserHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/47.0.2526.106 Safari/537.36");


            // Getting login form to retrieve CSRF token
            var myRequest = new Utils.Clients.WebRequest()
            {
                Url = LoginUrl
            };
            myRequest.Headers = emulatedBrowserHeaders;
            var loginPage = await webclient.GetString(myRequest);

            // Retrieving our CSRF token
            CQ loginPageDom = loginPage.Content;
            var csrfToken = loginPageDom["input[name=\"_csrf_token\"]"].Last();

            // Building login form data
            var pairs = new Dictionary<string, string> {
                { "_csrf_token", csrfToken.Attr("value") },
                { "_username", incomingConfig.Username.Value },
                { "_password", incomingConfig.Password.Value },
                { "_remember_me", "on" },
                { "_submit", "" }
            };

            // Do the login
            var request = new Utils.Clients.WebRequest(){
                Cookies = loginPage.Cookies,
                PostData = pairs,
                Referer = LoginUrl,
                Type = RequestType.POST,
                Url = LoginUrl,
                Headers = emulatedBrowserHeaders
            };

            // Perform loggin
            latencyNow();
            Console.WriteLine("Perform loggin.. with " + LoginCheckUrl);
            var response = await RequestLoginAndFollowRedirect(LoginCheckUrl, pairs, loginPage.Cookies, true, null, null);

            // Test if we are logged in
            await ConfigureIfOK(response.Cookies, response.Content != null && response.Content.Contains("/logout"), () => {
                // Oops, unable to login
                throw new ExceptionWithConfigData("Failed to login", configData);
            });

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

            // Check cache first so we don't query the server
            /*lock (cache)
            {
                // Remove old cache items
                CleanCache();

                var cachedResult = cache.Where(i => i.Query == searchTerm).FirstOrDefault();
                if (cachedResult != null)
                    return cachedResult.Results.Select(s => (ReleaseInfo)s.Clone()).ToArray();
            }*/

            // Add emulated XHR request
            emulatedBrowserHeaders.Add("X-Requested-With", "XMLHttpRequest");

            try
            {
                
                // Cheking if we have cached search for our query
                /*var path = @"D:\wihd.txt";
                CQ fDom = null;
                if (System.IO.File.Exists(path))
                {
                    // Yes
                    Console.WriteLine("Using cached version on hard drive");
                    var json = System.IO.File.ReadAllText(path);
                    fDom = JsonConvert.DeserializeObject<CQ>(json);
                }
                else
                {
                    // No cached version
                    latencyNow();
                    //output("Perform search for \"" + searchTerm + "\"... with " + searchUrl);
                    var results = await RequestStringWithCookiesAndRetry(buildQuery(searchTerm, query, searchUrl), null, null, emulatedBrowserHeaders);
                    System.IO.File.WriteAllText(path, JsonConvert.SerializeObject(results.Content));
                    fDom = results.Content;
                }*/


                CQ fDom;
                int nbResults;

                // Request our first page
                latencyNow();
                var results = await RequestStringWithCookiesAndRetry(buildQuery(searchTerm, query, searchUrl), null, null, emulatedBrowserHeaders);
                fDom = results.Content;

                // Find number of results
                int.TryParse(fDom["div.ajaxtotaltorrentcount"].Text().Trim(new Char[] { ' ', '(', ')' }), out nbResults);
                output("Found " + nbResults + " results for query !");

                // Find torrent rows
                var firstPageRows = fDom[".torrent-item"];
                output("There are " + firstPageRows.Length + " results on the first page !");
                torrentRowList.AddRange(firstPageRows.Select(fRow => fRow.Cq()));

                // If a search term is used, follow upto the first 4 pages (initial plus 3 more)
                int pageLinkCount = (int)Math.Ceiling((double)nbResults / firstPageRows.Length);    // Based on number results and number of torrents on first page
                output("--> Pages available for query: " + pageLinkCount);

                // If we have a term used for search and pagination result superior to one
                if (!string.IsNullOrWhiteSpace(query.GetQueryString()) && pageLinkCount > 1)
                {
                    // Starting with page #2
                    for (int i = 2; i <= Math.Min(4, pageLinkCount); i++)
                    {
                        output("Processing page #" + i);

                        // Request our page
                        latencyNow();
                        results = await RequestStringWithCookiesAndRetry(buildQuery(searchTerm, query, searchUrl, i), null, null, emulatedBrowserHeaders);

                        var additionalPageRows = fDom[".torrent-item"];
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
                    output("Category: " + MapTrackerCatToNewznab(mediaToCategory(categoryID, categoryName)) + " (" + categoryName + ")");

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
                    output("Comments: " + commentsLink.AbsoluteUri);

                    // Torrent Download URL
                    string download = tRow.Find(".download-item > a").Attr("href").ToString().TrimStart('/');
                    Uri downloadLink = new Uri(SiteLink + download);
                    output("Download: " + downloadLink.AbsoluteUri);

                    // Building release infos
                    var release = new ReleaseInfo();
                    release.Category = MapTrackerCatToNewznab(mediaToCategory(categoryID, categoryName));
                    release.Title = name;
                    release.Seeders = seeders;
                    release.Peers = seeders + leechers;
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 345600;
                    release.PublishDate = clock;
                    release.Size = size;
                    release.Guid = detailsLink;
                    release.Comments = commentsLink;
                    release.Link = downloadLink;
                    releases.Add(release);
                }

            }
            catch (Exception ex)
            {
                OnParseError("Error, unable to parse result", ex);
            }

            // Remove our XHR request header
            emulatedBrowserHeaders.Remove("X-Requested-With");

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
        /// <param name="exclu">Exclusive state</param>
        /// <param name="freeleech">Freeleech state</param>
        /// <param name="reseed">Reseed state</param>
        /// <returns>URL to query for parsing and processing results</returns>
        private string buildQuery(string term, TorznabQuery query, string url, int page = 1, int exclu = 0, int freeleech = 0, int reseed = 0)
        {
            var parameters = new NameValueCollection();

            if (string.IsNullOrWhiteSpace(term))
            {
                // If no search string provided, use default (for test)
                term = "the walking dead";
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
            parameters.Add("exclu", exclu.ToString());
            parameters.Add("freeleech", freeleech.ToString());
            parameters.Add("reseed", reseed.ToString());

            // Loop on Categories needed
            List<string> categoriesList = MapTorznabCapsToTrackers(query);
            foreach (string category in categoriesList)
            {
                // Adding category to URL
                parameters.Add(Uri.EscapeDataString("subcat[]"), category);
            }

            // Add timestamp as a query param (for no caching)
            parameters.Add("_", UnixTimeNow().ToString());

            // Building our query
            url += parameters.GetQueryString();

            output("Builded query for \"" + term + "\"... with " + url);

            // Return our search url
            return url;
        }

        /// <summary>
        /// Generate a random fake latency to avoid detection on tracker side
        /// </summary>
        private void latencyNow(int first = 1589, int second = 3674)
        {
            // Need latency ?
            if(Latency)
            {
                var random = new Random(DateTime.Now.Millisecond);
                int waiting = random.Next(first, second);
                output("Latency Faker => Sleeping for " + waiting + " ms...");
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
        /// Convert Ago date to DateTime
        /// </summary>
        /// <param name="clockList"></param>
        /// <returns>A DateTime</returns>
        private DateTime agoToDate(IList<string> clockList)
        {
            DateTime release = DateTime.Now;
            foreach(var ago in clockList)
            {
                // Check for years
                if(ago.Contains("Années") || ago.Contains("Année"))
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
                if(media == "565af82d1fd35761568b4592" && name == "Animations - Bluray 3D")
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
                        logger.Debug(message);
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
    }
}
