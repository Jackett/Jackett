using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class Partis : BaseWebIndexer
    {
        private string LoginUrl => SiteLink + "user/login/";
        private string SearchUrl => SiteLink + "torrent/search";

        private new ConfigurationDataBasicLogin configData => (ConfigurationDataBasicLogin)base.configData;

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
            Language = "sl-sl";
            Type = "private";

            // Blu Ray
            AddCategoryMapping(40, TorznabCatType.MoviesBluRay, "Blu-Ray 1080p/i");
            AddCategoryMapping(42, TorznabCatType.MoviesBluRay, "Blu-Ray 720p/i");
            AddCategoryMapping(43, TorznabCatType.MoviesBluRay, "Blu-Ray B-Disc");
            AddCategoryMapping(41, TorznabCatType.MoviesBluRay, "Blu-Ray 3D");
            AddCategoryMapping(44, TorznabCatType.MoviesBluRay, "Blu-Ray Remux");
            AddCategoryMapping(45, TorznabCatType.MoviesBluRay, "Blu-Ray Remux/Disc");

            // UHD
            AddCategoryMapping(32, TorznabCatType.MoviesUHD, "UHD 4K Disc");
            AddCategoryMapping(55, TorznabCatType.MoviesUHD, "UHD 4K Remux");

            // HD
            AddCategoryMapping(20, TorznabCatType.MoviesHD, "HD");
            AddCategoryMapping(4, TorznabCatType.MoviesSD, "DVD-R");
            AddCategoryMapping(7, TorznabCatType.MoviesSD, "XviD");
            AddCategoryMapping(12, TorznabCatType.MoviesSD, "Anime");
            AddCategoryMapping(30, TorznabCatType.MoviesSD, "Risanke");
            AddCategoryMapping(15, TorznabCatType.MoviesSD, "Sport");

            // TV Show
            AddCategoryMapping(53, TorznabCatType.TVWEBDL, "TV WEB-DL");
            AddCategoryMapping(60, TorznabCatType.TVSD, "TV-XviD");
            AddCategoryMapping(38, TorznabCatType.TVSD, "SD-TV");
            AddCategoryMapping(51, TorznabCatType.TVHD, "TV 1080p/i");
            AddCategoryMapping(52, TorznabCatType.TVHD, "TV 720p/i");

            AddCategoryMapping(54, TorznabCatType.MoviesSD, "WEBRip");
            AddCategoryMapping(59, TorznabCatType.MoviesWEBDL, "WEB-DL");
            AddCategoryMapping(24, TorznabCatType.TVDocumentary, "Dokumentarci");

            // Games
            AddCategoryMapping(10, TorznabCatType.PCGames, "PC igre/ISO");
            AddCategoryMapping(11, TorznabCatType.PCGames, "PC igre/Rips/Repack");
            AddCategoryMapping(64, TorznabCatType.PCGames, "PC igre/Update & Patch");

            // Music
            AddCategoryMapping(46, TorznabCatType.AudioLossless, "Glasba/Flac");
            AddCategoryMapping(8, TorznabCatType.AudioOther, "Glasba/Ostalo");
            AddCategoryMapping(47, TorznabCatType.AudioMP3, "Glasba/Mp3");
            AddCategoryMapping(8, TorznabCatType.AudioVideo, "Music DVD");
            AddCategoryMapping(8, TorznabCatType.AudioVideo, "Videospoti");
            AddCategoryMapping(21, TorznabCatType.AudioAudiobook, "AudioBook");
            AddCategoryMapping(3, TorznabCatType.BooksEBook, "eKnjige");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var loginPage = await RequestWithCookiesAsync(LoginUrl, string.Empty);

            var pairs = new Dictionary<string, string>
            {
                { "user", configData.Username.Value },
                { "pass", configData.Password.Value }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, string.Empty, false, null, null, true);
            await ConfigureIfOK(result.Cookies, result.ContentString != null && result.ContentString.Contains("/portal#prva"), () =>
                throw new ExceptionWithConfigData("Login failed", configData));
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();     //List of releases initialization
            var searchString = query.GetQueryString();  //get search string from query

            WebResult results;
            var queryCollection = new NameValueCollection();
            var catList = MapTorznabCapsToTrackers(query);     // map categories from query to indexer specific
            var categ = string.Join(",", catList);

            //create GET request - search URI
            queryCollection.Add("q", searchString);
            queryCollection.Add("cat", categ.TrimStart(','));

            //concatenate base search url with query
            var searchUrl = SearchUrl + "?" + queryCollection.GetQueryString();

            // log search URL
            logger.Info(string.Format("Searh URL Partis: {0}", searchUrl));

            //get results
            results = await RequestWithCookiesAsync(searchUrl);

            // are we logged in?
            if (!results.ContentString.StartsWith("data"))
            {
                await ApplyConfiguration(null);
            }

            // parse results
            try
            {
                var results2 = results.ContentString.FindSubstringsBetween('{', '}', true).Where(x => x.StartsWith("{\"torrent_list\""));
                var jsResults = JObject.Parse(results2.First());
                var Rows = jsResults["torrent_list"]["data"];

                foreach (var Row in Rows)
                {
                    try
                    {
                        // initialize REleaseInfo
                        var release = new ReleaseInfo
                        {
                            MinimumRatio = 1,
                            MinimumSeedTime = 0,
                        };

                        var tid = Row[0].ToString();

                        // Get Category
                        release.Category = MapTrackerCatToNewznab(Row[7].ToString());

                        // Title and torrent link
                        release.Title = Row[3].ToString();
                        release.Details = new Uri(SiteLink + "/portal#torrent/" + tid);
                        release.Guid = release.Details;

                        // Date of torrent creation
                        var reldate = (long)Row[2];
                        release.PublishDate = DateTimeOffset.FromUnixTimeSeconds(reldate).LocalDateTime;

                        // Download link
                        release.Link = new Uri(SiteLink + "torrent/download/" + tid);

                        // Various data - size, seeders, leechers, download count
                        var size = Row[9].ToString();
                        release.Size = ReleaseInfo.GetBytes(size);

                        release.Seeders = ((long)Row[4]);
                        release.Peers = ((long)Row[5]) + release.Seeders;

                        // Set download/upload factor
                        release.DownloadVolumeFactor = 1; //checkIfFree ? 0 : 1; freeleech not implemented yet
                        release.UploadVolumeFactor = 1;

                        // Add current release to List
                        releases.Add(release);
                    }

                    catch (Exception ex)
                    {
                        logger.Error(string.Format("{0}: Error while parsing row '{1}':\n\n{2}", Id, Row, ex));
                    }
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.ContentString, ex);
            }

            return releases;
        }
    }
}
