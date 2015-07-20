using CsQuery;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Jackett.Indexers
{
    public class MoreThanTV : BaseIndexer, IIndexer
    {
        private readonly string LoginUrl = "";
        private readonly string SearchUrl = "";
        private readonly string DownloadUrl = "";
        private readonly string GuidUrl = "";

        CookieContainer cookies;
        HttpClientHandler handler;
        HttpClient client;

        string cookieHeader;
        int retries = 3;

        public MoreThanTV(IIndexerManagerService i, Logger l)
            : base(name: "MoreThanTV",
                description: "ROMANIAN Private Torrent Tracker for TV / MOVIES, and the internal tracker for the release group DRACULA.",
                link: new Uri("https://www.morethan.tv"),
                caps: TorznabCapsUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                logger: l)
        {
            LoginUrl = SiteLink + "/login.php";
            SearchUrl = SiteLink + "/ajax.php?action=browse&searchstr=";
            DownloadUrl = SiteLink + "/torrents.php?action=download&id=";
            GuidUrl = SiteLink + "/torrents.php?torrentid=";

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
				{ "password", config.Password.Value },
				{ "login", "Log in" },
				{ "keeplogged", "1" }
			};

            var content = new FormUrlEncodedContent(pairs);

            string responseContent;

            var configSaveData = new JObject();

            if (Engine.IsWindows)
            {
                // If Windows use .net http
                var response = await client.PostAsync(LoginUrl, content);
                responseContent = await response.Content.ReadAsStringAsync();
                cookies.DumpToJson(SiteLink, configSaveData);

            }
            else
            {
                // If UNIX system use curl
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
                SaveConfig(configSaveData);
                IsConfigured = true;
            }
        }

        public void LoadFromSavedConfiguration(JToken jsonConfig)
        {
            cookies.FillFromJson(SiteLink, jsonConfig, logger);
            cookieHeader = cookies.GetCookieHeader(SiteLink);
            IsConfigured = true;
        }

        private void FillReleaseInfoFromJson(ReleaseInfo release, JObject r)
        {
            var id = r["torrentId"];
            release.Size = (long)r["size"];
            release.Seeders = (int)r["seeders"];
            release.Peers = (int)r["leechers"] + release.Seeders;
            release.Guid = new Uri(GuidUrl + id);
            release.Comments = release.Guid;
            release.Link = new Uri(DownloadUrl + id);
        }

        public async Task<ReleaseInfo[]> PerformQuery(TorznabQuery query)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            var searchString = query.SanitizedSearchTerm + " " + query.GetEpisodeSearchString();
            var episodeSearchUrl = SearchUrl + HttpUtility.UrlEncode(searchString);

            string results;
            if (Engine.IsWindows)
            {
                results = await client.GetStringAsync(episodeSearchUrl, retries);
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
                    {
                        pubDate = DateTimeUtil.UnixTimestampToDateTime(dateNum);
                        pubDate = DateTime.SpecifyKind(pubDate, DateTimeKind.Utc).ToLocalTime();
                    }

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
                OnParseError(results, ex);
            }

            return releases.ToArray();
        }

        public async Task<byte[]> Download(Uri link)
        {
            if (Engine.IsWindows)
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
