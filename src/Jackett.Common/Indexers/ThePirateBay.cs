using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jackett.Common.Converters;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class ThePirateBay : BaseWebIndexer
    {
        public override string[] AlternativeSiteLinks { get; protected set; } = {
            "https://thepiratebay.org/",
            "https://pirateproxy.dev/",
            "https://tpb19.ukpass.co/",
            "https://tpb.sadzawka.tk/",
            "https://www.tpbay.win/",
            "https://tpb.cnp.cx/",
            "https://thepiratebay.d4.re/",
            "https://baypirated.site/",
            "https://tpb.skynetcloud.site/",
            "https://piratetoday.xyz/",
            "https://piratenow.xyz/",
            "https://piratesbaycc.com/"
        };

        public override string[] LegacySiteLinks { get; protected set; } = {
            "https://thepiratebay0.org/",
            "https://thepiratebay10.org/",
            "https://pirateproxy.live/",
            "https://thehiddenbay.com/",
            "https://thepiratebay.zone/",
            "https://tpb.party/",
            "https://piratebayproxy.live/",
            "https://piratebay.live/",
            "https://tpb.biz/",
            "https://pirate.johnedwarddoyle.co.uk/",
            "https://knaben.ru/",
            "https://piratebayztemzmv.onion.pet/",
            "https://piratebayztemzmv.onion.ly/",
            "https://pirateproxy.cloud/",
            "https://tpb18.ukpass.co/"
        };

        private static readonly Uri _ApiBaseUri = new Uri("https://apibay.org/");

        public ThePirateBay(
            IIndexerConfigurationService configService,
            WebClient client,
            Logger logger,
            IProtectionService p
            ) : base(
                id: "thepiratebay",
                name: "The Pirate Bay",
                description: "Pirate Bay (TPB) is the galaxy’s most resilient Public BitTorrent site",
                link: "https://thepiratebay.org/",
                caps: new TorznabCapabilities
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
                    }
                },
                configService: configService,
                client: client,
                logger: logger,
                p: p,
                configData: new ConfigurationData()
                )
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "public";

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
            AddCategoryMapping(206, TorznabCatType.TVOther, "Handheld");
            AddCategoryMapping(207, TorznabCatType.MoviesHD, "HD - Movies");
            AddCategoryMapping(208, TorznabCatType.TVHD, "HD - TV shows");
            AddCategoryMapping(209, TorznabCatType.Movies3D, "3D");
            AddCategoryMapping(299, TorznabCatType.MoviesOther, "Video Other");
            // Applications
            AddCategoryMapping(300, TorznabCatType.PC, "Applications");
            AddCategoryMapping(301, TorznabCatType.PC, "Windows");
            AddCategoryMapping(302, TorznabCatType.PCMac, "Mac");
            AddCategoryMapping(303, TorznabCatType.PC, "UNIX");
            AddCategoryMapping(304, TorznabCatType.PCMobileOther, "Handheld");
            AddCategoryMapping(305, TorznabCatType.PCMobileiOS, "IOS (iPad/iPhone)");
            AddCategoryMapping(306, TorznabCatType.PCMobileAndroid, "Android");
            AddCategoryMapping(399, TorznabCatType.PC, "Other OS");
            // Games
            AddCategoryMapping(400, TorznabCatType.Console, "Games");
            AddCategoryMapping(401, TorznabCatType.PCGames, "PC");
            AddCategoryMapping(402, TorznabCatType.PCMac, "Mac");
            AddCategoryMapping(403, TorznabCatType.ConsolePS4, "PSx");
            AddCategoryMapping(404, TorznabCatType.ConsoleXBox, "XBOX360");
            AddCategoryMapping(405, TorznabCatType.ConsoleWii, "Wii");
            AddCategoryMapping(406, TorznabCatType.ConsoleOther, "Handheld");
            AddCategoryMapping(407, TorznabCatType.ConsoleOther, "IOS (iPad/iPhone)");
            AddCategoryMapping(408, TorznabCatType.ConsoleOther, "Android");
            AddCategoryMapping(499, TorznabCatType.ConsoleOther, "Games Other");
            // Porn
            AddCategoryMapping(500, TorznabCatType.XXX, "Porn");
            AddCategoryMapping(501, TorznabCatType.XXX, "Movies");
            AddCategoryMapping(502, TorznabCatType.XXXDVD, "Movies DVDR");
            AddCategoryMapping(503, TorznabCatType.XXXImageSet, "Pictures");
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

            var releases = await PerformQuery(new TorznabQuery());

            await ConfigureIfOK(
                string.Empty,
                releases.Any(),
                () => throw new Exception("Could not find releases from this URL")
                );

            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            // Keywordless search terms return recent torrents rather than no results.
            if (string.IsNullOrEmpty(query.SearchTerm))
                return await GetRecentTorrents();

            var categories = MapTorznabCapsToTrackers(query);

            var queryStringCategories = string.Join(
                ",",
                categories.Count == 0
                    ? GetAllTrackerCategories()
                    : categories
            );

            var queryCollection = new NameValueCollection
            {
                { "q", query.GetQueryString() },
                { "cat", queryStringCategories }
            };

            var response = await RequestWithCookiesAsync(
                $"{_ApiBaseUri}q.php?{queryCollection.GetQueryString()}"
            );

            var queryResponseItems = JsonConvert.DeserializeObject<List<QueryResponseItem>>(response.ContentString);

            // The API returns a single item to represent a state of no results. Avoid returning this result.
            if (queryResponseItems.Count == 1 && queryResponseItems.First().Id == 0)
                return Enumerable.Empty<ReleaseInfo>();

            return queryResponseItems.Select(CreateReleaseInfo);
        }

        private async Task<IEnumerable<ReleaseInfo>> GetRecentTorrents()
        {
            var response = await RequestWithCookiesAsync($"{_ApiBaseUri}precompiled/data_top100_recent.json");

            return JsonConvert
                   .DeserializeObject<List<QueryResponseItem>>(response.ContentString)
                   .Select(CreateReleaseInfo);
        }

        private ReleaseInfo CreateReleaseInfo(QueryResponseItem item)
        {
            var details = item.Id == 0 ? null : new Uri($"{SiteLink}description.php?id={item.Id}");
            var imdbId = string.IsNullOrEmpty(item.Imdb) ? null : ParseUtil.GetImdbID(item.Imdb);
            return new ReleaseInfo
            {
                Title = item.Name,
                Category = MapTrackerCatToNewznab(item.Category.ToString()),
                Guid = details,
                Details = details,
                InfoHash = item.InfoHash, // magnet link is auto generated from infohash
                PublishDate = DateTimeUtil.UnixTimestampToDateTime(item.Added),
                Seeders = item.Seeders,
                Peers = item.Seeders + item.Leechers,
                Size = item.Size,
                Files = item.NumFiles,
                DownloadVolumeFactor = 0,
                UploadVolumeFactor = 1,
                Imdb = imdbId
            };
        }

        private class QueryResponseItem
        {
            [JsonProperty("id")]
            [JsonConverter(typeof(StringToLongConverter))]
            public long Id { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("info_hash")]
            public string InfoHash { get; set; }

            [JsonProperty("leechers")]
            [JsonConverter(typeof(StringToLongConverter))]
            public long Leechers { get; set; }

            [JsonProperty("seeders")]
            [JsonConverter(typeof(StringToLongConverter))]
            public long Seeders { get; set; }

            [JsonProperty("num_files")]
            [JsonConverter(typeof(StringToLongConverter))]
            public long NumFiles { get; set; }

            [JsonProperty("size")]
            [JsonConverter(typeof(StringToLongConverter))]
            public long Size { get; set; }

            [JsonProperty("username")]
            public string Username { get; set; }

            [JsonProperty("added")]
            [JsonConverter(typeof(StringToLongConverter))]
            public long Added { get; set; }

            [JsonProperty("status")]
            public string Status { get; set; }

            [JsonProperty("category")]
            [JsonConverter(typeof(StringToLongConverter))]
            public long Category { get; set; }

            [JsonProperty("imdb")]
            public string Imdb { get; set; }
        }
    }
}
