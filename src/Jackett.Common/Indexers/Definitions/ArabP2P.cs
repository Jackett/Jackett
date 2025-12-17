using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class ArabP2P : IndexerBase
    {
        public override string Id => "arabp2p";
        public override string Name => "ArabP2P";
        public override string Description => "ArabP2P is an ARABIC Private Torrent Tracker for MOVIES / TV / GENERAL with enhanced episode parsing";
        public override string SiteLink { get; protected set; } = "https://www.arabp2p.net/";
        public override string[] LegacySiteLinks => new[]
        {
            "http://www.arabp2p.com/",
            "https://www.arabp2p.com/",
        };
        public override Encoding Encoding => Encoding.UTF8;
        public override string Language => "ar-AE";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private readonly Logger Logger;
        private readonly WebClient Client;

        private ConfigurationDataBasicLogin ConfigData => (ConfigurationDataBasicLogin)base.configData;

        private readonly TitleParser _titleParser = new TitleParser();
        private string LoginUrl => SiteLink + "index.php";
        private string LoginSubmitUrl => SiteLink + "index.php?page=login";
        private string SearchUrl => SiteLink + "index.php";

        public ArabP2P(IIndexerConfigurationService configService, WebClient wc, Logger l,
            IProtectionService ps, ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataBasicLogin())
        {
            this.Logger = l;
            this.Client = wc;
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

            // Categories from arabp2p.yml
            caps.Categories.AddCategoryMapping(14, TorznabCatType.Other, "اسلامي (Islamic)");
            caps.Categories.AddCategoryMapping(19, TorznabCatType.TVDocumentary, "وثائقي (Documentary)");
            caps.Categories.AddCategoryMapping(70, TorznabCatType.TV, "تعليمي (Educational)");
            caps.Categories.AddCategoryMapping(41, TorznabCatType.Movies, "افلام عربيه (Arabic Movies)");
            caps.Categories.AddCategoryMapping(88, TorznabCatType.Movies, "افلام مدبلجه عربي (Arabic Dubbed Movies)");
            caps.Categories.AddCategoryMapping(44, TorznabCatType.TV, "مسلسلات عربية (Arabic Series)");
            caps.Categories.AddCategoryMapping(89, TorznabCatType.TV, "مسلسلات عربية كامله (Full Arabic Series)");
            caps.Categories.AddCategoryMapping(52, TorznabCatType.TV, "مسرحيات (Plays)");
            caps.Categories.AddCategoryMapping(71, TorznabCatType.TV, "مسلسلات مدبلجه عربي (Arabic Dubbed Series)");
            caps.Categories.AddCategoryMapping(90, TorznabCatType.TV, "برامج ومسابقات (Shows)");
            caps.Categories.AddCategoryMapping(92, TorznabCatType.TVForeign, "تعليمي (Educational)");
            caps.Categories.AddCategoryMapping(93, TorznabCatType.TVDocumentary, "وثائقي (Documentary)");
            caps.Categories.AddCategoryMapping(45, TorznabCatType.TVForeign, "مسلسلات وبرامج اجنبيه (Serials)");
            caps.Categories.AddCategoryMapping(57, TorznabCatType.TVForeign, "مسلسلات آسيوية (Asian Series)");
            caps.Categories.AddCategoryMapping(42, TorznabCatType.MoviesForeign, "افلام اجنبيه (Foreign)");
            caps.Categories.AddCategoryMapping(74, TorznabCatType.MoviesHD, "جودة عالية HD");
            caps.Categories.AddCategoryMapping(113, TorznabCatType.TVForeign, "مسلسلات لاتينية مترجم.مدبلج(Latin Series");
            caps.Categories.AddCategoryMapping(59, TorznabCatType.MoviesForeign, "افلام آسيوية (Asian Movies)");
            caps.Categories.AddCategoryMapping(86, TorznabCatType.MoviesForeign, "افلام هنديه (Indian Movies)");
            caps.Categories.AddCategoryMapping(114, TorznabCatType.MoviesForeign, "افلام لاتينية مترجم.مدبلج (Latin Movies)");
            caps.Categories.AddCategoryMapping(115, TorznabCatType.TVForeign, "مسلسلات تركية مترجم.مدبلج (Turkish Series)");
            caps.Categories.AddCategoryMapping(116, TorznabCatType.MoviesForeign, "افلام تركية مترجم.مدبلج (Turkish Movies)");
            caps.Categories.AddCategoryMapping(98, TorznabCatType.TVAnime, "افلام (Movies)");
            caps.Categories.AddCategoryMapping(100, TorznabCatType.TVAnime, "مسلسلات (Series)");
            caps.Categories.AddCategoryMapping(102, TorznabCatType.TVAnime, "حلقات (Episdoes)");
            caps.Categories.AddCategoryMapping(99, TorznabCatType.TVAnime, "افلام (Movies)");
            caps.Categories.AddCategoryMapping(101, TorznabCatType.TVAnime, "مسلسلات (Series)");
            caps.Categories.AddCategoryMapping(103, TorznabCatType.TVAnime, "حلقات (Episodes)");
            caps.Categories.AddCategoryMapping(85, TorznabCatType.TVAnime, "الكارتون الصامت والكلاسيكي (Cartoons)");
            caps.Categories.AddCategoryMapping(25, TorznabCatType.Audio, "القران الكريم (The Holy Quran)");
            caps.Categories.AddCategoryMapping(27, TorznabCatType.Audio, "محاضرات (Lectures)");
            caps.Categories.AddCategoryMapping(26, TorznabCatType.Audio, "اناشيد (Chants)");
            caps.Categories.AddCategoryMapping(118, TorznabCatType.Audio, "برامج صوتية (Programs)");
            caps.Categories.AddCategoryMapping(22, TorznabCatType.PC, "برامج عربية (Arabic Software)");
            caps.Categories.AddCategoryMapping(23, TorznabCatType.PC, "برامج عامه (Public Software)");
            caps.Categories.AddCategoryMapping(17, TorznabCatType.Books, "كتب (Books)");
            caps.Categories.AddCategoryMapping(65, TorznabCatType.Other, "صور (Images)");
            caps.Categories.AddCategoryMapping(56, TorznabCatType.Other, "رياضي (Sport)");
            caps.Categories.AddCategoryMapping(46, TorznabCatType.Other, "منوع (Misc)");

            return caps;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var parser = new HtmlParser();
            var loginPage = await RequestWithCookiesAndRetryAsync(LoginUrl);

            if (string.IsNullOrWhiteSpace(loginPage.ContentString))
            {
                throw new ExceptionWithConfigData("Unable to load the login page.", configData);
            }

            using var loginDocument = parser.ParseDocument(loginPage.ContentString);
            var loginForm = loginDocument.QuerySelector("form[action^=\"index.php?page=login\"]") ??
                            loginDocument.QuerySelector("form[action*=\"page=login\"]");

            if (loginForm == null)
            {
                throw new ExceptionWithConfigData("Unable to locate the login form.", configData);
            }

            var pairs = new Dictionary<string, string>();
            var inputs = loginForm.QuerySelectorAll("input");

            if (inputs == null || inputs.Length == 0)
            {
                throw new ExceptionWithConfigData("Unable to locate the login inputs.", configData);
            }

            foreach (var input in inputs)
            {
                var name = input.GetAttribute("name");
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                if (input is IHtmlInputElement htmlInput)
                {
                    if (htmlInput.IsDisabled)
                    {
                        continue;
                    }

                    if ((htmlInput.Type == InputTypeNames.Checkbox || htmlInput.Type == InputTypeNames.Radio) && !htmlInput.IsChecked)
                    {
                        continue;
                    }

                    pairs[name] = htmlInput.Value ?? string.Empty;
                    continue;
                }

                pairs[name] = input.GetAttribute("value") ?? string.Empty;
            }

            pairs["uid"] = ConfigData.Username.Value;
            pairs["pwd"] = ConfigData.Password.Value;

            var action = loginForm.GetAttribute("action")?.Trim();
            var loginEndpoint = string.IsNullOrEmpty(action)
                ? LoginSubmitUrl
                : new Uri(new Uri(SiteLink), action).ToString();

            var result = await RequestLoginAndFollowRedirect(loginEndpoint, pairs, loginPage.Cookies, true, null, LoginUrl, true);
            await ConfigureIfOK(result.Cookies, result.ContentString != null && result.ContentString.Contains("logout.php?t="), () =>
            {
                var errorParser = new HtmlParser();
                using var document = errorParser.ParseDocument(result.ContentString);
                var errorMessage = document.QuerySelector("tr td span[style=\"color:#FF0000;\"]")?.TextContent;

                throw new ExceptionWithConfigData(errorMessage ?? "Login failed.", configData);
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchUrl = SearchUrl;

            // Transform search query from standard format to Arabic format
            // S02E209 → [209][م2] or Sonarr's query.Season/Episode properties
            var searchString = query.SanitizedSearchTerm;

            // If Sonarr provides season/episode, append in Arabic format
            if (query.Season > 0 && query.Episode != null)
            {
                // Transform: "Show Name" + S02E09 → "Show Name [09][م2]"
                searchString += $" [{query.Episode}][م{query.Season}]";
            }
            else if (query.Season > 0)
            {
                // Just season: "Show Name" + S02 → "Show Name [م2]"
                searchString += $" [م{query.Season}]";
            }
            else
            {
                // No season/episode info, try to parse from search string
                // Transform patterns like "Show Name S02E09" → "Show Name [09][م2]"
                var seasonEpisodeMatch = Regex.Match(searchString, @"S(\d+)E(\d+)", RegexOptions.IgnoreCase);
                if (seasonEpisodeMatch.Success)
                {
                    var season = seasonEpisodeMatch.Groups[1].Value;
                    var episode = seasonEpisodeMatch.Groups[2].Value;
                    searchString = Regex.Replace(searchString, @"S\d+E\d+", $"[{episode}][م{season}]", RegexOptions.IgnoreCase);
                }
                else
                {
                    // Also handle 2x09 format → [09][م2]
                    var altMatch = Regex.Match(searchString, @"(\d+)x(\d+)", RegexOptions.IgnoreCase);
                    if (altMatch.Success)
                    {
                        var season = altMatch.Groups[1].Value;
                        var episode = altMatch.Groups[2].Value;
                        searchString = Regex.Replace(searchString, @"\d+x\d+", $"[{episode}][م{season}]", RegexOptions.IgnoreCase);
                    }
                }
            }

            var queryCollection = new NameValueCollection
            {
                { "page", "torrents" },
                { "search", searchString.Trim() },
                { "category", "0" },
                { "active", "0" },
                { "internel", "0" },
                { "order", "3" },
                { "by", "2" }
            };

            searchUrl += "?" + queryCollection.GetQueryString();
            var results = await RequestWithCookiesAndRetryAsync(searchUrl);

            if (!results.ContentString.Contains("logout.php?t="))
            {
                await ApplyConfiguration(null);
                results = await RequestWithCookiesAndRetryAsync(searchUrl);
            }

            try
            {
                var parser = new HtmlParser();
                using var dom = parser.ParseDocument(results.ContentString);
                var rows = dom.QuerySelectorAll("table#torrents_list_p > tbody > tr:has(a[href^=\"download.php?id=\"]), table#torrents_list_p > tbody > tr:has(a[href^=\"magnet:?xt=\"])");

                foreach (var row in rows)
                {
                    var qCategoryLink = row.QuerySelector("a[href^=\"index.php?page=torrents&category=\"]:last-child");
                    var categoryId = ParseUtil.GetArgumentFromQueryString(qCategoryLink.GetAttribute("href"), "category");

                    var qDetailsLink = row.QuerySelector("a[href^=\"index.php?page=torrent-details\"]");
                    var title = qDetailsLink.TextContent.Trim();
                    var details = new Uri(SiteLink + qDetailsLink.GetAttribute("href"));

                    var qDownloadLink = row.QuerySelector("a[href^=\"download.php?id=\"]");
                    var qMagnetLink = row.QuerySelector("a[href^=\"magnet:?xt=\"]");

                    var dateSelector = row.QuerySelector("span.upload-date > span");
                    var dateStr = dateSelector?.GetAttribute("title") ?? "";
                    DateTime publishDate = DateTime.Now;
                    if (!string.IsNullOrEmpty(dateStr))
                    {
                        DateTime.TryParseExact(dateStr, "MM-yy-dd HH:mm:ss tt",
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None, out publishDate);
                    }

                    var size = ParseUtil.GetBytes(row.QuerySelector("span.size")?.TextContent);
                    var seeders = ParseUtil.CoerceInt(row.QuerySelector("span[title=\"Seeders\"]")?.TextContent);
                    var leechers = ParseUtil.CoerceInt(row.QuerySelector("span[title=\"Leechers\"]")?.TextContent);

                    var dlVolumeFactor = row.QuerySelector("span.free") != null ? 0.0 : 1.0;

                    var category = MapTrackerCatToNewznab(categoryId);
                    var isTvCategory = category.Contains(TorznabCatType.TV.ID) ||
                                     TorznabCatType.TV.SubCategories.Any(subCat => category.Contains(subCat.ID));

                    // Parse Arabic episode format for TV categories
                    if (isTvCategory)
                    {
                        title = _titleParser.Parse(title);
                    }

                    var release = new ReleaseInfo
                    {
                        Title = title,
                        Details = details,
                        Guid = details,
                        Link = qDownloadLink != null ? new Uri(SiteLink + qDownloadLink.GetAttribute("href")) : null,
                        MagnetUri = qMagnetLink != null ? new Uri(qMagnetLink.GetAttribute("href")) : null,
                        PublishDate = publishDate,
                        Category = category,
                        Size = size,
                        Seeders = seeders,
                        Peers = leechers + seeders,
                        DownloadVolumeFactor = dlVolumeFactor,
                        UploadVolumeFactor = 1,
                        MinimumRatio = 0.8,
                        MinimumSeedTime = 259200 // 3 days
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

        public class TitleParser
        {
            // Pattern: [09][م1] or [04-05-06][م1] or [209-210][2025][م2]
            // With optional space between brackets: [08] [2025][م6]
            // م is Arabic letter for مَوسِم (Season)
            private readonly Regex _arabicSeasonEpisodeRegex = new Regex(
                @"\[(\d+(?:[\s-]+\d+)*)\]\s*(?:\[(\d{4})\]\s*)?\[م(\d+)\]",
                RegexOptions.Compiled | RegexOptions.IgnoreCase
            );

            // Pattern for just episodes without season marker: [15] or [04-05-06]
            private readonly Regex _arabicEpisodeOnlyRegex = new Regex(
                @"^\[(\d+(?:[\s-]+\d+)*)\](?!\s*\[م)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase
            );

            public string Parse(string title)
            {
                var originalTitle = title;

                try
                {
                    // Match Arabic season/episode format: [episodes][year?][season]
                    var seasonEpisodeMatch = _arabicSeasonEpisodeRegex.Match(title);

                    if (seasonEpisodeMatch.Success)
                    {
                        var episodes = seasonEpisodeMatch.Groups[1].Value;
                        var year = seasonEpisodeMatch.Groups[2].Value; // Optional year
                        var season = seasonEpisodeMatch.Groups[3].Value;

                        // Convert episode format
                        var episodeTag = ConvertEpisodeFormat(episodes, season);

                        // Build replacement: episodeTag [year] (no extra spaces, just preserve year)
                        var replacement = string.IsNullOrEmpty(year)
                            ? episodeTag + " "
                            : episodeTag + " [" + year + "]";

                        title = _arabicSeasonEpisodeRegex.Replace(title, replacement, 1);
                    }
                    else
                    {
                        // Check for episode-only format (no season marker)
                        var episodeOnlyMatch = _arabicEpisodeOnlyRegex.Match(title);
                        if (episodeOnlyMatch.Success)
                        {
                            var episodes = episodeOnlyMatch.Groups[1].Value;
                            var episodeTag = ConvertEpisodeFormat(episodes, "1"); // Assume S01 if no season specified
                            title = _arabicEpisodeOnlyRegex.Replace(title, episodeTag + " ", 1);
                        }
                    }

                    // Clean up extra brackets and spaces
                    title = Regex.Replace(title, @"\s+", " ");
                    title = Regex.Replace(title, @"\[\s*\]", "");
                    title = title.Trim();

                    return title;
                }
                catch (Exception)
                {
                    // If parsing fails, return original title
                    return originalTitle;
                }
            }

            private string ConvertEpisodeFormat(string episodes, string season)
            {
                var seasonTag = "S" + season.PadLeft(2, '0');

                // Handle episode ranges: 04-05-06 or 209-210
                var normalizedEpisodes = Regex.Replace(episodes.Trim(), @"\s+", "-");

                if (normalizedEpisodes.Contains("-"))
                {
                    var episodeParts = normalizedEpisodes.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);

                    // Check if it's a range (e.g., 209-210) or multiple episodes (e.g., 04-05-06)
                    if (episodeParts.Length == 2)
                    {
                        // Range format: 209-210 -> S02E209-E210
                        var startEp = episodeParts[0].PadLeft(2, '0');
                        var endEp = episodeParts[1].PadLeft(2, '0');
                        return $"{seasonTag}E{startEp}-E{endEp}";
                    }
                    else
                    {
                        // Multiple episodes: 04-05-06 -> S01E04-E05-E06
                        var episodeTags = episodeParts.Select(ep => "E" + ep.PadLeft(2, '0'));
                        return seasonTag + string.Join("-", episodeTags);
                    }
                }
                else
                {
                    // Single episode: 09 -> S01E09
                    return seasonTag + "E" + normalizedEpisodes.PadLeft(2, '0');
                }
            }
        }
    }
}
