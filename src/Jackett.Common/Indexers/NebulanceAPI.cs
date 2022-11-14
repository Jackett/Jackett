using System;
using System.Collections;
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
using Microsoft.AspNetCore.Http.Internal;
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
            var ValidList = new List<string>() {
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
            var ValidCats = new List<string>() {
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

            var searchParam = new JObject();
            var searchString = query.GetQueryString();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchParam["name"] = "%" + Regex.Replace(searchString, @"[ -._]", "%").Trim() + "%";
            }
            else
            {
                searchParam["name"] = "%";
            }
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
            var releases = new List<ReleaseInfo>();

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
                throw new Exception($"Invalid API Key configured");
            try
            {
                var json = JObject.Parse(response.ContentString);
                foreach (var r in json["result"]["items"].Cast<JObject>())
                {
                    var descriptions = new List<string>();
                    if (!string.IsNullOrWhiteSpace((string)r["group_name"]))
                        descriptions.Add("Group Name: " + (string)r["group_name"]);
                    var link = new Uri((string)r["download"]);
                    var details = new Uri($"{SiteLink}torrents.php?id={(string)r["group_id"]}");
                    var publishDate = DateTime.ParseExact((string)r["rls_utc"] + " +00:00", "yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);
                    var tags = string.Join(",", r["tags"]);
                    char[] delimiters = { ',', ' ', '/', ')', '(', '.', ';', '[', ']', '"', '|', ':' };
                    var releaseGenres = ValidList.Intersect(tags.ToLower().Split(delimiters, System.StringSplitOptions.RemoveEmptyEntries)).ToList();
                    descriptions.Add("Tags: " + string.Join(",", releaseGenres));
                    var releaseCats = ValidCats.Intersect(tags.ToLower().Split(delimiters, System.StringSplitOptions.RemoveEmptyEntries)).ToList();
                    var release = new ReleaseInfo
                    {
                        Title = (string)r["rls_name"],
                        Category = MapTrackerCatToNewznab(releaseCats.Any() ? releaseCats.First() : "TV"),
                        Details = details,
                        Guid = link,
                        Link = link,
                        PublishDate = publishDate,
                        Seeders = (int)r["seed"],
                        Peers = (int)r["seed"] + (int)r["leech"],
                        Size = (long)r["size"],
                        Grabs = (int)r["snatch"],
                        UploadVolumeFactor = 1,
                        DownloadVolumeFactor = 0, // ratioless
                        MinimumRatio = 0, // ratioless
                        MinimumSeedTime = 86400, // 24 hours
                        Description = string.Join("<br />\n", descriptions)
                    };
                    if (release.Genres == null)
                        release.Genres = new List<string>();
                    release.Genres = releaseGenres;
                    var banner = (string)r["series_banner"];
                    if ((!string.IsNullOrEmpty(banner)) && (!banner.Contains("noimage.png")))
                        release.Poster = new Uri((string)r["series_banner"]);

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
