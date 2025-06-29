using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Jackett.Common.Extensions;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
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
        // https://anilibria.top/api/docs/v1
        private string ApiBase => $"{SiteLink}api/v1/";
        public override string Language => "ru-RU";
        public override string Type => "public";
        public override TorznabCapabilities TorznabCaps => SetCapabilities();
        private ConfigurationDataAnilibria ConfigData => (ConfigurationDataAnilibria)configData;

        public Anilibria(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
                            ICacheService cs) : base(
            configService: configService, client: wc, logger: l, p: ps, cacheService: cs,
            configData: new ConfigurationDataAnilibria())
        {
            // requestDelay to try to avoid DDoS-Guard and having to wait for Flaresolverr to resolve challenges
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
            var releases = new List<ReleaseInfo>();

            var queryString = query.GetQueryString();
            var searchQuery = queryString.IsNotNullOrWhiteSpace()
                ? Uri.EscapeDataString(queryString)
                : "*";

            var searchResponse = await RequestWithCookiesAsync($"{ApiBase}app/search/releases?query={searchQuery}");
            var searchResults = JsonConvert.DeserializeObject<IReadOnlyList<AnilibriaSearchResult>>(searchResponse.ContentString);

            var releaseIds = searchResults.Where(r => r.Id.HasValue).Select(r => r.Id.Value).Distinct().ToList();

            var addRusTag = ConfigData.AddRussianToTitle.Value ? " RUS" : string.Empty;

            foreach (var releaseId in releaseIds)
            {
                var torrentsResponse = await RequestWithCookiesAsync($"{ApiBase}anime/torrents/release/{releaseId}");
                var torrents = JsonConvert.DeserializeObject<IReadOnlyList<AnilibriaTorrent>>(torrentsResponse.ContentString);

                foreach (var torrent in torrents)
                {
                    var category = torrent.Release.Type.Value;

                    releases.Add(new ReleaseInfo
                    {
                        Guid = GetGuidLink(torrent.Release.Alias, torrent.Hash),
                        Link = GetDownloadLink(torrent.Hash),
                        Details = GetReleaseLink(torrent.Release.Alias),
                        Title = $"{torrent.Release.Name.Main} / {torrent.Label}{addRusTag}",
                        Category = MapTrackerCatToNewznab(category.IsNotNullOrWhiteSpace() ? category : "TV"),
                        Year = torrent.Release.Year,
                        InfoHash = torrent.Hash,
                        Size = torrent.Size,
                        Seeders = torrent.Seeders,
                        Peers = torrent.Seeders + torrent.Leechers,
                        Grabs = torrent.Grabs,
                        PublishDate = torrent.CreatedAt,
                        DownloadVolumeFactor = 0,
                        UploadVolumeFactor = 1,
                        Poster = GetPosterLink(torrent.Release.Poster.Original),
                    });
                }
            }

            return releases;
        }

        private Uri GetGuidLink(string alias, string hash) => new($"{SiteLink}anime/releases/release/{alias}/{hash}");
        private Uri GetReleaseLink(string alias) => new($"{SiteLink}anime/releases/release/{alias}");
        private Uri GetPosterLink(string posterSrc) => new($"{SiteLink}{posterSrc.TrimStart('/')}");
        private Uri GetDownloadLink(string hash) => new($"{ApiBase}anime/torrents/{hash}/file");
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
