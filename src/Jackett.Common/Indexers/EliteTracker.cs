using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    internal class EliteTracker : BaseWebIndexer
    {
        private string LoginUrl => SiteLink + "takelogin.php";
        private string BrowseUrl => SiteLink + "browse.php";
        private new ConfigurationDataEliteTracker configData => (ConfigurationDataEliteTracker)base.configData;

        public EliteTracker(IIndexerConfigurationService configService, WebClient webClient, Logger logger,
            IProtectionService ps, ICacheService cs)
            : base(id: "elitetracker",
                   name: "Elite-Tracker",
                   description: "French Torrent Tracker",
                   link: "https://elite-tracker.net/",
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
                   logger: logger,
                   p: ps,
                   cacheService: cs,
                   client: webClient,
                   configData: new ConfigurationDataEliteTracker()
                )
        {
            Encoding = Encoding.UTF8;
            Language = "fr-fr";
            Type = "private";

            AddCategoryMapping(27, TorznabCatType.TVAnime, "Animation/Animes");
            AddCategoryMapping(90, TorznabCatType.TVAnime, "Animes - 3D");
            AddCategoryMapping(99, TorznabCatType.TVAnime, "Animes - 4K");
            AddCategoryMapping(63, TorznabCatType.TVAnime, "Animes - DVD");
            AddCategoryMapping(56, TorznabCatType.TVAnime, "Animes - HD");
            AddCategoryMapping(89, TorznabCatType.TVAnime, "Animes - HDRip");
            AddCategoryMapping(87, TorznabCatType.TVAnime, "Animes - Pack");
            AddCategoryMapping(88, TorznabCatType.TVAnime, "Animes - SD");
            AddCategoryMapping(59, TorznabCatType.TVAnime, "Animes - Serie");

            AddCategoryMapping(3, TorznabCatType.PC0day, "APPLICATION");
            AddCategoryMapping(74, TorznabCatType.PCMobileAndroid, "APPLICATION - ANDROID");
            AddCategoryMapping(57, TorznabCatType.PCMobileiOS, "APPLICATION - IPHONE");
            AddCategoryMapping(6, TorznabCatType.PC0day, "APPLICATION - LINUX");
            AddCategoryMapping(5, TorznabCatType.PCMac, "APPLICATION - MAC");
            AddCategoryMapping(4, TorznabCatType.PC0day, "APPLICATION - WINDOWS");

            AddCategoryMapping(38, TorznabCatType.TVDocumentary, "DOCUMENTAIRES");
            AddCategoryMapping(97, TorznabCatType.TVDocumentary, "DOCUMENTAIRES - PACK");

            AddCategoryMapping(34, TorznabCatType.Books, "EBOOKS");
            AddCategoryMapping(86, TorznabCatType.Books, "EBOOKS - ABOOKS");

            AddCategoryMapping(48, TorznabCatType.MoviesHD, "FiLMS HD");
            AddCategoryMapping(51, TorznabCatType.MoviesHD, "FiLMS HD - 1080p");
            AddCategoryMapping(98, TorznabCatType.MoviesUHD, "FiLMS HD - 2160p");
            AddCategoryMapping(70, TorznabCatType.Movies3D, "FiLMS HD - 3D");
            AddCategoryMapping(84, TorznabCatType.MoviesUHD, "FiLMS HD - 4K");
            AddCategoryMapping(50, TorznabCatType.MoviesHD, "FiLMS HD - 720P");
            AddCategoryMapping(49, TorznabCatType.MoviesBluRay, "FiLMS HD - BluRay");
            AddCategoryMapping(78, TorznabCatType.MoviesHD, "FiLMS HD - HDRip");
            AddCategoryMapping(95, TorznabCatType.Movies, "FiLMS HD - VOSTFR");
            AddCategoryMapping(85, TorznabCatType.MoviesHD, "FiLMS HD - x265");

            AddCategoryMapping(7, TorznabCatType.Movies, "FiLMS SD");
            AddCategoryMapping(91, TorznabCatType.Movies3D, "FiLMS SD - 3D");
            AddCategoryMapping(11, TorznabCatType.MoviesDVD, "FiLMS SD - DVD");
            AddCategoryMapping(53, TorznabCatType.MoviesSD, "FiLMS SD - DVD-SCREENER");
            AddCategoryMapping(9, TorznabCatType.MoviesDVD, "FiLMS SD - R5");
            AddCategoryMapping(8, TorznabCatType.MoviesSD, "FiLMS SD - SCREENER");
            AddCategoryMapping(10, TorznabCatType.MoviesSD, "FiLMS SD - SDRip");
            AddCategoryMapping(40, TorznabCatType.Movies, "FiLMS SD - VO");
            AddCategoryMapping(39, TorznabCatType.Movies, "FiLMS SD - VOSTFR");

            AddCategoryMapping(15, TorznabCatType.Console, "JEUX VIDEO");
            AddCategoryMapping(76, TorznabCatType.Console3DS, "JEUX VIDEO - 3DS");
            AddCategoryMapping(18, TorznabCatType.ConsoleNDS, "JEUX VIDEO - DS");
            AddCategoryMapping(55, TorznabCatType.PCMobileiOS, "JEUX VIDEO - IPHONE");
            AddCategoryMapping(80, TorznabCatType.PCGames, "JEUX VIDEO - LINUX");
            AddCategoryMapping(96, TorznabCatType.ConsoleOther, "JEUX VIDEO - NSW");
            AddCategoryMapping(79, TorznabCatType.PCMac, "JEUX VIDEO - OSX");
            AddCategoryMapping(22, TorznabCatType.PCGames, "JEUX VIDEO - PC");
            AddCategoryMapping(66, TorznabCatType.ConsolePS3, "JEUX VIDEO - PS2");
            AddCategoryMapping(58, TorznabCatType.ConsolePS3, "JEUX VIDEO - PS3");
            AddCategoryMapping(81, TorznabCatType.ConsolePS4, "JEUX VIDEO - PS4");
            AddCategoryMapping(20, TorznabCatType.ConsolePSP, "JEUX VIDEO - PSP");
            AddCategoryMapping(75, TorznabCatType.ConsolePS3, "JEUX VIDEO - PSX");
            AddCategoryMapping(19, TorznabCatType.ConsoleWii, "JEUX VIDEO - WII");
            AddCategoryMapping(83, TorznabCatType.ConsoleWiiU, "JEUX VIDEO - WiiU");
            AddCategoryMapping(16, TorznabCatType.ConsoleXBox, "JEUX VIDEO - XBOX");
            AddCategoryMapping(82, TorznabCatType.ConsoleXBoxOne, "JEUX VIDEO - XBOX ONE");
            AddCategoryMapping(17, TorznabCatType.ConsoleXBox360, "JEUX VIDEO - XBOX360");

            AddCategoryMapping(23, TorznabCatType.Audio, "MUSIQUES");
            AddCategoryMapping(26, TorznabCatType.Audio, "MUSIQUES - CLIP/CONCERT");
            AddCategoryMapping(61, TorznabCatType.AudioLossless, "MUSIQUES - FLAC");
            AddCategoryMapping(60, TorznabCatType.AudioMP3, "MUSIQUES - MP3");

            AddCategoryMapping(30, TorznabCatType.TV, "SERIES");
            AddCategoryMapping(77, TorznabCatType.TVSD, "SERIES - DVD");
            AddCategoryMapping(100, TorznabCatType.TVUHD, "SERIES - 4k");
            AddCategoryMapping(67, TorznabCatType.TVHD, "SERIES - FR HD");
            AddCategoryMapping(31, TorznabCatType.TVSD, "SERIES - FR SD");
            AddCategoryMapping(102, TorznabCatType.TVUHD, "SERIES - Pack 4k");
            AddCategoryMapping(92, TorznabCatType.TVHD, "SERIES - Pack FR HD");
            AddCategoryMapping(73, TorznabCatType.TVSD, "SERIES - Pack FR SD");
            AddCategoryMapping(94, TorznabCatType.TVHD, "SERIES - Pack VOSTFR HD");
            AddCategoryMapping(93, TorznabCatType.TVSD, "SERIES - Pack VOSTFR SD");
            AddCategoryMapping(68, TorznabCatType.TVHD, "SERIES - VO HD");
            AddCategoryMapping(32, TorznabCatType.TVSD, "SERIES - VO SD");
            AddCategoryMapping(101, TorznabCatType.TVUHD, "SERIES - 4k");
            AddCategoryMapping(69, TorznabCatType.TVHD, "SERIES - VOSTFR HD");
            AddCategoryMapping(33, TorznabCatType.TVSD, "SERIES - VOSTFR SD");

            AddCategoryMapping(47, TorznabCatType.TV, "SPECTACLES/EMISSIONS");
            AddCategoryMapping(71, TorznabCatType.TV, "SPECTACLES/EMISSIONS - Emissions");
            AddCategoryMapping(103, TorznabCatType.TV, "SPECTACLES/EMISSIONS - Emissions Pack");
            AddCategoryMapping(72, TorznabCatType.TV, "SPECTACLES/EMISSIONS - Spectacles");

            AddCategoryMapping(35, TorznabCatType.TVSport, "SPORT");
            AddCategoryMapping(36, TorznabCatType.TVSport, "SPORT - CATCH");
            AddCategoryMapping(65, TorznabCatType.TVSport, "SPORT - UFC");

            AddCategoryMapping(37, TorznabCatType.XXX, "XXX");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string>
            {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value }
            };

            var result = await RequestWithCookiesAsync(LoginUrl, "", RequestType.POST, data: pairs);

            await ConfigureIfOK(result.Cookies, result.Cookies != null, () =>
           {
               var errorMessage = result.ContentString;
               throw new ExceptionWithConfigData(errorMessage, configData);
           });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var pairs = new Dictionary<string, string>
            {
                {"do", "search"},
                {"search_type", query.IsImdbQuery ? "t_genre" : "t_name"},
                {"keywords", query.IsImdbQuery ? query.ImdbID : query.GetQueryString()},
                {"category", "0"} // multi cat search not supported
            };

            var results = await RequestWithCookiesAsync(BrowseUrl, method: RequestType.POST, data: pairs);
            if (results.IsRedirect)
            {
                // re-login
                await ApplyConfiguration(null);
                results = await RequestWithCookiesAsync(BrowseUrl, method: RequestType.POST, data: pairs);
            }

            try
            {
                var lastDate = DateTime.Now;

                var parser = new HtmlParser();
                var doc = parser.ParseDocument(results.ContentString);
                var rows = doc.QuerySelectorAll("table[id='sortabletable'] > tbody > tr");

                foreach (var row in rows.Skip(1))
                {
                    if (row.Children.Length != 9)
                        continue; // not a torrent line

                    var cat = row.Children[0].QuerySelector("a").GetAttribute("href").Split('=')[1];
                    var title = row.Children[1].QuerySelector("a").TextContent;
                    var qLinks = row.Children[2].QuerySelectorAll("a");
                    var link = new Uri(configData.TorrentHTTPSMode.Value ? qLinks[1].GetAttribute("href") : qLinks[0].GetAttribute("href"));
                    var details = new Uri(row.Children[1].QuerySelector("a").GetAttribute("href"));
                    var size = row.Children[4].TextContent;
                    var grabs = row.Children[5].QuerySelector("a").TextContent;
                    var seeders = ParseUtil.CoerceInt(row.Children[6].QuerySelector("a").TextContent);
                    var leechers = ParseUtil.CoerceInt(row.Children[7].QuerySelector("a").TextContent);
                    var qTags = row.Children[1].QuerySelector("div:has(span[style=\"float: right;\"])");
                    var dlVolumeFactor = 1.0;
                    if (qTags.QuerySelector("img[alt^=\"TORRENT GRATUIT\"]") != null)
                        dlVolumeFactor = 0.0;
                    else if (qTags.QuerySelector("img[alt^=\"TORRENT SILVER\"]") != null)
                        dlVolumeFactor = 0.5;

                    var upVolumeFactor = qTags.QuerySelector("img[alt^=\"TORRENT X2\"]") != null ? 2.0 : 1.0;
                    var release = new ReleaseInfo
                    {
                        MinimumRatio = 1,
                        MinimumSeedTime = 172800,
                        Category = MapTrackerCatToNewznab(cat),
                        Title = title,
                        Link = link,
                        Details = details,
                        Size = ReleaseInfo.GetBytes(size),
                        Seeders = seeders,
                        Grabs = ParseUtil.CoerceLong(grabs),
                        DownloadVolumeFactor = dlVolumeFactor,
                        UploadVolumeFactor = upVolumeFactor,
                        Peers = leechers + seeders,
                        Guid = link
                    };

                    var qTooltip = row.Children[1].QuerySelector("div.tooltip-content");
                    if (qTooltip != null)
                    {
                        var qPoster = qTooltip.QuerySelector("img");
                        if (qPoster != null)
                        {
                            release.Poster = new Uri(qPoster.GetAttribute("src"));
                            qPoster.Remove();
                        }

                        qTooltip.QuerySelector("div:contains(\"Total Hits\")").Remove();

                        var qLongTitle = qTooltip.QuerySelector("div");
                        release.Title = qLongTitle.TextContent;
                        qLongTitle.Remove();

                        var description = qTooltip.TextContent.Trim();
                        if (!string.IsNullOrWhiteSpace(description))
                            release.Description = description;
                    }

                    // issue #5064 replace multi keyword
                    if (!string.IsNullOrEmpty(configData.ReplaceMulti.Value))
                    {
                        var regex = new Regex("(?i)([\\.\\- ])MULTI([\\.\\- ])");
                        release.Title = regex.Replace(release.Title, "$1" + configData.ReplaceMulti.Value + "$2");
                    }

                    // issue #6855 Replace VOSTFR with ENGLISH
                    if (configData.Vostfr.Value)
                        release.Title = release.Title.Replace("VOSTFR", "ENGLISH").Replace("SUBFRENCH", "ENGLISH");

                    var qPretime = qTags.QuerySelector("font.mkprettytime");
                    if (qPretime != null)
                    {
                        if (release.Description == null)
                            release.Description = qPretime.TextContent;
                        else
                            release.Description += "<br>\n" + qPretime.TextContent;
                        release.PublishDate = lastDate;
                    }
                    else
                    {
                        release.PublishDate = DateTime.ParseExact(qTags.TextContent.Trim(), "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture);
                        lastDate = release.PublishDate;
                    }

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
