using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Jackett.Common.Extensions;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
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
            "https://filelist.io/",
            "https://flro.org/"
        };
        public override string[] LegacySiteLinks => new[]
        {
            "https://filelist.ro/",
            "http://filelist.ro/",
            "http://flro.org/"
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

            return caps;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var pingResponse = await CallProviderAsync(new TorznabQuery());

            try
            {
                var json = JArray.Parse(pingResponse);
                if (json.Count > 0)
                {
                    IsConfigured = true;
                    SaveConfig();
                    return IndexerConfigurationStatus.Completed;
                }
            }
            catch (Exception ex)
            {
                throw new ExceptionWithConfigData(ex.Message, configData);
            }

            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var response = await CallProviderAsync(query);

            if (response.StartsWith("{\"error\""))
                throw new ExceptionWithConfigData(response, configData);

            try
            {
                var json = JArray.Parse(response);

                foreach (var row in json)
                {
                    var isFreeleech = row.Value<bool>("freeleech");

                    // skip non-freeleech results when freeleech only is set
                    if (configData.Freeleech.Value && !isFreeleech)
                        continue;

                    var detailsUri = new Uri(DetailsUrl + "?id=" + row.Value<string>("id"));
                    var seeders = row.Value<int>("seeders");
                    var peers = seeders + row.Value<int>("leechers");
                    var publishDate = DateTime.Parse(row.Value<string>("upload_date") + " +0300", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
                    var downloadVolumeFactor = isFreeleech ? 0 : 1;
                    var uploadVolumeFactor = row.Value<bool>("doubleup") ? 2 : 1;
                    var imdbId = ((JObject)row).ContainsKey("imdb") ? ParseUtil.GetImdbID(row.Value<string>("imdb")) : null;
                    var link = new Uri(row.Value<string>("download_link"));

                    var release = new ReleaseInfo
                    {
                        Guid = detailsUri,
                        Details = detailsUri,
                        Link = link,
                        Title = row.Value<string>("name").Trim(),
                        Category = MapTrackerCatDescToNewznab(row.Value<string>("category")),
                        Size = row.Value<long>("size"),
                        Files = row.Value<long>("files"),
                        Grabs = row.Value<long>("times_completed"),
                        Seeders = seeders,
                        Peers = peers,
                        Imdb = imdbId,
                        PublishDate = publishDate,
                        DownloadVolumeFactor = downloadVolumeFactor,
                        UploadVolumeFactor = uploadVolumeFactor,
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

        private async Task<string> CallProviderAsync(TorznabQuery query)
        {
            var searchUrl = ApiUrl;
            var searchString = query.SanitizedSearchTerm.Trim();

            var queryCollection = new NameValueCollection
            {
                {"category", string.Join(",", MapTorznabCapsToTrackers(query))}
            };

            if (configData.Freeleech.Value)
                queryCollection.Set("freeleech", "1");

            if (query.IsImdbQuery || searchString.IsNotNullOrWhiteSpace())
            {
                queryCollection.Set("action", "search-torrents");

                if (query.IsImdbQuery)
                {
                    queryCollection.Set("type", "imdb");
                    queryCollection.Set("query", query.ImdbID);
                }
                else if (searchString.IsNotNullOrWhiteSpace())
                {
                    queryCollection.Set("type", "name");
                    queryCollection.Set("query", searchString);
                }

                if (query.Season > 0)
                    queryCollection.Set("season", query.Season.ToString());

                if (query.Episode.IsNotNullOrWhiteSpace())
                    queryCollection.Set("episode", query.Episode);
            }
            else
                queryCollection.Set("action", "latest-torrents");

            searchUrl += "?" + queryCollection.GetQueryString();

            try
            {
                var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(configData.Username.Value + ":" + configData.Passkey.Value));
                var headers = new Dictionary<string, string>
                {
                    {"Authorization", "Basic " + auth}
                };
                var response = await RequestWithCookiesAsync(searchUrl, headers: headers);

                return response.ContentString;
            }
            catch (Exception inner)
            {
                throw new Exception("Error calling provider filelist", inner);
            }
        }
    }
}
