using System;
using System.Collections.Generic;
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
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers.Abstract
{
    public abstract class AvistazTracker : BaseWebIndexer
    {
        private string LoginUrl => $"{SiteLink}auth/login";
        private string SearchUrl => $"{SiteLink}torrents?in=1&type={{0}}&search={{1}}";

        private new ConfigurationDataBasicLogin configData
        {
            get => (ConfigurationDataBasicLogin)base.configData;
            set => base.configData = value;
        }

        // hook to adjust the search term
        protected virtual string GetSearchTerm(TorznabQuery query) => query.GetQueryString();

        public AvistazTracker(IIndexerConfigurationService configService, WebClient webClient, Logger logger,
                              IProtectionService protectionService, string name, string desc, string link) : base(
            name, description: desc, link: link, caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
            configService: configService, client: webClient, logger: logger, p: protectionService,
            configData: new ConfigurationDataBasicLogin())
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            AddCategoryMapping(1, TorznabCatType.Movies);
            AddCategoryMapping(1, TorznabCatType.MoviesForeign);
            AddCategoryMapping(1, TorznabCatType.MoviesHD);
            AddCategoryMapping(1, TorznabCatType.MoviesSD);
            AddCategoryMapping(2, TorznabCatType.TV);
            AddCategoryMapping(3, TorznabCatType.Audio);
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var loginPage = await RequestStringWithCookiesAsync(LoginUrl, string.Empty);
            var token = new Regex("<meta name=\"_token\" content=\"(.*?)\">").Match(loginPage.Content).Groups[1].ToString();
            var pairs = new Dictionary<string, string>
            {
                {"_token", token},
                {"email_username", configData.Username.Value},
                {"password", configData.Password.Value},
                {"remember", "1"}
            };
            var result = await RequestLoginAndFollowRedirectAsync(LoginUrl, pairs, loginPage.Cookies, true, null, LoginUrl);
            await ConfigureIfOkAsync(
                result.Cookies, result.Content?.Contains("auth/logout") == true, () =>
                {
                    CQ dom = result.Content;
                    var messageEl = dom[".form-error"];
                    var errorMessage = messageEl.Text().Trim();
                    throw new ExceptionWithConfigData(errorMessage, configData);
                });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var categoryMapping = MapTorznabCapsToTrackers(query).Distinct();
            var category = "0"; // Aka all
            if (categoryMapping.Count() == 1)
                category = categoryMapping.First();
            var episodeSearchUrl = string.Format(SearchUrl, category, WebUtility.UrlEncode(GetSearchTerm(query)));
            var response = await RequestStringWithCookiesAndRetryAsync(episodeSearchUrl);
            if (response.IsRedirect)
            {
                // re-login
                await ApplyConfiguration(null);
                response = await RequestStringWithCookiesAndRetryAsync(episodeSearchUrl);
            }

            try
            {
                CQ dom = response.Content;
                var rows = dom["table:has(thead) > tbody > tr"];
                foreach (var row in rows)
                {
                    var qRow = row.Cq();
                    var release = new ReleaseInfo
                    {
                        MinimumRatio = 1,
                        MinimumSeedTime = 172800 // 48 hours
                    };
                    var qLink = qRow.Find("a.torrent-filename");
                    ;
                    release.Title = qLink.Text().Trim();
                    release.Comments = new Uri(qLink.Attr("href"));
                    release.Guid = release.Comments;
                    var qDownload = qRow.Find("a.torrent-download-icon");
                    ;
                    release.Link = new Uri(qDownload.Attr("href"));
                    var dateStr = qRow.Find("td:eq(3) > span").Text().Trim();
                    release.PublishDate = DateTimeUtil.FromTimeAgo(dateStr);
                    var sizeStr = qRow.Find("td:eq(5) > span").Text().Trim();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);
                    release.Seeders = ParseUtil.CoerceInt(qRow.Find("td:eq(6)").Text().Trim());
                    release.Peers = ParseUtil.CoerceInt(qRow.Find("td:eq(7)").Text().Trim()) + release.Seeders;
                    var cat = row.Cq().Find("td:eq(0) i").First().Attr("class").Replace("torrent-icon", string.Empty)
                                 .Replace("fa fa-", string.Empty).Replace("film", "1").Replace("tv", "2")
                                 .Replace("music", "3").Replace("text-pink", string.Empty);
                    release.Category = MapTrackerCatToNewznab(cat.Trim());
                    var grabs = row.Cq().Find("td:nth-child(9)").Text();
                    release.Grabs = ParseUtil.CoerceInt(grabs);
                    release.DownloadVolumeFactor = row.Cq().Find("i.fa-star").Any() ? 0 : row.Cq().Find("i.fa-star-half-o").Any() ? 0.5 : 1;
                    release.UploadVolumeFactor = row.Cq().Find("i.fa-diamond").Any() ? 2 : 1;
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
