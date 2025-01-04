using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jackett.Common.Extensions;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;
using WebRequest = Jackett.Common.Utils.Clients.WebRequest;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class Knaben : IndexerBase
    {
        public override string Id => "knaben";
        public override string Name => "Knaben";
        public override string Description => "Knaben is a Public torrent meta-search engine";
        public override string SiteLink { get; protected set; } = "https://knaben.org/";
        public override string[] LegacySiteLinks => new[]
        {
            "https://knaben.eu/",
        };
        public override string Language => "en-US";
        public override string Type => "public";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        public Knaben(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps, ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationData())
        {
        }

        private static TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q
                },
                MusicSearchParams = new List<MusicSearchParam>
                {
                    MusicSearchParam.Q
                },
                BookSearchParams = new List<BookSearchParam>
                {
                    BookSearchParam.Q
                }
            };

            caps.Categories.AddCategoryMapping(1000000, TorznabCatType.Audio, "Audio");
            caps.Categories.AddCategoryMapping(1001000, TorznabCatType.AudioMP3, "MP3");
            caps.Categories.AddCategoryMapping(1002000, TorznabCatType.AudioLossless, "Lossless");
            caps.Categories.AddCategoryMapping(1003000, TorznabCatType.AudioAudiobook, "Audiobook");
            caps.Categories.AddCategoryMapping(1004000, TorznabCatType.AudioVideo, "Audio Video");
            caps.Categories.AddCategoryMapping(1005000, TorznabCatType.AudioOther, "Radio");
            caps.Categories.AddCategoryMapping(1006000, TorznabCatType.AudioOther, "Audio Other");
            caps.Categories.AddCategoryMapping(2000000, TorznabCatType.TV, "TV");
            caps.Categories.AddCategoryMapping(2001000, TorznabCatType.TVHD, "TV HD");
            caps.Categories.AddCategoryMapping(2002000, TorznabCatType.TVSD, "TV SD");
            caps.Categories.AddCategoryMapping(2003000, TorznabCatType.TVUHD, "TV UHD");
            caps.Categories.AddCategoryMapping(2004000, TorznabCatType.TVDocumentary, "Documentary");
            caps.Categories.AddCategoryMapping(2005000, TorznabCatType.TVForeign, "TV Foreign");
            caps.Categories.AddCategoryMapping(2006000, TorznabCatType.TVSport, "Sport");
            caps.Categories.AddCategoryMapping(2007000, TorznabCatType.TVOther, "Cartoon");
            caps.Categories.AddCategoryMapping(2008000, TorznabCatType.TVOther, "TV Other");
            caps.Categories.AddCategoryMapping(3000000, TorznabCatType.Movies, "Movies");
            caps.Categories.AddCategoryMapping(3001000, TorznabCatType.MoviesHD, "Movies HD");
            caps.Categories.AddCategoryMapping(3002000, TorznabCatType.MoviesSD, "Movies SD");
            caps.Categories.AddCategoryMapping(3003000, TorznabCatType.MoviesUHD, "Movies UHD");
            caps.Categories.AddCategoryMapping(3004000, TorznabCatType.MoviesDVD, "Movies DVD");
            caps.Categories.AddCategoryMapping(3005000, TorznabCatType.MoviesForeign, "Movies Foreign");
            caps.Categories.AddCategoryMapping(3006000, TorznabCatType.MoviesForeign, "Movies Bollywood");
            caps.Categories.AddCategoryMapping(3007000, TorznabCatType.Movies3D, "Movies 3D");
            caps.Categories.AddCategoryMapping(3008000, TorznabCatType.MoviesOther, "Movies Other");
            caps.Categories.AddCategoryMapping(4000000, TorznabCatType.PC, "PC");
            caps.Categories.AddCategoryMapping(4001000, TorznabCatType.PCGames, "Games");
            caps.Categories.AddCategoryMapping(4002000, TorznabCatType.PC0day, "Software");
            caps.Categories.AddCategoryMapping(4003000, TorznabCatType.PCMac, "Mac");
            caps.Categories.AddCategoryMapping(4004000, TorznabCatType.PCISO, "Unix");
            caps.Categories.AddCategoryMapping(5000000, TorznabCatType.XXX, "XXX");
            caps.Categories.AddCategoryMapping(5001000, TorznabCatType.XXXx264, "XXX Video");
            caps.Categories.AddCategoryMapping(5002000, TorznabCatType.XXXImageSet, "XXX ImageSet");
            caps.Categories.AddCategoryMapping(5003000, TorznabCatType.XXXOther, "XXX Games");
            caps.Categories.AddCategoryMapping(5004000, TorznabCatType.XXXOther, "XXX Hentai");
            caps.Categories.AddCategoryMapping(5005000, TorznabCatType.XXXOther, "XXX Other");
            caps.Categories.AddCategoryMapping(6000000, TorznabCatType.TVAnime, "Anime");
            caps.Categories.AddCategoryMapping(6001000, TorznabCatType.TVAnime, "Anime Subbed");
            caps.Categories.AddCategoryMapping(6002000, TorznabCatType.TVAnime, "Anime Dubbed");
            caps.Categories.AddCategoryMapping(6003000, TorznabCatType.TVAnime, "Anime Dual audio");
            caps.Categories.AddCategoryMapping(6004000, TorznabCatType.TVAnime, "Anime Raw");
            caps.Categories.AddCategoryMapping(6005000, TorznabCatType.AudioVideo, "Music Video");
            caps.Categories.AddCategoryMapping(6006000, TorznabCatType.BooksOther, "Literature");
            caps.Categories.AddCategoryMapping(6007000, TorznabCatType.AudioOther, "Music");
            caps.Categories.AddCategoryMapping(6008000, TorznabCatType.TVAnime, "Anime non-english translated");
            caps.Categories.AddCategoryMapping(7000000, TorznabCatType.Console, "Console");
            caps.Categories.AddCategoryMapping(7001000, TorznabCatType.ConsolePS4, "PS4");
            caps.Categories.AddCategoryMapping(7002000, TorznabCatType.ConsolePS3, "PS3");
            caps.Categories.AddCategoryMapping(7003000, TorznabCatType.ConsolePS3, "PS2");
            caps.Categories.AddCategoryMapping(7004000, TorznabCatType.ConsolePS3, "PS1");
            caps.Categories.AddCategoryMapping(7005000, TorznabCatType.ConsolePSVita, "PS Vita");
            caps.Categories.AddCategoryMapping(7006000, TorznabCatType.ConsolePSP, "PSP");
            caps.Categories.AddCategoryMapping(7007000, TorznabCatType.ConsoleXBox360, "Xbox 360");
            caps.Categories.AddCategoryMapping(7008000, TorznabCatType.ConsoleXBox, "Xbox");
            caps.Categories.AddCategoryMapping(7009000, TorznabCatType.ConsoleNDS, "Switch");
            caps.Categories.AddCategoryMapping(7010000, TorznabCatType.ConsoleNDS, "NDS");
            caps.Categories.AddCategoryMapping(7011000, TorznabCatType.ConsoleWii, "Wii");
            caps.Categories.AddCategoryMapping(7012000, TorznabCatType.ConsoleWiiU, "WiiU");
            caps.Categories.AddCategoryMapping(7013000, TorznabCatType.Console3DS, "3DS");
            caps.Categories.AddCategoryMapping(7014000, TorznabCatType.ConsoleWii, "GameCube");
            caps.Categories.AddCategoryMapping(7015000, TorznabCatType.ConsoleOther, "Other");
            caps.Categories.AddCategoryMapping(8000000, TorznabCatType.PCMobileOther, "Mobile");
            caps.Categories.AddCategoryMapping(8001000, TorznabCatType.PCMobileAndroid, "Android");
            caps.Categories.AddCategoryMapping(8002000, TorznabCatType.PCMobileiOS, "IOS");
            caps.Categories.AddCategoryMapping(8003000, TorznabCatType.PCMobileOther, "PC Other");
            caps.Categories.AddCategoryMapping(9000000, TorznabCatType.Books, "Books");
            caps.Categories.AddCategoryMapping(9001000, TorznabCatType.BooksEBook, "EBooks");
            caps.Categories.AddCategoryMapping(9002000, TorznabCatType.BooksComics, "Comics");
            caps.Categories.AddCategoryMapping(9003000, TorznabCatType.BooksMags, "Magazines");
            caps.Categories.AddCategoryMapping(9004000, TorznabCatType.BooksTechnical, "Technical");
            caps.Categories.AddCategoryMapping(9005000, TorznabCatType.BooksOther, "Books Other");
            caps.Categories.AddCategoryMapping(10000000, TorznabCatType.Other, "Other");
            caps.Categories.AddCategoryMapping(10001000, TorznabCatType.OtherMisc, "Other Misc");

            return caps;
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new KnabenRequestGenerator(TorznabCaps);
        }

        public override IParseIndexerResponse GetParser()
        {
            return new KnabenParser(TorznabCaps.Categories, logger);
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var releases = await PerformQuery(new TorznabQuery());

            await ConfigureIfOK(string.Empty, releases.Any(), () => throw new Exception("Could not find releases from this URL"));

            return IndexerConfigurationStatus.Completed;
        }
    }

    public class KnabenRequestGenerator : IIndexerRequestGenerator
    {
        private readonly TorznabCapabilities _capabilities;

        public KnabenRequestGenerator(TorznabCapabilities capabilities)
        {
            _capabilities = capabilities;
        }

        public IndexerPageableRequestChain GetSearchRequests(TorznabQuery query)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            var postData = new Dictionary<string, object>
            {
                { "order_by", "date" },
                { "order_direction", "desc" },
                { "from", 0 },
                { "size", 100 },
                { "hide_unsafe", true }
            };

            var searchString = query.GetQueryString().Trim();

            if (searchString.IsNotNullOrWhiteSpace())
            {
                postData.Add("search_type", "100%");
                postData.Add("search_field", "title");
                postData.Add("query", searchString);
            }

            var categories = _capabilities.Categories.MapTorznabCapsToTrackers(query);

            if (categories.Any())
            {
                postData.Add("categories", categories.Select(int.Parse).Distinct().ToArray());
            }

            pageableRequests.Add(GetPagedRequests(postData));

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(Dictionary<string, object> postData)
        {
            var request = new WebRequest
            {
                Url = "https://api.knaben.org/v1",
                Type = RequestType.POST,
                Headers = new Dictionary<string, string>
                {
                    { "Accept", "application/json" },
                    { "Content-Type", "application/json" }
                },
                RawBody = JsonConvert.SerializeObject(postData)
            };

            yield return new IndexerRequest(request);
        }
    }

    public class KnabenParser : IParseIndexerResponse
    {
        private readonly TorznabCapabilitiesCategories _categories;
        private readonly Logger _logger;

        private static readonly Regex _DateTimezoneRegex = new Regex(@"[+-]\d{2}:\d{2}$", RegexOptions.Compiled);

        public KnabenParser(TorznabCapabilitiesCategories categories, Logger logger)
        {
            _categories = categories;
            _logger = logger;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            if (indexerResponse.WebResponse.Status != HttpStatusCode.OK)
            {
                if (indexerResponse.WebResponse.IsRedirect)
                {
                    _logger.Warn("Redirected to {0} from indexer request", indexerResponse.WebResponse.RedirectingTo);
                }

                throw new Exception($"Unexpected response status '{indexerResponse.WebResponse.Status}' code from indexer request");
            }

            var releases = new List<ReleaseInfo>();

            var jsonResponse = JsonConvert.DeserializeObject<KnabenResponse>(indexerResponse.Content);

            if (jsonResponse?.Hits == null)
            {
                return releases;
            }

            var rows = jsonResponse.Hits.Where(r => r.Seeders > 0).ToList();

            foreach (var row in rows)
            {
                // Not all entries have the TZ in the "date" field
                var publishDate = row.Date.IsNotNullOrWhiteSpace() && !_DateTimezoneRegex.IsMatch(row.Date) ? $"{row.Date}+01:00" : row.Date;

                var releaseInfo = new ReleaseInfo
                {
                    Guid = new Uri(row.InfoUrl),
                    Title = row.Title,
                    Details = new Uri(row.InfoUrl),
                    Link = row.DownloadUrl.IsNotNullOrWhiteSpace() && Uri.TryCreate(row.DownloadUrl, UriKind.Absolute, out var downloadUrl) ? downloadUrl : null,
                    MagnetUri = row.MagnetUrl.IsNotNullOrWhiteSpace() && Uri.TryCreate(row.MagnetUrl, UriKind.Absolute, out var magnetUrl) ? magnetUrl : null,
                    Category = row.CategoryIds.SelectMany(cat => _categories.MapTrackerCatToNewznab(cat.ToString())).Distinct().ToList(),
                    InfoHash = row.InfoHash,
                    Size = row.Size,
                    Seeders = row.Seeders,
                    Peers = row.Leechers + row.Seeders,
                    PublishDate = DateTime.Parse(publishDate, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
                    DownloadVolumeFactor = 0,
                    UploadVolumeFactor = 1
                };

                releases.Add(releaseInfo);
            }

            return releases;
        }
    }

    internal sealed record KnabenResponse
    {
        public IReadOnlyCollection<KnabenRelease> Hits { get; set; } = Array.Empty<KnabenRelease>();
    }

    internal sealed record KnabenRelease
    {
        public string Title { get; set; }

        [JsonProperty("categoryId")]
        public IReadOnlyCollection<int> CategoryIds { get; set; } = Array.Empty<int>();

        [JsonProperty("hash")]
        public string InfoHash { get; set; }

        [JsonProperty("details")]
        public string InfoUrl { get; set; }

        [JsonProperty("link")]
        public string DownloadUrl { get; set; }

        public string MagnetUrl { get; set; }

        [JsonProperty("bytes")]
        public long Size { get; set; }

        public int Seeders { get; set; }

        [JsonProperty("peers")]
        public int Leechers { get; set; }

        public string Date { get; set; }
    }
}
