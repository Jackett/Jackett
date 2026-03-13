using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Jackett.Common.Exceptions;
using Jackett.Common.Extensions;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Serializer;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers.Definitions.Abstract
{
    [ExcludeFromCodeCoverage]
    public abstract class AvistazTracker : IndexerBase
    {
        public override string Language => "en-US";
        public override string Type => "private";

        public override bool SupportsPagination => false;

        private readonly Dictionary<string, string> AuthHeaders = new Dictionary<string, string>
        {
            {"Accept", "application/json"},
            {"Content-Type", "application/json"}
        };
        private string AuthUrl => SiteLink + "api/v1/jackett/auth";
        private string SearchUrl => SiteLink + "api/v1/jackett/torrents";
        private readonly HashSet<string> _hdResolutions = new HashSet<string> { "1080p", "1080i", "720p" };
        private string _token;

        private new ConfigurationDataAvistaZTracker configData => (ConfigurationDataAvistaZTracker)base.configData;

        // hook to adjust the search term
        protected virtual string GetSearchTerm(TorznabQuery query) => $"{query.SearchTerm} {GetEpisodeSearchTerm(query)}".Trim();

        protected virtual string GetEpisodeSearchTerm(TorznabQuery query) => query.GetEpisodeSearchString().Trim();

        // hook to adjust the search category
        protected virtual List<KeyValuePair<string, string>> GetSearchQueryParameters(TorznabQuery query)
        {
            var categoryMapping = MapTorznabCapsToTrackers(query).Distinct().ToList();

            var qc = new List<KeyValuePair<string, string>> // NameValueCollection don't support cat[]=19&cat[]=6
            {
                { "in", "1" },
                { "type", categoryMapping.FirstIfSingleOrDefault("0") },
                { "limit", "50" }
            };

            if (query.Limit > 0 && query.Offset > 0)
            {
                var page = query.Offset / query.Limit + 1;
                qc.Add("page", page.ToString());
            }

            // resolution filter to improve the search
            if (!query.Categories.Contains(TorznabCatType.Movies.ID) && !query.Categories.Contains(TorznabCatType.TV.ID) &&
                !query.Categories.Contains(TorznabCatType.Audio.ID))
            {
                if (query.Categories.Contains(TorznabCatType.MoviesUHD.ID) || query.Categories.Contains(TorznabCatType.TVUHD.ID))
                {
                    qc.Add("video_quality[]", "6"); // 2160p
                }

                if (query.Categories.Contains(TorznabCatType.MoviesHD.ID) || query.Categories.Contains(TorznabCatType.TVHD.ID))
                {
                    qc.Add("video_quality[]", "2"); // 720p
                    qc.Add("video_quality[]", "7"); // 1080i
                    qc.Add("video_quality[]", "3"); // 1080p
                }
                if (query.Categories.Contains(TorznabCatType.MoviesSD.ID) || query.Categories.Contains(TorznabCatType.TVSD.ID))
                {
                    qc.Add("video_quality[]", "1"); // SD
                }
            }

            // note, search by tmdb and tvdb are supported too
            // https://privatehd.to/api/v1/jackett/torrents?tmdb=1234
            // https://privatehd.to/api/v1/jackett/torrents?tvdb=3653
            if (query.IsImdbQuery)
            {
                qc.Add("imdb", query.ImdbID);
                qc.Add("search", GetEpisodeSearchTerm(query));
            }
            else if (query.IsTmdbQuery)
            {
                qc.Add("tmdb", query.TmdbID.ToString());
                qc.Add("search", GetEpisodeSearchTerm(query));
            }
            else if (query.IsTvdbQuery)
            {
                qc.Add("tvdb", query.TvdbID.ToString());
                qc.Add("search", GetEpisodeSearchTerm(query));
            }
            else
            {
                qc.Add("search", GetSearchTerm(query).Trim());
            }

            if (!string.IsNullOrWhiteSpace(query.Genre))
            {
                qc.Add("tags", query.Genre);
            }

            if (configData.Freeleech.Value)
            {
                qc.Add("discount[]", "1");
            }

            return qc;
        }

        // hook to adjust category parsing
        protected virtual IReadOnlyList<int> ParseCategories(TorznabQuery query, AvistazRelease row)
        {
            var categories = new List<int>();
            var videoQuality = row.VideoQuality;

            switch (row.Type.ToUpperInvariant())
            {
                case "MOVIE":
                    categories.Add(videoQuality switch
                    {
                        var res when _hdResolutions.Contains(res) => TorznabCatType.MoviesHD.ID,
                        "2160p" => TorznabCatType.MoviesUHD.ID,
                        _ => TorznabCatType.MoviesSD.ID
                    });
                    break;
                case "TV-SHOW":
                    categories.Add(videoQuality switch
                    {
                        var res when _hdResolutions.Contains(res) => TorznabCatType.TVHD.ID,
                        "2160p" => TorznabCatType.TVUHD.ID,
                        _ => TorznabCatType.TVSD.ID
                    });
                    break;
                case "MUSIC":
                    categories.Add(TorznabCatType.Audio.ID);
                    break;
                default:
                    throw new Exception("Error parsing category!");
            }

            return categories;
        }

        protected AvistazTracker(IIndexerConfigurationService configService, WebClient client, Logger logger, IProtectionService p, ICacheService cs)
            : base(configService: configService,
                   client: client,
                   logger: logger,
                   p: p,
                   cacheService: cs,
                   configData: new ConfigurationDataAvistaZTracker())
        {
            webclient.requestDelay = 6;
        }

        protected AvistazTracker(IIndexerConfigurationService configService, WebClient client, Logger logger, IProtectionService p, ICacheService cs, ConfigurationData configData)
            : base(configService: configService,
                   client: client,
                   logger: logger,
                   p: p,
                   cacheService: cs,
                   configData: configData)
        {
            webclient.requestDelay = 6;
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
                { "username", configData.Username.Value.Trim() },
                { "password", configData.Password.Value.Trim() },
                { "pid", configData.Pid.Value.Trim() }
            };
            var result = await RequestWithCookiesAsync(AuthUrl, method: RequestType.POST, data: body, headers: AuthHeaders);

            if ((int)result.Status == 429)
            {
                throw new TooManyRequestsException("Rate limited", result);
            }

            if (!STJson.TryDeserialize<AvistazAuthResponse>(result.ContentString, out var authResponse))
            {
                throw new Exception("Invalid response from AvistaZ, the response is not valid JSON");
            }

            _token = authResponse.Token;

            if (_token == null)
            {
                throw new Exception(authResponse.Message ?? "Unauthorized request to indexer");
            }
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var qc = GetSearchQueryParameters(query);
            var episodeSearchUrl = SearchUrl + "?" + qc.GetQueryString();

            var response = await RequestWithCookiesAndRetryAsync(episodeSearchUrl, headers: GetSearchHeaders());

            if (response.Status == HttpStatusCode.Unauthorized || response.Status == HttpStatusCode.PreconditionFailed)
            {
                await RenewalTokenAsync();
                response = await RequestWithCookiesAndRetryAsync(episodeSearchUrl, headers: GetSearchHeaders());
            }

            if (response.Status == HttpStatusCode.NotFound)
            {
                return releases; // search without results, eg CinemaZ: tt0075998
            }

            if ((int)response.Status == 429)
            {
                throw new TooManyRequestsException("Rate limited", response);
            }

            if ((int)response.Status >= 400)
            {
                throw new Exception($"Invalid status code {(int)response.Status} ({response.Status}) received from indexer");
            }

            if (response.Status != HttpStatusCode.OK)
            {
                throw new Exception($"Unknown status code: {(int)response.Status} ({response.Status})");
            }

            try
            {
                var jsonResponse = STJson.Deserialize<AvistazResponse>(response.ContentString);

                foreach (var row in jsonResponse.Data)
                {
                    var details = new Uri(row.Url);
                    var link = new Uri(row.Download);

                    var description = string.Empty;

                    if (row.Audio is { Count: > 0 })
                    {
                        var audioList = row.Audio.Select(tag => tag.Language).ToList();
                        description += $"Audio: {string.Join(", ", audioList)}";
                    }
                    if (row.Subtitle is { Count: > 0 })
                    {
                        var subtitleList = row.Subtitle.Select(tag => tag.Language).ToList();
                        description += $"<br/>Subtitles: {string.Join(", ", subtitleList)}";
                    }

                    var release = new ReleaseInfo
                    {
                        Title = row.FileName,
                        Link = link,
                        InfoHash = row.InfoHash,
                        Details = details,
                        Guid = details,
                        Category = ParseCategories(query, row).ToList(),
                        PublishDate = DateTime.Parse(row.CreatedAtIso, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
                        Description = description,
                        Size = row.FileSize,
                        Files = row.FileCount,
                        Grabs = row.Completed,
                        Seeders = row.Seed,
                        Peers = row.Leech + row.Seed,
                        DownloadVolumeFactor = row.DownloadMultiply,
                        UploadVolumeFactor = row.UploadMultiply,
                        MinimumRatio = 1,
                        MinimumSeedTime = 259200, // 72 hours
                        Languages = row.Audio?.Select(x => x.Language).ToList() ?? new List<string>(),
                        Subs = row.Subtitle?.Select(x => x.Language).ToList() ?? new List<string>(),
                    };

                    if (row.FileSize is > 0)
                    {
                        var sizeGigabytes = row.FileSize.Value / Math.Pow(1024, 3);

                        release.MinimumSeedTime = sizeGigabytes > 50.0
                            ? (long)((100 * Math.Log(sizeGigabytes)) - 219.2023) * 3600
                            : 259200 + (long)(sizeGigabytes * 7200);
                    }

                    if (row.MovieTvinfo != null)
                    {
                        release.Imdb = ParseUtil.GetImdbId(row.MovieTvinfo.Imdb);
                        release.TMDb = row.MovieTvinfo.Tmdb.IsNotNullOrWhiteSpace() && long.TryParse(row.MovieTvinfo.Tmdb, out var tmdbId) ? tmdbId : 0;
                        release.TVDBId = row.MovieTvinfo.Tvdb.IsNotNullOrWhiteSpace() && long.TryParse(row.MovieTvinfo.Tvdb, out var tvdbId) ? tvdbId : 0;
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

        private Dictionary<string, string> GetSearchHeaders() => new Dictionary<string, string>
        {
            {"Accept", "application/json"},
            {"Authorization", $"Bearer {_token}"}
        };
    }

    public class AvistazRelease
    {
        public string Url { get; set; }
        public string Download { get; set; }
        public Dictionary<string, string> Category { get; set; }

        [JsonPropertyName("movie_tv")]
        public AvistazIdInfo MovieTvinfo { get; set; }

        [JsonPropertyName("created_at_iso")]
        public string CreatedAtIso { get; set; }

        [JsonPropertyName("file_name")]
        public string FileName { get; set; }

        [JsonPropertyName("info_hash")]
        public string InfoHash { get; set; }

        public int? Leech { get; set; }
        public int? Completed { get; set; }
        public int? Seed { get; set; }

        [JsonPropertyName("file_size")]
        public long? FileSize { get; set; }

        [JsonPropertyName("file_count")]
        public int? FileCount { get; set; }

        [JsonPropertyName("download_multiply")]
        public double? DownloadMultiply { get; set; }

        [JsonPropertyName("upload_multiply")]
        public double? UploadMultiply { get; set; }

        [JsonPropertyName("video_quality")]
        public string VideoQuality { get; set; }

        public string Type { get; set; }

        public string Format { get; set; }

        public IReadOnlyCollection<AvistazLanguage> Audio { get; set; }
        public IReadOnlyCollection<AvistazLanguage> Subtitle { get; set; }
    }

    public class AvistazLanguage
    {
        public int Id { get; set; }
        public string Language { get; set; }
    }

    public class AvistazResponse
    {
        public IReadOnlyCollection<AvistazRelease> Data { get; set; }
    }

    public class AvistazIdInfo
    {
        public string Tmdb { get; set; }
        public string Tvdb { get; set; }
        public string Imdb { get; set; }
    }

    public class AvistazAuthResponse
    {
        public string Token { get; set; }
        public string Message { get; set; }
    }
}
