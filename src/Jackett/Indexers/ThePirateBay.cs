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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Jackett.Indexers
{
    public class ThePirateBay : BaseIndexer, IIndexer
    {
        private const string SearchUrl = "/search/{0}/0/99/208,205";
        private string BaseUrl;

        public ThePirateBay(IIndexerManagerService i, Logger l, IWebClient wc)
            : base(name: "The Pirate Bay",
                description: "The worlds largest bittorrent indexer",
                link: "https://thepiratebay.mn/",
                caps: TorznabCapsUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                client: wc,
                logger: l)
        {
            BaseUrl = SiteLink.ToString();
            IsConfigured = false;
        }

        public Task<ConfigurationData> GetConfigurationForSetup()
        {
            return Task.FromResult<ConfigurationData>(new ConfigurationDataUrl(BaseUrl));
        }

        public async Task ApplyConfiguration(JToken configJson)
        {
            var config = new ConfigurationDataUrl(SiteLink);
            config.LoadValuesFromJson(configJson);

            var formattedUrl = config.GetFormattedHostUrl();
            var releases = await PerformQuery(new TorznabQuery(), formattedUrl);
            if (releases.Length == 0)
                throw new Exception("Could not find releases from this URL");

            BaseUrl = formattedUrl;

            var configSaveData = new JObject();
            configSaveData["base_url"] = BaseUrl;
            SaveConfig(configSaveData);
            IsConfigured = true;
        }

        public override void LoadFromSavedConfiguration(JToken jsonConfig)
        {
            BaseUrl = (string)jsonConfig["base_url"];
            IsConfigured = true;
        }

        public async Task<ReleaseInfo[]> PerformQuery(TorznabQuery query)
        {
            return await PerformQuery(query, BaseUrl);
        }

        async Task<ReleaseInfo[]> PerformQuery(TorznabQuery query, string baseUrl)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.SanitizedSearchTerm + " " + query.GetEpisodeSearchString();
            var queryStr = HttpUtility.UrlEncode(searchString);
            var episodeSearchUrl = baseUrl + string.Format(SearchUrl, queryStr);
            var response = await RequestStringWithCookies(episodeSearchUrl, string.Empty);

            try
            {
                CQ dom = response.Content;

                var rows = dom["#searchResult > tbody > tr"];
                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();

                    CQ qRow = row.Cq();
                    CQ qLink = qRow.Find(".detName > .detLink").First();

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;
                    release.Title = qLink.Text().Trim();
                    release.Description = release.Title;
                    release.Comments = new Uri(baseUrl + "/" + qLink.Attr("href").TrimStart('/'));
                    release.Guid = release.Comments;

                    var downloadCol = row.ChildElements.ElementAt(1).Cq().Children("a");
                    release.MagnetUri = new Uri(downloadCol.Attr("href"));
                    release.InfoHash = release.MagnetUri.ToString().Split(':')[3].Split('&')[0];

                    var descString = qRow.Find(".detDesc").Text().Trim();
                    var descParts = descString.Split(',');

                    var timeString = descParts[0].Split(' ')[1];

                    if (timeString.Contains("mins ago"))
                    {
                        release.PublishDate = (DateTime.Now - TimeSpan.FromMinutes(ParseUtil.CoerceInt(timeString.Split(' ')[0])));
                    }
                    else if (timeString.Contains("Today"))
                    {
                        release.PublishDate = (DateTime.UtcNow - TimeSpan.FromHours(2) - TimeSpan.Parse(timeString.Split(' ')[1])).ToLocalTime();
                    }
                    else if (timeString.Contains("Y-day"))
                    {
                        release.PublishDate = (DateTime.UtcNow - TimeSpan.FromHours(26) - TimeSpan.Parse(timeString.Split(' ')[1])).ToLocalTime();
                    }
                    else if (timeString.Contains(':'))
                    {
                        var utc = DateTime.ParseExact(timeString, "MM-dd HH:mm", CultureInfo.InvariantCulture) - TimeSpan.FromHours(2);
                        release.PublishDate = DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();
                    }
                    else
                    {
                        var utc = DateTime.ParseExact(timeString, "MM-dd yyyy", CultureInfo.InvariantCulture) - TimeSpan.FromHours(2);
                        release.PublishDate = DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();
                    }

                    release.Size = ReleaseInfo.GetBytes(descParts[1]);

                    release.Seeders = ParseUtil.CoerceInt(row.ChildElements.ElementAt(2).Cq().Text());
                    release.Peers = ParseUtil.CoerceInt(row.ChildElements.ElementAt(3).Cq().Text()) + release.Seeders;

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.Content, ex);
            }
            return releases.ToArray();
        }

        public override Task<byte[]> Download(Uri link)
        {
            throw new NotImplementedException();
        }
    }
}
