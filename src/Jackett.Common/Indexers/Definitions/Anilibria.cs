using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Models.DTO.Anilibria;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Serializer;
using Jackett.Common.Services.Cache;
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
        private string ApiBase => $"{SiteLink}api/v1/";
        public override string Language => "ru-RU";
        public override string Type => "public";
        public override TorznabCapabilities TorznabCaps => SetCapabilities();
        private ConfigurationDataAnilibria ConfigData => (ConfigurationDataAnilibria)configData;

        public Anilibria(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
                            CacheManager cm) : base(
            configService: configService, client: wc, logger: l, p: ps, cacheManager: cm,
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
            var releases = new List<ReleaseInfo>();
            var template = Uri.EscapeDataString(query.GetQueryString());

            if (string.IsNullOrEmpty(template))
            {
                template = "*";
            }

            var responseReleases = await RequestWithCookiesAsync(
                $"{ApiBase}app/search/releases?query={template}", cookieOverride: string.Empty);
            var ids = JArray.Parse(responseReleases.ContentString).Select(o => (long?)o["id"]).Where(id => id.HasValue)
                            .Select(id => id.Value).ToList();
            var torrentsInfo = new List<AnilibriaTorrentInfo>();

            foreach (var id in ids)
            {
                var torrents = await RequestWithCookiesAsync(
                    $"{ApiBase}anime/torrents/release/{id}", cookieOverride: string.Empty);
                torrentsInfo.AddRange(
                    JsonConvert.DeserializeObject<List<AnilibriaTorrentInfo>>(
                        torrents.ContentString, new AnilibriaTopTorrentInfoConverter()));
            }

            var AddRusTag = (ConfigData.AddRussianToTitle.Value) ? " RUS" : string.Empty;

            releases.AddRange(
                torrentsInfo.Select(
                    torrentInfo => new ReleaseInfo
                    {
                        Guid = GetGuidLink(torrentInfo.Alias, torrentInfo.Hash),
                        Title = $"{torrentInfo.NameMain} / {torrentInfo.Label}{AddRusTag}",
                        Details = GetReleaseLink(torrentInfo.Alias),
                        Poster = GetPosterLink(torrentInfo.PosterSrc),
                        Year = torrentInfo.Year,
                        Link = GetDownloadLink(torrentInfo.Hash),
                        Size = torrentInfo.Size,
                        Seeders = torrentInfo.Seeders,
                        Peers = torrentInfo.Seeders + torrentInfo.Leechers,
                        PublishDate = torrentInfo.CreatedAt,
                        InfoHash = torrentInfo.Hash,
                        Grabs = torrentInfo.Grabs,
                        DownloadVolumeFactor = 0,
                        UploadVolumeFactor = 1,
                        Category = MapTrackerCatToNewznab(torrentInfo.Category)
                    }));
            return releases;
        }

        private Uri GetGuidLink(string alias, string hash) => new($"{SiteLink}anime/releases/release/{alias}/{hash}");
        private Uri GetReleaseLink(string alias) => new($"{SiteLink}anime/releases/release/{alias}");
        private Uri GetPosterLink(string posterSrc) => new($"{SiteLink}{posterSrc.TrimStart('/')}");
        private Uri GetDownloadLink(string hash) => new($"{ApiBase}anime/torrents/{hash}/file");
    }
}
