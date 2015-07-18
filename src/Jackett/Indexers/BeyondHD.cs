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
    public class BeyondHD : IndexerInterface
    {
        public event Action<IndexerInterface, JToken> OnSaveConfigurationRequested;

        public event Action<IndexerInterface, string, Exception> OnResultParsingError;

        public string DisplayName
        {
            get { return "BeyondHD"; }
        }

        public string DisplayDescription
        {
            get { return "Without BeyondHD, your HDTV is just a TV"; }
        }

        public Uri SiteLink
        {
            get { return new Uri(BaseUrl); }
        }

        public bool RequiresRageIDLookupDisabled { get { return true; } }

        public bool IsConfigured { get; private set; }

        const string BaseUrl = "https://beyondhd.me";
        const string SearchUrl = BaseUrl + "/browse.php?c40=1&c44=1&c48=1&c89=1&c46=1&c45=1&searchin=title&incldead=0&search={0}";
        const string DownloadUrl = BaseUrl + "/download.php?torrent={0}";

        CookieContainer cookies;
        HttpClientHandler handler;
        HttpClient client;

        public BeyondHD()
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
            var config = new ConfigurationDataCookie();
            return Task.FromResult<ConfigurationData>(config);
        }

        public async Task ApplyConfiguration(JToken configJson)
        {
            var config = new ConfigurationDataCookie();
            config.LoadValuesFromJson(configJson);

            var jsonCookie = new JObject();
            jsonCookie["cookie_header"] = config.CookieHeader;
            cookies.FillFromJson(new Uri(BaseUrl), jsonCookie);

            var responseContent = await client.GetStringAsync(BaseUrl);

            if (!responseContent.Contains("logout.php"))
            {
                CQ dom = responseContent;
                throw new ExceptionWithConfigData("Invalid cookie header", (ConfigurationData)config);
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

        public void LoadFromSavedConfiguration(JToken jsonConfig)
        {
            cookies.FillFromJson(new Uri(BaseUrl), jsonConfig);
            IsConfigured = true;
        }

        public async Task<ReleaseInfo[]> PerformQuery(TorznabQuery query)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            var searchString = query.SanitizedSearchTerm + " " + query.GetEpisodeSearchString();
            var episodeSearchUrl = string.Format(SearchUrl, HttpUtility.UrlEncode(searchString));
            var results = await client.GetStringAsync(episodeSearchUrl);

            try
            {
                CQ dom = results;
                var rows = dom["table.torrenttable > tbody > tr.browse_color"];
                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;

                    var qRow = row.Cq();

                    var qLink = row.ChildElements.ElementAt(2).FirstChild.Cq();
                    release.Link = new Uri(BaseUrl + "/" + qLink.Attr("href"));
                    var torrentID = qLink.Attr("href").Split('=').Last();

                    var descCol = row.ChildElements.ElementAt(3);
                    var qCommentLink = descCol.FirstChild.Cq();
                    release.Title = qCommentLink.Text();
                    release.Description = release.Title;
                    release.Comments = new Uri(BaseUrl + "/" + qCommentLink.Attr("href"));
                    release.Guid = release.Comments;

                    var dateStr = descCol.ChildElements.Last().Cq().Text().Split('|').Last().ToLowerInvariant().Replace("ago.", "").Trim();
                    var dateParts = dateStr.Split(new char[] { ' ', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    var timeSpan = TimeSpan.Zero;
                    for (var i = 0; i < dateParts.Length / 2; i++)
                    {
                        var timeVal = ParseUtil.CoerceInt(dateParts[i * 2]);
                        var timeUnit = dateParts[i * 2 + 1];
                        if (timeUnit.Contains("year"))
                            timeSpan += TimeSpan.FromDays(365 * timeVal);
                        else if (timeUnit.Contains("month"))
                            timeSpan += TimeSpan.FromDays(30 * timeVal);
                        else if (timeUnit.Contains("day"))
                            timeSpan += TimeSpan.FromDays(timeVal);
                        else if (timeUnit.Contains("hour"))
                            timeSpan += TimeSpan.FromHours(timeVal);
                        else if (timeUnit.Contains("min"))
                            timeSpan += TimeSpan.FromMinutes(timeVal);
                    }
                    release.PublishDate = DateTime.SpecifyKind(DateTime.Now - timeSpan, DateTimeKind.Local);

                    var sizeEl = row.ChildElements.ElementAt(7);
                    var sizeVal = ParseUtil.CoerceFloat(sizeEl.ChildNodes.First().NodeValue);
                    var sizeUnit = sizeEl.ChildNodes.Last().NodeValue;

                    release.Size = ReleaseInfo.GetBytes(sizeUnit, sizeVal);

                    release.Seeders = ParseUtil.CoerceInt(row.ChildElements.ElementAt(9).Cq().Text());
                    release.Peers = ParseUtil.CoerceInt(row.ChildElements.ElementAt(10).Cq().Text()) + release.Seeders;

                    releases.Add(release);

                }
            }

            catch (Exception ex)
            {
                OnResultParsingError(this, results, ex);
                throw ex;
            }

            return releases.ToArray();
        }

        public Task<byte[]> Download(Uri link)
        {
            return client.GetByteArrayAsync(link);
        }
    }
}
