using CsQuery;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
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
using Jackett.Models.IndexerConfig;

namespace Jackett.Indexers
{
    public class MoreThanTV : BaseIndexer, IIndexer
    {
        private string LoginUrl { get { return SiteLink + "login.php"; } }
        private string SearchUrl { get { return SiteLink + "ajax.php?action=browse&searchstr="; } }
        private string DownloadUrl { get { return SiteLink + "torrents.php?action=download&id="; } }
        private string GuidUrl { get { return SiteLink + "torrents.php?torrentid="; } }

        new ConfigurationDataBasicLogin configData
        {
            get { return (ConfigurationDataBasicLogin)base.configData; }
            set { base.configData = value; }
        }

        public MoreThanTV(IIndexerManagerService i, IWebClient c, Logger l, IProtectionService ps)
            : base(name: "MoreThanTV",
                description: "ROMANIAN Private Torrent Tracker for TV / MOVIES, and the internal tracker for the release group DRACULA.",
                link: "https://www.morethan.tv/",
                caps: new TorznabCapabilities(TorznabCatType.TV,
                                              TorznabCatType.Movies),
                manager: i,
                client: c,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLogin())
        {
        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "login", "Log in" },
                { "keeplogged", "1" }
            };

            var preRequest = await RequestStringWithCookiesAndRetry(LoginUrl, string.Empty);

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, preRequest.Cookies, true, SearchUrl, SiteLink);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("status\":\"success\""), () =>
            {
                CQ dom = result.Content;
                dom["#loginform > table"].Remove();
                var errorMessage = dom["#loginform"].Text().Trim().Replace("\n\t", " ");
                throw new ExceptionWithConfigData(errorMessage, configData);
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        private void FillReleaseInfoFromJson(ReleaseInfo release, JObject r)
        {
            var id = r["torrentId"];
            release.Size = (long)r["size"];
            release.Seeders = (int)r["seeders"];
            release.Peers = (int)r["leechers"] + release.Seeders;
            release.Guid = new Uri(GuidUrl + id);
            release.Comments = release.Guid;

            if ((string)r["category"] == "TV")
            {
                release.Category = TorznabCatType.TV.ID;
            }
            else if ((string)r["category"] == "Movies")
            {
                release.Category = TorznabCatType.Movies.ID;
            }

            release.Link = new Uri(DownloadUrl + id);
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var episodeSearchUrl = SearchUrl + HttpUtility.UrlEncode(query.GetQueryString());
            WebClientStringResult response = null;

            response = await RequestStringWithCookiesAndRetry(episodeSearchUrl);

            try
            {
                string decodedResponse = WebUtility.HtmlDecode(response.Content);
                var json = JObject.Parse(decodedResponse);
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
                OnParseError(response.Content, ex);
            }

            return releases;
        }
    }
}
