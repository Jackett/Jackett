using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
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
    public class TorrentNetwork : BaseWebIndexer
    {
        private string APIUrl => SiteLink + "api/";
        private string passkey;

        private readonly Dictionary<string, string> APIHeaders = new Dictionary<string, string>
        {
            {"Content-Type", "application/json"}
        };

        private new ConfigurationDataBasicLoginWithRSSAndDisplay configData
        {
            get => (ConfigurationDataBasicLoginWithRSSAndDisplay)base.configData;
            set => base.configData = value;
        }

        public TorrentNetwork(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "torrentnetwork",
                   name: "Torrent Network",
                   description: "Torrent Network (TN) is a GERMAN Private site for TV / MOVIES / GENERAL",
                   link: "https://tntracker.org/",
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
                   configData: new ConfigurationDataBasicLoginWithRSSAndDisplay())
        {
            Encoding = Encoding.UTF8;
            Language = "de-DE";
            Type = "private";

            configData.AddDynamic("token", new HiddenStringConfigurationItem("token"));
            configData.AddDynamic("passkey", new HiddenStringConfigurationItem("passkey"));

            AddCategoryMapping(24, TorznabCatType.MoviesSD, "Movies GER/SD");
            AddCategoryMapping(18, TorznabCatType.MoviesHD, "Movies GER/720p");
            AddCategoryMapping(17, TorznabCatType.MoviesHD, "Movies GER/1080p");
            AddCategoryMapping(20, TorznabCatType.MoviesUHD, "Movies GER/2160p");
            AddCategoryMapping(45, TorznabCatType.MoviesOther, "Movies GER/Remux");
            AddCategoryMapping(19, TorznabCatType.MoviesBluRay, "Movies GER/BluRay");
            AddCategoryMapping(34, TorznabCatType.TVAnime, "Movies GER/Anime");
            AddCategoryMapping(36, TorznabCatType.Movies3D, "Movies GER/3D");

            AddCategoryMapping(22, TorznabCatType.MoviesSD, "Movies ENG/SD");
            AddCategoryMapping(35, TorznabCatType.MoviesHD, "Movies ENG/720p");
            AddCategoryMapping(43, TorznabCatType.MoviesHD, "Movies ENG/1080p");
            AddCategoryMapping(48, TorznabCatType.MoviesUHD, "Movies ENG/2160p");
            AddCategoryMapping(46, TorznabCatType.MoviesOther, "Movies ENG/Remux");
            AddCategoryMapping(38, TorznabCatType.MoviesBluRay, "Movies ENG/BluRay");
            AddCategoryMapping(39, TorznabCatType.TVAnime, "Movies ENG/Anime");

            AddCategoryMapping(27, TorznabCatType.TVSD, "Series GER/SD");
            AddCategoryMapping(28, TorznabCatType.TVHD, "Series GER/HD");
            AddCategoryMapping(2, TorznabCatType.TVAnime, "Series GER/Anime");
            AddCategoryMapping(16, TorznabCatType.TV, "Series GER/Pack");
            AddCategoryMapping(6, TorznabCatType.TVDocumentary, "Docu/SD");
            AddCategoryMapping(7, TorznabCatType.TVDocumentary, "Docu/HD");

            AddCategoryMapping(29, TorznabCatType.TVSD, "Series ENG/SD");
            AddCategoryMapping(40, TorznabCatType.TVHD, "Series ENG/HD");
            AddCategoryMapping(41, TorznabCatType.TVAnime, "Series ENG/Anime");
            AddCategoryMapping(42, TorznabCatType.TV, "Series ENG/Pack");
            AddCategoryMapping(31, TorznabCatType.TVSport, "Sport");

            AddCategoryMapping(10, TorznabCatType.PCGames, "Games/Win");
            AddCategoryMapping(12, TorznabCatType.ConsoleWii, "Games/Wii");
            AddCategoryMapping(13, TorznabCatType.ConsolePS4, "Games/PSX");
            AddCategoryMapping(14, TorznabCatType.ConsoleXBox, "Games/XBOX");

            AddCategoryMapping(4, TorznabCatType.PCMac, "Apps/Mac");
            AddCategoryMapping(5, TorznabCatType.PC0day, "Apps/Win");

            AddCategoryMapping(1, TorznabCatType.AudioAudiobook, "Misc/aBook");
            AddCategoryMapping(8, TorznabCatType.Books, "Misc/eBook");
            AddCategoryMapping(30, TorznabCatType.Other, "Misc/Sonstiges");

            AddCategoryMapping(44, TorznabCatType.AudioLossless, "Musik/Flac");
            AddCategoryMapping(25, TorznabCatType.AudioMP3, "Musik/MP3");
            AddCategoryMapping(26, TorznabCatType.AudioVideo, "Musik/Video");

            AddCategoryMapping(32, TorznabCatType.XXX, "XXX/XXX");
            AddCategoryMapping(33, TorznabCatType.XXX, "XXX/XXX|HD");
        }

        public override void LoadValuesFromJson(JToken jsonConfig, bool useProtectionService = false)
        {
            base.LoadValuesFromJson(jsonConfig, useProtectionService);

            var tokenItem = (HiddenStringConfigurationItem)configData.GetDynamic("token");
            if (tokenItem != null)
            {
                var token = tokenItem.Value;
                if (!string.IsNullOrWhiteSpace(token))
                    APIHeaders["Authorization"] = token;
            }

            var passkeyItem = (HiddenStringConfigurationItem)configData.GetDynamic("passkey");
            if (passkeyItem != null)
            {
                passkey = passkeyItem.Value;
            }
        }

        private async Task<dynamic> SendAPIRequest(string endpoint, object data)
        {
            var jsonData = JsonConvert.SerializeObject(data);
            var result = await RequestWithCookiesAsync(
                APIUrl + endpoint, method: RequestType.POST, referer: SiteLink, headers: APIHeaders, rawbody: jsonData);
            if (!result.ContentString.StartsWith("{")) // not JSON => error
                throw new ExceptionWithConfigData(result.ContentString, configData);
            var json = JsonConvert.DeserializeObject<dynamic>(result.ContentString);
            return json;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            APIHeaders.Remove("Authorization"); // remove any token from the headers

            var pairs = new Dictionary<string, string>
            {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value }
            };

            var json = await SendAPIRequest("auth", pairs);
            string token = json.token;

            APIHeaders["Authorization"] = token;

            var curuser = await SendAPIRequest("curuser", null);
            if (string.IsNullOrWhiteSpace(curuser.passkey.ToString()))
                throw new ExceptionWithConfigData("got empty passkey: " + curuser.ToString(), configData);
            passkey = curuser.passkey;
            var passkeyItem = (HiddenStringConfigurationItem)configData.GetDynamic("passkey");
            passkeyItem.Value = passkey;

            var tokenItem = (HiddenStringConfigurationItem)configData.GetDynamic("token");
            tokenItem.Value = token;

            await ConfigureIfOK("", token.Length > 0, () =>
            {
                throw new ExceptionWithConfigData(json.ToString(), configData);
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var searchUrl = APIUrl + "browse";
            var searchString = query.GetQueryString();
            var queryCollection = new NameValueCollection
            {
                { "orderC", "4" },
                { "orderD", "desc" },
                { "start", "0" },
                { "length", "100" }
            };

            if (!string.IsNullOrWhiteSpace(searchString))
                queryCollection.Add("search", searchString);

            var cats = MapTorznabCapsToTrackers(query);
            if (cats.Count == 0)
                cats = GetAllTrackerCategories();
            queryCollection.Add("cats", string.Join(",", cats));

            searchUrl += "?" + queryCollection.GetQueryString();

            if (string.IsNullOrWhiteSpace(passkey))
                await ApplyConfiguration(null);

            var results = await RequestWithCookiesAndRetryAsync(searchUrl, referer: SiteLink, headers: APIHeaders);
            if (!results.ContentString.StartsWith("{")) // not JSON => error
                throw new ExceptionWithConfigData(results.ContentString, configData);
            var result = JsonConvert.DeserializeObject<dynamic>(results.ContentString);
            try
            {
                if (result["error"] != null)
                    throw new Exception(result["error"].ToString());

                var data = (JArray)result.data;

                foreach (JArray torrent in data)
                {
                    var torrentID = (long)torrent[2];
                    var details = new Uri(SiteLink + "torrent/" + torrentID);
                    //var preDelaySeconds = (long)torrent[4];
                    var seeders = (int)torrent[6];
                    //var imdbRating = (double)torrent[8] / 10;
                    var genres = (string)torrent[9];
                    if (!string.IsNullOrWhiteSpace(genres))
                        genres = "Genres: " + genres;
                    // 12/13/14 unknown, probably IDs/name of the uploader
                    //var row12 = (long)torrent[12];
                    //var row13 = (string)torrent[13];
                    //var row14 = (long)torrent[14];
                    var link = new Uri(SiteLink + "sdownload/" + torrentID + "/" + passkey);
                    var publishDate = DateTimeUtil.UnixTimestampToDateTime((double)torrent[3]);
                    var downloadVolumeFactor = (long)torrent[10] switch
                    {
                        // Only Up
                        2 => 0,
                        // 50 % Down
                        1 => 0.5,
                        // All others 100% down
                        _ => 1
                    };
                    var release = new ReleaseInfo
                    {
                        MinimumRatio = 0.8,
                        MinimumSeedTime = 172800, // 48 hours
                        Category = MapTrackerCatToNewznab(torrent[0].ToString()),
                        Title = torrent[1].ToString(),
                        Details = details,
                        Guid = details,
                        Link = link,
                        PublishDate = publishDate,
                        Size = (long)torrent[5],
                        Seeders = seeders,
                        Peers = seeders + (int)torrent[7],
                        Description = genres,
                        UploadVolumeFactor = 1,
                        DownloadVolumeFactor = downloadVolumeFactor,
                        Grabs = (long)torrent[11]
                    };
                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(result.ToString(), ex);
            }

            return releases;
        }
    }
}
