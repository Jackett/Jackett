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
    public class SuperBits : BaseWebIndexer
    {
        private string SearchUrl => SiteLink + "api/v1/torrents";
        private string LoginUrl => SiteLink + "api/v1/auth";

        private new ConfigurationDataCookie configData
        {
            get => (ConfigurationDataCookie)base.configData;
            set => base.configData = value;
        }

        public SuperBits(IIndexerConfigurationService configService, WebClient w, Logger l, IProtectionService ps)
            : base(id: "superbits",
                   name: "Superbits",
                   description: "Superbits is a SWEDISH Private Torrent Tracker for MOVIES / TV / GENERAL",
                   link: "https://superbits.org/",
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
                   configData: new ConfigurationDataCookie())
        {
            Encoding = Encoding.UTF8;
            Language = "sv-sw";
            Type = "private";

            AddCategoryMapping(1, TorznabCatType.MoviesDVD, "DVD-R Swesub");
            AddCategoryMapping(2, TorznabCatType.TV, "DVD-R TV");
            AddCategoryMapping(3, TorznabCatType.BooksEBook, "eBok");
            AddCategoryMapping(4, TorznabCatType.MoviesHD, "Film 1080");
            AddCategoryMapping(5, TorznabCatType.Movies3D, "Film 3D");
            AddCategoryMapping(6, TorznabCatType.MoviesHD, "Film 720");
            AddCategoryMapping(7, TorznabCatType.MoviesBluRay, "Film Bluray");
            AddCategoryMapping(8, TorznabCatType.TV, "Svensk TV");
            AddCategoryMapping(9, TorznabCatType.AudioAudiobook, "Ljudböcker");
            AddCategoryMapping(10, TorznabCatType.AudioVideo, "Musikvideos");
            AddCategoryMapping(11, TorznabCatType.BooksMags, "E-tidningar");
            AddCategoryMapping(12, TorznabCatType.Audio, "Musik");
            AddCategoryMapping(13, TorznabCatType.Other, "Omslag");
            AddCategoryMapping(14, TorznabCatType.Other, "Övrigt");
            AddCategoryMapping(15, TorznabCatType.PCGames, "PC-Spel");
            AddCategoryMapping(16, TorznabCatType.PC0day, "Program");
            AddCategoryMapping(17, TorznabCatType.ConsolePS3, "PS3");
            AddCategoryMapping(18, TorznabCatType.TV, "TV");
            AddCategoryMapping(19, TorznabCatType.ConsoleWii, "Wii");
            AddCategoryMapping(20, TorznabCatType.ConsoleXBox, "Xbox");
            AddCategoryMapping(21, TorznabCatType.MoviesOther, "Xvid");
            AddCategoryMapping(22, TorznabCatType.XXX, "XXX");
            AddCategoryMapping(24, TorznabCatType.MoviesUHD, "Film 4K");
            AddCategoryMapping(26, TorznabCatType.TV, "TV DK");
            AddCategoryMapping(27, TorznabCatType.TV, "TV NO");
            AddCategoryMapping(28, TorznabCatType.TV, "TV FI");            
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
            // And this was option one from
            // https://github.com/Jackett/Jackett/pull/7166#discussion_r376817517
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
            queryCollection.Add("sweaudio", "false");
            queryCollection.Add("swesub", "false");
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
                    release.MinimumSeedTime = 172800; // 48 hours
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

                    if (!string.IsNullOrWhiteSpace(row.customcover.ToString()))
                        release.Poster = new Uri(SiteLink + row.customcover);

                    if (row.imdbid2 != null && row.imdbid2.ToString().StartsWith("tt"))
                    {
                        release.Imdb = ParseUtil.CoerceLong(row.imdbid2.ToString().Substring(2));
                        descriptions.Add("Title: " + row.title);
                        descriptions.Add("Year: " + row.year);
                        descriptions.Add("Genres: " + row.genres);
                        descriptions.Add("Tagline: " + row.tagline);
                        descriptions.Add("Cast: " + row.cast);
                        descriptions.Add("Rating: " + row.rating);
                        descriptions.Add("Plot: " + row.plot);

                        release.Poster = new Uri(SiteLink + "img/imdb/" + row.imdbid2 + ".jpg");
                    }

                    if ((int)row.p2p == 1)
                        tags.Add("P2P");
                    if ((int)row.pack == 1)
                        tags.Add("Pack");
                    if ((int)row.reqid != 0)
                        tags.Add("Request");
                    if ((int)row.sweaudio != 0)
                        tags.Add("Swedish audio");
                    if ((int)row.swesub != 0)
                        tags.Add("Swedish subtitles");

                    if (tags.Count > 0)
                        descriptions.Add("Tags: " + string.Join(", ", tags));

                    var preDate = row.preDate.ToString();
                    if (!string.IsNullOrWhiteSpace(preDate) && preDate != "1970-01-01 01:00:00")
                        descriptions.Add("PRE: " + preDate);

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
