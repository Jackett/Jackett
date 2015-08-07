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
using Jackett.Models;
using Jackett.Utils;
using NLog;
using Jackett.Services;
using Jackett.Utils.Clients;
using Jackett.Models.IndexerConfig;

namespace Jackett.Indexers
{
    public class AlphaRatio : BaseIndexer, IIndexer
    {
        private string LoginUrl { get { return SiteLink + "login.php"; } }
        private string SearchUrl { get { return SiteLink + "ajax.php?action=browse&searchstr="; } }
        private string DownloadUrl { get { return SiteLink + "torrents.php?action=download&id="; } }
        private string GuidUrl { get { return SiteLink + "torrents.php?torrentid="; } }

        public AlphaRatio(IIndexerManagerService i, IWebClient w, Logger l, IProtectionService ps)
            : base(name: "AlphaRatio",
                description: "Legendary",
                link: "https://alpharatio.cc/",
                caps: TorznabCapsUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                client: w,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLogin())
        {
            webclient = w;
        }

        public async Task ApplyConfiguration(JToken configJson)
        {
            var incomingConfig = new ConfigurationDataBasicLogin();
            incomingConfig.LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string> {
                { "username", incomingConfig.Username.Value },
                { "password", incomingConfig.Password.Value },
                { "login", "Login" },
                { "keeplogged", "1" }
            };

            // Do the login
            var response = await RequestLoginAndFollowRedirect(LoginUrl, pairs, string.Empty, true, SiteLink);
            await ConfigureIfOK(response.Cookies, response.Content != null && response.Content.Contains("logout.php?"), () =>
               {
                   CQ dom = response.Content;
                   dom["#loginform > table"].Remove();
                   var errorMessage = dom["#loginform"].Text().Trim().Replace("\n\t", " ");
                   throw new ExceptionWithConfigData(errorMessage, (ConfigurationData)incomingConfig);
               });
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

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.SanitizedSearchTerm + " " + query.GetEpisodeSearchString();
            var episodeSearchUrl = SearchUrl + HttpUtility.UrlEncode(searchString);
            var response = await RequestStringWithCookiesAndRetry(episodeSearchUrl);

            try
            {
                var json = JObject.Parse(response.Content);
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
                OnParseError(response.Content, ex);
            }

            return releases;
        }

        static DateTime UnixTimestampToDateTime(double unixTime)
        {
            DateTime unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            long unixTimeStampInTicks = (long)(unixTime * TimeSpan.TicksPerSecond);
            return new DateTime(unixStart.Ticks + unixTimeStampInTicks);
        }
    }
}
