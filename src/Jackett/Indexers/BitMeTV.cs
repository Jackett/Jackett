using CsQuery;
using Newtonsoft.Json.Linq;
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

namespace Jackett
{
    public class BitMeTV : IndexerInterface
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

        static string BaseUrl = "http://www.bitmetv.org";
        static string LoginUrl = BaseUrl + "/login.php";
        static string LoginPost = BaseUrl + "/takelogin.php";
        static string CaptchaUrl = BaseUrl + "/visual.php";
        static string SearchUrl = BaseUrl + "/browse.php";

        CookieContainer cookies;
        HttpClientHandler handler;
        HttpClient client;

        public event Action<IndexerInterface, JToken> OnSaveConfigurationRequested;

        public BitMeTV()
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

        public string DisplayName { get { return "BitMeTV"; } }
        public string DisplayDescription { get { return "TV Episode specialty tracker"; } }
        public Uri SiteLink { get { return new Uri("https://bitmetv.org"); } }

        public bool IsConfigured { get; private set; }

        public async Task<ConfigurationData> GetConfigurationForSetup()
        {
            var loginPage = await client.GetAsync(LoginUrl);
            var captchaImage = await client.GetByteArrayAsync(CaptchaUrl);
            var config = new BmtvConfig();
            config.CaptchaImage.Value = captchaImage;
            return (ConfigurationData)config;
        }

        public async Task ApplyConfiguration(JToken configJson)
        {
            var config = new BmtvConfig();
            config.LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string>
                {
                    { "username", config.Username.Value},
                    { "password", config.Password.Value},
                    { "secimage", config.CaptchaText.Value}
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
                configSaveData["cookies"] = new JArray((
                    from cookie in cookies.GetCookies(new Uri(BaseUrl)).Cast<Cookie>()
                    select cookie.Name + ":" + cookie.Value
                ).ToArray());

                if (OnSaveConfigurationRequested != null)
                    OnSaveConfigurationRequested(this, configSaveData);

                IsConfigured = true;
            }
        }

        public async Task VerifyConnection()
        {
            var result = await client.GetStringAsync(new Uri(SearchUrl));
            if (result.Contains("<h1>Not logged in!</h1>"))
                throw new Exception("Detected as not logged in");
        }

        public void LoadFromSavedConfiguration(JToken jsonConfig)
        {
            cookies.FillFromJson(new Uri(BaseUrl), (JArray)jsonConfig["cookies"]);
            IsConfigured = true;
        }

        public async Task<ReleaseInfo[]> PerformQuery(TorznabQuery query)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            foreach (var title in query.ShowTitles)
            {

                var searchString = title + " " + query.GetEpisodeSearchString();
                var searchUrl = string.Format("{0}?search={1}&cat=0", SearchUrl, HttpUtility.UrlEncode(searchString));
                var results = await client.GetStringAsync(searchUrl);
                CQ dom = results;

                var table = dom["tbody > tr > .latest"].Parent().Parent();

                foreach (var row in table.Children().Skip(1))
                {
                    var release = new ReleaseInfo();

                    CQ qRow = row.Cq();
                    CQ qDetailsCol = row.ChildElements.ElementAt(1).Cq();
                    CQ qLink = qDetailsCol.Children("a").First();

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;
                    release.Comments = new Uri(BaseUrl + "/" + qLink.Attr("href"));
                    release.Guid = release.Comments;
                    release.Title = qLink.Attr("title");
                    release.Description = release.Title;

                    //"Tuesday, June 11th 2013 at 03:52:53 AM" to...
                    //"Tuesday June 11 2013 03 52 53 AM"
                    var timestamp = qDetailsCol.Children("font").Text().Trim() + " ";
                    var groups = new Regex(@"(.*?), (.*?) (.*?)th (.*?) at (.*?):(.*?):(.*?) (.*?) ").Match(timestamp).Groups;
                    var str = string.Join(" ", groups.Cast<Group>().Skip(1).Select(g => g.Value));
                    release.PublishDate = DateTime.ParseExact(str, "dddd MMMM d yyyy hh mm ss tt", CultureInfo.InvariantCulture);

                    release.Link = new Uri(BaseUrl + "/" + row.ChildElements.ElementAt(2).Cq().Children("a.index").Attr("href"));

                    var sizeCol = row.ChildElements.ElementAt(6);
                    var sizeVal = float.Parse(sizeCol.ChildNodes[0].NodeValue);
                    var sizeUnit = sizeCol.ChildNodes[2].NodeValue;

                    switch (sizeUnit)
                    {
                        case "GB": release.Size = ReleaseInfo.BytesFromGB(sizeVal); break;
                        case "MB": release.Size = ReleaseInfo.BytesFromMB(sizeVal); break;
                        case "KB": release.Size = ReleaseInfo.BytesFromKB(sizeVal); break;
                    }

                    release.Seeders = int.Parse(row.ChildElements.ElementAt(8).Cq().Text());
                    release.Peers = int.Parse(row.ChildElements.ElementAt(9).Cq().Text()) + release.Seeders;
                    releases.Add(release);
                }
            }

            return releases.ToArray();

        }

        public Task<byte[]> Download(Uri link)
        {
            return client.GetByteArrayAsync(link);
        }
    }
}
