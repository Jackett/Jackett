using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class TorrentMafya : BaseWebIndexer
    {
        private class TorrentMafyaRowResult
        {
            public string col1 { get; set; }
            public string col2 { get; set; }
            public string col3 { get; set; }
        }
        private class TorrentMafyaArchiveResponse
        {
            public IEnumerable<TorrentMafyaRowResult> aaData { get; set; }
        }

        public TorrentMafya(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "torrentmafya",
                   name: "TorrentMafya",
                   description: "TorrentMafya is a Turkish general torrent tracker ",
                   link: "https://www.torrentmafya.org/",
                   caps: new TorznabCapabilities
                   {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                       },
                       MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q
                       }
                   },
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationData())
        {
            Encoding = Encoding.UTF8;
            Language = "tr-TR";
            Type = "public";

            AddCategoryMapping("games", TorznabCatType.PCGames, "Oyun");
            AddCategoryMapping("programs", TorznabCatType.PC, "Program");
            AddCategoryMapping("movies", TorznabCatType.Movies, "Film");
            AddCategoryMapping("tv", TorznabCatType.TV, "Dizi");
            AddCategoryMapping("apk", TorznabCatType.PCMobileAndroid, "APK");

            configData.AddDynamic("keyInfo", new DisplayInfoConfigurationItem(String.Empty, "TorrentMafya allows only Turkish IP addressess. The error <b>403 Forbidden: Parse error</b> means your IP was not accepted."));
        }

        private static DateTime ParseReleasePublishDate(string date)
        {
            try
            {
                var splitDate = date.Split(' ');
                var firstPart = int.Parse(splitDate[0]) * -1;
                var secondPart = splitDate[1].ToLowerInvariant();
                switch (secondPart)
                {
                    case "saat":
                        return DateTime.Now.AddHours(firstPart);
                    case "gün":
                        return DateTime.Now.AddDays(firstPart);
                    case "ay":
                        return DateTime.Now.AddMonths(firstPart);
                    case "yıl":
                        return DateTime.Now.AddYears(firstPart);
                    default:
                        return DateTime.MinValue;
                }
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        private static List<int> ParseReleaseCategory(ITokenList classes)
        {
            var result = new List<int>();
            if (classes.Contains("fa-gamepad"))
            {
                result.Add(TorznabCatType.PCGames.ID);
            }
            else if (classes.Contains("fa-film"))
            {
                result.Add(TorznabCatType.Movies.ID);
            }
            else if (classes.Contains("fa-tv"))
            {
                result.Add(TorznabCatType.TV.ID);
            }
            else if (classes.Contains("fa-microchip"))
            {
                result.Add(TorznabCatType.PC.ID);
            }
            else if (classes.Contains("fa-android"))
            {
                result.Add(TorznabCatType.PCMobileAndroid.ID);
            }
            return result;
        }
        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var releases = await PerformQuery(new TorznabQuery());

            await ConfigureIfOK(string.Empty, releases.Any(), () =>
                                    throw new Exception("Could not find release from this URL."));

            return IndexerConfigurationStatus.Completed;
        }

        private static ReleaseInfo ParseReleaseInfo(IHtmlParser parser, TorrentMafyaRowResult row)
        {
            var firstColumn = parser.ParseDocument(row.col1);
            var mainColumn = parser.ParseDocument(row.col3);
            var sizeContent = mainColumn.QuerySelector("span.boyut")?.TextContent;
            var magnetLink = mainColumn.QuerySelector("a[href^=\"magnet:?xt=\"]")?.GetAttribute("href");
            var fileLink = mainColumn.QuerySelector("a[title^=\"İndir\"]")?.GetAttribute("href");
            var detailsLink = new Uri(firstColumn.QuerySelector("a").GetAttribute("href"));
            var category = ParseReleaseCategory(firstColumn.QuerySelector("i")?.ClassList);
            var seederContent = mainColumn.QuerySelector("span.sayiGonderen")?.TextContent;
            var leecherContent = mainColumn.QuerySelector("span.sayiIndiren")?.TextContent;
            int.TryParse(seederContent, out var seeders);
            int.TryParse(leecherContent, out var leechers);
            return new ReleaseInfo
            {
                Title = firstColumn.QuerySelector("a")?.TextContent,
                Details = detailsLink,
                Guid = detailsLink,
                Link = string.IsNullOrWhiteSpace(fileLink) ? null : new Uri(fileLink),
                MagnetUri = string.IsNullOrWhiteSpace(magnetLink) ? null : new Uri(magnetLink),
                PublishDate = ParseReleasePublishDate(row.col2),
                Category = category,
                Seeders = seeders,
                Peers = seeders + leechers,
                DownloadVolumeFactor = 0,
                UploadVolumeFactor = 1,
                Size = !string.IsNullOrEmpty(sizeContent) ? ReleaseInfo.GetBytes(sizeContent) : 0
            };
        }

        private static Dictionary<string, string> PrepareSearchQueryCollection(string searchString) => new Dictionary<string, string>
        {
            {"draw", "8"},
            {"columns[0][data]", "col1"},
            {"columns[0][name]", ""},
            {"columns[0][searchable]", "true"},
            {"columns[0][orderable]", "false"},
            {"columns[0][search][value]", ""},
            {"columns[0][search][regex]", "false"},
            {"columns[1][data]", "col2"},
            {"columns[1][name]", ""},
            {"columns[1][searchable]", "true"},
            {"columns[1][orderable]", "false"},
            {"columns[1][search][value]", ""},
            {"columns[1][search][regex]", "false"},
            {"columns[2][data]", "col3"},
            {"columns[2][name]", ""},
            {"columns[2][searchable]", "true"},
            {"columns[2][orderable]", "false"},
            {"columns[2][search][value]", ""},
            {"columns[2][search][regex]", "false"},
            {"start", "0"},
            {"length", "20"},
            {"search[value]", searchString},
            {"search[regex]", "false"},
            {"tur", "1"}
        };


        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var result = new List<ReleaseInfo>();
            var searchString = query.GetQueryString().Replace(" ", "%");
            var searchUrl = $"{SiteLink}table.php";
            var referrer = $"{SiteLink}arsiv/";
            var body = PrepareSearchQueryCollection(searchString);
            var response = await RequestWithCookiesAsync(searchUrl, null, RequestType.POST, referrer, body);
            var content = response.ContentString;
            try
            {
                var json = JsonConvert.DeserializeObject<TorrentMafyaArchiveResponse>(response.ContentString);
                var parser = new HtmlParser();
                result.AddRange(json.aaData.Select(row => ParseReleaseInfo(parser, row)));
            }
            catch (Exception ex)
            {
                OnParseError(content, ex);
            }
            return result;
        }
    }
}
