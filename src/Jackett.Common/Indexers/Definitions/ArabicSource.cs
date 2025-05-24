using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.Io;
using FlareSolverrSharp.Types;
using Jackett.Common.Extensions;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class ArabicSource : IndexerBase
    {
        public override string Id => "arabicsource";
        public override string Name => "ArabicSource";
        public override string Description => "ArabicSource is an ARABIC Private Torrent Tracker for MOVIES / TV";
        public override string SiteLink { get; protected set; } = "https://arabicsource.net/";
        public override string Language => "ar-SA";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private string SearchUrl => SiteLink + "browse.php?";
        private string SubmitLoginUrl => SiteLink + "takelogin.php";
        private string IndexUrl => SiteLink + "index.php";
        private string ThankYouUrl => SiteLink + "ts_ajax.php";
        private string TakeThanksUrl => SiteLink + "takethanks.php";

        private readonly Regex _idRegex = new Regex(@"TSajaxquickcomment\(\'(\d+)\'", RegexOptions.IgnoreCase);
        private readonly Regex _lcidRegex = new Regex(@"TSajaxquickcomment\(\'\d+\', \'(\d+)\'", RegexOptions.IgnoreCase);
        private new ConfigurationDataBasicLogin configData
        {
            get => (ConfigurationDataBasicLogin)base.configData;
            set => base.configData = value;
        }
        public ArabicSource(IIndexerConfigurationService configService, Utils.Clients.WebClient wc, Logger l,
            IProtectionService ps, ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataBasicLogin("For best results, change the <b>Torrents per page:</b> setting to <b>40</b> on your account profile. The Default is <i>15</i>."))
        {
            configData.AddDynamic("thankyoutext", new StringConfigurationItem("Thank You Text"));
            configData.AddDynamic("Thank you comment", new DisplayInfoConfigurationItem("Thank you comment", "This site requires you to leave a Thank You comment before you can download. Enter your personalised plain text comment above."));
            configData.AddDynamic("freeleech", new BoolConfigurationItem("Search freeleech only") { Value = false });
            // Configure the sort selects
            var sortBySelect = new SingleSelectConfigurationItem(
                "Sort by",
                new Dictionary<string, string>
                {
                    { "added", "created" },
                    { "seeders", "seeders" },
                    { "size", "size" },
                    { "name", "title" }
                })
            { Value = "added" };
            configData.AddDynamic("sortrequestedfromsite", sortBySelect);

            var orderSelect = new SingleSelectConfigurationItem(
                "Order",
                new Dictionary<string, string>
                {
                    { "desc", "descending" },
                    { "asc", "ascending" }
                })
            { Value = "desc" };
            configData.AddDynamic("orderrequestedfromsite", orderSelect);
            configData.AddDynamic("includevip", new BoolConfigurationItem(" Include VIP results") { Value = false });
            configData.AddDynamic("Account Inactivity", new DisplayInfoConfigurationItem("Account Inactivity", "The maximum number of days you can stay away from the site is 40 days, and only if you suspend the account, you will get a grace period of 180 days, but you must contact the administration in advance so that this is added to your personal account and you are not exposed to expulsion."));
        }
        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q, MovieSearchParam.ImdbId
                },
                MusicSearchParams = new List<MusicSearchParam>
                {
                    MusicSearchParam.Q
                },
                BookSearchParams = new List<BookSearchParam>
                {
                    BookSearchParam.Q
                }
            };

            caps.Categories.AddCategoryMapping(52, TorznabCatType.Movies, "(أفلام الرسوم المدبلجة) dubbed animated movies");
            caps.Categories.AddCategoryMapping(53, TorznabCatType.MoviesHD, "(أفلام الرسوم المدبلجة) dubbed animated movies 1080p)");
            caps.Categories.AddCategoryMapping(55, TorznabCatType.Movies3D, "(أفلام الرسوم المدبلجة) dubbed animated movies 3D");
            caps.Categories.AddCategoryMapping(78, TorznabCatType.MoviesUHD, "(أفلام الرسوم المدبلجة) dubbed animated movies 4K");
            caps.Categories.AddCategoryMapping(54, TorznabCatType.MoviesHD, "(أفلام الرسوم المدبلجة) dubbed animated movies 720p");
            caps.Categories.AddCategoryMapping(56, TorznabCatType.MoviesDVD, "(أفلام الرسوم المدبلجة) dubbed animated movies DVD");
            caps.Categories.AddCategoryMapping(58, TorznabCatType.MoviesHD, "(أفلام الرسوم المدبلجة) dubbed animated movies HDTV1080p");
            caps.Categories.AddCategoryMapping(57, TorznabCatType.Movies, "(أفلام الرسوم المدبلجة) dubbed animated movies TVRip");
            caps.Categories.AddCategoryMapping(72, TorznabCatType.Movies, "(الأفلام العربية) Arabic films");
            caps.Categories.AddCategoryMapping(15, TorznabCatType.Other, "(الحصرية) VIP");
            caps.Categories.AddCategoryMapping(42, TorznabCatType.Movies, "(الحصرية أفلام) VIP movies");
            caps.Categories.AddCategoryMapping(20, TorznabCatType.TVAnime, "(الحصرية رسوم) VIP cartoons ");
            caps.Categories.AddCategoryMapping(41, TorznabCatType.AudioVideo, "(الحصرية مسرحيات) VIP plays");
            caps.Categories.AddCategoryMapping(21, TorznabCatType.TV, "(الحصرية مسلسلات) VIP series");
            caps.Categories.AddCategoryMapping(35, TorznabCatType.AudioVideo, "(المسرحيات العربية) Arabic plays");
            caps.Categories.AddCategoryMapping(5, TorznabCatType.TV, "(المسلسلات العربية) Arabic series");
            caps.Categories.AddCategoryMapping(81, TorznabCatType.TVForeign, "(المسلسلات العربية الآسيوية) Arabic series Asian");
            caps.Categories.AddCategoryMapping(79, TorznabCatType.TVForeign, "(المسلسلات العربية التركية) Arabic series Turkish ");
            caps.Categories.AddCategoryMapping(80, TorznabCatType.TVForeign, "(المسلسلات العربية الهندية) Arabic series Hindi");
            caps.Categories.AddCategoryMapping(69, TorznabCatType.Movies, "(الوسائط المترجمة) Translated media");
            caps.Categories.AddCategoryMapping(70, TorznabCatType.Movies, "(الوسائط المترجمة فلام رسوم) Translated Animated Movies");
            caps.Categories.AddCategoryMapping(71, TorznabCatType.Movies, "(الوسائط المترجمة لأفلام الأجنبية) Translated Foreign films");
            caps.Categories.AddCategoryMapping(77, TorznabCatType.Movies, "(الوسائط المترجمة الأفلام الهندية) Translated Indian movies");
            caps.Categories.AddCategoryMapping(86, TorznabCatType.Movies, "(الوسائط المترجمة مسلسلات الرسوم المترجمة) Translated cartoon series");
            caps.Categories.AddCategoryMapping(14, TorznabCatType.TV, "(رمضانيات) Ramadan");
            caps.Categories.AddCategoryMapping(7, TorznabCatType.TVAnime, "(مسلسلات الرسوم المدبلجة) Dubbed cartoon series");
            caps.Categories.AddCategoryMapping(61, TorznabCatType.TVAnime, "(مسلسلات الرسوم المدبلجة) Dubbed cartoon series1080p");
            caps.Categories.AddCategoryMapping(62, TorznabCatType.TVAnime, "(مسلسلات الرسوم المدبلجة) Dubbed cartoon series 720p");
            caps.Categories.AddCategoryMapping(63, TorznabCatType.TVAnime, "(مسلسلات الرسوم المدبلجة) Dubbed cartoon series DVD");
            caps.Categories.AddCategoryMapping(65, TorznabCatType.TVAnime, "(مسلسلات الرسوم المدبلجة) Dubbed cartoon series HDTV1080p");
            caps.Categories.AddCategoryMapping(64, TorznabCatType.TVAnime, "(مسلسلات الرسوم المدبلجة) Dubbed cartoon series TVRip");
            caps.Categories.AddCategoryMapping(31, TorznabCatType.Other, "(وسائط منوعات) Miscellaneous media");
            caps.Categories.AddCategoryMapping(67, TorznabCatType.Other, "(وسائط منوعات إسلاميات) Miscellaneous media Islamic Studies");
            caps.Categories.AddCategoryMapping(32, TorznabCatType.Other, "(وسائط منوعات تربوي) Miscellaneous media Educational");
            caps.Categories.AddCategoryMapping(76, TorznabCatType.Audio, "(وسائط منوعات صوتيات) Miscellaneous media Phonetics");
            caps.Categories.AddCategoryMapping(68, TorznabCatType.TVAnime, "(وسائط منوعات كرتون كلاسيك) Miscellaneous media Classic Cartoon");
            caps.Categories.AddCategoryMapping(66, TorznabCatType.TVDocumentary, "(وسائط منوعات وثائقيات) Miscellaneous media Documentaries");

            return caps;
        }
        private string GetSortBy => ((SingleSelectConfigurationItem)configData.GetDynamic("sortrequestedfromsite")).Value;
        private string GetOrder => ((SingleSelectConfigurationItem)configData.GetDynamic("orderrequestedfromsite")).Value;
        private string GetThankYouText => ((StringConfigurationItem)configData.GetDynamic("thankyoutext")).Value;
        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            if (GetThankYouText.IsNullOrWhiteSpace())
            {
                throw new Exception("The Thank You Text in the config is missing. Site requires a thank you comment before download is allowed.");
            }

            // if we are already logged in, then we need to logout to use login form
            var testLoggedin = await RequestWithCookiesAndRetryAsync(IndexUrl);
            if (testLoggedin.Status == HttpStatusCode.OK && testLoggedin.ContentString.Contains("/logout.php?logouthash="))
            {
                var logoutParser = new HtmlParser();
                using var document = logoutParser.ParseDocument(testLoggedin.ContentString);
                var logoutUrl = document.QuerySelector("a[href*=\"/logout.php?logouthash=\"]").GetAttribute("href");
                var logout = await RequestWithCookiesAsync(logoutUrl);
            }

            // performing login
            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "logout", "" }
            };
            var response = await RequestLoginAndFollowRedirect(SubmitLoginUrl, pairs, null, true, null, IndexUrl);
            await ConfigureIfOK(response.Cookies, response.ContentString.Contains("/logout.php?logouthash="), () =>
            {
                var htmlParser = new HtmlParser();
                using var document = htmlParser.ParseDocument(response.ContentString);
                var errorMessage = document.QuerySelector("table:contains(\"ERROR:\")")?.Text().Trim();

                throw new ExceptionWithConfigData(errorMessage ?? "Login failed.", configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }
        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var searchUrl = SearchUrl;

            var queryCollection = new NameValueCollection
            {
                { "do", "search" },
                { "keywords", query.ImdbID ?? query.GetQueryString() },
                // t_name, t_description, t_both, t_uploader, t_genre
                { "search_type", query.ImdbID.IsNotNullOrWhiteSpace() ? "t_genre" : "t_name" },
                // does not support multi category searching so defaulting to all.
                { "category", "0" },
                { "include_dead_torrents", "yes" },
                { "sort", ((BoolConfigurationItem)configData.GetDynamic("freeleech")).Value ? "free" : GetSortBy },
                { "order", ((BoolConfigurationItem)configData.GetDynamic("freeleech")).Value ? "asc" : GetOrder }
            };
            // add masking to prevent exact matches only and mostly bypass the minimum 4 char word length block
            searchUrl += queryCollection.GetQueryString().Replace("+", "%");

            var results = await RequestWithCookiesAndRetryAsync(searchUrl);

            // Occasionally the cookies become invalid, login again if that happens
            if (!results.ContentString.Contains("/logout.php?logouthash="))
            {
                await ApplyConfiguration(null);
                results = await RequestWithCookiesAndRetryAsync(searchUrl);
            }

            try
            {
                var parser = new HtmlParser();
                using var dom = parser.ParseDocument(results.ContentString);

                var rowSelector = "table.sortable tr:has(a[href*=\"download.php?id=\"])";

                if (!((BoolConfigurationItem)configData.GetDynamic("includevip")).Value)
                {
                    rowSelector += ":not(:has(a[href$=\"?category=15\"])):not(:has(a[href$=\"?category=20\"])):not(:has(a[href$=\"?category=21\"])):not(:has(a[href$=\"?category=41\"])):not(:has(a[href$=\"?category=42\"]))";
                }

                var rows = dom.QuerySelectorAll(rowSelector);
                foreach (var row in rows)
                {

                    var categoryLink = row.QuerySelector("a[href*=\"?category=\"]").GetAttribute("href");
                    var cat = ParseUtil.GetArgumentFromQueryString(categoryLink, "category");
                    var description = string.Empty;
                    switch (cat)
                    {
                        case "15":
                        case "20":
                        case "21":
                        case "41":
                        case "42":
                            description = "**VIP**";
                            break;
                        default:
                            break;
                    }

                    var qDetailsLink = row.QuerySelector("a[href*=\"details.php?id=\"]");
                    var title = qDetailsLink.TextContent + description;
                    var details = new Uri(qDetailsLink.GetAttribute("href"));

                    var qPosterLink = row.QuerySelector("img[src*=\"/torrents/images/\"]");
                    var size = ParseUtil.GetBytes(row.QuerySelector("td:nth-last-child(5)").TextContent);
                    var matchDateAdded = Regex.Match(row.QuerySelector(" td:nth-child(2)").TextContent, @"(\d{2}-\d{2}-\d{4} \d{2}:\d{2})", RegexOptions.IgnoreCase);
                    var publishDate = matchDateAdded.Groups[1].Success && DateTime.TryParseExact(matchDateAdded.Groups[1].Value, "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsedDate) ? parsedDate : DateTime.Now;

                    var grabs = ParseUtil.CoerceInt(row.QuerySelector("td:nth-last-child(4)").TextContent);
                    var seeders = ParseUtil.CoerceInt(row.QuerySelector("td:nth-last-child(3)").TextContent);
                    var leechers = ParseUtil.CoerceInt(row.QuerySelector("td:nth-last-child(2)").TextContent) + seeders;

                    var dlVolumeFactor = 1.0;
                    var upVolumeFactor = 1.0;
                    if (row.QuerySelector("img[src$=\"/freedownload.gif\"]") != null)
                        dlVolumeFactor = 0;
                    else if (row.QuerySelector("img[src$=\"/silverdownload.gif\"]") != null)
                        dlVolumeFactor = 0.5;
                    if (row.QuerySelector("img[src$=\"/x2.gif\"]") != null)
                        upVolumeFactor = 2;

                    var release = new ReleaseInfo
                    {
                        Title = title,
                        Details = details,
                        Guid = details,
                        Link = details,
                        PublishDate = publishDate,
                        Category = MapTrackerCatToNewznab(cat),
                        Size = size,
                        Grabs = grabs,
                        Seeders = seeders,
                        Peers = leechers,
                        Description = description,
                        DownloadVolumeFactor = dlVolumeFactor,
                        UploadVolumeFactor = upVolumeFactor,
                        MinimumRatio = 1.05
                    };
                    if (qPosterLink != null)
                    {
                        release.Poster = new Uri(qPosterLink.GetAttribute("src"));
                    }

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.ContentString, ex);
            }

            return releases;
        }

        public override async Task<byte[]> Download(Uri link)
        {
            // before downloads are allowed, users must leave a comment on the comments section
            // of the details page, and then press the SayThanks button on the info section 8-O
            var detailsPage = await RequestWithCookiesAsync(link.ToString());

            var htmlParser = new HtmlParser();
            using var dom = htmlParser.ParseDocument(detailsPage.ContentString);

            // <input type="button" class="button" value="Submit" name="quickcomment" id="quickcomment" onclick="javascript:TSajaxquickcomment('9999', '9');"/>
            var quickCommentLink = dom.QuerySelector("#quickcomment").GetAttribute("onclick");
            var idMatch = _idRegex.Match(quickCommentLink);
            var qcid = idMatch.Success ? idMatch.Groups[1].Value : null;
            var lcidMatch = _lcidRegex.Match(quickCommentLink);
            var qclcid = lcidMatch.Success ? lcidMatch.Groups[1].Value : null;
            if (qcid.IsNullOrWhiteSpace() || qclcid.IsNullOrWhiteSpace())
            {
                throw new Exception("Unable to extract quickComment data from details page at " + link.ToString());
            }

            var thankYouPairs = new Dictionary<string, string> {
                { "ajax_quick_comment", "1" },
                { "id", qcid },
                { "lcid", qclcid },
                { "text", GetThankYouText }
            };

            var thankYouResponse = await RequestLoginAndFollowRedirect(ThankYouUrl, thankYouPairs, null, true, null, ThankYouUrl);

            // <input type="button" value="Say thanks!" onclick="javascript:TSajaxquickthanks(9999);"/>
            var takeThanksPairs = new Dictionary<string, string> {
                { "torrentid", qcid }
            };
            var takeThanksResponse = await RequestLoginAndFollowRedirect(TakeThanksUrl, takeThanksPairs, null, true, null, TakeThanksUrl);

            var downloadLink = dom.QuerySelector("a[href*=\"download.php?id=\"]")?.GetAttribute("href")?.Trim();

            if (downloadLink.IsNullOrWhiteSpace())
            {
                throw new Exception("Unable to find download link.");
            }
            var response = await RequestWithCookiesAsync(downloadLink);
            return response.ContentBytes;
        }
    }
}
