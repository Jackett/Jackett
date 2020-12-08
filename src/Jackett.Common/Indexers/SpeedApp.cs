using System;
using System.Collections.Generic;
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
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class SpeedApp : BaseWebIndexer
    {
        private readonly Dictionary<string, string> _apiHeaders = new Dictionary<string, string>
        {
            {"Accept", "application/json"},
            {"Content-Type", "application/json"}
        };
        // API DOC: https://speedapp.io/api/doc
        private string LoginUrl => SiteLink + "api/login";
        private string SearchUrl => SiteLink + "api/torrent";
        private string _token;

        private new ConfigurationDataBasicLoginWithEmail configData => (ConfigurationDataBasicLoginWithEmail)base.configData;

        public override string[] LegacySiteLinks { get; protected set; } = {
            "https://www.icetorrent.org/",
            "https://icetorrent.org/",
            "https://scenefz.me/",
            "https://www.scenefz.me/",
            "https://www.u-torrents.ro/",
            "https://myxz.eu/",
            "https://www.myxz.eu/",
            "https://www.myxz.org/"
        };

        public SpeedApp(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(
                id: "speedapp",
                name: "SpeedApp",
                description: "SpeedApp is a ROMANIAN Private Torrent Tracker for MOVIES / TV / GENERAL",
                link: "https://speedapp.io/",
                caps: new TorznabCapabilities
                {
                    TvSearchParams = new List<TvSearchParam>
                    {
                        TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId
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
                configData: new ConfigurationDataBasicLoginWithEmail())
        {
            Encoding = Encoding.UTF8;
            Language = "ro-ro";
            Type = "private";

            // requestDelay for API Limit (1 request per 2 seconds)
            webclient.requestDelay = 2.1;
 
            AddCategoryMapping(38, TorznabCatType.Movies, "Movie Packs");
            AddCategoryMapping(10, TorznabCatType.MoviesSD, "Movies: SD");
            AddCategoryMapping(35, TorznabCatType.MoviesSD, "Movies: SD Ro");
            AddCategoryMapping(8, TorznabCatType.MoviesHD, "Movies: HD");
            AddCategoryMapping(29, TorznabCatType.MoviesHD, "Movies: HD Ro");
            AddCategoryMapping(7, TorznabCatType.MoviesDVD, "Movies: DVD");
            AddCategoryMapping(2, TorznabCatType.MoviesDVD, "Movies: DVD Ro");
            AddCategoryMapping(17, TorznabCatType.MoviesBluRay, "Movies: BluRay");
            AddCategoryMapping(24, TorznabCatType.MoviesBluRay, "Movies: BluRay Ro");
            AddCategoryMapping(59, TorznabCatType.Movies, "Movies: Ro");
            AddCategoryMapping(57, TorznabCatType.MoviesUHD, "Movies: 4K (2160p) Ro");
            AddCategoryMapping(61, TorznabCatType.MoviesUHD, "Movies: 4K (2160p)");
            AddCategoryMapping(41, TorznabCatType.TV, "TV Packs");
            AddCategoryMapping(66, TorznabCatType.TV, "TV Packs Ro");
            AddCategoryMapping(45, TorznabCatType.TVSD, "TV Episodes");
            AddCategoryMapping(46, TorznabCatType.TVSD, "TV Episodes Ro");
            AddCategoryMapping(43, TorznabCatType.TVHD, "TV Episodes HD");
            AddCategoryMapping(44, TorznabCatType.TVHD, "TV Episodes HD Ro");
            AddCategoryMapping(60, TorznabCatType.TV, "TV Ro");
            AddCategoryMapping(11, TorznabCatType.PCGames, "Games: PC-ISO");
            AddCategoryMapping(52, TorznabCatType.Console, "Games: Console");
            AddCategoryMapping(1, TorznabCatType.PC0day, "Applications");
            AddCategoryMapping(14, TorznabCatType.PC, "Applications: Linux");
            AddCategoryMapping(37, TorznabCatType.PCMac, "Applications: Mac");
            AddCategoryMapping(19, TorznabCatType.PCMobileOther, "Applications: Mobile");
            AddCategoryMapping(62, TorznabCatType.TV, "TV Cartoons");
            AddCategoryMapping(3, TorznabCatType.TVAnime, "TV Anime / Hentai");
            AddCategoryMapping(6, TorznabCatType.BooksEBook, "E-books");
            AddCategoryMapping(5, TorznabCatType.Audio, "Music");
            AddCategoryMapping(64, TorznabCatType.AudioVideo, "Music Video");
            AddCategoryMapping(18, TorznabCatType.Other, "Images");
            AddCategoryMapping(22, TorznabCatType.TVSport, "TV Sports");
            AddCategoryMapping(58, TorznabCatType.TVSport, "TV Sports Ro");
            AddCategoryMapping(9, TorznabCatType.TVDocumentary, "TV Documentary");
            AddCategoryMapping(63, TorznabCatType.TVDocumentary, "TV Documentary Ro");
            AddCategoryMapping(65, TorznabCatType.Other, "Tutorial");
            AddCategoryMapping(67, TorznabCatType.OtherMisc, "Miscellaneous");
            AddCategoryMapping(15, TorznabCatType.XXX, "XXX Movies");
            AddCategoryMapping(47, TorznabCatType.XXX, "XXX DVD");
            AddCategoryMapping(48, TorznabCatType.XXX, "XXX HD");
            AddCategoryMapping(49, TorznabCatType.XXXImageSet, "XXX Images");
            AddCategoryMapping(50, TorznabCatType.XXX, "XXX Packs");
            AddCategoryMapping(51, TorznabCatType.XXX, "XXX SD");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            await RenewalTokenAsync();

            var releases = await PerformQuery(new TorznabQuery());
            await ConfigureIfOK(string.Empty, releases.Any(),
                                () => throw new Exception("Could not find releases."));

            return IndexerConfigurationStatus.Completed;
        }

        private async Task RenewalTokenAsync()
        {
            var body = new Dictionary<string, string>
            {
                { "username", configData.Email.Value.Trim() },
                { "password", configData.Password.Value.Trim() }
            };
            var jsonData = JsonConvert.SerializeObject(body);
            var result = await RequestWithCookiesAsync(
                LoginUrl, method: RequestType.POST, headers: _apiHeaders, rawbody: jsonData);
            var json = JObject.Parse(result.ContentString);
            _token = json.Value<string>("token");
            if (_token == null)
                throw new Exception(json.Value<string>("message"));
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            //var categoryMapping = MapTorznabCapsToTrackers(query).Distinct().ToList();
            var qc = new List<KeyValuePair<string, string>> // NameValueCollection don't support cat[]=19&cat[]=6
            {
                {"itemsPerPage", "100"},
                {"sort", "torrent.createdAt"},
                {"direction", "desc"}
            };

            foreach (var cat in MapTorznabCapsToTrackers(query))
                qc.Add("categories[]", cat);

            if (query.IsImdbQuery)
                qc.Add("imdbId", query.ImdbID);
            else
                qc.Add("search", query.GetQueryString());

            if (string.IsNullOrWhiteSpace(_token)) // fist time login
                await RenewalTokenAsync();

            var searchUrl = SearchUrl + "?" + qc.GetQueryString();
            var response = await RequestWithCookiesAsync(searchUrl, headers: GetSearchHeaders());
            if (response.Status == HttpStatusCode.Unauthorized)
            {
                await RenewalTokenAsync(); // re-login
                response = await RequestWithCookiesAsync(searchUrl, headers: GetSearchHeaders());
            }
            else if (response.Status != HttpStatusCode.OK)
                throw new Exception($"Unknown error in search: {response.ContentString}");

            try
            {
                var rows = JArray.Parse(response.ContentString);
                foreach (var row in rows)
                {
                    var id = row.Value<string>("id");
                    var details = new Uri($"{SiteLink}browse/{id}");
                    var link = new Uri($"{SiteLink}api/torrent/{id}/download");
                    var publishDate = DateTime.Parse(row.Value<string>("created_at"), CultureInfo.InvariantCulture);
                    var cat = row.Value<JToken>("category").Value<string>("id");

                    // "description" field in API has too much HTML code
                    var description = row.Value<string>("short_description");

                    var posterStr = row.Value<string>("poster");
                    var poster = Uri.TryCreate(posterStr, UriKind.Absolute, out var posterUri) ? posterUri : null;

                    var dlVolumeFactor = row.Value<bool>("is_half_download") ? 0.5: 1.0;
                    dlVolumeFactor = row.Value<bool>("is_freeleech") ? 0.0 : dlVolumeFactor;
                    var ulVolumeFactor = row.Value<bool>("is_double_upload") ? 2.0: 1.0;

                    var release = new ReleaseInfo
                    {
                        Title = row.Value<string>("name"),
                        Link = link,
                        Details = details,
                        Guid = details,
                        Category =  MapTrackerCatToNewznab(cat),
                        PublishDate = publishDate,
                        Description = description,
                        Poster = poster,
                        Size = row.Value<long>("size"),
                        Grabs = row.Value<long>("times_completed"),
                        Seeders = row.Value<int>("seeders"),
                        Peers = row.Value<int>("leechers") + row.Value<int>("seeders"),
                        DownloadVolumeFactor = dlVolumeFactor,
                        UploadVolumeFactor = ulVolumeFactor,
                        MinimumRatio = 1,
                        MinimumSeedTime = 172800 // 48 hours
                    };

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.ContentString, ex);
            }
            return releases;
        }

        public override async Task<byte[]> Download(Uri link)
        {
            var response = await RequestWithCookiesAsync(link.ToString(), headers: GetSearchHeaders());
            if (response.Status == HttpStatusCode.Unauthorized)
            {
                await RenewalTokenAsync();
                response = await RequestWithCookiesAsync(link.ToString(), headers: GetSearchHeaders());
            }
            else if (response.Status != HttpStatusCode.OK)
                throw new Exception($"Unknown error in download: {response.ContentBytes}");
            return response.ContentBytes;
        }

        private Dictionary<string, string> GetSearchHeaders() => new Dictionary<string, string>
        {
            {"Authorization", $"Bearer {_token}"}
        };
    }
}
