using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Newtonsoft.Json.Linq;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;

namespace Jackett.Common.Indexers
{
    public class EraiRaws : BaseWebIndexer
    {
        const string RSS_PATH = "rss-all-magnet";

        private readonly IReadOnlyDictionary<string, int> sizeEstimates = new Dictionary<string, int>() {
            { "1080p", 1332 }, // ~1.3GiB
            { "720p", 700 },
            { "540p", 350 }
        };

        public override string[] AlternativeSiteLinks { get; protected set; } = {
            "https://www.erai-raws.info/",
            "https://erairaws.nocensor.space/"
        };

        public EraiRaws(IIndexerConfigurationService configService, Utils.Clients.WebClient wc, Logger l,
            IProtectionService ps, ICacheService cs)
            : base(id: "erai-raws",
                   name: "Erai-Raws",
                   description: "Erai-Raws is a team release site for Anime subtitles.",
                   link: "https://www.erai-raws.info/",
                   caps: new TorznabCapabilities
                   {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q
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
            Language = "en-us";
            Type = "public";

            // Add note that download stats are not available
            configData.AddDynamic(
                "download-stats-unavailable",
                new DisplayInfoConfigurationItem("", "<p>Please note that the following stats are not available for this indexer. Default values are used instead. </p><ul><li>Size</li><li>Seeders</li><li>Leechers</li><li>Download Factor</li><li>Upload Factor</li></ul>")
            );

            // Config item for title detail parsing
            configData.AddDynamic("title-detail-parsing", new BoolConfigurationItem("Enable Title Detail Parsing"));
            configData.AddDynamic(
                "title-detail-parsing-help",
                new DisplayInfoConfigurationItem("", "Title Detail Parsing will attempt to determine the season and episode number from the release names and reformat them as a suffix in the format S1E1. If successful, this should provide better matching in applications such as Sonarr.")
            );

            // Configure the category mappings
            AddCategoryMapping(1, TorznabCatType.TVAnime, "Anime - Sub");
        }

        private TitleParser titleParser = new TitleParser();

        private bool IsTitleDetailParsingEnabled => ((BoolConfigurationItem)configData.GetDynamic("title-detail-parsing")).Value;

        public string RssFeedUri
        {
            get
            {
                return string.Concat(SiteLink, RSS_PATH);
            }
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var releases = await PerformQuery(new TorznabQuery());

            await ConfigureIfOK(string.Empty, releases.Any(), () =>
                throw new Exception("Could not find releases from this URL"));

            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var feedItems = await GetItemsFromFeed();
            var eraiRawsReleaseInfo = ConvertFeedItemsToEraiRawsReleaseInfo(feedItems);

            // Perform basic filter within Jackett
            var filteredItems = FilterForQuery(query, eraiRawsReleaseInfo);

            // Convert to release info
            return ConvertEraiRawsInfoToJackettInfo(filteredItems);
        }

        private async Task<IEnumerable<RssFeedItem>> GetItemsFromFeed()
        {
            // Retrieve RSS feed
            var result = await RequestWithCookiesAndRetryAsync(RssFeedUri);

            // Parse as XML document
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(result.ContentString);

            // Parse to RssFeedItems
            var xmlNodes = xmlDocument.GetElementsByTagName("item");
            List<RssFeedItem> feedItems = new List<RssFeedItem>();
            foreach (var n in xmlNodes)
            {
                var node = (XmlNode)n;

                if (RssFeedItem.TryParse(node, out RssFeedItem item))
                {
                    feedItems.Add(item);
                }
                else
                {
                    logger.Warn($"Could not parse {DisplayName} RSS item '{node.InnerText}'");
                }
            }

            return feedItems;
        }

        private IEnumerable<EraiRawsReleaseInfo> ConvertFeedItemsToEraiRawsReleaseInfo(IEnumerable<RssFeedItem> feedItems)
        {
            foreach (var fi in feedItems)
            {
                EraiRawsReleaseInfo releaseInfo = new EraiRawsReleaseInfo(fi);

                // Validate the release
                if (releaseInfo.PublishDate == null)
                {
                    logger.Warn($"Failed to parse {DisplayName} RSS feed item '{fi.Title}' due to malformed publish date.");
                    continue;
                }

                if (releaseInfo.MagnetLink == null)
                {
                    logger.Warn($"Failed to parse {DisplayName} RSS feed item '{fi.Title}' due to malformed link URI.");
                    continue;
                }

                // Run the title parser for the details link
                releaseInfo.DetailsLink = new Uri(string.Format("{0}anime-list/{1}", SiteLink, titleParser.GetUrlSlug(releaseInfo.Title)));

                // If enabled, perform detailed title parsing
                if (IsTitleDetailParsingEnabled)
                {
                    releaseInfo.Title = titleParser.Parse(releaseInfo.Title);
                }

                yield return releaseInfo;
            }
        }

        private static IEnumerable<EraiRawsReleaseInfo> FilterForQuery(TorznabQuery query, IEnumerable<EraiRawsReleaseInfo> feedItems)
        {
            foreach (var fi in feedItems)
            {
                if (!query.MatchQueryStringAND(fi.Title))
                    continue;

                yield return fi;
            }
        }

        private IEnumerable<ReleaseInfo> ConvertEraiRawsInfoToJackettInfo(IEnumerable<EraiRawsReleaseInfo> feedItems)
        {
            foreach (var fi in feedItems)
            {
                yield return new ReleaseInfo
                {
                    Title = string.Concat(fi.Title, " - ", fi.Quality),
                    Guid = fi.MagnetLink,
                    MagnetUri = fi.MagnetLink,
                    Details = fi.DetailsLink,
                    PublishDate = fi.PublishDate.Value.ToLocalTime().DateTime,
                    Category = MapTrackerCatToNewznab("1"),

                    // Download stats are not available through scraping so set some mock values.
                    Size = GetSizeEstimate(fi),
                    Seeders = 1,
                    Peers = 2,
                    DownloadVolumeFactor = 0,
                    UploadVolumeFactor = 1
                };
            }
        }

        /// <summary>
        /// Get an estimate of the file size based on the release info.
        /// </summary>
        /// <remarks>
        /// These estimates are currently only based on Quality. They will be very inaccurate for batch releases.
        /// </remarks>
        private long GetSizeEstimate(EraiRawsReleaseInfo releaseInfo)
        {
            long sizeEstimateInMiB = 256;
            if (sizeEstimates.ContainsKey(releaseInfo.Quality.ToLower()))
            {
                sizeEstimateInMiB = sizeEstimates[releaseInfo.Quality.ToLower()];
            }

            // Convert to bytes and return
            return sizeEstimateInMiB * (1024 * 1024);
        }

        private static string PrefixOrDefault(string prefix, string value, string def = "")
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return def;
            }
            else
            {
                return string.Concat(prefix, value);
            }
        }

