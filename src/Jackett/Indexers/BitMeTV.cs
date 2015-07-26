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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Jackett.Indexers
{
    public class BitMeTV : BaseIndexer, IIndexer
    {
        class BmtvConfig : ConfigurationData
        {
            public StringItem Username { get; private set; }

            public StringItem Password { get; private set; }

            public ImageItem CaptchaImage { get; private set; }

            public StringItem CaptchaText { get; private set; }

            public BmtvConfig()
            {
                Username = new StringItem { Name = "Username" };
                Password = new StringItem { Name = "Password" };
                CaptchaImage = new ImageItem { Name = "Captcha Image" };
                CaptchaText = new StringItem { Name = "Captcha Text" };
            }

            public override Item[] GetItems()
            {
                return new Item[] { Username, Password, CaptchaImage, CaptchaText };
            }
        }

        private readonly string LoginUrl = "";
        private readonly string LoginPost = "";
        private readonly string CaptchaUrl = "";
        private readonly string SearchUrl = "";

        CookieContainer cookies;
        HttpClientHandler handler;
        HttpClient client;

        public BitMeTV(IIndexerManagerService i, Logger l)
            : base(name: "BitMeTV",
                description: "TV Episode specialty tracker",
                link: new Uri("http://www.bitmetv.org"),
                caps: TorznabCapsUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                logger: l)
        {
            LoginUrl = SiteLink + "login.php";
            LoginPost = SiteLink + "takelogin.php";
            CaptchaUrl = SiteLink + "visual.php";
            SearchUrl = SiteLink + "browse.php";

            cookies = new CookieContainer();
            handler = new HttpClientHandler
            {
                CookieContainer = cookies,
                AllowAutoRedirect = true,
                UseCookies = true,
            };
            client = new HttpClient(handler);
        }

        public async Task<ConfigurationData> GetConfigurationForSetup()
        {
            await client.GetAsync(LoginUrl);
            var captchaImage = await client.GetByteArrayAsync(CaptchaUrl);
            var config = new BmtvConfig();
            config.CaptchaImage.Value = captchaImage;
            return (ConfigurationData)config;
        }

        public async Task ApplyConfiguration(JToken configJson)
        {
            var config = new BmtvConfig();
            config.LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string> {
				{ "username", config.Username.Value },
				{ "password", config.Password.Value },
				{ "secimage", config.CaptchaText.Value }
			};

            var content = new FormUrlEncodedContent(pairs);

            var response = await client.PostAsync(LoginPost, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!responseContent.Contains("/logout.php"))
            {
                CQ dom = responseContent;
                var messageEl = dom["table tr > td.embedded > h2"].Last();
                var errorMessage = messageEl.Text();
                var captchaImage = await client.GetByteArrayAsync(CaptchaUrl);
                config.CaptchaImage.Value = captchaImage;
                config.CaptchaText.Value = "";
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

        public async Task<ReleaseInfo[]> PerformQuery(TorznabQuery query)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            var searchString = query.SanitizedSearchTerm + " " + query.GetEpisodeSearchString();
            var episodeSearchUrl = string.Format("{0}?search={1}&cat=0", SearchUrl, HttpUtility.UrlEncode(searchString));
            var results = await client.GetStringAsync(episodeSearchUrl);
            try
            {
                CQ dom = results;

                var table = dom["tbody > tr > .latest"].Parent().Parent();

                foreach (var row in table.Children().Skip(1))
                {
                    var release = new ReleaseInfo();

                    CQ qDetailsCol = row.ChildElements.ElementAt(1).Cq();
                    CQ qLink = qDetailsCol.Children("a").First();

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;
                    release.Comments = new Uri(SiteLink + "/" + qLink.Attr("href"));
                    release.Guid = release.Comments;
                    release.Title = qLink.Attr("title");
                    release.Description = release.Title;

                    //"Tuesday, June 11th 2013 at 03:52:53 AM" to...
                    //"Tuesday June 11 2013 03:52:53 AM"
                    var timestamp = qDetailsCol.Children("font").Text().Trim() + " ";
                    var timeParts = new List<string>(timestamp.Replace(" at", "").Replace(",", "").Split(' '));
                    timeParts[2] = Regex.Replace(timeParts[2], "[^0-9.]", "");
                    var formattedTimeString = string.Join(" ", timeParts.ToArray()).Trim();
                    var date = DateTime.ParseExact(formattedTimeString, "dddd MMMM d yyyy hh:mm:ss tt", CultureInfo.InvariantCulture);
                    release.PublishDate = DateTime.SpecifyKind(date, DateTimeKind.Utc).ToLocalTime();

                    release.Link = new Uri(SiteLink + "/" + row.ChildElements.ElementAt(2).Cq().Children("a.index").Attr("href"));

                    var sizeStr = row.ChildElements.ElementAt(6).Cq().Text();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(row.ChildElements.ElementAt(8).Cq().Text());
                    release.Peers = ParseUtil.CoerceInt(row.ChildElements.ElementAt(9).Cq().Text()) + release.Seeders;

                    //if (!release.Title.ToLower().Contains(title.ToLower()))
                    //    continue;

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results, ex);
            }

            return releases.ToArray();

        }

        public Task<byte[]> Download(Uri link)
        {
            return client.GetByteArrayAsync(link);
        }

    }
}
