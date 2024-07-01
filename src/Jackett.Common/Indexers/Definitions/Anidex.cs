using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
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
    public class Anidex : IndexerBase
    {
        public override string Id => "anidex";
        public override string Name => "Anidex";
        public override string Description => "Anidex is a Public torrent tracker and indexer, primarily for English fansub groups of anime";
        public override string SiteLink { get; protected set; } = "https://anidex.info/";
        public override string Language => "en-US";
        public override string Type => "public";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        public Anidex(IIndexerConfigurationService configService, Utils.Clients.WebClient wc, Logger l,
                      IProtectionService ps, ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationData())
        {
            configData.AddDynamic("DDoS-Guard", new DisplayInfoConfigurationItem("DDoS-Guard", "This site may use DDoS-Guard Protection, therefore Jackett requires <a href='https://github.com/Jackett/Jackett#configuring-flaresolverr' target='_blank'>FlareSolverr</a> to access it."));

            AddLanguageConfiguration();

            // Configure the sort selects
            var sortBySelect = new SingleSelectConfigurationItem("Sort by", new Dictionary<string, string>
                {
                    {"upload_timestamp", "created"},
                    {"seeders", "seeders"},
                    {"size", "size"},
                    {"filename", "title"}
                })
            { Value = "upload_timestamp" };
            configData.AddDynamic("sortrequestedfromsite", sortBySelect);

            var orderSelect = new SingleSelectConfigurationItem("Order", new Dictionary<string, string>
                {
                    {"desc", "Descending"},
                    {"asc", "Ascending"}
                })
            { Value = "desc" };
            configData.AddDynamic("orderrequestedfromsite", orderSelect);

            EnableConfigurableRetryAttempts();
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                },
                MusicSearchParams = new List<MusicSearchParam>
                {
                    MusicSearchParam.Q,
                },
                BookSearchParams = new List<BookSearchParam>
                {
                    BookSearchParam.Q,
                }
            };

            // Configure the category mappings
            caps.Categories.AddCategoryMapping(1, TorznabCatType.TVAnime, "Anime - Sub");
            caps.Categories.AddCategoryMapping(2, TorznabCatType.TVAnime, "Anime - Raw");
            caps.Categories.AddCategoryMapping(3, TorznabCatType.TVAnime, "Anime - Dub");
            caps.Categories.AddCategoryMapping(4, TorznabCatType.TVAnime, "LA - Sub");
            caps.Categories.AddCategoryMapping(5, TorznabCatType.TVAnime, "LA - Raw");
            caps.Categories.AddCategoryMapping(6, TorznabCatType.BooksEBook, "Light Novel");
            caps.Categories.AddCategoryMapping(7, TorznabCatType.BooksComics, "Manga - TLed");
            caps.Categories.AddCategoryMapping(8, TorznabCatType.BooksComics, "Manga - Raw");
            caps.Categories.AddCategoryMapping(9, TorznabCatType.AudioMP3, "♫ - Lossy");
            caps.Categories.AddCategoryMapping(10, TorznabCatType.AudioLossless, "♫ - Lossless");
            caps.Categories.AddCategoryMapping(11, TorznabCatType.AudioVideo, "♫ - Video");
            caps.Categories.AddCategoryMapping(12, TorznabCatType.PCGames, "Games");
            caps.Categories.AddCategoryMapping(13, TorznabCatType.PC0day, "Applications");
            caps.Categories.AddCategoryMapping(14, TorznabCatType.XXXImageSet, "Pictures");
            caps.Categories.AddCategoryMapping(15, TorznabCatType.XXX, "Adult Video");
            caps.Categories.AddCategoryMapping(16, TorznabCatType.Other, "Other");

            return caps;
        }

        private void AddLanguageConfiguration()
        {
            // Configure the language select option
            var languageSelect = new MultiSelectConfigurationItem("Language (None ticked = ALL)", new Dictionary<string, string>
                {
                    {"1", "English"},
                    {"2", "Japanese"},
                    {"3", "Polish"},
                    {"4", "Serbo-Croatian"},
                    {"5", "Dutch"},
                    {"6", "Italian"},
                    {"7", "Russian"},
                    {"8", "German"},
                    {"9", "Hungarian"},
                    {"10", "French"},
                    {"11", "Finnish"},
                    {"12", "Vietnamese"},
                    {"13", "Greek"},
                    {"14", "Bulgarian"},
                    {"15", "Spanish (Spain)"},
                    {"16", "Portuguese (Brazil)"},
                    {"17", "Portuguese (Portugal)"},
                    {"18", "Swedish"},
                    {"19", "Arabic"},
                    {"20", "Danish"},
                    {"21", "Chinese (Simplified)"},
                    {"22", "Bengali"},
                    {"23", "Romanian"},
                    {"24", "Czech"},
                    {"25", "Mongolian"},
                    {"26", "Turkish"},
                    {"27", "Indonesian"},
                    {"28", "Korean"},
                    {"29", "Spanish (LATAM)"},
                    {"30", "Persian"},
                    {"31", "Malaysian"}
                })
            { Values = new[] { "" } };
            configData.AddDynamic("languageid", languageSelect);
        }

        private string GetSortBy => ((SingleSelectConfigurationItem)configData.GetDynamic("sortrequestedfromsite")).Value;

        private string GetOrder => ((SingleSelectConfigurationItem)configData.GetDynamic("orderrequestedfromsite")).Value;

        private Uri GetAbsoluteUrl(string relativeUrl) => new Uri(SiteLink + relativeUrl.TrimStart('/'));

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var releases = await PerformQuery(new TorznabQuery());

            await ConfigureIfOK(string.Empty, releases.Any(), () => throw new Exception("Could not find releases from this URL"));

            return IndexerConfigurationStatus.Completed;
        }

        /// <summary>
        /// Returns the selected languages, formatted so that they can be used in a query string.
        /// </summary>
        private string GetLanguagesForQuery()
        {
            var languagesConfig = (MultiSelectConfigurationItem)configData.GetDynamic("languageid");
            return string.Join(",", languagesConfig.Values);
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            // Prepare the search query
            var queryParameters = new NameValueCollection
            {
                { "page", "search" },
                { "s", GetSortBy },
                { "o", GetOrder },
                { "group_id", "0" }, // No group
                { "q", query.GetQueryString() ?? string.Empty }
            };

            // Get specified categories
            // AniDex throws errors when categories are url encoded. See issue #9727
            var searchCategories = MapTorznabCapsToTrackers(query);
            var catString = "";
            if (searchCategories.Any() && GetAllTrackerCategories().Except(searchCategories).Any())
            {
                catString = "&id=" + string.Join(",", searchCategories);
            }

            // Get Selected Languages
            // AniDex throws errors when the commas between language IDs are url encoded.
            var langIds = "&lang_id=" + GetLanguagesForQuery();

            // Make search request
            var searchUri = GetAbsoluteUrl("?" + queryParameters.GetQueryString() + catString + langIds);
            var response = await RequestWithCookiesAndRetryAsync(searchUri.AbsoluteUri);

            if (response.Status != HttpStatusCode.OK)
                throw new WebException($"Anidex search returned unexpected result. Expected 200 OK but got {response.Status}.", WebExceptionStatus.ProtocolError);

            // Search seems to have been a success so parse it
            return ParseResult(response.ContentString);
        }

        private IEnumerable<ReleaseInfo> ParseResult(string response)
        {
            try
            {
                var resultParser = new HtmlParser();
                using var resultDocument = resultParser.ParseDocument(response);
                IEnumerable<IElement> rows = resultDocument.QuerySelectorAll("div#content table > tbody > tr");

                var releases = new List<ReleaseInfo>();
                foreach (var r in rows)
                    try
                    {
                        var language = "";
                        var release = new ReleaseInfo();

                        release.Category = ParseValueFromRow(r, nameof(release.Category), "td:nth-child(1) a", (e) => MapTrackerCatToNewznab(e.Attributes["href"].Value.Substring(5)));
                        language = ParseValueFromRow(r, nameof(language), "td:nth-child(1) img", (e) => e.Attributes["title"].Value);
                        release.Title = ParseStringValueFromRow(r, nameof(release.Title), "td:nth-child(3) span") + " " + language;
                        release.Link = ParseValueFromRow(r, nameof(release.Link), "a[href^=\"/dl/\"]", (e) => GetAbsoluteUrl(e.Attributes["href"].Value));
                        release.MagnetUri = ParseValueFromRow(r, nameof(release.MagnetUri), "a[href^=\"magnet:?\"]", (e) => new Uri(e.Attributes["href"].Value));
                        release.Size = ParseValueFromRow(r, nameof(release.Size), "td:nth-child(7)", (e) => ParseUtil.GetBytes(e.Text()));
                        release.PublishDate = ParseValueFromRow(r, nameof(release.PublishDate), "td:nth-child(8)", (e) => DateTime.ParseExact(e.Attributes["title"].Value, "yyyy-MM-dd HH:mm:ss UTC", CultureInfo.InvariantCulture));
                        release.Seeders = ParseIntValueFromRow(r, nameof(release.Seeders), "td:nth-child(9)");
                        release.Peers = ParseIntValueFromRow(r, nameof(release.Peers), "td:nth-child(10)") + release.Seeders;
                        release.Grabs = ParseIntValueFromRow(r, nameof(release.Grabs), "td:nth-child(11)");
                        release.Details = ParseValueFromRow(r, nameof(release.Details), "td:nth-child(3) a", (e) => GetAbsoluteUrl(e.Attributes["href"].Value));
                        release.Guid = release.Details;
                        release.DownloadVolumeFactor = 0;
                        release.UploadVolumeFactor = 1;

                        releases.Add(release);
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Anidex: Error parsing search result row '{r.ToHtmlPretty()}':\n\n{ex}");
                    }

                return releases;
            }
            catch (Exception ex)
            {
                throw new IOException($"Error parsing search result page: {ex}");
            }
        }

        private static TResult ParseValueFromRow<TResult>(IElement row, string propertyName, string selector,
                                                          Func<IElement, TResult> parseFunction)
        {
            try
            {
                var selectedElement = row.QuerySelector(selector);
                if (selectedElement == null)
                    throw new IOException($"Unable to find '{selector}'.");

                return parseFunction(selectedElement);
            }
            catch (Exception ex)
            {
                throw new IOException($"Error parsing for property '{propertyName}': {ex.Message}");
            }
        }

        private static string ParseStringValueFromRow(IElement row, string propertyName, string selector)
        {
            try
            {
                var selectedElement = row.QuerySelector(selector);
                if (selectedElement == null)
                    throw new IOException($"Unable to find '{selector}'.");

                return selectedElement.Text();
            }
            catch (Exception ex)
            {
                throw new IOException($"Error parsing for property '{propertyName}': {ex.Message}");
            }
        }

        private static int ParseIntValueFromRow(IElement row, string propertyName, string selector)
        {
            try
            {
                var text = ParseStringValueFromRow(row, propertyName, selector);
                if (!int.TryParse(text, out var value))
                    throw new IOException($"Could not convert '{text}' to int.");
                return value;
            }
            catch (Exception ex)
            {
                throw new IOException($"Error parsing for property '{propertyName}': {ex.Message}");
            }
        }
    }
}
