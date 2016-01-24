using CsQuery;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Jackett.Models.IndexerConfig;
using System.Collections.Specialized;

namespace Jackett.Indexers
{
    public class SpeedCD : BaseIndexer, IIndexer
    {
        private string LoginUrl { get { return SiteLink + "take.login.php"; } }
        private string SearchUrl { get { return SiteLink + "browse.php?s=4&t=2&"; } }
        private string CommentsUrl { get { return SiteLink + "t/{0}"; } }
        private string DownloadUrl { get { return SiteLink + "download.php?torrent={0}"; } }

        new ConfigurationDataBasicLogin configData
        {
            get { return (ConfigurationDataBasicLogin)base.configData; }
            set { base.configData = value; }
        }

        public SpeedCD(IIndexerManagerService i, Logger l, IWebClient wc, IProtectionService ps)
            : base(name: "Speed.cd",
                description: "Your home now!",
                link: "https://speed.cd/",
                caps: new TorznabCapabilities(),
                manager: i,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLogin())
        {
            AddCategoryMapping("1", TorznabCatType.MoviesOther);
            AddCategoryMapping("42", TorznabCatType.Movies);
            AddCategoryMapping("32", TorznabCatType.Movies);
            AddCategoryMapping("43", TorznabCatType.MoviesHD);
            AddCategoryMapping("47", TorznabCatType.Movies);
            AddCategoryMapping("28", TorznabCatType.MoviesBluRay);
            AddCategoryMapping("48", TorznabCatType.Movies3D);
            AddCategoryMapping("40", TorznabCatType.MoviesDVD);
            AddCategoryMapping("49", TorznabCatType.TVHD);
            AddCategoryMapping("50", TorznabCatType.TVSport);
            AddCategoryMapping("52", TorznabCatType.TVHD);
            AddCategoryMapping("53", TorznabCatType.TVSD);
            AddCategoryMapping("41", TorznabCatType.TV);
            AddCategoryMapping("55", TorznabCatType.TV);
            AddCategoryMapping("2", TorznabCatType.TV);
            AddCategoryMapping("30", TorznabCatType.TVAnime);
            AddCategoryMapping("25", TorznabCatType.PCISO);
            AddCategoryMapping("39", TorznabCatType.ConsoleWii);
            AddCategoryMapping("45", TorznabCatType.ConsolePS3);
            AddCategoryMapping("35", TorznabCatType.Console);
            AddCategoryMapping("33", TorznabCatType.ConsoleXbox360);
            AddCategoryMapping("46", TorznabCatType.PCPhoneOther);
            AddCategoryMapping("24", TorznabCatType.PC0day);
            AddCategoryMapping("51", TorznabCatType.PCMac);
            AddCategoryMapping("27", TorznabCatType.Books);
            AddCategoryMapping("26", TorznabCatType.Audio);
            AddCategoryMapping("44", TorznabCatType.Audio);
            AddCategoryMapping("29", TorznabCatType.AudioVideo);
        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, SiteLink);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("logout.php"), () =>
            {
                CQ dom = result.Content;
                var errorMessage = dom["h5"].First().Text().Trim();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>(); // Create new variable to hold release info list
            var releasesAppend = new List<ReleaseInfo>(); // Create new list to hold release info to be appended
            var searchString = query.GetQueryString(); // Get the query string
            var searchUrl = SearchUrl; // Get the search url
            var qParams = new NameValueCollection(); // Greate a new variable to hold query string parameters
            int i = 1; // Creaate new variable to control loop limit
            CQ csquery; // Create new csquery object for html selections

            // If exists then add the search parameter to the query string parameters
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                qParams.Add("search", searchString);
            }

            // Add category parameters to the query string parameters
            foreach (var cat in MapTorznabCapsToTrackers(query))
            {
                qParams.Add("c" + cat, "1");
            }

            // Append the query string parameters to the search url
            if (qParams.Count > 0)
            {
                searchUrl += qParams.GetQueryString();
            }

            // Begin loop (if looped 10 times (10 page requests, each containing 25 torrents) or torrent with zero seeds is found then exist loop)
            while (i <= 10 && !releases.Exists(x => x.Seeders == 0))
            {
                // Execute web request to Speed.cd
                var response = await RequestStringWithCookiesAndRetry(searchUrl + "&p=" + i);

                try
                {
                    // Populate the csquery wiht the web request response content
                    csquery = response.Content;
                    // Query the cs object for the rows of the torrent table
                    var rows = csquery["#torrentTable > div > div.boxContent > table > tbody > tr"];

                    // Populate append variable with release info to append
                    releasesAppend = rows.Select(r => {
                        try {
                            string id = r.Attributes["id"].Remove(0, 2); // Get ID
                            int seeders = 0; int.TryParse(r.ChildNodes[5].Cq().Text(), out seeders); // Get seeders
                            int leechers = 0; int.TryParse(r.ChildNodes[6].Cq().Text(), out leechers); // Get leechers
                            long category; long.TryParse(csquery["a.cat", r].Attr("id").Trim(), out category); // Get category code

                            // Create release info
                            ReleaseInfo info = new ReleaseInfo()
                            {
                                Guid = new Uri(string.Format(CommentsUrl, id)),
                                Title = csquery["a.torrent", r].Text(),
                                Description = csquery["a.torrent", r].Text(),
                                Comments = new Uri(string.Format(CommentsUrl, id)),
                                Link = new Uri(string.Format(DownloadUrl, id)),
                                PublishDate = DateTime.ParseExact(csquery["span.elapsedDate", r].FirstOrDefault().Attributes["title"].Replace(" at", ""), "dddd, MMMM d, yyyy h:mmtt", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToLocalTime(),
                                Size = ReleaseInfo.GetBytes(r.ChildNodes[4].InnerText),
                                Seeders = seeders,
                                Peers = seeders + leechers,
                                Category = MapTrackerCatToNewznab(category.ToString()),
                                MinimumSeedTime = 172800,
                                MinimumRatio = 1
                            };

                            // Return release info
                            return info;
                        } catch(Exception ex) {
                            // Log error if individual row parsing fails
                            logger.Error("Speed.cd row parsing error", ex, new { searchUrl, r.OuterHTML });
                            return null;
                        }
                    })
                    .Where(x => x != null) // Ensure that failed rows are not appended
                    .ToList<ReleaseInfo>(); // Convert results to list of release info

                    // Add the rows to the append to the releases list
                    releases.AddRange(releasesAppend);

                    // Increment to next page
                    i++;

                }
                catch (Exception ex)
                {
                    // Log parser error is entire routine fails
                    OnParseError(response.Content, ex);
                }

            }

            // Return the releases
            return releases;
        }
    }
}
