using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    public class PiXELHD : BaseWebIndexer
    {
        private string LoginUrl => $"{SiteLink}login.php";
        private string BrowseUrl => $"{SiteLink}torrents.php";

        private new ConfigurationDataCaptchaLogin configData
        {
            get => (ConfigurationDataCaptchaLogin)base.configData;
            set => base.configData = value;
        }

        private string _inputCaptcha;
        private string _inputUsername;
        private string _inputPassword;

        public PiXELHD(IIndexerConfigurationService configService, WebClient webClient, Logger logger,
                       IProtectionService protectionService) : base(
            "PiXELHD", description: "PixelHD (PxHD) is a Private Torrent Tracker for HD .MP4 MOVIES / TV",
            link: "https://pixelhd.me/", caps: new TorznabCapabilities(), configService: configService, logger: logger,
            p: protectionService, client: webClient, configData: new ConfigurationDataCaptchaLogin())
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";
            TorznabCaps.SupportsImdbMovieSearch = true;
            AddCategoryMapping(1, TorznabCatType.MoviesHD);
        }

        public override async Task<ConfigurationData> GetConfigurationForSetup()
        {
            var loginPage = await RequestStringWithCookiesAsync(LoginUrl, string.Empty);
            var loginParser = new HtmlParser();
            var loginDocument = loginParser.ParseDocument(loginPage.Content);
            configData.CaptchaCookie.Value = loginPage.Cookies;
            var catchaImg = loginDocument.QuerySelector("img[alt=\"FuckOff Image\"]");
            if (catchaImg != null)
            {
                var catchaInput = loginDocument.QuerySelector("input[maxlength=\"6\"]");
                _inputCaptcha = catchaInput.GetAttribute("name");
                var captchaImage = await RequestBytesWithCookiesAsync(
                    SiteLink + catchaImg.GetAttribute("src"), loginPage.Cookies, RequestType.Get, LoginUrl);
                configData.CaptchaImage.Value = captchaImage.Content;
            }
            else
            {
                _inputCaptcha = null;
                configData.CaptchaImage.Value = null;
            }

            var usernameInput = loginDocument.QuerySelector("input[maxlength=\"20\"]");
            _inputUsername = usernameInput.GetAttribute("name");
            var passwordInput = loginDocument.QuerySelector("input[maxlength=\"40\"]");
            _inputPassword = passwordInput.GetAttribute("name");
            return configData;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string>
            {
                {_inputUsername, configData.Username.Value},
                {_inputPassword, configData.Password.Value},
                {"keeplogged", "1"}
            };
            if (_inputCaptcha != null)
                pairs.Add(_inputCaptcha, configData.CaptchaText.Value);
            var result = await RequestLoginAndFollowRedirectAsync(LoginUrl, pairs, configData.CaptchaCookie.Value, true);
            await ConfigureIfOkAsync(
                result.Cookies, result.Content.Contains("logout.php"), () =>
                {
                    var loginParser = new HtmlParser();
                    var loginDocument = loginParser.ParseDocument(result.Content);
                    var errorMessage = loginDocument.QuerySelector("span.warning[id!=\"no-cookies\"]:has(br)").TextContent;
                    throw new ExceptionWithConfigData(errorMessage, configData);
                });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();
            var queryCollection = new NameValueCollection { { "order_by", "time" }, { "order_way", "desc" } };
            if (!string.IsNullOrWhiteSpace(query.ImdbID))
                queryCollection.Add("imdbid", query.ImdbID);
            else if (!string.IsNullOrWhiteSpace(searchString))
                queryCollection.Add("groupname", searchString);
            queryCollection.Add("groupname", searchString);
            var searchUrl = $"{BrowseUrl}?{queryCollection.GetQueryString()}";
            var results = await RequestStringWithCookiesAsync(searchUrl);
            if (results.IsRedirect)
            {
                // re login
                await GetConfigurationForSetup();
                await ApplyConfiguration(null);
                results = await RequestStringWithCookiesAsync(searchUrl);
            }

            var imdbRegEx = new Regex(@"tt(\d+)", RegexOptions.Compiled);
            var hParser = new HtmlParser();
            var resultDocument = hParser.ParseDocument(results.Content);
            try
            {
                var groups = resultDocument.QuerySelectorAll("div.browsePoster");
                foreach (var @group in groups)
                {
                    var groupPoster = @group.QuerySelector("img.classBrowsePoster");
                    var bannerUrl = new Uri(SiteLink + groupPoster.GetAttribute("src"));
                    long? imdbId = null;
                    var imdbLink = @group.QuerySelector("a[href*=\"www.imdb.com/title/tt\"]");
                    if (imdbLink != null)
                    {
                        var imdbMatch = imdbRegEx.Match(imdbLink.GetAttribute("href"));
                        imdbId = ParseUtil.CoerceLong(imdbMatch.Groups[1].Value);
                    }

                    var groupTitle = @group.QuerySelector("strong:has(a[title=\"View Torrent\"])").TextContent
                                          .Replace(" ]", "]");
                    var rows = @group.QuerySelectorAll("tr.group_torrent:has(a[href^=\"torrents.php?id=\"])");
                    foreach (var row in rows)
                    {
                        var release = new ReleaseInfo { MinimumRatio = 1, MinimumSeedTime = 72 * 60 * 60 };
                        var title = row.QuerySelector("a[href^=\"torrents.php?id=\"]");
                        var link = row.QuerySelector("a[href^=\"torrents.php?action=download\"]");
                        var added = row.QuerySelector("td:nth-child(3)");
                        var size = row.QuerySelector("td:nth-child(4)");
                        var grabs = row.QuerySelector("td:nth-child(6)");
                        var seeders = row.QuerySelector("td:nth-child(7)");
                        var leechers = row.QuerySelector("td:nth-child(8)");
                        release.Title = $"{groupTitle} {title.TextContent}";
                        release.Category = new List<int> { TorznabCatType.MoviesHD.ID };
                        release.Link = new Uri(SiteLink + link.GetAttribute("href"));
                        release.Comments = new Uri(SiteLink + title.GetAttribute("href"));
                        release.Guid = release.Link;
                        release.Size = ReleaseInfo.GetBytes(size.TextContent);
                        release.Seeders = ParseUtil.CoerceInt(seeders.TextContent);
                        release.Peers = ParseUtil.CoerceInt(leechers.TextContent) + release.Seeders;
                        release.Grabs = ParseUtil.CoerceLong(grabs.TextContent);
                        release.PublishDate = DateTimeUtil.FromTimeAgo(added.TextContent);
                        release.BannerUrl = bannerUrl;
                        release.Imdb = imdbId;
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
