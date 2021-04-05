using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
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

        private readonly Dictionary<string, string> DETAIL_SEARCH_SEASON = new Dictionary<string, string> {
            { " Season (?<detail>[0-9]+)", "" }, // "Season 2"
            { " (?<detail>[0-9]+)(st|nd|rd|th) Season", "" }, // "2nd Season"
            { " (?<detail>[0-9]+) – ", " – " } // "<title> 2 – <episode>" - NOT A HYPHEN!
        };

        private readonly Dictionary<string, string> DETAIL_SEARCH_EPISODE = new Dictionary<string, string> {
            { " – (?<detail>[0-9]+)$", " – " }, // "<title> – <episode>" <end_of_title> - NOT A HYPHEN!
            { " – (?<detail>[0-9]+) ", " – " } // "<title> – <episode> ..." - NOT A HYPHEN!
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

            // Config item for title detail parsing
            configData.AddDynamic("title-detail-parsing", new BoolConfigurationItem("Enable Title Detail Parsing"));
            configData.AddDynamic(
                "title-detail-parsing-help", 
                new DisplayInfoConfigurationItem("", "Title Detail Parsing will attempt to determine the season and episode number from the release names and reformat them as a suffix in the format S1E1. If successful, this should provide better matching in applications such as Sonarr.")
            );

            // Configure the category mappings
            AddCategoryMapping(1, TorznabCatType.TVAnime, "Anime - Sub");
        }

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
            // Retrieve RSS feed
            var result = await RequestWithCookiesAndRetryAsync(RssFeedUri);

            // Parse as XML document
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(result.ContentString);

            // Parse to RssFeedItems
            var rawItems = xmlDocument.GetElementsByTagName("item");
            IEnumerable<RssFeedItem> feedItems = ParseRssItems(rawItems);

            // If enabled, reformat title to make details easier to detect
            if (IsTitleDetailParsingEnabled)
            {
                feedItems = ReformatTitles(feedItems);
            }

            // Perform basic filter within Jackett
            IEnumerable<RssFeedItem> filteredItems = FilterForQuery(query, feedItems);

            // Convert to release info
            IEnumerable<ReleaseInfo> releases = ConvertRssFeedItemsToReleaseInfo(filteredItems);

            return releases;
        }

        private IEnumerable<RssFeedItem> ParseRssItems(XmlNodeList xmlNodes)
        {
            foreach (var ri in xmlNodes)
            {
                var node = (XmlNode)ri;

                if (RssFeedItem.TryParse(node, out RssFeedItem item))
                {
                    yield return item;
                }
                else
                {
                    logger.Warn($"Could not parse {DisplayName} RSS item '{node.InnerText}'");
                }
            }
        }

        private IEnumerable<RssFeedItem> FilterForQuery(TorznabQuery query, IEnumerable<RssFeedItem> feedItems)
        {
            foreach (var fi in feedItems)
            {
                if (!query.MatchQueryStringAND(fi.Title))
                    continue;

                yield return fi;
            }
        }

        private IEnumerable<RssFeedItem> ReformatTitles(IEnumerable<RssFeedItem> feedItems)
        {
            foreach(var fi in feedItems)
            {
                var title = fi.Title;

                if (IsTitleDetailParsingEnabled)
                {
                    var results = SearchTitleForDetails(title, new Dictionary<string, Dictionary<string, string>> {
                        { "episode", DETAIL_SEARCH_EPISODE },
                        { "season", DETAIL_SEARCH_SEASON }
                    });
                    
                    fi.Title = string.Concat(results.strippedTitle, " ", PrefixOrDefault("S", results.details["season"]), PrefixOrDefault("E", results.details["episode"])).Trim();
                    yield return fi;
                }
            }
        }

        private IEnumerable<ReleaseInfo> ConvertRssFeedItemsToReleaseInfo(IEnumerable<RssFeedItem> feedItems)
        {
            foreach (var fi in feedItems)
            {
                if (fi.PublishDate == null)
                {
                    logger.Warn($"Failed to parse {DisplayName} RSS feed item '{fi.Title}' due to malformed publish date.");
                    continue;
                }

                Uri magnetUri;
                if (!Uri.TryCreate(fi.Link, UriKind.Absolute, out magnetUri))
                {
                    logger.Warn($"Failed to parse {DisplayName} RSS feed item '{fi.Title}' due to malformed link URI.");
                    continue;
                }

                // Do some basic parsing of the title to make it easier to detect the episode
                var title = fi.Title;

                if (IsTitleDetailParsingEnabled)
                {
                    var results = SearchTitleForDetails(title, new Dictionary<string, Dictionary<string, string>> {
                        { "episode", DETAIL_SEARCH_EPISODE },
                        { "season", DETAIL_SEARCH_SEASON }
                    });
                    
                    title = string.Concat(results.strippedTitle, " ", PrefixOrDefault("S", results.details["season"]), PrefixOrDefault("E", results.details["episode"])).Trim();
                }

                var release = new ReleaseInfo
                {
                    Title = title,
                    Guid = magnetUri,
                    MagnetUri = magnetUri,
                    PublishDate = fi.PublishDate.Value.ToLocalTime().DateTime,
                    Category = MapTrackerCatToNewznab("1")
                };
                yield return release;
            }
        }

        private static (string strippedTitle, Dictionary<string, string> details) SearchTitleForDetails(string title, Dictionary<string, Dictionary<string,string>> definition)
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
                var match = Regex.Match(title, srp.Key, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(0.5));
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
                PublishDateRaw = publishDate;
            }

            public string Title { get; set; }

            public string Link { get; }

            public DateTimeOffset? PublishDate
            {
                get
                {
                    DateTimeOffset publishDate;
                    if (DateTimeOffset.TryParse(PublishDateRaw, out publishDate))
                    {
                        return publishDate;
                    }

                    return null;
                }
            }

            public string PublishDateRaw { get; }
        }
    }
}