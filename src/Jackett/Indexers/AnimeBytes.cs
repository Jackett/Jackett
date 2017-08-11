using CsQuery;
using Jackett.Models;
using Jackett.Models.IndexerConfig;
using Jackett.Models.IndexerConfig.Bespoke;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Concurrent;
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
    public class AnimeBytes : BaseCachingWebIndexer
    {
        enum SearchType
        {
            Video,
            Audio
        }

        private string LoginUrl { get { return SiteLink + "user/login"; } }
        private string SearchUrl { get { return SiteLink + "torrents.php?"; } }
        private string MusicSearchUrl { get { return SiteLink + "torrents2.php?"; } }
        public bool AllowRaws { get { return configData.IncludeRaw.Value; } }
        public bool InsertSeason { get { return configData.InsertSeason != null && configData.InsertSeason.Value; } }

        new ConfigurationDataAnimeBytes configData
        {
            get { return (ConfigurationDataAnimeBytes)base.configData; }
            set { base.configData = value; }
        }

        public AnimeBytes(IIndexerConfigurationService configService, IWebClient client, Logger l, IProtectionService ps)
            : base(name: "AnimeBytes",
                link: "https://animebytes.tv/",
                description: "Powered by Tentacles",
                configService: configService,
                client: client,
                caps: new TorznabCapabilities(TorznabCatType.TVAnime,
                                              TorznabCatType.Movies,
                                              TorznabCatType.BooksComics,
                                              TorznabCatType.ConsolePSP,
                                              TorznabCatType.ConsoleOther,
                                              TorznabCatType.PCGames,
                                              TorznabCatType.AudioMP3,
                                              TorznabCatType.AudioLossless,
                                              TorznabCatType.AudioOther),
                logger: l,
                p: ps,
                configData: new ConfigurationDataAnimeBytes())
        {
            Encoding = Encoding.GetEncoding("UTF-8");
            Language = "en-us";
            Type = "private";

            webclient.EmulateBrowser = false; // Animebytes doesn't like fake user agents (issue #1535)
        }


        protected override IEnumerable<ReleaseInfo> FilterResults(TorznabQuery query, IEnumerable<ReleaseInfo> input)
        {
            // Prevent filtering
            return input;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            lock (cache)
            {
                cache.Clear();
            }

            // Get the login form as we need the CSRF Token
            var loginPage = await webclient.GetString(new Utils.Clients.WebRequest()
            {
                Url = LoginUrl,
                Encoding = Encoding,
            });

            CQ loginPageDom = loginPage.Content;
            var csrfIndex = loginPageDom["input[name=\"_CSRF_INDEX\"]"].Last();
            var csrfToken = loginPageDom["input[name=\"_CSRF_TOKEN\"]"].Last();

            // Build login form
            var pairs = new Dictionary<string, string> {
                    { "_CSRF_INDEX", csrfIndex.Attr("value") },
                    { "_CSRF_TOKEN", csrfToken.Attr("value") },
                    { "username", configData.Username.Value },
                    { "password", configData.Password.Value },
                    { "keeplogged_sent", "true" },
                    { "keeplogged", "on" },
                    { "login", "Log In!" }
            };

            // Do the login
            var request = new Utils.Clients.WebRequest()
            {
                Cookies = loginPage.Cookies,
                PostData = pairs,
                Referer = LoginUrl,
                Type = RequestType.POST,
                Encoding = Encoding,
                Url = LoginUrl,
            };
            var response = await RequestLoginAndFollowRedirect(LoginUrl, pairs, loginPage.Cookies, true, null);

            // Follow the redirect
            await FollowIfRedirect(response, request.Url, SearchUrl);

            await ConfigureIfOK(response.Cookies, response.Content != null && response.Content.Contains("/user/logout"), () =>
            {
                // Their login page appears to be broken and just gives a 500 error.
                throw new ExceptionWithConfigData("Failed to login, 6 failed attempts will get you banned for 6 hours.", configData);
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        private string StripEpisodeNumber(string term)
        {
            // Tracer does not support searching with episode number so strip it if we have one
            term = Regex.Replace(term, @"\W(\dx)?\d?\d$", string.Empty);
            term = Regex.Replace(term, @"\W(S\d\d?E)?\d?\d$", string.Empty);
            return term;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            // The result list
            var releases = new List<ReleaseInfo>();

            if (ContainsMusicCategories(query.Categories))
            {
                foreach (var result in await GetResults(SearchType.Audio, query.SanitizedSearchTerm))
                {
                    releases.Add(result);
                }
            }

            foreach (var result in await GetResults(SearchType.Video, StripEpisodeNumber(query.SanitizedSearchTerm)))
            {
                releases.Add(result);
            }

            return releases.ToArray();
        }

        private bool ContainsMusicCategories(int[] categories)
        {
            var music = new[]
            {
                TorznabCatType.Audio.ID,
                TorznabCatType.AudioMP3.ID,
                TorznabCatType.AudioLossless.ID,
                TorznabCatType.AudioOther.ID,
                TorznabCatType.AudioForeign.ID
            };

            return categories.Length == 0 || music.Any(categories.Contains);
        }

        private async Task<IEnumerable<ReleaseInfo>> GetResults(SearchType searchType, string searchTerm)
        {
            var cleanSearchTerm = HttpUtility.UrlEncode(searchTerm);

            // The result list
            var releases = new List<ReleaseInfo>();

            var queryUrl = searchType == SearchType.Video ? SearchUrl : MusicSearchUrl;
            // Only include the query bit if its required as hopefully the site caches the non query page
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                queryUrl += string.Format("searchstr={0}&action=advanced&search_type=title&year=&year2=&tags=&tags_type=0&sort=time_added&way=desc&hentai=2&releasegroup=&epcount=&epcount2=&artbooktitle=", cleanSearchTerm);
            }

            // Check cache first so we don't query the server for each episode when searching for each episode in a series.
            lock (cache)
            {
                // Remove old cache items
                CleanCache();

                var cachedResult = cache.Where(i => i.Query == queryUrl).FirstOrDefault();
                if (cachedResult != null)
                    return cachedResult.Results.Select(s => (ReleaseInfo)s.Clone()).ToArray();
            }

            // Get the content from the tracker
            var response = await RequestStringWithCookiesAndRetry(queryUrl);
            if (response.IsRedirect)
            {
                // re-login
                await ApplyConfiguration(null);
                response = await RequestStringWithCookiesAndRetry(queryUrl);
            }

            CQ dom = response.Content;

            // Parse
            try
            {
                var releaseInfo = "S01";
                var root = dom.Find(".group_cont");
                // We may have got redirected to the series page if we have none of these
                if (root.Count() == 0)
                    root = dom.Find(".torrent_table");

                foreach (var series in root)
                {
                    var seriesCq = series.Cq();

                    var synonyms = new List<string>();
                    string mainTitle;
                    if (searchType == SearchType.Video)
                        mainTitle = seriesCq.Find(".group_title strong a").First().Text().Trim();
                    else
                        mainTitle = seriesCq.Find(".group_title strong").Text().Trim();

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
                        for (int r = 1; r < releaseRows.Count(); r++)
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
                                int test = 0;
                                if (InsertSeason && int.TryParse(releaseInfo, out test) && releaseInfo.Length <= 3)
                                {
                                    releaseInfo = "E0" + releaseInfo;
                                }

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

                                // We dont know this so try to fake based on the release year
                                release.PublishDate = new DateTime(year, 1, 1);
                                release.PublishDate = release.PublishDate.AddDays(Math.Min(DateTime.Now.DayOfYear, 365) - 1);

                                var infoLink = links.Get(1);
                                release.Comments = new Uri(SiteLink + infoLink.Attributes.GetAttribute("href"));
                                release.Guid = new Uri(SiteLink + infoLink.Attributes.GetAttribute("href") + "&nh=" + StringUtil.Hash(title)); // Sonarr should dedupe on this url - allow a url per name.
                                release.Link = new Uri(downloadLink.Attributes.GetAttribute("href"));

                                string category = null;
                                if (searchType == SearchType.Video)
                                {
                                    category = seriesCq.Find("a[title=\"View Torrent\"]").Text().Trim();
                                    if (category == "TV Series")
                                        release.Category = new List<int> { TorznabCatType.TVAnime.ID };

                                    // Ignore these categories as they'll cause hell with the matcher
                                    // TV Special, OVA, ONA, DVD Special, BD Special

                                    if (category == "Movie")
                                        release.Category = new List<int> { TorznabCatType.Movies.ID };

                                    if (category == "Manga" || category == "Oneshot" || category == "Anthology" || category == "Manhwa" || category == "Manhua" || category == "Light Novel")
                                        release.Category = new List<int> { TorznabCatType.BooksComics.ID };

                                    if (category == "Novel" || category == "Artbook")
                                        release.Category = new List<int> { TorznabCatType.BooksComics.ID };

                                    if (category == "Game" || category == "Visual Novel")
                                    {
                                        var description = rowCq.Find(".torrent_properties a:eq(1)").Text();
                                        if (description.Contains(" PSP "))
                                            release.Category = new List<int> { TorznabCatType.ConsolePSP.ID };
                                        if (description.Contains("PSX"))
                                            release.Category = new List<int> { TorznabCatType.ConsoleOther.ID };
                                        if (description.Contains(" NES "))
                                            release.Category = new List<int> { TorznabCatType.ConsoleOther.ID };
                                        if (description.Contains(" PC "))
                                            release.Category = new List<int> { TorznabCatType.PCGames.ID };
                                    }
                                }

                                if (searchType == SearchType.Audio)
                                {
                                    category = seriesCq.Find(".group_img .cat a").Text();
                                    if (category == "Single" || category == "Album" || category == "Compilation" || category == "Soundtrack" || category == "Remix CD")
                                    {
                                        var description = rowCq.Find(".torrent_properties a:eq(1)").Text();
                                        if (description.Contains(" Lossless "))
                                            release.Category = new List<int> { TorznabCatType.AudioLossless.ID };
                                        else if (description.Contains("MP3"))
                                            release.Category = new List<int> { TorznabCatType.AudioMP3.ID };
                                        else
                                            release.Category = new List<int> { TorznabCatType.AudioOther.ID };
                                    }
                                }



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
                                    if (releaseTags[i] == "Raw" && !AllowRaws)
                                        continue;
                                    infoString += "[" + releaseTags[i] + "]";
                                }

                                if (category == "Movie")
                                {
                                    release.Title = string.Format("{0} {1} {2}{3}", title, year, group, infoString);
                                }
                                else
                                {
                                    release.Title = string.Format("{0}{1} {2} {3}", group, title, releaseInfo, infoString);
                                }
                                release.Description = title;

                                var size = rowCq.Find(".torrent_size");
                                if (size.Count() > 0)
                                {
                                    release.Size = ReleaseInfo.GetBytes(size.First().Text());
                                }

                                //  Additional 5 hours per GB 
                                release.MinimumSeedTime += (release.Size / 1000000000) * 18000;

                                // Peer info
                                release.Seeders = ParseUtil.CoerceInt(rowCq.Find(".torrent_seeders").Text());
                                release.Peers = release.Seeders + ParseUtil.CoerceInt(rowCq.Find(".torrent_leechers").Text());

                                // grabs
                                var grabs = rowCq.Find("td.torrent_snatched").Text();
                                release.Grabs = ParseUtil.CoerceInt(grabs);

                                // freeleech
                                if (rowCq.Find("img[alt=\"Freeleech!\"]").Length >= 1)
                                    release.DownloadVolumeFactor = 0;
                                else
                                    release.DownloadVolumeFactor = 1;
                                release.UploadVolumeFactor = 1;

                                if (release.Category != null)
                                    releases.Add(release);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.Content, ex);
            }

            // Add to the cache
            lock (cache)
            {
                cache.Add(new CachedQueryResult(queryUrl, releases));
            }

            return releases.Select(s => (ReleaseInfo)s.Clone());
        }
    }
}
