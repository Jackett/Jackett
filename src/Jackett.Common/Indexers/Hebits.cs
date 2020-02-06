using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CsQuery;
using Jackett.Common.Helpers;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    public class Hebits : BaseWebIndexer
    {
        private string LoginUrl => $"{SiteLink}login.php";
        private string LoginPostUrl => $"{SiteLink}takeloginAjax.php";
        private string SearchUrl => $"{SiteLink}browse.php?sort=4&type=desc";

        private new ConfigurationDataBasicLogin configData
        {
            get => (ConfigurationDataBasicLogin)base.configData;
            set => base.configData = value;
        }

        public Hebits(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps) : base(
            "Hebits", description: "The Israeli Tracker", link: "https://hebits.net/",
            caps: TorznabUtil.CreateDefaultTorznabTVCaps(), configService: configService, client: wc, logger: l, p: ps,
            downloadBase: "https://hebits.net/", configData: new ConfigurationDataBasicLogin())
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
                {"username", configData.Username.Value}, {"password", configData.Password.Value}
            };

            // Get inital cookies
            CookieHeader = string.Empty;
            var result = await RequestLoginAndFollowRedirectAsync(LoginPostUrl, pairs, CookieHeader, true, null, SiteLink);
            await ConfigureIfOkAsync(
                result.Cookies, result.Content?.Contains("OK") == true, () =>
                {
                    CQ dom = result.Content;
                    var errorMessage = dom.Text().Trim();
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
                searchUrl += $"&search={WebUtilityHelpers.UrlEncode(searchString, Encoding)}";
            string.Format(SearchUrl, WebUtilityHelpers.UrlEncode(searchString, Encoding));
            var cats = MapTorznabCapsToTrackers(query);
            if (cats.Count > 0)
                foreach (var cat in cats)
                    searchUrl += $"&c{cat}=1";
            var response = await RequestStringWithCookiesAsync(searchUrl);
            try
            {
                CQ dom = response.Content;
                var qRows = dom[".browse > div > div"];
                foreach (var row in qRows)
                {
                    var release = new ReleaseInfo();
                    var qRow = row.Cq();
                    var debug = qRow.Html();
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800; // 48 hours
                    var qTitle = qRow.Find(".bTitle");
                    var titleParts = qTitle.Text().Split('/');
                    release.Title = titleParts.Length >= 2 ? titleParts[1].Trim() : titleParts[0].Trim();
                    var qDetailsLink = qTitle.Find("a[href^=\"details.php\"]");
                    release.Comments = new Uri(SiteLink + qDetailsLink.Attr("href"));
                    release.Link = new Uri(SiteLink + qRow.Find("a[href^=\"download.php\"]").Attr("href"));
                    release.Guid = release.Link;
                    var dateString = qRow.Find("div:last-child").Text().Trim();
                    var pattern = "\\d{4}-\\d{2}-\\d{2} \\d{2}:\\d{2}:\\d{2}";
                    var match = Regex.Match(dateString, pattern);
                    if (match.Success)
                        release.PublishDate = DateTime.ParseExact(
                            match.Value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    var sizeStr = qRow.Find(".bSize").Text();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);
                    release.Seeders = ParseUtil.CoerceInt(qRow.Find(".bUping").Text().Trim());
                    release.Peers = release.Seeders + ParseUtil.CoerceInt(qRow.Find(".bDowning").Text().Trim());
                    var files = qRow.Find("div.bFiles").Get(0).LastChild.ToString();
                    release.Files = ParseUtil.CoerceInt(files);
                    var grabs = qRow.Find("div.bFinish").Get(0).LastChild.ToString();
                    release.Grabs = ParseUtil.CoerceInt(grabs);
                    release.DownloadVolumeFactor = qRow.Find("img[src=\"/pic/free.jpg\"]").Length >= 1 ? 0 : 1;
                    release.UploadVolumeFactor = qRow.Find("img[src=\"/pic/triple.jpg\"]").Length >= 1 ? 3 : qRow.Find("img[src=\"/pic/double.jpg\"]").Length >= 1 ? 2 : 1;
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
