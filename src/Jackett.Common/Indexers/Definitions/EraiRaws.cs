using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;

namespace Jackett.Common.Indexers.Definitions
{
    public class EraiRaws : IndexerBase
    {
        public override string Id => "erai-raws";
        public override string Name => "Erai-Raws";
        public override string Description => "Erai-Raws is a Semi-Private team release site for Anime subtitles.";
        public override string SiteLink { get; protected set; } = "https://www.erai-raws.info/";
        public override string[] LegacySiteLinks => new[]
        {
            "https://erairaws.mrunblock.bond/",
            "https://erairaws.nocensor.cloud/",
            "https://beta.erai-raws.info/"
        };
        public override string Language => "en-US";
        public override string Type => "semi-private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        const string RSS_FEED = "feed/?";
        public EraiRaws(IIndexerConfigurationService configService, Utils.Clients.WebClient wc, Logger l,
            IProtectionService ps, ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationData())
        {
            var rssKey = new StringConfigurationItem("RSSKey") { Value = "" };
            configData.AddDynamic("rssKey", rssKey);
            configData.AddDynamic("rssKeyHelp", new DisplayInfoConfigurationItem(string.Empty, "Find the RSS Key by accessing <a href=\"https://www.erai-raws.info/rss-page/\" target =_blank>Erai-Raws RSS page</a> while you're logged in. Copy the <i>All RSS</i> URL, the RSS Key is the last part. Example: for the URL <b>.../feed/?type=torrent&0879fd62733b8db8535eb1be2333</b> the RSS Key is <b>0879fd62733b8db8535eb1be2333</b>"));

            var rssCategories = new SingleSelectConfigurationItem("Select a Category", new Dictionary<string, string>
                {
                    {"none", "-- Categories --"},
                    {"episodes/", "Airing"},
                    {"batches/", "Batches"},
                    {"specials/", "Movies or Special Episodes"},
                    {"encodes/", "Encodings"},
                    {"raws/", "Raws"}
                })
            { Value = "none" };
            configData.AddDynamic("rssCategories", rssCategories);

            var rssResolution = new SingleSelectConfigurationItem("Select a Resolution", new Dictionary<string, string>
                {
                    {"none", "-- Resolution --"},
                    {"res=1080p&", "1080p"},
                    {"res=720p&", "720p"},
                    {"res=SD&", "SD"}
                })
            { Value = "none" };
            configData.AddDynamic("rssResolution", rssResolution);

            var rssLinkType = new SingleSelectConfigurationItem("Select a Link Type", new Dictionary<string, string>
                {
                    {"type=torrent&", "Torrent"},
                    {"type=magnet&", "Magnet"}
                })
            { Value = "type=magnet&" };
            configData.AddDynamic("rssLinkType", rssLinkType);

            var rssSubtitles = new MultiSelectConfigurationItem("Select one or more Subtitles (None ticked = ALL)", new Dictionary<string, string>
                {
                    {"subs[]=us&", "English"},
                    {"subs[]=br&", "Portuguese(Brazil)"},
                    {"subs[]=mx&", "Spanish(Latin_America)"},
                    {"subs[]=es&", "Spanish"},
                    {"subs[]=sa&", "Arabic"},
                    {"subs[]=fr&", "French"},
                    {"subs[]=de&", "German"},
                    {"subs[]=it&", "Italian"},
                    {"subs[]=ru&", "Russian"},
                    {"subs[]=jp&", "Japanese"},
                    {"subs[]=pt&", "Portuguese"},
                    {"subs[]=pl&", "Polish"},
                    {"subs[]=nl&", "Dutch"},
                    {"subs[]=no&", "Norwegian"},
                    {"subs[]=fi&", "Finnish"},
                    {"subs[]=tr&", "Turkish"},
                    {"subs[]=se&", "Swedish"},
                    {"subs[]=gr&", "Greek"},
                    {"subs[]=il&", "Hebrew"},
                    {"subs[]=ro&", "Romanian"},
                    {"subs[]=id&", "Indonesian"},
                    {"subs[]=th&", "Thai"},
                    {"subs[]=kr&", "Korean"},
                    {"subs[]=dk&", "Danish"},
                    {"subs[]=cn&", "Chinese(Simplified&Traditional)"},
                    {"subs[]=bg&", "Bulgarian"},
                    {"subs[]=vn&", "Vietnamese"},
                    {"subs[]=in&", "Hindi"},
                    {"subs[]=lk&", "Tamil"},
                    {"subs[]=ua&", "Ukrainian"},
                    {"subs[]=hu&", "Hungarian"},
                    {"subs[]=cz&", "Czech"},
                    {"subs[]=hr&", "Croatian"},
                    {"subs[]=my&", "Malaysian"},
                    {"subs[]=sk&", "Slovakian"},
                    {"subs[]=ph&", "Filipino"}
                })
            { Values = new[] { "" } };
            configData.AddDynamic("rssSubtitles", rssSubtitles);

            configData.AddDynamic(
                "DDoS-Guard",
                new DisplayInfoConfigurationItem("", "This site may use DDoS-Guard Protection, therefore Jackett requires <a href='https://github.com/Jackett/Jackett#configuring-flaresolverr' target='_blank'>FlareSolverr</a> to access it.")
            );
            // Add note that download stats are not available
            configData.AddDynamic(
                "download-stats-unavailable",
                new DisplayInfoConfigurationItem("", "<p>Please note that the following stats are not available for this indexer. Default values are used instead. </p><ul><li>Files</li><li>Seeders</li><li>Leechers</li></ul>")
            );

            configData.AddDynamic("include-subs", new BoolConfigurationItem("Enable appending SubTitles to the Title"));

            // Config item for title detail parsing
            configData.AddDynamic("title-detail-parsing", new BoolConfigurationItem("Enable Title Detail Parsing"));
            configData.AddDynamic(
                "title-detail-parsing-help",
                new DisplayInfoConfigurationItem("", "Title Detail Parsing will attempt to determine the season and episode number from the release names and reformat them as a suffix in the format S1E1. If successful, this should provide better matching in applications such as Sonarr.")
            );
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q
                }
            };

