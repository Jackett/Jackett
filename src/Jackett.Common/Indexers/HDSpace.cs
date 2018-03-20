using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
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
    public class HDSpace : BaseWebIndexer
    {
        private string LoginUrl { get { return SiteLink + "index.php?page=login"; } }
        private string SearchUrl { get { return SiteLink + "index.php?page=torrents&"; } }

        private new ConfigurationDataBasicLogin configData
        {
            get { return (ConfigurationDataBasicLogin)base.configData; }
            set { base.configData = value; }
        }

        public HDSpace(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(name: "HD-Space",
                description: "Sharing The Universe",
                link: "https://hd-space.org/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                configService: configService,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLogin())
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";

            AddCategoryMapping(15, TorznabCatType.MoviesBluRay); // Movie / Blu-ray
            AddMultiCategoryMapping(TorznabCatType.MoviesHD,
                19, // Movie / 1080p
                41, // Movie / 4K UHD
                18, // Movie / 720p
                40, // Movie / Remux
                16 // Movie / HD-DVD
            );

            AddMultiCategoryMapping(TorznabCatType.TVHD,
                21, // TV Show / 720p HDTV
                22 // TV Show / 1080p HDTV
            );

            AddCategoryMapping(30, TorznabCatType.AudioLossless); // Music / Lossless
            AddCategoryMapping(31, TorznabCatType.AudioVideo); // Music / Videos

            AddMultiCategoryMapping(TorznabCatType.TVDocumentary,
                24, // TV Show / Documentary / 720p
                25 // TV Show / Documentary / 1080p
            );

            AddMultiCategoryMapping(TorznabCatType.XXX,
                33, // XXX / 720p
                34 // XXX / 1080p
            );

            AddCategoryMapping("37", TorznabCatType.PC);
            AddCategoryMapping("38", TorznabCatType.Other);
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var loginPage = await RequestStringWithCookies(LoginUrl, string.Empty);

            var pairs = new Dictionary<string, string> {
                { "uid", configData.Username.Value },
                { "pwd", configData.Password.Value }
            };

            // Send Post
            var response = await RequestLoginAndFollowRedirect(LoginUrl, pairs, loginPage.Cookies, true, null, LoginUrl);

            await ConfigureIfOK(response.Cookies, response.Content != null && response.Content.Contains("logout.php"), () =>
            {
                var errorStr = "You have {0} remaining login attempts";
                var remainingAttemptSpan = new Regex(string.Format(errorStr, "(.*?)")).Match(loginPage.Content).Groups[1].ToString();
                var attempts = Regex.Replace(remainingAttemptSpan, "<.*?>", String.Empty);
                var errorMessage = string.Format(errorStr, attempts);
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var searchString = query.GetQueryString();
            var searchUrl = SearchUrl;
            var queryCollection = new NameValueCollection();
            queryCollection.Add("active", "0");
            queryCollection.Add("options", "0");
            queryCollection.Add("category", string.Join(";", MapTorznabCapsToTrackers(query)));

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                queryCollection.Add("search", searchString);
            }

            searchUrl += queryCollection.GetQueryString();

            var response = await RequestStringWithCookiesAndRetry(searchUrl);
            var results = response.Content;

            try
            {
                CQ dom = results;
                var rows = dom["table.lista > tbody > tr"];
                foreach (var row in rows)
                {
                    // this tracker has horrible markup, find the result rows by looking for the style tag before each one
                    var prev = row.PreviousElementSibling;
                    if (prev == null || prev.NodeName.ToLowerInvariant() != "style") continue;

                    CQ qRow = row.Cq();
                    var release = new ReleaseInfo();

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;

                    var qLink = row.ChildElements.ElementAt(1).FirstElementChild.Cq();
                    release.Title = qLink.Text().Trim();
                    release.Comments = new Uri(SiteLink + qLink.Attr("href"));
                    release.Guid = release.Comments;

                    var qDownload = row.ChildElements.ElementAt(3).FirstElementChild.Cq();
                    release.Link = new Uri(SiteLink + qDownload.Attr("href"));

                    //"July 11, 2015, 13:34:09", "Today at 20:04:23"
                    var dateStr = row.ChildElements.ElementAt(4).Cq().Text().Trim();
                    if (dateStr.StartsWith("Today"))
                        release.PublishDate = DateTime.Today + TimeSpan.ParseExact(dateStr.Replace("Today at ", ""), "hh\\:mm\\:ss", CultureInfo.InvariantCulture);
                    else if (dateStr.StartsWith("Yesterday"))
                        release.PublishDate = DateTime.Today - TimeSpan.FromDays(1) + TimeSpan.ParseExact(dateStr.Replace("Yesterday at ", ""), "hh\\:mm\\:ss", CultureInfo.InvariantCulture);
                    else
                        release.PublishDate = DateTime.SpecifyKind(DateTime.ParseExact(dateStr, "MMMM dd, yyyy, HH:mm:ss", CultureInfo.InvariantCulture), DateTimeKind.Local);

                    var sizeStr = row.ChildElements.ElementAt(5).Cq().Text();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(row.ChildElements.ElementAt(7).Cq().Text());
                    release.Peers = ParseUtil.CoerceInt(row.ChildElements.ElementAt(8).Cq().Text()) + release.Seeders;

                    var grabs = qRow.Find("td:nth-child(10)").Text();
                    grabs = grabs.Replace("---", "0");
                    release.Grabs = ParseUtil.CoerceInt(grabs);

                    if (qRow.Find("img[title=\"FreeLeech\"]").Length >= 1)
                        release.DownloadVolumeFactor = 0;
                    else if (qRow.Find("img[src=\"images/sf.png\"]").Length >= 1) // side freeleech
                        release.DownloadVolumeFactor = 0;
                    else if (qRow.Find("img[title=\"Half FreeLeech\"]").Length >= 1)
                        release.DownloadVolumeFactor = 0.5;
                    else
                        release.DownloadVolumeFactor = 1;

                    release.UploadVolumeFactor = 1;

                    var qCat = qRow.Find("a[href^=\"index.php?page=torrents&category=\"]");
                    var cat = qCat.Attr("href").Split('=')[2];
                    release.Category = MapTrackerCatToNewznab(cat);

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results, ex);
            }
            return releases;
        }
    }
}
