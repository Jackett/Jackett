using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jackett.Common.Helpers;
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
    public class TorrentDay : BaseWebIndexer
    {
        private string SearchUrl => SiteLink + "t.json";

        public override string[] AlternativeSiteLinks { get; protected set; } = {
            "https://tday.love/",
            "https://torrentday.cool/",
            "https://secure.torrentday.com/",
            "https://classic.torrentday.com/",
            "https://www.torrentday.com/",
            "https://torrentday.it/",
            "https://td.findnemo.net/",
            "https://td.getcrazy.me/",
            "https://td.venom.global/",
            "https://td.workisboring.net/"
        };

        public override string[] LegacySiteLinks { get; protected set; } = {
            "https://torrentday.com/",
            "https://tdonline.org/", // redirect to https://www.torrentday.com/
            "https://torrentday.eu/", // redirect to https://www.torrentday.com/
            "https://td-update.com/", // redirect to https://www.torrentday.com/
            "https://www.torrentday.me/",
            "https://www.torrentday.ru/",
            "https://www.td.af/"
        };

        private new ConfigurationDataCookie configData => (ConfigurationDataCookie)base.configData;

        public TorrentDay(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "torrentday",
                   name: "TorrentDay",
                   description: "TorrentDay (TD) is a Private site for TV / MOVIES / GENERAL",
                   link: "https://tday.love/",
                   caps: new TorznabCapabilities
                   {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId
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
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataCookie(
                       "Make sure you get the cookies from the same torrent day domain as configured above."))
        {
            Encoding = Encoding.UTF8;
            Language = "en-US";
            Type = "private";

            wc.EmulateBrowser = false;

            AddCategoryMapping(29, TorznabCatType.TVAnime, "Anime");
            AddCategoryMapping(28, TorznabCatType.PC, "Appz/Packs");
            AddCategoryMapping(42, TorznabCatType.AudioAudiobook, "Audio Books");
            AddCategoryMapping(20, TorznabCatType.Books, "Books");
            AddCategoryMapping(30, TorznabCatType.TVDocumentary, "Documentary");
            AddCategoryMapping(47, TorznabCatType.Other, "Fonts");
            AddCategoryMapping(43, TorznabCatType.PCMac, "Mac");

            AddCategoryMapping(96, TorznabCatType.MoviesUHD, "Movie/4K");
            AddCategoryMapping(25, TorznabCatType.MoviesSD, "Movies/480p");
            AddCategoryMapping(11, TorznabCatType.MoviesBluRay, "Movies/Bluray");
            AddCategoryMapping(5, TorznabCatType.MoviesBluRay, "Movies/Bluray-Full");
            AddCategoryMapping(3, TorznabCatType.MoviesDVD, "Movies/DVD-R");
            AddCategoryMapping(21, TorznabCatType.MoviesSD, "Movies/MP4");
            AddCategoryMapping(22, TorznabCatType.MoviesForeign, "Movies/Non-English");
            AddCategoryMapping(13, TorznabCatType.Movies, "Movies/Packs");
            AddCategoryMapping(44, TorznabCatType.MoviesSD, "Movies/SD/x264");
            AddCategoryMapping(48, TorznabCatType.Movies, "Movies/x265");
            AddCategoryMapping(1, TorznabCatType.MoviesSD, "Movies/XviD");

            AddCategoryMapping(17, TorznabCatType.AudioMP3, "Music/Audio");
            AddCategoryMapping(23, TorznabCatType.AudioForeign, "Music/Non-English");
            AddCategoryMapping(41, TorznabCatType.Audio, "Music/Packs");
            AddCategoryMapping(16, TorznabCatType.AudioVideo, "Music/Video");
            AddCategoryMapping(27, TorznabCatType.Audio, "Music/Flac");

            AddCategoryMapping(45, TorznabCatType.AudioOther, "Podcast");

            AddCategoryMapping(4, TorznabCatType.PCGames, "PC/Games");
            AddCategoryMapping(18, TorznabCatType.ConsolePS3, "PS3");
            AddCategoryMapping(8, TorznabCatType.ConsolePSP, "PSP");
            AddCategoryMapping(10, TorznabCatType.ConsoleWii, "Wii");
            AddCategoryMapping(9, TorznabCatType.ConsoleXBox360, "Xbox-360");

            AddCategoryMapping(24, TorznabCatType.TVSD, "TV/480p");
            AddCategoryMapping(32, TorznabCatType.TVHD, "TV/Bluray");
            AddCategoryMapping(31, TorznabCatType.TVSD, "TV/DVD-R");
            AddCategoryMapping(33, TorznabCatType.TVSD, "TV/DVD-Rip");
            AddCategoryMapping(46, TorznabCatType.TVSD, "TV/Mobile");
            AddCategoryMapping(14, TorznabCatType.TV, "TV/Packs");
            AddCategoryMapping(26, TorznabCatType.TVSD, "TV/SD/x264");
            AddCategoryMapping(7, TorznabCatType.TVHD, "TV/x264");
            AddCategoryMapping(34, TorznabCatType.TVUHD, "TV/x265");
            AddCategoryMapping(2, TorznabCatType.TVSD, "TV/XviD");

            AddCategoryMapping(6, TorznabCatType.XXX, "XXX/Movies");
            AddCategoryMapping(15, TorznabCatType.XXXPack, "XXX/Packs");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            CookieHeader = configData.Cookie.Value;
            try
            {
                var results = await PerformQuery(new TorznabQuery());
                if (!results.Any())
                    throw new Exception("Found 0 results in the tracker");

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

            var cats = MapTorznabCapsToTrackers(query);
            if (cats.Count == 0)
                cats = GetAllTrackerCategories();
            var catStr = string.Join(";", cats);
            var searchUrl = SearchUrl + "?" + catStr;

            if (query.IsImdbQuery)
                searchUrl += ";q=" + query.ImdbID;
            else
                searchUrl += ";q=" + WebUtilityHelpers.UrlEncode(query.GetQueryString(), Encoding);

            var results = await RequestWithCookiesAndRetryAsync(searchUrl);

            // Check for being logged out
            if (results.IsRedirect)
                if (results.RedirectingTo.Contains("login.php"))
                    throw new Exception("The user is not logged in. It is possible that the cookie has expired or you made a mistake when copying it. Please check the settings.");
                else
                    throw new Exception($"Got a redirect to {results.RedirectingTo}, please adjust your the alternative link");

            try
            {
                var rows = JsonConvert.DeserializeObject<dynamic>(results.ContentString);

                foreach (var row in rows)
                {
                    var title = (string)row.name;
                    if ((!query.IsImdbQuery || !TorznabCaps.MovieSearchImdbAvailable) && !query.MatchQueryStringAND(title))
                        continue;
                    var torrentId = (long)row.t;
                    var details = new Uri(SiteLink + "details.php?id=" + torrentId);
                    var seeders = (int)row.seeders;
                    var imdbId = (string)row["imdb-id"];
                    var downloadMultiplier = (double?)row["download-multiplier"] ?? 1;
                    var link = new Uri(SiteLink + "download.php/" + torrentId + "/" + torrentId + ".torrent");
                    var publishDate = DateTimeUtil.UnixTimestampToDateTime((long)row.ctime).ToLocalTime();
                    var imdb = ParseUtil.GetImdbID(imdbId);

                    var release = new ReleaseInfo
                    {
                        Title = title,
                        Details = details,
                        Guid = details,
                        Link = link,
                        PublishDate = publishDate,
                        Category = MapTrackerCatToNewznab(row.c.ToString()),
                        Size = (long)row.size,
                        Files = (long)row.files,
                        Grabs = (long)row.completed,
                        Seeders = seeders,
                        Peers = seeders + (int)row.leechers,
                        Imdb = imdb,
                        DownloadVolumeFactor = downloadMultiplier,
                        UploadVolumeFactor = 1,
                        MinimumRatio = 1,
                        MinimumSeedTime = 172800 // 48 hours
                    };

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
