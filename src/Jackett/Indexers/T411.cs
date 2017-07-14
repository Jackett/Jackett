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
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Jackett.Models.IndexerConfig;

namespace Jackett.Indexers
{
    public class T411 : BaseWebIndexer
    {
        const string ApiUrl = "https://api.t411.al";
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

        private Dictionary<int, List<int>> _mediaCategoryMapping = new Dictionary<int, List<int>>();

        public T411(IIndexerConfigurationService configService, IWebClient wc, Logger l, IProtectionService ps)
            : base(name: "T411",
                description: "French Torrent Tracker",
                link: "https://t411.al/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                configService: configService,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataLoginTokin())
        {
            Encoding = Encoding.UTF8;
            Type = "semi-private";
            Language = "fr-fr";

            // 210, FilmVidéo
            AddCategoryMapping(210, 402, TorznabCatType.Movies, "Vidéoclips");
            AddCategoryMapping(210, 433, TorznabCatType.TV, "Série TV");
            AddCategoryMapping(210, 455, TorznabCatType.TVAnime, "Animation");
            AddCategoryMapping(210, 631, TorznabCatType.Movies, "Film");
            AddCategoryMapping(210, 633, TorznabCatType.Movies, "Concert");
            AddCategoryMapping(210, 634, TorznabCatType.TVDocumentary, "Documentaire");
            AddCategoryMapping(210, 635, TorznabCatType.TV, "Spectacle");
            AddCategoryMapping(210, 636, TorznabCatType.TVSport, "Sport");
            AddCategoryMapping(210, 637, TorznabCatType.TVAnime, "Animation Série");
            AddCategoryMapping(210, 639, TorznabCatType.TV, "Emission TV");

            // 233, Application
            AddCategoryMapping(233, 234, TorznabCatType.PC, "Linux");
            AddCategoryMapping(233, 235, TorznabCatType.PCMac, "MacOS");
            AddCategoryMapping(233, 236, TorznabCatType.PC, "Windows");
            AddCategoryMapping(233, 625, TorznabCatType.PCPhoneOther, "Smartphone");
            AddCategoryMapping(233, 627, TorznabCatType.PCPhoneOther, "Tablette");
            AddCategoryMapping(233, 629, TorznabCatType.PC, "Autre");
            AddCategoryMapping(233, 638, TorznabCatType.PC, "Formation");

            // 395, Audio
            AddCategoryMapping(395, 400, TorznabCatType.Audio, "Karaoke");
            AddCategoryMapping(395, 403, TorznabCatType.Audio, "Samples");
            AddCategoryMapping(395, 623, TorznabCatType.Audio, "Musique");
            AddCategoryMapping(395, 642, TorznabCatType.Audio, "Podcast Radio");

            // 404, eBook
            AddCategoryMapping(404, 405, TorznabCatType.Books, "Audio");
            AddCategoryMapping(404, 406, TorznabCatType.Books, "Bds");
            AddCategoryMapping(404, 407, TorznabCatType.Books, "Comics");
            AddCategoryMapping(404, 408, TorznabCatType.Books, "Livres");
            AddCategoryMapping(404, 409, TorznabCatType.Books, "Mangas");
            AddCategoryMapping(404, 410, TorznabCatType.Books, "Presse");

            // 456, xXx
            AddCategoryMapping(456, 461, TorznabCatType.XXX, "eBooks");
            AddCategoryMapping(456, 462, TorznabCatType.XXX, "Jeux vidéo");
            AddCategoryMapping(456, 632, TorznabCatType.XXX, "Video");
            AddCategoryMapping(456, 641, TorznabCatType.XXX, "Animation");

            // 624, Jeu vidéo
            AddCategoryMapping(624, 239, TorznabCatType.PCGames, "Linux");
            AddCategoryMapping(624, 245, TorznabCatType.PCMac, "MacOS");
            AddCategoryMapping(624, 246, TorznabCatType.PCGames, "Windows");
            AddCategoryMapping(624, 307, TorznabCatType.ConsoleNDS, "Nintendo");
            AddCategoryMapping(624, 308, TorznabCatType.ConsolePS4, "Sony");
            AddCategoryMapping(624, 309, TorznabCatType.ConsoleXbox, "Microsoft");
            AddCategoryMapping(624, 626, TorznabCatType.PCPhoneOther, "Smartphone");
            AddCategoryMapping(624, 628, TorznabCatType.PCPhoneOther, "Tablette");
            AddCategoryMapping(624, 630, TorznabCatType.ConsoleOther, "Autre");
        }

        private void AddCategoryMapping(int trackerMediaCategory, int trackerCategory, TorznabCategory newznabCategory, string trackerCategoryDesc = null)
        {
            AddCategoryMapping(trackerCategory, newznabCategory, trackerCategoryDesc);
            if (!_mediaCategoryMapping.ContainsKey(trackerMediaCategory))
                _mediaCategoryMapping.Add(trackerMediaCategory, new List<int>());
            _mediaCategoryMapping[trackerMediaCategory].Add(trackerCategory);
        }

        private KeyValuePair<int, List<int>> GetCategoryFromSubCat(int subCategory)
        {
            try
            {
                return _mediaCategoryMapping.First(pair => pair.Value.Contains(subCategory));
            }
            catch (Exception)
            {
                return new KeyValuePair<int, List<int>>(0, new List<int>() { 0 }); //If the provided category does not exist, we return 0 (ALL)
            }
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

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
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

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            // API doesn't support getting the latest torrents, searching for the empty string will cause an error and all torrents returned
            var searchUrl = SearchUrl + HttpUtility.UrlEncode(query.SanitizedSearchTerm).Replace("+", "%20");
            searchUrl += "?offset=0&limit=200&cat=" + GetCategoryFromSubCat(query.Categories.FirstOrDefault()).Key;

            // handle special term search for tvsearch
            var queryStringOverride = query.SanitizedSearchTerm;
            switch (query.QueryType)
            {
                case "tvsearch":
                    // T411 make the difference beetween Animation Movies and TV Animation Series, while Torznab does not.
                    // So here we take LastOrDefault from the category ids, so if the query specify an animation tv serie, we select the correct id.
                    searchUrl += "&subcat=" + GetCategoryFromSubCat(query.Categories.FirstOrDefault()).Value.LastOrDefault();
                    if (query.Season >= 1 && query.Season <= 30)
                    {
                        var seasonTermValue = 967 + query.Season;
                        searchUrl += "&term[45][]=" + seasonTermValue;
                        queryStringOverride += " " + query.Season;
                    }

                    if (query.Episode != null)
                    {
                        int episodeInt;
                        int episodeCategoryOffset = 936;
                        ParseUtil.TryCoerceInt(query.Episode, out episodeInt);
                        if (episodeInt >= 1 && episodeInt <= 8)
                            episodeCategoryOffset = 936;
                        else if (episodeInt >= 9 && episodeInt <= 30)
                            episodeCategoryOffset = 937;
                        else if (episodeInt >= 31)
                            episodeCategoryOffset = 1057;
                        searchUrl += "&term[46][]=" + (episodeCategoryOffset + episodeInt);
                        queryStringOverride += " " + query.Episode;
                    }
                    break;
                case "movie":
                    // T411 make the difference beetween Animation Movies and TV Animation Series, while Torznab does not.
                    // So here we take FirstOrDefault from the category ids, so if the query specify an animation movie, we select the correct id.
                    searchUrl += "&subcat=" + GetCategoryFromSubCat(query.Categories.FirstOrDefault()).Value.FirstOrDefault();
                    break;
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
