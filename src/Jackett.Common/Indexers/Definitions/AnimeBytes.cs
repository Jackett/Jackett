using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jackett.Common.Extensions;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
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

            var queryCats = MapTorznabCapsToTrackers(query);

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
                var json = JToken.Parse(response.ContentString);

                if (json.Value<string>("error") != null)
                {
                    throw new Exception(json.Value<string>("error"));
                }

                if (json.Value<int>("Matches") == 0)
                {
                    return releases;
                }

                foreach (var group in json.Value<JArray>("Groups"))
                {
                    var categoryName = group.Value<string>("CategoryName");
                    var description = group.Value<string>("Description");
                    var year = group.Value<int>("Year");
                    var posterStr = group.Value<string>("Image");
                    var poster = posterStr.IsNotNullOrWhiteSpace() ? new Uri(posterStr) : null;
                    var groupName = group.Value<string>("GroupName");
                    var seriesName = group.Value<string>("SeriesName");
                    var mainTitle = WebUtility.HtmlDecode(group.Value<string>("FullName"));

                    if (seriesName.IsNotNullOrWhiteSpace())
                    {
                        mainTitle = seriesName;
                    }

                    var synonyms = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        mainTitle
                    };

                    if (group.Value<JToken>("SynonymnsV2").HasValues && group.Value<JToken>("SynonymnsV2") is JObject)
                    {
                        var allSynonyms = group.Value<JToken>("SynonymnsV2").ToObject<Dictionary<string, string>>();

                        if (AddJapaneseTitle && allSynonyms.TryGetValue("Japanese", out var japaneseTitle) && japaneseTitle.IsNotNullOrWhiteSpace())
                        {
                            synonyms.Add(japaneseTitle.Trim());
                        }

                        if (AddRomajiTitle && allSynonyms.TryGetValue("Romaji", out var romajiTitle) && romajiTitle.IsNotNullOrWhiteSpace())
                        {
                            synonyms.Add(romajiTitle.Trim());
                        }

                        if (AddAlternativeTitles && allSynonyms.TryGetValue("Alternative", out var alternativeTitles) && alternativeTitles.IsNotNullOrWhiteSpace())
                        {
                            synonyms.UnionWith(alternativeTitles.Split(',').Select(x => x.Trim()).Where(x => x.IsNotNullOrWhiteSpace()));
                        }
                    }
                    else if (group.Value<JToken>("Synonymns").HasValues)
                    {
                        if (group.Value<JToken>("Synonymns") is JArray)
                        {
                            var allSyonyms = group.Value<JToken>("Synonymns").ToObject<List<string>>();

                            if (AddJapaneseTitle && allSyonyms.Count >= 1 && allSyonyms[0].IsNotNullOrWhiteSpace())
                            {
                                synonyms.Add(allSyonyms[0]);
                            }

                            if (AddRomajiTitle && allSyonyms.Count >= 2 && allSyonyms[1].IsNotNullOrWhiteSpace())
                            {
                                synonyms.Add(allSyonyms[1]);
                            }

                            if (AddAlternativeTitles && allSyonyms.Count >= 3 && allSyonyms[2].IsNotNullOrWhiteSpace())
                            {
                                synonyms.UnionWith(allSyonyms[2].Split(',').Select(x => x.Trim()).Where(x => x.IsNotNullOrWhiteSpace()));
                            }
                        }
                        else if (group.Value<JToken>("Synonymns") is JObject)
                        {
                            var allSynonyms = group.Value<JToken>("Synonymns").ToObject<Dictionary<int, string>>();

                            if (AddJapaneseTitle && allSynonyms.TryGetValue(0, out var japaneseTitle) && japaneseTitle.IsNotNullOrWhiteSpace())
                            {
                                synonyms.Add(japaneseTitle.Trim());
                            }

                            if (AddRomajiTitle && allSynonyms.TryGetValue(1, out var romajiTitle) && romajiTitle.IsNotNullOrWhiteSpace())
                            {
                                synonyms.Add(romajiTitle.Trim());
                            }

                            if (AddAlternativeTitles && allSynonyms.TryGetValue(2, out var alternativeTitles) && alternativeTitles.IsNotNullOrWhiteSpace())
                            {
                                synonyms.UnionWith(alternativeTitles.Split(',').Select(x => x.Trim()).Where(x => x.IsNotNullOrWhiteSpace()));
                            }
                        }
                    }

                    List<int> category = null;

                    foreach (var torrent in group.Value<JArray>("Torrents"))
                    {
                        // Skip non-freeleech results when freeleech only is set
                        if (ConfigData.FreeleechOnly.Value && torrent.Value<double>("RawDownMultiplier") != 0)
                        {
                            continue;
                        }

                        var torrentId = torrent.Value<long>("ID");
                        var link = torrent.Value<string>("Link");
                        var linkUri = new Uri(link);
                        var publishDate = DateTime.ParseExact(torrent.Value<string>("UploadTime"), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                        var details = new Uri(SiteLink + "torrent/" + torrentId + "/group");
                        var size = torrent.Value<long>("Size");
                        var snatched = torrent.Value<long>("Snatched");
                        var seeders = torrent.Value<int>("Seeders");
                        var leechers = torrent.Value<int>("Leechers");
                        var peers = seeders + leechers;
                        var fileCount = torrent.Value<int>("FileCount");
                        var rawDownMultiplier = torrent.Value<double>("RawDownMultiplier");
                        var rawUpMultiplier = torrent.Value<double>("RawUpMultiplier");

                        // MST with additional 5 hours per GB
                        var minimumSeedTime = 259200 + (int)(size / (int)Math.Pow(1024, 3) * 18000);

                        var propertyList = WebUtility.HtmlDecode(torrent.Value<string>("Property"))
                             .Split('|')
                             .Select(t => t.Trim())
                             .Where(p => p.IsNotNullOrWhiteSpace())
                             .ToList();

                        propertyList.RemoveAll(p => _ExcludedProperties.Any(p.ContainsIgnoreCase));
                        var properties = new HashSet<string>(propertyList);

                        if (torrent.Value<JToken>("FileList") != null &&
                            torrent.Value<JToken>("FileList").Any(f => f.Value<string>("filename").ContainsIgnoreCase("Remux")))
                        {
                            var resolutionProperty = properties.FirstOrDefault(_RemuxResolutions.ContainsIgnoreCase);

                            if (resolutionProperty.IsNotNullOrWhiteSpace())
                            {
                                properties.Add($"{resolutionProperty} Remux");
                            }
                        }

                        if (properties.Any(p => p.StartsWithIgnoreCase("M2TS")))
                        {
                            properties.Add("BR-DISK");
                        }

                        if (!AllowRaws &&
                            categoryName == "Anime" &&
                            properties.Any(p => p.StartsWithIgnoreCase("RAW") || p.Contains("BR-DISK")))
                        {
                            continue;
                        }

                        int? season = null;
                        int? episode = null;

                        var releaseInfo = categoryName == "Anime" ? "S01" : "";
                        var editionTitle = torrent.Value<JToken>("EditionData")?.Value<string>("EditionTitle");

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

                        foreach (var title in synonyms)
                        {
                            var releaseTitle = groupName is "Movie" or "Live Action Movie" ?
                                $"{releaseGroup}{title} {year} {infoString}" :
                                $"{releaseGroup}{title} {releaseInfo} {infoString}";

                            var guid = new Uri(details + "&nh=" + StringUtil.Hash(title));

                            var release = new ReleaseInfo
                            {
                                MinimumRatio = 1,
                                MinimumSeedTime = minimumSeedTime,
                                Title = releaseTitle,
                                Year = year,
                                Details = details,
                                Guid = guid,
                                Link = linkUri,
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

                        if (AddFileNameTitles && torrent.Value<JToken>("FileList") != null)
                        {
                            var files = torrent.Value<JToken>("FileList").ToList();

                            if (files.Count > 1)
                            {
                                files = files.Where(f => !_ExcludedFileExtensions.Contains(Path.GetExtension(f.Value<string>("filename")))).ToList();
                            }

                            if (files.Count != 1)
                            {
                                continue;
                            }

                            var releaseTitle = files.First().Value<string>("filename");

                            var guid = new Uri(details + "&nh=" + StringUtil.Hash(releaseTitle));

                            var release = new ReleaseInfo
                            {
                                MinimumRatio = 1,
                                MinimumSeedTime = minimumSeedTime,
                                Title = releaseTitle,
                                Year = year,
                                Details = details,
                                Guid = guid,
                                Link = linkUri,
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
            var seasonNumberRegex = new Regex(@"\b(?<!Part[- ._])(?:S)?(?<season>[2-9])$", RegexOptions.Compiled);

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
}
