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

namespace Jackett.Indexers
{
    public class MoreThanTV : IndexerInterface
    {
        public string DisplayName
        {
            get { return "MoreThanTV"; }
        }

        public string DisplayDescription
        {
            get { return "ROMANIAN Private Torrent Tracker for TV / MOVIES, and the internal tracker for the release group DRACULA"; }
        }

        public Uri SiteLink
        {
            get { return new Uri(BaseUrl); }
        }


        public event Action<IndexerInterface, JToken> OnSaveConfigurationRequested;

        public bool IsConfigured { get; private set; }

        static string BaseUrl = "http://www.morethan.tv";

        static string LoginUrl = BaseUrl + "/login.php";

        static string SearchUrl = BaseUrl + "/ajax.php?action=browse&searchstr=";

        static string DownloadUrl = BaseUrl + "/torrents.php?action=download&id=";

        static string GuidUrl = BaseUrl + "/torrents.php?torrentid=";

        CookieContainer cookies;
        HttpClientHandler handler;
        HttpClient client;

        public MoreThanTV()
        {
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

        public async Task ApplyConfiguration(Newtonsoft.Json.Linq.JToken configJson)
        {

            var config = new ConfigurationDataBasicLogin();
            config.LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string>
                {
                    { "username", config.Username.Value},
                    { "password", config.Password.Value},
                    { "login", "Log in" },
                    { "keeplogged", "1" }
                };

            var content = new FormUrlEncodedContent(pairs);

            var response = await client.PostAsync(LoginUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!responseContent.Contains("logout.php?"))
            {
                CQ dom = responseContent;
                dom["#loginform > table"].Remove();
                var errorMessage = dom["#loginform"].Text().Trim().Replace("\n\t", " ");
                throw new ExceptionWithConfigData(errorMessage, (ConfigurationData)config);
            }
            else
            {
                var configSaveData = new JObject();
                configSaveData["cookies"] = new JArray((
                    from cookie in cookies.GetCookies(new Uri(BaseUrl)).Cast<Cookie>()
                    select cookie.Name + ":" + cookie.Value
                ).ToArray());

                if (OnSaveConfigurationRequested != null)
                    OnSaveConfigurationRequested(this, configSaveData);

                IsConfigured = true;
            }
        }

        public void LoadFromSavedConfiguration(Newtonsoft.Json.Linq.JToken jsonConfig)
        {
            cookies.FillFromJson(new Uri(BaseUrl), (JArray)jsonConfig["cookies"]);
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

        public async Task<ReleaseInfo[]> PerformQuery(TorznabQuery query)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            foreach (var title in query.ShowTitles ?? new string[] { string.Empty })
            {

                var searchString = title + " " + query.GetEpisodeSearchString();
                var episodeSearchUrl = SearchUrl + HttpUtility.UrlEncode(searchString);
                var results = await client.GetStringAsync(episodeSearchUrl);
                var json = JObject.Parse(results);
                foreach (JObject r in json["response"]["results"])
                {

                    var pubDate = UnixTimestampToDateTime(double.Parse((string)r["groupTime"]));
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

            return releases.ToArray();
        }

        static DateTime UnixTimestampToDateTime(double unixTime)
        {
            DateTime unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            long unixTimeStampInTicks = (long)(unixTime * TimeSpan.TicksPerSecond);
            return new DateTime(unixStart.Ticks + unixTimeStampInTicks);
        }

        public Task<byte[]> Download(Uri link)
        {
            return client.GetByteArrayAsync(link);
        }
    }
}
