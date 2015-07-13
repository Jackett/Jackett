using CsQuery;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Jackett.Indexers
{
    public class AnimeBytes : IndexerInterface
    {
        class ConfigurationDataBasicLoginAnimeBytes : ConfigurationDataBasicLogin
        {
            public BoolItem IncludeRaw { get; private set; }
            public DisplayItem RageIdWarning { get; private set; }
            public DisplayItem DateWarning { get; private set; }

            public ConfigurationDataBasicLoginAnimeBytes()
                : base()
            {
                IncludeRaw = new BoolItem() { Name = "IncludeRaw", Value = false };
                RageIdWarning = new DisplayItem("Ensure rageid lookup is disabled in Sonarr for this tracker.") { Name = "RageWarning" };
                DateWarning = new DisplayItem("This tracker does not supply upload dates so they are based off year of release.") { Name = "DateWarning" };
            }

            public override Item[] GetItems()
            {
                return new Item[] { Username, Password, IncludeRaw, RageIdWarning, DateWarning };
            }
        }

        private static List<CachedResult> cache = new List<CachedResult>();
        private static readonly TimeSpan cacheTime = new TimeSpan(0, 9, 0);

        public event Action<IndexerInterface, string, Exception> OnResultParsingError;
        public event Action<IndexerInterface, JToken> OnSaveConfigurationRequested;

        static string chromeUserAgent = BrowserUtil.ChromeUserAgent;

        public string DisplayName
        {
            get { return "AnimeBytes"; }
        }

        public string DisplayDescription
        {
            get { return "The web's best Chinese cartoons"; }
        }

        public Uri SiteLink
        {
            get { return new Uri(BaseUrl); }
        }

        const string BaseUrl = "https://animebytes.tv";
        const string LoginUrl = BaseUrl + "/user/login";
        const string SearchUrl = BaseUrl + "/torrents.php?filter_cat[1]=1";

        public bool IsConfigured { get; private set; }
        public bool AllowRaws { get; private set; }


        CookieContainer cookieContainer;
        HttpClientHandler handler;
        HttpClient client;

        public AnimeBytes()
        {
            IsConfigured = false;
            cookieContainer = new CookieContainer();
            handler = new HttpClientHandler
            {
                CookieContainer = cookieContainer,
                AllowAutoRedirect = false,
                UseCookies = true,
            };
            client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("User-Agent", chromeUserAgent);
        }

        public Task<ConfigurationData> GetConfigurationForSetup()
        {
            var config = new ConfigurationDataBasicLoginAnimeBytes();
            return Task.FromResult<ConfigurationData>(config);
        }

        public async Task ApplyConfiguration(JToken configJson)
        {
            var config = new ConfigurationDataBasicLoginAnimeBytes();
            config.LoadValuesFromJson(configJson);


            // Get the login form as we need the CSRF Token
            var loginPage = await client.GetAsync(LoginUrl);
            CQ loginPageDom = await loginPage.Content.ReadAsStringAsync();
            var csrfToken = loginPageDom["input[name=\"csrf_token\"]"].Last();

            // Build login form
            var pairs = new Dictionary<string, string> {
                  { "csrf_token", csrfToken.Attr("value") },
				{ "username", config.Username.Value },
				{ "password", config.Password.Value },
                { "keeplogged_sent", "true" },
                { "keeplogged", "on" },
                { "login", "Log In!" }
			};

            var content = new FormUrlEncodedContent(pairs);

            // Do the login
            var response = await client.PostAsync(LoginUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Compatiblity issue between the cookie format and httpclient
            // Pull it out manually ignoring the expiry date then set it manually
            // http://stackoverflow.com/questions/14681144/httpclient-not-storing-cookies-in-cookiecontainer
            IEnumerable<string> cookies;
            if (response.Headers.TryGetValues("set-cookie", out cookies))
            {
                foreach (var c in cookies)
                {
                    cookieContainer.SetCookies(new Uri(BaseUrl), c.Substring(0, c.LastIndexOf(';')));
                }
            }

            foreach (Cookie cookie in cookieContainer.GetCookies(new Uri(BaseUrl)))
            {
                if (cookie.Name == "session")
                {
                    cookie.Expires = DateTime.Now.AddDays(360);
                    break;
                }
            }

            // Get the home page now we are logged in as AllowAutoRedirect is false as we needed to get the cookie manually.
            response = await client.GetAsync(BaseUrl);
            responseContent = await response.Content.ReadAsStringAsync();

            if (!responseContent.Contains("/user/logout"))
            {
                throw new ExceptionWithConfigData("Failed to login, 6 failed attempts will get you banned for 6 hours.", (ConfigurationData)config);
            }
            else
            {
                AllowRaws = config.IncludeRaw.Value;
                var configSaveData = new JObject();
                cookieContainer.DumpToJson(SiteLink, configSaveData);
                configSaveData["raws"] = AllowRaws;

                if (OnSaveConfigurationRequested != null)
                    OnSaveConfigurationRequested(this, configSaveData);

                IsConfigured = true;
            }
        }

        public void LoadFromSavedConfiguration(JToken jsonConfig)
        {
            cookieContainer.FillFromJson(new Uri(BaseUrl), jsonConfig);
            IsConfigured = true;
            AllowRaws = jsonConfig["raws"].Value<bool>();
        }


        private string Hash(string input)
        {
            // Use input string to calculate MD5 hash
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            // Convert the byte array to hexadecimal string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }
            return sb.ToString();
        }

        private void CleanCache()
        {
            foreach (var expired in cache.Where(i => i.Created - DateTime.Now > cacheTime).ToList())
            {
                cache.Remove(expired);
            }
        }


        public async Task<ReleaseInfo[]> PerformQuery(TorznabQuery query)
        {
            // This tracker only deals with full seasons so chop off the episode/season number if we have it D:
            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var splitindex = query.SearchTerm.LastIndexOf(' ');
                if (splitindex > -1)
                    query.SearchTerm = query.SearchTerm.Substring(0, splitindex);
            }

            // The result list
            var releases = new List<ReleaseInfo>();

            // Check cache first so we don't query the server for each episode when searching for each episode in a series.
            lock (cache)
            {
                // Remove old cache items
                CleanCache();

                var cachedResult = cache.Where(i => i.Query == query.SearchTerm).FirstOrDefault();
                if (cachedResult != null)
                    return cachedResult.Results.Select(s => (ReleaseInfo)s.Clone()).ToArray();
            }

            var queryUrl = SearchUrl;
            // Only include the query bit if its required as hopefully the site caches the non query page
            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {

                queryUrl += "&action=advanced&search_type=title&sort=time_added&way=desc&anime%5Btv_series%5D=1&searchstr=" + WebUtility.UrlEncode(query.SearchTerm);
            }

            // Get the content from the tracker
            var response = await client.GetAsync(queryUrl);
            var responseContent = await response.Content.ReadAsStringAsync();
            CQ dom = responseContent;

            // Parse
            try
            {
                var releaseInfo = "S01";
                var root = dom.Find(".anime");
                // We may have got redirected to the series page if we have none of these
                if (root.Count() == 0)
                    root = dom.Find(".torrent_table");

                foreach (var series in root)
                {
                    var seriesCq = series.Cq();

                    var synonyms = new List<string>();
                    var mainTitle = seriesCq.Find(".group_title strong a").First().Text().Trim();

                    var yearStr = seriesCq.Find(".group_title strong").First().Text().Trim().Replace("]", "").Trim();
                    int yearIndex = yearStr.LastIndexOf("[");
                    if (yearIndex > -1)
                        yearStr = yearStr.Substring(yearIndex + 1);

                    int year = 0;
                    if (!int.TryParse(yearStr, out year))
                        year = DateTime.Now.Year;

                    synonyms.Add(mainTitle);

                    // If the title contains a comma then we can't use the synonyms as they are comma seperated
                    if (!mainTitle.Contains(","))
                    {
                        var symnomnNames = string.Empty;
                        foreach (var e in seriesCq.Find(".group_statbox li"))
                        {
                            if (e.FirstChild.InnerText == "Synonyms:")
                            {
                                symnomnNames = e.InnerText;
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(symnomnNames))
                        {
                            foreach (var name in symnomnNames.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
                            {
                                var theName = name.Trim();
                                if (!theName.Contains("&#") && !string.IsNullOrWhiteSpace(theName))
                                {
                                    synonyms.Add(theName);
                                }
                            }
                        }
                    }

                    foreach (var title in synonyms)
                    {
                        var releaseRows = seriesCq.Find(".torrent_group tr");

                        // Skip the first two info rows
                        for (int r = 2; r < releaseRows.Count(); r++)
                        {
                            var row = releaseRows.Get(r);
                            var rowCq = row.Cq();
                            if (rowCq.HasClass("edition_info"))
                            {
                                releaseInfo = rowCq.Find("td").Text();

                                if (string.IsNullOrWhiteSpace(releaseInfo))
                                {
                                    // Single episodes alpha - Reported that this info is missing.
                                    // It should self correct when availible
                                    break;
                                }

                                releaseInfo = releaseInfo.Replace("Episode ", "");
                                releaseInfo = releaseInfo.Replace("Season ", "S");
                                releaseInfo = releaseInfo.Trim();
                            }
                            else if (rowCq.HasClass("torrent"))
                            {
                                var links = rowCq.Find("a");
                                // Protect against format changes
                                if (links.Count() != 2)
                                {
                                    continue;
                                }

                                var release = new ReleaseInfo();
                                release.MinimumRatio = 1;
                                release.MinimumSeedTime = 259200;
                                var downloadLink = links.Get(0);
                                release.Guid = new Uri(BaseUrl + "/" + downloadLink.Attributes.GetAttribute("href") + "&nh=" + Hash(title)); // Sonarr should dedupe on this url - allow a url per name.
                                release.Link = release.Guid;// We dont know this so try to fake based on the release year
                                release.PublishDate = new DateTime(year, 1, 1);
                                release.PublishDate = release.PublishDate.AddDays(Math.Min(DateTime.Now.DayOfYear, 365) - 1);

                                var infoLink = links.Get(1);
                                release.Comments = new Uri(BaseUrl + "/" + infoLink.Attributes.GetAttribute("href"));

                                // We dont actually have a release name >.> so try to create one
                                var releaseTags = infoLink.InnerText.Split("|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
                                for (int i = releaseTags.Count - 1; i >= 0; i--)
                                {
                                    releaseTags[i] = releaseTags[i].Trim();
                                    if (string.IsNullOrWhiteSpace(releaseTags[i]))
                                        releaseTags.RemoveAt(i);
                                }

                                var group = releaseTags.Last();
                                if (group.Contains("(") && group.Contains(")"))
                                {
                                    // Skip raws if set
                                    if (group.ToLowerInvariant().StartsWith("raw") && !AllowRaws)
                                    {
                                        continue;
                                    }

                                    var start = group.IndexOf("(");
                                    group = "[" + group.Substring(start + 1, (group.IndexOf(")") - 1) - start) + "] ";
                                }
                                else
                                {
                                    group = string.Empty;
                                }

                                var infoString = "";

                                for (int i = 0; i + 1 < releaseTags.Count(); i++)
                                {
                                    infoString += "[" + releaseTags[i] + "]";
                                }

                                release.Title = string.Format("{0}{1} {2} {3}", group, title, releaseInfo, infoString);
                                release.Description = title;

                                var size = rowCq.Find(".torrent_size");
                                if (size.Count() > 0)
                                {
                                    var sizeParts = size.First().Text().Split(' ');
                                    release.Size = ReleaseInfo.GetBytes(sizeParts[1], ParseUtil.CoerceFloat(sizeParts[0]));
                                }

                                //  Additional 5 hours per GB 
                                release.MinimumSeedTime += (release.Size / 1000000000) * 18000;

                                // Peer info
                                release.Seeders = ParseUtil.CoerceInt(rowCq.Find(".torrent_seeders").Text());
                                release.Peers = release.Seeders + ParseUtil.CoerceInt(rowCq.Find(".torrent_leechers").Text());

                                releases.Add(release);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnResultParsingError(this, responseContent, ex);
                throw ex;
            }


            // Add to the cache
            lock (cache)
            {
                cache.Add(new CachedResult(query.SearchTerm, releases));
            }

            return releases.Select(s => (ReleaseInfo)s.Clone()).ToArray();
        }

        public Task<byte[]> Download(Uri link)
        {
            return client.GetByteArrayAsync(link);
        }
    }
}
