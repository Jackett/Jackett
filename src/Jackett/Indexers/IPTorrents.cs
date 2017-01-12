using CsQuery;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Jackett.Models.IndexerConfig;

namespace Jackett.Indexers
{
    public class IPTorrents : BaseIndexer, IIndexer
    {
        string LoginUrl { get { return SiteLink + "login.php"; } }
        string TakeLoginUrl { get { return SiteLink + "take_login.php"; } }
        private string BrowseUrl { get { return SiteLink + "t"; } }
        public new string[] AlternativeSiteLinks { get; protected set; } = new string[] { "https://ipt-update.com/", "https://iptorrents.com/", "https://iptorrents.eu/", "https://nemo.iptorrents.com/", "https://ipt.rocks/" };

        new ConfigurationDataRecaptchaLogin configData
        {
            get { return (ConfigurationDataRecaptchaLogin)base.configData; }
            set { base.configData = value; }
        }

        public IPTorrents(IIndexerManagerService i, IWebClient wc, Logger l, IProtectionService ps)
            : base(name: "IPTorrents",
                description: "Always a step ahead.",
                link: "https://iptorrents.com/",
                caps: new TorznabCapabilities(),
                manager: i,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataRecaptchaLogin())
        {
            Encoding = Encoding.GetEncoding("UTF-8");
            Language = "en-us";

            AddCategoryMapping(72, TorznabCatType.Movies);
            AddCategoryMapping(77, TorznabCatType.MoviesSD);
            AddCategoryMapping(89, TorznabCatType.MoviesSD);
            AddCategoryMapping(90, TorznabCatType.MoviesSD);
            AddCategoryMapping(96, TorznabCatType.MoviesSD);
            AddCategoryMapping(6, TorznabCatType.MoviesSD);
            AddCategoryMapping(48, TorznabCatType.MoviesHD);
            AddCategoryMapping(54, TorznabCatType.Movies);
            AddCategoryMapping(62, TorznabCatType.MoviesSD);
            AddCategoryMapping(38, TorznabCatType.MoviesForeign);
            AddCategoryMapping(68, TorznabCatType.Movies);
            AddCategoryMapping(20, TorznabCatType.MoviesHD);
            AddCategoryMapping(7, TorznabCatType.MoviesSD);

            AddCategoryMapping(73, TorznabCatType.TV);
            AddCategoryMapping(26, TorznabCatType.TVSD);
            AddCategoryMapping(55, TorznabCatType.TVSD);
            AddCategoryMapping(78, TorznabCatType.TVSD);
            AddCategoryMapping(23, TorznabCatType.TVHD);
            AddCategoryMapping(24, TorznabCatType.TVSD);
            AddCategoryMapping(25, TorznabCatType.TVSD);
            AddCategoryMapping(65, TorznabCatType.TV);
            AddCategoryMapping(66, TorznabCatType.TVSD);
            AddCategoryMapping(82, TorznabCatType.TVSD);
            AddCategoryMapping(65, TorznabCatType.TV);
            AddCategoryMapping(83, TorznabCatType.TV);
            AddCategoryMapping(79, TorznabCatType.TVSD);
            AddCategoryMapping(22, TorznabCatType.TVHD);
            AddCategoryMapping(79, TorznabCatType.TVSD);
            AddCategoryMapping(4, TorznabCatType.TVSD);
            AddCategoryMapping(5, TorznabCatType.TVHD);
            AddCategoryMapping(99, TorznabCatType.TVHD); // TV/x265

            AddCategoryMapping(75, TorznabCatType.Audio);
            AddCategoryMapping(73, TorznabCatType.Audio);
            AddCategoryMapping(80, TorznabCatType.AudioLossless);
            AddCategoryMapping(93, TorznabCatType.Audio);

            AddCategoryMapping(60, TorznabCatType.TVAnime);
            AddCategoryMapping(1, TorznabCatType.PC);
            AddCategoryMapping(64, TorznabCatType.AudioAudiobook);
            AddCategoryMapping(35, TorznabCatType.Books);
            AddCategoryMapping(94, TorznabCatType.BooksComics);
        }

