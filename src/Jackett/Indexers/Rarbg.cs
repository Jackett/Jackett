using Jackett.Models;
using Jackett.Models.IndexerConfig;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Web;

namespace Jackett.Indexers
{
    public class Rarbg : BaseWebIndexer
    {
        readonly static string defaultSiteLink = "https://torrentapi.org/";

        private Uri BaseUri
        {
            get { return new Uri(configData.Url.Value); }
            set { configData.Url.Value = value.ToString(); }
        }

        private string ApiEndpoint { get { return BaseUri + "pubapi_v2.php"; } }

        new ConfigurationDataUrl configData
        {
            get { return (ConfigurationDataUrl)base.configData; }
            set { base.configData = value; }
        }

        private DateTime lastTokenFetch;
        private string token;

        readonly TimeSpan TOKEN_DURATION = TimeSpan.FromMinutes(10);

        private bool HasValidToken { get { return !string.IsNullOrEmpty(token) && lastTokenFetch > DateTime.Now - TOKEN_DURATION; } }

        public Rarbg(IIndexerConfigurationService configService, IWebClient wc, Logger l, IProtectionService ps)
            : base(name: "RARBG",
                description: null,
                link: "https://rarbg.to/",
                caps: new TorznabCapabilities(),
                configService: configService,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataUrl(defaultSiteLink))
        {
            Encoding = Encoding.GetEncoding("windows-1252");
            Language = "en-us";
            Type = "public";

            TorznabCaps.SupportsImdbSearch = true;

            webclient.requestDelay = 2.5; // 0.5 requests per second (2 causes problems)

            AddCategoryMapping(4, TorznabCatType.XXX, "XXX (18+)");
            AddCategoryMapping(14, TorznabCatType.MoviesSD, "Movies/XVID");
            AddCategoryMapping(48, TorznabCatType.MoviesHD, "Movies/XVID/720");
            AddCategoryMapping(17, TorznabCatType.MoviesSD, "Movies/x264");
            AddCategoryMapping(44, TorznabCatType.MoviesHD, "Movies/x264/1080");
            AddCategoryMapping(45, TorznabCatType.MoviesHD, "Movies/x264/720");
            AddCategoryMapping(47, TorznabCatType.Movies3D, "Movies/x264/3D");
            AddCategoryMapping(42, TorznabCatType.MoviesBluRay, "Movies/Full BD");
            AddCategoryMapping(46, TorznabCatType.MoviesBluRay, "Movies/BD Remux");
            AddCategoryMapping(18, TorznabCatType.TVSD, "TV Episodes");
            AddCategoryMapping(41, TorznabCatType.TVHD, "TV HD Episodes");
            AddCategoryMapping(23, TorznabCatType.AudioMP3, "Music/MP3");
            AddCategoryMapping(25, TorznabCatType.AudioLossless, "Music/FLAC");
            AddCategoryMapping(27, TorznabCatType.PCGames, "Games/PC ISO");
            AddCategoryMapping(28, TorznabCatType.PCGames, "Games/PC RIP");
            AddCategoryMapping(40, TorznabCatType.ConsolePS3, "Games/PS3");
            AddCategoryMapping(32, TorznabCatType.ConsoleXbox360, "Games/XBOX-360");
            AddCategoryMapping(33, TorznabCatType.PCISO, "Software/PC ISO");
            AddCategoryMapping(35, TorznabCatType.BooksEbook, "e-Books");
        }


        async Task CheckToken()
        {
            if (!HasValidToken)
            {
                var queryCollection = new NameValueCollection();
                queryCollection.Add("get_token", "get_token");

                var tokenUrl = ApiEndpoint + "?" + queryCollection.GetQueryString();

                var result = await RequestStringWithCookiesAndRetry(tokenUrl);
                var json = JObject.Parse(result.Content);
                token = json.Value<string>("token");
                lastTokenFetch = DateTime.Now;
            }
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);
            var releases = await PerformQuery(new TorznabQuery());

