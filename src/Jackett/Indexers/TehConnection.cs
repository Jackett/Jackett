using CsQuery;
using Jackett.Indexers;
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
using System.Web;
using Jackett.Models.IndexerConfig;

namespace Jackett.Indexers
{
    public class TehConnection : BaseIndexer, IIndexer
    {
        private string LoginUrl { get { return SiteLink + "login.php"; } }
        private string indexUrl { get { return SiteLink + "index.php"; } }
        private string SearchUrl { get { return SiteLink + "torrents.php"; } }
        
        new ConfigurationDataBasicLoginWithFilter configData
        {
            get { return (ConfigurationDataBasicLoginWithFilter)base.configData; }
            set { base.configData = value; }
        }

        public TehConnection(IIndexerManagerService i, Logger l, IWebClient c, IProtectionService ps)
            : base(name: "TehConnection",
                description: "Working towards providing a well-seeded archive of all available digital forms of cinema and film in their highest possible quality",
                link: "https://tehconnection.eu/",
                caps: new TorznabCapabilities(),
                manager: i,
                client: c,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLoginWithFilter(@"Enter filter options below to restrict search results. 
                                                                        Separate options with a space if using more than one option.<br>Filter options available:
                                                                        <br><code>QualityEncodeOnly</code><br><code>FreeLeechOnly</code>"))
        {
            AddCategoryMapping(7, TorznabCatType.Movies);
            AddCategoryMapping(7, TorznabCatType.MoviesForeign);
            AddCategoryMapping(7, TorznabCatType.MoviesOther);
            AddCategoryMapping(7, TorznabCatType.MoviesSD);
            AddCategoryMapping(7, TorznabCatType.MoviesHD);
            AddCategoryMapping(7, TorznabCatType.Movies3D);
            AddCategoryMapping(7, TorznabCatType.MoviesBluRay);
            AddCategoryMapping(7, TorznabCatType.MoviesDVD);
            AddCategoryMapping(7, TorznabCatType.MoviesWEBDL);
        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "keeplogged", "1" },
                { "login", "Log In!" }
            };
         
            var response = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, indexUrl, SiteLink);

            await ConfigureIfOK(response.Cookies, response.Content != null && response.Content.Contains("/logout.php"), () =>
            {
                CQ dom = response.Content;
                string errorMessage = "Unable to login to TehConnection";
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            bool configFreeLeechOnly = configData.FilterString.Value.ToLowerInvariant().Contains("freeleechonly");
            bool configQualityEncodeOnly = configData.FilterString.Value.ToLowerInvariant().Contains("qualityencodeonly");
            string movieListSearchUrl;

            if (string.IsNullOrEmpty(query.GetQueryString()))
                movieListSearchUrl = SearchUrl;
            else
            {
                movieListSearchUrl = string.Format("{0}?action=basic&searchstr={1}", SearchUrl, HttpUtility.UrlEncode(query.GetQueryString()));
            }

            var results = await RequestStringWithCookiesAndRetry(movieListSearchUrl);
            try
            {
                CQ mdom = results.Content;

                var mrows = mdom[".torrent_title_box"];
                foreach (var mrow in mrows.Take(5))
                {
                    var mqRow = mrow.Cq();

                    Uri movieReleasesLink = new Uri(SiteLink.TrimEnd('/') + mqRow.Find("a[title='View Torrent']").First().Attr("href").Trim());
                    bool commentsPresent = !mqRow.Find("font[class='tags'] > strong > a:contains('Comment')").First().Text().Trim().Contains("0 Comments");
                    Uri commentsLink = new Uri(SiteLink.TrimEnd('/') + mqRow.Find("font[class='tags'] > strong > a:contains('Comment')").First().Attr("href").Trim());

                    string imdblink = mqRow.Find("span[class='imdb-number-rating']").Length > 0 ? mqRow.Find("span[class='imdb-number-rating'] > a").First().Attr("href").Trim() : "";
                    long imdb_id = 0;
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(imdblink) && imdblink.ToLowerInvariant().Contains("tt"))
                        {
                            imdb_id = long.Parse(imdblink.Substring(imdblink.LastIndexOf('t') + 1).Replace("/", ""));
                        }
                    }
                    catch { imdb_id = 0; }

                    var release_results = await RequestStringWithCookiesAndRetry(movieReleasesLink.ToString());

                    //Iterate over the releases for each movie

                    CQ dom = release_results.Content;

                    var rows = dom[".torrent_widget.box.pad"];
                    foreach (var row in rows)
                    {
                        var qRow = row.Cq();
                        
                        string title = qRow.Find("[id^=desc_] > h2 > strong").First().Text().Trim();
                        Uri link = new Uri(SiteLink.TrimEnd('/') + qRow.Find("a[title='Download']").First().Attr("href").Trim());
                        Uri guid = new Uri(SiteLink.TrimEnd('/') + qRow.Find("a[title='Permalink']").First().Attr("href").Trim());
                        string pubDateStr = qRow.Find("div[class='box pad'] > p:contains('Uploaded by') > span").First().Attr("title").Trim();
                        DateTime pubDate = DateTime.ParseExact(pubDateStr, "MMM dd yyyy, HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToLocalTime();
                        string sizeStr = qRow.Find("[id^=desc_] > div > table > tbody > tr > td > strong:contains('Size:')").First().Parent().Parent().Find("td").Last().Text().Trim();
                        Int32 seeders = Convert.ToInt32(qRow.Find("img[title='Seeders']").First().Parent().Text().Trim());
                        Int32 peers = Convert.ToInt32(qRow.Find("img[title='Leechers']").First().Parent().Text().Trim()) + seeders;
                        Uri CoverUrl = new Uri(SiteLink.TrimEnd('/') + dom.Find("div[id='poster'] > a > img").First().Attr("src").Trim());
                        bool freeleech = qRow.Find("span[class='freeleech']").Length == 1 ? true : false;
                        bool qualityEncode = qRow.Find("img[class='approved']").Length == 1 ? true : false;
                        string grabs = qRow.Find("img[title='Snatches']").First().Parent().Text().Trim();
                        if (String.IsNullOrWhiteSpace(sizeStr))
                        {
                            string secondSizeStr = qRow.Find("div[class='details_title'] > strong:contains('(')").Last().Text().Trim();
                            if (secondSizeStr.Length > 3 && secondSizeStr.Contains("(") && secondSizeStr.Contains(")"))
                            { sizeStr = secondSizeStr.Replace("(", "").Replace(")", "").Trim(); }
                        }
                        
                        var release = new ReleaseInfo();

                        release.Title = title;
                        release.Guid = guid;
                        release.Link = link;
                        release.PublishDate = pubDate;
                        release.Size = ReleaseInfo.GetBytes(sizeStr);
                        release.Description = release.Title;
                        release.Seeders = seeders;
                        release.Peers = peers;
                        release.MinimumRatio = 1;
                        release.MinimumSeedTime = 345600;
                        release.Category = 2000;
                        if (commentsPresent) { release.Comments = commentsLink; }
                        if (imdb_id > 0) { release.Imdb = imdb_id; }

                        if (configFreeLeechOnly && !freeleech)
                        {
                            continue; //Skip release if user only wants FreeLeech
                        }
                        if (configQualityEncodeOnly && !qualityEncode)
                        {
                            continue; //Skip release if user only wants Quality Encode
                        }

                        releases.Add(release);
                    }

                }
            }
            catch (Exception ex)
            {
                OnParseError(results.Content, ex);
            }

            return releases;
        }
    }
}
