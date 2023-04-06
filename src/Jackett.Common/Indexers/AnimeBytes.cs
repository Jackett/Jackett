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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers
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
        private bool PadEpisode => ConfigData.PadEpisode != null && ConfigData.PadEpisode.Value;
        private bool AddJapaneseTitle => ConfigData.AddJapaneseTitle.Value;
        private bool AddRomajiTitle => ConfigData.AddRomajiTitle.Value;
        private bool AddAlternativeTitles => ConfigData.AddAlternativeTitles.Value;
        private bool AddFileNameTitles => ConfigData.AddFileNameTitles.Value;
        private bool FilterSeasonEpisode => ConfigData.FilterSeasonEpisode.Value;

        private static Regex YearRegex => new Regex(@"\b((?:19|20)\d{2})$", RegexOptions.Compiled);

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

            releases.AddRange(await GetResults(query, "anime", StripEpisodeNumber(query.SanitizedSearchTerm.Trim())));

            if (ContainsMusicCategories(query.Categories))
                releases.AddRange(await GetResults(query, "music", query.SanitizedSearchTerm.Trim()));

            return releases
                   .OrderByDescending(o => o.PublishDate)
                   .ToArray();
        }

        private string StripEpisodeNumber(string term)
        {
            // Tracer does not support searching with episode number so strip it if we have one
            term = Regex.Replace(term, @"\W(\dx)?\d?\d$", string.Empty);
            term = Regex.Replace(term, @"\W(S\d\d?E)?\d?\d$", string.Empty);
            term = Regex.Replace(term, @"\W\d+$", string.Empty);

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

            if (ConfigData.SearchByYear.Value)
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
                throw new ExceptionWithConfigData("Unexpected response (not JSON)", ConfigData);

            var json = JsonConvert.DeserializeObject<dynamic>(response.ContentString);

            // Parse
            try
            {
                if (json["error"] != null)
                    throw new Exception(json["error"].ToString());

                var matches = (long)json["Matches"];

                if (matches == 0)
                {
                    return releases;
                }

                var groups = (JArray)json.Groups;

                foreach (var group in groups)
                {
                    var synonyms = new List<string>();
                    var posterStr = (string)group["Image"];
                    var poster = (string.IsNullOrWhiteSpace(posterStr) ? null : new Uri(posterStr));
                    var year = (int)group["Year"];
                    var groupName = (string)group["GroupName"];
                    var seriesName = (string)group["SeriesName"];
                    var mainTitle = WebUtility.HtmlDecode((string)group["FullName"]);

                    if (seriesName.IsNotNullOrWhiteSpace())
                        mainTitle = seriesName;

                    synonyms.Add(mainTitle);

                    if (group["Synonymns"].HasValues)
                    {
                        if (group["Synonymns"] is JArray)
                        {
                            var allSyonyms = group["Synonymns"].ToObject<List<string>>();

                            if (AddJapaneseTitle && allSyonyms.Count >= 1)
                                synonyms.Add(allSyonyms[0]);
                            if (AddRomajiTitle && allSyonyms.Count >= 2)
                                synonyms.Add(allSyonyms[1]);
                            if (AddAlternativeTitles && allSyonyms.Count >= 3)
                                synonyms.AddRange(allSyonyms[2].Split(',').Select(t => t.Trim()));
                        }
                        else
                        {
                            var allSynonyms = group["Synonymns"].ToObject<Dictionary<int, string>>();

                            if (AddJapaneseTitle && allSynonyms.ContainsKey(0))
                                synonyms.Add(allSynonyms[0]);
                            if (AddRomajiTitle && allSynonyms.ContainsKey(1))
                                synonyms.Add(allSynonyms[1]);
                            if (AddAlternativeTitles && allSynonyms.ContainsKey(2))
                            {
                                synonyms.AddRange(allSynonyms[2].Split(',').Select(t => t.Trim()));
                            }
                        }
                    }

                    List<int> category = null;
                    var categoryName = (string)group["CategoryName"];

                    var description = (string)group["Description"];

                    foreach (var torrent in group["Torrents"])
                    {
                        var releaseInfo = "S01";
                        string episode = null;
                        int? season = null;
                        var editionTitle = (string)torrent["EditionData"]["EditionTitle"];
                        if (!string.IsNullOrWhiteSpace(editionTitle))
                            releaseInfo = WebUtility.HtmlDecode(editionTitle);

                        var seasonRegEx = new Regex(@"Season (\d+)", RegexOptions.Compiled);
                        var seasonRegExMatch = seasonRegEx.Match(releaseInfo);
                        if (seasonRegExMatch.Success)
                            season = ParseUtil.CoerceInt(seasonRegExMatch.Groups[1].Value);

                        var episodeRegEx = new Regex(@"Episode (\d+)", RegexOptions.Compiled);
                        var episodeRegExMatch = episodeRegEx.Match(releaseInfo);
                        if (episodeRegExMatch.Success)
                            episode = episodeRegExMatch.Groups[1].Value;

                        releaseInfo = releaseInfo.Replace("Episode ", "");
                        releaseInfo = releaseInfo.Replace("Season ", "S");
                        releaseInfo = releaseInfo.Trim();

                        if (PadEpisode && int.TryParse(releaseInfo, out _) && releaseInfo.Length == 1)
                        {
                            releaseInfo = "0" + releaseInfo;
                        }

                        if (FilterSeasonEpisode)
                        {
                            if (query.Season != 0 && season != null && season != query.Season) // skip if season doesn't match
                                continue;
                            if (query.Episode != null && episode != null && episode != query.Episode) // skip if episode doesn't match
                                continue;
                        }
                        var torrentId = (long)torrent["ID"];
                        var property = ((string)torrent["Property"]).Replace(" | Freeleech", "");
                        var link = (string)torrent["Link"];
                        var linkUri = new Uri(link);
                        var uploadTimeString = (string)torrent["UploadTime"];
                        var uploadTime = DateTime.ParseExact(uploadTimeString, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                        var publishDate = DateTime.SpecifyKind(uploadTime, DateTimeKind.Utc).ToLocalTime();
                        var details = new Uri(SiteLink + "torrent/" + torrentId + "/group");
                        var size = (long)torrent["Size"];
                        var snatched = (long)torrent["Snatched"];
                        var seeders = (int)torrent["Seeders"];
                        var leechers = (int)torrent["Leechers"];
                        var fileCount = (long)torrent["FileCount"];
                        var peers = seeders + leechers;

                        var rawDownMultiplier = (int?)torrent["RawDownMultiplier"] ?? 0;
                        var rawUpMultiplier = (int?)torrent["RawUpMultiplier"] ?? 0;

                        if (searchType == "anime")
                        {
                            if (groupName == "TV Series" || groupName == "OVA")
                                category = new List<int> { TorznabCatType.TVAnime.ID };

                            // Ignore these categories as they'll cause hell with the matcher
                            // TV Special, OVA, ONA, DVD Special, BD Special

                            if (groupName == "Movie" || groupName == "Live Action Movie")
                                category = new List<int> { TorznabCatType.Movies.ID };

                            if (categoryName == "Manga" || categoryName == "Oneshot" || categoryName == "Anthology" || categoryName == "Manhwa" || categoryName == "Manhua" || categoryName == "Light Novel")
                                category = new List<int> { TorznabCatType.BooksComics.ID };

                            if (categoryName == "Novel" || categoryName == "Artbook")
                                category = new List<int> { TorznabCatType.BooksComics.ID };

                            if (categoryName == "Game" || categoryName == "Visual Novel")
                            {
                                if (property.Contains(" PSP "))
                                    category = new List<int> { TorznabCatType.ConsolePSP.ID };
                                if (property.Contains("PSX") || property.Contains(" NES ") || property.Contains(" Switch "))
                                    category = new List<int> { TorznabCatType.ConsoleOther.ID };
                                if (property.Contains(" PC "))
                                    category = new List<int> { TorznabCatType.PCGames.ID };
                            }
                        }
                        else if (searchType == "music")
                        {
                            if (categoryName == "Single" || categoryName == "EP" || categoryName == "Album" || categoryName == "Compilation" || categoryName == "Soundtrack" || categoryName == "Remix CD" || categoryName == "PV" || categoryName == "Live Album" || categoryName == "Image CD" || categoryName == "Drama CD" || categoryName == "Vocal CD")
                            {
                                if (property.Contains(" Lossless "))
                                    category = new List<int> { TorznabCatType.AudioLossless.ID };
                                else if (property.Contains("MP3"))
                                    category = new List<int> { TorznabCatType.AudioMP3.ID };
                                else
                                    category = new List<int> { TorznabCatType.AudioOther.ID };
                            }
                        }

                        // We don't actually have a release name >.> so try to create one
                        var releaseTags = property.Split("|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
                        for (var i = releaseTags.Count - 1; i >= 0; i--)
                        {
                            releaseTags[i] = releaseTags[i].Trim();
                            if (string.IsNullOrWhiteSpace(releaseTags[i]))
                                releaseTags.RemoveAt(i);
                        }

                        var releaseGroup = releaseTags.LastOrDefault();
                        if (releaseGroup != null && releaseGroup.Contains("(") && releaseGroup.Contains(")"))
                        {
                            // Skip raws if set
                            if (releaseGroup.ToLowerInvariant().StartsWith("raw") && !AllowRaws)
                            {
                                continue;
                            }

                            var start = releaseGroup.IndexOf("(", StringComparison.Ordinal);
                            releaseGroup = "[" + releaseGroup.Substring(start + 1, (releaseGroup.IndexOf(")", StringComparison.Ordinal) - 1) - start) + "] ";
                        }
                        else
                        {
                            releaseGroup = string.Empty;
                        }
                        if (!AllowRaws && releaseTags.Contains("raw", StringComparer.InvariantCultureIgnoreCase))
                            continue;

                        var infoString = releaseTags.Aggregate("", (prev, cur) => prev + "[" + cur + "]");
                        var minimumSeedTime = 259200;
                        //  Additional 5 hours per GB
                        minimumSeedTime += (int)((size / 1000000000) * 18000);

                        foreach (var title in synonyms)
                        {
                            var releaseTitle = groupName == "Movie" ?
                                $"{title} {year} {releaseGroup}{infoString}" :
                                $"{releaseGroup}{title} {releaseInfo} {infoString}";

                            var guid = new Uri(details + "&nh=" + StringUtil.Hash(title));

                            var release = new ReleaseInfo
                            {
                                MinimumRatio = 1,
                                MinimumSeedTime = minimumSeedTime,
                                Title = releaseTitle,
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

                        if (AddFileNameTitles && (int)torrent["FileCount"] == 1)
                        {
                            var releaseTitle = Path.GetFileNameWithoutExtension((string)torrent["FileList"][0]["filename"]);

                            var guid = new Uri(details + "&nh=" + StringUtil.Hash(releaseTitle));
                            var release = new ReleaseInfo
                            {
                                MinimumRatio = 1,
                                MinimumSeedTime = minimumSeedTime,
                                Title = releaseTitle,
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

            // Add to the cache
            lock (cache)
            {
                cache.Add(new CachedQueryResult(searchUrl, releases));
            }

            return releases.Select(r => (ReleaseInfo)r.Clone());
        }
    }
}