            caps.Categories.AddCategoryMapping(1, TorznabCatType.TVAnime, "Anime - Sub");

            return caps;
        }

        private TitleParser titleParser = new TitleParser();
        private string RSS_Key => ((StringConfigurationItem)configData.GetDynamic("rssKey")).Value;
        private string RSS_Categories => ((SingleSelectConfigurationItem)configData.GetDynamic("rssCategories")).Value;
        private string RSS_Resolution => ((SingleSelectConfigurationItem)configData.GetDynamic("rssResolution")).Value;
        private string RSS_LinkType => ((SingleSelectConfigurationItem)configData.GetDynamic("rssLinkType")).Value;
        private string GetRSS_Subtitles()
        {
            var rssSubtitles = (MultiSelectConfigurationItem)configData.GetDynamic("rssSubtitles");
            return string.Join("", rssSubtitles.Values);
        }
        private bool IsTitleDetailParsingEnabled => ((BoolConfigurationItem)configData.GetDynamic("title-detail-parsing")).Value;
        private bool IsSubsEnabled => ((BoolConfigurationItem)configData.GetDynamic("include-subs")).Value;
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
            var RssFeedUri = SiteLink +
                RSS_Categories.Replace("none", string.Empty) +
                RSS_FEED +
                RSS_Resolution.Replace("none", string.Empty) +
                GetRSS_Subtitles() +
                RSS_LinkType +
                "token=" +
                RSS_Key;

            // Retrieve RSS feed
            var result = await RequestWithCookiesAndRetryAsync(RssFeedUri);
            if (result.IsRedirect)
                result = await FollowIfRedirect(result);
            if (result.ContentString.Contains("<status>403</status>"))
            {
                logger.Error("[EraiRaws] 403 Forbidden");
                throw new Exception("The RSSkey may need to be replaced as EraiRaws returned 403 Forbidden.");
            }

            // Parse as XML document
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(result.ContentString);

            var nsm = new XmlNamespaceManager(xmlDocument.NameTable);
            nsm.AddNamespace("erai", "https://www.erai-raws.info/rss-page/");

            // Parse to RssFeedItems
            using var xmlNodes = xmlDocument.GetElementsByTagName("item");
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
                    logger.Warn($"Could not parse {Name} RSS item '{node.OuterXml}'");
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
                    logger.Warn($"Failed to parse {Name} RSS feed item '{fi.Title}' due to malformed publish date.");
                    continue;
                }

                if (releaseInfo.MagnetLink == null && string.IsNullOrWhiteSpace(releaseInfo.InfoHash))
                {
                    logger.Warn($"Failed to parse {Name} RSS feed item '{fi.Title}' due to malformed link URI and no infohash available.");
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

                var subs = "";
                if (IsSubsEnabled)
                {
                    subs = string.Concat(" JAPANESE [Subs: ", fi.SubTitles, "]");
                }
                yield return new ReleaseInfo
                {
                    Title = string.Concat(fi.Title, " - ", fi.Quality, subs),
                    Guid = guid,
                    MagnetUri = fi.MagnetLink,
                    InfoHash = fi.InfoHash,
                    Details = fi.DetailsLink,
                    PublishDate = fi.PublishDate.Value.ToLocalTime().DateTime,
                    Category = MapTrackerCatToNewznab("1"),

                    // Download stats are not available through scraping so set some mock values.
                    Size = fi.Size,
                    Files = 1,
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
                var subs = rssItem.SelectSingleNode("erai:subtitles", nsm)?.InnerText;
                if (string.IsNullOrEmpty(subs))
                {
                    subs = "[]";
                }

                item = new RssFeedItem
                {
                    Title = title,
                    Link = link,
                    InfoHash = infoHash,
                    PublishDate = publishDate,
                    Size = string.IsNullOrWhiteSpace(size) ? "512MB" : size,
                    Description = description,
                    Quality = quality,
                    SubTitles = subs
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

            public string SubTitles { get; private set; }

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
                Size = ParseUtil.GetBytes(feedItem.Size);
                DetailsLink = ParseDetailsLink(feedItem.Description);
                InfoHash = feedItem.InfoHash;
                SubTitles = feedItem.SubTitles.Replace("[", " ").Replace("]", " ").ToUpper();

                if (Uri.TryCreate(feedItem.Link, UriKind.Absolute, out Uri magnetUri))
                    MagnetLink = magnetUri;

                if (DateTimeOffset.TryParse(feedItem.PublishDate, out DateTimeOffset publishDate))
                    PublishDate = publishDate;
            }

            private string StripTitle(string rawTitle)
            {
                var prefixStripped = Regex.Replace(rawTitle, "^\\[.+?\\]", "");
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

            public string SubTitles { get; }

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
