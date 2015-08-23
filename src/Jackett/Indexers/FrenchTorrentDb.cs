using CsQuery;
using Jackett.Models;
using Jackett.Models.IndexerConfig;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;

namespace Jackett.Indexers
{
    class FrenchTorrentDb : BaseIndexer, IIndexer
    {
        private string MainUrl { get { return SiteLink + "?section=INDEX"; } }
        private string SearchUrl { get { return SiteLink + "?section=TORRENTS&exact=1&name={0}&submit=GO"; } }

        new ConfigurationDataCookie configData
        {
            get { return (ConfigurationDataCookie)base.configData; }
            set { base.configData = value; }
        }

        public FrenchTorrentDb(IIndexerManagerService i, Logger l, IWebClient c, IProtectionService ps)
            : base(name: "FrenchTorrentDb",
                description: "One the biggest French Torrent Tracker",
                link: "http://www.frenchtorrentdb.com/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                client: c,
                logger: l,
                p: ps,
                configData: new ConfigurationDataCookie())
        {
        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);
            var response = await webclient.GetString(new Utils.Clients.WebRequest()
            {
                Url = MainUrl,
                Type = RequestType.GET,
                Cookies = configData.Cookie.Value
            });

            await ConfigureIfOK(configData.Cookie.Value, response.Content.Contains("/?section=LOGOUT"), () =>
            {
                throw new ExceptionWithConfigData("Failed to login", configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var episodeSearchUrl = string.Format(SearchUrl, HttpUtility.UrlEncode(query.GetQueryString()));
            var response = await RequestStringWithCookiesAndRetry(episodeSearchUrl);
            try
            {
                CQ dom = response.Cookies;
                var rows = dom[".results_index ul"];
                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();
                    CQ qRow = row.Cq();
                    CQ qLink = qRow.Find("li.torrents_name > .torrents_name_link").First();
                    CQ qDlLink = qRow.Find("li.torrents_download  > a").First();

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;
                    release.Title = qLink.Text().Trim();
                    release.Description = release.Title;
                    release.Comments = new Uri(SiteLink + "/" + qLink.Attr("href").TrimStart('/'));
                    release.Guid = release.Comments;
                    release.Link = new Uri(SiteLink + "/" + qDlLink.Attr("href").TrimStart('/'));
                    release.PublishDate = DateTime.Now;
                    release.Seeders = ParseUtil.CoerceInt(qRow.Find("li.torrents_seeders").Text());
                    release.Peers = ParseUtil.CoerceInt(qRow.Find("li.torrents_leechers").Text()) + release.Seeders;
                    var sizeParts = qRow.Find("li.torrents_size").Text();
                    release.Size = ReleaseInfo.GetBytes(sizeParts);

                    releases.Add(release);
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
