using CsQuery;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
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
    public class BitHdtv : BaseIndexer, IIndexer
    {
        private readonly string LoginUrl = "";
        private readonly string SearchUrl = "";
        private readonly string DownloadUrl = "";

        CookieContainer cookies;
        HttpClientHandler handler;
        HttpClient client;

        public BitHdtv(IIndexerManagerService i, Logger l)
            : base(name: "BIT-HDTV",
                description: "Home of high definition invites",
                link: new Uri("https://www.bit-hdtv.com"),
                caps: TorznabCapsUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                logger: l)
        {
            LoginUrl = SiteLink + "takelogin.php";
            SearchUrl = SiteLink + "torrents.php?cat=0&search=";
            DownloadUrl = SiteLink + "download.php?/{0}/dl.torrent";

            cookies = new CookieContainer();
            handler = new HttpClientHandler
            {
                CookieContainer = cookies,
                AllowAutoRedirect = true,
                UseCookies = true,
            };
            client = new HttpClient(handler);
        }

        public Task<ConfigurationData> GetConfigurationForSetup()
        {
            var config = new ConfigurationDataBasicLogin();
            return Task.FromResult<ConfigurationData>(config);
        }

        public async Task ApplyConfiguration(JToken configJson)
        {
            var config = new ConfigurationDataBasicLogin();
            config.LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string> {
				{ "username", config.Username.Value },
				{ "password", config.Password.Value }
			};

            var content = new FormUrlEncodedContent(pairs);

            var response = await client.PostAsync(LoginUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!responseContent.Contains("logout.php"))
            {
                CQ dom = responseContent;
                var messageEl = dom["table.detail td.text"].Last();
                messageEl.Children("a").Remove();
                messageEl.Children("style").Remove();
                var errorMessage = messageEl.Text().Trim();
                throw new ExceptionWithConfigData(errorMessage, (ConfigurationData)config);
            }
            else
            {
                var configSaveData = new JObject();
                cookies.DumpToJson(SiteLink, configSaveData);
                SaveConfig(configSaveData);
                IsConfigured = true;
            }
        }

        public void LoadFromSavedConfiguration(JToken jsonConfig)
        {
            cookies.FillFromJson(SiteLink, jsonConfig, logger);
            IsConfigured = true;
        }

        public async Task<ReleaseInfo[]> PerformQuery(TorznabQuery query)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            var searchString = query.SanitizedSearchTerm + " " + query.GetEpisodeSearchString();
            var episodeSearchUrl = SearchUrl + HttpUtility.UrlEncode(searchString);
            var results = await client.GetStringAsync(episodeSearchUrl);
            try
            {
                CQ dom = results;
                dom["#needseed"].Remove();
                var rows = dom["table[width='750'] > tbody"].Children();
                foreach (var row in rows.Skip(1))
                {

                    var release = new ReleaseInfo();

                    var qRow = row.Cq();
                    var qLink = qRow.Children().ElementAt(2).Cq().Children("a").First();

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;
                    release.Title = qLink.Attr("title");
                    release.Description = release.Title;
                    release.Guid = new Uri(SiteLink + qLink.Attr("href"));
                    release.Comments = release.Guid;
                    release.Link = new Uri(string.Format(DownloadUrl, qLink.Attr("href").Split('=')[1]));

                    var dateString = qRow.Children().ElementAt(5).Cq().Text().Trim();
                    var pubDate = DateTime.ParseExact(dateString, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    release.PublishDate = DateTime.SpecifyKind(pubDate, DateTimeKind.Local);

                    var sizeCol = qRow.Children().ElementAt(6);
                    var sizeVal = sizeCol.ChildNodes[0].NodeValue;
                    var sizeUnit = sizeCol.ChildNodes[2].NodeValue;
                    release.Size = ReleaseInfo.GetBytes(sizeUnit, ParseUtil.CoerceFloat(sizeVal));

                    release.Seeders = ParseUtil.CoerceInt(qRow.Children().ElementAt(8).Cq().Text().Trim());
                    release.Peers = ParseUtil.CoerceInt(qRow.Children().ElementAt(9).Cq().Text().Trim()) + release.Seeders;

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results, ex);
            }

            return releases.ToArray();
        }

        public Task<byte[]> Download(Uri link)
        {
            return client.GetByteArrayAsync(link);
        }
    }
}
