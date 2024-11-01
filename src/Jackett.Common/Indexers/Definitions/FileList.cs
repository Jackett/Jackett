using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Jackett.Common.Exceptions;
using Jackett.Common.Extensions;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Serializer;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class FileList : IndexerBase
    {
        public override string Id => "filelist";
        public override string Name => "FileList";
        public override string Description => "The best Romanian site.";
        public override string SiteLink { get; protected set; } = "https://filelist.io/";
        public override string[] AlternativeSiteLinks => new[]
        {
            "https://filelist.io/"
        };
        public override string[] LegacySiteLinks => new[]
        {
            "https://filelist.ro/",
            "http://filelist.ro/",
            "http://flro.org/",
            "https://flro.org/"
        };
        public override Encoding Encoding => Encoding.UTF8;
        public override string Language => "ro-RO";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private string ApiUrl => SiteLink + "api.php";
        private string DetailsUrl => SiteLink + "details.php";

        private new ConfigurationDataFileList configData => (ConfigurationDataFileList)base.configData;

        public FileList(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataFileList())
        {
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
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
                },
                TvSearchImdbAvailable = true
            };

            caps.Categories.AddCategoryMapping(1, TorznabCatType.MoviesSD, "Filme SD");
            caps.Categories.AddCategoryMapping(2, TorznabCatType.MoviesDVD, "Filme DVD");
            caps.Categories.AddCategoryMapping(3, TorznabCatType.MoviesForeign, "Filme DVD-RO");
            caps.Categories.AddCategoryMapping(4, TorznabCatType.MoviesHD, "Filme HD");
            caps.Categories.AddCategoryMapping(5, TorznabCatType.AudioLossless, "FLAC");
            caps.Categories.AddCategoryMapping(6, TorznabCatType.MoviesUHD, "Filme 4K");
            caps.Categories.AddCategoryMapping(7, TorznabCatType.XXX, "XXX");
            caps.Categories.AddCategoryMapping(8, TorznabCatType.PC, "Programe");
            caps.Categories.AddCategoryMapping(9, TorznabCatType.PCGames, "Jocuri PC");
            caps.Categories.AddCategoryMapping(10, TorznabCatType.Console, "Jocuri Console");
            caps.Categories.AddCategoryMapping(11, TorznabCatType.Audio, "Audio");
            caps.Categories.AddCategoryMapping(12, TorznabCatType.AudioVideo, "Videoclip");
            caps.Categories.AddCategoryMapping(13, TorznabCatType.TVSport, "Sport");
            caps.Categories.AddCategoryMapping(15, TorznabCatType.TV, "Desene");
            caps.Categories.AddCategoryMapping(16, TorznabCatType.Books, "Docs");
            caps.Categories.AddCategoryMapping(17, TorznabCatType.PC, "Linux");
            caps.Categories.AddCategoryMapping(18, TorznabCatType.Other, "Diverse");
            caps.Categories.AddCategoryMapping(19, TorznabCatType.MoviesForeign, "Filme HD-RO");
            caps.Categories.AddCategoryMapping(20, TorznabCatType.MoviesBluRay, "Filme Blu-Ray");
            caps.Categories.AddCategoryMapping(21, TorznabCatType.TVHD, "Seriale HD");
            caps.Categories.AddCategoryMapping(22, TorznabCatType.PCMobileOther, "Mobile");
            caps.Categories.AddCategoryMapping(23, TorznabCatType.TVSD, "Seriale SD");
            caps.Categories.AddCategoryMapping(24, TorznabCatType.TVAnime, "Anime");
            caps.Categories.AddCategoryMapping(25, TorznabCatType.Movies3D, "Filme 3D");
            caps.Categories.AddCategoryMapping(26, TorznabCatType.MoviesBluRay, "Filme 4K Blu-Ray");
            caps.Categories.AddCategoryMapping(27, TorznabCatType.TVUHD, "Seriale 4K");
            caps.Categories.AddCategoryMapping(28, TorznabCatType.MoviesForeign, "RO Dubbed");
            caps.Categories.AddCategoryMapping(28, TorznabCatType.TVForeign, "RO Dubbed");

            return caps;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var releases = await PerformQuery(new TorznabQuery());
            await ConfigureIfOK(string.Empty, releases.Any(),
                                () => throw new Exception("Could not find releases."));

            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var indexerResponse = await CallProviderAsync(query);

            if (indexerResponse == null)
            {
                return releases;
            }

            var response = indexerResponse.ContentString;

            if ((int)indexerResponse.Status == 429)
            {
                throw new TooManyRequestsException("Rate limited", indexerResponse);
            }

            if (response.StartsWith("{\"error\"") && STJson.TryDeserialize<FileListErrorResponse>(response, out var errorResponse))
            {
                throw new ExceptionWithConfigData(errorResponse.Error, configData);
            }

            if (indexerResponse.Status != HttpStatusCode.OK)
            {
                throw new Exception($"Unknown status code: {(int)indexerResponse.Status} ({indexerResponse.Status})");
            }

            try
            {
                var results = STJson.Deserialize<List<FileListTorrent>>(response);

                foreach (var row in results)
                {
                    var isFreeleech = row.FreeLeech;

                    // skip non-freeleech results when freeleech only is set
                    if (configData.Freeleech.Value && !isFreeleech)
                    {
                        continue;
                    }

                    var detailsUri = new Uri($"{DetailsUrl}?id={row.Id}");
                    var link = new Uri(row.DownloadLink);
                    var imdbId = row.ImdbId.IsNotNullOrWhiteSpace() ? ParseUtil.GetImdbId(row.ImdbId) : null;

                    var release = new ReleaseInfo
                    {
                        Guid = detailsUri,
                        Details = detailsUri,
                        Link = link,
                        Title = row.Name.Trim(),
                        Category = MapTrackerCatDescToNewznab(row.Category),
                        Size = row.Size,
                        Files = row.Files,
                        Grabs = row.TimesCompleted,
                        Seeders = row.Seeders,
                        Peers = row.Seeders + row.Leechers,
                        Imdb = imdbId,
                        PublishDate = DateTime.Parse(row.UploadDate + " +0200", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
                        DownloadVolumeFactor = isFreeleech ? 0 : 1,
                        UploadVolumeFactor = row.DoubleUp ? 2 : 1,
                        MinimumRatio = 1,
                        MinimumSeedTime = 172800 // 48 hours
                    };

                    releases.Add(release);
                }

                return releases;
            }
            catch (Exception ex)
            {
                OnParseError(response, ex);
            }

            return releases;
        }

        private async Task<WebResult> CallProviderAsync(TorznabQuery query)
        {
            var searchUrl = ApiUrl;
            var searchQuery = query.SanitizedSearchTerm.Trim();

            var queryCollection = new NameValueCollection
            {
                { "category", string.Join(",", MapTorznabCapsToTrackers(query).Distinct().ToList()) }
            };

            if (configData.Freeleech.Value)
            {
                queryCollection.Set("freeleech", "1");
            }

            if (query.IsImdbQuery || searchQuery.IsNotNullOrWhiteSpace())
            {
                queryCollection.Set("action", "search-torrents");

                if (DateTime.TryParseExact($"{query.Season} {query.Episode}", "yyyy MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var showDate))
                {
                    if (query.IsImdbQuery)
                    {
                        // Skip ID searches for daily episodes
                        return await Task.FromResult<WebResult>(null);
                    }

                    searchQuery = $"{searchQuery} {showDate:yyyy.MM.dd}".Trim();
                }
                else
                {
                    if (query.Season > 0)
                    {
                        queryCollection.Set("season", query.Season.ToString());
                    }

                    if (query.Episode.IsNotNullOrWhiteSpace())
                    {
                        queryCollection.Set("episode", query.Episode);
                    }
                }

                if (query.IsImdbQuery)
                {
                    queryCollection.Set("type", "imdb");
                    queryCollection.Set("query", query.ImdbID);
                }
                else if (searchQuery.IsNotNullOrWhiteSpace())
                {
                    queryCollection.Set("type", "name");
                    queryCollection.Set("query", searchQuery);
                }
            }
            else
            {
                queryCollection.Set("action", "latest-torrents");
            }

            searchUrl += "?" + queryCollection.GetQueryString();

            try
            {
                var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(configData.Username.Value + ":" + configData.Passkey.Value));
                var headers = new Dictionary<string, string>
                {
                    {"Authorization", "Basic " + auth}
                };

                return await RequestWithCookiesAsync(searchUrl, headers: headers);
            }
            catch (Exception inner)
            {
                throw new Exception("Error calling provider filelist", inner);
            }
        }
    }

    public class FileListTorrent
    {
        public uint Id { get; set; }

        public string Name { get; set; }

        [JsonPropertyName("download_link")]
        public string DownloadLink { get; set; }

        public long Size { get; set; }

        public int Leechers { get; set; }

        public int Seeders { get; set; }

        [JsonPropertyName("times_completed")]
        public uint TimesCompleted { get; set; }

        public uint Files { get; set; }

        [JsonPropertyName("imdb")]
        public string ImdbId { get; set; }

        public bool Internal { get; set; }

        [JsonPropertyName("freeleech")]
        public bool FreeLeech { get; set; }

        [JsonPropertyName("doubleup")]
        public bool DoubleUp { get; set; }

        [JsonPropertyName("upload_date")]
        public string UploadDate { get; set; }

        public string Category { get; set; }

        [JsonPropertyName("small_description")]
        public string SmallDescription { get; set; }
    }

    public class FileListErrorResponse
    {
        public string Error { get; set; }
    }
}
