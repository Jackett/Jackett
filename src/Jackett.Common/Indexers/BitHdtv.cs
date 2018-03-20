using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsQuery;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    public class BitHdtv : BaseWebIndexer
    {
        private string LoginUrl { get { return SiteLink + "login.php"; } }
        private string TakeLoginUrl { get { return SiteLink + "takelogin.php"; } }
        private string SearchUrl { get { return SiteLink + "torrents.php?"; } }
        private string DownloadUrl { get { return SiteLink + "download.php?id={0}"; } }

        private new ConfigurationDataRecaptchaLogin configData
        {
            get { return (ConfigurationDataRecaptchaLogin)base.configData; }
            set { base.configData = value; }
        }

        public BitHdtv(IIndexerConfigurationService configService, WebClient w, Logger l, IProtectionService ps)
            : base(name: "BIT-HDTV",
                description: "Home of high definition invites",
                link: "https://www.bit-hdtv.com/",
                caps: new TorznabCapabilities(),
                configService: configService,
                client: w,
                logger: l,
                p: ps,
                configData: new ConfigurationDataRecaptchaLogin())
        {
            Encoding = Encoding.GetEncoding("iso-8859-1");
            Language = "en-us";
            Type = "private";

            AddCategoryMapping(1, TorznabCatType.TVAnime); // Anime
            AddCategoryMapping(2, TorznabCatType.MoviesBluRay); // Blu-ray
            AddCategoryMapping(4, TorznabCatType.TVDocumentary); // Documentaries
            AddCategoryMapping(6, TorznabCatType.AudioLossless); // HQ Audio
            AddCategoryMapping(7, TorznabCatType.Movies); // Movies
            AddCategoryMapping(8, TorznabCatType.AudioVideo); // Music Videos
            AddCategoryMapping(5, TorznabCatType.TVSport); // Sports
            AddCategoryMapping(10, TorznabCatType.TV); // TV
            AddCategoryMapping(12, TorznabCatType.TV); // TV/Seasonpack
            AddCategoryMapping(11, TorznabCatType.XXX); // XXX
        }

        public override async Task<ConfigurationData> GetConfigurationForSetup()
        {
            var loginPage = await RequestStringWithCookies(LoginUrl, configData.CookieHeader.Value);
            CQ cq = loginPage.Content;
            string recaptchaSiteKey = cq.Find(".g-recaptcha").Attr("data-sitekey");
            var result = this.configData;
            result.CookieHeader.Value = loginPage.Cookies;
            result.Captcha.SiteKey = recaptchaSiteKey;
            result.Captcha.Version = "2";
            return result;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "g-recaptcha-response", configData.Captcha.Value },
            };

            if (!string.IsNullOrWhiteSpace(configData.Captcha.Cookie))
            {
                // Cookie was manually supplied
                CookieHeader = configData.Captcha.Cookie;
                try
                {
                    var results = await PerformQuery(new TorznabQuery());
                    if (!results.Any())
                    {
                        throw new Exception("Your cookie did not work");
                    }

                    IsConfigured = true;
                    SaveConfig();
                    return IndexerConfigurationStatus.Completed;
                }
                catch (Exception e)
                {
                    IsConfigured = false;
                    throw new Exception("Your cookie did not work: " + e.Message);
                }
            }

            var response = await RequestLoginAndFollowRedirect(TakeLoginUrl, pairs, null, true, null, SiteLink);
            await ConfigureIfOK(response.Cookies, response.Content != null && response.Content.Contains("logout.php"), () =>
            {
                CQ dom = response.Content;
                var messageEl = dom["table.detail td.text"].Last();
                messageEl.Children("a").Remove();
                messageEl.Children("style").Remove();
                var errorMessage = messageEl.Text().Trim();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();
            var queryCollection = new NameValueCollection();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                queryCollection.Add("search", searchString);
            }

            queryCollection.Add("incldead", "1");

            var searchUrl = SearchUrl + queryCollection.GetQueryString();

            var trackerCats = MapTorznabCapsToTrackers(query, mapChildrenCatsToParent: true);

            var results = await RequestStringWithCookiesAndRetry(searchUrl);
            try
            {
                CQ dom = results.Content;
                dom["#needseed"].Remove();
                foreach (var table in dom["table[align=center] + br + table > tbody"])
                {
                    var rows = table.Cq().Children();
                    foreach (var row in rows.Skip(1))
                    {
                        var release = new ReleaseInfo();

                        var qRow = row.Cq();
                        var qLink = qRow.Children().ElementAt(2).Cq().Children("a").First();

                        release.MinimumRatio = 1;
                        release.MinimumSeedTime = 172800;
                        release.Title = qLink.Attr("title");
                        if (!query.MatchQueryStringAND(release.Title))
                            continue;
                        release.Files = ParseUtil.CoerceLong(qRow.Find("td:nth-child(4)").Text());
                        release.Grabs = ParseUtil.CoerceLong(qRow.Find("td:nth-child(8)").Text());
                        release.Guid = new Uri(qLink.Attr("href"));
                        release.Comments = release.Guid;
                        release.Link = new Uri(string.Format(DownloadUrl, qLink.Attr("href").Split('=')[1]));

                        var catUrl = qRow.Children().ElementAt(1).FirstElementChild.Cq().Attr("href");
                        var catNum = catUrl.Split(new char[] { '=', '&' })[1];
                        release.Category = MapTrackerCatToNewznab(catNum);

                        // This tracker cannot search multiple cats at a time, so search all cats then filter out results from different cats
                        if (trackerCats.Count > 0 && !trackerCats.Contains(catNum))
                            continue;

                        var dateString = qRow.Children().ElementAt(5).Cq().Text().Trim();
                        var pubDate = DateTime.ParseExact(dateString, "yyyy-MM-ddHH:mm:ss", CultureInfo.InvariantCulture);
                        release.PublishDate = DateTime.SpecifyKind(pubDate, DateTimeKind.Local);

                        var sizeStr = qRow.Children().ElementAt(6).Cq().Text();
                        release.Size = ReleaseInfo.GetBytes(sizeStr);

                        release.Seeders = ParseUtil.CoerceInt(qRow.Children().ElementAt(8).Cq().Text().Trim());
                        release.Peers = ParseUtil.CoerceInt(qRow.Children().ElementAt(9).Cq().Text().Trim()) + release.Seeders;

                        var bgcolor = qRow.Attr("bgcolor");
                        if (bgcolor == "#DDDDDD")
                        {
                            release.DownloadVolumeFactor = 1;
                            release.UploadVolumeFactor = 2;
                        }
                        else if (bgcolor == "#FFFF99")
                        {
                            release.DownloadVolumeFactor = 0;
                            release.UploadVolumeFactor = 1;
                        }
                        else if (bgcolor == "#CCFF99")
                        {
                            release.DownloadVolumeFactor = 0;
                            release.UploadVolumeFactor = 2;
                        }
                        else
                        {
                            release.DownloadVolumeFactor = 1;
                            release.UploadVolumeFactor = 1;
                        }
                        releases.Add(release);
                    }
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