        /// <summary>
        /// Raw RSS feed item containing the data as received.
        /// </summary>
        private class RssFeedItem
        {
            public static bool TryParse(XmlNode rssItem, out RssFeedItem item)
            {
                var title = rssItem.SelectSingleNode("title")?.InnerText;
                var link = rssItem.SelectSingleNode("link")?.InnerText;
                var publishDate = rssItem.SelectSingleNode("pubDate")?.InnerText;

                if (string.IsNullOrWhiteSpace(title) ||
                    string.IsNullOrWhiteSpace(link) ||
                    string.IsNullOrWhiteSpace(publishDate))
                {
                    // One of the properties was empty so fail to parse
                    item = null;
                    return false;
                }

                item = new RssFeedItem(title, link, publishDate);
                return true;
            }

            private RssFeedItem(string title, string link, string publishDate)
            {
                Title = title;
                Link = link;
                PublishDate = publishDate;
            }

            public string Title { get; set; }

            public string Link { get; }

            public string PublishDate { get; }
        }

        /// <summary>
        /// Details of an EraiRaws release
        /// </summary>
        private class EraiRawsReleaseInfo
        {
            public EraiRawsReleaseInfo(RssFeedItem feedItem)
            {
                var splitTitle = SplitQualityAndTitle(feedItem.Title);

                Quality = splitTitle.quality;
                Title = splitTitle.title;

                if (Uri.TryCreate(feedItem.Link, UriKind.Absolute, out Uri magnetUri))
                {
                    MagnetLink = magnetUri;
                }

                if (DateTimeOffset.TryParse(feedItem.PublishDate, out DateTimeOffset publishDate))
                {
                    PublishDate = publishDate;
                }
            }

