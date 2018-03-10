using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    public class BeyondHD : BaseWebIndexer
    {
        private string SearchUrl { get { return SiteLink + "browse.php?searchin=title&incldead=0&"; } }

        private new ConfigurationDataLoginLink configData
        {
            get { return (ConfigurationDataLoginLink)base.configData; }
            set { base.configData = value; }
        }

        public BeyondHD(IIndexerConfigurationService configService, WebClient w, Logger l, IProtectionService ps)
            : base(name: "BeyondHD",
                description: "Without BeyondHD, your HDTV is just a TV",
                link: "https://beyond-hd.me/",
                caps: new TorznabCapabilities(),
                configService: configService,
                client: w,
                logger: l,
                p: ps,
                configData: new ConfigurationDataLoginLink())
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";

            configData.DisplayText.Value = "Go to the general tab of your BeyondHD user profile and create/copy the Login Link.";

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
                101, // Internal / FraMeSToR 4K REMUX
                61, // Internal / HDX REMUX
                86, // Internal / SC4R
                95, // Nightripper 1080p
                96, // Nightripper 720p
                98 // Nightripper MicroHD
            );

            AddMultiCategoryMapping(TorznabCatType.TVHD,
                40, // TV Show / Blu-ray
                44, // TV Show / Encodes
                48, // TV Show / HDTV
                89, // TV Show / Packs
                46, // TV Show / Remux
                45, // TV Show / WEB-DL
                97 //  Nightripper TV Show Encodes
            );

            AddCategoryMapping(36, TorznabCatType.AudioLossless); // Music / Lossless
            AddCategoryMapping(69, TorznabCatType.AudioMP3); // Music / MP3
            AddMultiCategoryMapping(TorznabCatType.AudioVideo,
                55, // Music / 1080p/i
                56, // Music / 720p
                42 // Music / Blu-ray
            );
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var result = await RequestStringWithCookies(configData.LoginLink.Value);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("Welcome Back"), () =>
            {
                var errorMessage = result.Content;
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

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                Regex ReplaceRegex = new Regex("[^a-zA-Z0-9]+");
                searchString = "%" + ReplaceRegex.Replace(searchString, "%") + "%";
                searchString = Regex.Replace(searchString, @"(%\d{3,4})[ip](%)", "$1$2"); // remove i/p from resolution tags (see #835)
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
                    var torrentId = qLink.Attr("href").Split('=').Last();

                    var descCol = row.ChildElements.ElementAt(3);
                    var qCommentLink = descCol.FirstChild.Cq();
                    release.Title = qCommentLink.Text();
                    release.Comments = new Uri(SiteLink + "/" + qCommentLink.Attr("href"));
                    release.Guid = release.Comments;
                    release.Link = new Uri($"{SiteLink}download.php?torrent={torrentId}");

                    var dateStr = descCol.ChildElements.Last().Cq().Text().Split('|').Last().ToLowerInvariant().Replace("ago.", "").Trim();
                    release.PublishDate = DateTimeUtil.FromTimeAgo(dateStr);

                    var sizeStr = row.ChildElements.ElementAt(7).Cq().Text();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(row.ChildElements.ElementAt(9).Cq().Text());
                    release.Peers = ParseUtil.CoerceInt(row.ChildElements.ElementAt(10).Cq().Text()) + release.Seeders;

                    var files = qRow.Find("td:nth-child(6)").Text();
                    release.Files = ParseUtil.CoerceInt(files);

                    var grabs = qRow.Find("td:nth-child(9) > a").Get(0).FirstChild.ToString();
                    release.Grabs = ParseUtil.CoerceInt(grabs);

                    release.DownloadVolumeFactor = 0; // ratioless
                    release.UploadVolumeFactor = 1;

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
