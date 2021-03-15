using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public class DigitalCore : BaseWebIndexer
    {
        private string SearchUrl => SiteLink + "api/v1/torrents";
        private string LoginUrl => SiteLink + "api/v1/auth";

        private new ConfigurationDataCookie configData
        {
            get => (ConfigurationDataCookie)base.configData;
            set => base.configData = value;
        }

        public DigitalCore(IIndexerConfigurationService configService, WebClient w, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "digitalcore",
                   name: "DigitalCore",
                   description: "DigitalCore is a Private Torrent Tracker for MOVIES / TV / GENERAL",
                   link: "https://digitalcore.club/",
                   caps: new TorznabCapabilities
                   {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                       },
                       MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q, MovieSearchParam.ImdbId
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
                   configData: new ConfigurationDataCookie())
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";

            AddCategoryMapping(1, TorznabCatType.MoviesDVD, "Movies/DVDR");
            AddCategoryMapping(2, TorznabCatType.MoviesSD, "Movies/SD");
            AddCategoryMapping(3, TorznabCatType.MoviesBluRay, "Movies/BluRay");
            AddCategoryMapping(4, TorznabCatType.MoviesUHD, "Movies/4K");
            AddCategoryMapping(5, TorznabCatType.MoviesHD, "Movies/720p");
            AddCategoryMapping(6, TorznabCatType.MoviesHD, "Movies/1080p");
            AddCategoryMapping(7, TorznabCatType.MoviesHD, "Movies/PACKS");

            AddCategoryMapping(8, TorznabCatType.TVHD, "TV/720p");
            AddCategoryMapping(9, TorznabCatType.TVHD, "TV/1080p");
            AddCategoryMapping(10, TorznabCatType.TVSD, "TV/SD");
            AddCategoryMapping(11, TorznabCatType.TVSD, "TV/DVDR");
            AddCategoryMapping(12, TorznabCatType.TVHD, "TV/PACKS");
            AddCategoryMapping(13, TorznabCatType.TVUHD, "TV/4K");
            AddCategoryMapping(14, TorznabCatType.TVHD, "TV/BluRay");

            AddCategoryMapping(17, TorznabCatType.Other, "Unknown");
            AddCategoryMapping(18, TorznabCatType.PC0day, "Apps/0day");
            AddCategoryMapping(20, TorznabCatType.PCISO, "Apps/PC");
            AddCategoryMapping(21, TorznabCatType.PCMac, "Apps/Mac");
            AddCategoryMapping(33, TorznabCatType.PC, "Apps/Tutorials");

            AddCategoryMapping(22, TorznabCatType.AudioMP3, "Music/MP3");
            AddCategoryMapping(23, TorznabCatType.AudioLossless, "Music/FLAC");
            AddCategoryMapping(24, TorznabCatType.Audio, "Music/MTV");
            AddCategoryMapping(29, TorznabCatType.Audio, "Music/PACKS");

            AddCategoryMapping(25, TorznabCatType.PCGames, "Games/PC");
            AddCategoryMapping(26, TorznabCatType.Console, "Games/NSW");
            AddCategoryMapping(27, TorznabCatType.PCMac, "Games/Mac");

            AddCategoryMapping(28, TorznabCatType.Books, "Ebooks");

            AddCategoryMapping(30, TorznabCatType.XXXSD, "XXX/SD");
            AddCategoryMapping(31, TorznabCatType.XXX, "XXX/HD");
            AddCategoryMapping(32, TorznabCatType.XXXUHD, "XXX/4K");
            AddCategoryMapping(35, TorznabCatType.XXXSD, "XXX/Movies/SD");
            AddCategoryMapping(36, TorznabCatType.XXX, "XXX/Movies/HD");
            AddCategoryMapping(37, TorznabCatType.XXXUHD, "XXX/Movies/4K");
            AddCategoryMapping(34, TorznabCatType.XXXImageSet, "XXX/Imagesets");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            // TODO: implement captcha
            CookieHeader = configData.Cookie.Value;
            try
            {
                var results = await PerformQuery(new TorznabQuery());
                if (results.Count() == 0)
                {
                    throw new Exception("Found 0 results in the tracker");
                }

                IsConfigured = true;
                SaveConfig();
                return IndexerConfigurationStatus.Completed;
            }
            catch (Exception e)
            {
                IsConfigured = false;
                throw new Exception("Your cookie did not work: " + e.Message);
            }
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var queryCollection = new NameValueCollection();
            var searchString = query.GetQueryString();
            var searchUrl = SearchUrl;

            queryCollection.Add("extendedSearch", "false");
            queryCollection.Add("freeleech", "false");
            queryCollection.Add("index", "0");
            queryCollection.Add("limit", "100");
            queryCollection.Add("order", "desc");
            queryCollection.Add("page", "search");
            if (query.ImdbID != null)
                queryCollection.Add("searchText", query.ImdbID);
            else
                queryCollection.Add("searchText", searchString);
            queryCollection.Add("sort", "d");
            queryCollection.Add("section", "all");
            queryCollection.Add("stereoscopic", "false");
            queryCollection.Add("watchview", "false");

            searchUrl += "?" + queryCollection.GetQueryString();
            foreach (var cat in MapTorznabCapsToTrackers(query))
                searchUrl += "&categories[]=" + cat;
            var results = await RequestWithCookiesAsync(searchUrl, referer: SiteLink);

            try
            {
                //var json = JArray.Parse(results.Content);
                var json = JsonConvert.DeserializeObject<dynamic>(results.ContentString);
                foreach (var row in json ?? Enumerable.Empty<dynamic>())
                {
                    var release = new ReleaseInfo();
                    var descriptions = new List<string>();
                    var tags = new List<string>();

                    release.MinimumRatio = 1.1;
                    release.MinimumSeedTime = 432000; // 120 hours
                    release.Title = row.name;
                    release.Category = MapTrackerCatToNewznab(row.category.ToString());
                    release.Size = row.size;
                    release.Seeders = row.seeders;
                    release.Peers = row.leechers + release.Seeders;
                    release.PublishDate = DateTime.ParseExact(row.added.ToString() + " +01:00", "yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);
                    release.Files = row.numfiles;
                    release.Grabs = row.times_completed;

                    release.Details = new Uri(SiteLink + "torrent/" + row.id.ToString() + "/");
                    release.Guid = release.Details;
                    release.Link = new Uri(SiteLink + "api/v1/torrents/download/" + row.id.ToString());

                    if (row.frileech == 1)
                        release.DownloadVolumeFactor = 0;
                    else
                        release.DownloadVolumeFactor = 1;
                    release.UploadVolumeFactor = 1;


                    if (!string.IsNullOrWhiteSpace(row.firstpic.ToString()))
                    {
                        release.Poster = (row.firstpic);
                    }


                    if (row.imdbid2 != null && row.imdbid2.ToString().StartsWith("tt"))
                    {
                        release.Imdb = ParseUtil.CoerceLong(row.imdbid2.ToString().Substring(2));
                        descriptions.Add("Title: " + row.title);
                        descriptions.Add("Year: " + row.year);
                        descriptions.Add("Genres: " + row.genres);
                        descriptions.Add("Tagline: " + row.tagline);
                        descriptions.Add("Cast: " + row.cast);
                        descriptions.Add("Rating: " + row.rating);
                        //descriptions.Add("Plot: " + row.plot);

                        release.Poster = new Uri(SiteLink + "img/imdb/" + row.imdbid2 + ".jpg");
                    }

                    if ((int)row.p2p == 1)
                        tags.Add("P2P");
                    if ((int)row.pack == 1)
                        tags.Add("Pack");
                    if ((int)row.reqid != 0)
                        tags.Add("Request");

                    if (tags.Count > 0)
                        descriptions.Add("Tags: " + string.Join(", ", tags));

                    var preDate = row.preDate.ToString();
                    if (!string.IsNullOrWhiteSpace(preDate) && preDate != "1970-01-01 01:00:00")
                    {
                        descriptions.Add("Pre: " + preDate);
                    }
                    descriptions.Add("Section: " + row.section);

                    release.Description = string.Join("<br>\n", descriptions);

                    releases.Add(release);

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
