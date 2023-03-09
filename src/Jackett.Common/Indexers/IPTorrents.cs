using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class IPTorrents : IndexerBase
    {
        public override string Id => "iptorrents";
        public override string Name => "IPTorrents";
        public override string Description => "Always a step ahead.";
        public override string SiteLink { get; protected set; } = "https://iptorrents.com/";
        public override string[] AlternativeSiteLinks => new[]
        {
            "https://iptorrents.com/",
            "https://www.iptorrents.com/",
            "https://iptorrents.me/",
            "https://nemo.iptorrents.com/",
            "https://ipt.getcrazy.me/",
            "https://ipt.findnemo.net/",
            "https://ipt.beelyrics.net/",
            "https://ipt.venom.global/",
            "https://ipt.workisboring.net/",
            "https://ipt.lol/",
            "https://ipt.cool/",
            "https://ipt.world/"
        };
        public override string[] LegacySiteLinks => new[]
        {
            "https://ipt-update.com/",
            "http://ipt.read-books.org/",
            "http://alien.eating-organic.net/",
            "http://kong.net-freaks.com/",
            "http://ghost.cable-modem.org/",
            "http://logan.unusualperson.com/",
            "https://ipt.rocks/",
            "http://baywatch.workisboring.com/",
            "https://iptorrents.eu/"
        };
        public override string Language => "en-US";
        public override string Type => "private";

        public override bool SupportsPagination => true;

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private string SearchUrl => SiteLink + "t";

        private new ConfigurationDataCookieUA configData => (ConfigurationDataCookieUA)base.configData;

        public IPTorrents(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataCookieUA("For best results, change the 'Torrents per page' option to 100 and check the 'Torrents - Show files count' option in the website Settings."))
        {
            var sort = new SingleSelectConfigurationItem("Sort requested from site", new Dictionary<string, string>
                {
                    {"time", "created"},
                    {"size", "size"},
                    {"seeders", "seeders"},
                    {"name", "title"}
                })
            { Value = "time" };
            configData.AddDynamic("sort", sort);

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
                },
                TvSearchImdbAvailable = true
            };

            caps.Categories.AddCategoryMapping(72, TorznabCatType.Movies, "Movies");
            caps.Categories.AddCategoryMapping(87, TorznabCatType.Movies3D, "Movie/3D");
            caps.Categories.AddCategoryMapping(77, TorznabCatType.MoviesSD, "Movie/480p");
            caps.Categories.AddCategoryMapping(101, TorznabCatType.MoviesUHD, "Movie/4K");
            caps.Categories.AddCategoryMapping(89, TorznabCatType.MoviesBluRay, "Movie/BD-R");
            caps.Categories.AddCategoryMapping(90, TorznabCatType.MoviesHD, "Movie/BD-Rip");
            caps.Categories.AddCategoryMapping(96, TorznabCatType.MoviesSD, "Movie/Cam");
            caps.Categories.AddCategoryMapping(6, TorznabCatType.MoviesDVD, "Movie/DVD-R");
            caps.Categories.AddCategoryMapping(48, TorznabCatType.MoviesHD, "Movie/HD/Bluray");
            caps.Categories.AddCategoryMapping(54, TorznabCatType.Movies, "Movie/Kids");
            caps.Categories.AddCategoryMapping(62, TorznabCatType.MoviesSD, "Movie/MP4");
            caps.Categories.AddCategoryMapping(38, TorznabCatType.MoviesForeign, "Movie/Non-English");
            caps.Categories.AddCategoryMapping(68, TorznabCatType.Movies, "Movie/Packs");
            caps.Categories.AddCategoryMapping(20, TorznabCatType.MoviesWEBDL, "Movie/Web-DL");
            caps.Categories.AddCategoryMapping(100, TorznabCatType.MoviesHD, "Movie/x265");
            caps.Categories.AddCategoryMapping(7, TorznabCatType.MoviesSD, "Movie/Xvid");

            caps.Categories.AddCategoryMapping(73, TorznabCatType.TV, "TV");
            caps.Categories.AddCategoryMapping(26, TorznabCatType.TVDocumentary, "TV/Documentaries");
            caps.Categories.AddCategoryMapping(55, TorznabCatType.TVSport, "Sports");
            caps.Categories.AddCategoryMapping(78, TorznabCatType.TVSD, "TV/480p");
            caps.Categories.AddCategoryMapping(23, TorznabCatType.TVHD, "TV/BD");
            caps.Categories.AddCategoryMapping(24, TorznabCatType.TVSD, "TV/DVD-R");
            caps.Categories.AddCategoryMapping(25, TorznabCatType.TVSD, "TV/DVD-Rip");
            caps.Categories.AddCategoryMapping(66, TorznabCatType.TVSD, "TV/Mobile");
            caps.Categories.AddCategoryMapping(82, TorznabCatType.TVForeign, "TV/Non-English");
            caps.Categories.AddCategoryMapping(65, TorznabCatType.TV, "TV/Packs");
            caps.Categories.AddCategoryMapping(83, TorznabCatType.TVForeign, "TV/Packs/Non-English");
            caps.Categories.AddCategoryMapping(79, TorznabCatType.TVSD, "TV/SD/x264");
            caps.Categories.AddCategoryMapping(22, TorznabCatType.TVWEBDL, "TV/Web-DL");
            caps.Categories.AddCategoryMapping(5, TorznabCatType.TVHD, "TV/x264");
            caps.Categories.AddCategoryMapping(99, TorznabCatType.TVHD, "TV/x265");
            caps.Categories.AddCategoryMapping(4, TorznabCatType.TVSD, "TV/Xvid");

            caps.Categories.AddCategoryMapping(74, TorznabCatType.Console, "Games");
            caps.Categories.AddCategoryMapping(2, TorznabCatType.ConsoleOther, "Games/Mixed");
            caps.Categories.AddCategoryMapping(47, TorznabCatType.ConsoleOther, "Games/Nintendo");
            caps.Categories.AddCategoryMapping(43, TorznabCatType.PCGames, "Games/PC-ISO");
            caps.Categories.AddCategoryMapping(45, TorznabCatType.PCGames, "Games/PC-Rip");
            caps.Categories.AddCategoryMapping(71, TorznabCatType.ConsolePS4, "Games/Playstation");
            caps.Categories.AddCategoryMapping(50, TorznabCatType.ConsoleWii, "Games/Wii");
            caps.Categories.AddCategoryMapping(44, TorznabCatType.ConsoleXBox, "Games/Xbox");

            caps.Categories.AddCategoryMapping(75, TorznabCatType.Audio, "Music");
            caps.Categories.AddCategoryMapping(3, TorznabCatType.AudioMP3, "Music/Audio");
            caps.Categories.AddCategoryMapping(80, TorznabCatType.AudioLossless, "Music/Flac");
            caps.Categories.AddCategoryMapping(93, TorznabCatType.Audio, "Music/Packs");
            caps.Categories.AddCategoryMapping(37, TorznabCatType.AudioVideo, "Music/Video");
            caps.Categories.AddCategoryMapping(21, TorznabCatType.AudioOther, "Podcast");

            caps.Categories.AddCategoryMapping(76, TorznabCatType.Other, "Miscellaneous");
            caps.Categories.AddCategoryMapping(60, TorznabCatType.TVAnime, "Anime");
            caps.Categories.AddCategoryMapping(1, TorznabCatType.PC0day, "Appz");
            caps.Categories.AddCategoryMapping(86, TorznabCatType.PC0day, "Appz/Non-English");
            caps.Categories.AddCategoryMapping(64, TorznabCatType.AudioAudiobook, "AudioBook");
            caps.Categories.AddCategoryMapping(35, TorznabCatType.Books, "Books");
            caps.Categories.AddCategoryMapping(102, TorznabCatType.Books, "Books/Non-English");
            caps.Categories.AddCategoryMapping(94, TorznabCatType.BooksComics, "Comics");
            caps.Categories.AddCategoryMapping(95, TorznabCatType.BooksOther, "Educational");
            caps.Categories.AddCategoryMapping(98, TorznabCatType.Other, "Fonts");
            caps.Categories.AddCategoryMapping(69, TorznabCatType.PCMac, "Mac");
            caps.Categories.AddCategoryMapping(92, TorznabCatType.BooksMags, "Magazines / Newspapers");
            caps.Categories.AddCategoryMapping(58, TorznabCatType.PCMobileOther, "Mobile");
            caps.Categories.AddCategoryMapping(36, TorznabCatType.Other, "Pics/Wallpapers");

            caps.Categories.AddCategoryMapping(88, TorznabCatType.XXX, "XXX");
            caps.Categories.AddCategoryMapping(85, TorznabCatType.XXXOther, "XXX/Magazines");
            caps.Categories.AddCategoryMapping(8, TorznabCatType.XXX, "XXX/Movie");
            caps.Categories.AddCategoryMapping(81, TorznabCatType.XXX, "XXX/Movie/0Day");
            caps.Categories.AddCategoryMapping(91, TorznabCatType.XXXPack, "XXX/Packs");
            caps.Categories.AddCategoryMapping(84, TorznabCatType.XXXImageSet, "XXX/Pics/Wallpapers");

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
                throw new Exception("Your cookie did not work, make sure the user agent matches your computer: " + e.Message);
            }
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var validTagList = new List<string>
            {
                "action",
                "adventure",
                "animation",
                "biography",
                "comedy",
                "crime",
                "documentary",
                "drama",
                "family",
                "fantasy",
                "game-show",
                "history",
                "horror",
                "music",
                "musical",
                "mystery",
                "news",
                "reality-tv",
                "romance",
                "sci-fi",
                "sitcom",
                "sport",
                "talk-show",
                "thriller",
                "war",
                "western"
            };

            /* notes:
             * IPTorrents can search for genre (tags) using the default title&tags search
             * qf=
             * "" = Title and Tags
             * ti = Title
             * ta = Tags
             * all = Title, Tags & Description
             * adv = Advanced
             *
             * But only movies and tv have tags.
             */

            var headers = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(configData.UserAgent.Value))
                headers.Add("User-Agent", configData.UserAgent.Value);

            var qc = new NameValueCollection();

            foreach (var cat in MapTorznabCapsToTrackers(query))
                qc.Set(cat, string.Empty);

            if (((BoolConfigurationItem)configData.GetDynamic("freeleech")).Value)
                qc.Set("free", "on");

            var searchQuery = new List<string>();

            // IPT uses sphinx, which supports boolean operators and grouping
            if (query.IsImdbQuery)
                searchQuery.Add($"+({query.ImdbID})");
            else if (query.IsGenreQuery)
                searchQuery.Add($"+({query.Genre})");

            if (!string.IsNullOrWhiteSpace(query.GetQueryString()))
                searchQuery.Add($"+({query.GetQueryString()})");

            if (searchQuery.Any())
                qc.Set("q", $"{string.Join(" ", searchQuery)}");

            qc.Set("o", ((SingleSelectConfigurationItem)configData.GetDynamic("sort")).Value);

            if (query.Limit > 0 && query.Offset > 0)
            {
                var page = query.Offset / query.Limit + 1;
                qc.Add("p", page.ToString());
            }

            var searchUrl = SearchUrl + "?" + qc.GetQueryString();
            var response = await RequestWithCookiesAndRetryAsync(searchUrl, referer: SearchUrl, headers: headers);
            var results = response.ContentString;

            if (results == null || !results.Contains("/lout.php"))
                throw new Exception("The user is not logged in. It is possible that the cookie has expired or you made a mistake when copying it. Please check the settings.");

            if (string.IsNullOrWhiteSpace(query.ImdbID) && string.IsNullOrWhiteSpace(query.SearchTerm) && results.Contains("No Torrents Found!"))
                throw new Exception("Got No Torrents Found! Make sure your IPTorrents profile config contain proper default category settings.");

            char[] delimiters = { ',', ' ', '/', ')', '(', '.', ';', '[', ']', '"', '|', ':' };

            try
            {
                var parser = new HtmlParser();
                var doc = parser.ParseDocument(results);

                var rows = doc.QuerySelectorAll("table[id=\"torrents\"] > tbody > tr");
                foreach (var row in rows)
                {
                    var qTitleLink = row.QuerySelector("a.hv");
                    if (qTitleLink == null) // no results
                        continue;

                    var title = CleanTitle(qTitleLink.TextContent);
                    var details = new Uri(SiteLink + qTitleLink.GetAttribute("href").TrimStart('/'));

                    var qLink = row.QuerySelector("a[href^=\"/download.php/\"]");
                    var link = new Uri(SiteLink + qLink.GetAttribute("href").TrimStart('/'));

                    var descrSplit = row.QuerySelector("div.sub").TextContent.Split('|');
                    var dateSplit = descrSplit.Last().Split(new[] { " by " }, StringSplitOptions.None);
                    var publishDate = DateTimeUtil.FromTimeAgo(dateSplit.First());
                    var description = descrSplit.Length > 1 ? "Tags: " + descrSplit.First().Trim() : "";
                    description += dateSplit.Length > 1 ? " Uploaded by: " + dateSplit.Last().Trim() : "";
                    var releaseGenres = validTagList.Intersect(description.ToLower().Split(delimiters, StringSplitOptions.RemoveEmptyEntries)).ToList();

                    var catIcon = row.QuerySelector("td:nth-of-type(1) a");
                    if (catIcon == null)
                        // Torrents - Category column == Text or Code
                        // release.Category = MapTrackerCatDescToNewznab(row.Cq().Find("td:eq(0)").Text()); // Works for "Text" but only contains the parent category
                        throw new Exception("Please, change the 'Torrents - Category column' option to 'Icons' in the website Settings. Wait a minute (cache) and then try again.");
                    // Torrents - Category column == Icons
                    var cat = MapTrackerCatToNewznab(catIcon.GetAttribute("href").Substring(1));

                    var size = ParseUtil.GetBytes(row.Children[5].TextContent);

                    var colIndex = 6;
                    int? files = null;
                    if (row.Children.Length == 10) // files column is enabled in the site settings
                    {
                        files = ParseUtil.CoerceInt(row.Children[colIndex].TextContent.Replace("Go to files", ""));
                        colIndex++;
                    }
                    var grabs = ParseUtil.CoerceInt(row.Children[colIndex++].TextContent);
                    var seeders = ParseUtil.CoerceInt(row.Children[colIndex++].TextContent);
                    var leechers = ParseUtil.CoerceInt(row.Children[colIndex].TextContent);
                    var dlVolumeFactor = row.QuerySelector("span.free") != null ? 0 : 1;

                    var release = new ReleaseInfo
                    {
                        Title = title,
                        Details = details,
                        Guid = details,
                        Link = link,
                        PublishDate = publishDate,
                        Category = cat,
                        Description = description,
                        Genres = releaseGenres,
                        Size = size,
                        Files = files,
                        Grabs = grabs,
                        Seeders = seeders,
                        Peers = seeders + leechers,
                        DownloadVolumeFactor = dlVolumeFactor,
                        UploadVolumeFactor = 1,
                        MinimumRatio = 1,
                        MinimumSeedTime = 1209600 // 336 hours
                    };

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results, ex);
            }

            return releases;
        }

        private static string CleanTitle(string title)
        {
            // drop invalid chars that seems to have cropped up in some titles. #6582
            title = Regex.Replace(title, @"[\u0000-\u0008\u000A-\u001F\u0100-\uFFFF]", string.Empty, RegexOptions.Compiled);
            title = Regex.Replace(title, @"[\(\[\{]REQ(UEST(ED)?)?[\)\]\}]", string.Empty, RegexOptions.Compiled | RegexOptions.IgnoreCase);

            return title.Trim(' ', '-', ':');
        }
    }
}
