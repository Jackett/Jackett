using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class NebulanceAPI : BaseWebIndexer
    {
        // Docs at https://nebulance.io/articles.php?topic=api_key
        protected virtual string APIUrl => SiteLink + "api.php";
        protected virtual int KeyLength => 32;

        // TODO: remove ConfigurationDataAPIKey class and use ConfigurationDataPasskey instead
        private new ConfigurationDataAPIKey configData
        {
            get => (ConfigurationDataAPIKey)base.configData;
            set => base.configData = value;
        }

        public NebulanceAPI(IIndexerConfigurationService configService, WebClient wc, Logger l,
            IProtectionService ps, ICacheService cs)
            : base(id: "nebulanceapi",
                   name: "NebulanceAPI",
                   description: "At Nebulance we will change the way you think about TV. Using API.",
                   link: "https://nebulance.io/",
                   caps: new TorznabCapabilities
                   {
                       LimitsDefault = 100,
                       LimitsMax = 1000,
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.Genre
                       },
                       SupportsRawSearch = true
                   },
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataAPIKey())
        {
            Encoding = Encoding.UTF8;
            Language = "en-US";
            Type = "private";

            AddCategoryMapping("tv", TorznabCatType.TV, "tv");
            AddCategoryMapping("sd", TorznabCatType.TVSD, "sd");
            AddCategoryMapping("hd", TorznabCatType.TVHD, "hd");
            AddCategoryMapping("uhd", TorznabCatType.TVUHD, "uhd");
            AddCategoryMapping("4k", TorznabCatType.TVUHD, "4k");
            AddCategoryMapping("480p", TorznabCatType.TVSD, "480p");
            AddCategoryMapping("720p", TorznabCatType.TVHD, "720p");
            AddCategoryMapping("1080p", TorznabCatType.TVHD, "1080p");
            AddCategoryMapping("1080i", TorznabCatType.TVHD, "1080i");
            AddCategoryMapping("2160p", TorznabCatType.TVUHD, "2160p");

            configData.AddDynamic("keyInfo", new DisplayInfoConfigurationItem(String.Empty, "Generate a new key by accessing your account profile settings at <a href=\"https://nebulance.io/\" target=_blank>Nebulance</a>, scroll down to the <b>API Keys</b> section, tick the <i>New Key</i>, <i>list</i> and <i>download</i> checkboxes and save."));

        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            IsConfigured = false;
            var apiKey = configData.Key;
            if (apiKey.Value.Length != KeyLength)
                throw new Exception($"Invalid API Key configured: expected length: {KeyLength}, got {apiKey.Value.Length}");

            try
            {
                var results = await PerformQuery(new TorznabQuery());
                if (!results.Any())
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

        private string JsonRPCRequest(string method, JArray parameters)
        {
            dynamic request = new JObject();
            request["jsonrpc"] = "2.0";
            request["method"] = method;
            request["params"] = parameters;
            request["id"] = Guid.NewGuid().ToString().Substring(0, 8);

            return request.ToString();
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var validList = new List<string>
            {
                "action",
                "adventure",
                "children",
                "biography",
                "comedy",
                "crime",
                "documentary",
                "drama",
                "family",
                "fantasy",
                "game-show",
                "history",
                "horror",
                "medical",
                "music",
                "musical",
                "mystery",
                "news",
                "reality-tv",
                "romance",
                "sci-fi",
                "sitcom",
                "sport",
                "talk-show",
                "thriller",
                "travel",
                "war",
                "western"
            };
            var validCats = new List<string>
            {
                "sd",
                "hd",
                "uhd",
                "4k",
                "480p",
                "720p",
                "1080i",
                "1080p",
                "2160p"
            };

            var searchParam = new JObject
            {
                ["age"] = ">0"
            };

            var searchString = query.GetQueryString();
            if (!string.IsNullOrWhiteSpace(searchString))
                searchParam["name"] = "%" + Regex.Replace(searchString, @"[ -._]+", "%").Trim() + "%";

            if (query.IsGenreQuery)
            {
                var genre = new JArray
                {
                    new JValue(query.Genre)
                };
                searchParam["tags"] = genre;
            }

            var limit = query.Limit;
            if (limit == 0)
                limit = (int)TorznabCaps.LimitsDefault;
            var offset = query.Offset;

            var parameters = new JArray
            {
                new JValue(configData.Key.Value),
                JObject.FromObject(searchParam),
                new JValue(limit),
                new JValue(offset)
            };

            var response = await RequestWithCookiesAndRetryAsync(
                APIUrl, method: RequestType.POST,
                headers: new Dictionary<string, string>
                {
                    {"Accept", "application/json-rpc, application/json"},
                    {"Content-Type", "application/json-rpc"}
                }, rawbody: JsonRPCRequest("getTorrents", parameters), emulateBrowser: false);

            if (response.ContentString != null && response.ContentString.Contains("Invalid params"))
                throw new Exception("Invalid API Key configured");

            char[] delimiters = { ',', ' ', '/', ')', '(', '.', ';', '[', ']', '"', '|', ':' };

            var releases = new List<ReleaseInfo>();

            try
            {
                var jsonContent = JObject.Parse(response.ContentString);

                foreach (var item in jsonContent.Value<JObject>("result").Value<JArray>("items"))
                {
                    var link = new Uri(item.Value<string>("download"));
                    var details = new Uri($"{SiteLink}torrents.php?id={item.Value<string>("group_id")}");

                    var descriptions = new List<string>();
                    if (!string.IsNullOrWhiteSpace(item.Value<string>("group_name")))
                        descriptions.Add("Group Name: " + item.Value<string>("group_name"));
                    var tags = string.Join(",", item.Value<JArray>("tags"));
                    var releaseGenres = validList.Intersect(tags.ToLower().Split(delimiters, StringSplitOptions.RemoveEmptyEntries)).ToList();
                    descriptions.Add("Tags: " + string.Join(",", releaseGenres));
                    var releaseCats = validCats.Intersect(tags.ToLower().Split(delimiters, StringSplitOptions.RemoveEmptyEntries)).ToList();

                    var release = new ReleaseInfo
                    {
                        Guid = link,
                        Link = link,
                        Details = details,
                        Title = item.Value<string>("rls_name").Trim(),
                        Category = MapTrackerCatToNewznab(releaseCats.Any() ? releaseCats.First() : "TV"),
                        PublishDate = DateTime.Parse(item.Value<string>("rls_utc"), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
                        Seeders = item.Value<int>("seed"),
                        Peers = item.Value<int>("seed") + item.Value<int>("leech"),
                        Size = item.Value<long>("size"),
                        Files = item.Value<JArray>("file_list").Count,
                        Grabs = item.Value<int>("snatch"),
                        DownloadVolumeFactor = 0, // ratioless
                        UploadVolumeFactor = 1,
                        MinimumRatio = 0, // ratioless
                        MinimumSeedTime = item.Value<string>("cat").ToLower() == "season" ? 432000 : 86400, // 120 hours for seasons and 24 hours for episodes
                        Description = string.Join("<br />\n", descriptions)
                    };

                    if (release.Genres == null)
                        release.Genres = new List<string>();
                    release.Genres = releaseGenres;

                    var banner = item.Value<string>("series_banner");
                    if (!string.IsNullOrEmpty(banner) && !banner.Contains("noimage.png"))
                        release.Poster = new Uri(banner);

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.ContentString, ex);
            }

            return releases;
        }
    }
}
