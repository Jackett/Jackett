using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
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
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class PolishTracker : BaseWebIndexer
    {
        private readonly string APIBASE = "https://api.pte.nu/torrents/";

        private new ConfigurationDataAPIKey configData
        {
            get => (ConfigurationDataAPIKey)base.configData;
            set => base.configData = value;
        }

        private readonly HttpClient client;
        private readonly int maxQueryCount = 250;

        public PolishTracker(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
                             ICacheService cs) : base(
            id: "polishtracker", name: "PolishTracker",
            description: "Polish Tracker is a POLISH Private site for 0DAY / MOVIES / GENERAL", link: "https://pte.nu/",
            caps: new TorznabCapabilities
            {
                TvSearchParams =
                    new List<TvSearchParam>
                    {
                        TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId
                    },
                MovieSearchParams = new List<MovieSearchParam> { MovieSearchParam.Q, MovieSearchParam.ImdbId },
                MusicSearchParams = new List<MusicSearchParam> { MusicSearchParam.Q },
                BookSearchParams = new List<BookSearchParam> { BookSearchParam.Q }
            }, configService: configService, client: wc, logger: l, p: ps, cacheService: cs,
            configData: new ConfigurationDataAPIKey())
        {
            Encoding = Encoding.UTF8;
            Language = "pl-PL";
            Type = "private";
            var handler = new HttpClientHandler();
            handler.ClientCertificateOptions = ClientCertificateOption.Manual;
            handler.ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => true;
            client = new HttpClient(handler);
            client.BaseAddress = new Uri(APIBASE);
            configData.AddDynamic(
                "LanguageTitle",
                new BoolConfigurationItem("Add POLISH to title if has Polish language. Use this if you using Sonarr/Radarr")
                {
                    Value = false
                });
            AddCategoryMapping(1, TorznabCatType.PC0day, "0-Day");
            AddCategoryMapping(2, TorznabCatType.AudioVideo, "Music Video");
            AddCategoryMapping(3, TorznabCatType.PC0day, "Apps");
            AddCategoryMapping(4, TorznabCatType.Console, "Consoles");
            AddCategoryMapping(5, TorznabCatType.Books, "E-book");
            AddCategoryMapping(6, TorznabCatType.MoviesHD, "Movies HD");
            AddCategoryMapping(7, TorznabCatType.MoviesSD, "Movies SD");
            AddCategoryMapping(8, TorznabCatType.Audio, "Music");
            AddCategoryMapping(9, TorznabCatType.MoviesUHD, "Movies UHD");
            AddCategoryMapping(10, TorznabCatType.PCGames, "PcGames");
            AddCategoryMapping(11, TorznabCatType.TVHD, "TV HD");
            AddCategoryMapping(12, TorznabCatType.TVSD, "TV SD");
            AddCategoryMapping(13, TorznabCatType.XXX, "XXX");
            AddCategoryMapping(14, TorznabCatType.TVUHD, "TV-UHD");
            AddCategoryMapping(15, TorznabCatType.AudioAudiobook, "Audiobook");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            IsConfigured = false;
            try
            {
                var results = await PerformQuery(new TorznabQuery { IsTest = true });
                if (!results.Any())
                    throw new Exception("Testing returned no results!");
                IsConfigured = true;
                SaveConfig();
            }
            catch (Exception e)
            {
                throw new ExceptionWithConfigData(e.Message, configData);
            }

            return IndexerConfigurationStatus.Completed;
        }

        private static string RemoveSpecialCharacters(string str)
        {
            var sb = new StringBuilder();
            foreach (var c in str)
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_' ||
                    c == ' ')
                    sb.Append(c);
            return sb.ToString();
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            string url;
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("API-Key", configData.Key.Value);
            if (query.IsTest)
                url = "list?num=1";
            else
                url = "list?num=" + maxQueryCount;
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var resp = await response.Content.ReadAsStringAsync();
            try
            {
                var pteResponse = JsonConvert.DeserializeObject<List<PTEResultItem>>(resp);
                if (pteResponse?.Count > 0)
                    foreach (var pteResult in pteResponse)
                    {
                        if (!RemoveSpecialCharacters(pteResult.Name).ToLower().Contains(
                            RemoveSpecialCharacters(query.SanitizedSearchTerm).ToLower()))
                            continue;
                        var descriptions = new List<string>
                        {
                            "ID: " + pteResult.ID,
                            "Name: " + pteResult.Name,
                            "Category: " + pteResult.Category,
                            "Comments: " + pteResult.Comments,
                            "Leechers: " + pteResult.Leechers,
                            "Seeders: " + pteResult.Seeders,
                            "Completed: " + pteResult.Completed
                        };
                        if (!string.IsNullOrWhiteSpace(pteResult.Imdb_ID))
                            descriptions.Add("IMDB_ID: " + pteResult.Imdb_ID);
                        if (!string.IsNullOrWhiteSpace(pteResult.Cdu_ID))
                            descriptions.Add("CDU_ID: " + pteResult.Cdu_ID);
                        if (!string.IsNullOrWhiteSpace(pteResult.Steam_ID))
                            descriptions.Add("STEAM_ID: " + pteResult.Steam_ID);
                        if (!string.IsNullOrWhiteSpace(pteResult.Subs))
                            descriptions.Add("Subs: " + pteResult.Subs);
                        if (!string.IsNullOrWhiteSpace(pteResult.Language))
                            descriptions.Add("Language: " + pteResult.Language);
                        var publishDate = DateTime.Parse(pteResult.Added);
                        descriptions.Add("Added: " + publishDate);
                        var imdb = ParseUtil.GetImdbID(pteResult.Imdb_ID);
                        var link = new Uri(APIBASE + "download/" + pteResult.ID);
                        var details = new Uri(DefaultSiteLink + "torrents/" + pteResult.ID);
                        var title = pteResult.Name;
                        if (pteResult.Language.Contains("pl") &&
                            ((BoolConfigurationItem)configData.GetDynamic("LanguageTitle")).Value)
                            title += " POLISH";
                        var release = new ReleaseInfo
                        {
                            Category = MapTrackerCatToNewznab(pteResult.Category),
                            Details = details,
                            Guid = link,
                            Link = link,
                            MinimumRatio = 1,
                            PublishDate = publishDate,
                            Seeders = pteResult.Seeders,
                            Peers = pteResult.Seeders + pteResult.Leechers,
                            Size = pteResult.Size,
                            Title = title,
                            Grabs = pteResult.Completed,
                            Description = string.Join("<br />\n", descriptions),
                            Imdb = imdb,
                            DownloadVolumeFactor = 1,
                            UploadVolumeFactor = 1
                        };
                        releases.Add(release);
                    }
            }
            catch (Exception ex)
            {
                OnParseError(resp, ex);
            }

            return releases;
        }

        public override async Task<byte[]> Download(Uri link)
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("API-Key", configData.Key.Value);
            var response = await client.GetAsync(link.ToString());
            response.EnsureSuccessStatusCode();
            return response.Content.ReadAsByteArrayAsync().Result;
        }

        public class PTEResultItem
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public long Size { get; set; }
            public string Category { get; set; }
            public string Added { get; set; }
            public int Comments { get; set; }
            public int Seeders { get; set; }
            public int Leechers { get; set; }
            public int Completed { get; set; }
            public string Imdb_ID { get; set; }
            public string Cdu_ID { get; set; }
            public string Steam_ID { get; set; }
            public string Subs { get; set; }
            public string Language { get; set; }
        }
    }
}
