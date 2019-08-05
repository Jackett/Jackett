using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CsQuery;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    public class TVstore : BaseWebIndexer
    {

        private string LoginUrl { get { return SiteLink + "takelogin.php"; } }
        private string LoginPageUrl { get { return SiteLink + "login.php?returnto=%2F"; } }
        private string SearchUrl { get { return SiteLink + "torrent/br_process.php"; } }
        private string DownloadUrl { get { return SiteLink + "torrent/download.php"; } }
        private string BrowseUrl { get { return SiteLink + "torrent/browse.php"; } }
        private List<SeriesDetail> series = new List<SeriesDetail>();
        private Regex _searchStringRegex = new Regex(@"(.+?)S0?(\d+)(E0?(\d+))?$", RegexOptions.IgnoreCase);

        private new ConfigurationDataTVstore configData
        {
            get { return (ConfigurationDataTVstore)base.configData; }
            set { base.configData = value; }
        }

        public TVstore(IIndexerConfigurationService configService, Utils.Clients.WebClient wc, Logger l, IProtectionService ps)
            : base(name: "TVstore",
                description: "TV Store is a HUNGARIAN Private Torrent Tracker for TV",
                link: "https://tvstore.me/",
                caps: new TorznabCapabilities(),
                configService: configService,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataTVstore())
        {
            Encoding = Encoding.UTF8;
            Language = "hu-hu";
            Type = "private";

            TorznabCaps.SupportsImdbTVSearch = true;
            AddCategoryMapping(1, TorznabCatType.TV);
            AddCategoryMapping(2, TorznabCatType.TVHD);
            AddCategoryMapping(3, TorznabCatType.TVSD);

        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var loginPage = await RequestStringWithCookies(LoginPageUrl, string.Empty);
            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "back", "%2F" },
                { "logout", "1"}
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, loginPage.Cookies, true, referer: SiteLink);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("Főoldal"), () =>
            {
                throw new ExceptionWithConfigData("Error while trying to login with: Username: " + configData.Username.Value +
                                                  " Password: " + configData.Password.Value, configData);
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        /// <summary>
        /// Calculate the Upload Factor for the torrents
        /// </summary>
        /// <returns>The calculated factor</returns>
        /// <param name="dateTime">Date time.</param>
        /// <param name="type">Type of the torrent (SeasonPack/SingleEpisode).</param>
        public double UploadFactorCalculator(DateTime dateTime, string type)
        {
            var today = DateTime.Now;
            int dd = (today - dateTime).Days;

            /* In case of season Packs */
            if (type.Equals("season"))
            {
                if (dd >= 90) return 4;
                if (dd >= 30) return 2;
                if (dd >= 14) return 1.5;
            }
            else /* In case of single episodes */
            {
                if (dd >= 60) return 2; 
                if (dd >= 30) return 1.5;
            }
            return 1;
        }

        /// <summary>
        /// Parses the torrents from the content
        /// </summary>
        /// <returns>The parsed torrents.</returns>
        /// <param name="results">The result of the query</param>
        /// <param name="query">Query.</param>
        /// <param name="already_found">Number of the already found torrents.(used for limit)</param>
        /// <param name="limit">The limit to the number of torrents to download </param>
        async Task<List<ReleaseInfo>> ParseTorrents(WebClientStringResult results, TorznabQuery query, int already_found, int limit, int previously_parsed_on_page)
        {
            var releases = new List<ReleaseInfo>();
            try
            {
                String content = results.Content;
                /* Content Looks like this
                 * 2\15\2\1\1727\207244\1x08 \[WebDL-720p - Eng - AJP69]\gb\2018-03-09 08:11:53\akció, kaland, sci-fi \0\0\1\191170047\1\0\Anonymous\50\0\0\\0\4\0\174\0\
                 * 1\ 0\0\1\1727\207243\1x08 \[WebDL-1080p - Eng - AJP69]\gb\2018-03-09 08:11:49\akció, kaland, sci-fi \0\0\1\305729738\1\0\Anonymous\50\0\0\\0\8\0\102\0\0\0\0\1\\\
                 */
                string[] parameters = content.Split(new string[] { "\\" }, StringSplitOptions.None);
                string type = "normal";

                /* 
                 * Split the releases by '\' and go through them. 
                 * 27 element belongs to one torrent
                 */
                for (int j = previously_parsed_on_page * 27; (j + 27 < parameters.Length && ((already_found + releases.Count) < limit)); j = j + 27)
                {
                    ReleaseInfo release = new ReleaseInfo();

                    int imdb_id = 4 + j;
                    int torrent_id = 5 + j;
                    int is_season_id = 6 + j;
                    int publish_date_id = 9 + j;
                    int files_id = 13 + j;
                    int size_id = 14 + j;
                    int seeders_id = 23;
                    int peers_id = 24 + j;
                    int grabs_id = 25 + j;


                    type = "normal";
                    //IMDB id of the series
                    SeriesDetail seriesinfo = series.Find(x => x.id.Contains(parameters[imdb_id]));
                    if (seriesinfo != null && !parameters[imdb_id].Equals(""))
                        release.Imdb = long.Parse(seriesinfo.imdbid);

                    //ID of the torrent
                    Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

                    string fileinfoURL = SearchUrl + "?func=getToggle&id=" + parameters[torrent_id] + "&w=F&pg=0&now=" + unixTimestamp;
                    string fileinfo = (await RequestStringWithCookiesAndRetry(fileinfoURL)).Content;
                    release.Link = new Uri(DownloadUrl + "?id=" + parameters[torrent_id]);
                    release.Guid = release.Link;
                    release.Comments = release.Link;
                    string[] fileinf = fileinfo.Split(new string[] { "\\\\" }, StringSplitOptions.None);
                    if (fileinf.Length > 1)
                    {
                        release.Title = fileinf[1];
                        if (fileinf[1].Length > 5 && fileinf[1].Substring(fileinf[1].Length - 4).Contains("."))
                            release.Title = fileinf[1].Substring(0, fileinf[1].Length - 4);
                    }
                    // SeasonPack check
                    if (parameters[is_season_id].Contains("évad/"))
                    {
                        type = "season";
                        // If this is a seasonpack, remove episode nunmber from title.
                        release.Title = Regex.Replace(release.Title, "s0?(\\d+)(e0?(\\d+))", "S$1", RegexOptions.IgnoreCase);
                    }

                    release.PublishDate = DateTime.Parse(parameters[publish_date_id], CultureInfo.InvariantCulture);
                    release.Files = int.Parse(parameters[files_id]);
                    release.Size = long.Parse(parameters[size_id]);
                    release.Seeders = int.Parse(parameters[seeders_id]);
                    release.Peers = (int.Parse(parameters[peers_id]) + release.Seeders);
                    release.Grabs = int.Parse(parameters[grabs_id]);
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;
                    release.DownloadVolumeFactor = 1;
                    release.UploadVolumeFactor = UploadFactorCalculator(release.PublishDate, type);
                    release.Category = new List<int> { TvCategoryParser.ParseTvShowQuality(release.Title) };
                    if ((already_found + releases.Count) < limit)
                        releases.Add(release);
                    else
                        return releases;
                }

            }
            catch (Exception ex)
            {
                OnParseError(results.Content, ex);
            }

            return releases;
        }
        /* Search is possible only based by Series ID. 
         * All known series ID is on main page, with their attributes. (ID, EngName, HunName, imdbid)
         */

        /// <summary>
        /// Get all series info known by site
        /// These are:
        ///     - Series ID
        ///     - Hungarian name
        ///     - English name
        ///     - IMDB ID
        /// </summary>
        /// <returns>The series info.</returns>
        protected async Task<Boolean> GetSeriesInfo()
        {

            var result = (await RequestStringWithCookiesAndRetry(BrowseUrl)).Content;

            CQ dom = result;
            var scripts = dom["script"];

            foreach (var script in scripts)
            {
                if (script.TextContent.Contains("catsh=Array"))
                {
                    string[] seriesknowbysite = Regex.Split(script.TextContent, "catl");
                    for (int i = 1; i < seriesknowbysite.Length; i++)
                    {
                        try
                        {
                            var id = seriesknowbysite[i];
                            string[] serieselement = WebUtility.HtmlDecode(id).Split(';');
                            SeriesDetail sd = new SeriesDetail();
                            sd.HunName = serieselement[1].Split('=')[1].Trim('\'').ToLower();
                            sd.EngName = serieselement[2].Split('=')[1].Trim('\'').ToLower();
                            sd.id = serieselement[0].Split('=')[1].Trim('\'');
                            sd.imdbid = serieselement[7].Split('=')[1].Trim('\'');
                            series.Add(sd);
                        }
                        catch (IndexOutOfRangeException e)
                        {
                            throw (e);
                        }
                    }
                }
            }
            return true;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            /* If series from sites are indexed than we dont need to reindex them. */
            if (series == null || series.IsEmpty())
            {
                await GetSeriesInfo();
            }

            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

            WebClientStringResult results;

            string searchString = "";
            string exactSearchURL = "";
            int page = 1;
            SeriesDetail seriesinfo = null;
            String base64coded = "";
            bool noimdbmatch = false;
            var limit = query.Limit;
            if (limit == 0)
                limit = 100;
            if (query.IsImdbQuery)
            {
                seriesinfo = series.Find(x => x.imdbid.Equals(query.ImdbIDShort));
                if (seriesinfo != null && !query.ImdbIDShort.Equals(""))
                {
                    String querrySeason = "";
                    if (query.Season != 0)
                        querrySeason = query.Season.ToString();
                    exactSearchURL = SearchUrl + "?s=" + querrySeason + "&e=" + query.Episode + "&g=" + seriesinfo.id + "&now=" + unixTimestamp.ToString();
                }
                else
                {
                    // IMDB_ID was not founded in site database.
                    return releases;
                }

            }
            if (!query.IsImdbQuery || noimdbmatch)
            {
                /* SearchString format is the following: Seriesname 1X09 */
                if (query.SearchTerm != null && !query.SearchTerm.Equals(""))
                {
                    searchString += query.SanitizedSearchTerm;
                    // convert SnnEnn to nnxnn for dashboard searches
                    if (query.Season == 0 && (query.Episode == null || query.Episode.Equals("")))
                    {
                        Match searchMatch = _searchStringRegex.Match(searchString);
                        if (searchMatch.Success)
                        {
                            query.Season = int.Parse(searchMatch.Groups[2].Value);
                            query.Episode = searchMatch.Groups[4].Success ? string.Format("{0:00}", (int?)int.Parse(searchMatch.Groups[4].Value)) : null;
                            searchString = searchMatch.Groups[1].Value; // strip SnnEnn
                        }
                    }

                    if (query.Season != 0)
                        searchString += " " + query.Season.ToString();
                    if (query.Episode != null && !query.Episode.Equals(""))
                        searchString += string.Format("x{0:00}", int.Parse(query.Episode));
                }
                else
                {
                    // if searchquery is empty this is a test, so shorten the response time
                    limit = 20;
                }

                /* Search string must be converted to Base64 */
                var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(searchString);
                base64coded = System.Convert.ToBase64String(plainTextBytes);


                exactSearchURL = SearchUrl + "?gyors=" + base64coded + "&p=" + page + "&now=" + unixTimestamp.ToString();
            }

            /*Start search*/
            results = await RequestStringWithCookiesAndRetry(exactSearchURL);

            /* Parse page Information from result */
            string content = results.Content;
            var splits = content.Split('\\');
            int max_found = int.Parse(splits[0]);
            int torrent_per_page = int.Parse(splits[1]);


            if (torrent_per_page == 0)
                return releases;
            int start_page = (query.Offset / torrent_per_page) + 1;
            int previously_parsed_on_page = query.Offset - (start_page * torrent_per_page) + 1; //+1 because indexing start from 0
            if (previously_parsed_on_page <= 0)
                previously_parsed_on_page = query.Offset;
                

            double pages = Math.Ceiling((double)max_found / (double)torrent_per_page);

            /* First page content is already ready */
            if (start_page == 1)
            {
                releases.AddRange(await ParseTorrents(results, query, releases.Count, limit, previously_parsed_on_page));
                previously_parsed_on_page = 0;
                start_page++;
            }

            for (page =start_page; (page<=pages && releases.Count<limit);page++)
            {
                if(query.IsImdbQuery && seriesinfo != null )
                    exactSearchURL = SearchUrl + "?s=" + query.Season + "&e=" + query.Episode + "&g=" + seriesinfo.id + "&p=" + page + "&now=" + unixTimestamp.ToString();
                else
                    exactSearchURL = SearchUrl + "?gyors=" + base64coded + "&p=" + page + "&now=" + unixTimestamp.ToString();
                results = await RequestStringWithCookiesAndRetry(exactSearchURL);
                releases.AddRange(await ParseTorrents(results, query, releases.Count, limit, previously_parsed_on_page));
                previously_parsed_on_page = 0;

            }

            return releases;
        }
    }
    public class SeriesDetail
    {
        public string id;
        public string HunName;
        public string EngName;
        public string imdbid;

    }

}
