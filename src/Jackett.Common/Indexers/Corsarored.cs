using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class Corsarored : BaseWebIndexer
    {
        private const int MaxSearchPageLimit = 4;
        private const int MaxResultsPerPage = 25;

        private readonly Dictionary<string, string[]> _apiCategories = new Dictionary<string, string[]>
        {
            // 0 = Misc. Probably only shows up in search all anyway
            ["0"] = new[] { "25", "27" },
            // 1 = TV
            ["1"] = new[] { "1", "14", "22", "23", "24", "29", "31" },
            // 2 = Movies
            ["2"] = new[] { "4" },
            // 3 = Music/podcasts/audiobooks
            ["3"] = new[] { "2", "21", "34", "35" },
            // 4 = ebooks
            ["4"] = new[] { "3", "13", "30", "36" },
            // 5 = Software
            ["5"] = new[] { "6", "9", "10", "37" },
            // 6 = Video Games
            ["6"] = new[] { "11", "12", "26", "28", "32" },
            // 7 = Anime
            ["7"] = new[] { "7", "8" }
        };

        private readonly Dictionary<string, string> _apiHeaders = new Dictionary<string, string>
        {
            ["Content-Type"] = "application/json"
        };

        public Corsarored(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(id: "corsarored",
                   name: "Corsaro.red",
                   description: "Italian Torrents",
                   link: "https://corsaro.red/",
                   caps: new TorznabCapabilities(),
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   configData: new ConfigurationData())
        {
            Encoding = Encoding.UTF8;
            Language = "it-it";
            Type = "public";

            // TNTVillage cats
            AddCategoryMapping(1, TorznabCatType.TV, "TV Movies");
            AddCategoryMapping(2, TorznabCatType.Audio, "Music");
            AddCategoryMapping(3, TorznabCatType.BooksEbook, "eBooks");
            AddCategoryMapping(4, TorznabCatType.Movies, "Movies");
            AddCategoryMapping(6, TorznabCatType.PC, "Linux");
            AddCategoryMapping(7, TorznabCatType.TVAnime, "Anime");
            AddCategoryMapping(8, TorznabCatType.TVAnime, "Cartoons");
            AddCategoryMapping(9, TorznabCatType.PC, "Mac Software");
            AddCategoryMapping(10, TorznabCatType.PC, "Windows Software");
            AddCategoryMapping(11, TorznabCatType.PCGames, "PC Games");
            AddCategoryMapping(12, TorznabCatType.Console, "Playstation Games");
            AddCategoryMapping(13, TorznabCatType.Books, "Textbooks");
            AddCategoryMapping(14, TorznabCatType.TVDocumentary, "Documentaries");
            AddCategoryMapping(21, TorznabCatType.AudioVideo, "Music Video");
            AddCategoryMapping(22, TorznabCatType.TVSport, "Sport");
            AddCategoryMapping(23, TorznabCatType.TV, "Theater");
            AddCategoryMapping(24, TorznabCatType.TV, "Wrestling");
            AddCategoryMapping(25, TorznabCatType.OtherMisc, "Other");
            AddCategoryMapping(26, TorznabCatType.Console, "Xbox Games");
            AddCategoryMapping(27, TorznabCatType.Other, "Wallpaper");
            AddCategoryMapping(28, TorznabCatType.ConsoleOther, "Other Games");
            AddCategoryMapping(29, TorznabCatType.TV, "TV Series");
            AddCategoryMapping(30, TorznabCatType.BooksComics, "Comics");
            AddCategoryMapping(31, TorznabCatType.TV, "TV");
            AddCategoryMapping(32, TorznabCatType.Console, "Nintendo Games");
            AddCategoryMapping(34, TorznabCatType.AudioAudiobook, "Audiobook");
            AddCategoryMapping(35, TorznabCatType.Audio, "Podcasts");
            AddCategoryMapping(36, TorznabCatType.BooksMagazines, "Newspapers");
            AddCategoryMapping(37, TorznabCatType.PCPhoneOther, "Phone Apps");
        }

        private string ApiLatest => $"{SiteLink}api/latests";
        private string ApiSearch => $"{SiteLink}api/search";

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            base.LoadValuesFromJson(configJson);
            var releases = await PerformQuery(new TorznabQuery());

            await ConfigureIfOK(string.Empty, releases.Any(),
                () => throw new Exception("Could not find release from this URL."));

            return IndexerConfigurationStatus.Completed;
        }

        private dynamic CheckResponse(WebClientStringResult result)
        {
            try
            {
                var json = JsonConvert.DeserializeObject<dynamic>(result.Content);

                switch (json)
                {
                    case JObject _ when json["ok"] != null && (bool)json["ok"] == false:
                        throw new Exception("Server error");
                    default:
                        return json;
                }
            }
            catch (Exception e)
            {
                logger.Error("checkResponse() Error: ", e.Message);
                throw new ExceptionWithConfigData(result.Content, configData);
            }
        }

        private async Task<dynamic> SendApiRequest(IEnumerable<KeyValuePair<string, string>> data)
        {
            var result = await PostDataWithCookiesAndRetry(ApiSearch, data, null, SiteLink, _apiHeaders, null, true);
            return CheckResponse(result);
        }

        private async Task<dynamic> SendApiRequestLatest()
        {
            var result = await RequestStringWithCookiesAndRetry(ApiLatest, null, SiteLink, _apiHeaders);
            return CheckResponse(result);
        }

        private string GetApiCategory(TorznabQuery query)
        {
            var cats = MapTorznabCapsToTrackers(query);
            if (cats.Count == 0)
                return "0";
            string apiCat = null;
            foreach (var cat in cats.Select(cat =>
                _apiCategories.FirstOrDefault(kvp => kvp.Value.Contains(cat)).Key))
            {
                if (apiCat == null)
                    apiCat = cat;
                if (apiCat != cat)
                    return "0";

            }

            return apiCat;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();

            if (string.IsNullOrWhiteSpace(searchString))
            {
                // no term execute latest search
                var result = await SendApiRequestLatest();

                try
                {
                    // this time is a jarray
                    var json = (JArray)result;

                    releases.AddRange(json.Select(MakeRelease));
                }
                catch (Exception ex)
                {
                    OnParseError(result.ToString(), ex);
                }

                return releases;
            }

            var queryCollection = new Dictionary<string, string>
            {
                ["term"] = searchString,
                ["category"] = GetApiCategory(query)
            };

            for (var page = 1; page <= MaxSearchPageLimit; page++)
            {
                // update page number
                queryCollection["page"] = page.ToString();

                var result = await SendApiRequest(queryCollection);
                try
                {
                    // this time is a jobject
                    var json = (JObject)result;

                    // throws exception if json["results"] is null or not a JArray
                    if (json["results"] == null)
                        throw new Exception("Error invalid JSON response");

                    var results = json["results"].Select(MakeRelease).ToList();
                    releases.AddRange(results);
                    if (results.Count < MaxResultsPerPage)
                        break;
                }
                catch (Exception ex)
                {
                    OnParseError(result.ToString(), ex);
                }
            }

            return releases;
        }

        private ReleaseInfo MakeRelease(JToken torrent)
        {
            //https://corsaro.red/details/E5BB62E2E58C654F4450325046723A3F013CD7A4
            var magnetUri = new Uri((string)torrent["magnet"]);
            var comments = new Uri($"{SiteLink}details/{(string)torrent["hash"]}");
            var seeders = (int)torrent["seeders"];
            var publishDate = torrent["last_updated"] != null
                ? DateTime.Parse((string)torrent["last_updated"])
                : DateTime.Now;
            var cat = (string)torrent["category"] ?? "25"; // if category is null set "25 / Other" category
            var size = torrent["size"]?.ToObject<long>();
            return new ReleaseInfo
            {
                Title = (string)torrent["title"],
                Grabs = (long)torrent["completed"],
                Description = $"{(string)torrent["category"]} {(string)torrent["description"]}",
                Seeders = seeders,
                InfoHash = (string)torrent["hash"],
                MagnetUri = magnetUri,
                Comments = comments,
                MinimumRatio = 1,
                MinimumSeedTime = 172800, // 48 hours
                DownloadVolumeFactor = 0,
                UploadVolumeFactor = 1,
                Guid = comments,
                Peers = seeders + (int)torrent["leechers"],
                PublishDate = publishDate,
                Category = MapTrackerCatToNewznab(cat),
                Size = size
            };
        }
    }
}
