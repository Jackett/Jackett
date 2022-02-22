using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers.Abstract
{
    [ExcludeFromCodeCoverage]
    public abstract class SpeedAppTracker : BaseWebIndexer
    {
        protected virtual string ItemsPerPage => "100";
        protected virtual bool UseP2PReleaseName => false;
        private readonly Dictionary<string, string> _apiHeaders = new Dictionary<string, string>
        {
            {"Accept", "application/json"},
            {"Content-Type", "application/json"}
        };
        // API DOC: https://speedapp.io/api/doc
        private string LoginUrl => SiteLink + "api/login";
        private string SearchUrl => SiteLink + "api/torrent";
        private string _token;

        private new ConfigurationDataBasicLoginWithEmail configData => (ConfigurationDataBasicLoginWithEmail)base.configData;

        protected SpeedAppTracker(string link, string id, string name, string description,
            IIndexerConfigurationService configService, WebClient client, Logger logger,
            IProtectionService p, ICacheService cs, TorznabCapabilities caps)
            : base(id: id,
                name: name,
                description: description,
                link: link,
                caps: caps,
                configService: configService,
                client: client,
                logger: logger,
                p: p,
                cacheService: cs,
                configData: new ConfigurationDataBasicLoginWithEmail())
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
                throw new Exception("Please, check the indexer configuration.");
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
                throw new Exception(json.Value<string>("message"));
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            //var categoryMapping = MapTorznabCapsToTrackers(query).Distinct().ToList();
            var qc = new List<KeyValuePair<string, string>> // NameValueCollection don't support cat[]=19&cat[]=6
            {
                {"itemsPerPage", ItemsPerPage},
                {"sort", "torrent.createdAt"},
                {"direction", "desc"}
            };

            foreach (var cat in MapTorznabCapsToTrackers(query))
                qc.Add("categories[]", cat);

            if (query.IsImdbQuery)
                qc.Add("imdbId", query.ImdbID);
            else
                qc.Add("search", query.GetQueryString());

            if (string.IsNullOrWhiteSpace(_token)) // fist time login
                await RenewalTokenAsync();

            var searchUrl = SearchUrl + "?" + qc.GetQueryString();
            var response = await RequestWithCookiesAsync(searchUrl, headers: GetSearchHeaders());
            if (response.Status == HttpStatusCode.Unauthorized)
            {
                await RenewalTokenAsync(); // re-login
                response = await RequestWithCookiesAsync(searchUrl, headers: GetSearchHeaders());
            }
            else if (response.Status != HttpStatusCode.OK)
                throw new Exception($"Unknown error in search: {response.ContentString}");

            try
            {
                var rows = JArray.Parse(response.ContentString);
                foreach (var row in rows)
                {
                    var id = row.Value<string>("id");
                    var link = new Uri($"{SiteLink}api/torrent/{id}/download");
                    var urlStr = row.Value<string>("url");
                    var details = new Uri(urlStr);
                    var publishDate = DateTime.Parse(row.Value<string>("created_at"), CultureInfo.InvariantCulture);
                    var cat = row.Value<JToken>("category").Value<string>("id");

                    // "description" field in API has too much HTML code
                    var description = row.Value<string>("short_description");

                    var posterStr = row.Value<string>("poster");
                    var poster = Uri.TryCreate(posterStr, UriKind.Absolute, out var posterUri) ? posterUri : null;

                    var dlVolumeFactor = row.Value<double>("download_volume_factor");
                    var ulVolumeFactor = row.Value<double>("upload_volume_factor");

                    var title = row.Value<string>("name");
                    // fix for #10883
                    if (UseP2PReleaseName && !string.IsNullOrWhiteSpace(row.Value<string>("p2p_release_name")))
                        title = row.Value<string>("p2p_release_name");

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
                        UploadVolumeFactor = ulVolumeFactor,
                        MinimumRatio = 1,
                        MinimumSeedTime = 172800 // 48 hours
                    };

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
                throw new Exception($"Unknown error in download: {response.ContentBytes}");
            return response.ContentBytes;
        }

        private Dictionary<string, string> GetSearchHeaders() => new Dictionary<string, string>
        {
            {"Authorization", $"Bearer {_token}"}
        };
    }
}
