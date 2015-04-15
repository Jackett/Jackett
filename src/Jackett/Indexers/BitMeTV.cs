using CsQuery;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

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
                Username = new StringItem { Name = "Username", ItemType = ItemType.InputString };
                Password = new StringItem { Name = "Password", ItemType = ItemType.InputString };
                CaptchaImage = new ImageItem { Name = "Captcha Image", ItemType = ItemType.DisplayImage };
                CaptchaText = new StringItem { Name = "Captcha Text", ItemType = ItemType.InputString };
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

        public Task<ConfigurationData> GetConfigurationForSetup()
        {
            return Task.Run(async () =>
            {
                var loginPage = await client.GetAsync(LoginUrl);
                var captchaImage = await client.GetByteArrayAsync(CaptchaUrl);
                var config = new BmtvConfig();
                config.CaptchaImage.Value = captchaImage;
                return (ConfigurationData)config;
            });
        }

        public Task ApplyConfiguration(JToken configJson)
        {
            return Task.Run(async () =>
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
            });
        }

        public Task VerifyConnection()
        {
            return Task.Run(async () =>
            {
                var result = await client.GetStringAsync(new Uri(SearchUrl));
                if (result.Contains("<h1>Not logged in!</h1>"))
                    throw new Exception("Detected as not logged in");
            });
        }

        public void LoadFromSavedConfiguration(JToken jsonConfig)
        {
            cookies.FillFromJson(new Uri(BaseUrl), (JArray)jsonConfig["cookies"]);
            IsConfigured = true;
        }
    }
}
