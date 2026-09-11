using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using AngleSharp.Html.Parser;
using Jackett.Common.Extensions;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class BigBbs : IndexerBase
    {
        public override string Id => "bigbbs";
        public override string Name => "BigBBS";
        public override string Description => "BigBBS is a POLISH Private Torrent Tracker for MOVIES / TV / GENERAL";
        public override string SiteLink { get; protected set; } = "https://bigbbs.eu/";
        public override string Language => "pl-PL";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private string LoginUrl => SiteLink + "?p=home&pid=1";
        private string SearchUrl => SiteLink + "?p=torrents&pid=10";

        private ConfigurationDataBigBbs _configData => (ConfigurationDataBigBbs)configData;

        public BigBbs(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
                      ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataBigBbs())
        {
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
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
            };

            // Movies
            caps.Categories.AddCategoryMapping(1, TorznabCatType.Movies, "Filmi");
            caps.Categories.AddCategoryMapping(14, TorznabCatType.MoviesSD, "Filmi DivX / XviD");
            caps.Categories.AddCategoryMapping(41, TorznabCatType.MoviesHD, "Filmi x264");
            caps.Categories.AddCategoryMapping(40, TorznabCatType.Movies, "Filmi Al / Lektor Amatorski");
            caps.Categories.AddCategoryMapping(39, TorznabCatType.MoviesBluRay, "Filmi BluRay");
            caps.Categories.AddCategoryMapping(147, TorznabCatType.Movies, "Filmi Xmas");
            caps.Categories.AddCategoryMapping(37, TorznabCatType.MoviesDVD, "Filmi DVD 5 / 9");
            caps.Categories.AddCategoryMapping(52, TorznabCatType.Movies3D, "Filmi 3D");
            caps.Categories.AddCategoryMapping(17, TorznabCatType.MoviesHD, "Filmi HD 1080p , 720p");
            caps.Categories.AddCategoryMapping(99, TorznabCatType.MoviesHD, "Filmi x265");
            caps.Categories.AddCategoryMapping(62, TorznabCatType.XXX, "Filmi XXX");
            caps.Categories.AddCategoryMapping(16, TorznabCatType.MoviesSD, "Filmi CAM / TS");
            caps.Categories.AddCategoryMapping(65, TorznabCatType.MoviesHD, "Filmi Rmvb");
            caps.Categories.AddCategoryMapping(66, TorznabCatType.Movies, "Filmi TV RIP");
            caps.Categories.AddCategoryMapping(84, TorznabCatType.TVDocumentary, "Filmi Dokumentalne");
            caps.Categories.AddCategoryMapping(100, TorznabCatType.Movies, "Filmi Seriale");
            caps.Categories.AddCategoryMapping(103, TorznabCatType.Movies, "Filmi FILMY GSM / PDA");
            caps.Categories.AddCategoryMapping(107, TorznabCatType.MoviesUHD, "Filmi 4K-UHD");
            caps.Categories.AddCategoryMapping(109, TorznabCatType.Movies, "Filmi Biblijny");
            caps.Categories.AddCategoryMapping(113, TorznabCatType.Movies, "Filmi Prawniczy");
            caps.Categories.AddCategoryMapping(61, TorznabCatType.Movies, "Bajki");
            caps.Categories.AddCategoryMapping(88, TorznabCatType.Movies3D, "Filmi 3D");
            caps.Categories.AddCategoryMapping(108, TorznabCatType.MoviesBluRay, "Filmi BluRay");
            caps.Categories.AddCategoryMapping(89, TorznabCatType.MoviesHD, "Filmi HD x264");
            caps.Categories.AddCategoryMapping(90, TorznabCatType.MoviesHD, "Filmi HD DivX / XviD");
            caps.Categories.AddCategoryMapping(91, TorznabCatType.MoviesDVD, "Filmi DVD 5 / DVD 9");
            caps.Categories.AddCategoryMapping(92, TorznabCatType.MoviesSD, "Filmi SD DivX / XviD");
            caps.Categories.AddCategoryMapping(93, TorznabCatType.MoviesSD, "Filmi SD x264");
            caps.Categories.AddCategoryMapping(96, TorznabCatType.Movies, "Filmi TVRip");
            caps.Categories.AddCategoryMapping(101, TorznabCatType.Movies, "Filmi Boxset");
            caps.Categories.AddCategoryMapping(98, TorznabCatType.Movies, "Filmi Seriale");
            caps.Categories.AddCategoryMapping(95, TorznabCatType.Movies, "Bajki");
            caps.Categories.AddCategoryMapping(97, TorznabCatType.AudioVideo, "Kabarety");
            caps.Categories.AddCategoryMapping(102, TorznabCatType.MoviesHD, "Filmi x265");
            caps.Categories.AddCategoryMapping(104, TorznabCatType.Movies, "Filmi FILMY GSM / PDA");
            caps.Categories.AddCategoryMapping(110, TorznabCatType.Movies, "Filmi Biblijny");
            caps.Categories.AddCategoryMapping(114, TorznabCatType.MoviesUHD, "Filmi 4K-UHD");
            caps.Categories.AddCategoryMapping(115, TorznabCatType.Movies, "Filmi Fan BBRG");
            caps.Categories.AddCategoryMapping(112, TorznabCatType.Movies, "Filmi ENG");

            // Comics & Books
            caps.Categories.AddCategoryMapping(106, TorznabCatType.BooksComics, "Manga");
            caps.Categories.AddCategoryMapping(47, TorznabCatType.BooksEBook, "EEbooki");
            caps.Categories.AddCategoryMapping(50, TorznabCatType.BooksEBook, "Ebook Pdf");
            caps.Categories.AddCategoryMapping(67, TorznabCatType.BooksComics, "Komiksy");

            // Anime
            caps.Categories.AddCategoryMapping(53, TorznabCatType.TVAnime, "Anime");

            // TV
            caps.Categories.AddCategoryMapping(56, TorznabCatType.TV, "TV");
            caps.Categories.AddCategoryMapping(57, TorznabCatType.TV, "TV BOXSETS");
            caps.Categories.AddCategoryMapping(58, TorznabCatType.TV, "TV EPIZODY");

            // Applications
            caps.Categories.AddCategoryMapping(6, TorznabCatType.PC, "Aplikacje");
            caps.Categories.AddCategoryMapping(18, TorznabCatType.PC0day, "Aplikacje Windows");
            caps.Categories.AddCategoryMapping(64, TorznabCatType.PCMobileOther, "Aplikacje GSM/PDA");
            caps.Categories.AddCategoryMapping(19, TorznabCatType.PCMac, "Aplikacje Macintosh");
            caps.Categories.AddCategoryMapping(42, TorznabCatType.PC, "Aplikacje Linux");

            // Sport
            caps.Categories.AddCategoryMapping(63, TorznabCatType.TVSport, "Sport");

            // Music
            caps.Categories.AddCategoryMapping(7, TorznabCatType.Audio, "Muzyka");
            caps.Categories.AddCategoryMapping(20, TorznabCatType.AudioVideo, "Koncert");
            caps.Categories.AddCategoryMapping(144, TorznabCatType.Audio, "BiGBBS RMG (Rellase Music Group)");
            caps.Categories.AddCategoryMapping(21, TorznabCatType.AudioMP3, "MP3");
            caps.Categories.AddCategoryMapping(38, TorznabCatType.AudioLossless, "FLAC");
            caps.Categories.AddCategoryMapping(135, TorznabCatType.Audio, "RetroRemix,ClubDance");
            caps.Categories.AddCategoryMapping(43, TorznabCatType.Audio, "Soundtrack");
            caps.Categories.AddCategoryMapping(136, TorznabCatType.Audio, "Rock");
            caps.Categories.AddCategoryMapping(80, TorznabCatType.AudioLossless, "lossless");
            caps.Categories.AddCategoryMapping(81, TorznabCatType.Audio, "MusicVid");
            caps.Categories.AddCategoryMapping(82, TorznabCatType.Audio, "Radio");
            caps.Categories.AddCategoryMapping(138, TorznabCatType.Audio, "Dyskografie Płytowe");
            caps.Categories.AddCategoryMapping(117, TorznabCatType.Audio, "Metal Rock");
            caps.Categories.AddCategoryMapping(139, TorznabCatType.Audio, "Kolekcje Muzyczne");
            caps.Categories.AddCategoryMapping(118, TorznabCatType.Audio, "Disco Polo");
            caps.Categories.AddCategoryMapping(119, TorznabCatType.Audio, "Clubbing");
            caps.Categories.AddCategoryMapping(120, TorznabCatType.Audio, "House");
            caps.Categories.AddCategoryMapping(116, TorznabCatType.Audio, "PHC");
            caps.Categories.AddCategoryMapping(125, TorznabCatType.Audio, "Elektro");
            caps.Categories.AddCategoryMapping(127, TorznabCatType.Audio, "Tranc");
            caps.Categories.AddCategoryMapping(128, TorznabCatType.Audio, "Dance");
            caps.Categories.AddCategoryMapping(130, TorznabCatType.Audio, "Opus");
            caps.Categories.AddCategoryMapping(129, TorznabCatType.Audio, "Pop");
            caps.Categories.AddCategoryMapping(131, TorznabCatType.Audio, "Italo");
            caps.Categories.AddCategoryMapping(133, TorznabCatType.Audio, "ClubDance");
            caps.Categories.AddCategoryMapping(134, TorznabCatType.Audio, "Retro Remixes");
            caps.Categories.AddCategoryMapping(146, TorznabCatType.Audio, "Techno");
            caps.Categories.AddCategoryMapping(132, TorznabCatType.Audio, "eurodance");
            caps.Categories.AddCategoryMapping(145, TorznabCatType.Audio, "Chillout");
            caps.Categories.AddCategoryMapping(83, TorznabCatType.Audio, "BLUES / REGGAE/ ROCK / METAL/CLASSIC/");
            caps.Categories.AddCategoryMapping(86, TorznabCatType.Audio, "Muzyka BBRG");

            // Games
            caps.Categories.AddCategoryMapping(2, TorznabCatType.Console, "Gry");
            caps.Categories.AddCategoryMapping(3, TorznabCatType.ConsolePS3, "Sony PS");
            caps.Categories.AddCategoryMapping(4, TorznabCatType.ConsoleWii, "Wii");
            caps.Categories.AddCategoryMapping(26, TorznabCatType.ConsoleXBox, "XboX");
            caps.Categories.AddCategoryMapping(27, TorznabCatType.PCGames, "Gry PC");
            caps.Categories.AddCategoryMapping(28, TorznabCatType.ConsoleNDS, "Nintendo");

            // Audiobooks
            caps.Categories.AddCategoryMapping(48, TorznabCatType.AudioAudiobook, "Audio Book");

            // Other
            caps.Categories.AddCategoryMapping(59, TorznabCatType.Other, "BBRG");

            return caps;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            try
            {
                var loginPage = await RequestWithCookiesAsync(LoginUrl);
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(loginPage.ContentString);

                var scripts = dom.QuerySelectorAll("script");

                var securityToken =
                    (from script in scripts
                     where script.TextContent.Contains("stKey:")
                     select Regex.Match(script.TextContent, "stKey: \"(.+?)\",")
                     into match
                     where match.Success
                     select match.Groups[1].Value).FirstOrDefault();

                if (securityToken.IsNullOrWhiteSpace())
                    throw new Exception("Could not find security token");

                var loginFormUrl = SiteLink + "ajax/login.php";
                var loginData = new Dictionary<string, string>
                {
                    { "action", "login" },
                    { "loginbox_membername", _configData.Username.Value },
                    { "loginbox_password", _configData.Password.Value },
                    { "loginbox_remember", "1" },
                    { "securitytoken", securityToken }
                };

                var response = await RequestWithCookiesAsync(loginFormUrl, method: RequestType.POST, data: loginData);

                if (response.ContentString.Contains("error") || response.ContentString.Contains("-ERROR-"))
                    throw new Exception("Invalid username or password");

                var searchResults = await PerformQuery(new TorznabQuery());
                if (!searchResults.Any())
                    throw new Exception("Found 0 results in the tracker");

                IsConfigured = true;
                SaveConfig();
                return IndexerConfigurationStatus.Completed;
            }
            catch (Exception ex)
            {
                IsConfigured = false;
                throw new Exception($"Configuration failed: {ex.Message}");
            }
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var cats = MapTorznabCapsToTrackers(query);

            var queryParams = cats
                              .Select(cat => new KeyValuePair<string, string>("cid[]", cat))
                              .ToList();

            var sort = _configData.Sort.Value;
            var type = _configData.Type.Value;
            var freeleech = _configData.Freeleech.Value;

            if (query.GetQueryString().IsNotNullOrWhiteSpace())
            {
                var keywords = Regex.Replace(query.GetQueryString(), "[^a-zA-Z0-9]+", "%25");
                queryParams.Add(new KeyValuePair<string, string>("keywords", keywords));
            }

            queryParams.Add(new KeyValuePair<string, string>("search_type", "name"));
            queryParams.Add(new KeyValuePair<string, string>("sortOptions[sortBy]", sort));
            queryParams.Add(new KeyValuePair<string, string>("sortOptions[sortOrder]", type));

            var searchUrl = SearchUrl + "&" + string.Join("&", queryParams.Select(x => $"{x.Key}={x.Value}"));
            var response = await RequestWithCookiesAsync(searchUrl);

            if (response.IsRedirect && response.RedirectingTo.Contains("login"))
                throw new Exception("The user is not logged in. It is possible that the cookie has expired or you made a mistake when copying it. Please check the settings.");

            try
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(response.ContentString);

                var selector = freeleech
                    ? "table#torrents_table_classic > tbody > tr:has(a[href*=\"?p=torrents&pid=10&action=download&tid=\"]):has(img[src$=\"/torrent_free.png\"])"
                    : "table#torrents_table_classic > tbody > tr:has(a[href*=\"?p=torrents&pid=10&action=download&tid=\"])";

                var rows = dom.QuerySelectorAll(selector);

                foreach (var row in rows)
                {
                    try
                    {
                        var categoryLink = row.QuerySelector("a[href*=\"?p=torrents&pid=10&cid=\"]");
                        var titleLink = row.QuerySelector("a[href*=\"?p=torrents&pid=10&action=details&tid=\"]");
                        var downloadLink = row.QuerySelector("a[href*=\"?p=torrents&pid=10&action=download&tid=\"]");
                        var sizeLink = row.QuerySelector("a[rel=\"torrent_size\"]");
                        var grabsLink = row.QuerySelector("a[rel=\"times_completed\"]");
                        var seedersLink = row.QuerySelector("a[rel=\"torrent_seeders\"]");
                        var leechersLink = row.QuerySelector("a[rel=\"torrent_leechers\"]");
                        var imdbLink = row.QuerySelector("a[href*=\"imdb.com/title/tt\"]");

                        if (titleLink == null || downloadLink == null)
                            continue;

                        var title = titleLink.TextContent.Trim();
                        if (!query.MatchQueryStringAND(title))
                            continue;

                        var categoryStr = categoryLink?.GetAttribute("href")?.Split(new[] { "cid=" }, StringSplitOptions.None).LastOrDefault() ?? "1";
                        var category = MapTrackerCatToNewznab(categoryStr);

                        var downloadUrl = downloadLink.GetAttribute("href");
                        var detailsUrl = titleLink.GetAttribute("href");

                        var size = ParseUtil.GetBytes(sizeLink?.TextContent ?? "0");
                        var grabs = ParseUtil.CoerceInt(grabsLink?.TextContent ?? "0");
                        var seeders = ParseUtil.CoerceInt(seedersLink?.TextContent ?? "0");
                        var leechers = ParseUtil.CoerceInt(leechersLink?.TextContent ?? "0");

                        var imdbId = ParseUtil.GetImdbId(imdbLink?.GetAttribute("href")) ?? 0;

                        var dateElem = row.QuerySelector("td.torrent_name");
                        var dateStr = dateElem?.TextContent ?? "";

                        dateStr = NormalizeDateString(dateStr);

                        var publishDate = ParsePublishDate(dateStr);

                        var isFreeleech = row.QuerySelector("img[src$=\"/torrent_free.png\"]") != null;

                        var release = new ReleaseInfo
                        {
                            Title = title,
                            Link = new Uri(downloadUrl),
                            Details = new Uri(detailsUrl),
                            Guid = new Uri(detailsUrl),
                            PublishDate = publishDate,
                            Category = category,
                            Size = size,
                            Grabs = grabs,
                            Seeders = seeders,
                            Peers = seeders + leechers,
                            Imdb = imdbId,
                            DownloadVolumeFactor = isFreeleech ? 0 : 1,
                            UploadVolumeFactor = 1,
                            MinimumRatio = 1.0,
                            MinimumSeedTime = 172800
                        };

                        releases.Add(release);
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Error parsing row: {ex.Message}");
                    }
                }

                if (response.ContentString.Contains("There are no results found."))
                    logger.Info("No results found");
            }
            catch (Exception ex)
            {
                OnParseError(response.ContentString, ex);
            }

            return releases;
        }

        private string NormalizeDateString(string dateStr)
        {
            if (dateStr.IsNullOrWhiteSpace())
                return dateStr;

            dateStr = Regex.Replace(dateStr, @"Wstawione", "Uploaded", RegexOptions.IgnoreCase);
            dateStr = Regex.Replace(dateStr, @"przez", "by", RegexOptions.IgnoreCase);

            var todayMatch = Regex.Match(dateStr, @"Uploaded (Today|Dzisiaj)\s+(?:at|o)\s+(\d{2}:\d{2}:\d{2})", RegexOptions.IgnoreCase);
            if (todayMatch.Success)
            {
                var time = todayMatch.Groups[2].Value;
                return $"Uploaded {DateTime.Now:dd-MM-yyyy} {time}";
            }

            var yesterdayMatch = Regex.Match(dateStr, @"Uploaded (Yesterday|Wczoraj)\s+(?:at|o)\s+(\d{2}:\d{2}:\d{2})", RegexOptions.IgnoreCase);
            if (yesterdayMatch.Success)
            {
                var time = yesterdayMatch.Groups[2].Value;
                return $"Uploaded {DateTime.Now.AddDays(-1):dd-MM-yyyy} {time}";
            }

            var momentAgoMatch = Regex.Match(dateStr, @"Uploaded (a moment ago|minutę temu)", RegexOptions.IgnoreCase);
            if (momentAgoMatch.Success)
            {
                var now = DateTime.Now;
                return $"Uploaded {now:dd-MM-yyyy} {now:HH:mm:ss}";
            }

            var hoursAgoMatch = Regex.Match(dateStr, @"Uploaded (Godzinę temu|One hour ago)", RegexOptions.IgnoreCase);
            if (hoursAgoMatch.Success)
            {
                var now = DateTime.Now;
                var hoursAgo = now.AddHours(-1);
                return $"Uploaded {hoursAgo:dd-MM-yyyy} {hoursAgo:HH:mm:ss}";
            }

            var minutesAgoMatch = Regex.Match(dateStr, @"Uploaded (\d+)\s+(?:minut\(?y?\)?|minutes)\s+(?:temu|ago)", RegexOptions.IgnoreCase);
            if (minutesAgoMatch.Success)
            {
                var minutes = int.Parse(minutesAgoMatch.Groups[1].Value);
                var minutesAgo = DateTime.Now.AddMinutes(-minutes);
                return $"Uploaded {minutesAgo:dd-MM-yyyy} {minutesAgo:HH:mm:ss}";
            }

            var days = new Dictionary<string, int>
            {
                { "Poniedziałek", 1 },
                { "Wtorek", 2 },
                { "Środa", 3 },
                { "Czwartek", 4 },
                { "Piątek", 5 },
                { "Sobota", 6 },
                { "Niedziela", 0 },
                { "Monday", 1 },
                { "Tuesday", 2 },
                { "Wednesday", 3 },
                { "Thursday", 4 },
                { "Friday", 5 },
                { "Saturday", 6 },
                { "Sunday", 0 }
            };

            var weekdayMatch = Regex.Match(dateStr, @"Uploaded (\w+)\s+(?:o|at)\s+(\d{2}:\d{2}:\d{2})", RegexOptions.IgnoreCase);
            if (weekdayMatch.Success)
            {
                var weekdayName = weekdayMatch.Groups[1].Value;
                var time = weekdayMatch.Groups[2].Value;

                foreach (var day in days)
                {
                    if (string.Equals(day.Key, weekdayName, StringComparison.OrdinalIgnoreCase))
                    {
                        var daysToSubtract = ((int)DateTime.Now.DayOfWeek - day.Value + 7) % 7;
                        if (daysToSubtract == 0)
                            daysToSubtract = 7;
                        var date = DateTime.Now.AddDays(-daysToSubtract);
                        return $"Uploaded {date:dd-MM-yyyy} {time}";
                    }
                }
            }

            return dateStr;
        }

        private DateTime ParsePublishDate(string dateStr)
        {
            if (dateStr.IsNullOrWhiteSpace())
                return DateTime.Now;

            var dateMatch = Regex.Match(dateStr, @"Uploaded (\d{1,2}-\d{1,2}-\d{4}) (\d{2}:\d{2}:\d{2})");

            if (dateMatch.Success)
            {
                var date = dateMatch.Groups[1].Value;
                var time = dateMatch.Groups[2].Value;
                return DateTime.ParseExact($"{date} {time}", "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture);
            }

            return DateTime.Now;
        }

        public override async Task<byte[]> Download(Uri link)
        {
            var torrentId = ExtractTorrentIdFromLink(link);

            if (torrentId.IsNotNullOrWhiteSpace())
                await SendThankYouAsync(torrentId);

            return await base.Download(link);
        }

        private string ExtractTorrentIdFromLink(Uri link)
        {
            if (link.Query.IsNullOrWhiteSpace())
                return null;

            try
            {
                var torrentId = HttpUtility.ParseQueryString(link.Query).Get("tid");
                if (torrentId.IsNotNullOrWhiteSpace())
                    return torrentId;
            }
            catch (Exception ex)
            {
                logger.Debug(ex, "Could not extract torrent ID from link");

            }

            return null;
        }

        private async Task SendThankYouAsync(string torrentId)
        {
            if (string.IsNullOrEmpty(torrentId))
                return;

            try
            {
                var thankUrl = SiteLink + "ajax/torrents.php";
                var securityToken = await GetSecurityTokenAsync();

                if (securityToken.IsNullOrWhiteSpace())
                {
                    logger.Warn("Could not retrieve security token for thank you request");
                    return;
                }

                var thankData = new Dictionary<string, string>
                {
                    { "action", "thank" },
                    { "tid", torrentId },
                    { "securitytoken", securityToken }
                };

                var response = await RequestWithCookiesAsync(thankUrl, method: RequestType.POST, data: thankData);

                if (response.ContentString.Contains("error") || response.ContentString.Contains("-ERROR-"))
                    logger.Warn($"Failed to send thank you for torrent {torrentId}");
                else
                    logger.Debug($"Thank you sent successfully for torrent {torrentId}");
            }
            catch (Exception ex)
            {
                logger.Warn($"Error sending thank you for torrent {torrentId}: {ex.Message}");
            }
        }

        private async Task<string> GetSecurityTokenAsync()
        {
            try
            {
                var loginPage = await RequestWithCookiesAsync(LoginUrl);
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(loginPage.ContentString);

                var scripts = dom.QuerySelectorAll("script");
                foreach (var script in scripts)
                {
                    if (!script.TextContent.Contains("stKey:"))
                        continue;

                    var match = Regex.Match(script.TextContent, "stKey: \"(.+?)\",");
                    if (match.Success)
                        return match.Groups[1].Value;
                }
            }
            catch (Exception ex)
            {
                logger.Warn($"Error retrieving security token: {ex.Message}");
            }

            return string.Empty;
        }
    }
}
