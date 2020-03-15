using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
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
        private string LoginUrl => SiteLink + "index.php?page=login";
        private string SearchUrl => SiteLink + "index.php?page=torrents&";

        private new ConfigurationDataBasicLogin configData
        {
            get => (ConfigurationDataBasicLogin)base.configData;
            set => base.configData = value;
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
            TorznabCaps.SupportsImdbMovieSearch = true;
            TorznabCaps.SupportsImdbTVSearch = true;

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
            var pairs = new Dictionary<string, string>
            {
                {"uid", configData.Username.Value},
                {"pwd", configData.Password.Value}
            };

            // Send Post
            var response = await RequestLoginAndFollowRedirect(LoginUrl, pairs, loginPage.Cookies, true, referer: LoginUrl);

            await ConfigureIfOK(response.Cookies, response.Content?.Contains("logout.php") == true, () =>
            {
                var errorStr = "You have {0} remaining login attempts";
                var remainingAttemptSpan = new Regex(string.Format(errorStr, "(.*?)"))
                                           .Match(loginPage.Content).Groups[1].ToString();
                var attempts = Regex.Replace(remainingAttemptSpan, "<.*?>", string.Empty);
                var errorMessage = string.Format(errorStr, attempts);
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var queryCollection = new NameValueCollection
            {
                {"active", "0"},
                {"category", string.Join(";", MapTorznabCapsToTrackers(query))}
            };

            if (query.IsImdbQuery)
            {
                queryCollection.Add("options", "2");
                queryCollection.Add("search", query.ImdbIDShort);
            }
            else
            {
                queryCollection.Add("options", "0");
                queryCollection.Add("search", query.GetQueryString());
            }

            var response = await RequestStringWithCookiesAndRetry(SearchUrl + queryCollection.GetQueryString());

            try
            {
                var resultParser = new HtmlParser();
                var searchResultDocument = resultParser.ParseDocument(response.Content);
                var rows = searchResultDocument.QuerySelectorAll("table.lista > tbody > tr");

                foreach (var row in rows)
                {
                    // this tracker has horrible markup, find the result rows by looking for the style tag before each one
                    var prev = row.PreviousElementSibling;
                    if (prev == null || !string.Equals(prev.NodeName, "style", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var release = new ReleaseInfo();
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800; // 48 hours

                    var qLink = row.Children[1].FirstElementChild;
                    release.Title = qLink.TextContent.Trim();
                    release.Comments = new Uri(SiteLink + qLink.GetAttribute("href"));
                    release.Guid = release.Comments;

                    var imdbLink = row.Children[1].QuerySelector("a[href*=imdb]");
                    if(imdbLink != null)
                        release.Imdb = ParseUtil.GetImdbID(imdbLink.GetAttribute("href").Split('/').Last());
                    
                    var qDownload = row.Children[3].FirstElementChild;
                    release.Link = new Uri(SiteLink + qDownload.GetAttribute("href"));

                    var dateStr = row.Children[4].TextContent.Trim();
                    //"July 11, 2015, 13:34:09", "Today|Yesterday at 20:04:23"
                    release.PublishDate = DateTimeUtil.FromUnknown(dateStr);
                    var sizeStr = row.Children[5].TextContent;
                    release.Size = ReleaseInfo.GetBytes(sizeStr);
                    release.Seeders = ParseUtil.CoerceInt(row.Children[7].TextContent);
                    release.Peers = ParseUtil.CoerceInt(row.Children[8].TextContent) + release.Seeders;
                    var grabs = row.QuerySelector("td:nth-child(10)").TextContent;
                    grabs = grabs.Replace("---", "0");
                    release.Grabs = ParseUtil.CoerceInt(grabs);
                    if (row.QuerySelector("img[title=\"FreeLeech\"]") != null)
                        release.DownloadVolumeFactor = 0;
                    else if (row.QuerySelector("img[src=\"images/sf.png\"]") != null) // side freeleech
                        release.DownloadVolumeFactor = 0;
                    else if (row.QuerySelector("img[title=\"Half FreeLeech\"]") != null)
                        release.DownloadVolumeFactor = 0.5;
                    else
                        release.DownloadVolumeFactor = 1;
                    release.UploadVolumeFactor = 1;
                    var qCat = row.QuerySelector("a[href^=\"index.php?page=torrents&category=\"]");
                    var cat = qCat.GetAttribute("href").Split('=')[2];
                    release.Category = MapTrackerCatToNewznab(cat);
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
