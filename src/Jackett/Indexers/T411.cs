using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Jackett.Models.IndexerConfig;

namespace Jackett.Indexers
{
    public class T411 : BaseIndexer, IIndexer
    {
        private readonly string CommentsUrl = "";
        const string ApiUrl = "http://api.t411.in";
        const string AuthUrl = ApiUrl + "/auth";
        const string SearchUrl = ApiUrl + "/torrents/search/{0}";
        const string DownloadUrl = ApiUrl + "/torrents/download/{0}";

        HttpClientHandler handler;
        HttpClient client;

        new ConfigurationDataLoginTokin configData
        {
            get { return (ConfigurationDataLoginTokin)base.configData; }
            set { base.configData = value; }
        }

        public T411(IIndexerManagerService i, Logger l, IWebClient wc, IProtectionService ps)
            : base(name: "T411",
                description: "French Torrent Tracker",
                link: "http://www.t411.in/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataLoginTokin())
        {
            CommentsUrl = SiteLink + "/torrents/{0}";
            IsConfigured = false;
            handler = new HttpClientHandler
            {
                AllowAutoRedirect = true
            };
            client = new HttpClient(handler);

            AddCategoryMapping("624", TorznabCatType.Console);
            AddCategoryMapping("307", TorznabCatType.ConsoleNDS);
            AddCategoryMapping("308", TorznabCatType.ConsolePSP);
            AddCategoryMapping("307", TorznabCatType.ConsoleWii);
            AddCategoryMapping("309", TorznabCatType.ConsoleXbox);
            AddCategoryMapping("309", TorznabCatType.ConsoleXbox360);
            AddCategoryMapping("307", TorznabCatType.ConsoleWiiwareVC);
            AddCategoryMapping("309", TorznabCatType.ConsoleXBOX360DLC);
            AddCategoryMapping("308", TorznabCatType.ConsolePS3);
            AddCategoryMapping("239", TorznabCatType.ConsoleOther);
            AddCategoryMapping("245", TorznabCatType.ConsoleOther);
            AddCategoryMapping("246", TorznabCatType.ConsoleOther);
            AddCategoryMapping("626", TorznabCatType.ConsoleOther);
            AddCategoryMapping("628", TorznabCatType.ConsoleOther);
            AddCategoryMapping("630", TorznabCatType.ConsoleOther);
            AddCategoryMapping("307", TorznabCatType.Console3DS);
            AddCategoryMapping("308", TorznabCatType.ConsolePSVita);
            AddCategoryMapping("307", TorznabCatType.ConsoleWiiU);
            AddCategoryMapping("309", TorznabCatType.ConsoleXboxOne);
            AddCategoryMapping("308", TorznabCatType.ConsolePS4);
            AddCategoryMapping("631", TorznabCatType.Movies);
            AddCategoryMapping("631", TorznabCatType.MoviesForeign);
            AddCategoryMapping("455", TorznabCatType.MoviesOther);
            AddCategoryMapping("633", TorznabCatType.MoviesOther);
            AddCategoryMapping("631", TorznabCatType.MoviesSD);
            AddCategoryMapping("631", TorznabCatType.MoviesHD);
            AddCategoryMapping("631", TorznabCatType.Movies3D);
            AddCategoryMapping("631", TorznabCatType.MoviesBluRay);
            AddCategoryMapping("631", TorznabCatType.MoviesDVD);
            AddCategoryMapping("631", TorznabCatType.MoviesWEBDL);
            AddCategoryMapping("395", TorznabCatType.Audio);
            AddCategoryMapping("623", TorznabCatType.AudioMP3);
            AddCategoryMapping("400", TorznabCatType.AudioVideo);
            AddCategoryMapping("402", TorznabCatType.AudioVideo);
            AddCategoryMapping("405", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("403", TorznabCatType.AudioOther);
            AddCategoryMapping("642", TorznabCatType.AudioOther);
            AddCategoryMapping("233", TorznabCatType.PC);
            AddCategoryMapping("236", TorznabCatType.PC);
            AddCategoryMapping("235", TorznabCatType.PCMac);
            AddCategoryMapping("627", TorznabCatType.PCPhoneOther);
            AddCategoryMapping("246", TorznabCatType.PCGames);
            AddCategoryMapping("625", TorznabCatType.PCPhoneIOS);
            AddCategoryMapping("625", TorznabCatType.PCPhoneAndroid);
            AddCategoryMapping("639", TorznabCatType.TV);
            AddCategoryMapping("433", TorznabCatType.TV);
            AddCategoryMapping("639", TorznabCatType.TVWEBDL);
            AddCategoryMapping("433", TorznabCatType.TVWEBDL);
            AddCategoryMapping("639", TorznabCatType.TVFOREIGN);
            AddCategoryMapping("433", TorznabCatType.TVFOREIGN);
            AddCategoryMapping("639", TorznabCatType.TVSD);
            AddCategoryMapping("433", TorznabCatType.TVSD);
            AddCategoryMapping("639", TorznabCatType.TVHD);
            AddCategoryMapping("433", TorznabCatType.TVHD);
            AddCategoryMapping("635", TorznabCatType.TVOTHER);
            AddCategoryMapping("636", TorznabCatType.TVSport);
            AddCategoryMapping("637", TorznabCatType.TVAnime);
            AddCategoryMapping("634", TorznabCatType.TVDocumentary);
            AddCategoryMapping("340", TorznabCatType.Other);
            AddCategoryMapping("342", TorznabCatType.Other);
            AddCategoryMapping("344", TorznabCatType.Other);
            AddCategoryMapping("391", TorznabCatType.Other);
            AddCategoryMapping("392", TorznabCatType.Other);
            AddCategoryMapping("393", TorznabCatType.Other);
            AddCategoryMapping("394", TorznabCatType.Other);
            AddCategoryMapping("234", TorznabCatType.Other);
            AddCategoryMapping("638", TorznabCatType.Other);
            AddCategoryMapping("629", TorznabCatType.Other);
            AddCategoryMapping("408", TorznabCatType.Books);
            AddCategoryMapping("404", TorznabCatType.BooksEbook);
            AddCategoryMapping("406", TorznabCatType.BooksComics);
            AddCategoryMapping("407", TorznabCatType.BooksComics);
            AddCategoryMapping("409", TorznabCatType.BooksComics);
            AddCategoryMapping("410", TorznabCatType.BooksMagazines);
        }

        async Task<string> GetAuthToken(bool forceFetch = false)
        {
            if (!forceFetch && configData.LastTokenFetchDateTime > DateTime.Now - TimeSpan.FromHours(48))
            {
                return configData.ApiToken.Value;
            }

            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value }
            };

            var content = new FormUrlEncodedContent(pairs);

            var response = await client.PostAsync(AuthUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonResponse = JObject.Parse(responseContent);
            if (jsonResponse["error"] != null)
            {
                throw new ApplicationException((string)jsonResponse["error"]);
            }
            configData.ApiToken.Value = (string)jsonResponse["token"];
            configData.LastTokenFetchDateTime = DateTime.Now;
            return configData.ApiToken.Value;
        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);

            Exception tokenFetchEx = null;
            try
            {
                await GetAuthToken(true);
            }
            catch (Exception ex)
            {
                tokenFetchEx = new ExceptionWithConfigData(ex.Message, configData);
            }

            await ConfigureIfOK(string.Empty, tokenFetchEx == null, () =>
            {
                throw tokenFetchEx;
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        // Override to load legacy config format
        public override void LoadFromSavedConfiguration(JToken jsonConfig)
        {
            if (jsonConfig is JObject)
            {
                configData.ApiToken.Value = jsonConfig.Value<string>("token"); ;
                configData.Username.Value = jsonConfig.Value<string>("username");
                configData.Password.Value = jsonConfig.Value<string>("password");
                SaveConfig();
                IsConfigured = true;
                return;
            }

            base.LoadFromSavedConfiguration(jsonConfig);
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchTerm = string.IsNullOrEmpty(query.SanitizedSearchTerm) ? "%20" : query.SanitizedSearchTerm;
            var searchString = searchTerm + " " + query.GetEpisodeSearchString();
            var episodeSearchUrl = string.Format(SearchUrl, HttpUtility.UrlEncode(searchString));

            var message = new HttpRequestMessage();
            message.Method = HttpMethod.Get;
            message.RequestUri = new Uri(episodeSearchUrl);
            message.Headers.TryAddWithoutValidation("Authorization", await GetAuthToken());

            var response = await client.SendAsync(message);
            var results = await response.Content.ReadAsStringAsync();

            var jsonResult = JObject.Parse(results);
            try
            {
                var items = (JArray)jsonResult["torrents"];
                foreach (var item in items)
                {
                    var release = new ReleaseInfo();

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;
                    var torrentId = (string)item["id"];
                    release.Link = new Uri(string.Format(DownloadUrl, torrentId));
                    release.Title = (string)item["name"];
                    release.Description = release.Title;
                    release.Comments = new Uri(string.Format(CommentsUrl, (string)item["rewritename"]));
                    release.Guid = release.Comments;

                    var dateUtc = DateTime.ParseExact((string)item["added"], "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    release.PublishDate = DateTime.SpecifyKind(dateUtc, DateTimeKind.Utc).ToLocalTime();

                    release.Seeders = ParseUtil.CoerceInt((string)item["seeders"]);
                    release.Peers = ParseUtil.CoerceInt((string)item["leechers"]) + release.Seeders;
                    release.Size = ParseUtil.CoerceLong((string)item["size"]);
                    release.Category = MapTrackerCatToNewznab((string)item["categoryname"]);

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results, ex);
            }
            return releases;
        }

        public override async Task<byte[]> Download(Uri link)
        {
            var message = new HttpRequestMessage();
            message.Method = HttpMethod.Get;
            message.RequestUri = link;
            message.Headers.TryAddWithoutValidation("Authorization", await GetAuthToken());

            var response = await client.SendAsync(message);
            return await response.Content.ReadAsByteArrayAsync();
        }
    }
}
