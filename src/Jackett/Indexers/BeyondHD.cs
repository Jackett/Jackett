using CsQuery;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Jackett.Models.IndexerConfig;
using System.Collections.Specialized;

namespace Jackett.Indexers
{
    public class BeyondHD : BaseIndexer, IIndexer
    {
        private string SearchUrl { get { return SiteLink + "browse.php?searchin=title&incldead=0&"; } }
        private string DownloadUrl { get { return SiteLink + "download.php?torrent={0}"; } }

        new ConfigurationDataCookie configData
        {
            get { return (ConfigurationDataCookie)base.configData; }
            set { base.configData = value; }
        }

        public BeyondHD(IIndexerManagerService i, Logger l, IWebClient w, IProtectionService ps)
            : base(name: "BeyondHD",
                description: "Without BeyondHD, your HDTV is just a TV",
                link: "https://beyondhd.me/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                client: w,
                logger: l,
                p: ps,
                configData: new ConfigurationDataCookie())
        {
            AddCategoryMapping("40,44,48,89,46,45", TorznabCatType.TV);
            AddCategoryMapping("40", TorznabCatType.TVHD);
            AddCategoryMapping("44", TorznabCatType.TVHD);
            AddCategoryMapping("48", TorznabCatType.TVHD);
            AddCategoryMapping("46", TorznabCatType.TVHD);
            AddCategoryMapping("45", TorznabCatType.TVHD);
            AddCategoryMapping("44", TorznabCatType.TVSD);
            AddCategoryMapping("46", TorznabCatType.TVSD);
            AddCategoryMapping("45", TorznabCatType.TVSD);

            AddCategoryMapping("41,77,71,94,78,37,54,17", TorznabCatType.Movies);
            AddCategoryMapping("77", TorznabCatType.MoviesHD);
            AddCategoryMapping("71", TorznabCatType.Movies3D);
            AddCategoryMapping("78", TorznabCatType.MoviesHD);
            AddCategoryMapping("37", TorznabCatType.MoviesBluRay);
            AddCategoryMapping("54", TorznabCatType.MoviesHD);

            AddCategoryMapping("55,56,42,36,69", TorznabCatType.Audio);
            AddCategoryMapping("36", TorznabCatType.AudioLossless);
            AddCategoryMapping("69", TorznabCatType.AudioMP3);
        }

        public async Task ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);

            var response = await webclient.GetString(new Utils.Clients.WebRequest()
            {
                Url = SiteLink,
                Cookies = configData.Cookie.Value
            });

            await ConfigureIfOK(CookieHeader, response.Content.Contains("logout.php"), () =>
            {
                CQ dom = response.Content;
                throw new ExceptionWithConfigData("Invalid cookie header", configData);
            });
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            var searchString = query.SanitizedSearchTerm + " " + query.GetEpisodeSearchString();
            var searchUrl = SearchUrl;
            var queryCollection = new NameValueCollection();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                queryCollection.Add("search", searchString);
            }

            var cats = new List<string>();
            foreach (var cat in MapTorznabCapsToTrackers(query))
            {
                cats.AddRange(cat.Split(','));
            }
            foreach (var cat in cats.Distinct())
            {
                queryCollection.Add("c" + cat, "1");
            }

            searchUrl += queryCollection.GetQueryString();

            var results = await RequestStringWithCookiesAndRetry(searchUrl);
            await FollowIfRedirect(results);
            try
            {
                CQ dom = results.Content;
                var rows = dom["table.torrenttable > tbody > tr.browse_color"];
                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;

                    var qRow = row.Cq();

                    var catStr = row.ChildElements.ElementAt(0).FirstElementChild.GetAttribute("href").Split('=')[1];
                    release.Category = MapTrackerCatToNewznab(catStr);

                    var qLink = row.ChildElements.ElementAt(2).FirstChild.Cq();
                    release.Link = new Uri(SiteLink + "/" + qLink.Attr("href"));
                    var torrentID = qLink.Attr("href").Split('=').Last();

                    var descCol = row.ChildElements.ElementAt(3);
                    var qCommentLink = descCol.FirstChild.Cq();
                    release.Title = qCommentLink.Text();
                    release.Description = release.Title;
                    release.Comments = new Uri(SiteLink + "/" + qCommentLink.Attr("href"));
                    release.Guid = release.Comments;

                    var dateStr = descCol.ChildElements.Last().Cq().Text().Split('|').Last().ToLowerInvariant().Replace("ago.", "").Trim();
                    release.PublishDate = DateTimeUtil.FromTimeAgo(dateStr);

                    var sizeStr = row.ChildElements.ElementAt(7).Cq().Text();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(row.ChildElements.ElementAt(9).Cq().Text());
                    release.Peers = ParseUtil.CoerceInt(row.ChildElements.ElementAt(10).Cq().Text()) + release.Seeders;

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
