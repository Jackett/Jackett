using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    internal class AniDUB : BaseWebIndexer
    {
        private static readonly Regex EpisodeInfoRegex = new Regex(@"\[(.*?)(?: \(.*?\))? из (.*?)\]$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex SeasonInfoQueryRegex = new Regex(@"S(\d+)(?:E\d*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex SeasonInfoRegex = new Regex(@"(?:(?:TV-)|(?:ТВ-))(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Lazy<Regex> StripRussianTitleRegex = new Lazy<Regex>(() => new Regex(@"^.*?\/\s*", RegexOptions.Compiled));

        public AniDUB(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "anidub",
                   name: "AniDUB",
                   description: "AniDUB Tracker is a semi-private russian tracker and release group for anime",
                   link: "https://tr.anidub.com/",
                   caps: new TorznabCapabilities
                   {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
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
                   configData: new ConfigurationDataAniDub())
        {
            Encoding = Encoding.UTF8;
            Language = "ru-RU";
            Type = "semi-private";

            webclient.AddTrustedCertificate(new Uri(SiteLink).Host, "392E98CE1447B59CA62BAB8824CA1EEFC2ED3D37");

            AddCategoryMapping(2, TorznabCatType.TVAnime, "Аниме TV");
            AddCategoryMapping(14, TorznabCatType.TVAnime, "Аниме TV / Законченные сериалы");
            AddCategoryMapping(10, TorznabCatType.TVAnime, "Аниме TV / Аниме Ongoing");
            AddCategoryMapping(11, TorznabCatType.TVAnime, "Аниме TV / Многосерийный сёнэн");
            AddCategoryMapping(13, TorznabCatType.XXX, "18+");
            AddCategoryMapping(15, TorznabCatType.BooksComics, "Манга");
            AddCategoryMapping(16, TorznabCatType.Audio, "OST");
            AddCategoryMapping(17, TorznabCatType.Audio, "Подкасты");
            AddCategoryMapping(3, TorznabCatType.TVAnime, "Аниме Фильмы");
            AddCategoryMapping(4, TorznabCatType.TVAnime, "Аниме OVA");
            AddCategoryMapping(5, TorznabCatType.TVAnime, "Аниме OVA |- Аниме ONA");
            AddCategoryMapping(9, TorznabCatType.TV, "Дорамы");
            AddCategoryMapping(6, TorznabCatType.TV, "Дорамы / Японские Сериалы и Фильмы");
            AddCategoryMapping(7, TorznabCatType.TV, "Дорамы / Корейские Сериалы и Фильмы");
            AddCategoryMapping(8, TorznabCatType.TV, "Дорамы / Китайские Сериалы и Фильмы");
            AddCategoryMapping(12, TorznabCatType.Other, "Аниме Ongoing Анонсы");
            AddCategoryMapping(1, TorznabCatType.Other, "Новости проекта Anidub");
        }

        private static Dictionary<string, string> CategoriesMap => new Dictionary<string, string>
            {
                { "/anime_tv/full", "14" },
                { "/anime_tv/anime_ongoing", "10" },
                { "/anime_tv/shonen", "11" },
                { "/anime_tv", "2" },
                { "/xxx", "13" },
                { "/manga", "15" },
                { "/ost", "16" },
                { "/podcast", "17" },
                { "/anime_movie", "3" },
                { "/anime_ova/anime_ona", "5" },
                { "/anime_ova", "4" },
                { "/dorama/japan_dorama", "6" },
                { "/dorama/korea_dorama", "7" },
                { "/dorama/china_dorama", "8" },
                { "/dorama", "9" },
                { "/anons_ongoing", "12" }
            };

        private static ICollection<string> DefaultSearchCategories => new[] { "0" };

        private ConfigurationDataAniDub Configuration
        {
            get => (ConfigurationDataAniDub)configData;
            set => configData = value;
        }

        /// <summary>
        /// https://tr.anidub.com/index.php
        /// </summary>
        private string LoginUrl => SiteLink + "index.php";

        /// <summary>
        /// https://tr.anidub.com/index.php?do=search
        /// </summary>
        private string SearchUrl => SiteLink + "index.php?do=search";

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var data = new Dictionary<string, string>
            {
                { "login_name", Configuration.Username.Value },
                { "login_password", Configuration.Password.Value },
                { "login", "submit" }
            };

            var result = await RequestLoginAndFollowRedirect(
                LoginUrl,
                data,
                CookieHeader,
                returnCookiesFromFirstCall: true
            );

            var parser = new HtmlParser();
            var document = await parser.ParseDocumentAsync(result.ContentString);

            await ConfigureIfOK(result.Cookies, IsAuthorized(result), () =>
            {
                const string ErrorSelector = "#content .berror .berror_c";
                var errorMessage = document.QuerySelector(ErrorSelector).Text().Trim();
                throw new ExceptionWithConfigData(errorMessage, Configuration);
            });

            return IndexerConfigurationStatus.Completed;
        }

        public override async Task<byte[]> Download(Uri link)
        {
            await EnsureAuthorized();
            return await base.Download(link);
        }

        // If the search string is empty use the latest releases
        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
            => query.IsTest || string.IsNullOrWhiteSpace(query.SearchTerm)
            ? await FetchNewReleases()
            : await PerformSearch(query);

        private async Task EnsureAuthorized()
        {
            var result = await RequestWithCookiesAndRetryAsync(SiteLink);

            if (!IsAuthorized(result))
            {
                await ApplyConfiguration(null);
            }
        }

        private async Task<List<ReleaseInfo>> FetchNewReleases()
        {
            const string ReleaseLinksSelector = "#dle-content > .story > .story_h > .lcol > h2 > a";
            var result = await RequestWithCookiesAndRetryAsync(SiteLink);
            var releases = new List<ReleaseInfo>();

            try
            {
                var parser = new HtmlParser();
                var document = await parser.ParseDocumentAsync(result.ContentString);

                foreach (var linkNode in document.QuerySelectorAll(ReleaseLinksSelector))
                {
                    var url = linkNode.GetAttribute("href");
                    releases.AddRange(await FetchShowReleases(url));
                }
            }
            catch (Exception ex)
            {
                OnParseError(result.ContentString, ex);
            }

            return releases;
        }

        private async Task<List<ReleaseInfo>> FetchShowReleases(string url)
        {
            const string ContentId = "dle-content";
            const string ReleasesSelector = "#tabs .torrent_c > div";

            var releases = new List<ReleaseInfo>();

            var uri = new Uri(url);
            var categories = ParseCategories(uri)?.ToArray();
            if (categories == null)
            {
                // If no category then it should be a news topic
                // Doesn't happen often
                return releases;
            }

            var result = await RequestWithCookiesAndRetryAsync(url);

            try
            {
                var parser = new HtmlParser();
                var document = await parser.ParseDocumentAsync(result.ContentString);
                var content = document.GetElementById(ContentId);

                var date = GetDateFromShowPage(url, content);

                var baseTitle = GetBaseTitle(categories, content);
                var poster = GetPoster(url, content);

                foreach (var releaseNode in content.QuerySelectorAll(ReleasesSelector))
                {
                    IElement tabNode;
                    if (releaseNode.Children.Any(node => node.ClassName?.Contains("torrent_h") == true))
                    {
                        // No quality, one tab, seems like a buggy page
                        tabNode = releaseNode;
                    }
                    else
                    {
                        const StringComparison comparisonType = StringComparison.InvariantCultureIgnoreCase;
                        tabNode = releaseNode.Children.First(node => node.TagName.Equals("div", comparisonType));
                    }

                    var seeders = GetReleaseSeeders(tabNode);
                    var guid = new Uri(GetReleaseGuid(url, tabNode));
                    var release = new ReleaseInfo
                    {
                        Title = BuildReleaseTitle(baseTitle, tabNode),
                        Guid = guid,
                        Details = uri,
                        Link = GetReleaseLink(tabNode),
                        PublishDate = date,
                        Category = categories,
                        DownloadVolumeFactor = 0,
                        UploadVolumeFactor = 0,
                        Size = GetReleaseSize(tabNode),
                        Grabs = GetReleaseGrabs(tabNode),
                        Description = GetReleaseDescription(tabNode),
                        Seeders = seeders,
                        Peers = GetReleaseLeechers(tabNode) + seeders,
                        Poster = poster
                    };

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(result.ContentString, ex);
            }

            return releases;
        }

        // Appending id to differentiate between different quality versions
        private static string GetReleaseGuid(string url, IElement tabNode) => QueryHelpers.AddQueryString(url, "id", GetTorrentId(tabNode));

        private static int GetReleaseLeechers(IElement tabNode)
        {
            const string LeechersSelector = ".list.down > .li_swing_m";

            var leechersStr = tabNode.QuerySelector(LeechersSelector).Text();
            int.TryParse(leechersStr, out var leechers);
            return leechers;
        }

        private static int GetReleaseSeeders(IElement tabNode)
        {
            const string SeedersSelector = ".list.down > .li_distribute_m";

            var seedersStr = tabNode.QuerySelector(SeedersSelector).Text();
            int.TryParse(seedersStr, out var seeders);
            return seeders;
        }

        private static string GetReleaseDescription(IElement tabNode)
        {
            const string DescriptionSelector = ".tech > pre";
            return tabNode.QuerySelector(DescriptionSelector)?.Text()?.Trim();
        }

        private static long GetReleaseGrabs(IElement tabNode)
        {
            const string GrabsSelector = ".list.down > .li_download_m";

            var grabsStr = tabNode.QuerySelector(GrabsSelector).Text();
            long.TryParse(grabsStr, out var grabs);
            return grabs;
        }

        private static long GetReleaseSize(IElement tabNode)
        {
            const string SizeSelector = ".list.down > .red";

            var sizeStr = tabNode.QuerySelector(SizeSelector).Text();
            return ReleaseInfo.GetBytes(sizeStr);
        }

        private Uri GetReleaseLink(IElement tabNode) =>
            new Uri($"{SiteLink}engine/download.php?id={GetTorrentId(tabNode)}");

        private static string GetTorrentId(IElement tabNode)
        {
            var nodeId = tabNode.Id;

            // Format is "torrent_{id}_info"
            return nodeId
                .Replace("torrent_", string.Empty)
                .Replace("_info", string.Empty);
        }

        private static string BuildReleaseTitle(string baseTitle, IElement tabNode)
        {
            var releaseNode = tabNode.ParentElement;
            var quality = GetQuality(releaseNode);

            if (!string.IsNullOrWhiteSpace(quality))
            {
                return $"{baseTitle} [{quality}]";
            }

            return baseTitle;
        }

        private static string GetQuality(IElement releaseNode)
        {
            // For some releases there's no block with quality
            if (string.IsNullOrWhiteSpace(releaseNode.Id))
            {
                return null;
            }

            var quality = releaseNode.Id.Trim();
            switch (quality.ToLowerInvariant())
            {
                case "tv720":
                    return "HDTV 720p";
                case "tv1080":
                    return "HDTV 1080p";
                case "bd720":
                    return "BDRip 720p";
                case "bd1080":
                    return "BDRip 1080p";
                case "hwp":
                    return "SDTV";
                default:
                    return quality.ToUpperInvariant();
            }
        }

        private Uri GetPoster(string url, IElement content)
        {
            var posterNode = content.QuerySelector(".poster_bg .poster img");
            var posterSrc = posterNode.GetAttribute("src");

            if (Uri.TryCreate(posterSrc, UriKind.Absolute, out var poster))
                return poster;

            logger.Warn($"[AniDub] Poster URL couldn't be parsed on '{url}'. Poster node src: {posterSrc}");
            return null;
        }

        private string GetBaseTitle(int[] categories, IElement content)
        {
            var domTitle = content.QuerySelector("#news-title");

            var baseTitle = domTitle.Text().Trim();
            baseTitle = StripRussianTitle(baseTitle);
            baseTitle = FixBookInfo(baseTitle);

            var isShow = categories.Contains(TorznabCatType.TVAnime.ID);

            if (isShow)
            {
                baseTitle = FixShowTitle(baseTitle);
            }
            else
            {
                // Just fix TV-\d to S\d and [\d+] to E\d
                baseTitle = FixSeasonInfo(baseTitle);
                baseTitle = FixEpisodeInfo(baseTitle);
            }

            baseTitle = FixMovieInfo(baseTitle);

            // Mostly audio is in original name, which can't be known during parsing
            // Skipping appending russing language tag
            var isAudio = categories.Contains(TorznabCatType.Audio.ID);

            if (!isAudio)
            {
                baseTitle = AppendRussianLanguageTag(baseTitle);
            }

            return baseTitle.Trim();
        }

        private string FixShowTitle(string title)
        {
            var seasonNum = GetSeasonNum(title);

            // Remove season info
            title = SeasonInfoRegex.Replace(title, string.Empty);

            // Normalize for parsing usages
            // Should look like S01E01-E09
            return EpisodeInfoRegex.Replace(
                title,
                match => match.Success ? $"S{seasonNum:00}E01-E{match.Groups[1]}" : string.Empty
            );
        }

        private int GetSeasonNum(string title)
        {
            // First season is often skipped so return 1 if nothing matched
            const int defaultSeason = 1;

            var seasonMatch = SeasonInfoRegex.Match(title);

            if (!seasonMatch.Success)
            {
                return defaultSeason;
            }

            var seasonVal = seasonMatch.Groups[defaultSeason].Value;
            if (int.TryParse(seasonVal, out var seasonNum))
            {
                return seasonNum;
            }

            return defaultSeason;
        }

        private string StripRussianTitle(string title) => Configuration.StripRussianTitle.Value
            ? StripRussianTitleRegex.Value.Replace(title, string.Empty)
            : title;

        private static string FixBookInfo(string title) =>
            title.Replace("[Главы ", "[");

        private static string FixEpisodeInfo(string title) =>
            EpisodeInfoRegex.Replace(
                title,
                match => match.Success ? $"E01-E{match.Groups[1]}" : string.Empty
            );

        private static string FixMovieInfo(string title) =>
            title.Replace(" [Movie]", string.Empty);

        private static string FixSeasonInfo(string title) =>
            SeasonInfoRegex.Replace(
                title,
                match => match.Success ? $"S{int.Parse(match.Groups[1].Value):00}" : string.Empty
            );

        private static string AppendRussianLanguageTag(string title) => title + " [RUS]";

        private DateTime GetDateFromShowPage(string url, IElement content)
        {
            const string dateFormat = "d-MM-yyyy";
            const string dateTimeFormat = dateFormat + ", HH:mm";

            // Would be better to use AssumeLocal and provide "ru-RU" culture,
            // but doesn't work cross-platform
            const DateTimeStyles style = DateTimeStyles.AssumeUniversal;

            var culture = CultureInfo.InvariantCulture;

            var dateText = GetDateFromDocument(content);

            //Correct way but will not always work on cross-platform
            //var localTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
            //var nowLocal = TimeZoneInfo.ConvertTime(DateTime.UtcNow, localTimeZone);

            // Russian Standard Time is +03:00, no DST
            const int russianStandardTimeDiff = 3;
            var nowLocal = DateTime.UtcNow.AddHours(russianStandardTimeDiff);

            dateText = dateText
                .Replace("Вчера", nowLocal.AddDays(-1).ToString(dateFormat))
                .Replace("Сегодня", nowLocal.ToString(dateFormat));

            if (DateTime.TryParseExact(dateText, dateTimeFormat, culture, style, out var date))
            {
                var utcDate = date.ToUniversalTime();
                return utcDate.AddHours(-russianStandardTimeDiff);
            }

            logger.Warn($"[AniDub] Date time couldn't be parsed on '{url}'. Date text: {dateText}");

            return DateTime.UtcNow;
        }

        private static string GetDateFromDocument(IElement content)
        {
            const string DateSelector = ".story_inf > li:nth-child(2)";

            var domDate = content.QuerySelector(DateSelector).LastChild;

            if (domDate?.NodeName != "#text")
            {
                return string.Empty;
            }

            return domDate.NodeValue.Trim();
        }

        private bool IsAuthorized(WebResult result) =>
            result.ContentString.Contains("index.php?action=logout");

        private IEnumerable<int> ParseCategories(Uri showUri)
        {
            var categoriesMap = CategoriesMap;

            var path = showUri.AbsolutePath.ToLowerInvariant();

            return categoriesMap
                .Where(categoryMap => path.StartsWith(categoryMap.Key))
                .Select(categoryMap => MapTrackerCatToNewznab(categoryMap.Value))
                .FirstOrDefault();
        }

        private async Task<IEnumerable<ReleaseInfo>> PerformSearch(TorznabQuery query)
        {
            const string searchLinkSelector = "#dle-content > .searchitem > h3 > a";

            var releases = new List<ReleaseInfo>();
            var response = await RequestWithCookiesAndRetryAsync(SearchUrl, method: RequestType.POST, data: PreparePostData(query));

            try
            {
                var parser = new HtmlParser();
                var document = await parser.ParseDocumentAsync(response.ContentString);

                foreach (var linkNode in document.QuerySelectorAll(searchLinkSelector))
                {
                    var link = linkNode.GetAttribute("href");
                    releases.AddRange(await FetchShowReleases(link));
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.ContentString, ex);
            }
            return releases.Where(release => query.MatchQueryStringAND(release.Title));
        }

        private List<KeyValuePair<string, string>> PreparePostData(TorznabQuery query)
        {
            var data = new List<KeyValuePair<string, string>>
            {
                { "do", "search" },
                { "subaction", "search" },
                { "search_start", "1" },
                { "full_search", "1" },
                { "result_from", "1" },
                { "story", NormalizeSearchQuery(query)},
                { "titleonly", "0" },
                { "searchuser", "" },
                { "replyless", "0" },
                { "replylimit", "0" },
                { "searchdate", "0" },
                { "beforeafter", "after" },
                { "sortby", "" },
                { "resorder", "desc" },
                { "showposts", "1" }
            };

            data.AddRange(PrepareCategoriesQuery(query));

            return data;
        }

        private IEnumerable<KeyValuePair<string, string>> PrepareCategoriesQuery(TorznabQuery query)
        {
            var categories = query.HasSpecifiedCategories
                ? MapTorznabCapsToTrackers(query)
                : DefaultSearchCategories;

            return categories.Select(
                category => new KeyValuePair<string, string>("catlist[]", category)
            );
        }

        private static string NormalizeSearchQuery(TorznabQuery query)
        {
            var searchQuery = query.SanitizedSearchTerm;

            // Convert S\dE\d to TV-{Season}
            // because of the convention on the tracker
            searchQuery = SeasonInfoQueryRegex.Replace(
                searchQuery,
                match => match.Success ? $"TV-{int.Parse(match.Groups[1].Value)}" : string.Empty
            );

            if (query.Season > 0)
            {
                // Replace "TV- " with season from query
                searchQuery = SeasonInfoRegex.Replace(searchQuery, string.Empty);
                searchQuery += $" TV-{query.Season}";
            }

            return searchQuery.ToLowerInvariant();
        }
    }
}
