using CsQuery;
using Jackett.Indexers;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI.WebControls;
using Jackett.Models.IndexerConfig;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace Jackett.Indexers
{
    public class Hounddawgs : BaseWebIndexer
    {
        private string LoginUrl { get { return SiteLink + "login.php"; } }
        private string SearchUrl { get { return SiteLink + "torrents.php"; } }

        new NxtGnConfigurationData configData
        {
            get { return (NxtGnConfigurationData)base.configData; }
            set { base.configData = value; }
        }

        public Hounddawgs(IIndexerConfigurationService configService, IWebClient c, Logger l, IProtectionService ps)
            : base(name: "Hounddawgs",
                description: "A danish closed torrent tracker",
                link: "https://hounddawgs.org/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                configService: configService,
                client: c,
                logger: l,
                p: ps,
                configData: new NxtGnConfigurationData())
        {
            Encoding = Encoding.GetEncoding("UTF-8");
            Language = "da-dk";
            Type = "private";

            AddCategoryMapping(68, TorznabCatType.Movies3D, "3D");
            AddCategoryMapping(80, TorznabCatType.PCPhoneAndroid, "Appz / Android");
            AddCategoryMapping(86, TorznabCatType.PC0day, "Appz / Div");
            AddCategoryMapping(71, TorznabCatType.PCPhoneIOS, "Appz / iOS");
            AddCategoryMapping(70, TorznabCatType.PCMac, "Appz / Mac");
            AddCategoryMapping(69, TorznabCatType.PC0day, "Appz / PC");
            AddCategoryMapping(72, TorznabCatType.AudioAudiobook, "Audio Books");
            AddCategoryMapping(82, TorznabCatType.MoviesBluRay, "BluRay/REMUX");
            AddCategoryMapping(78, TorznabCatType.Books, "Books");
            AddCategoryMapping(87, TorznabCatType.Other, "Cover");
            AddCategoryMapping(90, TorznabCatType.MoviesDVD, "DK DVDr");
            AddCategoryMapping(89, TorznabCatType.TVHD, "DK HD");
            AddCategoryMapping(91, TorznabCatType.TVSD, "DK SD");
            AddCategoryMapping(92, TorznabCatType.TVHD, "DK TV HD");
            AddCategoryMapping(93, TorznabCatType.TVSD, "DK TV SD");
            AddCategoryMapping(83, TorznabCatType.Other, "ELearning");
            AddCategoryMapping(84, TorznabCatType.Movies, "Film Boxset");
            AddCategoryMapping(81, TorznabCatType.MoviesSD, "Film CAM/TS");
            AddCategoryMapping(60, TorznabCatType.MoviesDVD, "Film DVDr");
            AddCategoryMapping(59, TorznabCatType.MoviesHD, "Film HD");
            AddCategoryMapping(73, TorznabCatType.MoviesSD, "Film SD");
            AddCategoryMapping(77, TorznabCatType.MoviesOther, "Film Tablet");
            AddCategoryMapping(61, TorznabCatType.Audio, "Musik");
            AddCategoryMapping(76, TorznabCatType.AudioVideo, "MusikVideo/Koncert");
            AddCategoryMapping(75, TorznabCatType.Console, "Spil / Konsol");
            AddCategoryMapping(79, TorznabCatType.PCMac, "Spil / Mac");
            AddCategoryMapping(64, TorznabCatType.PCGames, "Spil / PC");
            AddCategoryMapping(85, TorznabCatType.TV, "TV Boxset");
            AddCategoryMapping(58, TorznabCatType.TVSD, "TV DVDr");
            AddCategoryMapping(57, TorznabCatType.TVHD, "TV HD");
            AddCategoryMapping(74, TorznabCatType.TVSD, "TV SD");
            AddCategoryMapping(94, TorznabCatType.TVOTHER, "TV Tablet");
            AddCategoryMapping(67, TorznabCatType.XXX, "XXX");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "keeplogged", "1" },
                { "login", "Login" }

            };
            // Get inital cookies
            var response = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, "https://hounddawgs.org/");

            await ConfigureIfOK(response.Cookies, response.Content != null && response.Content.Contains("Velkommen til"), () =>
                {
                    CQ dom = response.Content;
                    var messageEl = dom["inputs"];
                    var errorMessage = messageEl.Text().Trim();
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

            queryCollection.Add("order_by", "time");
            queryCollection.Add("order_way", "desc");

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                queryCollection.Add("searchstr", searchString);
            }

            foreach (var cat in MapTorznabCapsToTrackers(query))
            {
                queryCollection.Add("filter_cat[" + cat + "]", "1");
            }

            searchUrl += "?" + queryCollection.GetQueryString();
            var results = await RequestStringWithCookiesAndRetry(searchUrl);
            if (results.Content.Contains("Din søgning gav intet resultat."))
            {
                return releases;
            }
            try
            {
                CQ dom = results.Content;

                var rows = dom["#torrent_table > tbody > tr"].ToArray();

                foreach (var row in rows.Skip(1))
                {
                    try
                    {
                        var qRow = row.Cq();
                        var release = new ReleaseInfo();
                        release.MinimumRatio = 1;
                        release.MinimumSeedTime = 172800;

                        var qCat = row.ChildElements.ElementAt(0).ChildElements.ElementAt(0).Cq();
                        var catUrl = qCat.Attr("href");
                        var cat = catUrl.Substring(catUrl.LastIndexOf('[') + 1).Trim(']');
                        release.Category = MapTrackerCatToNewznab(cat);

                        // support both date format (profile settings)
                        var qAdded = row.ChildElements.ElementAt(4).ChildElements.ElementAt(0).Cq();
                        var addedStr = qAdded.Attr("title");
                        if (!addedStr.Contains(","))
                            addedStr = qAdded.Text();
                        release.PublishDate = DateTime.ParseExact(addedStr, "MMM dd yyyy, HH:mm", CultureInfo.InvariantCulture);

                        var overlayScript = qRow.Find("script:contains(\"var overlay\")").Text();
                        var overlayHtmlEscaped = overlayScript.Substring(overlayScript.IndexOf('=') + 1).Trim().Trim('"');
                        var overlayHtml = Regex.Unescape(overlayHtmlEscaped);
                        CQ qOverlay = overlayHtml;
                        var title = qOverlay.Find("td.overlay > strong");
                        var banner = qOverlay.Find("td.leftOverlay > img").Attr("src");
                        var description = qOverlay.Find("td.rightOverlay");

                        foreach (var img in description.Find("img")) // convert relativ flag paths to full uri
                            img.SetAttribute("src", SiteLink + img.GetAttribute("src"));

                        var descriptionDom = description.Get(0);
                        for (var i = 14; i > 0; i--) // remove size/seeders/leechers
                            descriptionDom.ChildNodes.RemoveAt(0);

                        release.Description = descriptionDom.OuterHTML;
                        release.Title = title.Text();

                        Uri bannerUri;
                        if (Uri.TryCreate(banner, UriKind.Absolute, out bannerUri))
                            release.BannerUrl = bannerUri;

                        var qLink = row.Cq().Find("a[href^=\"torrents.php?id=\"][onmouseover]");
                        Uri commentUri;
                        if (Uri.TryCreate(SiteLink + qLink.Attr("href"), UriKind.Absolute, out commentUri))
                            release.Comments = commentUri;

                        release.Guid = release.Comments;

                        var qDownload = row.ChildElements.ElementAt(1).ChildElements.ElementAt(1).ChildElements.ElementAt(0).Cq();
                        Uri linkUri;
                        if (Uri.TryCreate(SiteLink + qDownload.Attr("href"), UriKind.Absolute, out linkUri))
                            release.Link = linkUri;

                        var sizeStr = row.ChildElements.ElementAt(5).Cq().Text();
                        release.Size = ReleaseInfo.GetBytes(sizeStr);

                        release.Seeders = ParseUtil.CoerceInt(row.ChildElements.ElementAt(6).Cq().Text());
                        release.Peers = ParseUtil.CoerceInt(row.ChildElements.ElementAt(7).Cq().Text()) + release.Seeders;

                        var files = row.Cq().Find("td:nth-child(4)").Text();
                        release.Files = ParseUtil.CoerceInt(files);

                        if (row.Cq().Find("img[src=\"/static//common/browse/freeleech.png\"]").Any())
                            release.DownloadVolumeFactor = 0;
                        else
                            release.DownloadVolumeFactor = 1;

                        release.UploadVolumeFactor = 1;

                        releases.Add(release);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Error parsing item");
                    }
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.Content, ex);
            }

            return releases;
        }
        public class NxtGnConfigurationData : ConfigurationData
        {
            public NxtGnConfigurationData()
            {
                Username = new StringItem { Name = "Username" };
                Password = new StringItem { Name = "Password" };
            }
            public StringItem Username { get; private set; }
            public StringItem Password { get; private set; }
        }
    }
}
