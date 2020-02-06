using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
    public class AnimeBytes : BaseCachingWebIndexer
    {
        private string ScrapeUrl => $"{SiteLink}scrape.php";
        private string TorrentsUrl => $"{SiteLink}torrents.php";
        public bool AllowRaws => configData.IncludeRaw.Value;
        public bool PadEpisode => configData.PadEpisode?.Value == true;
        public bool AddSynonyms => configData.AddSynonyms.Value;
        public bool FilterSeasonEpisode => configData.FilterSeasonEpisode.Value;

        private new ConfigurationDataAnimeBytes configData
        {
            get => (ConfigurationDataAnimeBytes)base.configData;
            set => base.configData = value;
        }

        public AnimeBytes(IIndexerConfigurationService configService, WebClient client, Logger l, IProtectionService ps) :
            base(
                "AnimeBytes", "https://animebytes.tv/", "Powered by Tentacles", configService, client,
                caps: new TorznabCapabilities(
                    TorznabCatType.TVAnime, TorznabCatType.Movies, TorznabCatType.BooksComics, TorznabCatType.ConsolePSP,
                    TorznabCatType.ConsoleOther, TorznabCatType.PCGames, TorznabCatType.AudioMP3,
                    TorznabCatType.AudioLossless, TorznabCatType.AudioOther), logger: l, p: ps,
                configData: new ConfigurationDataAnimeBytes(
                    "Note: Go to AnimeBytes site and open your account settings. Go to 'Account' tab, move cursor over black part near 'Passkey' and copy its value. Your username is case sensitive."))
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";
            webclient.EmulateBrowser = false; // Animebytes doesn't like fake user agents (issue #1535)
            AddCategoryMapping("anime[tv_series]", TorznabCatType.TVAnime, "TV Series");
            AddCategoryMapping("anime[tv_special]", TorznabCatType.TVAnime, "TV Special");
            AddCategoryMapping("anime[ova]", TorznabCatType.TVAnime, "OVA");
            AddCategoryMapping("anime[ona]", TorznabCatType.TVAnime, "ONA");
            AddCategoryMapping("anime[dvd_special]", TorznabCatType.TVAnime, "DVD Special");
            AddCategoryMapping("anime[bd_special]", TorznabCatType.TVAnime, "BD Special");
            AddCategoryMapping("anime[movie]", TorznabCatType.Movies, "Movie");
            AddCategoryMapping("gamec[game]", TorznabCatType.PCGames, "Game");
            AddCategoryMapping("gamec[visual_novel]", TorznabCatType.PCGames, "Visual Novel");
            AddCategoryMapping("printedtype[manga]", TorznabCatType.BooksComics, "Manga");
            AddCategoryMapping("printedtype[oneshot]", TorznabCatType.BooksComics, "Oneshot");
            AddCategoryMapping("printedtype[anthology]", TorznabCatType.BooksComics, "Anthology");
            AddCategoryMapping("printedtype[manhwa]", TorznabCatType.BooksComics, "Manhwa");
            AddCategoryMapping("printedtype[light_novel]", TorznabCatType.BooksComics, "Light Novel");
            AddCategoryMapping("printedtype[artbook]", TorznabCatType.BooksComics, "Artbook");
        }

        protected override IEnumerable<ReleaseInfo> FilterResults(TorznabQuery query, IEnumerable<ReleaseInfo> input) =>
            // Prevent filtering
            input;

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            if (configData.Passkey.Value.Length != 32 && configData.Passkey.Value.Length != 48)
                throw new Exception(
                    $"invalid passkey configured: expected length: 32 or 48, got {configData.Passkey.Value.Length}");
            var results = await PerformQuery(new TorznabQuery());
            if (results.Count() == 0)
                throw new Exception("no results found, please report this bug");
            IsConfigured = true;
            SaveConfig();
            return IndexerConfigurationStatus.Completed;
        }

        private string StripEpisodeNumber(string term)
        {
            // Tracer does not support searching with episode number so strip it if we have one
            term = Regex.Replace(term, @"\W(\dx)?\d?\d$", string.Empty);
            term = Regex.Replace(term, @"\W(S\d\d?E)?\d?\d$", string.Empty);
            term = Regex.Replace(term, @"\W\d+$", string.Empty);
            return term;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            // The result list
            var releases = new List<ReleaseInfo>();
            if (ContainsMusicCategories(query.Categories))
                foreach (var result in await GetResultsAsync(query, "music", query.SanitizedSearchTerm))
                    releases.Add(result);
            foreach (var result in await GetResultsAsync(query, "anime", StripEpisodeNumber(query.SanitizedSearchTerm)))
                releases.Add(result);
            return releases.ToArray();
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

        private async Task<IEnumerable<ReleaseInfo>> GetResultsAsync(TorznabQuery query, string searchType, string searchTerm)
        {
            // The result list
            var releases = new List<ReleaseInfo>();
            var queryCollection = new NameValueCollection();
            var queryCats = MapTorznabCapsToTrackers(query);
            if (queryCats.Count > 0)
                foreach (var cat in queryCats)
                    queryCollection.Add(cat, "1");
            queryCollection.Add("username", configData.Username.Value);
            queryCollection.Add("torrent_pass", configData.Passkey.Value);
            queryCollection.Add("type", searchType);
            queryCollection.Add("searchstr", searchTerm);
            var queryUrl = $"{ScrapeUrl}?{queryCollection.GetQueryString()}";

            // Check cache first so we don't query the server for each episode when searching for each episode in a series.
            lock (cache)
            {
                // Remove old cache items
                CleanCache();
                var cachedResult = cache.Where(i => i.Query == queryUrl).FirstOrDefault();
                if (cachedResult != null)
                    return cachedResult.Results.Select(s => (ReleaseInfo)s.Clone()).ToArray();
            }

            // Get the content from the tracker
            var response = await RequestStringWithCookiesAndRetryAsync(queryUrl);
            if (!response.Content.StartsWith("{")) // not JSON => error
                throw new ExceptionWithConfigData("unexcepted response (not JSON)", configData);
            var json = JsonConvert.DeserializeObject<dynamic>(response.Content);

            // Parse
            try
            {
                if (json["error"] != null)
                    throw new Exception(json["error"].ToString());
                var matches = (long)json["Matches"];
                if (matches > 0)
                {
                    var groups = (JArray)json.Groups;
                    foreach (JObject group in groups)
                    {
                        var synonyms = new List<string>();
                        var groupId = (long)group["ID"];
                        var image = (string)group["Image"];
                        var imageUrl = (string.IsNullOrWhiteSpace(image) ? null : new Uri(image));
                        var year = (int)group["Year"];
                        var groupName = (string)group["GroupName"];
                        var seriesName = (string)group["SeriesName"];
                        var mainTitle = WebUtility.HtmlDecode((string)group["FullName"]);
                        if (seriesName != null)
                            mainTitle = seriesName;
                        synonyms.Add(mainTitle);
                        if (AddSynonyms)
                            foreach (string synonym in group["Synonymns"])
                                synonyms.Add(synonym);
                        List<int> category = null;
                        var categoryName = (string)group["CategoryName"];
                        var description = (string)group["Description"];
                        foreach (JObject torrent in group["Torrents"])
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
                            if (PadEpisode && int.TryParse(releaseInfo, out var test) && releaseInfo.Length == 1)
                                releaseInfo = $"0{releaseInfo}";
                            if (FilterSeasonEpisode)
                            {
                                if (query.Season != 0 && season != null && season != query.Season
                                    ) // skip if season doesn't match
                                    continue;
                                if (query.Episode != null && episode != null && episode != query.Episode
                                    ) // skip if episode doesn't match
                                    continue;
                            }

                            var torrentId = (long)torrent["ID"];
                            var property = (string)torrent["Property"];
                            property = property.Replace(" | Freeleech", "");
                            var link = (string)torrent["Link"];
                            var linkUri = new Uri(link);
                            var uploadTimeString = (string)torrent["UploadTime"];
                            var uploadTime = DateTime.ParseExact(
                                uploadTimeString, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                            var publushDate = DateTime.SpecifyKind(uploadTime, DateTimeKind.Utc).ToLocalTime();
                            var commentsLink = $"{TorrentsUrl}?id={groupId}&torrentid={torrentId}";
                            var commentsLinkUri = new Uri(commentsLink);
                            var size = (long)torrent["Size"];
                            var snatched = (long)torrent["Snatched"];
                            var seeders = (int)torrent["Seeders"];
                            var leechers = (int)torrent["Leechers"];
                            var fileCount = (long)torrent["FileCount"];
                            var peers = seeders + leechers;
                            var rawDownMultiplier = (int?)torrent["RawDownMultiplier"];
                            if (rawDownMultiplier == null)
                                rawDownMultiplier = 0;
                            var rawUpMultiplier = (int?)torrent["RawUpMultiplier"];
                            if (rawUpMultiplier == null)
                                rawDownMultiplier = 0;
                            if (searchType == "anime")
                            {
                                if (groupName == "TV Series" || groupName == "OVA")
                                    category = new List<int> { TorznabCatType.TVAnime.ID };

                                // Ignore these categories as they'll cause hell with the matcher
                                // TV Special, OVA, ONA, DVD Special, BD Special
                                if (groupName == "Movie" || groupName == "Live Action Movie")
                                    category = new List<int> { TorznabCatType.Movies.ID };
                                if (categoryName == "Manga" || categoryName == "Oneshot" || categoryName == "Anthology" ||
                                    categoryName == "Manhwa" || categoryName == "Manhua" || categoryName == "Light Novel")
                                    category = new List<int> { TorznabCatType.BooksComics.ID };
                                if (categoryName == "Novel" || categoryName == "Artbook")
                                    category = new List<int> { TorznabCatType.BooksComics.ID };
                                if (categoryName == "Game" || categoryName == "Visual Novel")
                                {
                                    if (property.Contains(" PSP "))
                                        category = new List<int> { TorznabCatType.ConsolePSP.ID };
                                    if (property.Contains("PSX"))
                                        category = new List<int> { TorznabCatType.ConsoleOther.ID };
                                    if (property.Contains(" NES "))
                                        category = new List<int> { TorznabCatType.ConsoleOther.ID };
                                    if (property.Contains(" PC "))
                                        category = new List<int> { TorznabCatType.PCGames.ID };
                                }
                            }
                            else if (searchType == "music")
                                if (categoryName == "Single" || categoryName == "EP" || categoryName == "Album" ||
                                    categoryName == "Compilation" || categoryName == "Soundtrack" || categoryName == "Remix CD" ||
                                    categoryName == "PV" || categoryName == "Live Album" || categoryName == "Image CD" ||
                                    categoryName == "Drama CD" || categoryName == "Vocal CD")
                                {
                                    category = property.Contains(" Lossless ")
                                        ? new List<int> { TorznabCatType.AudioLossless.ID }
                                        : property.Contains("MP3") ? new List<int> { TorznabCatType.AudioMP3.ID } : new List<int> { TorznabCatType.AudioOther.ID };
                                }

                            // We dont actually have a release name >.> so try to create one
                            var releaseTags = property.Split("|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                                                      .ToList();
                            for (var i = releaseTags.Count - 1; i >= 0; i--)
                            {
                                releaseTags[i] = releaseTags[i].Trim();
                                if (string.IsNullOrWhiteSpace(releaseTags[i]))
                                    releaseTags.RemoveAt(i);
                            }

                            var releasegroup = releaseTags.LastOrDefault();
                            if (releasegroup?.Contains("(") == true && releasegroup.Contains(")"))
                            {
                                // Skip raws if set
                                if (releasegroup.ToLowerInvariant().StartsWith("raw") && !AllowRaws)
                                    continue;
                                var start = releasegroup.IndexOf("(");
                                releasegroup =
                                    $"[{releasegroup.Substring(start + 1, (releasegroup.IndexOf(")") - 1) - start)}] ";
                            }
                            else
                                releasegroup = string.Empty;

                            if (!AllowRaws && releaseTags.Contains("raw", StringComparer.InvariantCultureIgnoreCase))
                                continue;
                            var infoString = releaseTags.Aggregate("", (prev, cur) => $"{prev}[{cur}]");
                            var minimumSeedTime = 259200;
                            //  Additional 5 hours per GB
                            minimumSeedTime += (int)((size / 1000000000) * 18000);
                            foreach (var title in synonyms)
                            {
                                var releaseTitle = groupName == "Movie"
                                    ? string.Format("{0} {1} {2}{3}", title, year, releasegroup, infoString)
                                    : string.Format(
                                        "{0}{1} {2} {3}", releasegroup, title, releaseInfo, infoString);
                                var release = new ReleaseInfo
                                {
                                    MinimumRatio = 1,
                                    MinimumSeedTime = minimumSeedTime,
                                    Title = releaseTitle,
                                    Comments = commentsLinkUri,
                                    Guid =
                                        new Uri($"{commentsLinkUri}&nh={StringUtil.Hash(title)}"), // Sonarr should dedupe on this url - allow a url per name.
                                    Link = linkUri,
                                    BannerUrl = imageUrl,
                                    PublishDate = publushDate,
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
            }
            catch (Exception ex)
            {
                OnParseError(response.Content, ex);
            }

            // Add to the cache
            lock (cache)
                cache.Add(new CachedQueryResult(queryUrl, releases));
            return releases.Select(s => (ReleaseInfo)s.Clone());
        }
    }
}
