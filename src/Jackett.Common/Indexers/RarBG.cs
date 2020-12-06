using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class RarBG : BaseWebIndexer
    {
        // API doc: https://torrentapi.org/apidocs_v2.txt?app_id=Jackett
        private const string ApiEndpoint = "https://torrentapi.org/pubapi_v2.php";
        private readonly TimeSpan TokenDuration = TimeSpan.FromMinutes(14); // 15 minutes expiration
        private readonly string _appId;
        private string _token;
        private DateTime _lastTokenFetch;
        private string _sort;

        private new ConfigurationData configData => base.configData;

        public RarBG(IIndexerConfigurationService configService, Utils.Clients.WebClient wc, Logger l, IProtectionService ps)
            : base(id: "rarbg",
                   name: "RARBG",
                   description: "RARBG is a Public torrent site for MOVIES / TV / GENERAL",
                   link: "https://rarbg.to/",
                   caps: new TorznabCapabilities
                   {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                       },
                       MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q, MovieSearchParam.ImdbId
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
                   configData: new ConfigurationData())
        {
            Encoding = Encoding.GetEncoding("windows-1252");
            Language = "en-us";
            Type = "public";

            webclient.requestDelay = 2.5; // The api has a 1req/2s limit

            var sort = new SelectItem(new Dictionary<string, string>
            {
                {"last", "created"},
                {"seeders", "seeders"},
                {"leechers", "leechers"}
            })
            { Name = "Sort requested from site", Value = "last" };
            configData.AddDynamic("sort", sort);

            AddCategoryMapping(4, TorznabCatType.XXX, "XXX (18+)");
            AddCategoryMapping(14, TorznabCatType.MoviesSD, "Movies/XVID");
            AddCategoryMapping(17, TorznabCatType.MoviesSD, "Movies/x264");
            AddCategoryMapping(18, TorznabCatType.TVSD, "TV Episodes");
            AddCategoryMapping(23, TorznabCatType.AudioMP3, "Music/MP3");
            AddCategoryMapping(25, TorznabCatType.AudioLossless, "Music/FLAC");
            AddCategoryMapping(27, TorznabCatType.PCGames, "Games/PC ISO");
            AddCategoryMapping(28, TorznabCatType.PCGames, "Games/PC RIP");
            AddCategoryMapping(32, TorznabCatType.ConsoleXBox360, "Games/XBOX-360");
            AddCategoryMapping(33, TorznabCatType.PCISO, "Software/PC ISO");
            AddCategoryMapping(35, TorznabCatType.BooksEBook, "e-Books");
            AddCategoryMapping(40, TorznabCatType.ConsolePS3, "Games/PS3");
            AddCategoryMapping(41, TorznabCatType.TVHD, "TV HD Episodes");
            AddCategoryMapping(42, TorznabCatType.MoviesBluRay, "Movies/Full BD");
            AddCategoryMapping(44, TorznabCatType.MoviesHD, "Movies/x264/1080");
            AddCategoryMapping(45, TorznabCatType.MoviesHD, "Movies/x264/720");
            AddCategoryMapping(46, TorznabCatType.MoviesBluRay, "Movies/BD Remux");
            AddCategoryMapping(47, TorznabCatType.Movies3D, "Movies/x264/3D");
            AddCategoryMapping(48, TorznabCatType.MoviesHD, "Movies/XVID/720");
            AddCategoryMapping(49, TorznabCatType.TVUHD, "TV UHD Episodes");
            // torrentapi.org returns "Movies/TV-UHD-episodes" for some reason
            // possibly because thats what the category is called on the /top100.php page
            AddCategoryMapping(49, TorznabCatType.TVUHD, "Movies/TV-UHD-episodes");
            AddCategoryMapping(50, TorznabCatType.MoviesUHD, "Movies/x264/4k");
            AddCategoryMapping(51, TorznabCatType.MoviesUHD, "Movies/x265/4k");
            AddCategoryMapping(52, TorznabCatType.MoviesUHD, "Movs/x265/4k/HDR");
            AddCategoryMapping(53, TorznabCatType.ConsolePS4, "Games/PS4");
            AddCategoryMapping(54, TorznabCatType.MoviesHD, "Movies/x265/1080");

            _appId = "jackett_" + EnvironmentUtil.JackettVersion();
        }

        public override void LoadValuesFromJson(JToken jsonConfig, bool useProtectionService = false)
        {
            base.LoadValuesFromJson(jsonConfig, useProtectionService);

            var sort = (SelectItem)configData.GetDynamic("sort");
            _sort = sort != null ? sort.Value : "last";
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var releases = await PerformQuery(new TorznabQuery());

            await ConfigureIfOK(string.Empty, releases.Any(), () =>
                throw new Exception("Could not find releases from this URL"));

            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
            => await PerformQueryWithRetry(query, true);

        private async Task<IEnumerable<ReleaseInfo>> PerformQueryWithRetry(TorznabQuery query, bool retry) {
            var releases = new List<ReleaseInfo>();

            // check the token and renewal if necessary
            await RenewalTokenAsync();

            var response = await RequestWithCookiesAndRetryAsync(BuildSearchUrl(query));
            var jsonContent = JObject.Parse(response.ContentString);
            var errorCode = jsonContent.Value<int>("error_code");
            switch (errorCode)
            {
                case 0: // valid response with results
                    break;
                case 2:
                case 4: // invalid token
                    await RenewalTokenAsync(true); // force renewal token
                    response = await RequestWithCookiesAndRetryAsync(BuildSearchUrl(query));
                    jsonContent = JObject.Parse(response.ContentString);
                    break;
                case 10: // imdb not found, see issue #1486
                case 20: // no results found
                    // the api returns "no results" in some valid queries. we do one retry on this case but we can't do more
                    // because we can't distinguish between search without results and api malfunction
                    return retry ? await PerformQueryWithRetry(query, false) : releases;
                default:
                    throw new Exception("Unknown error code: " + errorCode + " response: " + response.ContentString);
            }

            try
            {
                foreach (var item in jsonContent.Value<JArray>("torrent_results"))
                {
                    var title = WebUtility.HtmlDecode(item.Value<string>("title"));

                    var magnetStr = item.Value<string>("download");
                    var magnetUri = new Uri(magnetStr);
                    var infoHash = magnetStr.Split(':')[3].Split('&')[0];

                    // append app_id to prevent api server returning 403 forbidden
                    var details = new Uri(item.Value<string>("info_page") + "&app_id=" + _appId);

                    // ex: 2015-08-16 21:25:08 +0000
                    var dateStr = item.Value<string>("pubdate").Replace(" +0000", "");
                    var dateTime = DateTime.ParseExact(dateStr, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    var publishDate = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc).ToLocalTime();

                    var size = item.Value<long>("size");
                    var seeders = item.Value<int>("seeders");
                    var leechers = item.Value<int>("leechers");

                    var release = new ReleaseInfo
                    {
                        Title = title,
                        Category = MapTrackerCatDescToNewznab(item.Value<string>("category")),
                        MagnetUri = magnetUri,
                        InfoHash = infoHash,
                        Details = details,
                        PublishDate = publishDate,
                        Guid = magnetUri,
                        Seeders = seeders,
                        Peers = leechers + seeders,
                        Size = size,
                        DownloadVolumeFactor = 0,
                        UploadVolumeFactor = 1
                    };

                    var episodeInfo = item.Value<JToken>("episode_info");
                    if (episodeInfo.HasValues)
                    {
                        release.Imdb = ParseUtil.GetImdbID(episodeInfo.Value<string>("imdb"));
                        release.TVDBId = episodeInfo.Value<long?>("tvdb");
                        release.RageID = episodeInfo.Value<long?>("tvrage");
                        release.TMDb = episodeInfo.Value<long?>("themoviedb");
                    }

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.ContentString, ex);
            }

            return releases;
        }

        private string BuildSearchUrl(TorznabQuery query)
        {
            var searchString = query.GetQueryString();
            var qc = new NameValueCollection
            {
                { "token", _token },
                { "format", "json_extended" },
                { "app_id", _appId },
                { "limit", "100" },
                { "ranked", "0" },
                { "sort", _sort }
            };

            if (query.ImdbID != null)
            {
                qc.Add("mode", "search");
                qc.Add("search_imdb", query.ImdbID);
            }
            else if (query.RageID != null)
            {
                qc.Add("mode", "search");
                qc.Add("search_tvrage", query.RageID.ToString());
            }
            /*else if (query.TvdbID != null)
            {
                queryCollection.Add("mode", "search");
                queryCollection.Add("search_tvdb", query.TvdbID);
            }*/
            else if (!string.IsNullOrWhiteSpace(searchString))
            {
                // ignore ' (e.g. search for america's Next Top Model)
                searchString = searchString.Replace("'", "");
                qc.Add("mode", "search");
                qc.Add("search_string", searchString);
            }
            else
            {
                qc.Add("mode", "list");
                qc.Remove("sort");
            }

            var querycats = MapTorznabCapsToTrackers(query);
            if (querycats.Count == 0)
                // default to all, without specifying it some categories are missing (e.g. games), see #4146
                querycats = GetAllTrackerCategories();
            var cats = string.Join(";", querycats);
            qc.Add("category", cats);

            return ApiEndpoint + "?" + qc.GetQueryString();
        }

        private async Task RenewalTokenAsync(bool force = false)
        {
            if (!HasValidToken || force)
            {
                var qc = new NameValueCollection
                {
                    { "get_token", "get_token" },
                    { "app_id", _appId }
                };
                var tokenUrl = ApiEndpoint + "?" + qc.GetQueryString();
                var result = await RequestWithCookiesAndRetryAsync(tokenUrl);
                var json = JObject.Parse(result.ContentString);
                _token = json.Value<string>("token");
                _lastTokenFetch = DateTime.Now;
            }
        }

        private bool HasValidToken => !string.IsNullOrEmpty(_token) && _lastTokenFetch > DateTime.Now - TokenDuration;
    }
}
