using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
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
    public class FileList : BaseWebIndexer
    {
        public override string[] AlternativeSiteLinks { get; protected set; } = {
            "https://filelist.io/",
            "https://flro.org/"
        };

        public override string[] LegacySiteLinks { get; protected set; } =
        {
            "https://filelist.ro/",
            "http://filelist.ro/",
            "http://flro.org/"
        };

        private string ApiUrl => SiteLink + "api.php";
        private string DetailsUrl => SiteLink + "details.php";

        private new ConfigurationDataFileList configData => (ConfigurationDataFileList)base.configData;

        public FileList(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "filelist",
                   name: "FileList",
                   description: "The best Romanian site.",
                   link: "https://filelist.io/",
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
                   cacheService: cs,
                   configData: new ConfigurationDataFileList())
        {
            Encoding = Encoding.UTF8;
            Language = "ro-RO";
            Type = "private";

            AddCategoryMapping(1, TorznabCatType.MoviesSD, "Filme SD");
            AddCategoryMapping(2, TorznabCatType.MoviesDVD, "Filme DVD");
            AddCategoryMapping(3, TorznabCatType.MoviesForeign, "Filme DVD-RO");
            AddCategoryMapping(4, TorznabCatType.MoviesHD, "Filme HD");
            AddCategoryMapping(5, TorznabCatType.AudioLossless, "FLAC");
            AddCategoryMapping(6, TorznabCatType.MoviesUHD, "Filme 4K");
            AddCategoryMapping(7, TorznabCatType.XXX, "XXX");
            AddCategoryMapping(8, TorznabCatType.PC, "Programe");
            AddCategoryMapping(9, TorznabCatType.PCGames, "Jocuri PC");
            AddCategoryMapping(10, TorznabCatType.Console, "Jocuri Console");
            AddCategoryMapping(11, TorznabCatType.Audio, "Audio");
            AddCategoryMapping(12, TorznabCatType.AudioVideo, "Videoclip");
            AddCategoryMapping(13, TorznabCatType.TVSport, "Sport");
            AddCategoryMapping(15, TorznabCatType.TV, "Desene");
            AddCategoryMapping(16, TorznabCatType.Books, "Docs");
            AddCategoryMapping(17, TorznabCatType.PC, "Linux");
            AddCategoryMapping(18, TorznabCatType.Other, "Diverse");
            AddCategoryMapping(19, TorznabCatType.MoviesForeign, "Filme HD-RO");
            AddCategoryMapping(20, TorznabCatType.MoviesBluRay, "Filme Blu-Ray");
            AddCategoryMapping(21, TorznabCatType.TVHD, "Seriale HD");
            AddCategoryMapping(22, TorznabCatType.PCMobileOther, "Mobile");
            AddCategoryMapping(23, TorznabCatType.TVSD, "Seriale SD");
            AddCategoryMapping(24, TorznabCatType.TVAnime, "Anime");
            AddCategoryMapping(25, TorznabCatType.Movies3D, "Filme 3D");
            AddCategoryMapping(26, TorznabCatType.MoviesBluRay, "Filme 4K Blu-Ray");
            AddCategoryMapping(27, TorznabCatType.TVUHD, "Seriale 4K");
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
                    var publishDate = DateTime.Parse(row.Value<string>("upload_date") + " +0200", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
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
            var searchString = query.GetQueryString().Trim();

            var queryCollection = new NameValueCollection
            {
                {"category", string.Join(",", MapTorznabCapsToTrackers(query))}
            };

            if (configData.Freeleech.Value)
                queryCollection.Set("freeleech", "1");

            if (query.IsImdbQuery)
            {
                queryCollection.Set("action", "search-torrents");
                queryCollection.Set("type", "imdb");
                queryCollection.Set("query", query.ImdbID);
            }
            else if (!string.IsNullOrWhiteSpace(searchString))
            {
                queryCollection.Set("action", "search-torrents");
                queryCollection.Set("type", "name");
                queryCollection.Set("query", searchString);
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
