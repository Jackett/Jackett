using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
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
    public class IPTorrents : BaseWebIndexer
    {
        private string SearchUrl => SiteLink + "t";

        public override string[] AlternativeSiteLinks { get; protected set; } = {
            "https://iptorrents.com/",
            "https://www.iptorrents.com/",
            "https://iptorrents.eu/",
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

        public override string[] LegacySiteLinks { get; protected set; } = {
            "https://ipt-update.com/",
            "http://ipt.read-books.org/",
            "http://alien.eating-organic.net/",
            "http://kong.net-freaks.com/",
            "http://ghost.cable-modem.org/",
            "http://logan.unusualperson.com/",
            "https://ipt.rocks/",
            "http://baywatch.workisboring.com/"
        };

        private new ConfigurationDataCookie configData => (ConfigurationDataCookie)base.configData;

        public IPTorrents(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "iptorrents",
                   name: "IPTorrents",
                   description: "Always a step ahead.",
                   link: "https://iptorrents.com/",
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
                   configData: new ConfigurationDataCookie("For best results, change the 'Torrents per page' option to 100 and check the 'Torrents - Show files count' option in the website Settings."))
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";

            AddCategoryMapping(72, TorznabCatType.Movies, "Movies");
            AddCategoryMapping(87, TorznabCatType.Movies3D, "Movie/3D");
            AddCategoryMapping(77, TorznabCatType.MoviesSD, "Movie/480p");
            AddCategoryMapping(101, TorznabCatType.MoviesUHD, "Movie/4K");
            AddCategoryMapping(89, TorznabCatType.MoviesHD, "Movie/BD-R");
            AddCategoryMapping(90, TorznabCatType.MoviesSD, "Movie/BD-Rip");
            AddCategoryMapping(96, TorznabCatType.MoviesSD, "Movie/Cam");
            AddCategoryMapping(6, TorznabCatType.MoviesDVD, "Movie/DVD-R");
            AddCategoryMapping(48, TorznabCatType.MoviesBluRay, "Movie/HD/Bluray");
            AddCategoryMapping(54, TorznabCatType.Movies, "Movie/Kids");
            AddCategoryMapping(62, TorznabCatType.MoviesSD, "Movie/MP4");
            AddCategoryMapping(38, TorznabCatType.MoviesForeign, "Movie/Non-English");
            AddCategoryMapping(68, TorznabCatType.Movies, "Movie/Packs");
            AddCategoryMapping(20, TorznabCatType.MoviesHD, "Movie/Web-DL");
            AddCategoryMapping(7, TorznabCatType.MoviesSD, "Movie/Xvid");
            AddCategoryMapping(100, TorznabCatType.Movies, "Movie/x265");

            AddCategoryMapping(73, TorznabCatType.TV, "TV");
            AddCategoryMapping(26, TorznabCatType.TVDocumentary, "TV/Documentaries");
            AddCategoryMapping(55, TorznabCatType.TVSport, "Sports");
            AddCategoryMapping(78, TorznabCatType.TVSD, "TV/480p");
            AddCategoryMapping(23, TorznabCatType.TVHD, "TV/BD");
            AddCategoryMapping(24, TorznabCatType.TVSD, "TV/DVD-R");
            AddCategoryMapping(25, TorznabCatType.TVSD, "TV/DVD-Rip");
            AddCategoryMapping(66, TorznabCatType.TVSD, "TV/Mobile");
            AddCategoryMapping(82, TorznabCatType.TVForeign, "TV/Non-English");
            AddCategoryMapping(65, TorznabCatType.TV, "TV/Packs");
            AddCategoryMapping(83, TorznabCatType.TVForeign, "TV/Packs/Non-English");
            AddCategoryMapping(79, TorznabCatType.TVSD, "TV/SD/x264");
            AddCategoryMapping(22, TorznabCatType.TVWEBDL, "TV/Web-DL");
            AddCategoryMapping(5, TorznabCatType.TVHD, "TV/x264");
            AddCategoryMapping(99, TorznabCatType.TVHD, "TV/x265");
            AddCategoryMapping(4, TorznabCatType.TVSD, "TV/Xvid");

            AddCategoryMapping(74, TorznabCatType.Console, "Games");
            AddCategoryMapping(2, TorznabCatType.ConsoleOther, "Games/Mixed");
            AddCategoryMapping(47, TorznabCatType.ConsoleNDS, "Games/Nintendo DS");
            AddCategoryMapping(43, TorznabCatType.PCISO, "Games/PC-ISO");
            AddCategoryMapping(45, TorznabCatType.PCGames, "Games/PC-Rip");
            AddCategoryMapping(71, TorznabCatType.ConsolePS3, "Games/PS3");
            AddCategoryMapping(50, TorznabCatType.ConsoleWii, "Games/Wii");
            AddCategoryMapping(44, TorznabCatType.ConsoleXBox360, "Games/Xbox-360");

            AddCategoryMapping(75, TorznabCatType.Audio, "Music");
            AddCategoryMapping(3, TorznabCatType.AudioMP3, "Music/Audio");
            AddCategoryMapping(80, TorznabCatType.AudioLossless, "Music/Flac");
            AddCategoryMapping(93, TorznabCatType.Audio, "Music/Packs");
            AddCategoryMapping(37, TorznabCatType.AudioVideo, "Music/Video");
            AddCategoryMapping(21, TorznabCatType.AudioVideo, "Podcast");

            AddCategoryMapping(76, TorznabCatType.Other, "Other/Miscellaneous");
            AddCategoryMapping(60, TorznabCatType.TVAnime, "Anime");
            AddCategoryMapping(1, TorznabCatType.PC0day, "Appz");
            AddCategoryMapping(86, TorznabCatType.PC0day, "Appz/Non-English");
            AddCategoryMapping(64, TorznabCatType.AudioAudiobook, "AudioBook");
            AddCategoryMapping(35, TorznabCatType.Books, "Books");
            AddCategoryMapping(94, TorznabCatType.BooksComics, "Books/Comics");
            AddCategoryMapping(95, TorznabCatType.BooksOther, "Books/Educational");
            AddCategoryMapping(98, TorznabCatType.Other, "Other/Fonts");
            AddCategoryMapping(69, TorznabCatType.PCMac, "Appz/Mac");
            AddCategoryMapping(92, TorznabCatType.BooksMags, "Books/Magazines & Newspapers");
            AddCategoryMapping(58, TorznabCatType.PCMobileOther, "Appz/Mobile");
            AddCategoryMapping(36, TorznabCatType.Other, "Other/Pics/Wallpapers");

            AddCategoryMapping(88, TorznabCatType.XXX, "XXX");
            AddCategoryMapping(85, TorznabCatType.XXXOther, "XXX/Magazines");
            AddCategoryMapping(8, TorznabCatType.XXX, "XXX/Movie");
            AddCategoryMapping(81, TorznabCatType.XXX, "XXX/Movie/0Day");
            AddCategoryMapping(91, TorznabCatType.XXXPack, "XXX/Packs");
            AddCategoryMapping(84, TorznabCatType.XXXImageSet, "XXX/Pics/Wallpapers");
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

            var qc = new NameValueCollection();

            if (query.IsImdbQuery)
                qc.Add("q", query.ImdbID);
            else if (!string.IsNullOrWhiteSpace(query.GetQueryString()))
                qc.Add("q", query.GetQueryString());

            foreach (var cat in MapTorznabCapsToTrackers(query))
                qc.Add(cat, string.Empty);

            var searchUrl = SearchUrl + "?" + qc.GetQueryString();
            var response = await RequestWithCookiesAndRetryAsync(searchUrl, referer: SearchUrl);
            var results = response.ContentString;

            if (results == null || !results.Contains("/lout.php"))
                throw new Exception("The user is not logged in. It is possible that the cookie has expired or you made a mistake when copying it. Please check the settings.");

            if (string.IsNullOrWhiteSpace(query.ImdbID) && string.IsNullOrWhiteSpace(query.SearchTerm) && results.Contains("No Torrents Found!"))
                throw new Exception("Got No Torrents Found! Make sure your IPTorrents profile config contain proper default category settings.");

            try
            {
                var parser = new HtmlParser();
                var doc = parser.ParseDocument(results);

                var rows = doc.QuerySelectorAll("table[id='torrents'] > tbody > tr");
                foreach (var row in rows)
                {
                    var qTitleLink = row.QuerySelector("a.hv");
                    if (qTitleLink == null) // no results
                        continue;

                    // drop invalid char that seems to have cropped up in some titles. #6582
                    var title = qTitleLink.TextContent.Trim().Replace("\u000f", "");
                    var details = new Uri(SiteLink + qTitleLink.GetAttribute("href").TrimStart('/'));

                    var qLink = row.QuerySelector("a[href^=\"/download.php/\"]");
                    var link = new Uri(SiteLink + qLink.GetAttribute("href").TrimStart('/'));

                    var descrSplit = row.QuerySelector("div.sub").TextContent.Split('|');
                    var dateSplit = descrSplit.Last().Split(new [] {" by "}, StringSplitOptions.None);
                    var publishDate = DateTimeUtil.FromTimeAgo(dateSplit.First());
                    var description = descrSplit.Length > 1 ? "Tags: " + descrSplit.First().Trim() : "";
                    description += dateSplit.Length > 1 ? " Uploaded by: " + dateSplit.Last().Trim() : "";

                    var catIcon = row.QuerySelector("td:nth-of-type(1) a");
                    if (catIcon == null)
                        // Torrents - Category column == Text or Code
                        // release.Category = MapTrackerCatDescToNewznab(row.Cq().Find("td:eq(0)").Text()); // Works for "Text" but only contains the parent category
                        throw new Exception("Please, change the 'Torrents - Category column' option to 'Icons' in the website Settings. Wait a minute (cache) and then try again.");
                    // Torrents - Category column == Icons
                    var cat = MapTrackerCatToNewznab(catIcon.GetAttribute("href").Substring(1));

                    var size = ReleaseInfo.GetBytes(row.Children[5].TextContent);

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
    }
}