            private (string quality, string title) SplitQualityAndTitle(string rawTitle)
            {
                var match = Regex.Match(rawTitle, @"^\[(?<quality>[0-9]+[ip])\] (?<title>.*)$", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(0.5));
                if (match.Success)
                {
                    return (match.Groups["quality"].Value, match.Groups["title"].Value);
                }

                return (string.Empty, rawTitle);
            }

            public string Quality { get; }

            public string Title { get; set; }

            public Uri MagnetLink { get; }

            public Uri DetailsLink { get; set; }

            public DateTimeOffset? PublishDate { get; }
        }

        public class TitleParser
        {
            private readonly Dictionary<string, string> DETAIL_SEARCH_SEASON = new Dictionary<string, string> {
                { " Season (?<detail>[0-9]+)", "" }, // "Season 2"
                { " (?<detail>[0-9]+)(st|nd|rd|th) Season", "" }, // "2nd Season"
                { " Part (?<detail>[0-9]+) – ", " – " }, // "<title> Part 2 – <episode>"
                { " (?<detail>[0-9]+) – ", " – " } // "<title> 2 – <episode>" - NOT A HYPHEN!
            };

            private readonly Dictionary<string, string> DETAIL_SEARCH_EPISODE = new Dictionary<string, string> {
                { " – (?<detail>[0-9]+)$", " – " }, // "<title> – <episode>" <end_of_title> - NOT A HYPHEN!
                { " – (?<detail>[0-9]+) ", " – " } // "<title> – <episode> ..." - NOT A HYPHEN!
            };

            private const string TITLE_URL_SLUG_REGEX = @"^(?<url_slug>.+) –";

            public string Parse(string title)
            {
                var results = SearchTitleForDetails(title, new Dictionary<string, Dictionary<string, string>> {
                    { "episode", DETAIL_SEARCH_EPISODE },
                    { "season", DETAIL_SEARCH_SEASON }
                });

                var seasonEpisodeIdentifier = string.Concat(
                    PrefixOrDefault("S", results.details["season"]).Trim(),
                    PrefixOrDefault("E", results.details["episode"]).Trim()
                    );

                // If title still contains the strange hyphen, insert the identifier after it. Otherwise put it at the end.
                int strangeHyphenPosition = results.strippedTitle.LastIndexOf("–");
                if (strangeHyphenPosition > -1)
                {
                    return string.Concat(
                        results.strippedTitle.Substring(0, strangeHyphenPosition).Trim(),
                        " – ",
                        seasonEpisodeIdentifier,
                        " ",
                        results.strippedTitle.Substring(strangeHyphenPosition + 1).Trim()
                    ).Trim();
                }

                return string.Concat(
                    results.strippedTitle.Trim(),
                    " ",
                    seasonEpisodeIdentifier
                ).Trim();
            }

            public string GetUrlSlug(string title)
            {
                var match = Regex.Match(title, TITLE_URL_SLUG_REGEX, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(0.5));
                if (!match.Success)
                {
                    return null;
                }

                var urlSlug = match.Groups["url_slug"].Value.ToLowerInvariant();
                urlSlug = Regex.Replace(urlSlug, "[^a-zA-Z0-9]", "-");
                urlSlug = urlSlug.Trim('-');
                while (urlSlug.Contains("--"))
                {
                    urlSlug = urlSlug.Replace("--", "-");
                }

                return urlSlug;
            }

            private static (string strippedTitle, Dictionary<string, string> details) SearchTitleForDetails(string title, Dictionary<string, Dictionary<string, string>> definition)
            {
                Dictionary<string, string> details = new Dictionary<string, string>();
                foreach (var search in definition)
                {
                    var searchResult = SearchTitleForDetail(title, search.Value);
                    details.Add(search.Key, searchResult.detail);
                    title = searchResult.strippedTitle;
                }

                return (title, details);
            }

            private static (string strippedTitle, string detail) SearchTitleForDetail(string title, Dictionary<string, string> searchReplacePatterns)
            {
                foreach (var srp in searchReplacePatterns)
                {
                    var match = Regex.Match(title, srp.Key, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(0.5));
                    if (match.Success)
                    {
                        string detail = match.Groups["detail"].Value;
                        var strippedTitle = Regex.Replace(title, srp.Key, srp.Value, RegexOptions.IgnoreCase);
                        return (strippedTitle, detail);
                    }
                }

                // Nothing found so return null
                return (title, "");
            }
        }
    }
}
