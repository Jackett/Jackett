using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Jackett.Models.IndexerConfig;

namespace Jackett.Indexers
{
    public class T411 : BaseIndexer, IIndexer
    {
        const string ApiUrl = "https://api.t411.li";
        const string AuthUrl = ApiUrl + "/auth";
        const string SearchUrl = ApiUrl + "/torrents/search/";
        const string TermsUrl = ApiUrl + "/terms/tree";
        const string DownloadUrl = ApiUrl + "/torrents/download/";
        private string CommentsUrl { get { return SiteLink + "torrents/"; } }

        new ConfigurationDataLoginTokin configData
        {
            get { return (ConfigurationDataLoginTokin)base.configData; }
            set { base.configData = value; }
        }

        public T411(IIndexerManagerService i, Logger l, IWebClient wc, IProtectionService ps)
            : base(name: "T411",
                description: "French Torrent Tracker",
                link: "https://t411.li/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataLoginTokin())
        {
            Encoding = Encoding.UTF8;
            Type = "semi-private";
            Language = "fr-fr";

            // 210, FilmVidéo
            AddCategoryMapping(402, TorznabCatType.Movies, "Vidéoclips");
            AddCategoryMapping(433, TorznabCatType.TV, "Série TV");
            AddCategoryMapping(455, TorznabCatType.TVAnime, "Animation");
            AddCategoryMapping(631, TorznabCatType.Movies, "Film");
            AddCategoryMapping(633, TorznabCatType.Movies, "Concert");
            AddCategoryMapping(634, TorznabCatType.TVDocumentary, "Documentaire");
            AddCategoryMapping(635, TorznabCatType.TV, "Spectacle");
            AddCategoryMapping(636, TorznabCatType.TVSport, "Sport");
            AddCategoryMapping(637, TorznabCatType.TVAnime, "Animation Série");
            AddCategoryMapping(639, TorznabCatType.TV, "Emission TV");

            // 233, Application
            AddCategoryMapping(234, TorznabCatType.PC0day, "Linux");
            AddCategoryMapping(235, TorznabCatType.PCMac, "MacOS");
            AddCategoryMapping(236, TorznabCatType.PC0day, "Windows");
            AddCategoryMapping(625, TorznabCatType.PCPhoneOther, "Smartphone");
            AddCategoryMapping(627, TorznabCatType.PCPhoneOther, "Tablette");
            AddCategoryMapping(629, TorznabCatType.PC, "Autre");
            AddCategoryMapping(638, TorznabCatType.PC, "Formation");

            // 340, Emulation
            AddCategoryMapping(342, TorznabCatType.ConsoleOther, "Emulateurs");
            AddCategoryMapping(344, TorznabCatType.ConsoleOther, "Roms");

            // undefined
            AddCategoryMapping(389, TorznabCatType.ConsoleOther, "Jeux vidéo");

            // 392, GPS
            AddCategoryMapping(391, TorznabCatType.PC0day, "Applications");
            AddCategoryMapping(393, TorznabCatType.PC0day, "Cartes");
            AddCategoryMapping(394, TorznabCatType.PC0day, "Divers");

            // 395, Audio
            AddCategoryMapping(400, TorznabCatType.Audio, "Karaoke");
            AddCategoryMapping(403, TorznabCatType.Audio, "Samples");
            AddCategoryMapping(623, TorznabCatType.Audio, "Musique");
            AddCategoryMapping(642, TorznabCatType.Audio, "Podcast Radio");

            // 404, eBook
            AddCategoryMapping(405, TorznabCatType.Books, "Audio");
            AddCategoryMapping(406, TorznabCatType.Books, "Bds");
            AddCategoryMapping(407, TorznabCatType.Books, "Comics");
            AddCategoryMapping(408, TorznabCatType.Books, "Livres");
            AddCategoryMapping(409, TorznabCatType.Books, "Mangas");
            AddCategoryMapping(410, TorznabCatType.Books, "Presse");

            // 456, xXx
            AddCategoryMapping(461, TorznabCatType.XXX, "eBooks");
            AddCategoryMapping(462, TorznabCatType.XXX, "Jeux vidéo");
            AddCategoryMapping(632, TorznabCatType.XXX, "Video");
            AddCategoryMapping(641, TorznabCatType.XXX, "Animation");

            // 624, Jeu vidéo
            AddCategoryMapping(239, TorznabCatType.PCGames, "Linux");
            AddCategoryMapping(245, TorznabCatType.PCMac, "MacOS");
            AddCategoryMapping(246, TorznabCatType.PCGames, "Windows");
            AddCategoryMapping(307, TorznabCatType.ConsoleNDS, "Nintendo");
            AddCategoryMapping(308, TorznabCatType.ConsolePS4, "Sony");
            AddCategoryMapping(309, TorznabCatType.ConsoleXbox, "Microsoft");
            AddCategoryMapping(626, TorznabCatType.PCPhoneOther, "Smartphone");
            AddCategoryMapping(628, TorznabCatType.PCPhoneOther, "Tablette");
            AddCategoryMapping(630, TorznabCatType.ConsoleOther, "Autre");
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

            var response = await PostDataWithCookies(AuthUrl, pairs);
            var responseContent = response.Content;
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

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            // API doesn't support getting the latest torrents, searching for the empty string will cause an error and all torrents returned
            var searchUrl = SearchUrl + HttpUtility.UrlEncode(query.SanitizedSearchTerm).Replace("+", "%20");
            searchUrl += "?offset=0&limit=200";

            // handle special term search for tvsearch
            var queryStringOverride = query.SanitizedSearchTerm;
            if (query.QueryType == "tvsearch")
            {
                searchUrl += "&cat=210&subcat=433";

                if (query.Season >= 1 && query.Season <= 30)
                {
                    var seasonTermValue = 967 + query.Season;
                    searchUrl += "&term[45][]=" + seasonTermValue;
                    queryStringOverride += " " + query.Season;
                }

                if (query.Episode != null)
                {
                    int episodeInt;
                    ParseUtil.TryCoerceInt(query.Episode, out episodeInt);
                    if (episodeInt >= 1 && episodeInt <= 30)
                    {
                        var episodeTermValue = 937 + episodeInt;
                        searchUrl += "&term[46][]=" + episodeTermValue;
                    }
                    else if (episodeInt >= 31 && episodeInt <= 60)
                    {
                        var episodeTermValue = 1087 + episodeInt - 30;
                        searchUrl += "&term[46][]=" + episodeTermValue;
                    }
                    queryStringOverride += " " + query.Episode;
                }
            }

            var headers = new Dictionary<string, string>();
            headers.Add("Authorization", await GetAuthToken());

            var response = await RequestStringWithCookies(searchUrl, null, null, headers);
            var results = response.Content;

            var jsonStart = results.IndexOf('{');
            var jsonLength = results.Length - jsonStart;
            var jsonResult = JObject.Parse(results.Substring(jsonStart));
            try
            {
                var items = (JArray)jsonResult["torrents"];
                foreach (var item in items)
                {
                    if (item.GetType() == typeof(JValue))
                    {
                        logger.Debug(string.Format("{0}: skipping torrent ID {1} (pending release without details)", ID, item.ToString()));
                        continue;
                    }
                    var release = new ReleaseInfo();

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;
                    release.DownloadVolumeFactor = 0;
                    release.DownloadVolumeFactor = 1;
                    var torrentId = (string)item["id"];
                    release.Link = new Uri(DownloadUrl + torrentId);
                    release.Title = (string)item["name"];

                    if ((query.ImdbID == null || !TorznabCaps.SupportsImdbSearch) && !query.MatchQueryStringAND(release.Title, null, queryStringOverride))
                        continue;

                    if ((string)item["isVerified"] == "1")
                        release.Description = "Verified";
                    release.Comments = new Uri(CommentsUrl + (string)item["rewritename"]);
                    release.Guid = release.Comments;

                    var dateUtc = DateTime.ParseExact((string)item["added"], "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    release.PublishDate = DateTime.SpecifyKind(dateUtc, DateTimeKind.Utc).ToLocalTime();

                    release.Seeders = ParseUtil.CoerceInt((string)item["seeders"]);
                    release.Peers = ParseUtil.CoerceInt((string)item["leechers"]) + release.Seeders;
                    release.Size = ParseUtil.CoerceLong((string)item["size"]);
                    release.Category = MapTrackerCatToNewznab((string)item["category"]);
                    release.Grabs = ParseUtil.CoerceLong((string)item["times_completed"]);

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
            var headers = new Dictionary<string, string>();
            headers.Add("Authorization", await GetAuthToken());

            var response = await RequestBytesWithCookies(link.AbsoluteUri, null, RequestType.GET, null, null, headers);
            return response.Content;
        }
    }
}