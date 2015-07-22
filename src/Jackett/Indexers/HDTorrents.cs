using CsQuery;
using Jackett.Models;
using Jackett.Services;
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
    public class HDTorrents : BaseIndexer, IIndexer
    {
        private readonly string SearchUrl = "";
        private static string LoginUrl = "";
        private const int MAXPAGES = 3;

        CookieContainer cookies;
        HttpClientHandler handler;
        HttpClient client;

        public HDTorrents(IIndexerManagerService i, Logger l)
            : base(name: "HD-Torrents",
                description: "HD-Torrents is a private torrent website with HD torrents and strict rules on their content.",
                link: new Uri("http://hdts.ru"),// Of the accessible domains the .ru seems the most reliable.  https://hdts.ru | https://hd-torrents.org | https://hd-torrents.net | https://hd-torrents.me
                caps: TorznabCapsUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                logger: l)
        {
            SearchUrl = SiteLink + "/torrents.php?search={0}&active=1&options=0&category%5B%5D=59&category%5B%5D=60&category%5B%5D=30&category%5B%5D=38&page={1}";
            LoginUrl = SiteLink + "/login.php";

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

        HttpRequestMessage CreateHttpRequest(string url)
        {
            var message = new HttpRequestMessage();
            message.Method = HttpMethod.Get;
            message.RequestUri = new Uri(url);
            message.Headers.UserAgent.ParseAdd(BrowserUtil.ChromeUserAgent);
            return message;
        }

        public async Task ApplyConfiguration(JToken configJson)
        {
            var config = new ConfigurationDataBasicLogin();
            config.LoadValuesFromJson(configJson);

            var startMessage = CreateHttpRequest(LoginUrl);
            var results = await (await client.SendAsync(startMessage)).Content.ReadAsStringAsync();


            var pairs = new Dictionary<string, string> {
				{ "uid", config.Username.Value },
				{ "pwd", config.Password.Value }
			};

            var content = new FormUrlEncodedContent(pairs);

            var loginRequest = CreateHttpRequest(LoginUrl);
            loginRequest.Method = HttpMethod.Post;
            loginRequest.Content = content;
            loginRequest.Headers.Referrer = new Uri("https://hd-torrents.org/torrents.php");

            var response = await client.SendAsync(loginRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!responseContent.Contains("If your browser doesn't have javascript enabled"))
            {
                var errorMessage = "Couldn't login";
                throw new ExceptionWithConfigData(errorMessage, (ConfigurationData)config);
            }
            else
            {
                var configSaveData = new JObject();
                cookies.DumpToJson(SiteLink, configSaveData);
                SaveConfig(configSaveData);
                IsConfigured = true;
            }
        }

        public void LoadFromSavedConfiguration(JToken jsonConfig)
        {
            cookies.FillFromJson(SiteLink, jsonConfig, logger);
            IsConfigured = true;
        }

        async Task<ReleaseInfo[]> PerformQuery(TorznabQuery query, Uri baseUrl)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();
            List<string> searchurls = new List<string>();

            var searchString = query.SanitizedSearchTerm + " " + query.GetEpisodeSearchString();
            for (int page = 0; page < MAXPAGES; page++)
            {
                searchurls.Add(string.Format(SearchUrl, HttpUtility.UrlEncode(searchString.Trim()), page));
            }

            foreach (string SearchUrl in searchurls)
            {
                var results = await client.GetStringAsync(SearchUrl);
                try
                {
                    CQ dom = results;
                    ReleaseInfo release;

                    int rowCount = 0;
                    var rows = dom[".mainblockcontenttt > tbody > tr"];
                    foreach (var row in rows)
                    {
                        CQ qRow = row.Cq();
                        if (rowCount < 2 || qRow.Children().Count() != 12) //skip 2 rows because there's an empty row & a title/sort row
                        {
                            rowCount++;
                            continue;
                        }

                        release = new ReleaseInfo();
                        long? size;

                        release.Title = qRow.Find("td.mainblockcontent b a").Text();
                        release.Description = release.Title;

                        if (0 != qRow.Find("td.mainblockcontent u").Length)
                        {
                            var imdbStr = qRow.Find("td.mainblockcontent u").Parent().First().Attr("href").Replace("http://www.imdb.com/title/tt", "").Replace("/", "");
                            long imdb;
                            if (ParseUtil.TryCoerceLong(imdbStr, out imdb))
                            {
                                release.Imdb = imdb;
                            }
                        }

                        release.MinimumRatio = 1;
                        release.MinimumSeedTime = 172800;



                        int seeders, peers;
                        if (ParseUtil.TryCoerceInt(qRow.Find("td").Get(9).FirstChild.FirstChild.InnerText, out seeders))
                        {
                            release.Seeders = seeders;
                            if (ParseUtil.TryCoerceInt(qRow.Find("td").Get(10).FirstChild.FirstChild.InnerText, out peers))
                            {
                                release.Peers = peers + release.Seeders;
                            }
                        }

                        string fullSize = qRow.Find("td.mainblockcontent").Get(6).InnerText;
                        string[] sizeSplit = fullSize.Split(' ');
                        switch (sizeSplit[1].ToLower())
                        {
                            case "kb":
                                size = ReleaseInfo.BytesFromKB(ParseUtil.CoerceFloat(sizeSplit[0]));
                                break;
                            case "mb":
                                size = ReleaseInfo.BytesFromMB(ParseUtil.CoerceFloat(sizeSplit[0]));
                                break;
                            case "gb":
                                size = ReleaseInfo.BytesFromGB(ParseUtil.CoerceFloat(sizeSplit[0]));
                                break;
                            default:
                                size = null;
                                break;
                        }
                        release.Size = size;

                        release.Guid = new Uri(SiteLink + "/" + qRow.Find("td.mainblockcontent b a").Attr("href"));
                        release.Link = new Uri(SiteLink + "/" + qRow.Find("td.mainblockcontent").Get(3).FirstChild.GetAttribute("href"));
                        release.Comments = new Uri(SiteLink + "/" + qRow.Find("td.mainblockcontent b a").Attr("href") + "#comments");

                        string[] dateSplit = qRow.Find("td.mainblockcontent").Get(5).InnerHTML.Split(',');
                        string dateString = dateSplit[1].Substring(0, dateSplit[1].IndexOf('>'));
                        release.PublishDate = DateTime.Parse(dateString, CultureInfo.InvariantCulture);

                        releases.Add(release);
                    }
                }
                catch (Exception ex)
                {
                    OnParseError(results, ex);
                }
            }

            return releases.ToArray();
        }

        public async Task<ReleaseInfo[]> PerformQuery(TorznabQuery query)
        {
            return await PerformQuery(query, SiteLink);
        }

        public Task<byte[]> Download(Uri link)
        {
            return client.GetByteArrayAsync(link);
        }
    }
}
