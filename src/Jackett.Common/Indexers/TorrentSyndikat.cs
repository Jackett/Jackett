using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class TorrentSyndikat : BaseWebIndexer
    {
        private string ApiBase => SiteLink + "api_9djWe8Tb2NE3p6opyqnh/v1";

        private ConfigurationDataAPIKey ConfigData
        {
            get => (ConfigurationDataAPIKey)configData;
            set => configData = value;
        }

        public TorrentSyndikat(IIndexerConfigurationService configService, WebClient w, Logger l, IProtectionService ps)
            : base(id: "torrentsyndikat",
                   name: "Torrent-Syndikat",
                   description: "A German general tracker",
                   link: "https://torrent-syndikat.org/",
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
                   client: w,
                   logger: l,
                   p: ps,
                   configData: new ConfigurationDataAPIKey())
        {
            Encoding = Encoding.UTF8;
            Language = "de-de";
            Type = "private";

            AddCategoryMapping(2, TorznabCatType.PC, "Apps / Windows");
            AddCategoryMapping(13, TorznabCatType.PC, "Apps / Linux");
            AddCategoryMapping(4, TorznabCatType.PCMac, "Apps / MacOS");
            AddCategoryMapping(6, TorznabCatType.PC, "Apps / Misc");

            AddCategoryMapping(50, TorznabCatType.PCGames, "Spiele / Windows");
            AddCategoryMapping(51, TorznabCatType.PCGames, "Spiele / MacOS");
            AddCategoryMapping(52, TorznabCatType.PCGames, "Spiele / Linux");
            AddCategoryMapping(8, TorznabCatType.ConsoleOther, "Spiele / Playstation");
            AddCategoryMapping(7, TorznabCatType.ConsoleOther, "Spiele / Nintendo");
            AddCategoryMapping(32, TorznabCatType.ConsoleOther, "Spiele / XBOX");

            AddCategoryMapping(42, TorznabCatType.MoviesUHD, "Filme / 2160p");
            AddCategoryMapping(9, TorznabCatType.MoviesHD, "Filme / 1080p");
            AddCategoryMapping(20, TorznabCatType.MoviesHD, "Filme / 720p");
            AddCategoryMapping(10, TorznabCatType.MoviesSD, "Filme / SD");

            AddCategoryMapping(43, TorznabCatType.TVUHD, "Serien / 2160p");
            AddCategoryMapping(53, TorznabCatType.TVHD, "Serien / 1080p");
            AddCategoryMapping(54, TorznabCatType.TVHD, "Serien / 720p");
            AddCategoryMapping(15, TorznabCatType.TVSD, "Serien / SD");
            AddCategoryMapping(30, TorznabCatType.TVSport, "Serien / Sport");

            AddCategoryMapping(44, TorznabCatType.TVUHD, "Serienpacks / 2160p");
            AddCategoryMapping(55, TorznabCatType.TVHD, "Serienpacks / 1080p");
            AddCategoryMapping(56, TorznabCatType.TVHD, "Serienpacks / 720p");
            AddCategoryMapping(27, TorznabCatType.TVSD, "Serienpacks / SD");

            AddCategoryMapping(24, TorznabCatType.AudioLossless, "Audio / Musik / FLAC");
            AddCategoryMapping(25, TorznabCatType.AudioMP3, "Audio / Musik / MP3");
            AddCategoryMapping(35, TorznabCatType.AudioOther, "Audio / Other");
            AddCategoryMapping(18, TorznabCatType.AudioAudiobook, "Audio / aBooks");
            AddCategoryMapping(33, TorznabCatType.AudioVideo, "Audio / Videos");

            AddCategoryMapping(17, TorznabCatType.Books, "Misc / eBooks");
            AddCategoryMapping(5, TorznabCatType.PCMobileOther, "Misc / Mobile");
            AddCategoryMapping(39, TorznabCatType.Other, "Misc / Bildung");

            AddCategoryMapping(36, TorznabCatType.TVForeign, "Englisch / Serien");
            AddCategoryMapping(57, TorznabCatType.TVForeign, "Englisch / Serienpacks");
            AddCategoryMapping(37, TorznabCatType.MoviesForeign, "Englisch / Filme");
            AddCategoryMapping(47, TorznabCatType.Books, "Englisch / eBooks");
            AddCategoryMapping(48, TorznabCatType.Other, "Englisch / Bildung");
            AddCategoryMapping(49, TorznabCatType.TVSport, "Englisch / Sport");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            ConfigData.LoadValuesFromJson(configJson);
            var releases = await PerformQuery(new TorznabQuery());

            await ConfigureIfOK(
                string.Empty,
                releases.Any(),
                () => throw new Exception("Could not find any releases"));

            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var searchString = query.GetQueryString();
            var queryCollection = new NameValueCollection {{"apikey", ConfigData.Key.Value}};

            queryCollection.Add("limit", "50"); // Default 30
            //queryCollection.Add("ponly", true); // Torrents with products only

            if (query.ImdbIDShort != null)
            {
                queryCollection.Add("imdbId", query.ImdbIDShort);
            }
            else if (!string.IsNullOrWhiteSpace(searchString))
            {
                // Suffix the first occurence of `s01` surrounded by whitespace with *
                // That way we also search for single episodes in a whole season search
                var regex = new Regex(@"(^|\s)(s\d{2})(\s|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                queryCollection.Add("searchstring", regex.Replace(searchString.Trim(), @"$1$2*$3"));
            }

            var cats = string.Join(",", MapTorznabCapsToTrackers(query));
            if (!string.IsNullOrEmpty(cats))
            {
                queryCollection.Add("cats", cats);
            }

            var searchUrl = ApiBase + "/browse.php?" + queryCollection.GetQueryString();
            var response = await RequestWithCookiesAsync(searchUrl, string.Empty);

            try
            {
                CheckResponseStatus(response.Status, "browse");

                var jsonContent = JObject.Parse(response.ContentString);

                foreach (var row in jsonContent.Value<JArray>("rows"))
                {
                    var dateTime = new DateTime(1970,1,1,0,0,0,0,DateTimeKind.Utc);

                    var id = row.Value<string>("id");
                    var details = new Uri(SiteLink + "details.php?id=" + id);
                    var seeders = row.Value<int>("seeders");

                    var release = new ReleaseInfo
                    {
                        MinimumRatio = 1,
                        MinimumSeedTime = 96 * 60 * 60,
                        DownloadVolumeFactor = 1,
                        UploadVolumeFactor = 1,
                        Guid = details,
                        Details = details,
                        Link = new Uri(SiteLink + "download.php?id=" + id),
                        Title = row.Value<string>("name"),
                        Category = MapTrackerCatToNewznab(row.Value<int>("category").ToString()),
                        PublishDate = dateTime.AddSeconds(row.Value<long>("added")).ToLocalTime(),
                        Size = row.Value<long>("size"),
                        Files = row.Value<long>("numfiles"),
                        Seeders = seeders,
                        Peers = seeders + row.Value<int>("leechers"),
                        Grabs = row.Value<int>("snatched"),
                        Imdb = row.Value<long?>("imdbId"),
                        TVDBId = row.Value<long?>("tvdbId"),
                        TMDb = row.Value<long?>("tmdbId")
                    };

                    var poster = row.Value<string>("poster");
                    if (!string.IsNullOrWhiteSpace(poster))
                    {
                        release.Poster = new Uri(SiteLink + poster.Substring(1));
                    }

                    var descriptions = new List<string>();
                    var title = row.Value<string>("title");
                    var titleOrigin = row.Value<string>("title_origin");
                    var year = row.Value<int?>("year");
                    var pid = row.Value<int?>("pid");
                    var releaseType = row.Value<string>("release_type");
                    var tags = row.Value<JArray>("tags");
                    var genres = row.Value<JArray>("genres");

                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        descriptions.Add("Title: " + title);
                    }
                    if (!string.IsNullOrWhiteSpace(titleOrigin))
                    {
                        descriptions.Add("Original Title: " + titleOrigin);
                    }
                    if (year > 0)
                    {
                        descriptions.Add("Year: " + year);
                    }
                    if (pid > 0)
                    {
                        descriptions.Add("Product-Link: " + SiteLink + "product.php?pid=" + pid);
                    }
                    if (genres != null && genres.Any())
                    {
                        descriptions.Add("Genres: " + string.Join(", ", genres));
                    }
                    if (tags != null && tags.Any())
                    {
                        descriptions.Add("Tags: " + string.Join(", ", tags));
                    }
                    if (!string.IsNullOrWhiteSpace(releaseType))
                    {
                        descriptions.Add("Release Type: " + releaseType);
                    }

                    if (descriptions.Any())
                    {
                        release.Description = string.Join(Environment.NewLine, descriptions);
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

        public override async Task<byte[]> Download(Uri link)
        {
            var response = await RequestWithCookiesAsync(link.ToString() + "&apikey=" + ConfigData.Key.Value, string.Empty);
            CheckResponseStatus(response.Status, "download");
            return response.ContentBytes;
        }

        private static void CheckResponseStatus(HttpStatusCode status, string scope)
        {
            switch (status)
            {
                case HttpStatusCode.OK:
                    return;
                case HttpStatusCode.BadRequest:
                    throw new Exception("Unknown or missing parameters");
                case HttpStatusCode.Unauthorized:
                    throw new Exception("Wrong API-Key");
                case HttpStatusCode.Forbidden:
                    throw new Exception("API-Key has no authorization for the endpoint / scope " + scope);
                default:
                    throw new Exception("Unexpected response status code " + status);
            }
        }
    }
}
