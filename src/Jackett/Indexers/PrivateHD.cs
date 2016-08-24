using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jackett.Models;
using Newtonsoft.Json.Linq;
using NLog;
using Jackett.Utils;
using CsQuery;
using System.Web;
using Jackett.Services;
using Jackett.Utils.Clients;
using Jackett.Models.IndexerConfig;
using System.Globalization;

namespace Jackett.Indexers
{
    public class PrivateHD : BaseIndexer, IIndexer
    {
        private string LoginUrl { get { return SiteLink + "auth/login"; } }
        private string SearchUrl { get { return SiteLink + "torrents?in=1&type={0}&search={1}"; } }

        new ConfigurationDataBasicLogin configData
        {
            get { return (ConfigurationDataBasicLogin)base.configData; }
            set { base.configData = value; }
        }

        public PrivateHD(IIndexerManagerService i, Logger l, IWebClient c, IProtectionService ps)
            : base(name: "PrivateHD",
                description: "BitTorrent site for High Quality, High Definition (HD) movies and TV Shows",
                link: "https://privatehd.to/",
                caps: new TorznabCapabilities(),
                manager: i,
                client: c,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLogin())
        {
            AddCategoryMapping(1, TorznabCatType.Movies);
            AddCategoryMapping(2, TorznabCatType.TV);
            AddCategoryMapping(2, TorznabCatType.TVSD);
            AddCategoryMapping(2, TorznabCatType.TVHD);
            AddCategoryMapping(3, TorznabCatType.Audio);
        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);
            var loginPage = await RequestStringWithCookies(LoginUrl, string.Empty);
            CQ loginDom = loginPage.Content;
            string token = loginDom["input[name='_token']"].Val();
            var pairs = new Dictionary<string, string> {
                { "_token", token },
                { "email_username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "remember", "1" }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, loginPage.Cookies, true, null, LoginUrl);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("auth/logout"), () =>
            {
                CQ dom = result.Content;
                var messageEl = dom[".form-error"];
                var errorMessage = messageEl.Text().Trim();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var categoryMapping = MapTorznabCapsToTrackers(query).Distinct();
            string category = "0"; // Aka all
            if (categoryMapping.Count() == 1)
            {
                category = categoryMapping.First();
            }

            var episodeSearchUrl = string.Format(SearchUrl, category, HttpUtility.UrlEncode(query.GetQueryString()));
            var response = await RequestStringWithCookiesAndRetry(episodeSearchUrl);

            try
            {
                CQ dom = response.Content;
                var rows = dom["table.table-condensed.table-striped.table-bordered:first"].Find("tbody > tr");

                foreach (var row in rows)
                {
                    CQ qRow = row.Cq();
                    var release = new ReleaseInfo();

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;

                    release.Title = qRow.Find("a[class='torrent-filename']").Text().Trim();
                    release.Comments = new Uri(qRow.Find("a[class='torrent-filename']").Attr("href"));
                    release.Guid = release.Comments;

                    release.Link = new Uri(qRow.Find("a[class='torrent-download-icon']").Attr("href"));

                    //05 Aug 2016 01:08
                    var dateString = row.ChildElements.ElementAt(3).Cq().Find("span").Attr("title");
                    TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                    DateTime pubDateEastern = DateTime.SpecifyKind(DateTime.ParseExact(dateString, "dd MMM yyyy HH:mm", CultureInfo.InvariantCulture), DateTimeKind.Unspecified);
                    release.PublishDate = TimeZoneInfo.ConvertTimeToUtc(pubDateEastern, easternZone).ToLocalTime();
                    
                    var sizeStr = row.ChildElements.ElementAt(5).Cq().Text().Trim();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(row.ChildElements.ElementAt(6).Cq().Text().Trim());
                    release.Peers = ParseUtil.CoerceInt(row.ChildElements.ElementAt(7).Cq().Text().Trim()) + release.Seeders;

                    int cat = 0;
                    var catElement = qRow.Find("i[class$='torrent-icon']");

                    if (catElement.Length == 1)
                    {
                        string catName = catElement.Attr("title").Trim();

                        if (catName.StartsWith("TV"))
                        {
                            cat = TvCategoryParser.ParseTvShowQuality(release.Title);
                        }
                        if (catName.StartsWith("Music")) { cat = TorznabCatType.Audio.ID; }
                        if (catName.StartsWith("Movie")) { cat = TorznabCatType.Movies.ID; }
                    }

                    release.Category = cat;
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
