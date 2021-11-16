using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class Partis : BaseWebIndexer
    {
        private string LoginUrl => SiteLink + "user/login/";
        private string SearchUrl => SiteLink + "torrent/search/";

        private new ConfigurationDataBasicLogin configData
        {
            get => (ConfigurationDataBasicLogin)base.configData;
            set => base.configData = value;
        }

        public Partis(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "partis",
                   name: "Partis",
                   description: "Partis is a SLOVENIAN Private Torrent Tracker",
                   link: "https://www.partis.si/",
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
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataBasicLogin())
        {
            Encoding = Encoding.UTF8;
            Language = "sl-SL";
            Type = "private";

            // Movies
            AddCategoryMapping(40, TorznabCatType.MoviesBluRay, "Blu-Ray 1080p/i");
            AddCategoryMapping(42, TorznabCatType.MoviesBluRay, "Blu-Ray 720p/i");
            AddCategoryMapping(43, TorznabCatType.MoviesBluRay, "Blu-Ray B-Disc");
            AddCategoryMapping(41, TorznabCatType.MoviesBluRay, "Blu-Ray 3D");
            AddCategoryMapping(44, TorznabCatType.MoviesBluRay, "Blu-Ray Remux");
            AddCategoryMapping(45, TorznabCatType.MoviesBluRay, "Blu-Ray Remux/Disc");
            AddCategoryMapping(32, TorznabCatType.MoviesUHD, "UHD 4K Disc");
            AddCategoryMapping(55, TorznabCatType.MoviesUHD, "UHD 4K Remux");
            AddCategoryMapping(20, TorznabCatType.MoviesHD, "HD");
            AddCategoryMapping(4, TorznabCatType.MoviesSD, "DVD-R");
            AddCategoryMapping(7, TorznabCatType.MoviesSD, "XviD");
            AddCategoryMapping(30, TorznabCatType.MoviesSD, "Risanke");
            AddCategoryMapping(54, TorznabCatType.MoviesSD, "WEBRip");
            AddCategoryMapping(59, TorznabCatType.MoviesWEBDL, "WEB-DL");

            // TV
            AddCategoryMapping(53, TorznabCatType.TVWEBDL, "TV WEB-DL");
            AddCategoryMapping(60, TorznabCatType.TVSD, "TV-XviD");
            AddCategoryMapping(38, TorznabCatType.TVSD, "SD-TV");
            AddCategoryMapping(51, TorznabCatType.TVHD, "TV 1080p/i");
            AddCategoryMapping(52, TorznabCatType.TVHD, "TV 720p/i");
            AddCategoryMapping(5, TorznabCatType.TVSport, "Sport");
            AddCategoryMapping(2, TorznabCatType.TVAnime, "Anime");
            AddCategoryMapping(24, TorznabCatType.TVDocumentary, "Dokumentarci");

            // Games
            AddCategoryMapping(10, TorznabCatType.PCGames, "PC igre/ISO");
            AddCategoryMapping(11, TorznabCatType.PCGames, "PC igre/Rips/Repack");
            AddCategoryMapping(64, TorznabCatType.PCGames, "PC igre/Update & Patch");
            AddCategoryMapping(13, TorznabCatType.ConsolePSP, "PSP");
            AddCategoryMapping(12, TorznabCatType.ConsoleOther, "PS2");
            AddCategoryMapping(28, TorznabCatType.ConsolePS3, "PS3");
            AddCategoryMapping(63, TorznabCatType.ConsolePS4, "PS4");
            AddCategoryMapping(27, TorznabCatType.ConsoleWii, "Wii");
            AddCategoryMapping(14, TorznabCatType.ConsoleXBox, "XboX");
            AddCategoryMapping(49, TorznabCatType.PCGames, "Mac Igre");
            AddCategoryMapping(48, TorznabCatType.PCGames, "Linux Igre");

            // Music
            AddCategoryMapping(46, TorznabCatType.AudioLossless, "Glasba/Flac");
            AddCategoryMapping(8, TorznabCatType.AudioOther, "Glasba/Ostalo");
            AddCategoryMapping(47, TorznabCatType.AudioMP3, "Glasba/Mp3");
            AddCategoryMapping(8, TorznabCatType.AudioVideo, "Music DVD");
            AddCategoryMapping(8, TorznabCatType.AudioVideo, "Videospoti");

            // Programs
            AddCategoryMapping(15, TorznabCatType.PC, "PC programi/drugo");
            AddCategoryMapping(15, TorznabCatType.PCMac, "Mac Programi");
            AddCategoryMapping(16, TorznabCatType.PCISO, "PC programi/ISO");

            // Other
            AddCategoryMapping(21, TorznabCatType.AudioAudiobook, "AudioBook");
            AddCategoryMapping(3, TorznabCatType.BooksEBook, "eKnjige");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string>
            {
                { "user", configData.Username.Value },
                { "pass", configData.Password.Value }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, string.Empty, false, null, null, true);
            await ConfigureIfOK(result.Cookies, result.ContentString != null && result.Cookies.Contains("udata"), () =>
            {
                var errorMessage = "Login failed. Invalid username or password.";
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();     //List of releases initialization
            var searchString = query.GetQueryString();  //get search string from query

            WebResult results = null;
            var queryCollection = new NameValueCollection();
            var catList = MapTorznabCapsToTrackers(query);     // map categories from query to indexer specific
            var categ = string.Join(",", catList);

            //create GET request - search URI
            queryCollection.Add("q", searchString);
            queryCollection.Add("cat", categ.TrimStart(','));

            //c oncatenate base search url with query
            var searchUrl = $"{SearchUrl}?{queryCollection.GetQueryString()}";

            // log search URL
            logger.Info(string.Format("Searh URL Partis_: {0}", searchUrl));

            // add necessary headers
            var header = new Dictionary<string, string>
            {
                { "X-requested-with", "XMLHttpRequest" }
            };

            // get results and follow redirect
            results = await RequestWithCookiesAsync(searchUrl, referer: SearchUrl, headers: header);

            // parse results
            try
            {
                // successful search returns javascript code containing 'data' variable with actual torrent descriptions. 
                // find this variable and extract its value
                var resultDataJson = Regex.Match(results.ContentString, @"data( |=)*(?<json>\{.*\})").Groups["json"];

                var resultsData = JsonConvert.DeserializeObject<dynamic>(resultDataJson.Value);

                foreach (var torrent in resultsData.sets.torrent_list.data)
                {
                    var release = ParseRelease(torrent);
                    if (release != null)
                        releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.ContentString, ex);
            }

            return releases;
        }

        private ReleaseInfo ParseRelease(dynamic torrent)
        {
            /* Single torrent is represented with array:
            [
                600934, -- id
                4571,   -- ?
                1636450141, -- added timestamp
                "No.Time.To.Die.2021.ENGSubs.REPACK.HDRip.",  -- title (max 41 chars)
                677, -- leechers
                30, -- seeders
                "NI ČAS ZA SMRT/ NOVI JAMES BOND | Akcijski | HDRip...", -- description 
                7, -- category
                54.9014462385559, -- health? scaled 0-100
                "1.7 GB", -- size
                "/img/ics/xvid.gif", -- icon
                "/torrent/image/600/600934/coverflow/james-bond.jpg" -- thumbnail
            ]
            */
            try
            {
                // initialize ReleaseInfo
                var release = new ReleaseInfo
                {
                    MinimumRatio = 1,
                    MinimumSeedTime = 0
                };

                // Get Category
                release.Category = MapTrackerCatToNewznab(torrent[7].ToString());

                // Title, description and details link
                release.Title = torrent[3].ToString();
                release.Description = torrent[6].ToString();
                release.Details = new Uri($"{SiteLink}index.html#torrent/{torrent[0]}");
                release.Guid = release.Details;

                // Date of torrent creation                   
                release.PublishDate = DateTimeUtil.UnixTimestampToDateTime((long)torrent[2]);

                // Download link
                release.Link = new Uri($"{SiteLink}torrent/download/{torrent[0]}");

                // Various data - size, seeders, leechers, download count
                release.Size = ReleaseInfo.GetBytes(torrent[9].ToString());
                release.Seeders = ParseUtil.CoerceInt(torrent[4].ToString());
                release.Peers = ParseUtil.CoerceInt(torrent[5].ToString()) + release.Seeders;

                // Poster
                release.Poster = new Uri($"{SiteLink}{torrent[11]}");

                // // Set download/upload factor
                release.DownloadVolumeFactor = 1; //No way to determine if torrent is freeleech from single request.
                release.UploadVolumeFactor = 1;

                return release;
            }
            catch (Exception ex)
            {
                logger.Error(string.Format("{0}: Error while parsing torrent '{1}':\n\n{2}", Id, torrent, ex));
                return null;
            }
        }
    }
}
