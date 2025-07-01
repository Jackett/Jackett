using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using AngleSharp;
using AngleSharp.Html.Parser;
using BencodeNET.Torrents;
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
    public class MegaPeer : IndexerBase
    {
        public override string Id => "megapeerC";
        public override string Name => "Megapeer sharp";
        public override string Description => "MegaPeer is a RUSSIAN Public Torrent Tracker for MOVIES / TV";
        public override string SiteLink { get; protected set; } = "https://megapeer.vip/";
        public override string Language => "ru-RU";
        public override string Type => "public";
        private const int MaxConcurrentRequest = 5;
        private ConfigurationDataMegaPeer ConfigData => (ConfigurationDataMegaPeer)configData;
        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        public MegaPeer(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
                        ICacheService cs) : base(
            configService: configService, client: wc, logger: l, p: ps, cacheService: cs,
            configData: new ConfigurationDataMegaPeer())
        {
            // requestDelay to try to avoid DDoS-Guard and having to wait for Flaresolverr to resolve challenges
            webclient.requestDelay = 2.1;
        }

        private readonly string[] _categories =
        {
            "79",
            "80",
            "5",
            "76",
            "6",
            "0"
        };

        private static TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q, MovieSearchParam.ImdbId
                },
            };

            caps.Categories.AddCategoryMapping("79", TorznabCatType.Movies, "Наши фильмы");
            caps.Categories.AddCategoryMapping("80", TorznabCatType.Movies, "Зарубежные фильмы");
            caps.Categories.AddCategoryMapping("5", TorznabCatType.TV, "Наши сериалы");
            caps.Categories.AddCategoryMapping("6", TorznabCatType.TV, "Зарубежные сериалы сериалы");
            caps.Categories.AddCategoryMapping("76", TorznabCatType.TVOther, "Мультипликация");
            caps.Categories.AddCategoryMapping("0", TorznabCatType.OtherMisc, "Прочее");

            return caps;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            IsConfigured = false;

            try
            {
                var results = await PerformQuery(new TorznabQuery());

                if (!results.Any())
                {
                    throw new Exception("API unavailable or unknown error");
                }

                IsConfigured = true;
                SaveConfig();
            }
            catch (Exception e)
            {
                throw new ExceptionWithConfigData(e.Message, configData);
            }

            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            return query?.GetQueryString().IsNotNullOrWhiteSpace() ?? false
                ? await SearchReleasesAsync(query)
                : await ReturnLastReleasesAsync(query);
        }

        private async Task<IEnumerable<ReleaseInfo>> ReturnLastReleasesAsync(TorznabQuery query)
        {
            var headers = new Dictionary<string, string>()
            {
                {"dnt", "1"},
                {"pragma", "no-cache"},
                {"referer", SiteLink + "search.php"},
                {"sec-fetch-dest", "document"},
                {"sec-fetch-mode", "navigate"},
                {"sec-fetch-site", "same-origin"},
                {"sec-fetch-user", "?1"},
                {"upgrade-insecure-requests", "1"},
                {"User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36"}
            };

            var commonLink =
                $"https://megapeer.vip/browse.php?search=&age={DateTime.UtcNow.Year}&cat=searchCat&stype=0&sort=0&ascdesc=0";

            var megaPeerTorrents = new List<MegaPeerTorrent>();
            var link = commonLink.Replace("searchCat", "0");
            var responseGrid = await RequestWithCookiesAndRetryAsync(link, headers: headers);
            responseGrid.Encoding = Encoding.GetEncoding(1251);
            var megaPeerTorrentsPart = ParseHtmlGridPage(responseGrid);
            megaPeerTorrentsPart.ForEach(t => t.Category = "0");
            megaPeerTorrents.AddRange(megaPeerTorrentsPart);
            var categories = _categories.Where(c => c != "0").ToArray();

            foreach (var cat in categories)
            {
                link = commonLink.Replace("searchCat", cat);
                responseGrid = await RequestWithCookiesAndRetryAsync(link, headers: headers);
                responseGrid.Encoding = Encoding.GetEncoding(1251);
                megaPeerTorrentsPart = ParseHtmlGridPage(responseGrid);
                megaPeerTorrentsPart.ForEach(t => t.Category = cat);
                megaPeerTorrents.ReplaceIfExistsByKey(megaPeerTorrentsPart, t => t.Url);
                headers["referer"] = link;
            }

            return MapToReleaseInfo(megaPeerTorrents);
        }

        private async Task<List<MegaPeerTorrent>> GetMegaPeerDetailTorrentsAsync(List<MegaPeerTorrent> megaPeersTorrents, Dictionary<string, string> headers)
        {
            var semaphore = new SemaphoreSlim(MaxConcurrentRequest);
            var tasks = new List<Task>();

            foreach (var megaPeersTorrent in megaPeersTorrents)
            {
                await semaphore.WaitAsync();
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var responseForm = await RequestWithCookiesAndRetryAsync(megaPeersTorrent.Url, headers: headers);
                        responseForm.Encoding = Encoding.GetEncoding(1251);
                        var torrentDetail = ParseHtmlFormPage(responseForm);
                        megaPeersTorrent.Date = torrentDetail.Date ?? megaPeersTorrent.Date;
                        megaPeersTorrent.Magnet = torrentDetail.Magnet;
                        megaPeersTorrent.Title = torrentDetail.Title;
                        megaPeersTorrent.OriginalTitle = torrentDetail.OriginalTitle;
                        megaPeersTorrent.Year = torrentDetail.Year;
                        megaPeersTorrent.Category = torrentDetail.Category;
                        megaPeersTorrent.Poster = torrentDetail.Poster;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks);
            return megaPeersTorrents;
        }

        private async Task<List<ReleaseInfo>> SearchReleasesAsync(TorznabQuery query)
        {
            var searchQuery = HttpUtility.UrlEncode(query.GetQueryString(), Encoding.GetEncoding("windows-1251"));
            var headers = new Dictionary<string, string>
            {
                {"dnt", "1"},
                {"pragma", "no-cache"},
                {"referer", SiteLink + "search.php"},
                {"sec-fetch-dest", "document"},
                {"sec-fetch-mode", "navigate"},
                {"sec-fetch-site", "same-origin"},
                {"sec-fetch-user", "?1"},
                {"upgrade-insecure-requests", "1"},
                {"User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36"}
            };

            var commonLink =
                $"https://megapeer.vip/browse.php?search={searchQuery}&age=&cat=searchCat&stype=0&sort=0&ascdesc=0&page=searchPage";

            var megaPeerTorrents = new List<MegaPeerTorrent>();
            var page = 0;

            foreach (var cat in _categories)
            {
                var link = commonLink.Replace("searchCat", cat).Replace("searchPage", page.ToString());

                while (true)
                {
                    var responseGrid = await RequestWithCookiesAndRetryAsync(link, headers: headers);
                    responseGrid.Encoding = Encoding.GetEncoding(1251);
                    var megaPeerTorrentsPart = ParseHtmlGridPage(responseGrid);
                    megaPeerTorrentsPart.ForEach(t => t.Category = cat);
                    megaPeerTorrents.AddRangeIfNotExists(megaPeerTorrentsPart, t => t.Url);
                    page++;
                    headers["referer"] = link;

                    if (page > GetMaxPageCount(responseGrid.ContentString))
                    {
                        page = 0;
                        break;
                    }
                }
            }

            return MapToReleaseInfo(megaPeerTorrents);

        }

        private List<ReleaseInfo> MapToReleaseInfo(IEnumerable<MegaPeerTorrent> torrents)
        {
            return torrents.Select(
                               megaPeerTorrent => new ReleaseInfo
                               {
                                   Guid = new Uri(megaPeerTorrent.Url),
                                   Link = new Uri(megaPeerTorrent.Download),
                                   Details = new Uri(megaPeerTorrent.Url),
                                   Title = GetReleaseInfoTitle(megaPeerTorrent.Label),
                                   Category = MapTrackerCatToNewznab(megaPeerTorrent.Category),
                                   Year = megaPeerTorrent.Year,
                                   MagnetUri = !string.IsNullOrEmpty(megaPeerTorrent.Magnet) ? new Uri(megaPeerTorrent.Magnet) : null,
                                   Size = megaPeerTorrent.Size,
                                   Seeders = megaPeerTorrent.Seeders,
                                   Peers = megaPeerTorrent.Seeders + megaPeerTorrent.Leechers,
                                   PublishDate = megaPeerTorrent.Date ?? default,
                                   DownloadVolumeFactor = 0,
                                   UploadVolumeFactor = 1,
                                   Poster = !string.IsNullOrEmpty(megaPeerTorrent.Poster) ? new Uri(megaPeerTorrent.Poster) : null
                               })
                           .ToList();
        }

        private MegaPeerTorrent ParseHtmlFormPage(WebResult response)
        {
            var html = response.ContentString;

            if (!Regex.IsMatch(html, @"id=""logo""", RegexOptions.IgnoreCase))
            {
                logger.Debug(html);
                throw new Exception("Failed to fetch torrents for the last hours.");
            }

            var parser = new HtmlParser();
            using var document = parser.ParseDocument(html);
            var torrent = new MegaPeerTorrent();
            var descriptionBlock = document.QuerySelector("#descr")?.InnerHtml ?? "";
            var plainDescriptionBlock = Regex.Replace(descriptionBlock, "<.*?>", "", RegexOptions.Singleline).Trim();

            torrent.OriginalTitle = GetRegex(plainDescriptionBlock, @"Оригинальное название:\s*(.*?)\s*\n");
            torrent.Title = GetRegex(plainDescriptionBlock, @"Название:\s*(.*?)\s*\n");

            if (int.TryParse(GetRegex(plainDescriptionBlock, @"Год выхода:\s*(\d{4})"), out var year))
            {
                torrent.Year = year;
            }

            torrent.Poster = document.QuerySelector("#descr")?.QuerySelector("img")?.GetAttribute("src")?.Trim();
            torrent.Magnet = document.QuerySelector("a[href^='magnet']")?.GetAttribute("href");
            var dateRow = document.QuerySelectorAll("tr")
                                  .FirstOrDefault(tr => tr.QuerySelector("td.heading")?.TextContent.Contains("Добавлен") == true);
            var dateText = dateRow?.QuerySelector("td:not(.heading)")?.TextContent.Trim();
            torrent.Date = ParseRussianDateTime(dateText);
            torrent.Category = document.QuerySelector("tr:contains('Категория')")?.QuerySelector("a.online")
                                       ?.GetAttribute("href")?.Replace("/cat/", "").Trim();
            return torrent;
        }

        private string GetReleaseInfoTitle(string input)
        {
            input = ReplaceSeasonInfoWithTag(input);

            if (ConfigData.EnglishTitleOnly.Value)
            {
                input = RemoveRussianLettersAndTrailingSlash(input);
            }

            if (ConfigData.AddRussianToTitle.Value)
            {
                input += " RUS";
            }

            return input;
        }

        private static DateTime? ParseRussianDateTime(string dateTimeStr)
        {
            if (string.IsNullOrWhiteSpace(dateTimeStr))
            {
                return null;
            }

            var cleanDateTimeStr = dateTimeStr.Split(new[] { " (" }, StringSplitOptions.RemoveEmptyEntries)[0];
            var russianCulture = new CultureInfo("ru-RU");
            const string format = "dd MMMM yyyy 'в' HH:mm:ss";

            if (DateTime.TryParseExact(
                    cleanDateTimeStr,
                    format,
                    russianCulture,
                    DateTimeStyles.None,
                    out var result))
            {
                return result;
            }

            return null;
        }

        public string RemoveRussianLettersAndTrailingSlash(string input)
        {
            var noRussian = Regex.Replace(input, "[а-яА-ЯёЁ]", "");
            return Regex.Replace(noRussian, @"^\s*/\s*", "");
        }

        private string GetRegex(string input, string pattern, RegexOptions options = RegexOptions.None)
        {
            var match = Regex.Match(input, pattern, options);
            return match.Success ? match.Groups[1].Value.Trim() : "";
        }

        private List<MegaPeerTorrent> ParseHtmlGridPage(WebResult response)
        {
            var html = response.ContentString;

            if (!Regex.IsMatch(html, @"id=""logo""", RegexOptions.IgnoreCase))
            {
                logger.Debug(html);
                throw new Exception("Failed to fetch torrents for the last hours.");
            }

            var parser = new HtmlParser();
            using var document = parser.ParseDocument(html);
            var rows = document.QuerySelectorAll("tr.table_fon");

            var torrents = new List<MegaPeerTorrent>();

            foreach (var row in rows)
            {
                var torrent = new MegaPeerTorrent();
                var cells = row.Children;

                if (cells.Length < 4)
                {
                    continue;
                }

                torrent.Date = ParsePublishDate(cells[0].TextContent.Trim());
                var urlNode = row.QuerySelector("a.url");
                torrent.Url = !string.IsNullOrEmpty(urlNode?.GetAttribute("href"))
                    ? SiteLink + urlNode.GetAttribute("href")?.TrimStart('/')
                    : string.Empty;
                var titleHtml = urlNode?.InnerHtml ?? "";
                torrent.Label = Regex.Replace(titleHtml, "<[^>]+>", "").Trim();

                if (string.IsNullOrWhiteSpace(torrent.Label))
                {
                    continue;
                }

                var x = row.QuerySelector("a:has(img)");
                torrent.Download = !string.IsNullOrEmpty(row.QuerySelector("a:has(img)")?.GetAttribute("href"))
                    ? SiteLink + row.QuerySelector("a:has(img)")?.GetAttribute("href")
                    : string.Empty;

                if (string.IsNullOrEmpty(torrent.Download))
                {
                    continue;
                }

                torrent.Size = ParseSize(cells[cells.Length - 2].TextContent.Trim());
                var peerHtml = cells[cells.Length - 1].InnerHtml;
                torrent.Seeders = TryMatchInt(peerHtml, @"alt=""S"">\s*<font[^>]*>([0-9]+)</font>");
                torrent.Leechers = TryMatchInt(peerHtml, @"alt=""L"">\s*<font[^>]*>([0-9]+)</font>");

                torrents.Add(torrent);
            }

            return torrents;
        }

        public static string ReplaceSeasonInfoWithTag(string input, int defaultSeason = 1)
        {
            const string episodeWords = "сер(и|и[яе])|выпуск(и|ов|а)?";

            var matchFull = Regex.Match(input,
                $@"\[(\d+)\s*сезон[:\s]*(\d+)(?:-(\d+))?\s*({episodeWords})[^]]*\]",
                RegexOptions.IgnoreCase);

            if (matchFull.Success)
            {
                var season = int.Parse(matchFull.Groups[1].Value);
                var fromEp = int.Parse(matchFull.Groups[2].Value);
                var toEp = matchFull.Groups[3].Success ? int.Parse(matchFull.Groups[3].Value) : fromEp;
                var replacement = $"S{season:D2}E{fromEp:D2}" + (toEp != fromEp ? $"-E{toEp:D2}" : "");
                return input.Replace(matchFull.Value, replacement);
            }

            var matchRange = Regex.Match(input, @"\[(\d+)-(\d+)\s*сезон\]", RegexOptions.IgnoreCase);
            if (matchRange.Success)
            {
                var fromSeason = int.Parse(matchRange.Groups[1].Value);
                var toSeason = int.Parse(matchRange.Groups[2].Value);
                var replacement = $"[S{fromSeason:D2}-S{toSeason:D2}]";
                return input.Replace(matchRange.Value, replacement);
            }

            var matchSingle = Regex.Match(input, @"\[(\d+)\s*сезон\]", RegexOptions.IgnoreCase);
            if (matchSingle.Success)
            {
                var season = int.Parse(matchSingle.Groups[1].Value);
                var replacement = $"[S{season:D2}]";
                return input.Replace(matchSingle.Value, replacement);
            }

            var matchEpisodesOnly = Regex.Match(input,
                $@"\[(\d+)(?:-(\d+))?\s*({episodeWords})[^]]*\]",
                RegexOptions.IgnoreCase);

            if (matchEpisodesOnly.Success)
            {
                var fromEp = int.Parse(matchEpisodesOnly.Groups[1].Value);
                var toEp = matchEpisodesOnly.Groups[2].Success ? int.Parse(matchEpisodesOnly.Groups[2].Value) : fromEp;
                var replacement = $"S{defaultSeason:D2}E{fromEp:D2}" + (toEp != fromEp ? $"-E{toEp:D2}" : "");
                return input.Replace(matchEpisodesOnly.Value, replacement);
            }

            return input;
        }


        private long ParseSize(string input)
        {
            var match = Regex.Match(input, @"^([\d.,]+)\s*(KB|MB|GB|TB)$", RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                return 0;
            }

            var numberPart = match.Groups[1].Value.Replace(',', '.');
            var unit = match.Groups[2].Value.ToUpper();

            if (!double.TryParse(numberPart, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            {
                return 0;
            }

            return unit switch
            {
                "KB" => (long)(value * 1024),
                "MB" => (long)(value * 1024 * 1024),
                "GB" => (long)(value * 1024 * 1024 * 1024),
                "TB" => (long)(value * 1024L * 1024 * 1024 * 1024),
                _ => 0
            };
        }

        private DateTime ParsePublishDate(string input)
        {
            input = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input.ToLower());
            var culture = new CultureInfo("ru-RU", false);
            var format = culture.DateTimeFormat;

            format.AbbreviatedMonthNames = new[]
            {
                "Янв", "Фев", "Мар", "Апр", "Май", "Июн",
                "Июл", "Авг", "Сен", "Окт", "Ноя", "Дек", ""
            };

            format.AbbreviatedMonthGenitiveNames = new[]
            {
                "Янв", "Фев", "Мар", "Апр", "Мая", "Июня",
                "Июля", "Авг", "Сен", "Окт", "Ноя", "Дек", ""
            };

            format.MonthNames = new[]
            {
                "Января", "Февраля", "Марта", "Апреля", "Мая", "Июня",
                "Июля", "Августа", "Сентября", "Октября", "Ноября", "Декабря", ""
            };

            format.MonthGenitiveNames = format.MonthNames;
            string[] formats = { "d MMM yy", "d MMMM yy" };

            foreach (var fmt in formats)
            {
                if (DateTime.TryParseExact(input, fmt, format, DateTimeStyles.None, out var parsed))
                {
                    return parsed.ToUniversalTime();
                }
            }

            throw new FormatException($"Не удалось распарсить дату: {input}");
        }

        private int TryMatchInt(string input, string pattern)
        {
            var match = Regex.Match(input, pattern);
            return match.Success ? int.Parse(match.Groups[1].Value) : 0;
        }

        private int GetMaxPageCount(string html)
        {
            int.TryParse(Regex.Match(html, ">Всего: ([0-9]+)").Groups[1].Value, out var maxPages);
            maxPages = maxPages / 50;
            return maxPages;
        }
    }

    class MegaPeerTorrent
    {
        public DateTime? Date { get; set; }
        public string Url { get; set; }
        public string Magnet { get; set; }
        public string Label { get; set; }
        public long Size { get; set; }
        public int Seeders { get; set; }
        public int Leechers { get; set; }
        public string OriginalTitle { get; set; }
        public string Title { get; set; }
        public int? Year { get; set; }
        public string Category { get; set; }
        public string Poster { get; set; }
        public string Download { get; set; }
    }
}
