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
                caps: new TorznabCapabilities(),
                manager: i,
                client: w,
                logger: l,
                p: ps,
                configData: new ConfigurationDataCookie())
        {
            AddCategoryMapping(37, TorznabCatType.MoviesBluRay); // Movie / Blu-ray
            AddMultiCategoryMapping(TorznabCatType.Movies3D,
                71,  // Movie / 3D
                83 // FraMeSToR 3D
            );
            AddMultiCategoryMapping(TorznabCatType.MoviesHD,
                77, // Movie / 1080p/i
                94, // Movie / 4K
                78, // Movie / 720p
                54, // Movie / MP4
                17, // Movie / Remux
                50, // Internal / FraMeSToR 1080p
                75, // Internal / FraMeSToR 720p
                49, // Internal / FraMeSToR REMUX
                61, // Internal / HDX REMUX
                86 // Internal / SC4R
            );

            AddMultiCategoryMapping(TorznabCatType.TVHD,
                40, // TV Show / Blu-ray
                44, // TV Show / Encodes
                48, // TV Show / HDTV
                89, // TV Show / Packs
                46, // TV Show / Remux
                45 // TV Show / WEB-DL
            );

            AddCategoryMapping(36, TorznabCatType.AudioLossless); // Music / Lossless
            AddCategoryMapping(69, TorznabCatType.AudioMP3); // Music / MP3
            AddMultiCategoryMapping(TorznabCatType.AudioVideo,
                55, // Music / 1080p/i
                56, // Music / 720p
                42 // Music / Blu-ray
            );


        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);

            var response = await webclient.GetString(new Utils.Clients.WebRequest()
            {
                Url = SiteLink,
                Cookies = configData.Cookie.Value
            });

            await ConfigureIfOK(configData.Cookie.Value, response.Content.Contains("logout.php"), () =>
            {
                CQ dom = response.Content;
                throw new ExceptionWithConfigData("Invalid cookie header", configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            var searchString = query.GetQueryString();
            var searchUrl = SearchUrl;
            var queryCollection = new NameValueCollection();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                queryCollection.Add("search", searchString);
            }

            foreach (var cat in MapTorznabCapsToTrackers(query))
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
