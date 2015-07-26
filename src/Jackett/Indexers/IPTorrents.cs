using CsQuery;
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
    public class IPTorrents : BaseIndexer, IIndexer
    {
        private readonly string SearchUrl = "";
        private string cookieHeader = "";

        private IWebClient webclient;

        public IPTorrents(IIndexerManagerService i, IWebClient wc, Logger l)
            : base(name: "IPTorrents",
                description: "Always a step ahead.",
                link: new Uri("https://iptorrents.com/"),
                caps: TorznabCapsUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                logger: l)
        {
            SearchUrl = SiteLink + "t?q=";
            webclient = wc;
        }

        public Task<ConfigurationData> GetConfigurationForSetup()
        {
            var config = new ConfigurationDataBasicLogin();
            return Task.FromResult<ConfigurationData>((ConfigurationData)config);
        }

        public async Task ApplyConfiguration(JToken configJson)
        {

            var config = new ConfigurationDataBasicLogin();
            config.LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string> {
				{ "username", config.Username.Value },
				{ "password", config.Password.Value }
			};

            var response = await webclient.GetString(new Utils.Clients.WebRequest()
            {
                Url = SiteLink.ToString(),
                PostData = pairs,
                Referer = SiteLink.ToString(),
                Type = RequestType.POST
            });

            cookieHeader = response.Cookies;
            if (response.Status == HttpStatusCode.Found)
            {
                response = await webclient.GetString(new Utils.Clients.WebRequest()
                {
                    Url = SearchUrl,
                    Referer = SiteLink.ToString(),
                    Cookies = response.Cookies
                });
            }

            var responseContent = response.Content;

            if (!responseContent.Contains("/my.php"))
            {
                CQ dom = responseContent;
                var messageEl = dom["body > div"].First();
                var errorMessage = messageEl.Text().Trim();
                throw new ExceptionWithConfigData(errorMessage, (ConfigurationData)config);
            }
            else
            {
                var configSaveData = new JObject();
                configSaveData["cookie_header"] = cookieHeader;
                SaveConfig(configSaveData);
                IsConfigured = true;
            }
        }

        public void LoadFromSavedConfiguration(JToken jsonConfig)
        {
            cookieHeader = (string)jsonConfig["cookie_header"];
            IsConfigured = true;
        }

        public async Task<ReleaseInfo[]> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.SanitizedSearchTerm + " " + query.GetEpisodeSearchString();
            var episodeSearchUrl = SearchUrl + HttpUtility.UrlEncode(searchString);

            WebClientStringResult response = null;

            // Their web server is fairly flakey - try up to three times.
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    response = await webclient.GetString(new Utils.Clients.WebRequest()
                    {
                        Url = episodeSearchUrl,
                        Referer = SiteLink.ToString(),
                        Cookies = cookieHeader
                    });

                    break;
                }
                catch (Exception e)
                {
                    logger.Error("On attempt " + (i + 1) + " checking for results from IPTorrents: " + e.Message);
                }
            }

            var results = response.Content;
            try
            {
                CQ dom = results;

                var rows = dom["table.torrents > tbody > tr"];
                foreach (var row in rows.Skip(1))
                {
                    var release = new ReleaseInfo();
                    var qRow = row.Cq();
                    var qTitleLink = qRow.Find("a.t_title").First();
                    release.Title = qTitleLink.Text().Trim();

                    // If we get a no results found page we still get a table but without any data
                    if (string.IsNullOrWhiteSpace(release.Title))
                    {
                        break;
                    }

                    release.Description = release.Title;
                    release.Guid = new Uri(SiteLink + qTitleLink.Attr("href"));
                    release.Comments = release.Guid;

                    var descString = qRow.Find(".t_ctime").Text();
                    var dateString = descString.Split('|').Last().Trim();
                    dateString = dateString.Split(new string[] { " by " }, StringSplitOptions.None)[0];
                    release.PublishDate = DateTimeUtil.FromTimeAgo(dateString);

                    var qLink = row.ChildElements.ElementAt(3).Cq().Children("a");
                    release.Link = new Uri(SiteLink + qLink.Attr("href"));

                    var sizeStr = row.ChildElements.ElementAt(5).Cq().Text().Trim();
                    var sizeVal = ParseUtil.CoerceFloat(sizeStr.Split(' ')[0]);
                    var sizeUnit = sizeStr.Split(' ')[1];
                    release.Size = ReleaseInfo.GetBytes(sizeUnit, sizeVal);

                    release.Seeders = ParseUtil.CoerceInt(qRow.Find(".t_seeders").Text().Trim());
                    release.Peers = ParseUtil.CoerceInt(qRow.Find(".t_leechers").Text().Trim()) + release.Seeders;

                    releases.Add(release);
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
            var response = await webclient.GetBytes(new Utils.Clients.WebRequest()
            {
                Url = link.ToString(),
                Cookies = cookieHeader
            });

            return response.Content;
        }
    }
}
