using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Jackett.Indexers
{
    public class Strike : BaseIndexer, IIndexer
    {
        private string DownloadUrl { get { return baseUrl + "torrents/api/download/{0}.torrent"; } }
        private string SearchUrl { get { return baseUrl + "api/v2/torrents/search/?category=TV&phrase={0}"; } }
        private string baseUrl = null;

        public Strike(IIndexerManagerService i, Logger l, IWebClient wc)
            : base(name: "Strike",
                description: "Torrent search engine",
                link: "https://getstrike.net/",
                caps: TorznabCapsUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                client: wc,
                logger: l)
        {
        }

        public Task<ConfigurationData> GetConfigurationForSetup()
        {
            return Task.FromResult<ConfigurationData>(new ConfigurationDataUrl(SiteLink));
        }

        public Task ApplyConfiguration(JToken configJson)
        {
            var config = new ConfigurationDataUrl(SiteLink);
            config.LoadValuesFromJson(configJson);
            baseUrl = config.GetFormattedHostUrl();
            var configSaveData = new JObject();
            configSaveData["base_url"] = baseUrl;
            SaveConfig(configSaveData);
            IsConfigured = true;
            return Task.FromResult(0);
        }

        public override void LoadFromSavedConfiguration(JToken jsonConfig)
        {
            baseUrl = (string)jsonConfig["base_url"];
            IsConfigured = !string.IsNullOrEmpty(baseUrl);
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            var searchTerm = string.IsNullOrEmpty(query.SanitizedSearchTerm) ? "2015" : query.SanitizedSearchTerm;

            var searchString = searchTerm + " " + query.GetEpisodeSearchString();
            var episodeSearchUrl =string.Format(SearchUrl, HttpUtility.UrlEncode(searchString.Trim()));
            var results = await RequestStringWithCookies(episodeSearchUrl, string.Empty);
            try
            {
                var jResults = JObject.Parse(results.Content);
                foreach (JObject result in (JArray)jResults["torrents"])
                {
                    var release = new ReleaseInfo();

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;

                    release.Title = (string)result["torrent_title"];
                    release.Description = release.Title;
                    release.Seeders = (int)result["seeds"];
                    release.Peers = (int)result["leeches"] + release.Seeders;
                    release.Size = (long)result["size"];

                    // "Apr  2, 2015", "Apr 12, 2015" (note the spacing)
                    // some are unix timestamps, some are not.. :/
                    var dateString = string.Join(" ", ((string)result["upload_date"]).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                    float dateVal;
                    if (ParseUtil.TryCoerceFloat(dateString, out dateVal))
                        release.PublishDate = DateTimeUtil.UnixTimestampToDateTime(dateVal);
                    else
                        release.PublishDate = DateTime.ParseExact(dateString, "MMM d, yyyy", CultureInfo.InvariantCulture);

                    release.Guid = new Uri((string)result["page"]);
                    release.Comments = release.Guid;

                    release.InfoHash = (string)result["torrent_hash"];
                    release.MagnetUri = new Uri((string)result["magnet_uri"]);
                    release.Link = new Uri(string.Format(DownloadUrl, release.InfoHash));

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.Content, ex);
            }

            return releases;
        }

        public override Task<byte[]> Download(Uri link)
        {
            throw new NotImplementedException();
        }
    }
}
