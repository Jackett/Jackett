using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jackett.Common.Extensions;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Serializer;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class MTeamTp : IndexerBase
    {
        public override string Id => "mteamtp";
        public override string[] Replaces => new[] { "mteamtp2fa" };
        public override string Name => "M-Team - TP";
        public override string Description => "M-Team TP (MTTP) is a CHINESE Private Torrent Tracker for HD MOVIES / TV / 3X";
        public override string SiteLink { get; protected set; } = "https://kp.m-team.cc/";
        public override string[] AlternativeSiteLinks => new[]
        {
            "https://kp.m-team.cc/",
            "https://tp.m-team.cc/",
            "https://pt.m-team.cc/"
        };
        public override string Language => "zh-CN";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private readonly int[] _trackerAdultCategories = { 410, 429, 424, 430, 426, 437, 431, 432, 436, 425, 433, 411, 412, 413, 440 };

        private new ConfigurationDataMTeamTp configData => (ConfigurationDataMTeamTp)base.configData;

        public MTeamTp(IIndexerConfigurationService configService, WebClient client, Logger logger, IProtectionService p, ICacheService cs)
            : base(configService: configService,
                   client: client,
                   logger: logger,
                   p: p,
                   cacheService: cs,
                   configData: new ConfigurationDataMTeamTp())
        {
            webclient.requestDelay = 5;
        }

        private static TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
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
            };

            caps.Categories.AddCategoryMapping(401, TorznabCatType.MoviesSD, "Movie(電影)/SD");
            caps.Categories.AddCategoryMapping(419, TorznabCatType.MoviesHD, "Movie(電影)/HD");
            caps.Categories.AddCategoryMapping(420, TorznabCatType.MoviesDVD, "Movie(電影)/DVDiSo");
            caps.Categories.AddCategoryMapping(421, TorznabCatType.MoviesBluRay, "Movie(電影)/Blu-Ray");
            caps.Categories.AddCategoryMapping(439, TorznabCatType.MoviesHD, "Movie(電影)/Remux");
            caps.Categories.AddCategoryMapping(403, TorznabCatType.TVSD, "TV Series(影劇/綜藝)/SD");
            caps.Categories.AddCategoryMapping(402, TorznabCatType.TVHD, "TV Series(影劇/綜藝)/HD");
            caps.Categories.AddCategoryMapping(435, TorznabCatType.TVSD, "TV Series(影劇/綜藝)/DVDiSo");
            caps.Categories.AddCategoryMapping(438, TorznabCatType.TVHD, "TV Series(影劇/綜藝)/BD");
            caps.Categories.AddCategoryMapping(404, TorznabCatType.TVDocumentary, "紀錄教育");
            caps.Categories.AddCategoryMapping(405, TorznabCatType.TVAnime, "Anime(動畫)");
            caps.Categories.AddCategoryMapping(407, TorznabCatType.TVSport, "Sports(運動)");
            caps.Categories.AddCategoryMapping(422, TorznabCatType.PC0day, "Software(軟體)");
            caps.Categories.AddCategoryMapping(423, TorznabCatType.PCGames, "PCGame(PC遊戲)");
            caps.Categories.AddCategoryMapping(427, TorznabCatType.BooksEBook, "Study/Edu ebook(教育書面)");
            caps.Categories.AddCategoryMapping(441, TorznabCatType.BooksOther, "Study/Edu video(教育影片)");
            caps.Categories.AddCategoryMapping(442, TorznabCatType.AudioAudiobook, "Study/Edu audio(教育音檔)");
            caps.Categories.AddCategoryMapping(409, TorznabCatType.Other, "Misc(其他)");

            // music
            caps.Categories.AddCategoryMapping(406, TorznabCatType.AudioVideo, "MV(演唱)");
            caps.Categories.AddCategoryMapping(408, TorznabCatType.AudioOther, "Music(AAC/ALAC)");
            caps.Categories.AddCategoryMapping(434, TorznabCatType.Audio, "Music(無損)");

            // adult
            caps.Categories.AddCategoryMapping(410, TorznabCatType.XXX, "AV(有碼)/HD Censored");
            caps.Categories.AddCategoryMapping(429, TorznabCatType.XXX, "AV(無碼)/HD Uncensored");
            caps.Categories.AddCategoryMapping(424, TorznabCatType.XXXSD, "AV(有碼)/SD Censored");
            caps.Categories.AddCategoryMapping(430, TorznabCatType.XXXSD, "AV(無碼)/SD Uncensored");
            caps.Categories.AddCategoryMapping(426, TorznabCatType.XXXDVD, "AV(無碼)/DVDiSo Uncensored");
            caps.Categories.AddCategoryMapping(437, TorznabCatType.XXXDVD, "AV(有碼)/DVDiSo Censored");
            caps.Categories.AddCategoryMapping(431, TorznabCatType.XXX, "AV(有碼)/Blu-Ray Censored");
            caps.Categories.AddCategoryMapping(432, TorznabCatType.XXX, "AV(無碼)/Blu-Ray Uncensored");
            caps.Categories.AddCategoryMapping(436, TorznabCatType.XXX, "AV(網站)/0Day");
            caps.Categories.AddCategoryMapping(425, TorznabCatType.XXX, "IV(寫真影集)/Video Collection");
            caps.Categories.AddCategoryMapping(433, TorznabCatType.XXXImageSet, "IV(寫真圖集)/Picture Collection");
            caps.Categories.AddCategoryMapping(411, TorznabCatType.XXX, "H-Game(遊戲)");
            caps.Categories.AddCategoryMapping(412, TorznabCatType.XXX, "H-Anime(動畫)");
            caps.Categories.AddCategoryMapping(413, TorznabCatType.XXX, "H-Comic(漫畫)");
            caps.Categories.AddCategoryMapping(440, TorznabCatType.XXX, "AV(Gay)/HD");

            return caps;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            if (configData.ApiKey.Value.IsNullOrWhiteSpace())
            {
                throw new Exception("Missing API Key.");
            }

            var releases = await PerformQuery(new TorznabQuery());

            await ConfigureIfOK(string.Empty, releases.Any(),
                                () => throw new Exception("Could not find releases."));

            return IndexerConfigurationStatus.Completed;
        }

        public override async Task<byte[]> Download(Uri link)
        {
            var response = await RequestWithCookiesAsync(
                link.ToString(),
                method: RequestType.POST,
                headers: new Dictionary<string, string>
                {
                    { "Accept", "application/json" },
                    { "x-api-key", configData.ApiKey.Value }
                });

            if (!STJson.TryDeserialize<MTeamTpApiDownloadTokenResponse>(response.ContentString, out var jsonResponse))
            {
                throw new Exception("Invalid response received from M-Team, not a valid JSON");
            }

            if (jsonResponse.Data.IsNullOrWhiteSpace())
            {
                throw new Exception($"Unable to find download link for: {link}");
            }

            return await base.Download(new Uri(jsonResponse.Data));
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var categoryMapping = MapTorznabCapsToTrackers(query).Select(int.Parse).Distinct().ToList();

            var adultCategories = categoryMapping.Where(c => _trackerAdultCategories.Contains(c)).ToList();
            var normalCategories = categoryMapping.Except(adultCategories).ToList();

            if (!categoryMapping.Any() || normalCategories.Any())
            {
                releases.AddRange(await FetchTrackerReleasesAsync(MTeamTpRequestType.Normal, query, normalCategories));
            }

            if (adultCategories.Any())
            {
                releases.AddRange(await FetchTrackerReleasesAsync(MTeamTpRequestType.Adult, query, adultCategories));
            }

            return releases
               .OrderByDescending(o => o.PublishDate)
               .ToArray();
        }

        private async Task<IEnumerable<ReleaseInfo>> FetchTrackerReleasesAsync(MTeamTpRequestType requestType, TorznabQuery query, IEnumerable<int> categories)
        {
            var releases = new List<ReleaseInfo>();

            var searchQuery = new MTeamTpApiSearchQuery
            {
                Mode = requestType,
                Categories = categories?.Select(x => x.ToString()).ToArray() ?? Array.Empty<string>(),
                PageNumber = 1,
                PageSize = 100
            };

            if (query.ImdbID.IsNotNullOrWhiteSpace())
            {
                searchQuery.Imdb = query.ImdbID.Trim();
            }

            var searchTerm = query.GetQueryString();

            if (searchTerm.IsNotNullOrWhiteSpace())
            {
                searchQuery.Keyword = searchTerm;
            }

            if (configData.FreeleechOnly.Value)
            {
                searchQuery.Discount = "FREE";
            }

            var apiUrl = "https://api." + $"{SiteLink}".Substring(SiteLink.IndexOf('.') + 1);

            var response = await RequestWithCookiesAndRetryAsync(
                $"{apiUrl}api/torrent/search",
                method: RequestType.POST,
                rawbody: STJson.ToJson(searchQuery),
                headers: new Dictionary<string, string>
                {
                    { "Accept", "application/json" },
                    { "Content-Type", "application/json" },
                    { "x-api-key", configData.ApiKey.Value }
                });

            if (response.Status != HttpStatusCode.OK)
            {
                throw new Exception($"Unknown status code: {(int)response.Status} ({response.Status})");
            }

            if (!STJson.TryDeserialize<MTeamTpApiResponse>(response.ContentString, out var jsonResponse))
            {
                throw new Exception("Invalid response received from M-Team, not a valid JSON");
            }

            if (jsonResponse?.Data?.Torrents == null)
            {
                if (jsonResponse != null &&
                    jsonResponse.Message.IsNotNullOrWhiteSpace() &&
                    jsonResponse.Message.ToUpperInvariant() != "SUCCESS")
                {
                    throw new Exception($"Invalid response received from M-Team. Response from API: {jsonResponse.Message}");
                }

                return releases;
            }

            foreach (var torrent in jsonResponse.Data.Torrents)
            {
                var torrentId = int.Parse(torrent.Id);
                var infoUrl = new Uri($"{SiteLink}detail/{torrentId}");
                var downloadUrl = new Uri($"{apiUrl}api/torrent/genDlToken?id={torrentId}");

                var release = new ReleaseInfo
                {
                    Guid = infoUrl,
                    Title = CleanTitle(torrent.Name),
                    Details = infoUrl,
                    Link = downloadUrl,
                    Category = MapTrackerCatToNewznab(torrent.Category),
                    Description = torrent.Description,
                    Files = int.Parse(torrent.NumFiles),
                    Size = long.Parse(torrent.Size),
                    Grabs = int.Parse(torrent.Status.TimesCompleted),
                    Seeders = int.Parse(torrent.Status.Seeders),
                    Peers = int.Parse(torrent.Status.Seeders) + int.Parse(torrent.Status.Leechers),
                    DownloadVolumeFactor = torrent.Status.Discount.ToUpperInvariant() switch
                    {
                        "FREE" => 0,
                        "_2X_FREE" => 0,
                        "PERCENT_50" => 0.5,
                        "_2X_PERCENT_50" => 0.5,
                        "PERCENT_70" => 0.3,
                        _ => 1
                    },
                    UploadVolumeFactor = torrent.Status.Discount.ToUpperInvariant() switch
                    {
                        "_2X_FREE" => 2,
                        "_2X_PERCENT_50" => 2,
                        _ => 1
                    },
                    MinimumRatio = 1,
                    MinimumSeedTime = 172800 // 2 days
                };

                if (torrent.Status?.CreatedDate != null &&
                    DateTime.TryParseExact($"{torrent.Status.CreatedDate} +08:00", "yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var publishDate))
                {
                    release.PublishDate = publishDate;
                }

                releases.Add(release);
            }

            return releases;
        }

        private static string CleanTitle(string title)
        {
            title = Regex.Replace(title, @"\s+", " ", RegexOptions.Compiled);

            return title.Trim();
        }
    }

    internal enum MTeamTpRequestType
    {
        Normal,
        Adult
    }

    internal class MTeamTpApiSearchQuery
    {
        [JsonProperty(Required = Required.Always)]
        public MTeamTpRequestType Mode { get; set; }

        [JsonProperty(Required = Required.Always)]
        public IEnumerable<string> Categories { get; set; }

        public string Discount { get; set; }
        public string Imdb { get; set; }
        public string Keyword { get; set; }
        public int? PageNumber { get; set; }
        public int? PageSize { get; set; }
    }

    internal class MTeamTpApiResponse
    {
        public MTeamTpApiData Data { get; set; }
        public string Message { get; set; }
    }

    internal class MTeamTpApiData
    {
        [JsonPropertyName("data")]
        public IReadOnlyCollection<MTeamTpApiTorrent> Torrents { get; set; }
    }

    internal class MTeamTpApiTorrent
    {
        public string Id { get; set; }
        public string Name { get; set; }

        [JsonPropertyName("smallDescr")]
        public string Description { get; set; }

        public string Category { get; set; }

        [JsonPropertyName("numfiles")]
        public string NumFiles { get; set; }

        public string Imdb { get; set; }
        public string Size { get; set; }
        public MTeamTpApiReleaseStatus Status { get; set; }
    }

    internal class MTeamTpApiReleaseStatus
    {
        public string CreatedDate { get; set; }
        public string Discount { get; set; }
        public string TimesCompleted { get; set; }
        public string Seeders { get; set; }
        public string Leechers { get; set; }
    }

    internal class MTeamTpApiDownloadTokenResponse
    {
        public string Data { get; set; }
    }
}
