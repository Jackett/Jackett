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
        const string RSS_PATH = "feed/?type=magnet";

        public override string[] AlternativeSiteLinks { get; protected set; } = {
            "https://www.erai-raws.info/",
            "https://beta.erai-raws.info/",
            "https://erairaws.nocensor.biz/"
        };

        public override string[] LegacySiteLinks { get; protected set; } = {
            "https://erairaws.nocensor.space/",
            "https://erairaws.nocensor.work/"
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
            Language = "en-US";
            Type = "public";

            configData.AddDynamic(
                "DDoS-Guard",
                new DisplayInfoConfigurationItem("", "This site may use DDoS-Guard Protection, therefore Jackett requires <a href='https://github.com/Jackett/Jackett#configuring-flaresolverr' target='_blank'>FlareSolver</a> to access it.")
            );
            // Add note that download stats are not available
            configData.AddDynamic(
                "download-stats-unavailable",
                new DisplayInfoConfigurationItem("", "<p>Please note that the following stats are not available for this indexer. Default values are used instead. </p><ul><li>Seeders</li><li>Leechers</li><li>Download Factor</li><li>Upload Factor</li></ul>")
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
            if (result.IsRedirect)
                await FollowIfRedirect(result);

            // Parse as XML document
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(result.ContentString);

            var nsm = new XmlNamespaceManager(xmlDocument.NameTable);
            nsm.AddNamespace("erai", "https://www.erai-raws.info/rss-page/");

            // Parse to RssFeedItems
            var xmlNodes = xmlDocument.GetElementsByTagName("item");
            List<RssFeedItem> feedItems = new List<RssFeedItem>();
            foreach (var n in xmlNodes)
            {
                var node = (XmlNode)n;

                if (RssFeedItem.TryParse(nsm, node, out RssFeedItem item))
                {
                    feedItems.Add(item);
                }
                else
                {
                    logger.Warn($"Could not parse {DisplayName} RSS item '{node.OuterXml}'");
                }
            }

            return feedItems;
        }

        private IEnumerable<EraiRawsReleaseInfo> ConvertFeedItemsToEraiRawsReleaseInfo(IEnumerable<RssFeedItem> feedItems)
        {
            foreach (var fi in feedItems)
            {
                var releaseInfo = new EraiRawsReleaseInfo(fi);

                // Validate the release
                if (releaseInfo.PublishDate == null)
                {
                    logger.Warn($"Failed to parse {DisplayName} RSS feed item '{fi.Title}' due to malformed publish date.");
                    continue;
                }

                if (releaseInfo.MagnetLink == null && string.IsNullOrWhiteSpace(releaseInfo.InfoHash))
                {
                    logger.Warn($"Failed to parse {DisplayName} RSS feed item '{fi.Title}' due to malformed link URI and no infohash available.");
                    continue;
                }

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
                var guid = fi.MagnetLink;
                if (guid == null)
                {
                    // Magnet link is not available so generate something unique
                    var builder = new UriBuilder(fi.DetailsLink);
                    if (!string.IsNullOrWhiteSpace(builder.Query))
                    {
                        builder.Query += "&";
                    }
                    builder.Query += $"infoHash={fi.InfoHash}";
                    guid = builder.Uri;
                }

                yield return new ReleaseInfo
                {
                    Title = string.Concat(fi.Title, " - ", fi.Quality),
                    Guid = guid,
                    MagnetUri = fi.MagnetLink,
                    InfoHash = fi.InfoHash,
                    Details = fi.DetailsLink,
                    PublishDate = fi.PublishDate.Value.ToLocalTime().DateTime,
                    Category = MapTrackerCatToNewznab("1"),

                    // Download stats are not available through scraping so set some mock values.
                    Size = fi.Size,
                    Seeders = 1,
                    Peers = 2,
                    DownloadVolumeFactor = 0,
                    UploadVolumeFactor = 1
                };
            }
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
            public static bool TryParse(XmlNamespaceManager nsm, XmlNode rssItem, out RssFeedItem item)
            {
                var title = rssItem.SelectSingleNode("title")?.InnerText;
                var link = rssItem.SelectSingleNode("link")?.InnerText;
                var publishDate = rssItem.SelectSingleNode("pubDate")?.InnerText;
                var infoHash = rssItem.SelectSingleNode("erai:infohash", nsm)?.InnerText;
                var size = rssItem.SelectSingleNode("erai:size", nsm)?.InnerText;
                var description = rssItem.SelectSingleNode("description")?.InnerText;
                var quality = rssItem.SelectSingleNode("erai:resolution", nsm)?.InnerText;

                item = new RssFeedItem
                {
                    Title = title,
                    Link = link,
                    InfoHash = infoHash,
                    PublishDate = publishDate,
                    Size = size,
                    Description = description,
                    Quality = quality
                };
                return item.IsValid();
            }

            private RssFeedItem()
            {
                // Nothing to do
            }

            public string Title { get; set; }

            public string Link { get; private set; }

            public string InfoHash { get; private set; }

            public string PublishDate { get; private set; }

            public string Size { get; private set; }

            public string Description { get; private set; }

            public string Quality { get; private set; }

            /// <summary>
            /// Check there is enough information to process the item.
            /// </summary>
            private bool IsValid()
            {
                var missingBothHashAndLink = string.IsNullOrWhiteSpace(Link) && string.IsNullOrWhiteSpace(InfoHash);

                return !(missingBothHashAndLink ||
                    string.IsNullOrWhiteSpace(Title) ||
                    string.IsNullOrWhiteSpace(PublishDate) ||
                    string.IsNullOrWhiteSpace(Size) ||
                    string.IsNullOrWhiteSpace(Quality));
            }
        }

        /// <summary>
        /// Details of an EraiRaws release
        /// </summary>
        private class EraiRawsReleaseInfo
        {
            public EraiRawsReleaseInfo(RssFeedItem feedItem)
            {
                Title = StripTitle(feedItem.Title);
                Quality = feedItem.Quality;
                Size = ReleaseInfo.GetBytes(feedItem.Size);
                DetailsLink = ParseDetailsLink(feedItem.Description);
                InfoHash = feedItem.InfoHash;

                if (Uri.TryCreate(feedItem.Link, UriKind.Absolute, out Uri magnetUri))
                {
                    MagnetLink = magnetUri;
                }

                if (DateTimeOffset.TryParse(feedItem.PublishDate, out DateTimeOffset publishDate))
                {
                    PublishDate = publishDate;
                }
            }

            private string StripTitle(string rawTitle)
            {
                var prefixStripped = Regex.Replace(rawTitle, "^\\[.+?\\] ", "");
                var suffixStripped = Regex.Replace(prefixStripped, " \\[.+\\]", "");
                return suffixStripped.Trim();
            }

            private Uri ParseDetailsLink(string description)
            {
                var match = Regex.Match(description, "href=\"(.+?)\"");
                if (match.Success)
                {
                    var detailsLinkText = match.Groups[1].Value;
                    if (Uri.TryCreate(detailsLinkText, UriKind.Absolute, out Uri detailsLink))
                    {
                        return detailsLink;
                    }
                }

                return null;
            }

            public string Quality { get; }

            public string Title { get; set; }

            public Uri MagnetLink { get; }

            public string InfoHash { get; }

            public Uri DetailsLink { get; set; }

            public DateTimeOffset? PublishDate { get; }

            public long Size { get; }
        }

        public class TitleParser
        {
            private readonly Dictionary<string, string> DETAIL_SEARCH_SEASON = new Dictionary<string, string> {
                { " Season (?<detail>[0-9]+)", "" }, // "Season 2"
                { " (?<detail>[0-9]+)(st|nd|rd|th) Season", "" }, // "2nd Season"
                { " Part (?<detail>[0-9]+) - ", " - " }, // "<title> Part 2 - <episode>"
                { " (?<detail>[0-9]+) - ", " - " } // "<title> 2 - <episode>"
            };

            private readonly Dictionary<string, string> DETAIL_SEARCH_EPISODE = new Dictionary<string, string> {
                { " - (?<detail>[0-9]+)$", " - " }, // "<title> - <episode>" <end_of_title>
                { " - (?<detail>[0-9]+) ", " - " } // "<title> - <episode> ..."
            };

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

                // If title still contains the hyphen, insert the identifier after it. Otherwise put it at the end.
                int strangeHyphenPosition = results.strippedTitle.LastIndexOf("-");
                if (strangeHyphenPosition > -1)
                {
                    return string.Concat(
                        results.strippedTitle.Substring(0, strangeHyphenPosition).Trim(),
                        " - ",
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
