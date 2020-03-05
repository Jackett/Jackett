using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Jackett.Common.Helpers;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    public class Hebits : BaseWebIndexer
    {
        private string LoginPostUrl => SiteLink + "takeloginAjax.php";
        private string SearchUrl => SiteLink + "browse.php?sort=4&type=desc";

        private new ConfigurationDataBasicLogin configData
        {
            get => (ConfigurationDataBasicLogin)base.configData;
            set => base.configData = value;
        }

        public Hebits(IIndexerConfigurationService configService, Utils.Clients.WebClient wc, Logger l, IProtectionService ps)
            : base(name: "Hebits",
                   description: "The Israeli Tracker",
                   link: "https://hebits.net/",
                   caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   downloadBase: "https://hebits.net/",
                   configData: new ConfigurationDataBasicLogin())
        {
            Encoding = Encoding.GetEncoding("windows-1255");
            Language = "he-il";
            Type = "private";
            AddCategoryMapping(19, TorznabCatType.MoviesSD);
            AddCategoryMapping(25, TorznabCatType.MoviesOther); // Israeli Content
            AddCategoryMapping(20, TorznabCatType.MoviesDVD);
            AddCategoryMapping(36, TorznabCatType.MoviesBluRay);
            AddCategoryMapping(27, TorznabCatType.MoviesHD);
            AddCategoryMapping(7, TorznabCatType.TVSD); // Israeli SDTV
            AddCategoryMapping(24, TorznabCatType.TVSD); // English SDTV
            AddCategoryMapping(1, TorznabCatType.TVHD); // Israel HDTV
            AddCategoryMapping(37, TorznabCatType.TVHD); // Israel HDTV
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string>
            {
                {"username", configData.Username.Value},
                {"password", configData.Password.Value}
            };

            // Get inital cookies
            CookieHeader = string.Empty;
            var result = await RequestLoginAndFollowRedirect(LoginPostUrl, pairs, CookieHeader, true, null, SiteLink);
            await ConfigureIfOK(
                result.Cookies, result.Content?.Contains("OK") == true, () =>
                {
                    var parser = new HtmlParser();
                    var dom = parser.ParseDocument(result.Content);
                    var errorMessage = dom.TextContent.Trim();
                    errorMessage += " attempts left. Please check your credentials.";
                    throw new ExceptionWithConfigData(errorMessage, configData);
                });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();
            var searchUrl = SearchUrl;
            if (!string.IsNullOrWhiteSpace(searchString))
                searchUrl += "&search=" + WebUtilityHelpers.UrlEncode(searchString, Encoding);
            var cats = MapTorznabCapsToTrackers(query);
            if (cats.Count > 0)
                searchUrl = cats.Aggregate(searchUrl, (url, cat) => $"{url}&c{cat}=1");
            var response = await RequestStringWithCookies(searchUrl);
            try
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(response.Content);
                var rows = dom.QuerySelectorAll(".browse > div > div");
                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800; // 48 hours
                    var qTitle = row.QuerySelector(".bTitle");
                    var titleParts = qTitle.TextContent.Split('/');
                    release.Title = titleParts.Length >= 2 ? titleParts[1].Trim() : titleParts[0].Trim();
                    var qDetailsLink = qTitle.QuerySelector("a[href^=\"details.php\"]");
                    release.Comments = new Uri(SiteLink + qDetailsLink.GetAttribute("href"));
                    release.Link = new Uri(SiteLink + row.QuerySelector("a[href^=\"download.php\"]").GetAttribute("href"));
                    release.Guid = release.Link;
                    var dateString = row.QuerySelector("div:last-child").TextContent.Trim();
                    var match = Regex.Match(dateString, @"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}");
                    if (match.Success)
                        release.PublishDate = DateTime.ParseExact(match.Value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    var sizeStr = row.QuerySelector(".bSize").TextContent.Trim();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);
                    release.Seeders = ParseUtil.CoerceInt(row.QuerySelector(".bUping").TextContent.Trim());
                    release.Peers = release.Seeders + ParseUtil.CoerceInt(row.QuerySelector(".bDowning").TextContent.Trim());
                    var files = row.QuerySelector("div.bFiles").ChildNodes.Last().TextContent.Trim();
                    release.Files = ParseUtil.CoerceInt(files);
                    var grabs = row.QuerySelector("div.bFinish").ChildNodes.Last().TextContent.Trim();
                    release.Grabs = ParseUtil.CoerceInt(grabs);
                    release.DownloadVolumeFactor = row.QuerySelector("img[src=\"/pic/free.jpg\"]") != null ? 0 : 1;
                    if (row.QuerySelector("img[src=\"/pic/triple.jpg\"]") != null)
                        release.UploadVolumeFactor = 3;
                    else if (row.QuerySelector("img[src=\"/pic/double.jpg\"]") != null)
                        release.UploadVolumeFactor = 2;
                    else
                        release.UploadVolumeFactor = 1;
                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.Content, ex);
            }

            return releases;
        }
    }
}
