using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    public class HDBitsApi : BaseWebIndexer
    {
        private string APIUrl => $"{SiteLink}api/";

        private new ConfigurationDataUserPasskey configData
        {
            get => (ConfigurationDataUserPasskey)base.configData;
            set => base.configData = value;
        }

        public HDBitsApi(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps) : base(
            "HDBits (API)", description: "The HighDefinition Bittorrent Community", link: "https://hdbits.org/",
            caps: new TorznabCapabilities(), configService: configService, client: wc, logger: l, p: ps,
            configData: new ConfigurationDataUserPasskey())
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
                await MakeApiRequestAsync("test", requestData);
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
                requestData["search"] = queryString;

            var categories = MapTorznabCapsToTrackers(query);
            if (categories.Count > 0)
            {
                requestData["category"] = new JArray();
                foreach (var cat in categories)
                    requestData["category"].Add(new JValue(cat));
            }

            requestData["limit"] = 100;
            var response = await MakeApiRequestAsync("torrents", requestData);
            var releases = new List<ReleaseInfo>();
            foreach (JObject r in response["data"])
            {
                var release = new ReleaseInfo
                {
                    Title = (string)r["name"],
                    Comments = new Uri($"{SiteLink}details.php?id={(string)r["id"]}"),
                    Link = new Uri(
                        $"{SiteLink}download.php/{(string)r["filename"]}?id={(string)r["id"]}&passkey={configData.Passkey.Value}")
                };
                release.Guid = release.Link;
                if (r.ContainsKey("imdb"))
                    release.Imdb = ParseUtil.GetImdbID((string)r["imdb"]["id"]);
                if (r.ContainsKey("tvdb"))
                    release.TVDBId = (long)r["tvdb"]["id"];
                release.UploadVolumeFactor = 1;
                int[] mediumsFor50 =
                {
                    1,
                    5,
                    4
                };

                // 100% Neutral Leech: all XXX content.
                if ((int)r["type_category"] == 7)
                {
                    release.DownloadVolumeFactor = 0;
                    release.UploadVolumeFactor = 0;
                }
                // 100% Free Leech: all blue torrents.
                else
                    release.DownloadVolumeFactor = (string)r["freeleech"] == "yes"
                   ? 0
                   : mediumsFor50.Contains((int)r["type_medium"]) || (int)r["type_origin"] == 1
                   ? 0.5
                   : (int)r["type_category"] == 2 && (int)r["type_origin"] != 1 ? 0.75 : 1;

                release.Category = MapTrackerCatToNewznab((string)r["type_category"]);
                release.Size = (long)r["size"];
                release.Files = (long)r["numfiles"];
                release.Grabs = (long)r["times_completed"];
                release.Seeders = (int)r["seeders"];
                release.Peers = release.Seeders + (int)r["leechers"];
                release.PublishDate = DateTimeUtil.UnixTimestampToDateTime((int)r["utadded"]);
                releases.Add(release);
            }

            return releases;
        }

        private async Task<JObject> MakeApiRequestAsync(string url, JObject requestData)
        {
            requestData["username"] = configData.Username.Value;
            requestData["passkey"] = configData.Passkey.Value;
            var response = await PostDataWithCookiesAndRetryAsync(
                APIUrl + url, null, null, null,
                new Dictionary<string, string> { { "Accept", "application/json" }, { "Content-Type", "application/json" } },
                requestData.ToString(), false);
            CheckTrackerDown(response);
            JObject json;
            try
            {
                json = JObject.Parse(response.Content);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error while parsing json: {response.Content}", ex);
            }

            if ((int)json["status"] != 0)
                throw new Exception(
                    $"HDBits returned an error with status code {(int)json["status"]}: {(string)json["message"]}");
            return json;
        }
    }
}
