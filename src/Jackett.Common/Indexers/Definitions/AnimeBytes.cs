using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jackett.Common.Extensions;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Serializer;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class AnimeBytes : BaseCachingWebIndexer
    {
        public override string Id => "animebytes";
        public override string Name => "AnimeBytes";
        public override string Description => "Powered by Tentacles";
        public override string SiteLink { get; protected set; } = "https://animebytes.tv/";
        public override string Language => "en-US";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private string ScrapeUrl => SiteLink + "scrape.php";
        private bool AllowRaws => ConfigData.IncludeRaw.Value;
        private bool PadEpisode => ConfigData.PadEpisode is { Value: true };
        private bool AddJapaneseTitle => ConfigData.AddJapaneseTitle.Value;
        private bool AddRomajiTitle => ConfigData.AddRomajiTitle.Value;
        private bool AddAlternativeTitles => ConfigData.AddAlternativeTitles.Value;
        private bool AddFileNameTitles => ConfigData.AddFileNameTitles.Value;
        private bool FilterSeasonEpisode => ConfigData.FilterSeasonEpisode.Value;

        private static Regex YearRegex => new Regex(@"\b((?:19|20)\d{2})$", RegexOptions.Compiled);

        private static readonly HashSet<string> _ExcludedProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Freeleech" };
        private static readonly HashSet<string> _RemuxResolutions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "1080i", "1080p", "2160p", "4K" };
        private static readonly HashSet<string> _CommonReleaseGroupsProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Softsubs",
            "Hardsubs",
            "RAW",
            "Translated"
        };
        private static readonly HashSet<string> _ExcludedFileExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".mka", ".mds", ".md5", ".nfo", ".sfv", ".ass", ".mks", ".srt", ".ssa", ".sup", ".jpeg", ".jpg", ".png", ".otf", ".ttf" };

        private ConfigurationDataAnimeBytes ConfigData => (ConfigurationDataAnimeBytes)configData;

        public AnimeBytes(IIndexerConfigurationService configService, WebClient client, Logger l,
            IProtectionService ps, ICacheService cs)
            : base(configService: configService,
                   client: client,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataAnimeBytes("Note: Go to AnimeBytes site and open your account settings. Go to 'Account' tab, move cursor over black part near 'Passkey' and copy its value. Your username is case sensitive."))
        {
            // AnimeBytes doesn't like fake user agents (issue #1535)
            webclient.EmulateBrowser = false;
            // requestDelay for API Limit (1 request per 3 seconds)
            webclient.requestDelay = 3.1;
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
                },
                SupportsRawSearch = true
            };

            caps.Categories.AddCategoryMapping("anime[tv_series]", TorznabCatType.TVAnime, "TV Series");
            caps.Categories.AddCategoryMapping("anime[tv_special]", TorznabCatType.TVAnime, "TV Special");
            caps.Categories.AddCategoryMapping("anime[ova]", TorznabCatType.TVAnime, "OVA");
            caps.Categories.AddCategoryMapping("anime[ona]", TorznabCatType.TVAnime, "ONA");
            caps.Categories.AddCategoryMapping("anime[dvd_special]", TorznabCatType.TVAnime, "DVD Special");
            caps.Categories.AddCategoryMapping("anime[bd_special]", TorznabCatType.TVAnime, "BD Special");
            caps.Categories.AddCategoryMapping("anime[movie]", TorznabCatType.Movies, "Movie");
            caps.Categories.AddCategoryMapping("audio", TorznabCatType.Audio, "Music");
            caps.Categories.AddCategoryMapping("gamec[game]", TorznabCatType.PCGames, "Game");
            caps.Categories.AddCategoryMapping("gamec[visual_novel]", TorznabCatType.PCGames, "Game Visual Novel");
            caps.Categories.AddCategoryMapping("printedtype[manga]", TorznabCatType.BooksComics, "Manga");
            caps.Categories.AddCategoryMapping("printedtype[oneshot]", TorznabCatType.BooksComics, "Oneshot");
            caps.Categories.AddCategoryMapping("printedtype[anthology]", TorznabCatType.BooksComics, "Anthology");
            caps.Categories.AddCategoryMapping("printedtype[manhwa]", TorznabCatType.BooksComics, "Manhwa");
            caps.Categories.AddCategoryMapping("printedtype[light_novel]", TorznabCatType.BooksComics, "Light Novel");
            caps.Categories.AddCategoryMapping("printedtype[artbook]", TorznabCatType.BooksComics, "Artbook");

            return caps;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            if (ConfigData.Passkey.Value.Length != 32 && ConfigData.Passkey.Value.Length != 48)
                throw new Exception("invalid passkey configured: expected length: 32 or 48, got " + ConfigData.Passkey.Value.Length);

            var results = await PerformQuery(new TorznabQuery());
            if (!results.Any())
                throw new Exception("no results found, please report this bug");

            IsConfigured = true;
            SaveConfig();
            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            releases.AddRange(await GetResults(query, "anime", CleanSearchTerm(query.SanitizedSearchTerm.Trim())));

            if (ContainsMusicCategories(query.Categories))
            {
                releases.AddRange(await GetResults(query, "music", query.SanitizedSearchTerm.Trim()));
            }

            return releases
                   .OrderByDescending(o => o.PublishDate)
                   .ToArray();
        }

        private string CleanSearchTerm(string term)
        {
            // Tracer does not support searching with episode number so strip it if we have one
            term = Regex.Replace(term, @"\W(\dx)?\d?\d$", string.Empty, RegexOptions.Compiled);
            term = Regex.Replace(term, @"\W(S\d\d?E)?\d?\d$", string.Empty, RegexOptions.Compiled);
            term = Regex.Replace(term, @"\W\d+$", string.Empty, RegexOptions.Compiled);

            term = Regex.Replace(term.Trim(), @"\bThe Movie$", string.Empty, RegexOptions.Compiled | RegexOptions.IgnoreCase);

            return term.Trim();
        }

        private static int? ParseYearFromSearchTerm(string term)
        {
            if (term.IsNullOrWhiteSpace())
            {
                return null;
            }

            var yearMatch = YearRegex.Match(term);

            if (!yearMatch.Success)
            {
                return null;
            }

            return ParseUtil.CoerceInt(yearMatch.Groups[1].Value);
        }

        private bool ContainsMusicCategories(int[] categories)
        {
            var music = new[]
            {
                TorznabCatType.Audio.ID,
                TorznabCatType.AudioMP3.ID,
                TorznabCatType.AudioLossless.ID,
                TorznabCatType.AudioOther.ID,
                TorznabCatType.AudioForeign.ID
            };

            return categories.Length == 0 || music.Any(categories.Contains);
        }

        private async Task<IEnumerable<ReleaseInfo>> GetResults(TorznabQuery query, string searchType, string searchTerm)
        {
            var releases = new List<ReleaseInfo>();

            var parameters = new NameValueCollection
            {
                { "username", ConfigData.Username.Value },
                { "torrent_pass", ConfigData.Passkey.Value },
                { "sort", "grouptime" },
                { "way", "desc" },
                { "type", searchType },
                { "limit", searchTerm.IsNotNullOrWhiteSpace() ? "50" : "15" },
                { "searchstr", searchTerm }
            };

            if (ConfigData.SearchByYear.Value && searchType == "anime")
            {
                var searchYear = ParseYearFromSearchTerm(query.SanitizedSearchTerm.Trim());

                if (searchYear > 0)
                {
                    parameters.Set("year", searchYear.ToString());
                }
            }

            var queryCats = MapTorznabCapsToTrackers(query).Distinct().ToList();

            if (queryCats.Any() && query.IsTVSearch && query.Season is > 0)
            {
                // Avoid searching for specials if it's a non-zero season search
                queryCats.RemoveAll(cat => cat is "anime[tv_special]" or "anime[ova]" or "anime[ona]" or "anime[dvd_special]" or "anime[bd_special]");
            }

            if (queryCats.Any())
            {
                queryCats.ForEach(cat => parameters.Set(cat, "1"));
            }

            if (ConfigData.FreeleechOnly.Value)
            {
                parameters.Set("freeleech", "1");
            }

            if (ConfigData.ExcludeHentai.Value && searchType == "anime")
            {
                parameters.Set("hentai", "0");
            }

            var searchUrl = ScrapeUrl + "?" + parameters.GetQueryString();

            // Check cache first so we don't query the server for each episode when searching for each episode in a series.
            lock (cache)
            {
                // Remove old cache items
                CleanCache();

                var cachedResult = cache.FirstOrDefault(i => i.Query == searchUrl);

                if (cachedResult != null)
                {
                    return cachedResult.Results.Select(r => (ReleaseInfo)r.Clone()).ToArray();
                }
            }

            // Get the content from the tracker
            var response = await RequestWithCookiesAndRetryAsync(searchUrl);

            if (!response.ContentString.StartsWith("{")) // not JSON => error
            {
                throw new ExceptionWithConfigData("Unexpected response (not JSON)", ConfigData);
            }

            try
            {
                var jsonResponse = STJson.Deserialize<AnimeBytesResponse>(response.ContentString);

                if (jsonResponse.Error.IsNotNullOrWhiteSpace())
                {
                    throw new Exception($"Unexpected response from indexer request: {jsonResponse.Error}");
                }

                if (jsonResponse.Matches == 0)
                {
                    return releases;
                }

                foreach (var group in jsonResponse.Groups)
                {
                    var categoryName = group.CategoryName;
                    var description = group.Description;
                    var year = group.Year;
                    var posterStr = group.Image;
                    var poster = posterStr.IsNotNullOrWhiteSpace() ? new Uri(posterStr) : null;
                    var groupName = group.GroupName;
                    var seriesName = group.SeriesName;
                    var mainTitle = WebUtility.HtmlDecode(group.FullName);

                    if (seriesName.IsNotNullOrWhiteSpace())
                    {
                        mainTitle = seriesName;
                    }

                    var synonyms = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        mainTitle
                    };

                    if (group.Synonymns != null && group.Synonymns.Any())
                    {
                        if (AddJapaneseTitle && group.Synonymns.TryGetValue("Japanese", out var japaneseTitle) && japaneseTitle.IsNotNullOrWhiteSpace())
                        {
                            synonyms.Add(japaneseTitle.Trim());
                        }

                        if (AddRomajiTitle && group.Synonymns.TryGetValue("Romaji", out var romajiTitle) && romajiTitle.IsNotNullOrWhiteSpace())
                        {
                            synonyms.Add(romajiTitle.Trim());
                        }

                        if (AddAlternativeTitles && group.Synonymns.TryGetValue("Alternative", out var alternativeTitles) && alternativeTitles.IsNotNullOrWhiteSpace())
                        {
                            synonyms.UnionWith(alternativeTitles.Split(',').Select(x => x.Trim()).Where(x => x.IsNotNullOrWhiteSpace()));
                        }
                    }

                    List<int> category = null;

                    foreach (var torrent in group.Torrents)
                    {
                        // Skip non-freeleech results when freeleech only is set
                        if (ConfigData.FreeleechOnly.Value && torrent.RawDownMultiplier != 0)
                        {
                            continue;
                        }

                        var torrentId = torrent.Id;
                        var link = torrent.Link;
                        var publishDate = DateTime.ParseExact(torrent.UploadTime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                        var details = new Uri(SiteLink + "torrent/" + torrentId + "/group");
                        var size = torrent.Size;
                        var snatched = torrent.Snatched;
                        var seeders = torrent.Seeders;
                        var leechers = torrent.Leechers;
                        var peers = seeders + leechers;
                        var fileCount = torrent.FileCount;
                        var rawDownMultiplier = torrent.RawDownMultiplier;
                        var rawUpMultiplier = torrent.RawUpMultiplier;

                        // MST with additional 5 hours per GB
                        var minimumSeedTime = 259200 + (int)(size / (int)Math.Pow(1024, 3) * 18000);

                        var propertyList = WebUtility.HtmlDecode(torrent.Property)
                             .Split('|')
                             .Select(t => t.Trim())
                             .Where(p => p.IsNotNullOrWhiteSpace())
                             .ToList();

                        propertyList.RemoveAll(p => _ExcludedProperties.Any(p.ContainsIgnoreCase));
                        var properties = propertyList.ToHashSet();

                        if (properties.Any(p => p.ContainsIgnoreCase("M2TS")))
                        {
                            properties.Add("BR-DISK");
                        }

                        var isBluRayDisk = properties.Any(p => p.ContainsIgnoreCase("RAW") || p.ContainsIgnoreCase("M2TS") || p.ContainsIgnoreCase("ISO"));

                        if (!AllowRaws && categoryName == "Anime" && isBluRayDisk)
                        {
                            continue;
                        }

                        properties = properties
                             .Select(property =>
                             {
                                 if (isBluRayDisk)
                                 {
                                     property = Regex.Replace(property, @"\b(H\.?265)\b", "HEVC", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                                     property = Regex.Replace(property, @"\b(H\.?264)\b", "AVC", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                                 }

                                 if (torrent.Files.Any(f => f.FileName.ContainsIgnoreCase("Remux"))
                                     && _RemuxResolutions.ContainsIgnoreCase(property))
                                 {
                                     property += " Remux";
                                 }

                                 return property;
                             })
                             .ToHashSet();

                        int? season = null;
                        int? episode = null;

                        var releaseInfo = categoryName == "Anime" ? "S01" : "";
                        var editionTitle = torrent.EditionData?.EditionTitle;

                        if (editionTitle.IsNotNullOrWhiteSpace())
                        {
                            releaseInfo = WebUtility.HtmlDecode(editionTitle);

                            var seasonRegex = new Regex(@"\bSeason (\d+)\b", RegexOptions.Compiled);
                            var seasonRegexMatch = seasonRegex.Match(releaseInfo);
                            if (seasonRegexMatch.Success)
                            {
                                season = ParseUtil.CoerceInt(seasonRegexMatch.Groups[1].Value);
                            }

                            var episodeRegex = new Regex(@"\bEpisode (\d+)\b", RegexOptions.Compiled);
                            var episodeRegexMatch = episodeRegex.Match(releaseInfo);
                            if (episodeRegexMatch.Success)
                            {
                                episode = ParseUtil.CoerceInt(episodeRegexMatch.Groups[1].Value);
                            }
                        }

                        if (categoryName == "Anime")
                        {
                            season ??= ParseSeasonFromTitles(synonyms);
                        }

                        if (PadEpisode && episode > 0 && season == null)
                        {
                            releaseInfo = $"- {episode:00}";
                        }
                        else if (season > 0)
                        {
                            releaseInfo = $"S{season:00}";

                            if (episode > 0)
                            {
                                releaseInfo += $"E{episode:00} - {episode:00}";
                            }
                        }

                        if (FilterSeasonEpisode)
                        {
                            if (query.Season is > 0 && season != null && season != query.Season) // skip if season doesn't match
                            {
                                continue;
                            }

                            if (query.Episode.IsNotNullOrWhiteSpace() && episode != null && episode != int.Parse(query.Episode)) // skip if episode doesn't match
                            {
                                continue;
                            }
                        }

                        if (searchType == "anime")
                        {
                            // Ignore these categories as they'll cause hell with the matcher
                            // TV Special, DVD Special, BD Special
                            if (groupName is "TV Special" or "DVD Special" or "BD Special")
                            {
                                continue;
                            }

                            if (groupName is "TV Series" or "OVA" or "ONA")
                            {
                                category = new List<int> { TorznabCatType.TVAnime.ID };
                            }

                            if (groupName is "Movie" or "Live Action Movie")
                            {
                                category = new List<int> { TorznabCatType.Movies.ID };
                            }

                            if (categoryName is "Manga" or "Oneshot" or "Anthology" or "Manhwa" or "Manhua" or "Light Novel")
                            {
                                category = new List<int> { TorznabCatType.BooksComics.ID };
                            }

                            if (categoryName is "Novel" or "Artbook")
                            {
                                category = new List<int> { TorznabCatType.BooksComics.ID };
                            }

                            if (categoryName is "Game" or "Visual Novel")
                            {
                                if (properties.Contains("PSP"))
                                {
                                    category = new List<int> { TorznabCatType.ConsolePSP.ID };
                                }

                                if (properties.Contains("PS3"))
                                {
                                    category = new List<int> { TorznabCatType.ConsolePS3.ID };
                                }

                                if (properties.Contains("PS Vita"))
                                {
                                    category = new List<int> { TorznabCatType.ConsolePSVita.ID };
                                }

                                if (properties.Contains("3DS"))
                                {
                                    category = new List<int> { TorznabCatType.Console3DS.ID };
                                }

                                if (properties.Contains("NDS"))
                                {
                                    category = new List<int> { TorznabCatType.ConsoleNDS.ID };
                                }

                                if (properties.Contains("PSX") || properties.Contains("PS2") || properties.Contains("SNES") || properties.Contains("NES") || properties.Contains("GBA") || properties.Contains("Switch"))
                                {
                                    category = new List<int> { TorznabCatType.ConsoleOther.ID };
                                }

                                if (properties.Contains("PC"))
                                {
                                    category = new List<int> { TorznabCatType.PCGames.ID };
                                }
                            }
                        }
                        else if (searchType == "music")
                        {
                            if (categoryName is "Single" or "EP" or "Album" or "Compilation" or "Soundtrack" or "Remix CD" or "PV" or "Live Album" or "Image CD" or "Drama CD" or "Vocal CD")
                            {
                                if (properties.Any(p => p.Contains("Lossless")))
                                {
                                    category = new List<int> { TorznabCatType.AudioLossless.ID };
                                }
                                else if (properties.Any(p => p.Contains("MP3")))
                                {
                                    category = new List<int> { TorznabCatType.AudioMP3.ID };
                                }
                                else
                                {
                                    category = new List<int> { TorznabCatType.AudioOther.ID };
                                }
                            }
                        }

                        // We don't actually have a release name >.> so try to create one
                        var releaseGroup = properties.LastOrDefault(p => _CommonReleaseGroupsProperties.Any(p.StartsWithIgnoreCase) && p.Contains("(") && p.Contains(")"));

                        if (releaseGroup.IsNotNullOrWhiteSpace())
                        {
                            var start = releaseGroup.IndexOf("(", StringComparison.Ordinal);
                            releaseGroup = "[" + releaseGroup.Substring(start + 1, releaseGroup.IndexOf(")", StringComparison.Ordinal) - 1 - start) + "] ";
                        }
                        else
                        {
                            releaseGroup = string.Empty;
                        }

                        var infoString = properties.Select(p => "[" + p + "]").Join(string.Empty);

                        var useYearInTitle = year is > 0 && torrent.Files.Any(f => f.FileName.Contains(year.Value.ToString()));

                        foreach (var title in synonyms)
                        {
                            var releaseTitle = groupName is "Movie" or "Live Action Movie" ?
                                $"{releaseGroup}{title} {year} {infoString}" :
                                $"{releaseGroup}{title}{(useYearInTitle ? $" {year}" : string.Empty)} {releaseInfo} {infoString}";

                            var guid = new Uri(details + "&nh=" + StringUtil.Hash(title));

                            var release = new ReleaseInfo
                            {
                                MinimumRatio = 1,
                                MinimumSeedTime = minimumSeedTime,
                                Title = releaseTitle,
                                Year = year,
                                Details = details,
                                Guid = guid,
                                Link = link,
                                Poster = poster,
                                PublishDate = publishDate,
                                Category = category,
                                Description = description,
                                Size = size,
                                Seeders = seeders,
                                Peers = peers,
                                Grabs = snatched,
                                Files = fileCount,
                                DownloadVolumeFactor = rawDownMultiplier,
                                UploadVolumeFactor = rawUpMultiplier
                            };

                            releases.Add(release);
                        }

                        if (AddFileNameTitles)
                        {
                            var files = torrent.Files.ToList();

                            if (files.Count > 1)
                            {
                                files = files.Where(f => !_ExcludedFileExtensions.Contains(Path.GetExtension(f.FileName))).ToList();
                            }

                            if (files.Count != 1)
                            {
                                continue;
                            }

                            var releaseTitle = files.First().FileName;

                            var guid = new Uri(details + "&nh=" + StringUtil.Hash(releaseTitle));

                            var release = new ReleaseInfo
                            {
                                MinimumRatio = 1,
                                MinimumSeedTime = minimumSeedTime,
                                Title = releaseTitle,
                                Year = year,
                                Details = details,
                                Guid = guid,
                                Link = link,
                                Poster = poster,
                                PublishDate = publishDate,
                                Category = category,
                                Description = description,
                                Size = size,
                                Seeders = seeders,
                                Peers = peers,
                                Grabs = snatched,
                                Files = fileCount,
                                DownloadVolumeFactor = rawDownMultiplier,
                                UploadVolumeFactor = rawUpMultiplier
                            };

                            releases.Add(release);
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.ContentString, ex);
            }

            releases = releases.OrderByDescending(o => o.PublishDate).ToList();

            if (query.IsRssSearch)
            {
                releases = releases.Where((r, index) => r.PublishDate > DateTime.UtcNow.AddDays(-1) || index < 20).ToList();
            }

            // Add to the cache
            lock (cache)
            {
                cache.Add(new CachedQueryResult(searchUrl, releases));
            }

            return releases.Select(r => (ReleaseInfo)r.Clone());
        }

        private static int? ParseSeasonFromTitles(IReadOnlyCollection<string> titles)
        {
            var advancedSeasonRegex = new Regex(@"\b(?:(?<season>\d+)(?:st|nd|rd|th) Season|Season (?<season>\d+))\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var seasonCharactersRegex = new Regex(@"(I{2,})$", RegexOptions.Compiled);
            var seasonNumberRegex = new Regex(@"\b(?<!Part[- ._])(?<!\d[/])(?:S)?(?<season>[2-9])$", RegexOptions.Compiled);

            foreach (var title in titles)
            {
                var advancedSeasonRegexMatch = advancedSeasonRegex.Match(title);
                if (advancedSeasonRegexMatch.Success)
                {
                    return ParseUtil.CoerceInt(advancedSeasonRegexMatch.Groups["season"].Value);
                }

                var seasonCharactersRegexMatch = seasonCharactersRegex.Match(title);
                if (seasonCharactersRegexMatch.Success)
                {
                    return seasonCharactersRegexMatch.Groups[1].Value.Length;
                }

                var seasonNumberRegexMatch = seasonNumberRegex.Match(title);
                if (seasonNumberRegexMatch.Success)
                {
                    return ParseUtil.CoerceInt(seasonNumberRegexMatch.Groups["season"].Value);
                }
            }

            return null;
        }
    }

    public class AnimeBytesResponse
    {
        [JsonPropertyName("Matches")]
        public int Matches { get; set; }

        [JsonPropertyName("Groups")]
        public IReadOnlyCollection<AnimeBytesGroup> Groups { get; set; }

        public string Error { get; set; }
    }

    public class AnimeBytesGroup
    {
        [JsonPropertyName("ID")]
        public long Id { get; set; }

        [JsonPropertyName("CategoryName")]
        public string CategoryName { get; set; }

        [JsonPropertyName("FullName")]
        public string FullName { get; set; }

        [JsonPropertyName("GroupName")]
        public string GroupName { get; set; }

        [JsonPropertyName("SeriesName")]
        public string SeriesName { get; set; }

        [JsonPropertyName("Year")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int? Year { get; set; }

        [JsonPropertyName("Image")]
        public string Image { get; set; }

        [JsonPropertyName("SynonymnsV2")]
        public IReadOnlyDictionary<string, string> Synonymns { get; set; }

        [JsonPropertyName("Description")]
        public string Description { get; set; }

        [JsonPropertyName("Tags")]
        public IReadOnlyCollection<string> Tags { get; set; }

        [JsonPropertyName("Torrents")]
        public IReadOnlyCollection<AnimeBytesTorrent> Torrents { get; set; }
    }

    public class AnimeBytesTorrent
    {
        [JsonPropertyName("ID")]
        public long Id { get; set; }

        [JsonPropertyName("EditionData")]
        public AnimeBytesEditionData EditionData { get; set; }

        [JsonPropertyName("RawDownMultiplier")]
        public double RawDownMultiplier { get; set; }

        [JsonPropertyName("RawUpMultiplier")]
        public double RawUpMultiplier { get; set; }

        [JsonPropertyName("Link")]
        public Uri Link { get; set; }

        [JsonPropertyName("Property")]
        public string Property { get; set; }

        [JsonPropertyName("Snatched")]
        public int Snatched { get; set; }

        [JsonPropertyName("Seeders")]
        public int Seeders { get; set; }

        [JsonPropertyName("Leechers")]
        public int Leechers { get; set; }

        [JsonPropertyName("Size")]
        public long Size { get; set; }

        [JsonPropertyName("FileCount")]
        public int FileCount { get; set; }

        [JsonPropertyName("FileList")]
        public IReadOnlyCollection<AnimeBytesFile> Files { get; set; }

        [JsonPropertyName("UploadTime")]
        public string UploadTime { get; set; }
    }

    public class AnimeBytesFile
    {
        [JsonPropertyName("filename")]
        public string FileName { get; set; }

        [JsonPropertyName("size")]
        public long FileSize { get; set; }
    }

    public class AnimeBytesEditionData
    {
        [JsonPropertyName("EditionTitle")]
        public string EditionTitle { get; set; }
    }
}
