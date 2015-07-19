using CsQuery;
using Jackett.Models;
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
    public class BitHdtv : IIndexer
    {
        public event Action<IIndexer, string, Exception> OnResultParsingError;

        public string DisplayName
        {
            get { return "BIT-HDTV"; }
        }

        public string DisplayDescription
        {
            get { return "Home of high definition invites"; }
        }

        public Uri SiteLink
        {
            get { return new Uri(BaseUrl); }
        }

        static string BaseUrl = "https://www.bit-hdtv.com";
        static string LoginUrl = BaseUrl + "/takelogin.php";
        static string SearchUrl = BaseUrl + "/torrents.php?cat=0&search=";
        static string DownloadUrl = BaseUrl + "/download.php?/{0}/dl.torrent";

        CookieContainer cookies;
        HttpClientHandler handler;
        HttpClient client;
        Logger loggger;

        public BitHdtv(Logger l)
        {
            loggger = l;
            IsConfigured = false;
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

                if (OnSaveConfigurationRequested != null)
                    OnSaveConfigurationRequested(this, configSaveData);

                IsConfigured = true;
            }
        }

        public event Action<IIndexer, JToken> OnSaveConfigurationRequested;

        public bool IsConfigured { get; private set; }

        public void LoadFromSavedConfiguration(JToken jsonConfig)
        {
            cookies.FillFromJson(new Uri(BaseUrl), jsonConfig, loggger);
            IsConfigured = true;
        }

        public async Task<ReleaseInfo[]> PerformQuery(TorznabQuery query)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            foreach (var title in query.ShowTitles ?? new string[] { string.Empty })
            {
                var searchString = title + " " + query.GetEpisodeSearchString();
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
                        release.Guid = new Uri(BaseUrl + qLink.Attr("href"));
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
                    OnResultParsingError(this, results, ex);
                    throw ex;
                }
            }

            return releases.ToArray();
        }

        public Task<byte[]> Download(Uri link)
        {
            return client.GetByteArrayAsync(link);
        }


    }
}
