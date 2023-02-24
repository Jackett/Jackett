using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
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
    [ExcludeFromCodeCoverage]
    public class AudioBookBay : BaseWebIndexer
    {
        public override string[] AlternativeSiteLinks { get; protected set; } = {
            "https://audiobookbay.li/",
            "https://audiobookbay.se/"
        };

        public override string[] LegacySiteLinks { get; protected set; } =
        {
            "https://audiobookbay.la/",
            "http://audiobookbay.net/",
            "https://audiobookbay.unblockit.tv/",
            "http://audiobookbay.nl/",
            "http://audiobookbay.ws/",
            "https://audiobookbay.unblockit.how/",
            "https://audiobookbay.unblockit.cam/",
            "https://audiobookbay.unblockit.biz/",
            "https://audiobookbay.unblockit.day/",
            "https://audiobookbay.unblockit.llc/",
            "https://audiobookbay.unblockit.blue/",
            "https://audiobookbay.unblockit.name/",
            "http://audiobookbay.fi/",
            "http://audiobookbay.se/",
            "http://audiobookbayabb.com/",
            "https://audiobookbay.unblockit.ist/",
            "https://audiobookbay.unblockit.bet/",
            "https://audiobookbay.unblockit.cat/",
            "https://audiobookbay.unblockit.nz/",
            "https://audiobookbay.fi/",
            "https://audiobookbay.unblockit.page/",
            "https://audiobookbay.unblockit.pet/",
            "https://audiobookbay.unblockit.ink/",
            "https://audiobookbay.unblockit.bio/" // error 502
        };

        public AudioBookBay(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps, ICacheService cs)
            : base(id: "audiobookbay",
                   name: "AudioBook Bay",
                   description: "AudioBook Bay (ABB) is a public Torrent Tracker for AUDIOBOOKS",
                   link: "https://audiobookbay.li/",
                   caps: new TorznabCapabilities
                   {
                       BookSearchParams = new List<BookSearchParam>
                       {
                           BookSearchParam.Q
                       }
                   },
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationData())
        {
            Encoding = Encoding.UTF8;
            Language = "en-US";
            Type = "public";

            // requestDelay for API Limit (1 request per 2 seconds)
            webclient.requestDelay = 2.1;

            // Age
            AddCategoryMapping("children", TorznabCatType.AudioAudiobook, "Children");
            AddCategoryMapping("teen-young-adult", TorznabCatType.AudioAudiobook, "Teen & Young Adult");
            AddCategoryMapping("adults", TorznabCatType.AudioAudiobook, "Adults");

            // Category
            AddCategoryMapping("postapocalyptic", TorznabCatType.AudioAudiobook, "(Post)apocalyptic");
            AddCategoryMapping("action", TorznabCatType.AudioAudiobook, "Action");
            AddCategoryMapping("adventure", TorznabCatType.AudioAudiobook, "Adventure");
            AddCategoryMapping("art", TorznabCatType.AudioAudiobook, "Art");
            AddCategoryMapping("autobiography-biographies", TorznabCatType.AudioAudiobook, "Autobiography & Biographies");
            AddCategoryMapping("business", TorznabCatType.AudioAudiobook, "Business");
            AddCategoryMapping("computer", TorznabCatType.AudioAudiobook, "Computer");
            AddCategoryMapping("contemporary", TorznabCatType.AudioAudiobook, "Contemporary");
            AddCategoryMapping("crime", TorznabCatType.AudioAudiobook, "Crime");
            AddCategoryMapping("detective", TorznabCatType.AudioAudiobook, "Detective");
            AddCategoryMapping("doctor-who-sci-fi", TorznabCatType.AudioAudiobook, "Doctor Who");
            AddCategoryMapping("education", TorznabCatType.AudioAudiobook, "Education");
            AddCategoryMapping("fantasy", TorznabCatType.AudioAudiobook, "Fantasy");
            AddCategoryMapping("general-fiction", TorznabCatType.AudioAudiobook, "General Fiction");
            AddCategoryMapping("historical-fiction", TorznabCatType.AudioAudiobook, "Historical Fiction");
            AddCategoryMapping("history", TorznabCatType.AudioAudiobook, "History");
            AddCategoryMapping("horror", TorznabCatType.AudioAudiobook, "Horror");
            AddCategoryMapping("humor", TorznabCatType.AudioAudiobook, "Humor");
            AddCategoryMapping("lecture", TorznabCatType.AudioAudiobook, "Lecture");
            AddCategoryMapping("lgbt", TorznabCatType.AudioAudiobook, "LGBT");
            AddCategoryMapping("literature", TorznabCatType.AudioAudiobook, "Literature");
            AddCategoryMapping("litrpg", TorznabCatType.AudioAudiobook, "LitRPG");
            AddCategoryMapping("general-non-fiction", TorznabCatType.AudioAudiobook, "Misc. Non-fiction");
            AddCategoryMapping("mystery", TorznabCatType.AudioAudiobook, "Mystery");
            AddCategoryMapping("paranormal", TorznabCatType.AudioAudiobook, "Paranormal");
            AddCategoryMapping("plays-theater", TorznabCatType.AudioAudiobook, "Plays & Theater");
            AddCategoryMapping("poetry", TorznabCatType.AudioAudiobook, "Poetry");
            AddCategoryMapping("political", TorznabCatType.AudioAudiobook, "Political");
            AddCategoryMapping("radio-productions", TorznabCatType.AudioAudiobook, "Radio Productions");
            AddCategoryMapping("romance", TorznabCatType.AudioAudiobook, "Romance");
            AddCategoryMapping("sci-fi", TorznabCatType.AudioAudiobook, "Sci-Fi");
            AddCategoryMapping("science", TorznabCatType.AudioAudiobook, "Science");
            AddCategoryMapping("self-help", TorznabCatType.AudioAudiobook, "Self-help");
            AddCategoryMapping("spiritual", TorznabCatType.AudioAudiobook, "Spiritual & Religious");
            AddCategoryMapping("sports", TorznabCatType.AudioAudiobook, "Sport & Recreation");
            AddCategoryMapping("suspense", TorznabCatType.AudioAudiobook, "Suspense");
            AddCategoryMapping("thriller", TorznabCatType.AudioAudiobook, "Thriller");
            AddCategoryMapping("true-crime", TorznabCatType.AudioAudiobook, "True Crime");
            AddCategoryMapping("tutorial", TorznabCatType.AudioAudiobook, "Tutorial");
            AddCategoryMapping("westerns", TorznabCatType.AudioAudiobook, "Westerns");
            AddCategoryMapping("zombies", TorznabCatType.AudioAudiobook, "Zombies");

            // Category Modifiers
            AddCategoryMapping("anthology", TorznabCatType.AudioAudiobook, "Anthology");
            AddCategoryMapping("bestsellers", TorznabCatType.AudioAudiobook, "Bestsellers");
            AddCategoryMapping("classic", TorznabCatType.AudioAudiobook, "Classic");
            AddCategoryMapping("documentary", TorznabCatType.AudioAudiobook, "Documentary");
            AddCategoryMapping("full-cast", TorznabCatType.AudioAudiobook, "Full Cast");
            AddCategoryMapping("libertarian", TorznabCatType.AudioAudiobook, "Libertarian");
            AddCategoryMapping("military", TorznabCatType.AudioAudiobook, "Military");
            AddCategoryMapping("novel", TorznabCatType.AudioAudiobook, "Novel");
            AddCategoryMapping("short-story", TorznabCatType.AudioAudiobook, "Short Story");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var releases = await PerformQuery(new TorznabQuery());

            await ConfigureIfOK(string.Empty, releases.Any(), () => throw new Exception("Could not find releases from this URL"));

            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var urls = new HashSet<string>
            {
                SiteLink,
                SiteLink + "page/2/",
                SiteLink + "page/3/"
            };

            foreach (var url in urls)
            {
                var searchUrl = url;

                var parameters = new NameValueCollection();

                var searchString = query.GetQueryString().Trim();
                if (!string.IsNullOrWhiteSpace(searchString))
                {
                    searchString = Regex.Replace(searchString, @"[\W]+", " ").Trim();
                    parameters.Set("s", searchString);
                    parameters.Set("tt", "1");
                }

                if (parameters.Count > 0)
                    searchUrl += $"?{parameters.GetQueryString()}";

                var response = await RequestWithCookiesAsync(searchUrl);

                var pageReleases = ParseReleases(response);
                releases.AddRange(pageReleases);

                // Stop fetching the next page when less than 15 results are found.
                if (pageReleases.Count < 15)
                    break;
            }

            return releases;
        }

        public override async Task<byte[]> Download(Uri link)
        {
            var response = await RequestWithCookiesAsync(link.ToString());

            var parser = new HtmlParser();
            var dom = parser.ParseDocument(response.ContentString);

            var hash = dom.QuerySelector("td:contains(\"Info Hash:\") ~ td")?.TextContent.Trim();
            if (hash == null)
                throw new Exception($"Failed to fetch hash from {link}");

            var title = dom.QuerySelector("div.postTitle h1")?.TextContent.Trim();
            if (title == null)
                throw new Exception($"Failed to fetch title from {link}");

            title = StringUtil.MakeValidFileName(title, '_', false);

            var magnet = MagnetUtil.InfoHashToPublicMagnet(hash, title);

            return await base.Download(magnet);
        }

        private List<ReleaseInfo> ParseReleases(WebResult response)
        {
            var releases = new List<ReleaseInfo>();

            var dom = ParseHtmlDocument(response.ContentString);

            var rows = dom.QuerySelectorAll("div.post:has(div[class=\"postTitle\"])");
            foreach (var row in rows)
            {
                var detailsLink = row.QuerySelector("div.postTitle h2 a")?.GetAttribute("href")?.Trim().TrimStart('/');
                var details = new Uri(SiteLink + detailsLink);

                var title = row.QuerySelector("div.postTitle")?.TextContent.Trim();

                var infoString = row.QuerySelector("div.postContent")?.TextContent.Trim() ?? string.Empty;

                var matchFormat = Regex.Match(infoString, @"Format: (.+) \/", RegexOptions.IgnoreCase);
                if (matchFormat.Groups[1].Success && matchFormat.Groups[1].Value.Length > 0 && matchFormat.Groups[1].Value != "?")
                    title += $" [{matchFormat.Groups[1].Value.Trim()}]";

                var matchBitrate = Regex.Match(infoString, @"Bitrate: (.+)File", RegexOptions.IgnoreCase);
                if (matchBitrate.Groups[1].Success && matchBitrate.Groups[1].Value.Length > 0 && matchBitrate.Groups[1].Value != "?")
                    title += $" [{matchBitrate.Groups[1].Value.Trim()}]";

                var matchSize = Regex.Match(infoString, @"File Size: (.+?)s?$", RegexOptions.IgnoreCase);
                var size = matchSize.Groups[1].Success ? ParseUtil.GetBytes(matchSize.Groups[1].Value) : 0;

                var matchDateAdded = Regex.Match(infoString, @"Posted: (\d{1,2} \D{3} \d{4})", RegexOptions.IgnoreCase);
                var publishDate = matchDateAdded.Groups[1].Success && DateTime.TryParseExact(matchDateAdded.Groups[1].Value, "d MMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsedDate) ? parsedDate : DateTime.Now;

                var postInfo = row.QuerySelector("div.postInfo")?.FirstChild?.TextContent.Trim().Replace("\xA0", ";") ?? string.Empty;
                var matchCategory = Regex.Match(postInfo, @"Category: (.+)$", RegexOptions.IgnoreCase);
                var category = matchCategory.Groups[1].Success ? matchCategory.Groups[1].Value.Split(';').Select(c => c.Trim()).ToList() : new List<string>();
                var categories = category.SelectMany(MapTrackerCatDescToNewznab).Distinct().ToList();

                var release = new ReleaseInfo
                {
                    Guid = details,
                    Details = details,
                    Link = details,
                    Title = CleanTitle(title),
                    Category = categories,
                    Size = size,
                    Seeders = 1,
                    Peers = 1,
                    PublishDate = publishDate,
                    DownloadVolumeFactor = 0,
                    UploadVolumeFactor = 1
                };

                var cover = row.QuerySelector("img[src]")?.GetAttribute("src")?.Trim();
                if (!string.IsNullOrEmpty(cover))
                    release.Poster = cover.StartsWith("http") ? new Uri(cover) : new Uri(SiteLink + cover);

                releases.Add(release);
            }

            return releases;
        }

        private static IHtmlDocument ParseHtmlDocument(string response)
        {
            var parser = new HtmlParser();
            var dom = parser.ParseDocument(response);

            var hidden = dom.QuerySelectorAll("div.post.re-ab");
            foreach (var element in hidden)
            {
                var body = dom.CreateElement<IHtmlDivElement>();
                body.ClassList.Add("post");
                body.InnerHtml = Encoding.UTF8.GetString(Convert.FromBase64String(element.TextContent));
                element.Parent.ReplaceChild(body, element);
            }

            return dom;
        }

        private static string CleanTitle(string title)
        {
            title = Regex.Replace(title, @"[\u0000-\u0008\u000A-\u001F\u0100-\uFFFF]", string.Empty, RegexOptions.Compiled);
            title = Regex.Replace(title, @"\s+", " ", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            return title.Trim();
        }
    }
}
