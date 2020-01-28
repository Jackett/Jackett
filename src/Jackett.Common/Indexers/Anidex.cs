using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;

namespace Jackett.Common.Indexers
{
    public class Anidex : BaseWebIndexer
    {
        private const string DEFAULT_SITE_LINK = "https://anidex.info/";

        public Anidex(IIndexerConfigurationService configService, Utils.Clients.WebClient wc, Logger l, IProtectionService ps)
            : base(name: "Anidex",
                description: "Anidex is a Public torrent tracker and indexer, primarily for English fansub groups of anime",
                link: "https://anidex.info/",
                caps: new TorznabCapabilities(),
                configService: configService,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataUrl(DEFAULT_SITE_LINK))
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
            SelectItem languageSelect = new SelectItem(new Dictionary<string, string>()
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
            }) { Name = "Language", Value = "1" };
            configData.AddDynamic("languageid", languageSelect);

            // Configure the sort selects
            SelectItem sortBySelect = new SelectItem(new Dictionary<string, string>()
            {
                {"upload_timestamp", "created"},
                {"seeders", "seeders"},
                {"size", "size"},
                {"filename", "title"}
            }) { Name = "Sort by", Value = "upload_timestamp" };
            configData.AddDynamic("sortrequestedfromsite", sortBySelect);

            SelectItem orderSelect = new SelectItem(new Dictionary<string, string>()
            {
                {"desc", "Descending"},
                {"asc", "Ascending"}
            })
            { Name = "Order", Value = "desc" };
            configData.AddDynamic("orderrequestedfromsite", orderSelect);
        }

        public string SortBy
        {
            get
            {
                return ((SelectItem)this.configData.GetDynamic("sortrequestedfromsite")).Value;
            }
        }

        public string Order
        {
            get
            {
                return ((SelectItem)this.configData.GetDynamic("orderrequestedfromsite")).Value;
            }
        }

        private Uri SiteUri
        {
            get
            {
                return new Uri(this.SiteLink);
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
            try
            {
                await this.ConfigureDDoSGuardCookie();

                // Get specified categories. If none were specified, use all available.
                List<string> searchCategories = this.MapTorznabCapsToTrackers(query);
                if (searchCategories.Count == 0)
                {
                    searchCategories = this.GetAllTrackerCategories();
                }

                // Prepare the search query
                NameValueCollection queryParameters = new NameValueCollection();
                queryParameters.Add("page", "search");
                queryParameters.Add("id", string.Join(",", searchCategories));
                queryParameters.Add("group", "0"); // No group
                queryParameters.Add("q", query.SearchTerm ?? string.Empty);
                queryParameters.Add("s", this.SortBy);
                queryParameters.Add("o", this.Order);

                // Make search request
                Uri searchUri = new Uri(this.SiteUri, "?" + queryParameters.GetQueryString());
                WebClientStringResult response = await RequestStringWithCookiesAndRetry(searchUri.AbsoluteUri);

                // Check for DDOS Guard or other error
                if (response.Status == System.Net.HttpStatusCode.Forbidden)
                {
                    throw new IOException("Anidex search was forbidden. This was likely caused by DDOS protection.");
                }
                else if (response.Status != System.Net.HttpStatusCode.OK)
                {
                    throw new IOException($"Anidex search returned unexpected result. Expected 200 OK but got {response.Status.ToString()}.");
                }

                // Search seems to have been a success so parse it
                return this.ParseResult(response.Content);
            }
            catch (Exception ex)
            {
                this.LogIndexerError(ex.Message);
                throw ex;
            }
        }

        private IEnumerable<ReleaseInfo> ParseResult(string response)
        {
            const string ROW_SELECTOR = "div#content table > tbody > tr";

            try
            {
                HtmlParser resultParser = new HtmlParser();
                IHtmlDocument resultDocument = resultParser.ParseDocument(response);
                IEnumerable<IElement> rows = resultDocument.QuerySelectorAll(ROW_SELECTOR);

                List<ReleaseInfo> releases = new List<ReleaseInfo>();
                foreach (IElement r in rows)
                {
                    try
                    {
                        ReleaseInfo release = new ReleaseInfo();

                        release.Category = this.MapTrackerCatToNewznab(r.QuerySelector("td:nth-child(1) a").Attributes["href"].Value.Substring(5));
                        release.Title = r.QuerySelector("td:nth-child(3) span").Text();
                        release.MagnetUri = new Uri(r.QuerySelector("a[href^=\"magnet:?\"]").Attributes["href"].Value);
                        release.Size = ReleaseInfo.GetBytes(r.QuerySelector("td:nth-child(7)").Text());
                        release.PublishDate = DateTime.ParseExact(r.QuerySelector("td:nth-child(8)").Attributes["title"].Value, "yyyy-MM-dd HH:mm:ss UTC", CultureInfo.InvariantCulture);
                        release.Seeders = int.Parse(r.QuerySelector("td:nth-child(9)").Text());
                        release.Peers = int.Parse(r.QuerySelector("td:nth-child(10)").Text()) + release.Seeders;
                        release.Grabs = int.Parse(r.QuerySelector("td:nth-child(11)").Text());
                        release.Comments = new Uri(this.SiteUri, r.QuerySelector("td:nth-child(3) a").Attributes["href"].Value);
                        release.MinimumRatio = 1;
                        release.MinimumSeedTime = 172800; // 48 hours

                        releases.Add(release);
                    }
                    catch (Exception ex)
                    {
                        this.LogIndexerError($"Error parsing search result row '{r.ToHtmlPretty()}':\n\n{ex}");
                    }
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
            const string PATH_AND_QUERY_BASE64_ENCODED = "Lw=="; // "/"
            const string BASE_URI_BASE64_ENCODED = "aHR0cHM6Ly9hbmlkZXguaW5mbw=="; // "http://anidex.info"
            const string DDOS_POST_URL = "https://ddgu.ddos-guard.net/ddgu/";

            // TODO: Check if the cookie already exists and is valid, if so exit without doing anything
            //if (this.CookieHeader)

            // Make a request to DDoS Guard to get the redirect URL
            List<KeyValuePair<string,string>> ddosPostData = new List<KeyValuePair<string, string>>();
            ddosPostData.Add("u", PATH_AND_QUERY_BASE64_ENCODED);
            ddosPostData.Add("h", BASE_URI_BASE64_ENCODED);
            ddosPostData.Add("p", string.Empty);

            WebClientStringResult result = await this.PostDataWithCookiesAndRetry(DDOS_POST_URL, ddosPostData);

            if (!result.IsRedirect)
            {
                // Success returns a redirect. For anything else, assume a failure.
                throw new IOException($"Unexpected result from DDOS Guard while attempting to bypass: {result.Content}");
            }

            // Call the redirect URL to retrieve the cookie
            result = await this.RequestStringWithCookiesAndRetry(result.RedirectingTo);
            if (!result.IsRedirect)
            {
                // Success is another redirect. For anything else, assume a failure.
                throw new IOException($"Unexpected result when returning from DDOS Guard bypass: {result.Content}");
            }

            // If we got to this point, the bypass should have succeeded and we have stored the necessary cookies to access the site normally.
        }

        private void LogIndexerError(string message)
        {
            logger.Error($"{nameof(Anidex)} indexer ({this.ID}): {message}");
        }
    }
}
