using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class TorrentDay : IndexerBase
    {
        public override string Id => "torrentday";
        public override string Name => "TorrentDay";
        public override string Description => "TorrentDay (TD) is a Private site for TV / MOVIES / GENERAL";
        public override string SiteLink { get; protected set; } = "https://tday.love/";
        public override string[] AlternativeSiteLinks => new[]
        {
            "https://tday.love/",
            "https://torrentday.cool/",
            "https://secure.torrentday.com/",
            "https://classic.torrentday.com/",
            "https://www.torrentday.com/",
            "https://www.torrentday.me/",
            "https://torrentday.it/",
            "https://td.findnemo.net/",
            "https://td.getcrazy.me/",
            "https://td.venom.global/",
            "https://td.workisboring.net/",
            "https://tday.findnemo.net/",
            "https://tday.getcrazy.me/",
            "https://tday.venom.global/",
            "https://tday.workisboring.net/"
        };
        public override string[] LegacySiteLinks => new[]
        {
            "https://torrentday.com/",
            "https://tdonline.org/", // redirect to https://www.torrentday.com/
            "https://torrentday.eu/", // redirect to https://www.torrentday.com/
            "https://td-update.com/", // redirect to https://www.torrentday.com/
            "https://www.torrentday.ru/",
            "https://www.td.af/"
        };
        public override string Language => "en-US";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private string SearchUrl => SiteLink + "t.json";

        private new ConfigurationDataCookie configData => (ConfigurationDataCookie)base.configData;

        public TorrentDay(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataCookie(
                       "Make sure you get the cookies from the same torrent day domain as configured above."))
        {
            wc.EmulateBrowser = false;

            configData.AddDynamic("freeleech", new BoolConfigurationItem("Search freeleech only") { Value = false });
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId, TvSearchParam.Genre
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q, MovieSearchParam.ImdbId, MovieSearchParam.Genre
                },
                MusicSearchParams = new List<MusicSearchParam>
                {
                    MusicSearchParam.Q
                },
                BookSearchParams = new List<BookSearchParam>
                {
                    BookSearchParam.Q
                }
            };

            caps.Categories.AddCategoryMapping(96, TorznabCatType.MoviesUHD, "Movie/4K");
            caps.Categories.AddCategoryMapping(25, TorznabCatType.MoviesSD, "Movies/480p");
            caps.Categories.AddCategoryMapping(11, TorznabCatType.MoviesBluRay, "Movies/Bluray");
            caps.Categories.AddCategoryMapping(5, TorznabCatType.MoviesBluRay, "Movies/Bluray-Full");
            caps.Categories.AddCategoryMapping(3, TorznabCatType.MoviesDVD, "Movies/DVD-R");
            caps.Categories.AddCategoryMapping(21, TorznabCatType.MoviesSD, "Movies/MP4");
            caps.Categories.AddCategoryMapping(22, TorznabCatType.MoviesForeign, "Movies/Non-English");
            caps.Categories.AddCategoryMapping(13, TorznabCatType.Movies, "Movies/Packs");
            caps.Categories.AddCategoryMapping(44, TorznabCatType.MoviesSD, "Movies/SD/x264");
            caps.Categories.AddCategoryMapping(48, TorznabCatType.Movies, "Movies/x265");
            caps.Categories.AddCategoryMapping(1, TorznabCatType.MoviesSD, "Movies/XviD");

            caps.Categories.AddCategoryMapping(24, TorznabCatType.TVSD, "TV/480p");
            caps.Categories.AddCategoryMapping(32, TorznabCatType.TVHD, "TV/Bluray");
            caps.Categories.AddCategoryMapping(31, TorznabCatType.TVSD, "TV/DVD-R");
            caps.Categories.AddCategoryMapping(33, TorznabCatType.TVSD, "TV/DVD-Rip");
            caps.Categories.AddCategoryMapping(46, TorznabCatType.TVSD, "TV/Mobile");
            caps.Categories.AddCategoryMapping(82, TorznabCatType.TVForeign, "TV/Non-English");
            caps.Categories.AddCategoryMapping(14, TorznabCatType.TV, "TV/Packs");
            caps.Categories.AddCategoryMapping(26, TorznabCatType.TVSD, "TV/SD/x264");
            caps.Categories.AddCategoryMapping(7, TorznabCatType.TVHD, "TV/x264");
            caps.Categories.AddCategoryMapping(34, TorznabCatType.TVUHD, "TV/x265");
            caps.Categories.AddCategoryMapping(2, TorznabCatType.TVSD, "TV/XviD");

            caps.Categories.AddCategoryMapping(4, TorznabCatType.PCGames, "PC/Games");
            caps.Categories.AddCategoryMapping(18, TorznabCatType.ConsolePS3, "PS");
            caps.Categories.AddCategoryMapping(8, TorznabCatType.ConsolePSP, "PSP");
            caps.Categories.AddCategoryMapping(10, TorznabCatType.ConsoleNDS, "Nintendo");
            caps.Categories.AddCategoryMapping(9, TorznabCatType.ConsoleXBox, "Xbox");

            caps.Categories.AddCategoryMapping(17, TorznabCatType.AudioMP3, "Music/Audio");
            caps.Categories.AddCategoryMapping(27, TorznabCatType.Audio, "Music/Flac");
            caps.Categories.AddCategoryMapping(23, TorznabCatType.AudioForeign, "Music/Non-English");
            caps.Categories.AddCategoryMapping(41, TorznabCatType.Audio, "Music/Packs");
            caps.Categories.AddCategoryMapping(16, TorznabCatType.AudioVideo, "Music/Video");

            caps.Categories.AddCategoryMapping(29, TorznabCatType.TVAnime, "Anime");
            caps.Categories.AddCategoryMapping(42, TorznabCatType.AudioAudiobook, "Audio Books");
            caps.Categories.AddCategoryMapping(20, TorznabCatType.Books, "Books");
            caps.Categories.AddCategoryMapping(102, TorznabCatType.BooksForeign, "Books/Non-English");
            caps.Categories.AddCategoryMapping(30, TorznabCatType.TVDocumentary, "Documentary");
            caps.Categories.AddCategoryMapping(95, TorznabCatType.TVDocumentary, "Educational");
            caps.Categories.AddCategoryMapping(47, TorznabCatType.Other, "Fonts");
            caps.Categories.AddCategoryMapping(43, TorznabCatType.PCMac, "Mac");
            caps.Categories.AddCategoryMapping(45, TorznabCatType.AudioOther, "Podcast");
            caps.Categories.AddCategoryMapping(28, TorznabCatType.PC, "Softwa/Packs");
            caps.Categories.AddCategoryMapping(12, TorznabCatType.PC, "Software");

            caps.Categories.AddCategoryMapping(19, TorznabCatType.XXX, "XXX/0Day");
            caps.Categories.AddCategoryMapping(6, TorznabCatType.XXX, "XXX/Movies");
            caps.Categories.AddCategoryMapping(15, TorznabCatType.XXXPack, "XXX/Packs");

            return caps;
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

            /* notes:
             * TorrentDay can search for genre (tags) using the default title&tags search
             * qf=
             * "" = Title and Tags
             * ta = Tags
             * all = Title, Tags & Description
             * adv = Advanced
             *
             * But only movies and tv have tags and the t.json does not return tags in results.
             */

            var cats = MapTorznabCapsToTrackers(query);
            if (cats.Count == 0)
                cats = GetAllTrackerCategories();
            var catStr = string.Join(";", cats);
            var searchUrl = SearchUrl + "?" + catStr;

            if (query.IsImdbQuery)
                searchUrl += ";q=" + query.ImdbID;
            else
            if (query.IsGenreQuery)
                searchUrl += ";q=" + WebUtilityHelpers.UrlEncode(query.GetQueryString() + " " + query.Genre, Encoding);
            else
                searchUrl += ";q=" + WebUtilityHelpers.UrlEncode(query.GetQueryString(), Encoding);

            if (((BoolConfigurationItem)configData.GetDynamic("freeleech")).Value)
                searchUrl += ";free=on";

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
                    var imdb = ParseUtil.GetImdbId(imdbId);

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
                        MinimumSeedTime = 259200 // 72 hours
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
