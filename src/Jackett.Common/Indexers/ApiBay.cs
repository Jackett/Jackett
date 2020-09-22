using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jackett.Common.Converters;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers
{
    public class ApiBay : BaseWebIndexer
    {
        private const string KeyInfoHash = "{info_hash}";

        private static readonly string MagnetUri =
            $"magnet:?xt=urn:btih:{KeyInfoHash}&dn=Carey%20M.%20Tribe%20of%20Hackers." +
            "%20Cybersecurity%20Advice...2019&tr=udp%3A%2F%2Ftracker.coppersurfer.tk" +
            "%3A6969%2Fannounce&tr=udp%3A%2F%2F9.rarbg.to%3A2920%2Fannounce&tr=udp%3" +
            "A%2F%2Ftracker.opentrackr.org%3A1337&tr=udp%3A%2F%2Ftracker.internetwar" +
            "riors.net%3A1337%2Fannounce&tr=udp%3A%2F%2Ftracker.leechers-paradise.or" +
            "g%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker.coppersurfer.tk%3A6969%2Fann" +
            "ounce&tr=udp%3A%2F%2Ftracker.pirateparty.gr%3A6969%2Fannounce&tr=udp%3A" +
            "%2F%2Ftracker.cyberia.is%3A6969%2Fannounce";

        public ApiBay(IIndexerConfigurationService configService, WebClient client, Logger logger, IProtectionService p)
            : base(
                id: "apibay", name: "The Pirate Bay (API Bay)",
                description: "Pirate Bay (TPB) is the galaxy’s most resilient Public BitTorrent site",
                link: "https://apibay.org/", caps: new TorznabCapabilities(), configService: configService,
                client: client,
                logger: logger, p: p, configData: new ConfigurationData())
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "public";
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            await ConfigureIfOK(string.Empty, true, () => throw new Exception("Could not find releases from this URL"));
            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var response = await RequestWithCookiesAsync($"{SiteLink}q.php?q={query.SearchTerm}&cat=0");

            return JsonConvert
                .DeserializeObject<List<QueryResponseItem>>(response.ContentString)
                .Select(CreateReleaseInfo);
        }

        private static ReleaseInfo CreateReleaseInfo(QueryResponseItem item)
        {
            var magnetUri = new Uri(MagnetUri.Replace(KeyInfoHash, item.InfoHash));

            return new ReleaseInfo
            {
                Title = item.Name,
                // Category = MapTrackerCatDescToNewznab(item.Value<string>("category")),
                MagnetUri = magnetUri,
                InfoHash = item.InfoHash,
                PublishDate = ParseUtil.ParseDateTimeFromUnixEpochTimeStamp(item.Added),
                Guid = magnetUri,
                Seeders = item.Seeders,
                Peers = item.Seeders + item.Leechers,
                Size = item.Size,
                DownloadVolumeFactor = 0,
                UploadVolumeFactor = 1
            };
        }

        public class QueryResponseItem
        {
            [JsonProperty("id")]
            [JsonConverter(typeof(ParseStringConverter))]
            public long Id { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("info_hash")]
            public string InfoHash { get; set; }

            [JsonProperty("leechers")]
            [JsonConverter(typeof(ParseStringConverter))]
            public long Leechers { get; set; }

            [JsonProperty("seeders")]
            [JsonConverter(typeof(ParseStringConverter))]
            public long Seeders { get; set; }

            [JsonProperty("num_files")]
            [JsonConverter(typeof(ParseStringConverter))]
            public long NumFiles { get; set; }

            [JsonProperty("size")]
            [JsonConverter(typeof(ParseStringConverter))]
            public long Size { get; set; }

            [JsonProperty("username")]
            public string Username { get; set; }

            [JsonProperty("added")]
            [JsonConverter(typeof(ParseStringConverter))]
            public long Added { get; set; }

            [JsonProperty("status")]
            public string Status { get; set; }

            [JsonProperty("category")]
            [JsonConverter(typeof(ParseStringConverter))]
            public long Category { get; set; }

            [JsonProperty("imdb")]
            public string Imdb { get; set; }
        }
    }
}