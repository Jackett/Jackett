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



            AddCategoryMapping("Film\\/Vidéo", TorznabCatType.Movies);
            AddCategoryMapping("Vidéo-clips", TorznabCatType.Other);
            AddCategoryMapping("Série TV", TorznabCatType.TV);
            AddCategoryMapping("Animation", TorznabCatType.TVAnime);
            AddCategoryMapping("Film", TorznabCatType.Movies);
            AddCategoryMapping("Concert", TorznabCatType.AudioVideo);
            AddCategoryMapping("Documentaire", TorznabCatType.Audio);
            AddCategoryMapping("Spectacle", TorznabCatType.TV);
            AddCategoryMapping("Sport", TorznabCatType.TVSport);
            AddCategoryMapping("Animation Série", TorznabCatType.TVAnime);
            AddCategoryMapping("Emission TV", TorznabCatType.TV);


            AddCategoryMapping("Application", TorznabCatType.PC0day);
            AddCategoryMapping("Linux", TorznabCatType.PC);
            AddCategoryMapping("MacOS", TorznabCatType.PCMac);
            AddCategoryMapping("Windows", TorznabCatType.PC);
            AddCategoryMapping("Smartphone", TorznabCatType.PCPhoneOther);
            AddCategoryMapping("Tablette", TorznabCatType.PCPhoneOther);
            AddCategoryMapping("Autre", TorznabCatType.PC);
            AddCategoryMapping("Formation", TorznabCatType.PC);

            AddCategoryMapping("Emulation", TorznabCatType.PC);
            AddCategoryMapping("Emulateurs", TorznabCatType.PC);
            AddCategoryMapping("Roms", TorznabCatType.PC);

            AddCategoryMapping("GPS", TorznabCatType.Other);
            AddCategoryMapping("Applications", TorznabCatType.Other);
            AddCategoryMapping("Cartes", TorznabCatType.Other);
            AddCategoryMapping("Divers", TorznabCatType.Other);

            AddCategoryMapping("Audio", TorznabCatType.Audio);
            AddCategoryMapping("Karaoke", TorznabCatType.Audio);
            AddCategoryMapping("Samples", TorznabCatType.Audio);
            AddCategoryMapping("Musique", TorznabCatType.Audio);
            AddCategoryMapping("Podcast Radio", TorznabCatType.Audio);

            AddCategoryMapping("eBook", TorznabCatType.BooksEbook);
            AddCategoryMapping("Audio", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("Bds", TorznabCatType.AudioVideo);
            AddCategoryMapping("Comics", TorznabCatType.BooksComics);
            AddCategoryMapping("Livres", TorznabCatType.Books);
            AddCategoryMapping("Mangas", TorznabCatType.BooksForeign);
            AddCategoryMapping("Presse", TorznabCatType.BooksMagazines);

            AddCategoryMapping("xXx", TorznabCatType.XXX);
            AddCategoryMapping("eBooks", TorznabCatType.XXXImageset);
            AddCategoryMapping("Jeux vidéo", TorznabCatType.XXX);
            AddCategoryMapping("Video", TorznabCatType.XXXDVD);
            //AddCategoryMapping("Animation", TorznabCatType.XXX); Used above :/

            AddCategoryMapping("Jeu vidéo", TorznabCatType.PCGames);
            AddCategoryMapping("Linux", TorznabCatType.PCGames);
            AddCategoryMapping("MacOS", TorznabCatType.PCGames);
            // AddCategoryMapping("Windows", TorznabCatType.PCGames); Used above :/
            AddCategoryMapping("Nintendo", TorznabCatType.Console);
            AddCategoryMapping("Sony", TorznabCatType.Console);
            AddCategoryMapping("Microsoft", TorznabCatType.PCGames);
            AddCategoryMapping("Smartphone", TorznabCatType.PCPhoneOther);
            AddCategoryMapping("Tablette", TorznabCatType.PCPhoneOther);
            AddCategoryMapping("Autre", TorznabCatType.Other);

            AddCategoryMapping("Jeux vidéo", TorznabCatType.Other);
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
