using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jackett.Common.Extensions;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class HDBitsApi : IndexerBase
    {
        public override string Id => "hdbitsapi";
        public override string Name => "HDBits (API)";
        public override string Description => "The HighDefinition Bittorrent Community";
        public override string SiteLink { get; protected set; } = "https://hdbits.org/";
        public override string Language => "en-US";
        public override string Type => "private";
        public override bool SupportsPagination => true;

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private string APIUrl => SiteLink + "api/";

        private new ConfigurationDataHDBitsApi configData
        {
            get => (ConfigurationDataHDBitsApi)base.configData;
            set => base.configData = value;
        }

        public HDBitsApi(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataHDBitsApi())
        {
            webclient.requestDelay = 2;
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.TvdbId
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q, MovieSearchParam.ImdbId
                }
            };

            caps.Categories.AddCategoryMapping(1, TorznabCatType.Movies, "Movie");
            caps.Categories.AddCategoryMapping(2, TorznabCatType.TV, "TV");
            caps.Categories.AddCategoryMapping(3, TorznabCatType.TVDocumentary, "Documentary");
            caps.Categories.AddCategoryMapping(4, TorznabCatType.Audio, "Music");
            caps.Categories.AddCategoryMapping(5, TorznabCatType.TVSport, "Sport");
            caps.Categories.AddCategoryMapping(6, TorznabCatType.Audio, "Audio Track");
            caps.Categories.AddCategoryMapping(7, TorznabCatType.XXX, "XXX");
            caps.Categories.AddCategoryMapping(8, TorznabCatType.Other, "Misc/Demo");

            return caps;
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
            var imdbId = ParseUtil.GetImdbId(query.ImdbID);

            if (imdbId != null)
            {
                requestData["imdb"] = new JObject
                {
                    ["id"] = imdbId
                };
            }
            else if (query.TvdbID != null)
            {
                requestData["tvdb"] = new JObject
                {
                    ["id"] = query.TvdbID
                };

                if (DateTime.TryParseExact($"{query.Season} {query.Episode}", "yyyy MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var showDate))
                {
                    requestData["search"] = showDate.ToString("yyyy-MM-dd");
                }
                else
                {
                    if (query.Season.HasValue)
                    {
                        requestData["tvdb"]["season"] = query.Season;
                    }

                    if (query.Episode.IsNotNullOrWhiteSpace())
                    {
                        requestData["tvdb"]["episode"] = query.Episode;
                    }
                }
            }
            else if (!string.IsNullOrWhiteSpace(queryString))
            {
                requestData["search"] = Regex.Replace(queryString, "[\\W]+", " ").Trim();
            }

            var categories = MapTorznabCapsToTrackers(query);

            if (categories.Any())
            {
                requestData.Add("category", JToken.FromObject(categories));
            }

            if (configData.Codecs.Values.Any())
            {
                requestData.Add("codec", JToken.FromObject(configData.Codecs.Values.Select(int.Parse)));
            }

            if (configData.Mediums.Values.Any())
            {
                requestData.Add("medium", JToken.FromObject(configData.Mediums.Values.Select(int.Parse)));
            }

            if (configData.Origins.Values.Any())
            {
                requestData.Add("origin", JToken.FromObject(configData.Origins.Values.Select(int.Parse)));
            }

            requestData["limit"] = 100;

            if (query.Limit > 0 && query.Offset > 0)
            {
                requestData["page"] = query.Offset / query.Limit;
            }

            var response = await MakeApiRequest("torrents", requestData);

            var releases = new List<ReleaseInfo>();

            foreach (JObject r in response["data"])
            {
                if (configData.FilterFreeleech.Value && (string)r["freeleech"] != "yes")
                {
                    continue;
                }

                var title = GetTitle(r);

                // if tv then match query keywords against title #12753
                if (!query.IsImdbQuery && !query.MatchQueryStringAND(title))
                {
                    continue;
                }

                var link = new Uri(
                    SiteLink + "download.php/" + (string)r["filename"] + "?id=" + (string)r["id"] + "&passkey=" +
                    configData.Passkey.Value);
                var seeders = (int)r["seeders"];
                var publishDate = DateTimeUtil.UnixTimestampToDateTime((int)r["utadded"]);
                var details = new Uri(SiteLink + "details.php?id=" + (string)r["id"]);
                var release = new ReleaseInfo
                {
                    Title = title,
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
                {
                    release.Imdb = ParseUtil.GetImdbId((string)r["imdb"]["id"]);
                }

                if (r.ContainsKey("tvdb"))
                {
                    release.TVDBId = (long)r["tvdb"]["id"];
                }

                releases.Add(release);
            }

            return releases;
        }

        private string GetTitle(JObject item)
        {
            var filename = (string)item["filename"];
            var name = (string)item["name"];

            return configData.UseFilenames.Value && filename.IsNotNullOrWhiteSpace()
                ? filename.Replace(".torrent", "")
                : name;
        }

        private static double GetUploadFactor(JObject r) => (int)r["type_category"] == 7 ? 0 : 1;

        private static double GetDownloadFactor(JObject r)
        {
            // 100% Neutral Leech: all XXX content.
            if ((int)r["type_category"] == 7)
            {
                return 0;
            }

            // 100% Free Leech: all blue torrents.
            if ((string)r["freeleech"] == "yes")
            {
                return 0;
            }

            var halfLeechMediums = new[] { 1, 5, 4 };

            // 50% Free Leech: all full discs, remuxes, captures and all internal encodes, also all TV and Documentary content.
            if (halfLeechMediums.Contains((int)r["type_medium"]) || (int)r["type_origin"] == 1 || (int)r["type_category"] == 2 || (int)r["type_category"] == 3)
            {
                return 0.5;
            }

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
            {
                throw new Exception("HDBits returned an error with status code " + (int)json["status"] + ": " + (string)json["message"]);
            }

            return json;
        }
    }
}