            await ConfigureIfOK(string.Empty, releases.Count() > 0, () =>
            {
                throw new Exception("Could not find releases from this URL");
            });

            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            return await PerformQuery(query, 0);
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query, int attempts)
        {
            await CheckToken();
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();
            
            var queryCollection = new NameValueCollection();
            queryCollection.Add("token", token);
            queryCollection.Add("format", "json_extended");
            queryCollection.Add("app_id", "jackett_v" + Engine.ConfigService.GetVersion());
            queryCollection.Add("limit", "100");
            queryCollection.Add("ranked", "0");

            if (query.ImdbID != null)
            {
                queryCollection.Add("mode", "search");
                queryCollection.Add("search_imdb", query.ImdbID);
            }
            else if (query.RageID != null)
            {
                queryCollection.Add("mode", "search");
                queryCollection.Add("search_tvrage", query.RageID.ToString());
            }
            /*else if (query.TvdbID != null)
            {
                queryCollection.Add("mode", "search");
                queryCollection.Add("search_tvdb", query.TvdbID);
            }*/
            else if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Replace("'", ""); // ignore ' (e.g. search for america's Next Top Model)
                queryCollection.Add("mode", "search");
                queryCollection.Add("search_string", searchString);
            }
            else
            {
                queryCollection.Add("mode", "list");
            }

            var cats = string.Join(";", MapTorznabCapsToTrackers(query));
            if (!string.IsNullOrEmpty(cats))
            {
                queryCollection.Add("category", cats);
            }

            var searchUrl = ApiEndpoint + "?" + queryCollection.GetQueryString();
            var response = await RequestStringWithCookiesAndRetry(searchUrl, string.Empty);

            try
            {
                var jsonContent = JObject.Parse(response.Content);

                int errorCode = jsonContent.Value<int>("error_code");
                if (errorCode == 20) // no results found
                {
                    return releases.ToArray();
                }

                // return empty results in case of invalid imdb ID, see issue #1486
                if (errorCode == 10) // Cant find imdb in database. Are you sure this imdb exists?
                    return releases;

                if (errorCode > 0) // too many requests per second
                {
                    // we use the IwebClient rate limiter now, this shouldn't happen 
                    throw new Exception(jsonContent.Value<string>("error"));

                    /*if (attempts < 3)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2));
                        return await PerformQuery(query, ++attempts);
                    }
                    else
                    {
                        throw new Exception(jsonContent.Value<string>("error"));
                    }*/
                }

                foreach (var item in jsonContent.Value<JArray>("torrent_results"))
                {
                    var release = new ReleaseInfo();
                    release.Title = item.Value<string>("title");
                    release.Category = MapTrackerCatDescToNewznab(item.Value<string>("category"));

                    release.MagnetUri = new Uri(item.Value<string>("download"));
                    release.InfoHash = release.MagnetUri.ToString().Split(':')[3].Split('&')[0];

                    release.Comments = new Uri(item.Value<string>("info_page"));
                    release.Guid = release.Comments;

                    var episode_info = item.Value<JToken>("episode_info");

                    if (episode_info.HasValues)
                    {
                        var imdb = episode_info.Value<string>("imdb");
                        release.Imdb = ParseUtil.GetImdbID(imdb);
                        release.TVDBId = episode_info.Value<long?>("tvdb");
                        release.RageID = episode_info.Value<long?>("tvrage");
                        release.TMDb = episode_info.Value<long?>("themoviedb");
                    }

                    // ex: 2015-08-16 21:25:08 +0000
                    var dateStr = item.Value<string>("pubdate").Replace(" +0000", "");
                    var dateTime = DateTime.ParseExact(dateStr, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    release.PublishDate = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc).ToLocalTime();

                    release.Seeders = item.Value<int>("seeders");
                    release.Peers = item.Value<int>("leechers") + release.Seeders;
                    release.Size = item.Value<long>("size");
                    release.DownloadVolumeFactor = 0;
                    release.UploadVolumeFactor = 1;

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.Content, ex);
            }

            return releases;
        }

    }
}