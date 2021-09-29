using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class BitTitan : BaseWebIndexer
    {
        private string APIBASE => SiteLink + "api.php";
        private string DETAILS => SiteLink + "details.php";

        private new ConfigurationDataAPIKey configData => (ConfigurationDataAPIKey)base.configData;
        public BitTitan(IIndexerConfigurationService configService, WebClient wc, Logger l,
            IProtectionService ps, ICacheService cs)
            : base(id: "bit-titan",
                   name: "BiT-TiTAN",
                   description: "BiT-TiTAN is a GERMAN Private Torrent Tracker for MOVIES / TV / GENERAL",
                   link: "https://bit-titan.net/",
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
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataAPIKey())
        {
            Encoding = Encoding.UTF8;
            Language = "de-DE";
            Type = "private";

            configData.AddDynamic("keyInfo", new DisplayInfoConfigurationItem(String.Empty, "Find or Generate a new key <a href=\"https://bit-titan.net/api_cp.php\" target =_blank>here</a>."));
            configData.AddDynamic("freeleech", new BoolConfigurationItem("Search freeleech only") { Value = false });

            // Configure the category mappings
            AddCategoryMapping(1010, TorznabCatType.MoviesUHD, "Movies 2160p");
            AddCategoryMapping(1020, TorznabCatType.MoviesHD, "Movies 1080p");
            AddCategoryMapping(1030, TorznabCatType.MoviesHD, "Movies 720p");
            AddCategoryMapping(1040, TorznabCatType.MoviesHD, "Movies x264");
            AddCategoryMapping(1050, TorznabCatType.MoviesHD, "Movies x265");
            AddCategoryMapping(1060, TorznabCatType.MoviesSD, "Movies XviD");
            AddCategoryMapping(1070, TorznabCatType.Movies3D, "Movies 3D");
            AddCategoryMapping(1080, TorznabCatType.MoviesDVD, "Movies DVD");
            AddCategoryMapping(1090, TorznabCatType.MoviesBluRay, "Movies BluRay");
            AddCategoryMapping(1100, TorznabCatType.MoviesDVD, "Movies HD2DVD");
            AddCategoryMapping(1110, TorznabCatType.MoviesForeign, "Movies International");
            AddCategoryMapping(1120, TorznabCatType.MoviesHD, "Movies HD Packs");
            AddCategoryMapping(1130, TorznabCatType.MoviesSD, "Movies SD Packs");
            AddCategoryMapping(2010, TorznabCatType.TVUHD, "TV 2160p");
            AddCategoryMapping(2020, TorznabCatType.TVHD, "TV 1080p");
            AddCategoryMapping(2030, TorznabCatType.TVHD, "TV 720p");
            AddCategoryMapping(2040, TorznabCatType.TVHD, "TV x264");
            AddCategoryMapping(2050, TorznabCatType.TVHD, "TV x265");
            AddCategoryMapping(2060, TorznabCatType.TVSD, "TV XviD");
            AddCategoryMapping(2070, TorznabCatType.TVHD, "TV HD Packs");
            AddCategoryMapping(2080, TorznabCatType.TVSD, "TV SD Packs");
            AddCategoryMapping(2090, TorznabCatType.TVForeign, "TV International");
            AddCategoryMapping(3010, TorznabCatType.TVDocumentary, "Docu 2160p");
            AddCategoryMapping(3020, TorznabCatType.TVDocumentary, "Docu 1080p");
            AddCategoryMapping(3030, TorznabCatType.TVDocumentary, "Docu 720p");
            AddCategoryMapping(3040, TorznabCatType.TVDocumentary, "Docu x264");
            AddCategoryMapping(3050, TorznabCatType.TVDocumentary, "Docu x265");
            AddCategoryMapping(3060, TorznabCatType.TVDocumentary, "Docu XviD");
            AddCategoryMapping(3070, TorznabCatType.TVDocumentary, "Docu HD Packs");
            AddCategoryMapping(3080, TorznabCatType.TVDocumentary, "Docu SD Packs");
            AddCategoryMapping(3090, TorznabCatType.TVDocumentary, "Docu International");
            AddCategoryMapping(4010, TorznabCatType.TVSport, "Sport 2160p");
            AddCategoryMapping(4020, TorznabCatType.TVSport, "Sport 1080p");
            AddCategoryMapping(4030, TorznabCatType.TVSport, "Sport 720p");
            AddCategoryMapping(4040, TorznabCatType.TVSport, "Sport SD Sport");
            AddCategoryMapping(4050, TorznabCatType.TVSport, "Sport HD Packs");
            AddCategoryMapping(4060, TorznabCatType.TVSport, "Sport SD Packs");
            AddCategoryMapping(5010, TorznabCatType.XXX, "XXX 2160p");
            AddCategoryMapping(5020, TorznabCatType.XXX, "XXX 1080p");
            AddCategoryMapping(5030, TorznabCatType.XXX, "XXX 720p");
            AddCategoryMapping(5040, TorznabCatType.XXX, "XXX x264");
            AddCategoryMapping(5050, TorznabCatType.XXX, "XXX x265");
            AddCategoryMapping(5060, TorznabCatType.XXX, "XXX XviD");
            AddCategoryMapping(5070, TorznabCatType.XXX, "XXX HD Packs");
            AddCategoryMapping(5080, TorznabCatType.XXX, "XXX SD Packs");
            AddCategoryMapping(5090, TorznabCatType.XXX, "XXX Sonstiges");
            AddCategoryMapping(6010, TorznabCatType.PCGames, "Games Windows");
            AddCategoryMapping(6020, TorznabCatType.Console, "Games Linux");
            AddCategoryMapping(6030, TorznabCatType.PCMac, "Games MacOS");
            AddCategoryMapping(6040, TorznabCatType.PCMobileAndroid, "Games Android");
            AddCategoryMapping(6050, TorznabCatType.ConsoleXBox, "Games Xbox");
            AddCategoryMapping(6060, TorznabCatType.ConsolePSP, "Games PlayStation");
            AddCategoryMapping(6070, TorznabCatType.ConsoleNDS, "Games Nintendo");
            AddCategoryMapping(6080, TorznabCatType.Console, "Games Sonstige");
            AddCategoryMapping(7010, TorznabCatType.PC0day, "Software Windows");
            AddCategoryMapping(7020, TorznabCatType.PC, "Software Linux");
            AddCategoryMapping(7030, TorznabCatType.PCMac, "Software MacOS");
            AddCategoryMapping(7040, TorznabCatType.PCMobileAndroid, "Software Android");
            AddCategoryMapping(8010, TorznabCatType.AudioMP3, "Music MP3-Album");
            AddCategoryMapping(8020, TorznabCatType.AudioMP3, "Music MP3-Charts");
            AddCategoryMapping(8030, TorznabCatType.AudioMP3, "Music MP3-Sampler");
            AddCategoryMapping(8040, TorznabCatType.AudioMP3, "Music MP3-Single");
            AddCategoryMapping(8050, TorznabCatType.AudioLossless, "Music FLAC-Album");
            AddCategoryMapping(8060, TorznabCatType.AudioLossless, "Music FLAC-Charts");
            AddCategoryMapping(8070, TorznabCatType.AudioLossless, "Music FLAC-Sampler");
            AddCategoryMapping(8080, TorznabCatType.AudioLossless, "Music FLAC-Single");
            AddCategoryMapping(8090, TorznabCatType.AudioVideo, "Music Video");
            AddCategoryMapping(9010, TorznabCatType.AudioAudiobook, "Books A-Book");
            AddCategoryMapping(9020, TorznabCatType.BooksEBook, "Books E-Book");
            AddCategoryMapping(9030, TorznabCatType.Books, "Books E-Paper");
            AddCategoryMapping(9040, TorznabCatType.Books, "Books E-Learning");
            AddCategoryMapping(9060, TorznabCatType.TVAnime, "Anime HD");
            AddCategoryMapping(9070, TorznabCatType.TVAnime, "Anime SD");
            AddCategoryMapping(9080, TorznabCatType.TVAnime, "Anime Pack");
            AddCategoryMapping(9999, TorznabCatType.Other, "unsort");
        }


        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            IsConfigured = false;
            try
            {
                var results = await PerformQuery(new TorznabQuery());
                if (results.Count() == 0)
                    throw new Exception("Testing returned no results!");
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

            var apiKey = configData.Key.Value;
            var searchUrl = $"{APIBASE}?apiKey={apiKey}";

            var cats = MapTorznabCapsToTrackers(query);
            if (cats.Count > 0)
                searchUrl += "&categories=" + string.Join(",", cats);

            searchUrl += "&search=" + query.SanitizedSearchTerm;

            searchUrl += "&downloadLink=1";

            searchUrl += "&limit=2";

            if (((BoolConfigurationItem)configData.GetDynamic("freeleech")).Value)
                searchUrl += "&searchIn=9";

            var results = await RequestWithCookiesAndRetryAsync(searchUrl);

            try
            {
                var rows = (JArray)((JObject)JsonConvert.DeserializeObject(results.ContentString))["results"];
                foreach (var row in rows)
                {
                    var title = row["name"].ToString();
                    if (!query.MatchQueryStringAND(title))
                        continue;

                    var torrentId = row["id"].ToString();
                    var details = new Uri(DETAILS + "?id=" + torrentId);
                    var link = new Uri(row["download"].ToString());
                    var publishDate = DateTime.Parse(row["added"].ToString());
                    var seeders = (int)row["seeds"];
                    var cat = MapTrackerCatToNewznab(row["category"].ToString());

                    var release = new ReleaseInfo
                    {
                        Title = title,
                        Details = details,
                        Guid = details,
                        Link = link,
                        PublishDate = publishDate,
                        Category = cat,
                        Size = (long)row["size"],
                        Grabs = (int)row["snatchers"],
                        Seeders = seeders,
                        Peers = seeders + (int)row["leechers"],
                        Imdb = null,
                        UploadVolumeFactor = (int)row["uploadFactor"],
                        DownloadVolumeFactor = (int)row["downloadFactor"],
                        MinimumRatio = 1,
                        MinimumSeedTime = 172800 // 2 days
                    };

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.ContentString, ex);
            }

            return releases;
        }
    }
}
