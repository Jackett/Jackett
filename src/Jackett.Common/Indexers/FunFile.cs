using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
    public class FunFile : BaseWebIndexer
    {
        private string SearchUrl { get { return SiteLink + "browse.php"; } }
        private string LoginUrl { get { return SiteLink + "takelogin.php"; } }

        private new ConfigurationDataBasicLoginWithRSSAndDisplay configData
        {
            get { return (ConfigurationDataBasicLoginWithRSSAndDisplay)base.configData; }
            set { base.configData = value; }
        }

        public FunFile(IIndexerConfigurationService configService, WebClient w, Logger l, IProtectionService ps)
            : base(name: "FunFile",
                description: "A general tracker",
                link: "https://www.funfile.org/",
                caps: new TorznabCapabilities(),
                configService: configService,
                client: w,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLoginWithRSSAndDisplay())
        {
            Encoding = Encoding.GetEncoding("iso-8859-1");
            Language = "en-us";
            Type = "private";

            AddCategoryMapping(44, TorznabCatType.TVAnime); // Anime
            AddCategoryMapping(22, TorznabCatType.PC); // Applications
            AddCategoryMapping(43, TorznabCatType.AudioAudiobook); // Audio Books
            AddCategoryMapping(27, TorznabCatType.Books); // Ebook
            AddCategoryMapping(4,  TorznabCatType.PCGames); // Games
            AddCategoryMapping(40, TorznabCatType.OtherMisc); // Miscellaneous
            AddCategoryMapping(19, TorznabCatType.Movies); // Movies
            AddCategoryMapping(6,  TorznabCatType.Audio); // Music
            AddCategoryMapping(31, TorznabCatType.PCPhoneOther); // Portable
            AddCategoryMapping(7,  TorznabCatType.TV); // TV
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "login", "Login" },
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, LoginUrl);
            await ConfigureIfOK(result.Cookies, result.Content.Contains("logout.php"), () =>
            {
                CQ dom = result.Content;
                var errorMessage = dom["td.mf_content"].Html();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            var searchString = query.GetQueryString();
            var searchUrl = SearchUrl;
            var queryCollection = new NameValueCollection();
            queryCollection.Add("incldead", "1");
            queryCollection.Add("showspam", "1");

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                queryCollection.Add("search", searchString);
            }

            var cats = MapTorznabCapsToTrackers(query);
            string cat = "0";
            if (cats.Count == 1)
            {
                cat = cats[0];
            }
            queryCollection.Add("cat", cat);

            searchUrl += "?" + queryCollection.GetQueryString();

            var results = await RequestStringWithCookiesAndRetry(searchUrl);

            // Occasionally the cookies become invalid, login again if that happens
            if (results.IsRedirect)
            {
                await ApplyConfiguration(null);
                results = await RequestStringWithCookiesAndRetry(searchUrl);
            }

            try
            {
                CQ dom = results.Content;
                var rows = dom["table[cellpadding=2] > tbody > tr:has(td.row3)"];
                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 48 * 60 * 60;

                    var qRow = row.Cq();
                    var qCatLink = qRow.Find("a[href^=browse.php?cat=]").First();
                    var qDetailsLink = qRow.Find("a[href^=details.php?id=]").First();
                    var qSeeders = qRow.Find("td:eq(9)");
                    var qLeechers = qRow.Find("td:eq(10)");
                    var qDownloadLink = qRow.Find("a[href^=download.php]").First();
                    var qTimeAgo = qRow.Find("td:eq(5)");
                    var qSize = qRow.Find("td:eq(7)");

                    var catStr = qCatLink.Attr("href").Split('=')[1].Split('&')[0];
                    release.Category = MapTrackerCatToNewznab(catStr);

                    release.Link = new Uri(SiteLink + qDownloadLink.Attr("href"));
                    release.Title = qDetailsLink.Attr("title").Trim();
                    release.Comments = new Uri(SiteLink + qDetailsLink.Attr("href"));
                    release.Guid = release.Link;

                    var sizeStr = qSize.Text();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(qSeeders.Text());
                    release.Peers = ParseUtil.CoerceInt(qLeechers.Text()) + release.Seeders;

                    var dateStr = qTimeAgo.Text();
                    release.PublishDate = DateTimeUtil.FromTimeAgo(dateStr);

                    var files = qRow.Find("td:nth-child(4)").Text();
                    release.Files = ParseUtil.CoerceInt(files);

                    var grabs = qRow.Find("td:nth-child(9)").Text();
                    release.Grabs = ParseUtil.CoerceInt(grabs);

                    var ka = qRow.Next();
                    var DLFactor = ka.Find("table > tbody > tr:nth-child(3) > td:nth-child(2)").Text().Replace("X", "");
                    var ULFactor = ka.Find("table > tbody > tr:nth-child(3) > td:nth-child(1)").Text().Replace("X", "");
                    release.DownloadVolumeFactor = ParseUtil.CoerceDouble(DLFactor);
                    release.UploadVolumeFactor = ParseUtil.CoerceDouble(ULFactor);

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
