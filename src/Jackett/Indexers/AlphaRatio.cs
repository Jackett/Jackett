using CsQuery;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Net.Http.Headers;
using Jackett.Utils;
using Jackett.Models;
using NLog;
using Jackett.Services;

namespace Jackett.Indexers
{
    public class AlphaRatio : BaseIndexer
    {
        private string LoginUrl;
        private string SearchUrl;
        private string DownloadUrl;
        private string GuidUrl;

        CookieContainer cookies;
        HttpClientHandler handler;
        HttpClient client;
        Logger logger;
        private IIndexerManagerService managementService;

        string cookieHeader;

        public AlphaRatio(Logger l, IIndexerManagerService m): 
            base(name: "AlphaRatio", 
                description: "Legendary", 
                link: new Uri("https://alpharatio.cc"), 
                logger:l)
        {
            logger = l;
            managementService = m;

        LoginUrl = SiteLink.ToString() + "/login.php";
        SearchUrl = SiteLink.ToString() + "/ajax.php?action=browse&searchstr=";
        DownloadUrl = SiteLink.ToString() + "/torrents.php?action=download&id=";
        GuidUrl = SiteLink.ToString() + "/torrents.php?torrentid=";

        cookies = new CookieContainer();
            handler = new HttpClientHandler
            {
                CookieContainer = cookies,
                AllowAutoRedirect = true,
                UseCookies = true,

            };
            client = new HttpClient(handler);
        }

        public override Task<ConfigurationData> GetConfigurationForSetup()
        {
            var config = new ConfigurationDataBasicLogin();
            return Task.FromResult<ConfigurationData>(config);
        }

        public override async Task ApplyConfiguration(JToken configJson)
        {
            var configSaveData = new JObject();
            managementService.SaveConfig(this, configSaveData);

            var config = new ConfigurationDataBasicLogin();
            config.LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string> {
				{ "username", config.Username.Value },
				{ "password", @config.Password.Value },
				{ "login", "Login" },
			    { "keeplogged", "1" }
			};

            var content = new FormUrlEncodedContent(pairs);
            var message = CreateHttpRequest(new Uri(LoginUrl));
            message.Content = content;

            //message.Headers.Referrer = new Uri(LoginUrl);
            string responseContent;

            configSaveData = new JObject();

            if (WebServer.IsWindows)
            {
                // If Windows use .net http
                var response = await client.SendAsync(message);
                responseContent = await response.Content.ReadAsStringAsync();
                cookies.DumpToJson(SiteLink, configSaveData);
            }
            else
            {
                // If UNIX system use curl, probably broken due to missing chromeUseragent record for CURL...cannot test
                var response = await CurlHelper.PostAsync(LoginUrl, pairs);
                responseContent = Encoding.UTF8.GetString(response.Content);
                cookieHeader = response.CookieHeader;
                configSaveData["cookie_header"] = cookieHeader;
            }

            if (!responseContent.Contains("logout.php?"))
            {
                CQ dom = responseContent;
                dom["#loginform > table"].Remove();
                var errorMessage = dom["#loginform"].Text().Trim().Replace("\n\t", " ");
                throw new ExceptionWithConfigData(errorMessage, (ConfigurationData)config);

            }
            else
            {
                managementService.SaveConfig(this, configSaveData);
                IsConfigured = true;
            }
        }

        HttpRequestMessage CreateHttpRequest(Uri uri)
        {
            var message = new HttpRequestMessage();
            message.Method = HttpMethod.Post;
            message.RequestUri = uri;
            message.Headers.UserAgent.ParseAdd(BrowserUtil.ChromeUserAgent);
            return message;
        }

        public override void LoadFromSavedConfiguration(JToken jsonConfig)
        {
            cookies.FillFromJson(SiteLink, jsonConfig, logger);
            cookieHeader = cookies.GetCookieHeader(SiteLink);
            IsConfigured = true;
        }

        void FillReleaseInfoFromJson(ReleaseInfo release, JObject r)
        {
            var id = r["torrentId"];
            release.Size = (long)r["size"];
            release.Seeders = (int)r["seeders"];
            release.Peers = (int)r["leechers"] + release.Seeders;
            release.Guid = new Uri(GuidUrl + id);
            release.Comments = release.Guid;
            release.Link = new Uri(DownloadUrl + id);
        }

        public override  async Task<ReleaseInfo[]> PerformQuery(TorznabQuery query)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            foreach (var title in query.ShowTitles ?? new string[] { string.Empty })
            {

                var searchString = title + " " + query.GetEpisodeSearchString();
                var episodeSearchUrl = SearchUrl + HttpUtility.UrlEncode(searchString);

                string results;
                if (WebServer.IsWindows)
                {
                    var request = CreateHttpRequest(new Uri(episodeSearchUrl));
                    request.Method = HttpMethod.Get;
                    var response = await client.SendAsync(request);
                    results = await response.Content.ReadAsStringAsync();
                }
                else
                {
                    var response = await CurlHelper.GetAsync(episodeSearchUrl, cookieHeader);
                    results = Encoding.UTF8.GetString(response.Content);
                }
                try
                {

                    var json = JObject.Parse(results);
                    foreach (JObject r in json["response"]["results"])
                    {
                        DateTime pubDate = DateTime.MinValue;
                        double dateNum;
                        if (double.TryParse((string)r["groupTime"], out dateNum))
                            pubDate = UnixTimestampToDateTime(dateNum);

                        var groupName = (string)r["groupName"];

                        if (r["torrents"] is JArray)
                        {
                            foreach (JObject t in r["torrents"])
                            {
                                var release = new ReleaseInfo();
                                release.PublishDate = pubDate;
                                release.Title = groupName;
                                release.Description = groupName;
                                FillReleaseInfoFromJson(release, t);
                                releases.Add(release);
                            }
                        }
                        else
                        {
                            var release = new ReleaseInfo();
                            release.PublishDate = pubDate;
                            release.Title = groupName;
                            release.Description = groupName;
                            FillReleaseInfoFromJson(release, r);
                            releases.Add(release);
                        }

                    }
                }
                catch (Exception ex)
                {
                    LogParseError(results, ex);
                    throw ex;
                }
            }

            return releases.ToArray();
        }

        static DateTime UnixTimestampToDateTime(double unixTime)
        {
            DateTime unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            long unixTimeStampInTicks = (long)(unixTime * TimeSpan.TicksPerSecond);
            return new DateTime(unixStart.Ticks + unixTimeStampInTicks);
        }

        public override async Task<byte[]> Download(Uri link)
        {
            if (WebServer.IsWindows)
            {
                return await client.GetByteArrayAsync(link);
            }
            else
            {
                var response = await CurlHelper.GetAsync(link.ToString(), cookieHeader);
                return response.Content;
            }

        }

    }
}
