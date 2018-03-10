using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CsQuery;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    public class BitMeTV : BaseWebIndexer
    {
        //https is poorly implemented on BitMeTV. Site uses http to login, but then redirects to https for search
        private string LoginUrl { get { return SiteLink + "login.php"; } }

        private string LoginPost { get { return SiteLink + "takelogin.php"; } }
        private string CaptchaUrl { get { return SiteLink + "visual.php"; } }
        private string SearchUrl { get { return "https://www.bitmetv.org/browse.php"; } }

        private new ConfigurationDataCaptchaLogin configData
        {
            get { return (ConfigurationDataCaptchaLogin)base.configData; }
            set { base.configData = value; }
        }

        public BitMeTV(IIndexerConfigurationService configService, Utils.Clients.WebClient c, Logger l, IProtectionService ps)
            : base(name: "BitMeTV",
                description: "TV Episode specialty tracker",
                link: "http://www.bitmetv.org/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                configService: configService,
                client: c,
                logger: l,
                p: ps,
                configData: new ConfigurationDataCaptchaLogin("Ensure that you have the 'Force SSL' option set to 'yes' in your profile on the BitMeTv webpage."))
        {
            Encoding = Encoding.GetEncoding("iso-8859-1");
            Language = "en-us";
            Type = "private";
        }

        public override async Task<ConfigurationData> GetConfigurationForSetup()
        {
            var response = await webclient.GetString(new Utils.Clients.WebRequest()
            {
                Url = LoginUrl
            });
            CookieHeader = response.Cookies;
            var captchaImage = await RequestBytesWithCookies(CaptchaUrl);
            configData.CaptchaImage.Value = captchaImage.Content;
            configData.CaptchaCookie.Value = captchaImage.Cookies;
            return configData;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "secimage", configData.CaptchaText.Value }
            };

            var response = await RequestLoginAndFollowRedirect(LoginPost, pairs, configData.CaptchaCookie.Value, true);
            await ConfigureIfOK(response.Cookies, response.Content.Contains("/logout.php"), async () =>
            {
                CQ dom = response.Content;
                var messageEl = dom["table tr > td.embedded > h2"].Last();
                var errorMessage = messageEl.Text();
                var captchaImage = await RequestBytesWithCookies(CaptchaUrl);
                configData.CaptchaImage.Value = captchaImage.Content;
                configData.CaptchaText.Value = "";
                configData.CaptchaCookie.Value = captchaImage.Cookies;
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var episodeSearchUrl = string.Format("{0}?search={1}&cat=0&incldead=1", SearchUrl, WebUtility.UrlEncode(query.GetQueryString()));
            var results = await RequestStringWithCookiesAndRetry(episodeSearchUrl);
            try
            {
                CQ dom = results.Content;

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
                    if (!query.MatchQueryStringAND(release.Title))
                        continue;

                    //"Tuesday, June 11th 2013 at 03:52:53 AM" to...
                    //"Tuesday June 11 2013 03:52:53 AM"
                    var timestamp = qDetailsCol.Children("font").Text().Trim() + " ";
                    var timeParts = new List<string>(timestamp.Replace(" at", "").Replace(",", "").Split(' '));
                    timeParts[2] = Regex.Replace(timeParts[2], "[^0-9.]", "");
                    var formattedTimeString = string.Join(" ", timeParts.ToArray()).Trim();
                    var date = DateTime.ParseExact(formattedTimeString, "dddd MMMM d yyyy hh:mm:ss tt", CultureInfo.InvariantCulture);
                    release.PublishDate = DateTime.SpecifyKind(date, DateTimeKind.Utc).ToLocalTime();

                    release.Link = new Uri(SiteLink.Replace("http:", "https:") + "/" + row.ChildElements.ElementAt(2).Cq().Children("a.index").Attr("href"));

                    var sizeStr = row.ChildElements.ElementAt(6).Cq().Text();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(row.ChildElements.ElementAt(8).Cq().Text());
                    release.Peers = ParseUtil.CoerceInt(row.ChildElements.ElementAt(9).Cq().Text()) + release.Seeders;

                    //if (!release.Title.ToLower().Contains(title.ToLower()))
                    //    continue;

                    var files = row.Cq().Find("td:nth-child(4)").Text();
                    release.Files = ParseUtil.CoerceInt(files);

                    var grabs = row.Cq().Find("td:nth-child(8)").Get(0).FirstChild.ToString();
                    release.Grabs = ParseUtil.CoerceInt(grabs);

                    release.DownloadVolumeFactor = 1;
                    release.UploadVolumeFactor = 1;

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.Content, ex);
            }

            return releases;
        }
    }
}
