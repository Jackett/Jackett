using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
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

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class Anidex : BaseWebIndexer
    {
        public Anidex(IIndexerConfigurationService configService, Utils.Clients.WebClient wc, Logger l, IProtectionService ps)
            : base(id: "anidex",
                   name: "Anidex",
                   description: "Anidex is a Public torrent tracker and indexer, primarily for English fansub groups of anime",
                   link: "https://anidex.info/",
                   caps: new TorznabCapabilities
                   {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                       }
                   },
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   configData: new ConfigurationData())
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "public";

            // Configure the category mappings
            AddCategoryMapping(1, TorznabCatType.TVAnime, "Anime - Sub");
            AddCategoryMapping(2, TorznabCatType.TVAnime, "Anime - Raw");
            AddCategoryMapping(3, TorznabCatType.TVAnime, "Anime - Dub");
            AddCategoryMapping(4, TorznabCatType.TVAnime, "LA - Sub");
            AddCategoryMapping(5, TorznabCatType.TVAnime, "LA - Raw");
            AddCategoryMapping(6, TorznabCatType.TVAnime, "Light Novel");
            AddCategoryMapping(7, TorznabCatType.TVAnime, "Manga - TLed");
            AddCategoryMapping(8, TorznabCatType.TVAnime, "Manga - Raw");
            AddCategoryMapping(9, TorznabCatType.TVAnime, "♫ - Lossy");
            AddCategoryMapping(10, TorznabCatType.TVAnime, "♫ - Lossless");
            AddCategoryMapping(11, TorznabCatType.TVAnime, "♫ - Video");
            AddCategoryMapping(12, TorznabCatType.TVAnime, "Games");
            AddCategoryMapping(13, TorznabCatType.TVAnime, "Applications");
            AddCategoryMapping(14, TorznabCatType.TVAnime, "Pictures");
            AddCategoryMapping(15, TorznabCatType.TVAnime, "Adult Video");
            AddCategoryMapping(16, TorznabCatType.TVAnime, "Other");

            // Configure the language select option
            var languageSelect = new SelectItem(new Dictionary<string, string>
                {
                {"1", "English"},
                {"2", "Japanese"},
                {"3", "Polish"},
                {"4", "Serbo-Croatian" },
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
                {"15", "Spanish (Spain)" },
                {"16", "Portuguese (Brazil)" },
                {"17", "Portuguese (Portugal)" },
                {"18", "Swedish"},
                {"19", "Arabic"},
                {"20", "Danish"},
                {"21", "Chinese (Simplified)" },
                {"22", "Bengali"},
                {"23", "Romanian"},
                {"24", "Czech"},
                {"25", "Mongolian"},
                {"26", "Turkish"},
                {"27", "Indonesian"},
                {"28", "Korean"},
                {"29", "Spanish (LATAM)" },
                {"30", "Persian"},
                {"31", "Malaysian"}
            })
            { Name = "Language", Value = "1" };
            configData.AddDynamic("languageid", languageSelect);

            // Configure the sort selects
            var sortBySelect = new SelectItem(new Dictionary<string, string>
                {
                {"upload_timestamp", "created"},
                {"seeders", "seeders"},
                {"size", "size"},
                {"filename", "title"}
            })
            { Name = "Sort by", Value = "upload_timestamp" };
            configData.AddDynamic("sortrequestedfromsite", sortBySelect);

            var orderSelect = new SelectItem(new Dictionary<string, string>
                {
                    {"desc", "Descending"},
                    {"asc", "Ascending"}
                })
            { Name = "Order", Value = "desc" };
            configData.AddDynamic("orderrequestedfromsite", orderSelect);
        }

        private string GetSortBy => ((SelectItem)configData.GetDynamic("sortrequestedfromsite")).Value;

        private string GetOrder => ((SelectItem)configData.GetDynamic("orderrequestedfromsite")).Value;

        private Uri GetAbsoluteUrl(string relativeUrl) => new Uri(SiteLink + relativeUrl.TrimStart('/'));

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
            // Prepare the search query
            var queryParameters = new NameValueCollection
            {
                { "q", query.SearchTerm ?? string.Empty },
                { "s", GetSortBy },
                { "o", GetOrder },
                { "group", "0" } // No group
            };

            // Get specified categories
            // AniDex throws errors when categories are url encoded. See issue #9727
            var searchCategories = MapTorznabCapsToTrackers(query);
            var catString = "";
            if (searchCategories.Count > 0)
                catString = "&id=" + string.Join(",", searchCategories);

            // Make search request
            var searchUri = GetAbsoluteUrl("?" + queryParameters.GetQueryString() + catString);
            var response = await RequestWithCookiesAndRetryAsync(searchUri.AbsoluteUri);

            // Check for DDOS Guard
            if (response.Status == HttpStatusCode.Forbidden)
            {
                await ConfigureDDoSGuardCookie();
                response = await RequestWithCookiesAndRetryAsync(searchUri.AbsoluteUri);
            }

            if (response.Status != HttpStatusCode.OK)
                throw new WebException($"Anidex search returned unexpected result. Expected 200 OK but got {response.Status}.", WebExceptionStatus.ProtocolError);

            // Search seems to have been a success so parse it
            return ParseResult(response.ContentString);
        }

        private IEnumerable<ReleaseInfo> ParseResult(string response)
        {
            const string rowSelector = "div#content table > tbody > tr";

            try
            {
                var resultParser = new HtmlParser();
                var resultDocument = resultParser.ParseDocument(response);
                IEnumerable<IElement> rows = resultDocument.QuerySelectorAll(rowSelector);

                var releases = new List<ReleaseInfo>();
                foreach (var r in rows)
                    try
                    {
                        var release = new ReleaseInfo();

                        release.Category = ParseValueFromRow(r, nameof(release.Category), "td:nth-child(1) a", (e) => MapTrackerCatToNewznab(e.Attributes["href"].Value.Substring(5)));
                        release.Title = ParseStringValueFromRow(r, nameof(release.Title), "td:nth-child(3) span");
                        release.Link = ParseValueFromRow(r, nameof(release.Link), "a[href^=\"/dl/\"]", (e) => GetAbsoluteUrl(e.Attributes["href"].Value));
                        release.MagnetUri = ParseValueFromRow(r, nameof(release.MagnetUri), "a[href^=\"magnet:?\"]", (e) => new Uri(e.Attributes["href"].Value));
                        release.Size = ParseValueFromRow(r, nameof(release.Size), "td:nth-child(7)", (e) => ReleaseInfo.GetBytes(e.Text()));
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

        private async Task ConfigureDDoSGuardCookie()
        {
            const string ddosPostUrl = "https://check.ddos-guard.net/check.js";
            var response = await RequestWithCookiesAsync(ddosPostUrl, string.Empty);
            if (response.Status != HttpStatusCode.OK)
                throw new WebException($"Unexpected DDOS Guard response: Status: {response.Status}", WebExceptionStatus.ProtocolError);
            if (response.IsRedirect)
                throw new WebException($"Unexpected DDOS Guard response: Redirect: {response.RedirectingTo}", WebExceptionStatus.UnknownError);
            if (string.IsNullOrWhiteSpace(response.Cookies))
                throw new WebException("Unexpected DDOS Guard response: Empty cookie", WebExceptionStatus.ReceiveFailure);
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
