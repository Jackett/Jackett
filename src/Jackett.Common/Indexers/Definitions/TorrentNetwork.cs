using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class TorrentNetwork : IndexerBase
    {
        public override string Id => "torrentnetwork";
        public override string Name => "Torrent Network";
        public override string Description => "Torrent Network (TN) is a GERMAN Private site for TV / MOVIES / GENERAL";
        public override string SiteLink { get; protected set; } = "https://tntracker.org/";
        public override string Language => "de-DE";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private string APIUrl => SiteLink + "api/";
        private string passkey;

        private readonly Dictionary<string, string> APIHeaders = new Dictionary<string, string>
        {
            {"Content-Type", "application/json"}
        };

        private new ConfigurationDataBasicLogin configData
        {
            get => (ConfigurationDataBasicLogin)base.configData;
            set => base.configData = value;
        }

        public TorrentNetwork(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps, ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataBasicLogin())
        {
            configData.AddDynamic("token", new HiddenStringConfigurationItem("token"));
            configData.AddDynamic("passkey", new HiddenStringConfigurationItem("passkey"));
            configData.AddDynamic("freeleech", new BoolConfigurationItem("Filter freeleech only") { Value = false });
            configData.AddDynamic("Account Inactivity", new DisplayInfoConfigurationItem("Account Inactivity", "After four weeks of inactivity (six weeks for power users) your account will be deleted (= finally and irrevocably removed from the database). To avoid deletion, you can park your account. Running torrents do not count as active, only logging in counts!!!"));
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.Genre
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q, MovieSearchParam.Genre
                },
                MusicSearchParams = new List<MusicSearchParam>
                {
                    MusicSearchParam.Q, MusicSearchParam.Genre
                },
                BookSearchParams = new List<BookSearchParam>
                {
                    BookSearchParam.Q, BookSearchParam.Genre
                }
            };

            caps.Categories.AddCategoryMapping(24, TorznabCatType.MoviesSD, "Movies GER/SD");
            caps.Categories.AddCategoryMapping(18, TorznabCatType.MoviesHD, "Movies GER/720p");
            caps.Categories.AddCategoryMapping(17, TorznabCatType.MoviesHD, "Movies GER/1080p");
            caps.Categories.AddCategoryMapping(20, TorznabCatType.MoviesUHD, "Movies GER/2160p");
            caps.Categories.AddCategoryMapping(45, TorznabCatType.MoviesOther, "Movies GER/Remux");
            caps.Categories.AddCategoryMapping(19, TorznabCatType.MoviesBluRay, "Movies GER/BluRay");
            caps.Categories.AddCategoryMapping(34, TorznabCatType.TVAnime, "Movies GER/Anime");
            caps.Categories.AddCategoryMapping(36, TorznabCatType.Movies3D, "Movies GER/3D");

            caps.Categories.AddCategoryMapping(22, TorznabCatType.MoviesSD, "Movies ENG/SD");
            caps.Categories.AddCategoryMapping(35, TorznabCatType.MoviesHD, "Movies ENG/720p");
            caps.Categories.AddCategoryMapping(43, TorznabCatType.MoviesHD, "Movies ENG/1080p");
            caps.Categories.AddCategoryMapping(48, TorznabCatType.MoviesUHD, "Movies ENG/2160p");
            caps.Categories.AddCategoryMapping(46, TorznabCatType.MoviesOther, "Movies ENG/Remux");
            caps.Categories.AddCategoryMapping(38, TorznabCatType.MoviesBluRay, "Movies ENG/BluRay");
            caps.Categories.AddCategoryMapping(39, TorznabCatType.TVAnime, "Movies ENG/Anime");

            caps.Categories.AddCategoryMapping(27, TorznabCatType.TVSD, "Series GER/SD");
            caps.Categories.AddCategoryMapping(28, TorznabCatType.TVHD, "Series GER/HD");
            caps.Categories.AddCategoryMapping(2, TorznabCatType.TVAnime, "Series GER/Anime");
            caps.Categories.AddCategoryMapping(16, TorznabCatType.TV, "Series GER/Pack");
            caps.Categories.AddCategoryMapping(6, TorznabCatType.TVDocumentary, "Docu/SD");
            caps.Categories.AddCategoryMapping(7, TorznabCatType.TVDocumentary, "Docu/HD");

            caps.Categories.AddCategoryMapping(29, TorznabCatType.TVSD, "Series ENG/SD");
            caps.Categories.AddCategoryMapping(40, TorznabCatType.TVHD, "Series ENG/HD");
            caps.Categories.AddCategoryMapping(41, TorznabCatType.TVAnime, "Series ENG/Anime");
            caps.Categories.AddCategoryMapping(42, TorznabCatType.TV, "Series ENG/Pack");
            caps.Categories.AddCategoryMapping(31, TorznabCatType.TVSport, "Sport");

            caps.Categories.AddCategoryMapping(10, TorznabCatType.PCGames, "Games/Win");
            caps.Categories.AddCategoryMapping(12, TorznabCatType.ConsoleWii, "Games/Wii");
            caps.Categories.AddCategoryMapping(13, TorznabCatType.ConsolePS4, "Games/PSX");
            caps.Categories.AddCategoryMapping(14, TorznabCatType.ConsoleXBox, "Games/XBOX");

            caps.Categories.AddCategoryMapping(4, TorznabCatType.PCMac, "Apps/Mac");
            caps.Categories.AddCategoryMapping(5, TorznabCatType.PC0day, "Apps/Win");

            caps.Categories.AddCategoryMapping(1, TorznabCatType.AudioAudiobook, "Misc/aBook");
            caps.Categories.AddCategoryMapping(8, TorznabCatType.Books, "Misc/eBook");
            caps.Categories.AddCategoryMapping(30, TorznabCatType.Other, "Misc/Sonstiges");

            caps.Categories.AddCategoryMapping(44, TorznabCatType.AudioLossless, "Musik/Flac");
            caps.Categories.AddCategoryMapping(25, TorznabCatType.AudioMP3, "Musik/MP3");
            caps.Categories.AddCategoryMapping(26, TorznabCatType.AudioVideo, "Musik/Video");

            caps.Categories.AddCategoryMapping(32, TorznabCatType.XXX, "XXX/XXX");
            caps.Categories.AddCategoryMapping(33, TorznabCatType.XXX, "XXX/XXX|HD");

            return caps;
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

            if (query.IsGenreQuery)
                queryCollection.Add("genre", query.Genre);

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
                    var downloadVolumeFactor = (long)torrent[10] switch
                    {
                        // Only Up
                        2 => 0,
                        // 50 % Down
                        1 => 0.5,
                        // All others 100% down
                        _ => 1
                    };
                    if (((BoolConfigurationItem)configData.GetDynamic("freeleech")).Value &&
                        downloadVolumeFactor != 0)
                        continue;
                    var torrentID = (long)torrent[2];
                    var details = new Uri(SiteLink + "torrent/" + torrentID);
                    //var preDelaySeconds = (long)torrent[4];
                    var seeders = (int)torrent[6];
                    //var imdbRating = (double)torrent[8] / 10;
                    var genres = torrent[9].ToString().Trim(',');
                    var description = "";
                    if (!string.IsNullOrWhiteSpace(genres))
                        description = "Genres: " + genres;
                    // 12/13/14 unknown, probably IDs/name of the uploader
                    //var row12 = (long)torrent[12];
                    //var row13 = (string)torrent[13];
                    //var row14 = (long)torrent[14];
                    var link = new Uri(SiteLink + "sdownload/" + torrentID + "/" + passkey);
                    var publishDate = DateTimeUtil.UnixTimestampToDateTime((double)torrent[3]);
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
                        Description = description,
                        UploadVolumeFactor = 1,
                        DownloadVolumeFactor = downloadVolumeFactor,
                        Grabs = (long)torrent[11]
                    };
                    if (release.Genres == null)
                        release.Genres = new List<string>();
                    release.Genres = release.Genres.Union(genres.Split(',')).ToList();
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
