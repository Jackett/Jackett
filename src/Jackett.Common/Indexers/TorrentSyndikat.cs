using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
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
    public class TorrentSyndikat : BaseWebIndexer
    {
        private string SearchUrl => SiteLink + "browse.php";
        private string LoginUrl => SiteLink + "eing2.php";
        private string CaptchaUrl => SiteLink + "simpleCaptcha.php?numImages=1";
        private readonly TimeZoneInfo germanyTz;

        private new ConfigurationDataBasicLoginWithRSSAndDisplay configData
        {
            get => (ConfigurationDataBasicLoginWithRSSAndDisplay)base.configData;
            set => base.configData = value;
        }

        public TorrentSyndikat(IIndexerConfigurationService configService, WebClient w, Logger l, IProtectionService ps)
            : base(name: "Torrent-Syndikat",
                description: "A German general tracker",
                link: "https://torrent-syndikat.org/",
                caps: new TorznabCapabilities(),
                configService: configService,
                client: w,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLoginWithRSSAndDisplay())
        {
            Encoding = Encoding.UTF8;
            Language = "de-de";
            Type = "private";

            TorznabCaps.SupportsImdbMovieSearch = true;

            configData.DisplayText.Value = "Only the results from the first search result page are shown, adjust your profile settings to show the maximum.";
            configData.DisplayText.Name = "Notice";

            AddCategoryMapping(2, TorznabCatType.PC, "Apps / Windows");
            AddCategoryMapping(13, TorznabCatType.PC, "Apps / Linux");
            AddCategoryMapping(4, TorznabCatType.PCMac, "Apps / MacOS");
            AddCategoryMapping(6, TorznabCatType.PC, "Apps / Misc");

            AddCategoryMapping(50, TorznabCatType.PCGames, "Spiele / Windows");
            AddCategoryMapping(51, TorznabCatType.PCGames, "Spiele / MacOS");
            AddCategoryMapping(52, TorznabCatType.PCGames, "Spiele / Linux");
            AddCategoryMapping(8, TorznabCatType.ConsoleOther, "Spiele / Playstation");
            AddCategoryMapping(7, TorznabCatType.ConsoleOther, "Spiele / Nintendo");
            AddCategoryMapping(32, TorznabCatType.ConsoleOther, "Spiele / XBOX");

            AddCategoryMapping(42, TorznabCatType.MoviesUHD, "Filme / 2160p");
            AddCategoryMapping(9, TorznabCatType.MoviesHD, "Filme / 1080p");
            AddCategoryMapping(20, TorznabCatType.MoviesHD, "Filme / 720p");
            AddCategoryMapping(10, TorznabCatType.MoviesSD, "Filme / SD");

            AddCategoryMapping(43, TorznabCatType.TVUHD, "Serien / 2160p");
            AddCategoryMapping(53, TorznabCatType.TVHD, "Serien / 1080p");
            AddCategoryMapping(54, TorznabCatType.TVHD, "Serien / 720p");
            AddCategoryMapping(15, TorznabCatType.TVSD, "Serien / SD");
            AddCategoryMapping(30, TorznabCatType.TVSport, "Serien / Sport");

            AddCategoryMapping(44, TorznabCatType.TVUHD, "Serienpacks / 2160p");
            AddCategoryMapping(55, TorznabCatType.TVHD, "Serienpacks / 1080p");
            AddCategoryMapping(56, TorznabCatType.TVHD, "Serienpacks / 720p");
            AddCategoryMapping(27, TorznabCatType.TVSD, "Serienpacks / SD");

            AddCategoryMapping(24, TorznabCatType.AudioLossless, "Audio / Musik / FLAC");
            AddCategoryMapping(25, TorznabCatType.AudioMP3, "Audio / Musik / MP3");
            AddCategoryMapping(35, TorznabCatType.AudioOther, "Audio / Other");
            AddCategoryMapping(18, TorznabCatType.AudioAudiobook, "Audio / aBooks");
            AddCategoryMapping(33, TorznabCatType.AudioVideo, "Audio / Videos");

            AddCategoryMapping(17, TorznabCatType.Books, "Misc / eBooks");
            AddCategoryMapping(5, TorznabCatType.PCPhoneOther, "Misc / Mobile");
            AddCategoryMapping(39, TorznabCatType.Other, "Misc / Bildung");

            AddCategoryMapping(36, TorznabCatType.TVFOREIGN, "Englisch / Serien");
            AddCategoryMapping(57, TorznabCatType.TVFOREIGN, "Englisch / Serienpacks");
            AddCategoryMapping(37, TorznabCatType.MoviesForeign, "Englisch / Filme");
            AddCategoryMapping(47, TorznabCatType.Books, "Englisch / eBooks");
            AddCategoryMapping(48, TorznabCatType.Other, "Englisch / Bildung");
            AddCategoryMapping(49, TorznabCatType.TVSport, "Englisch / Sport");

            var startTransition = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 3, 0, 0), 3, 5, DayOfWeek.Sunday);
            var endTransition = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 4, 0, 0), 10, 5, DayOfWeek.Sunday);
            var delta = new TimeSpan(1, 0, 0);
            var adjustment = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(1999, 10, 1), DateTime.MaxValue.Date, delta, startTransition, endTransition);
            TimeZoneInfo.AdjustmentRule[] adjustments = { adjustment };
            germanyTz = TimeZoneInfo.CreateCustomTimeZone("W. Europe Standard Time", new TimeSpan(1, 0, 0), "(GMT+01:00) W. Europe Standard Time", "W. Europe Standard Time", "W. Europe DST Time", adjustments);
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            CookieHeader = "";

            var result1 = await RequestStringWithCookies(CaptchaUrl);
            var json1 = JObject.Parse(result1.Content);
            var captchaSelection = json1["images"][0]["hash"];

            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "captchaSelection", (string)captchaSelection },
                { "submitme", "X" }
            };

            var result2 = await RequestLoginAndFollowRedirect(LoginUrl, pairs, result1.Cookies, true, null, null, true);

            await ConfigureIfOK(result2.Cookies, result2.Content.Contains("/logout.php"),
                () => throw new ExceptionWithConfigData(result2.Content, configData));
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var searchString = query.GetQueryString();
            var searchUrl = SearchUrl;
            var queryCollection = new NameValueCollection();

            queryCollection.Add("incldead", "1");
            queryCollection.Add("rel_type", "0"); // Alle

            if (query.ImdbID != null)
            {
                queryCollection.Add("searchin", "imdb");
                queryCollection.Add("search", query.ImdbID);
            }
            else
            {
                queryCollection.Add("searchin", "title");

                if (!string.IsNullOrWhiteSpace(searchString))
                {
                    // use AND+wildcard operator to avoid getting to many useless results
                    var searchStringArray = Regex.Split(searchString.Trim(), "[ _.-]+", RegexOptions.Compiled).ToList();
                    searchStringArray = searchStringArray.Select(x => "+" + x).ToList(); // add AND operators
                    var searchStringFinal = string.Join(" ", searchStringArray);
                    queryCollection.Add("search", searchStringFinal);
                }

            }
            foreach (var cat in MapTorznabCapsToTrackers(query))
                queryCollection.Add("c" + cat, "1");

            searchUrl += "?" + queryCollection.GetQueryString();

            var results = await RequestStringWithCookiesAndRetry(searchUrl);

            if (results.IsRedirect)
            {
                await ApplyConfiguration(null);
                results = await RequestStringWithCookiesAndRetry(searchUrl);
            }

            try
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(results.Content);
                var rows = dom.QuerySelectorAll("table.torrent_table > tbody > tr");
                var globalFreeleech = dom.QuerySelector("legend:contains(\"Freeleech\")+ul > li > b:contains(\"Freeleech\")") != null;
                foreach (var row in rows.Skip(1))
                {
                    var release = new ReleaseInfo();
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 96 * 60 * 60;

                    var catStr = row.Children[0].FirstElementChild.GetAttribute("href").Split('=')[1].Split('&')[0];
                    release.Category = MapTrackerCatToNewznab(catStr);

                    var qLink = row.Children[2].FirstElementChild;
                    release.Link = new Uri(SiteLink + qLink.GetAttribute("href"));

                    var descCol = row.Children[1];
                    var torrentTag = descCol.QuerySelectorAll("span.torrent-tag");
                    if (torrentTag.Any())
                        release.Description = string.Join(", ", torrentTag.Select(x => x.InnerHtml));

                    var qCommentLink = descCol.QuerySelector("a[href*=\"details.php\"]");
                    release.Title = qCommentLink.GetAttribute("title");
                    release.Comments = new Uri(SiteLink + qCommentLink.GetAttribute("href").Replace("&hit=1", ""));
                    release.Guid = release.Comments;

                    var torrentDetails = descCol.QuerySelector(".torrent_details");
                    var rawDateStr = torrentDetails.ChildNodes[1].TextContent;
                    var dateStr = rawDateStr.Trim().Replace("von", "").Trim();
                    DateTime dateGerman;
                    if (dateStr.StartsWith("Heute "))
                        dateGerman = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified) + TimeSpan.Parse(dateStr.Split(' ')[1]);
                    else if (dateStr.StartsWith("Gestern "))
                        dateGerman = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified) + TimeSpan.Parse(dateStr.Split(' ')[1]) - TimeSpan.FromDays(1);
                    else
                        dateGerman = DateTime.SpecifyKind(DateTime.ParseExact(dateStr, "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture), DateTimeKind.Unspecified);

                    var pubDateUtc = TimeZoneInfo.ConvertTimeToUtc(dateGerman, germanyTz);
                    release.PublishDate = pubDateUtc.ToLocalTime();

                    var imdbLink = descCol.QuerySelector("a[href*=\"&searchin=imdb\"]");
                    if (imdbLink != null)
                        release.Imdb = ParseUtil.GetLongFromString(imdbLink.GetAttribute("href"));

                    var sizeFileCountRowChilds = row.Children[5].Children;
                    release.Size = ReleaseInfo.GetBytes(sizeFileCountRowChilds[0].TextContent);
                    release.Files = ParseUtil.CoerceInt(sizeFileCountRowChilds[2].TextContent);

                    release.Seeders = ParseUtil.CoerceInt(row.Children[7].TextContent);
                    release.Peers = ParseUtil.CoerceInt(row.Children[8].TextContent) + release.Seeders;

                    var grabs = row.QuerySelector("td:nth-child(7)").TextContent;
                    release.Grabs = ParseUtil.CoerceInt(grabs);

                    if (globalFreeleech)
                        release.DownloadVolumeFactor = 0;
                    else if (row.QuerySelector("span.torrent-tag-free") != null)
                        release.DownloadVolumeFactor = 0;
                    else
                        release.DownloadVolumeFactor = 1;

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
