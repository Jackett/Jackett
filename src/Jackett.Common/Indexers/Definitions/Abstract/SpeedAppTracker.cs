using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers.Definitions.Abstract
{
    [ExcludeFromCodeCoverage]
    public abstract class SpeedAppTracker : IndexerBase
    {
        public override bool SupportsPagination => true;

        protected virtual int MinimumSeedTime => 172800; // 48h

        private readonly Dictionary<string, string> _apiHeaders = new Dictionary<string, string>
        {
            {"Accept", "application/json"},
            {"Content-Type", "application/json"}
        };
        // API DOC: https://speedapp.io/api/doc
        private string LoginUrl => SiteLink + "api/login";
        private string SearchUrl => SiteLink + "api/torrent";
        private string _token;

        private new ConfigurationDataSpeedAppTracker configData => (ConfigurationDataSpeedAppTracker)base.configData;

        protected SpeedAppTracker(IIndexerConfigurationService configService, WebClient client, Logger logger, IProtectionService p, ICacheService cs)
            : base(configService: configService,
                   client: client,
                   logger: logger,
                   p: p,
                   cacheService: cs,
                   configData: new ConfigurationDataSpeedAppTracker())
        {
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
            if (configData.Email.Value == null || configData.Password.Value == null)
            {
                throw new Exception("Please, check the indexer configuration.");
            }

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
            {
                throw new Exception(json.Value<string>("message"));
            }
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var qc = new List<KeyValuePair<string, string>> // NameValueCollection don't support cat[]=19&cat[]=6
            {
                { "itemsPerPage", "100" },
                { "includingDead", "1" },
                { "sort", "torrent.createdAt" },
                { "direction", "desc" }
            };

            if (query.Limit > 0 && query.Offset > 0)
            {
                var page = query.Offset / query.Limit + 1;
                qc.Add("page", page.ToString());
            }

            foreach (var cat in MapTorznabCapsToTrackers(query))
            {
                qc.Add("categories[]", cat);
            }

            if (query.IsImdbQuery)
            {
                qc.Add("imdbId", query.ImdbID);
            }
            else
            {
                qc.Add("search", query.GetQueryString());
            }

            if (string.IsNullOrWhiteSpace(_token)) // fist time login
            {
                await RenewalTokenAsync();
            }

            var searchUrl = SearchUrl + "?" + qc.GetQueryString();
            var response = await RequestWithCookiesAsync(searchUrl, headers: GetSearchHeaders());

            if (response.Status == HttpStatusCode.Unauthorized)
            {
                await RenewalTokenAsync(); // re-login
                response = await RequestWithCookiesAsync(searchUrl, headers: GetSearchHeaders());
            }
            else if (response.Status != HttpStatusCode.OK)
            {
                throw new Exception($"Unknown error in search: {response.ContentString}");
            }

            try
            {
                var rows = JArray.Parse(response.ContentString);

                foreach (var row in rows)
                {
                    var dlVolumeFactor = row.Value<double>("download_volume_factor");

                    // skip non-freeleech results when freeleech only is set
                    if (configData.FreeleechOnly.Value && dlVolumeFactor != 0)
                    {
                        continue;
                    }

                    var id = row.Value<string>("id");
                    var link = new Uri($"{SiteLink}api/torrent/{id}/download");
                    var urlStr = row.Value<string>("url");
                    var details = new Uri(urlStr);
                    var publishDate = DateTime.Parse(row.Value<string>("created_at"), CultureInfo.InvariantCulture);
                    var cat = row.Value<JToken>("category").Value<string>("id");

                    var description = "";
                    var genres = row.Value<string>("short_description");
                    char[] delimiters = { ',', ' ', '/', ')', '(', '.', ';', '[', ']', '"', '|', ':' };
                    var genresSplit = genres.Split(delimiters, System.StringSplitOptions.RemoveEmptyEntries);
                    var genresList = genresSplit.ToList();
                    genres = string.Join(", ", genresList);
                    if (!string.IsNullOrEmpty(genres))
                    {
                        description = genres;
                    }

                    var posterStr = row.Value<string>("poster");
                    var poster = Uri.TryCreate(posterStr, UriKind.Absolute, out var posterUri) ? posterUri : null;

                    var title = CleanTitle(row.Value<string>("name"));

                    if (!query.IsImdbQuery && !query.MatchQueryStringAND(title))
                    {
                        continue;
                    }

                    var release = new ReleaseInfo
                    {
                        Title = title,
                        Link = link,
                        Details = details,
                        Guid = details,
                        Category = MapTrackerCatToNewznab(cat),
                        PublishDate = publishDate,
                        Description = description,
                        Poster = poster,
                        Size = row.Value<long>("size"),
                        Grabs = row.Value<long>("times_completed"),
                        Seeders = row.Value<int>("seeders"),
                        Peers = row.Value<int>("leechers") + row.Value<int>("seeders"),
                        DownloadVolumeFactor = dlVolumeFactor,
                        UploadVolumeFactor = row.Value<double>("upload_volume_factor"),
                        MinimumRatio = 1,
                        MinimumSeedTime = MinimumSeedTime
                    };

                    release.Genres ??= new List<string>();
                    release.Genres = release.Genres.Union(genres.Split(',')).ToList();

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
            {
                throw new Exception($"Unknown error in download: {response.ContentBytes}");
            }

            return response.ContentBytes;
        }

        private Dictionary<string, string> GetSearchHeaders() => new Dictionary<string, string>
        {
            {"Authorization", $"Bearer {_token}"}
        };

        private static string CleanTitle(string title)
        {
            title = Regex.Replace(title, @"\[REQUEST(ED)?\]", string.Empty, RegexOptions.Compiled | RegexOptions.IgnoreCase);

            return title.Trim(' ', '.');
        }
    }
}
