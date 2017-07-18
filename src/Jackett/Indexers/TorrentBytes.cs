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
    public class TorrentBytes : BaseWebIndexer
    {
        private string BrowseUrl { get { return SiteLink + "browse.php"; } }
        private string LoginUrl { get { return SiteLink + "takelogin.php"; } }

        new ConfigurationDataBasicLogin configData
        {
            get { return (ConfigurationDataBasicLogin)base.configData; }
            set { base.configData = value; }
        }

        public TorrentBytes(IIndexerConfigurationService configService, IWebClient wc, Logger l, IProtectionService ps)
            : base(name: "TorrentBytes",
                description: "A decade of torrentbytes",
                link: "https://www.torrentbytes.net/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                configService: configService,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLogin())
        {
            Encoding = Encoding.GetEncoding("iso-8859-1");
            Language = "en-us";
            Type = "semi-private";

            AddCategoryMapping(41, TorznabCatType.TV);
            AddCategoryMapping(33, TorznabCatType.TVSD);
            AddCategoryMapping(38, TorznabCatType.TVHD);
            AddCategoryMapping(32, TorznabCatType.TVSD);
            AddCategoryMapping(37, TorznabCatType.TVSD);
            AddCategoryMapping(44, TorznabCatType.TVSD);

            AddCategoryMapping(40, TorznabCatType.Movies);
            AddCategoryMapping(19, TorznabCatType.MoviesSD);
            AddCategoryMapping(5, TorznabCatType.MoviesHD);
            AddCategoryMapping(20, TorznabCatType.MoviesSD);
            AddCategoryMapping(28, TorznabCatType.MoviesForeign);
            AddCategoryMapping(45, TorznabCatType.MoviesSD);

            AddCategoryMapping(43, TorznabCatType.Audio);
            AddCategoryMapping(48, TorznabCatType.AudioLossless);
            AddCategoryMapping(6, TorznabCatType.AudioMP3);
            AddCategoryMapping(46, TorznabCatType.Movies);

            AddCategoryMapping(1, TorznabCatType.PC);
            AddCategoryMapping(2, TorznabCatType.PC);
            AddCategoryMapping(23, TorznabCatType.TVAnime);
            AddCategoryMapping(21, TorznabCatType.XXX);
            AddCategoryMapping(9, TorznabCatType.XXXXviD);
            AddCategoryMapping(39, TorznabCatType.XXXx264);
            AddCategoryMapping(29, TorznabCatType.XXXXviD);
            AddCategoryMapping(24, TorznabCatType.XXXImageset);
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "returnto", "/" },
                { "login", "Log in!" }
            };

            var loginPage = await RequestStringWithCookies(SiteLink, string.Empty);

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, loginPage.Cookies, true, SiteLink, SiteLink);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("logout.php"), () =>
            {
                CQ dom = result.Content;
                var messageEl = dom["td.embedded"].First();
                var errorMessage = messageEl.Text();
                if (string.IsNullOrWhiteSpace(errorMessage))
                    errorMessage = result.Content;
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();
            var searchUrl = BrowseUrl;
            var trackerCats = MapTorznabCapsToTrackers(query);
            var queryCollection = new NameValueCollection();

            // Tracker can only search OR return things in categories
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                queryCollection.Add("search", searchString);
                queryCollection.Add("cat", "0");
                queryCollection.Add("sc", "1");
            }
            else
            {
                foreach (var cat in MapTorznabCapsToTrackers(query))
                {
                    queryCollection.Add("c" + cat, "1");
                }

                queryCollection.Add("incldead", "0");
            }

            searchUrl += "?" + queryCollection.GetQueryString();

            // 15 results per page - really don't want to call the server twice but only 15 results per page is a bit crap!
            await ProcessPage(releases, searchUrl);
            await ProcessPage(releases, searchUrl + "&page=1");
            return releases;
        }

        private async Task ProcessPage(List<ReleaseInfo> releases, string searchUrl)
        {
            var response = await RequestStringWithCookiesAndRetry(searchUrl, null, BrowseUrl);
            // On IP change the cookies become invalid, login again and retry
            if (response.IsRedirect)
            {
                await ApplyConfiguration(null);
                response = await RequestStringWithCookiesAndRetry(searchUrl, null, BrowseUrl);
            }

            var results = response.Content;
            try
            {
                CQ dom = results;

                var rows = dom["table > tbody:has(tr > td.colhead) > tr:not(:has(td.colhead))"];
                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();

                    var link = row.Cq().Find("td:eq(1) a:eq(1)").First();
                    release.Guid = new Uri(SiteLink + link.Attr("href"));
                    release.Comments = release.Guid;
                    release.Title = link.Get(0).FirstChild.ToString();
                    release.Description = release.Title;

                    // If we search an get no results, we still get a table just with no info.
                    if (string.IsNullOrWhiteSpace(release.Title))
                    {
                        break;
                    }

                    // Check if the release has been assigned a category
                    if (row.Cq().Find("td:eq(0) a").Length > 0)
                    {
                        var cat = row.Cq().Find("td:eq(0) a").First().Attr("href").Substring(15);
                        release.Category = MapTrackerCatToNewznab(cat);
                    }

                    var qLink = row.Cq().Find("td:eq(1) a").First();
                    release.Link = new Uri(SiteLink + qLink.Attr("href"));

                    var added = row.Cq().Find("td:eq(4)").First().Text().Trim();
                    release.PublishDate = DateTime.ParseExact(added, "yyyy-MM-ddHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToLocalTime();

                    var sizeStr = row.Cq().Find("td:eq(6)").First().Text().Trim();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(row.Cq().Find("td:eq(8)").First().Text().Trim());
                    release.Peers = ParseUtil.CoerceInt(row.Cq().Find("td:eq(9)").First().Text().Trim()) + release.Seeders;

                    var files = row.Cq().Find("td:nth-child(3)").Text();
                    release.Files = ParseUtil.CoerceInt(files);

                    var grabs = row.Cq().Find("td:nth-child(8)").Text();
                    if (grabs != "----")
                        release.Grabs = ParseUtil.CoerceInt(grabs);

                    if (row.Cq().Find("font[color=\"green\"]:contains(F):contains(L)").Length >= 1)
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
        }
    }
}
