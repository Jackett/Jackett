using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    public class HDBitsApi : BaseWebIndexer
    {
        private string APIUrl => SiteLink + "api/";

        private new ConfigurationDataHDBitsApi configData
        {
            get => (ConfigurationDataHDBitsApi)base.configData;
            set => base.configData = value;
        }

        public HDBitsApi(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(name: "HDBits (API)",
                description: "The HighDefinition Bittorrent Community",
                link: "https://hdbits.org/",
                caps: new TorznabCapabilities(),
                configService: configService,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataHDBitsApi())
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";
            TorznabCaps.SupportsImdbMovieSearch = true;

            AddCategoryMapping(6, TorznabCatType.Audio, "Audio Track");
            AddCategoryMapping(3, TorznabCatType.TVDocumentary, "Documentary");
            AddCategoryMapping(8, TorznabCatType.Other, "Misc/Demo");
            AddCategoryMapping(1, TorznabCatType.Movies, "Movie");
            AddCategoryMapping(4, TorznabCatType.Audio, "Music");
            AddCategoryMapping(5, TorznabCatType.TVSport, "Sport");
            AddCategoryMapping(2, TorznabCatType.TV, "TV");
            AddCategoryMapping(7, TorznabCatType.XXX, "XXX");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            dynamic requestData = new JObject();

            try
            {
                await MakeApiRequest("test", requestData);
            }
            catch (Exception e)
            {
                throw new ExceptionWithConfigData(e.Message, configData);
            }

            IsConfigured = true;

            SaveConfig();

            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            dynamic requestData = new JObject();
            var queryString = query.GetQueryString();
            var imdbId = ParseUtil.GetImdbID(query.ImdbID);

            if (imdbId != null)
            {
                requestData["imdb"] = new JObject();
                requestData["imdb"]["id"] = imdbId;
            }
            else if (!string.IsNullOrWhiteSpace(queryString))
            {
                requestData["search"] = queryString;
            }

            var categories = MapTorznabCapsToTrackers(query);

            if (categories.Count > 0)
            {
                requestData["category"] = new JArray();

                foreach (var cat in categories)
                {
                    requestData["category"].Add(new JValue(cat));
                }
            }

            if (configData.Codecs.Values.Length > 0)
            {
                requestData["codec"] = new JArray();

                foreach (var codec in configData.Codecs.Values)
                {
                    requestData["codec"].Add(new JValue(int.Parse(codec)));
                }
            }

            if (configData.Mediums.Values.Length > 0)
            {
                requestData["medium"] = new JArray();

                foreach (var medium in configData.Mediums.Values)
                {
                    requestData["medium"].Add(new JValue(int.Parse(medium)));
                }
            }

            requestData["limit"] = 100;

            var response = await MakeApiRequest("torrents", requestData);
            var releases = new List<ReleaseInfo>();
            foreach (JObject r in response["data"])
            {
                var link = new Uri(
                    SiteLink + "download.php/" + (string)r["filename"] + "?id=" + (string)r["id"] + "&passkey=" +
                    configData.Passkey.Value);
                var seeders = (int)r["seeders"];
                var publishDate = DateTimeUtil.UnixTimestampToDateTime((int)r["utadded"]);
                var release = new ReleaseInfo
                {
                    Title = (string)r["name"],
                    Comments = new Uri(SiteLink + "details.php?id=" + (string)r["id"]),
                    Link = link,
                    Category = MapTrackerCatToNewznab((string)r["type_category"]),
                    Size = (long)r["size"],
                    Files = (long)r["numfiles"],
                    Grabs = (long)r["times_completed"],
                    Seeders = seeders,
                    PublishDate = publishDate,
                    UploadVolumeFactor = GetUploadFactor(r),
                    DownloadVolumeFactor = GetDownloadFactor(r),
                    Guid = link,
                    Peers = seeders + (int)r["leechers"]
                };

                if (r.ContainsKey("imdb"))
                {
                    release.Imdb = ParseUtil.GetImdbID((string)r["imdb"]["id"]);
                }

                if (r.ContainsKey("tvdb"))
                {
                    release.TVDBId = (long)r["tvdb"]["id"];
                }

                releases.Add(release);
            }

            return releases;
        }

        private static double GetUploadFactor(JObject r) => (int)r["type_category"] == 7 ? 0 : 1;

        private static double GetDownloadFactor(JObject r)
        {
            var halfLeechMediums = new[] { 1, 5, 4 };
            // 100% Neutral Leech: all XXX content.
            if ((int)r["type_category"] == 7)
                return 0;
            // 100% Free Leech: all blue torrents.
            if ((string)r["freeleech"] == "yes")
                return 0;
            // 50% Free Leech: all full discs, remuxes, caps and all internal encodes.
            if (halfLeechMediums.Contains((int)r["type_medium"]) || (int)r["type_origin"] == 1)
                return 0.5;
            if ((int)r["type_category"] == 2 && (int)r["type_origin"] != 1)
                return 0.75;
            return 1;
        }

        private async Task<JObject> MakeApiRequest(string url, JObject requestData)
        {
            requestData["username"] = configData.Username.Value;
            requestData["passkey"] = configData.Passkey.Value;
            JObject json = null;

            var response = await PostDataWithCookiesAndRetry(APIUrl + url, null, null, null, new Dictionary<string, string>()
            {
                {"Accept", "application/json"},
                {"Content-Type", "application/json"}
            }, requestData.ToString(), false);

            CheckTrackerDown(response);

            try
            {
                json = JObject.Parse(response.Content);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while parsing json: " + response.Content, ex);
            }

            if ((int)json["status"] != 0)
            {
                throw new Exception("HDBits returned an error with status code " + (int)json["status"] + ": " + (string)json["message"]);
            }

            return json;
        }
    }
}

