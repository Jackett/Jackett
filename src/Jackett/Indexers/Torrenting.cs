using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CsQuery;
using Jackett.Models;
using Jackett.Models.IndexerConfig.Bespoke;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Indexers
{
    /// <summary>
    /// Provider for torrenting.me
    /// </summary>
    public class Torrenting : BaseIndexer, IIndexer
    {
        private string LoginUrl { get { return SiteLink + "login.php?returnto=Login "; } }
        private string LoginCheckUrl { get { return SiteLink + "secure.php"; } }
        private string SearchUrl { get { return SiteLink + "browse.php"; } }
        private string DownloadUrl { get { return SiteLink + "download.php"; } }
        private string RSSCats { get { return "3,2,4,5,49,1"; } }
        //41,46,3,42,2,45,48,35,36,37,38,39,43,47,4,5,49,1,44,40
        private string RSSUrl { get { return SiteLink + "get_rss.php?feed=direct&user={1}&cat=" + RSSCats + "&passkey={0}"; } }
        //private string RSSUrl { get { return SiteLink + "get_rss.php?feed=direct&user={1}&cat=" + RSSCats + "&passkey={0}"; } }
        string GetRSSKeyUrl { get { return SiteLink + "rss.php"; } }
        private Dictionary<string, string> emulatedBrowserHeaders = new Dictionary<string, string>();
        private CQ fDom = null;
        private bool DevMode { get { return ConfigData.DevMode.Value; } }

        private ConfigurationDataTorrenting ConfigData
        {
            get { return (ConfigurationDataTorrenting)configData; }
            set { base.configData = value; }
        }

        public Torrenting(IIndexerManagerService i, IWebClient w, Logger l, IProtectionService ps)
            : base(
                name: "Torrenting",
                description: "Entertainment Evolved",
                link: "https://www.torrenting.com/",
                caps: new TorznabCapabilities(),
                manager: i,
                client: w,
                logger: l,
                p: ps,
                configData: new ConfigurationDataTorrenting())
        {
            // Clean capabilities
            TorznabCaps.Categories.Clear();

            // Movies
            AddCategoryMapping("1", TorznabCatType.MoviesSD);        // 1080P
            AddCategoryMapping("2", TorznabCatType.MoviesSD);        // 720P
            AddCategoryMapping("3", TorznabCatType.MoviesBluRay);    // Bluray
            AddCategoryMapping("48", TorznabCatType.Movies);    
            AddCategoryMapping("49", TorznabCatType.Movies);        

            // TV
            AddCategoryMapping("4", TorznabCatType.TVSD);            // 1080P
            AddCategoryMapping("5", TorznabCatType.TVHD);            // 720P
            
            // Other
            AddCategoryMapping("41", TorznabCatType.PC);              // Apps
        }

        /// <summary>
        /// Configure the Provider
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


            // Getting login form to retrieve CSRF token
            var myRequest = new Utils.Clients.WebRequest()
            {
                Url = LoginUrl
            };

            // Add our headers to request
            myRequest.Headers = emulatedBrowserHeaders;

            // Get login page - to receive the phpsessid and __cfduid cookies
            var loginPage = await webclient.GetString(myRequest);

            // Retrieving our CSRF token
            // CQ loginPageDom = loginPage.Content;
            //var csrfToken = loginPageDom["input[name=\"_csrf_token\"]"].Last();

            // Building login form data
            var pairs = new Dictionary<string, string> {
                //{ "_csrf_token", csrfToken.Attr("value") },
                { "username", ConfigData.Username.Value },
                { "password", ConfigData.Password.Value },
                //{ "_remember_me", "on" },
                //{ "_submit", "" }
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
            output("Perform loggin.. with " + LoginCheckUrl);
            var response = await RequestLoginAndFollowRedirect(LoginCheckUrl, pairs, loginPage.Cookies, true, null, null, true);

            // Test if we are logged in
            await ConfigureIfOK(response.Cookies, response.Content != null && response.Content.Contains("/logout"), () => {
                // Oops, unable to login
                throw new ExceptionWithConfigData("Failed to login", configData);
            });

            try
            {
                // Get RSS key
                
                var rssParams = new Dictionary<string, string> {
                    { "feed", "direct" },
                    { "login", "passkey" },
                    { "cat[]", "4" }
                };
                var rssPage = await PostDataWithCookies(GetRSSKeyUrl, rssParams, response.Cookies);
                var match = Regex.Match(rssPage.Content, "(?<=passkey\\=)([a-zA-z0-9]*)");
                ConfigData.RSSKey.Value = match.Success ? match.Value : string.Empty;
                if (string.IsNullOrWhiteSpace(ConfigData.RSSKey.Value))
                    throw new Exception("Failed to get RSS Key");
                SaveConfig();
            }
            catch (Exception e)
            {
                IsConfigured = false;
                throw e;
            }

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

            /************************
             * TODO: Rss grab       *
             *  issue: rss has no   *
             *  seeders values      *
             ************************/
            // Check cache first so we don't query the server (if search term used or not in dev mode)
            if (!DevMode && !string.IsNullOrEmpty(searchTerm))
            {
                lock (cache)
                {
                    // Remove old cache items
                    CleanCache();

                    var cachedResult = cache.Where(i => i.Query == searchTerm).FirstOrDefault();
                    if (cachedResult != null)
                        return cachedResult.Results.Select(s => (ReleaseInfo)s.Clone()).ToArray();
                }
            }

            // Add emulated XHR request
            emulatedBrowserHeaders.Add("X-Requested-With", "XMLHttpRequest");

            // Request our first page
            var page = 0;
            var getPage = true;
            var err = "";
            
            while (getPage && page < 2)
            {
                var results = await RequestStringWithCookiesAndRetry(buildQuery(searchTerm, query, searchUrl, page), null, null, emulatedBrowserHeaders);
                fDom = results.Content;
                getPage = false;
                try
                {
                    var pageLinks = fDom[".pageLinkLabels"];
                    try
                    {
                        var p2 = pageLinks.First().Next().Text();
                        if (p2.StartsWith("36")) getPage = true;
                    }
                    catch (Exception e2)
                    {
                        getPage = false;
                        err = e2.Message;
                    }
                    // Find torrent rows
                    var pageRows = findTorrentRows();
                    output("There are " + pageRows.Length + " results on page " + page);
                    torrentRowList.AddRange(pageRows.Select(fRow => fRow.Cq()));

                    // we are only goign to get the first page of results
                    int nbResults = pageRows.Length;
                } catch (Exception ex)
                {
                    output(ex.Message);
                    page++;
                }
                page++;
            }
            
            try
            {
                // Loop on results
                foreach (CQ tRow in torrentRowList)
                {
                    output("\n=>> Torrent #" + (releases.Count + 1));

                    // Release Name
                    string name = tRow.Find("td:eq(2) a").First().Text().Trim();
                    output("Release: " + name);

                    // Torrent Details URL
                    string details = tRow.Find("td:eq(2) a").Attr("href").ToString().TrimStart('/');
                    Uri detailsLink = new Uri(SiteLink + details);
                    output("Details: " + detailsLink.AbsoluteUri);

                    Uri commentsLink = new Uri(SiteLink + details + "#startcomments");

                    string download = tRow.Find("td:eq(3) a").Attr("href").ToString().TrimStart('/');
                    Uri downloadLink = new Uri(SiteLink + download);
                    output("Download: " + downloadLink.AbsoluteUri);

                    // Category
                    string categoryID = tRow.Find("td:eq(0) a").Attr("href").ToString();
                    int idx = categoryID.LastIndexOf("=") + 1;
                    categoryID = categoryID.Substring(idx).TrimEnd();
                    categoryID = ParseUtil.CoerceInt(categoryID).ToString();
                    output("Category: " + MapTrackerCatToNewznab(categoryID));

                    // Seeders
                    //int seeders = ParseUtil.CoerceInt(Regex.Match(tRow.Find(".seeders")[0].LastChild.ToString(), @"\d+").Value);
                    int seeders = ParseUtil.CoerceInt(tRow.Find("td:eq(6)").Text());
                    output("Seeders: " + seeders);

                    // Leechers
                    int leechers = ParseUtil.CoerceInt(tRow.Find("td:eq(7)").Text());
                    output("Leechers: " + leechers);

                    // Size
                    var sizeStr = tRow.Find("td:eq(5)").Text();
                    output("Size: " + sizeStr);
                    //release.Size = ReleaseInfo.GetBytes(sizeStr);

                    // Size & Publish Date
                    string dateInfo = tRow.Find("td:eq(2) .uploaded").Text();

                    IList<string> infosList = dateInfo.Split('-').Select(s => s.Trim()).Where(s => s != String.Empty).ToList();

                    // --> Publish Date
                    IList<string> clockList = infosList[0].Replace("ago","").Trim().Replace("and", ",").Split(',').Select(s => s.Trim()).Where(s => s != String.Empty).ToList();
                    var clock = agoToDate(clockList);
                    output("Released on: " + clock.ToString());

                    // Building release infos
                    var release = new ReleaseInfo();
                    release.Category = MapTrackerCatToNewznab(categoryID);
                    release.Title = name;
                    release.Seeders = seeders;
                    release.Peers = seeders + leechers;
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 345600;
                    release.PublishDate = clock;
                    release.Size = ReleaseInfo.GetBytes(sizeStr);
                    release.Guid = detailsLink;
                    release.Comments = commentsLink;
                    release.Link = downloadLink;
                    releases.Add(release);
                }
                page++;
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
        
        /// <returns>URL to query for parsing and processing results</returns>
        private string buildQuery(string term, TorznabQuery query, string url, int page = 0)
        {
            var parameters = new NameValueCollection();
            var trackerCats = MapTorznabCapsToTrackers(query);
            var queryCollection = new NameValueCollection();

            // If no search string provided, use empty search string
            if (string.IsNullOrWhiteSpace(term))
            {
                queryCollection.Add("page", page.ToString());
                url += "?" + queryCollection.GetQueryString();
                return url;
            }

            queryCollection.Add("search", term );
            queryCollection.Add("page", page.ToString());
            for (var ct = 0; ct < trackerCats.Count; ct++) queryCollection.Add("c" + trackerCats.ElementAt(ct), "1");
            
            url += "?" + queryCollection.GetQueryString();
                        
            output("Built query for \"" + term + "\"... with " + url);

            // Return our search url
            return url;
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
            return fDom[".torrentsTableTR"];
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
                if (ago.Contains("year"))
                {
                    // Number of years to remove
                    int years = ParseUtil.CoerceInt(Regex.Match(ago.ToString(), @"\d+").Value);
                    // Removing
                    release = release.AddYears(-years);

                    continue;
                }
                // Check for months
                else if (ago.Contains("month"))
                {
                    // Number of months to remove
                    int months = ParseUtil.CoerceInt(Regex.Match(ago.ToString(), @"\d+").Value);
                    // Removing
                    release = release.AddMonths(-months);

                    continue;
                }
                // Check for week
                else if (ago.Contains("week"))
                {
                    // Number of days to remove
                    int days = ParseUtil.CoerceInt(Regex.Match(ago.ToString(), @"\d+").Value)*7;
                    // Removing
                    release = release.AddDays(-days);

                    continue;
                }
                // Check for days
                else if ( ago.Contains("day"))
                {
                    // Number of days to remove
                    int days = ParseUtil.CoerceInt(Regex.Match(ago.ToString(), @"\d+").Value);
                    // Removing
                    release = release.AddDays(-days);

                    continue;
                }
                // Check for hours
                else if (ago.Contains("hour"))
                {
                    // Number of hours to remove
                    int hours = ParseUtil.CoerceInt(Regex.Match(ago.ToString(), @"\d+").Value);
                    // Removing
                    release = release.AddHours(-hours);

                    continue;
                }
                // Check for minutes
                else if (ago.Contains("minute"))
                {
                    // Number of minutes to remove
                    int minutes = ParseUtil.CoerceInt(Regex.Match(ago.ToString(), @"\d+").Value);
                    // Removing
                    release = release.AddMinutes(-minutes);

                    continue;
                }
                // Check for seconds
                else if (ago.Contains("second"))
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
            // Check Username Setting
            if (string.IsNullOrEmpty(ConfigData.Username.Value))
            {
                throw new ExceptionWithConfigData("You must provide a username for this tracker to login !", ConfigData);
            }

            // Check Password Setting
            if (string.IsNullOrEmpty(ConfigData.Password.Value))
            {
                throw new ExceptionWithConfigData("You must provide a password with your username for this tracker to login !", ConfigData);
            }

            // Check Browser Setting
            if (ConfigData.Browser.Value)
            {
                // Check ACCEPT header Setting
                if (string.IsNullOrEmpty(ConfigData.HeaderAccept.Value))
                {
                    throw new ExceptionWithConfigData("Browser Simulation enabled, Please enter an ACCEPT header !", ConfigData);
                }

                // Check ACCEPT-LANG header Setting
                if (string.IsNullOrEmpty(ConfigData.HeaderAcceptLang.Value))
                {
                    throw new ExceptionWithConfigData("Browser Simulation enabled, Please enter an ACCEPT-LANG header !", ConfigData);
                }

                // Check USER-AGENT header Setting
                if (string.IsNullOrEmpty(ConfigData.HeaderUserAgent.Value))
                {
                    throw new ExceptionWithConfigData("Browser Simulation enabled, Please enter an USER-AGENT header !", ConfigData);
                }
            }
        }
    }
}