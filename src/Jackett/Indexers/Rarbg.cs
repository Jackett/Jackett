using Jackett.Models;
using Jackett.Models.IndexerConfig;
using Jackett.Services;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Jackett.Indexers
{
    public class Rarbg : BaseIndexer, IIndexer
    {
        readonly static string defaultSiteLink = "https://torrentapi.org/";

        private Uri BaseUri
        {
            get { return new Uri(configData.Url.Value); }
            set { configData.Url.Value = value.ToString(); }
        }

        private string ApiEndpoint { get { return BaseUri + "pubapi_v2.php"; } }
        private string TokenUrl { get { return ApiEndpoint + "?get_token=get_token"; } }
        private string SearchUrl { get { return ApiEndpoint + "?app_id=jackett_v{0}&mode={1}&format=json_extended&search_string={2}&token={3}"; } }


        new ConfigurationDataUrl configData
        {
            get { return (ConfigurationDataUrl)base.configData; }
            set { base.configData = value; }
        }

        private DateTime lastTokenFetch;
        private string token;

        readonly TimeSpan TOKEN_DURATION = TimeSpan.FromMinutes(10);

        private bool HasValidToken { get { return !string.IsNullOrEmpty(token) && lastTokenFetch > DateTime.Now - TOKEN_DURATION; } }

        Dictionary<string, int> categoryLabels;

        public Rarbg(IIndexerManagerService i, IWebClient wc, Logger l, IProtectionService ps)
            : base(name: "RARBG",
                description: "RARBG",
                link: defaultSiteLink,
                caps: new TorznabCapabilities(),
                manager: i,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataUrl(defaultSiteLink))
        {
            categoryLabels = new Dictionary<string, int>();

            AddCat(4, TorznabCatType.XXX, "XXX (18+)");
            AddCat(14, TorznabCatType.MoviesSD, "Movies/XVID");
            AddCat(48, TorznabCatType.MoviesHD, "Movies/XVID/720");
            AddCat(17, TorznabCatType.MoviesSD, "Movies/x264");
            AddCat(44, TorznabCatType.MoviesHD, "Movies/x264/1080");
            AddCat(45, TorznabCatType.MoviesHD, "Movies/x264/720");
            AddCat(47, TorznabCatType.Movies3D, "Movies/x264/3D");
            AddCat(42, TorznabCatType.MoviesBluRay, "Movies/Full BD");
            AddCat(46, TorznabCatType.MoviesBluRay, "Movies/BD Remux");
            AddCat(18, TorznabCatType.TVSD, "TV Episodes");
            AddCat(41, TorznabCatType.TVHD, "TV HD Episodes");
            AddCat(23, TorznabCatType.AudioMP3, "Music/MP3");
            AddCat(25, TorznabCatType.AudioLossless, "Music/FLAC");
            AddCat(27, TorznabCatType.PCGames, "Games/PC ISO");
            AddCat(28, TorznabCatType.PCGames, "Games/PC RIP");
            AddCat(40, TorznabCatType.ConsolePS3, "Games/PS3");
            AddCat(32, TorznabCatType.ConsoleXbox360, "Games/XBOX-360");
            AddCat(33, TorznabCatType.PCISO, "Software/PC ISO");
            AddCat(35, TorznabCatType.BooksEbook, "e-Books");
        }

        void AddCat(int cat, TorznabCategory catType, string label)
        {
            AddCategoryMapping(cat, catType);
            categoryLabels.Add(label, cat);
        }

        async Task CheckToken()
        {
            if (!HasValidToken)
            {
                var result = await RequestStringWithCookiesAndRetry(TokenUrl);
                var json = JObject.Parse(result.Content);
                token = json.Value<string>("token");
                lastTokenFetch = DateTime.Now;
            }
        }

        public async Task ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);
            var releases = await PerformQuery(new TorznabQuery());

            await ConfigureIfOK(string.Empty, releases.Count() > 0, () =>
            {
                throw new Exception("Could not find releases from this URL");
            });
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            await CheckToken();
            var releases = new List<ReleaseInfo>();
            var queryStr = HttpUtility.UrlEncode(query.GetQueryString());

            var mode = string.IsNullOrEmpty(queryStr) ? "list" : "search";
            var episodeSearchUrl = string.Format(SearchUrl, Engine.ConfigService.GetVersion(), mode, queryStr, token);
            var cats = string.Join(";", MapTorznabCapsToTrackers(query));
            if (!string.IsNullOrEmpty(cats))
            {
                episodeSearchUrl += "&category=" + cats;
            }
            var response = await RequestStringWithCookiesAndRetry(episodeSearchUrl, string.Empty);

            try
            {
                var jsonContent = JObject.Parse(response.Content);

                int errorCode = jsonContent.Value<int>("error_code");
                if (errorCode == 20) // no results found
                {
                    return releases.ToArray();
                }

                if (errorCode > 0) // too many requests per second
                {
                    throw new Exception(jsonContent.Value<string>("error"));
                }

                foreach (var item in jsonContent.Value<JArray>("torrent_results"))
                {
                    var release = new ReleaseInfo();
                    release.Title = item.Value<string>("title");
                    release.Description = release.Title;
                    release.Category = MapTrackerCatToNewznab(categoryLabels[item.Value<string>("category")].ToString());

                    release.MagnetUri = new Uri(item.Value<string>("download"));
                    release.InfoHash = release.MagnetUri.ToString().Split(':')[3].Split('&')[0];

                    release.Comments = new Uri(item.Value<string>("info_page"));
                    release.Guid = release.Comments;

                    // ex: 2015-08-16 21:25:08 +0000
                    var dateStr = item.Value<string>("pubdate").Replace(" +0000", "");
                    var dateTime = DateTime.ParseExact(dateStr, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    release.PublishDate = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc).ToLocalTime();

                    release.Seeders = item.Value<int>("seeders");
                    release.Peers = item.Value<int>("leechers") + release.Seeders;
                    release.Size = item.Value<long>("size");
                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.Content, ex);
            }

            return releases.ToArray();
        }

    }
}
