using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    [ExcludeFromCodeCoverage]
    public class HDBitsApi : BaseWebIndexer
    {
        private string APIUrl => SiteLink + "api/";

        private new ConfigurationDataHDBitsApi configData
        {
            get => (ConfigurationDataHDBitsApi)base.configData;
            set => base.configData = value;
        }

        public HDBitsApi(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "hdbitsapi",
                   name: "HDBits (API)",
                   description: "The HighDefinition Bittorrent Community",
                   link: "https://hdbits.org/",
                   caps: new TorznabCapabilities
                   {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.TvdbId
                       },
                       MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q, MovieSearchParam.ImdbId
                       }
                   },
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataHDBitsApi())
        {
            Encoding = Encoding.UTF8;
            Language = "en-US";
            Type = "private";

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
            var requestData = new JObject();
            var queryString = query.GetQueryString();
            var imdbId = ParseUtil.GetImdbID(query.ImdbID);

            if (imdbId != null)
                requestData["imdb"] = new JObject
                {
                    ["id"] = imdbId
                };
            else if (query.TvdbID != null)
            {
                requestData["tvdb"] = new JObject
                {
                    ["id"] = query.TvdbID
                };

                if (query.Season != 0)
                    requestData["tvdb"]["season"] = query.Season;

                if (!string.IsNullOrEmpty(query.Episode))
                    requestData["tvdb"]["episode"] = query.Episode;
            }
            else if (!string.IsNullOrWhiteSpace(queryString))
                requestData["search"] = queryString;

            var categories = MapTorznabCapsToTrackers(query);

            if (categories.Any())
                requestData.Add("category", JToken.FromObject(categories));

            if (configData.Codecs.Values.Any())
                requestData.Add("codec", JToken.FromObject(configData.Codecs.Values.Select(int.Parse)));

            if (configData.Mediums.Values.Any())
                requestData.Add("medium", JToken.FromObject(configData.Mediums.Values.Select(int.Parse)));

            if (configData.Origins.Values.Any())
                requestData.Add("origin", JToken.FromObject(configData.Origins.Values.Select(int.Parse)));

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
                var details = new Uri(SiteLink + "details.php?id=" + (string)r["id"]);
                var release = new ReleaseInfo
                {
                    Title = (string)r["name"],
                    Details = details,
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
                    release.Imdb = ParseUtil.GetImdbID((string)r["imdb"]["id"]);

                if (r.ContainsKey("tvdb"))
                    release.TVDBId = (long)r["tvdb"]["id"];

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
            // 25% Free Leech: all TV content that is not an internal encode.
            if ((int)r["type_category"] == 2 && (int)r["type_origin"] != 1)
                return 0.75;
            return 1;
        }

        private async Task<JObject> MakeApiRequest(string url, JObject requestData)
        {
            requestData["username"] = configData.Username.Value;
            requestData["passkey"] = configData.Passkey.Value;

            var response = await RequestWithCookiesAndRetryAsync(
                APIUrl + url, null, RequestType.POST, null, null,
                new Dictionary<string, string>
                {
                    {"Accept", "application/json"},
                    {"Content-Type", "application/json"}
                }, requestData.ToString(), false);
            CheckSiteDown(response);

            JObject json;
            try
            {
                json = JObject.Parse(response.ContentString);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while parsing json: " + response.ContentString, ex);
            }

            if ((int)json["status"] != 0)
                throw new Exception("HDBits returned an error with status code " + (int)json["status"] + ": " + (string)json["message"]);

            return json;
        }
    }
}
