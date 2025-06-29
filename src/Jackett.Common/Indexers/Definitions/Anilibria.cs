using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Jackett.Common.Extensions;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Serializer;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class Anilibria : IndexerBase
    {
        public override string Id => "anilibria";
        public override string Name => "Anilibria";
        public override string Description => "Anilibria is a russian-language anime distribution platform";
        public override string SiteLink { get; protected set; } = "https://anilibria.top/";
        public override string[] LegacySiteLinks => new[]
        {
            "https://www.anilibria.tv/",
        };
        private string ApiBase => $"{SiteLink}api/v1/";
        public override string Language => "ru-RU";
        public override string Type => "public";
        public override TorznabCapabilities TorznabCaps => SetCapabilities();
        private ConfigurationDataAnilibria ConfigData => (ConfigurationDataAnilibria)configData;
        private const int DefaultRssLimit = 15;
        public Anilibria(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
                            ICacheService cs) : base(
            configService: configService, client: wc, logger: l, p: ps, cacheService: cs,
            configData: new ConfigurationDataAnilibria())
        {
            // requestDelay to try to avoid DDoS-Guard and avoind having to wait for Flaresolverr to resolve challenge
            webclient.requestDelay = 2.1;
        }

        private static TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                TvSearchParams = new List<TvSearchParam> { TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep }
            };
            caps.Categories.AddCategoryMapping("TV", TorznabCatType.TVAnime, "Аниме TV");
            caps.Categories.AddCategoryMapping("MOVIE", TorznabCatType.TVAnime, "Аниме Фильмы");
            caps.Categories.AddCategoryMapping("OVA", TorznabCatType.TVAnime, "Аниме OVA");
            caps.Categories.AddCategoryMapping("ONA", TorznabCatType.TVAnime, "Аниме ONA");
            caps.Categories.AddCategoryMapping("SPECIAL", TorznabCatType.TVAnime, "Аниме Спешл");
            caps.Categories.AddCategoryMapping("WEB", TorznabCatType.TVAnime, "Аниме WEB");
            caps.Categories.AddCategoryMapping("OAD", TorznabCatType.TVAnime, "Аниме OAD");
            caps.Categories.AddCategoryMapping("DORAMA", TorznabCatType.TV, "Дорамы");
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
            var releases = new List<ReleaseInfo>();
            var response = await RequestWithCookiesAsync($"{ApiBase}anime/torrents/rss?limit={(query.IsTest || !IsConfigured ? 1 : DefaultRssLimit)}");
            var doc = XDocument.Parse(response.ContentString);
            var torrentIds = doc.Descendants("torrentId")
                                .Select(x => x.Value)
                                .ToList();

            foreach (var releaseId in torrentIds)
            {
                var torrentsResponse = await RequestWithCookiesAsync($"{ApiBase}anime/torrents/{releaseId}");
                releases.AddRange(MapToReleaseInfo(torrentsResponse));
            }

            return releases;
        }

        private async Task<IEnumerable<ReleaseInfo>> SearchReleasesAsync(TorznabQuery query)
        {
            var searchQuery = Uri.EscapeDataString(query.GetQueryString());
            var releases = new List<ReleaseInfo>();
            var searchResponse = await RequestWithCookiesAsync($"{ApiBase}app/search/releases?query={searchQuery}");
            var searchResults = JsonConvert.DeserializeObject<IReadOnlyList<AnilibriaSearchResult>>(searchResponse.ContentString);
            var releaseIds = searchResults.Where(r => r.Id.HasValue).Select(r => r.Id.Value).Distinct().ToList();

            foreach (var releaseId in releaseIds)
            {
                var torrentsResponse = await RequestWithCookiesAsync($"{ApiBase}anime/torrents/release/{releaseId}");
                releases.AddRange(MapToReleaseInfo(torrentsResponse));
            }

            return releases;
        }

        private List<ReleaseInfo> MapToReleaseInfo(WebResult torrentsResponse)
        {
            var releases = new List<ReleaseInfo>();
            var token = JToken.Parse(torrentsResponse.ContentString);
            IReadOnlyList<AnilibriaTorrent> torrents;

            switch (token.Type)
            {
                case JTokenType.Array:
                    torrents = token.ToObject<IReadOnlyList<AnilibriaTorrent>>();
                    break;
                case JTokenType.Object:
                    {
                        var singleTorrent = token.ToObject<AnilibriaTorrent>();
                        torrents = new List<AnilibriaTorrent> { singleTorrent };
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            releases.AddRange(
                from torrent in torrents
                let category = torrent.Release.Type.Value
                select new ReleaseInfo
                {
                    Guid = GetGuidLink(torrent.Release.Alias, torrent.Hash),
                    Link = GetDownloadLink(torrent.Hash),
                    Details = GetReleaseLink(torrent.Release.Alias),
                    Title = $"{torrent.Release.Name.Main} / {GetFormatLabel(torrent.Label)}",
                    Category = MapTrackerCatToNewznab(category.IsNotNullOrWhiteSpace() ? category : "TV"),
                    Year = torrent.Release.Year,
                    InfoHash = torrent.Hash,
                    Size = torrent.Size,
                    Seeders = torrent.Seeders,
                    Peers = torrent.Seeders + torrent.Leechers,
                    Grabs = torrent.Grabs,
                    PublishDate = torrent.UpdateAt,
                    DownloadVolumeFactor = 0,
                    UploadVolumeFactor = 1,
                    Poster = GetPosterLink(torrent.Release.Poster.Original),
                });

            return releases;
        }

        private string GetFormatLabel(string label)
        {
            var (season, episodes) = ParseSeasonEpisodes(label);
            return label =
                $"{(ConfigData.AddSeasonToTitle.Value ? season : string.Empty)}{episodes} {label} {(ConfigData.AddRussianToTitle.Value ? " RUS" : string.Empty)}";
        }

        private Uri GetGuidLink(string alias, string hash) => new($"{SiteLink}anime/releases/release/{alias}/{hash}");

        private Uri GetReleaseLink(string alias) => new($"{SiteLink}anime/releases/release/{alias}");

        private Uri GetPosterLink(string posterSrc) => new($"{SiteLink}{posterSrc.TrimStart('/')}");

        private Uri GetDownloadLink(string hash) => new($"{ApiBase}anime/torrents/{hash}/file");

        public static (string season, string episodes) ParseSeasonEpisodes(string title)
        {
            var firstBracket = title.IndexOf('[');
            var lastBracket = title.LastIndexOf(']');
            var seasonPart = firstBracket >= 0 ? title.Substring(0, firstBracket) : title;
            var episodesPart = (lastBracket > firstBracket && firstBracket >= 0)
                ? title.Substring(title.LastIndexOf('[') + 1, lastBracket - title.LastIndexOf('[') - 1)
                : "";
            seasonPart = Regex.Replace(seasonPart, @"\(\d{4}\)", "");
            seasonPart = Regex.Replace(seasonPart, @"\b\d{4}\b$", "");
            var hasPartNumber = Regex.IsMatch(seasonPart, @"\bPart\s+\d+\b", RegexOptions.IgnoreCase);
            var seasonMatch = Regex.Match(seasonPart,
                @"\b(?:Season|S|Series)\s*(\d+)|\b(\d+)(?:st|nd|rd|th)?\s*Season\b|\b([IVXLCDM]+)\b|\b(\d+)\b",
                RegexOptions.IgnoreCase);
            var season = "S01";

            if (seasonMatch.Success && !hasPartNumber)
            {
                if (!string.IsNullOrEmpty(seasonMatch.Groups[1].Value))
                {
                    season = $"S{int.Parse(seasonMatch.Groups[1].Value):D2}";
                }
                else if (!string.IsNullOrEmpty(seasonMatch.Groups[2].Value))
                {
                    season = $"S{int.Parse(seasonMatch.Groups[2].Value):D2}";
                }
                else if (!string.IsNullOrEmpty(seasonMatch.Groups[3].Value))
                {
                    season = $"S{RomanToArabic(seasonMatch.Groups[3].Value):D2}";
                }
                else if (!string.IsNullOrEmpty(seasonMatch.Groups[4].Value))
                {
                    season = $"S{int.Parse(seasonMatch.Groups[4].Value):D2}";
                }
            }

            var episodes = string.Empty;
            var epMatch = Regex.Match(episodesPart, @"(\d+)(?:[-–—](\d+))?");

            if (epMatch.Success)
            {
                var start = int.Parse(epMatch.Groups[1].Value);
                if (epMatch.Groups[2].Success)
                {
                    var end = int.Parse(epMatch.Groups[2].Value);
                    episodes = $"E{start:D2}-E{end:D2}";
                }
                else
                {
                    episodes = $"E{start:D2}";
                }
            }

            return (season, episodes);
        }

        private static int RomanToArabic(string roman)
        {
            int[] values = { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };
            string[] numerals = { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };
            var result = 0;
            var i = 0;
            roman = roman.ToUpper();
            while (roman.Length > 0)
            {
                if (roman.StartsWith(numerals[i]))
                {
                    result += values[i];
                    roman = roman.Substring(numerals[i].Length);
                }
                else
                {
                    i++;
                }
            }

            return result;
        }
    }

    public sealed class AnilibriaSearchResult
    {
        public long? Id { get; set; }
    }

    public sealed class AnilibriaTorrent
    {
        public AnilibriaTorrentRelease Release { get; set; }
        public string Label { get; set; }
        public string Hash { get; set; }
        public long Size { get; set; }
        public long Seeders { get; set; }
        public long Leechers { get; set; }

        [JsonProperty("completed_times")]
        public long Grabs { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public DateTime UpdateAt { get; set; }
    }

    public sealed class AnilibriaTorrentRelease
    {
        public long Id { get; set; }
        public AnilibriaTorrentReleaseType Type { get; set; }
        public AnilibriaTorrentReleaseName Name { get; set; }
        public string Alias { get; set; }
        public int? Year { get; set; }
        public AnilibriaTorrentReleasePoster Poster { get; set; }
    }

    public sealed class AnilibriaTorrentReleaseType
    {
        public string Value { get; set; }
    }

    public sealed class AnilibriaTorrentReleaseName
    {
        public string Main { get; set; }
        public string English { get; set; }
    }

    public sealed class AnilibriaTorrentReleasePoster
    {
        [JsonProperty("src")]
        public string Original { get; set; }
    }
}
