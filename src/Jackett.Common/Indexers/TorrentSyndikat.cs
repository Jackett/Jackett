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
                caps: new TorznabCapabilities
                {
                    SupportsImdbMovieSearch = true
                },
                configService: configService,
                client: w,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLoginWithRSSAndDisplay())
        {
            Encoding = Encoding.UTF8;
            Language = "de-de";
            Type = "private";

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
            var queryCollection = new NameValueCollection
            {
                {"incldead", "1"},
                {"rel_type", "0"} // Alle
            };

            if (query.ImdbID != null)
            {
                queryCollection.Add("searchin", "imdb");
                queryCollection.Add("search", query.ImdbID);
            }
            else
            {
                queryCollection.Add("searchin", "title");

                if (!string.IsNullOrWhiteSpace(query.GetQueryString()))
                {
                    // use AND+wildcard operator to avoid getting to many useless results
                    var searchStringArray = Regex.Split(
                        query.SanitizedSearchTerm, "[ _.-]+",
                        RegexOptions.Compiled).Select(term => $"+{term}");

                    // If only season search add * wildcard to get all episodes
                    var tvEpisode = query.GetEpisodeSearchString();
                    if (!string.IsNullOrWhiteSpace(tvEpisode))
                    {
                        if (tvEpisode.StartsWith("S") && !tvEpisode.Contains("E"))
                            tvEpisode += "*";
                        searchStringArray = searchStringArray.Append($"+{tvEpisode}");
                    }

                    queryCollection.Add("search", string.Join(" ", searchStringArray));
                }

            }
            foreach (var cat in MapTorznabCapsToTrackers(query))
                queryCollection.Add("c" + cat, "1");

            var searchUrl = SearchUrl + "?" + queryCollection.GetQueryString();

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

                    var catStr = row.Children[0].FirstElementChild.GetAttribute("href").Split('=')[1].Split('&')[0];

                    var qLink = row.Children[2].FirstElementChild;

                    var descCol = row.Children[1];
                    var torrentTag = descCol.QuerySelectorAll("span.torrent-tag");
                    //Empty list gives string.Empty in string.Join
                    var description = string.Join(", ", torrentTag.Select(x => x.InnerHtml));
                    var qCommentLink = descCol.QuerySelector("a[href*=\"details.php\"]");
                    var comments = new Uri(SiteLink + qCommentLink.GetAttribute("href").Replace("&hit=1", ""));

                    var torrentDetails = descCol.QuerySelector(".torrent_details");
                    var rawDateStr = torrentDetails.ChildNodes[1].TextContent;
                    var dateStr = rawDateStr.Replace("von", "")
                                            .Replace("Heute", "Today")
                                            .Replace("Gestern", "Yesterday");
                    var dateGerman = DateTimeUtil.FromUnknown(dateStr);
                    var pubDateUtc = TimeZoneInfo.ConvertTimeToUtc(dateGerman, germanyTz);
                    var longFromString = ParseUtil.GetLongFromString(descCol.QuerySelector("a[href*=\"&searchin=imdb\"]")?.GetAttribute("href"));
                    var sizeFileCountRowChilds = row.Children[5].Children;
                    var seeders = ParseUtil.CoerceInt(row.Children[7].TextContent);
                    var link = new Uri(SiteLink + qLink.GetAttribute("href"));
                    var files = ParseUtil.CoerceInt(sizeFileCountRowChilds[2].TextContent);
                    var leechers = ParseUtil.CoerceInt(row.Children[8].TextContent);
                    var grabs = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(7)").TextContent);
                    var downloadVolumeFactor = globalFreeleech || row.QuerySelector("span.torrent-tag-free") != null
                        ? 0 : 1;
                    var size = ReleaseInfo.GetBytes(sizeFileCountRowChilds[0].TextContent);
                    var release = new ReleaseInfo
                    {
                        MinimumRatio = 1,
                        MinimumSeedTime = 345600, //8 days
                        Category = MapTrackerCatToNewznab(catStr),
                        Link = link,
                        Description = description,
                        Title = qCommentLink.GetAttribute("title"),
                        Comments = comments,
                        Guid = comments,
                        PublishDate = pubDateUtc.ToLocalTime(),
                        Imdb = longFromString,
                        Size = size,
                        Files = files,
                        Seeders = seeders,
                        Peers = leechers + seeders,
                        Grabs = grabs,
                        DownloadVolumeFactor = downloadVolumeFactor,
                        UploadVolumeFactor = 1
                    };
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
