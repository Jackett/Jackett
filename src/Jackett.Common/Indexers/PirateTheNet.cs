using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
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
    public class PirateTheNet : BaseWebIndexer
    {
        private string SearchUrl { get { return SiteLink + "torrentsutils.php"; } }
        private string LoginUrl { get { return SiteLink + "takelogin.php"; } }
        private string CaptchaUrl { get { return SiteLink + "simpleCaptcha.php?numImages=1"; } }
        private TimeZoneInfo germanyTz = TimeZoneInfo.CreateCustomTimeZone("W. Europe Standard Time", new TimeSpan(1, 0, 0), "W. Europe Standard Time", "W. Europe Standard Time");
        private readonly List<String> categories = new List<string>() { "1080P", "720P", "BDRip", "BluRay", "BRRip", "DVDR", "DVDRip", "FLAC", "MP3", "MP4", "Packs", "R5", "Remux", "TVRip", "WebRip" };

        private new ConfigurationDataBasicLoginWithRSSAndDisplay configData
        {
            get { return (ConfigurationDataBasicLoginWithRSSAndDisplay)base.configData; }
            set { base.configData = value; }
        }

        public PirateTheNet(IIndexerConfigurationService configService, WebClient w, Logger l, IProtectionService ps)
            : base(name: "PirateTheNet",
                description: "A movie tracker",
                link: "http://piratethenet.org/",
                caps: new TorznabCapabilities(),
                configService: configService,
                client: w,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLoginWithRSSAndDisplay())
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";

            this.configData.DisplayText.Value = "Only the results from the first search result page are shown, adjust your profile settings to show the maximum.";
            this.configData.DisplayText.Name = "Notice";

            AddCategoryMapping("1080P", TorznabCatType.MoviesHD, "1080P");
            AddCategoryMapping("720P", TorznabCatType.MoviesHD, "720P");
            AddCategoryMapping("BDRip", TorznabCatType.MoviesSD, "BDRip");
            AddCategoryMapping("BluRay", TorznabCatType.MoviesBluRay, "BluRay");
            AddCategoryMapping("BRRip", TorznabCatType.MoviesSD, "BRRip");
            AddCategoryMapping("DVDR", TorznabCatType.MoviesDVD, "DVDR");
            AddCategoryMapping("DVDRip", TorznabCatType.MoviesSD, "DVDRip");
            AddCategoryMapping("FLAC", TorznabCatType.AudioLossless, "FLAC");
            AddCategoryMapping("MP3", TorznabCatType.AudioMP3, "MP3");
            AddCategoryMapping("MP4", TorznabCatType.MoviesOther, "MP4");
            AddCategoryMapping("Packs", TorznabCatType.MoviesOther, "Packs");
            AddCategoryMapping("R5", TorznabCatType.MoviesDVD, "R5");
            AddCategoryMapping("Remux", TorznabCatType.MoviesOther, "Remux");
            AddCategoryMapping("TVRip", TorznabCatType.MoviesOther, "TVRip");
            AddCategoryMapping("WebRip", TorznabCatType.MoviesWEBDL, "WebRip");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            CookieHeader = ""; // clear old cookies

            var result1 = await RequestStringWithCookies(CaptchaUrl);
            var json1 = JObject.Parse(result1.Content);
            var captchaSelection = json1["images"][0]["hash"];

            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "captchaSelection", (string)captchaSelection }
            };

            var result2 = await RequestLoginAndFollowRedirect(LoginUrl, pairs, result1.Cookies, true, null, null, true);

            await ConfigureIfOK(result2.Cookies, result2.Content.Contains("logout.php"), () =>
            {
                var errorMessage = "Login Failed";
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
            queryCollection.Add("action", "torrentstable");
            queryCollection.Add("viewtype", "0");
            queryCollection.Add("visiblecategories", "Action,Adventure,Animation,Biography,Comedy,Crime,Documentary,Drama,Eastern,Family,Fantasy,History,Holiday,Horror,Kids,Musical,Mystery,Romance,Sci-Fi,Short,Sports,Thriller,War,Western");
            queryCollection.Add("page", "1");
            queryCollection.Add("visibility", "showall");
            queryCollection.Add("compression", "showall");
            queryCollection.Add("sort", "added");
            queryCollection.Add("order", "DESC");
            queryCollection.Add("titleonly", "true");
            queryCollection.Add("packs", "showall");
            queryCollection.Add("bookmarks", "showall");
            queryCollection.Add("subscriptions", "showall");
            queryCollection.Add("skw", "showall");
            queryCollection.Add("advancedsearchparameters", "");

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                // search keywords use OR by default and it seems like there's no way to change it, expect unwanted results
                queryCollection.Add("searchstring", searchString);
            }

            var cats = MapTorznabCapsToTrackers(query);
            queryCollection.Add("hiddenqualities", String.Join(",", cats));

            searchUrl += "?" + queryCollection.GetQueryString();

            var results = await RequestStringWithCookiesAndRetry(searchUrl);
            if (results.IsRedirect)
            {
                // re-login
                await ApplyConfiguration(null);
                results = await RequestStringWithCookiesAndRetry(searchUrl);
            }

            try
            {
                CQ dom = results.Content;
                /*
                // parse logic for viewtype=1, unfortunately it's missing the release time so we can't use it
                var movieBlocks = dom["table.main"];
                foreach (var movieBlock in movieBlocks)
                {
                    var qMovieBlock = movieBlock.Cq();

                    var movieLink = qMovieBlock.Find("tr > td[class=colhead] > a").First();
                    var movieName = movieLink.Text();

                    var qDetailsBlock = qMovieBlock.Find("tr > td.torrentstd > table > tbody > tr");
                    var qDetailsHeader = qDetailsBlock.ElementAt(0);
                    var qDetailsTags = qDetailsBlock.ElementAt(1);
                    var qTorrents = qDetailsBlock.Find("td.moviestorrentstd > table > tbody > tr:eq(0)");

                    foreach (var torrent in qTorrents)
                    {
                        var qTorrent = torrent.Cq();
                        var qCatIcon = qTorrent.Find("td:eq(0) > img");
                        var qDetailsLink = qTorrent.Find("td:eq(1) > a:eq(0)");
                        var qSeeders = qTorrent.Find("td:eq(1) > b > a[alt=\"Number of Seeders\"]");
                        var qLeechers = qTorrent.Find("td:eq(1) > span[alt=\"Number of Leechers\"]");
                        var qDownloadLink = qTorrent.Find("td:eq(1) > a:has(img[alt=\"Download Torrent\"])");
                    }
                }
                */

                var rows = dom["table.main > tbody > tr"];
                foreach (var row in rows.Skip(1))
                {
                    var release = new ReleaseInfo();
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 72 * 60 * 60;

                    var qRow = row.Cq();

                    var qDetailsLink = qRow.Find("td:eq(1) > a:eq(0)"); // link to the movie, not the actual torrent
                    release.Title = qDetailsLink.Attr("alt");

                    var qCatIcon = qRow.Find("td:eq(0) > img");
                    var qSeeders = qRow.Find("td:eq(8)");
                    var qLeechers = qRow.Find("td:eq(9)");
                    var qDownloadLink = qRow.Find("td > a:has(img[alt=\"Download Torrent\"])");
                    var qPudDate = qRow.Find("td:eq(5) > nobr");
                    var qSize = qRow.Find("td:eq(6)");

                    var catStr = qCatIcon.Attr("alt");
                    release.Category = MapTrackerCatToNewznab(catStr);
                    release.Link = new Uri(SiteLink + qDownloadLink.Attr("href").Substring(1));
                    release.Title = qDetailsLink.Attr("alt");
                    release.Comments = new Uri(SiteLink + qDetailsLink.Attr("href"));
                    release.Guid = release.Link;

                    var dateStr = qPudDate.Text().Trim();
                    DateTime pubDateUtc;
                    var Timeparts = dateStr.Split(new char[] { ' ' }, 2)[1];
                    if (dateStr.StartsWith("Today "))
                        pubDateUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified) + DateTime.ParseExact(dateStr.Split(new char[] { ' ' }, 2)[1], "hh:mm tt", System.Globalization.CultureInfo.InvariantCulture).TimeOfDay;
                    else if (dateStr.StartsWith("Yesterday "))
                        pubDateUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified) +
                            DateTime.ParseExact(dateStr.Split(new char[] { ' ' }, 2)[1], "hh:mm tt", System.Globalization.CultureInfo.InvariantCulture).TimeOfDay - TimeSpan.FromDays(1);
                    else
                        pubDateUtc = DateTime.SpecifyKind(DateTime.ParseExact(dateStr, "MMM d yyyy hh:mm tt", CultureInfo.InvariantCulture), DateTimeKind.Unspecified);

                    release.PublishDate = pubDateUtc.ToLocalTime();

                    var sizeStr = qSize.Text();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(qSeeders.Text());
                    release.Peers = ParseUtil.CoerceInt(qLeechers.Text()) + release.Seeders;

                    var files = qRow.Find("td:nth-child(4)").Text();
                    release.Files = ParseUtil.CoerceInt(files);

                    var grabs = qRow.Find("td:nth-child(8)").Text();
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
