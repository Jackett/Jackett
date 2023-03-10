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
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class AudioBookBay : IndexerBase
    {
        public override string Id => "audiobookbay";
        public override string Name => "AudioBook Bay";
        public override string Description => "AudioBook Bay (ABB) is a public Torrent Tracker for AUDIOBOOKS";
        public override string SiteLink { get; protected set; } = "https://audiobookbay.li/";
        public override string[] AlternativeSiteLinks => new[]
        {
            "https://audiobookbay.li/",
            "https://audiobookbay.se/"
        };
        public override string[] LegacySiteLinks => new[]
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
        public override string Language => "en-US";
        public override string Type => "public";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        public AudioBookBay(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps, ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationData())
        {
            webclient.requestDelay = 3.1;
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                BookSearchParams = new List<BookSearchParam>
                {
                    BookSearchParam.Q
                }
            };

            // Age
            caps.Categories.AddCategoryMapping("children", TorznabCatType.AudioAudiobook, "Children");
            caps.Categories.AddCategoryMapping("teen-young-adult", TorznabCatType.AudioAudiobook, "Teen & Young Adult");
            caps.Categories.AddCategoryMapping("adults", TorznabCatType.AudioAudiobook, "Adults");

            // Category
            caps.Categories.AddCategoryMapping("postapocalyptic", TorznabCatType.AudioAudiobook, "(Post)apocalyptic");
            caps.Categories.AddCategoryMapping("action", TorznabCatType.AudioAudiobook, "Action");
            caps.Categories.AddCategoryMapping("adventure", TorznabCatType.AudioAudiobook, "Adventure");
            caps.Categories.AddCategoryMapping("art", TorznabCatType.AudioAudiobook, "Art");
            caps.Categories.AddCategoryMapping("autobiography-biographies", TorznabCatType.AudioAudiobook, "Autobiography & Biographies");
            caps.Categories.AddCategoryMapping("business", TorznabCatType.AudioAudiobook, "Business");
            caps.Categories.AddCategoryMapping("computer", TorznabCatType.AudioAudiobook, "Computer");
            caps.Categories.AddCategoryMapping("contemporary", TorznabCatType.AudioAudiobook, "Contemporary");
            caps.Categories.AddCategoryMapping("crime", TorznabCatType.AudioAudiobook, "Crime");
            caps.Categories.AddCategoryMapping("detective", TorznabCatType.AudioAudiobook, "Detective");
            caps.Categories.AddCategoryMapping("doctor-who-sci-fi", TorznabCatType.AudioAudiobook, "Doctor Who");
            caps.Categories.AddCategoryMapping("education", TorznabCatType.AudioAudiobook, "Education");
            caps.Categories.AddCategoryMapping("fantasy", TorznabCatType.AudioAudiobook, "Fantasy");
            caps.Categories.AddCategoryMapping("general-fiction", TorznabCatType.AudioAudiobook, "General Fiction");
            caps.Categories.AddCategoryMapping("historical-fiction", TorznabCatType.AudioAudiobook, "Historical Fiction");
            caps.Categories.AddCategoryMapping("history", TorznabCatType.AudioAudiobook, "History");
            caps.Categories.AddCategoryMapping("horror", TorznabCatType.AudioAudiobook, "Horror");
            caps.Categories.AddCategoryMapping("humor", TorznabCatType.AudioAudiobook, "Humor");
            caps.Categories.AddCategoryMapping("lecture", TorznabCatType.AudioAudiobook, "Lecture");
            caps.Categories.AddCategoryMapping("lgbt", TorznabCatType.AudioAudiobook, "LGBT");
            caps.Categories.AddCategoryMapping("literature", TorznabCatType.AudioAudiobook, "Literature");
            caps.Categories.AddCategoryMapping("litrpg", TorznabCatType.AudioAudiobook, "LitRPG");
            caps.Categories.AddCategoryMapping("general-non-fiction", TorznabCatType.AudioAudiobook, "Misc. Non-fiction");
            caps.Categories.AddCategoryMapping("mystery", TorznabCatType.AudioAudiobook, "Mystery");
            caps.Categories.AddCategoryMapping("paranormal", TorznabCatType.AudioAudiobook, "Paranormal");
            caps.Categories.AddCategoryMapping("plays-theater", TorznabCatType.AudioAudiobook, "Plays & Theater");
            caps.Categories.AddCategoryMapping("poetry", TorznabCatType.AudioAudiobook, "Poetry");
            caps.Categories.AddCategoryMapping("political", TorznabCatType.AudioAudiobook, "Political");
            caps.Categories.AddCategoryMapping("radio-productions", TorznabCatType.AudioAudiobook, "Radio Productions");
            caps.Categories.AddCategoryMapping("romance", TorznabCatType.AudioAudiobook, "Romance");
            caps.Categories.AddCategoryMapping("sci-fi", TorznabCatType.AudioAudiobook, "Sci-Fi");
            caps.Categories.AddCategoryMapping("science", TorznabCatType.AudioAudiobook, "Science");
            caps.Categories.AddCategoryMapping("self-help", TorznabCatType.AudioAudiobook, "Self-help");
            caps.Categories.AddCategoryMapping("spiritual", TorznabCatType.AudioAudiobook, "Spiritual & Religious");
            caps.Categories.AddCategoryMapping("sports", TorznabCatType.AudioAudiobook, "Sport & Recreation");
            caps.Categories.AddCategoryMapping("suspense", TorznabCatType.AudioAudiobook, "Suspense");
            caps.Categories.AddCategoryMapping("thriller", TorznabCatType.AudioAudiobook, "Thriller");
            caps.Categories.AddCategoryMapping("true-crime", TorznabCatType.AudioAudiobook, "True Crime");
            caps.Categories.AddCategoryMapping("tutorial", TorznabCatType.AudioAudiobook, "Tutorial");
            caps.Categories.AddCategoryMapping("westerns", TorznabCatType.AudioAudiobook, "Westerns");
            caps.Categories.AddCategoryMapping("zombies", TorznabCatType.AudioAudiobook, "Zombies");

            // Category Modifiers
            caps.Categories.AddCategoryMapping("anthology", TorznabCatType.AudioAudiobook, "Anthology");
            caps.Categories.AddCategoryMapping("bestsellers", TorznabCatType.AudioAudiobook, "Bestsellers");
            caps.Categories.AddCategoryMapping("classic", TorznabCatType.AudioAudiobook, "Classic");
            caps.Categories.AddCategoryMapping("documentary", TorznabCatType.AudioAudiobook, "Documentary");
            caps.Categories.AddCategoryMapping("full-cast", TorznabCatType.AudioAudiobook, "Full Cast");
            caps.Categories.AddCategoryMapping("libertarian", TorznabCatType.AudioAudiobook, "Libertarian");
            caps.Categories.AddCategoryMapping("military", TorznabCatType.AudioAudiobook, "Military");
            caps.Categories.AddCategoryMapping("novel", TorznabCatType.AudioAudiobook, "Novel");
            caps.Categories.AddCategoryMapping("short-story", TorznabCatType.AudioAudiobook, "Short Story");

            return caps;
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