        public override async Task<ConfigurationData> GetConfigurationForSetup()
        {
            var loginPage = await RequestStringWithCookies(LoginUrl, string.Empty);
            CQ cq = loginPage.Content;
            var captcha = cq.Find(".g-recaptcha");
            if(captcha.Any())
            {
                var result = this.configData;
                result.CookieHeader.Value = loginPage.Cookies;
                result.Captcha.SiteKey = captcha.Attr("data-sitekey");
                result.Captcha.Version = "2";
                return result;
            }
            else
            {
                var result = new ConfigurationDataBasicLogin();
                result.SiteLink.Value = configData.SiteLink.Value;
                result.Instructions.Value = configData.Instructions.Value;
                result.Username.Value = configData.Username.Value;
                result.Password.Value = configData.Password.Value;
                result.CookieHeader.Value = loginPage.Cookies;
                return result;
            }
        }

        

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "g-recaptcha-response", configData.Captcha.Value }
            };

            if (!string.IsNullOrWhiteSpace(configData.Captcha.Cookie))
            {
                CookieHeader = configData.Captcha.Cookie;
                try
                {
                    var results = await PerformQuery(new TorznabQuery());
                    if (results.Count() == 0)
                    {
                        throw new Exception("Your cookie did not work");
                    }

                    SaveConfig();
                    IsConfigured = true;
                    return IndexerConfigurationStatus.Completed;
                }
                catch (Exception e)
                {
                    IsConfigured = false;
                    throw new Exception("Your cookie did not work: " + e.Message);
                }
            }

            var request = new Utils.Clients.WebRequest()
            {
                Url = TakeLoginUrl,
                Type = RequestType.POST,
                Referer = SiteLink,
                Encoding = Encoding,
                PostData = pairs
            };
            var response = await webclient.GetString(request);
            var firstCallCookies = response.Cookies;
            // Redirect to ? then to /t
            await FollowIfRedirect(response, request.Url, null, firstCallCookies);

            await ConfigureIfOK(firstCallCookies, response.Content.Contains("/my.php"), () =>
            {
                CQ dom = response.Content;
                var messageEl = dom["body > div"].First();
                var errorMessage = messageEl.Text().Trim();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();
            var searchUrl = BrowseUrl;
            var queryCollection = new NameValueCollection();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                queryCollection.Add("q", searchString);
            }

            foreach (var cat in MapTorznabCapsToTrackers(query))
            {
                queryCollection.Add(cat, string.Empty);
            }

            if (queryCollection.Count > 0)
            {
                searchUrl += "?" + queryCollection.GetQueryString();
            }

            var response = await RequestStringWithCookiesAndRetry(searchUrl, null, BrowseUrl);

            var results = response.Content;
            try
            {
                CQ dom = results;

                var rows = dom["table[id='torrents'] > tbody > tr"];
                foreach (var row in rows.Skip(1))
                {
                    var release = new ReleaseInfo();
                    var qRow = row.Cq();
                    var qTitleLink = qRow.Find("a.t_title").First();
                    release.Title = qTitleLink.Text().Trim();

                    // If we search an get no results, we still get a table just with no info.
                    if (string.IsNullOrWhiteSpace(release.Title))
                    {
                        break;
                    }

                    release.Description = release.Title;
                    release.Guid = new Uri(SiteLink + qTitleLink.Attr("href").Substring(1));
                    release.Comments = release.Guid;

                    var descString = qRow.Find(".t_ctime").Text();
                    var dateString = descString.Split('|').Last().Trim();
                    dateString = dateString.Split(new string[] { " by " }, StringSplitOptions.None)[0];
                    release.PublishDate = DateTimeUtil.FromTimeAgo(dateString);

                    var qLink = row.ChildElements.ElementAt(3).Cq().Children("a");
                    release.Link = new Uri(SiteLink + HttpUtility.UrlEncode(qLink.Attr("href").TrimStart('/')));

                    var sizeStr = row.ChildElements.ElementAt(5).Cq().Text();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(qRow.Find(".t_seeders").Text().Trim());
                    release.Peers = ParseUtil.CoerceInt(qRow.Find(".t_leechers").Text().Trim()) + release.Seeders;

                    var cat = row.Cq().Find("td:eq(0) a").First().Attr("href").Substring(1);
                    release.Category = MapTrackerCatToNewznab(cat);

                    var grabs = row.Cq().Find("td:nth-child(7)").Text();
                    release.Grabs = ParseUtil.CoerceInt(grabs);

                    if(row.Cq().Find("span.t_tag_free_leech").Any())
                        release.DownloadVolumeFactor = 0;
                    else
                        release.DownloadVolumeFactor = 1;

                    release.UploadVolumeFactor = 1;

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results, ex);
            }

            return releases;
        }
    }
}
