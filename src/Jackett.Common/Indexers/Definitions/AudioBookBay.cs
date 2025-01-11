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
using Jackett.Common.Extensions;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class AudioBookBay : IndexerBase
    {
        public override string Id => "audiobookbay";
        public override string Name => "AudioBook Bay";
        public override string Description => "AudioBook Bay (ABB) is a public Torrent Tracker for AUDIOBOOKS";
        public override string SiteLink { get; protected set; } = "https://audiobookbay.lu/";
        public override string[] AlternativeSiteLinks => new[]
        {
            "https://audiobookbay.lu/",
            "http://audiobookbay.is/",
        };
        public override string[] LegacySiteLinks => new[]
        {
            "https://audiobookbay.la/",
            "http://audiobookbay.net/",
            "http://audiobookbay.nl/",
            "http://audiobookbay.ws/",
            "http://audiobookbay.fi/",
            "http://audiobookbay.se/",
            "http://audiobookbayabb.com/",
            "https://audiobookbay.fi/",
            "https://audiobookbay.li/",
            "https://audiobookbay.se/", // redirects to .is but has invalid CA
            "https://audiobookbay.is/"
        };
        public override string Language => "en-US";
        public override string Type => "public";

        public override int PageSize => 9;

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        public AudioBookBay(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps, ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationData())
        {
            webclient.requestDelay = 6.1;
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

            caps.Categories.AddCategoryMapping(1, TorznabCatType.AudioAudiobook);

            return caps;
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new AudioBookBayRequestGenerator(SiteLink);
        }

        public override IParseIndexerResponse GetParser()
        {
            return new AudioBookBayParser(SiteLink);
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var releases = await PerformQuery(new TorznabQuery());

            await ConfigureIfOK(string.Empty, releases.Any(), () => throw new Exception("Could not find releases from this URL"));

            return IndexerConfigurationStatus.Completed;
        }

        public override async Task<byte[]> Download(Uri link)
        {
            var response = await RequestWithCookiesAsync(link.ToString());

            var parser = new HtmlParser();
            using var dom = parser.ParseDocument(response.ContentString);

            var hash = dom.QuerySelector("td:contains(\"Info Hash:\") ~ td")?.TextContent.Trim();
            if (hash == null)
            {
                throw new Exception($"Failed to fetch hash from {link}");
            }

            var title = dom.QuerySelector("div.postTitle h1")?.TextContent.Trim();
            if (title == null)
            {
                throw new Exception($"Failed to fetch title from {link}");
            }

            title = StringUtil.MakeValidFileName(title, '_', false);

            var magnet = MagnetUtil.InfoHashToPublicMagnet(hash, title);

            return await base.Download(magnet);
        }
    }

    public class AudioBookBayRequestGenerator : IIndexerRequestGenerator
    {
        private readonly string _siteLink;

        public AudioBookBayRequestGenerator(string siteLink)
        {
            _siteLink = siteLink;
        }

        public IndexerPageableRequestChain GetSearchRequests(TorznabQuery query)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            var queryParameters = new NameValueCollection();

            var searchString = query.GetQueryString().Trim();

            if (searchString.IsNotNullOrWhiteSpace())
            {
                searchString = Regex.Replace(searchString, @"[\W]+", " ").Trim().ToLower();
                queryParameters.Set("s", searchString);
                queryParameters.Set("tt", "1");
            }

            pageableRequests.Add(GetPagedRequests(queryParameters));

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(NameValueCollection queryParameters)
        {
            var pageUrls = new HashSet<string>
            {
                _siteLink,
                _siteLink + "page/2/"
            };

            foreach (var pageUrl in pageUrls)
            {
                var searchUrl = pageUrl;

                if (queryParameters.Count > 0)
                {
                    searchUrl += $"?{queryParameters.GetQueryString()}";
                }

                var webRequest = new WebRequest
                {
                    Url = searchUrl,
                    Headers = new Dictionary<string, string>
                    {
                        { "Accept", "text/html" }
                    },
                };

                yield return new IndexerRequest(webRequest);
            }
        }
    }

    public class AudioBookBayParser : IParseIndexerResponse
    {
        private readonly string _siteLink;

        public AudioBookBayParser(string siteLink)
        {
            _siteLink = siteLink;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var releases = new List<ReleaseInfo>();

            using var dom = ParseHtmlDocument(indexerResponse.Content);

            var rows = dom.QuerySelectorAll("div.post:has(div[class=\"postTitle\"])");
            foreach (var row in rows)
            {
                var detailsLink = row.QuerySelector("div.postTitle h2 a")?.GetAttribute("href")?.Trim().TrimStart('/');
                var details = new Uri(_siteLink + detailsLink);

                var title = row.QuerySelector("div.postTitle")?.TextContent.Trim();

                var infoString = row.QuerySelector("div.postContent")?.TextContent.Trim() ?? string.Empty;

                var matchFormat = Regex.Match(infoString, @"Format: (.+) \/", RegexOptions.IgnoreCase);
                if (matchFormat.Groups[1].Success && matchFormat.Groups[1].Value.Length > 0 && matchFormat.Groups[1].Value != "?")
                {
                    title += $" [{matchFormat.Groups[1].Value.Trim()}]";
                }

                var matchBitrate = Regex.Match(infoString, @"Bitrate: (.+)File", RegexOptions.IgnoreCase);
                if (matchBitrate.Groups[1].Success && matchBitrate.Groups[1].Value.Length > 0 && matchBitrate.Groups[1].Value != "?")
                {
                    title += $" [{matchBitrate.Groups[1].Value.Trim()}]";
                }

                var matchSize = Regex.Match(infoString, @"File Size: (.+?)s?$", RegexOptions.IgnoreCase);
                var size = matchSize.Groups[1].Success ? ParseUtil.GetBytes(matchSize.Groups[1].Value) : 0;

                var matchDateAdded = Regex.Match(infoString, @"Posted: (\d{1,2} \D{3} \d{4})", RegexOptions.IgnoreCase);
                var publishDate = matchDateAdded.Groups[1].Success && DateTime.TryParseExact(matchDateAdded.Groups[1].Value, "d MMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsedDate) ? parsedDate : DateTime.Now;

                var postInfo = row.QuerySelector("div.postInfo")?.FirstChild?.TextContent.Trim().Replace("\xA0", ";") ?? string.Empty;
                var matchCategory = Regex.Match(postInfo, @"Category: (.+)$", RegexOptions.IgnoreCase);
                var genres = matchCategory.Groups[1].Success ? matchCategory.Groups[1].Value.Split(';').Select(c => c.Trim()).ToList() : new List<string>();

                releases.Add(new ReleaseInfo
                {
                    Guid = details,
                    Details = details,
                    Link = details,
                    Title = CleanTitle(title),
                    Category = new List<int> { TorznabCatType.AudioAudiobook.ID },
                    Size = size,
                    Seeders = 1,
                    Peers = 1,
                    Poster = GetPosterUrl(row.QuerySelector("img[src]")?.GetAttribute("src")?.Trim()),
                    PublishDate = publishDate,
                    DownloadVolumeFactor = 0,
                    UploadVolumeFactor = 1,
                    Genres = genres
                });
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

        private Uri GetPosterUrl(string cover)
        {
            if (cover.IsNotNullOrWhiteSpace() &&
                Uri.TryCreate(cover.StartsWith("http") ? cover : _siteLink + cover, UriKind.Absolute, out var posterUri) &&
                (posterUri.Scheme == Uri.UriSchemeHttp || posterUri.Scheme == Uri.UriSchemeHttps))
            {
                return posterUri;
            }

            return null;
        }

        private static string CleanTitle(string title)
        {
            title = Regex.Replace(title, @"[\u0000-\u0008\u000A-\u001F\u0100-\uFFFF]", string.Empty, RegexOptions.Compiled);
            title = Regex.Replace(title, @"\s+", " ", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            return title.Trim();
        }
    }
}
