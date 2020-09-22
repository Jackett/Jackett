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

            ConfigureCategoryMappings();
        }

        private void ConfigureCategoryMappings()
        {
            // Audio
            AddCategoryMapping(100, TorznabCatType.Audio, "Audio");
            AddCategoryMapping(101, TorznabCatType.Audio, "Music");
            AddCategoryMapping(102, TorznabCatType.AudioAudiobook, "Audio Books");
            AddCategoryMapping(103, TorznabCatType.Audio, "Sound Clips");
            AddCategoryMapping(104, TorznabCatType.AudioLossless, "FLAC");
            AddCategoryMapping(199, TorznabCatType.AudioOther, "Audio Other");
            // Video
            AddCategoryMapping(200, TorznabCatType.Movies, "Video");
            AddCategoryMapping(201, TorznabCatType.Movies, "Movies");
            AddCategoryMapping(202, TorznabCatType.Movies, "Movies");
            AddCategoryMapping(203, TorznabCatType.AudioVideo, "Music Videos");
            AddCategoryMapping(204, TorznabCatType.MoviesOther, "Movie Clips");
            AddCategoryMapping(205, TorznabCatType.TV, "TV");
            AddCategoryMapping(206, TorznabCatType.TVOTHER, "Handheld");
            AddCategoryMapping(207, TorznabCatType.MoviesHD, "HD - Movies");
            AddCategoryMapping(208, TorznabCatType.TVHD, "HD - TV shows");
            AddCategoryMapping(209, TorznabCatType.Movies3D, "3D");
            AddCategoryMapping(299, TorznabCatType.MoviesOther, "Video Other");
            // Applications
            AddCategoryMapping(300, TorznabCatType.PC, "Applications");
            AddCategoryMapping(301, TorznabCatType.PC, "Windows");
            AddCategoryMapping(302, TorznabCatType.PCMac, "Mac");
            AddCategoryMapping(303, TorznabCatType.PC, "UNIX");
            AddCategoryMapping(304, TorznabCatType.PCPhoneOther, "Handheld");
            AddCategoryMapping(305, TorznabCatType.PCPhoneIOS, "IOS (iPad/iPhone)");
            AddCategoryMapping(306, TorznabCatType.PCPhoneAndroid, "Android");
            AddCategoryMapping(399, TorznabCatType.PC, "Other OS");
            // Games
            AddCategoryMapping(400, TorznabCatType.Console, "Games");
            AddCategoryMapping(401, TorznabCatType.PCGames, "PC");
            AddCategoryMapping(402, TorznabCatType.PCMac, "Mac");
            AddCategoryMapping(403, TorznabCatType.ConsolePS4, "PSx");
            AddCategoryMapping(404, TorznabCatType.ConsoleXbox, "XBOX360");
            AddCategoryMapping(405, TorznabCatType.ConsoleWii, "Wii");
            AddCategoryMapping(406, TorznabCatType.ConsoleOther, "Handheld");
            AddCategoryMapping(407, TorznabCatType.ConsoleOther, "IOS (iPad/iPhone)");
            AddCategoryMapping(408, TorznabCatType.ConsoleOther, "Android");
            AddCategoryMapping(499, TorznabCatType.ConsoleOther, "Games Other");
            // Porn
            AddCategoryMapping(500, TorznabCatType.XXX, "Porn");
            AddCategoryMapping(501, TorznabCatType.XXX, "Movies");
            AddCategoryMapping(502, TorznabCatType.XXXDVD, "Movies DVDR");
            AddCategoryMapping(503, TorznabCatType.XXXImageset, "Pictures");
            AddCategoryMapping(504, TorznabCatType.XXX, "Games");
            AddCategoryMapping(505, TorznabCatType.XXX, "HD - Movies");
            AddCategoryMapping(506, TorznabCatType.XXX, "Movie Clips");
            AddCategoryMapping(599, TorznabCatType.XXXOther, "Porn other");
            // Other
            AddCategoryMapping(600, TorznabCatType.Other, "Other");
            AddCategoryMapping(601, TorznabCatType.Books, "E-books");
            AddCategoryMapping(602, TorznabCatType.BooksComics, "Comics");
            AddCategoryMapping(603, TorznabCatType.Books, "Pictures");
            AddCategoryMapping(604, TorznabCatType.Books, "Covers");
            AddCategoryMapping(605, TorznabCatType.Books, "Physibles");
            AddCategoryMapping(699, TorznabCatType.BooksOther, "Other Other");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            await ConfigureIfOK(string.Empty, true, () => throw new Exception("Could not find releases from this URL"));
            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var categories = MapTorznabCapsToTrackers(query);

            var queryStringCategories = string.Join(
                ",",
                categories.Count == 0
                    ? GetAllTrackerCategories()
                    : categories
                );

            var response = await RequestWithCookiesAsync(
                $"{SiteLink}q.php?q={query.SearchTerm}&cat={queryStringCategories}"
                );

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