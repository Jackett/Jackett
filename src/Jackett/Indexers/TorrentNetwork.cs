using Jackett.Utils.Clients;
using NLog;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Models;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System;
using Jackett.Models.IndexerConfig;
using System.Collections.Specialized;
using System.Text;
using Newtonsoft.Json;
using static Jackett.Models.IndexerConfig.ConfigurationData;

namespace Jackett.Indexers
{
    public class TorrentNetwork : BaseWebIndexer
    {
        string APIUrl { get { return SiteLink + "api/"; } }
        private string passkey;

        private Dictionary<string, string> APIHeaders = new Dictionary<string, string>()
        {
            {"Content-Type", "application/json"},
        };

        new ConfigurationDataBasicLoginWithRSSAndDisplay configData
        {
            get { return (ConfigurationDataBasicLoginWithRSSAndDisplay)base.configData; }
            set { base.configData = value; }
        }

        public TorrentNetwork(IIndexerConfigurationService configService, IWebClient wc, Logger l, IProtectionService ps)
            : base(name: "Torrent Network",
                   description: null,
                   link: "https://tntracker.org/",
                   caps: new TorznabCapabilities(),
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   configData: new ConfigurationDataBasicLoginWithRSSAndDisplay())
        {
            Encoding = Encoding.GetEncoding("UTF-8");
            Language = "de-de";
            Type = "private";

            configData.AddDynamic("token", new HiddenItem() { Name = "token" });
            configData.AddDynamic("passkey", new HiddenItem() { Name = "passkey" });

            AddCategoryMapping(24, TorznabCatType.MoviesSD, "Movies GER/SD");
            AddCategoryMapping(18, TorznabCatType.MoviesHD, "Movies GER/720p");
            AddCategoryMapping(17, TorznabCatType.MoviesHD, "Movies GER/1080p");
            AddCategoryMapping(20, TorznabCatType.MoviesHD, "Movies GER/2160p");
            AddCategoryMapping(45, TorznabCatType.MoviesOther, "Movies GER/Remux");
            AddCategoryMapping(19, TorznabCatType.MoviesBluRay, "Movies GER/BluRay");
            AddCategoryMapping(34, TorznabCatType.TVAnime, "Movies GER/Anime");
            AddCategoryMapping(36, TorznabCatType.Movies3D, "Movies GER/3D");

            AddCategoryMapping(22, TorznabCatType.MoviesSD, "Movies ENG/SD");
            AddCategoryMapping(35, TorznabCatType.MoviesHD, "Movies ENG/720p");
            AddCategoryMapping(43, TorznabCatType.MoviesHD, "Movies ENG/1080p");
            AddCategoryMapping(37, TorznabCatType.MoviesHD, "Movies ENG/2160p");
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
            AddCategoryMapping(14, TorznabCatType.ConsoleXbox, "Games/XBOX");

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

            var tokenItem = (HiddenItem)configData.GetDynamic("token");
            if (tokenItem != null)
            {
                var token = tokenItem.Value;
                if (!string.IsNullOrWhiteSpace(token))
                    APIHeaders["Authorization"] = token;
            }

            var passkeyItem = (HiddenItem)configData.GetDynamic("passkey");
            if (passkeyItem != null)
            {
                passkey = passkeyItem.Value;
            }
        }

        private async Task<dynamic> SendAPIRequest(string endpoint, object data)
        {
            var jsonData = JsonConvert.SerializeObject(data);
            var result = await PostDataWithCookies(APIUrl + endpoint, null, null, SiteLink, APIHeaders, jsonData);
            if (!result.Content.StartsWith("{")) // not JSON => error
                throw new ExceptionWithConfigData(result.Content, configData);
            dynamic json = JsonConvert.DeserializeObject<dynamic>(result.Content);
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

            var passkeyItem = (HiddenItem)configData.GetDynamic("passkey");
            passkeyItem.Value = curuser.passkey;

            var tokenItem = (HiddenItem)configData.GetDynamic("token");
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

            var searchUrl = "browse";
            var searchString = query.GetQueryString();
            var queryCollection = new NameValueCollection();
            queryCollection.Add("orderC", "4");
            queryCollection.Add("orderD", "desc");
            queryCollection.Add("start", "0");
            queryCollection.Add("length", "100");

            if (!string.IsNullOrWhiteSpace(searchString))
                queryCollection.Add("search", searchString);

            var cats = MapTorznabCapsToTrackers(query);
            if (cats.Count > 0)
                queryCollection.Add("cats", string.Join(",", cats));

            searchUrl += "?" + queryCollection.GetQueryString();

            var result = await SendAPIRequest(searchUrl, null);
            try
            {
                if (result["error"] != null)
                    throw new Exception(result["error"].ToString());

                var data = (JArray)result.data;
                
                foreach (JArray torrent in data)
                {
                    var release = new ReleaseInfo();
                    release.MinimumRatio = 0.8;
                    release.MinimumSeedTime = 48 * 60 * 60;

                    release.Category = MapTrackerCatToNewznab(torrent[0].ToString());
                    release.Title = torrent[1].ToString();
                    var torrentID = (long)torrent[2];
                    release.Comments = new Uri(SiteLink + "torrent/" + torrentID);
                    release.Guid = release.Comments;
                    release.Link = new Uri(SiteLink + "sdownload/" + torrentID + "/" + passkey);
                    release.PublishDate = DateTimeUtil.UnixTimestampToDateTime((double)torrent[3]).ToLocalTime();
                    //var preDelaySeconds = (long)torrent[4];
                    release.Size = (long)torrent[5];
                    release.Seeders = (int)torrent[6];
                    release.Peers = release.Seeders + (int)torrent[7];
                    //var imdbRating = (double)torrent[8] / 10;
                    var genres = (string)torrent[9];
                    if (!string.IsNullOrWhiteSpace(genres))
                        release.Description = "Genres: " + genres;

                    var DownloadVolumeFlag = (long)torrent[10];
                    release.UploadVolumeFactor = 1;
                    if (DownloadVolumeFlag == 2) // Only Up
                        release.DownloadVolumeFactor = 0;
                    else if (DownloadVolumeFlag == 1) // 50 % Down
                        release.DownloadVolumeFactor = 0.5;
                    else if (DownloadVolumeFlag == 0)
                        release.DownloadVolumeFactor = 1;

                    release.Grabs = (long)torrent[11];

                    // 12/13/14 unknown, probably IDs/name of the uploader
                    //var row12 = (long)torrent[12];
                    //var row13 = (string)torrent[13];
                    //var row14 = (long)torrent[14];

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

