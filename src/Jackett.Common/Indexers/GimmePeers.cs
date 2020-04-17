using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    // ReSharper disable once InconsistentNaming
    public class GimmePeers : BaseWebIndexer
    {
        private string BrowseUrl => SiteLink + "browse.php";
        private string LoginUrl => SiteLink + "takelogin.php";

        private new ConfigurationDataBasicLogin configData
        {
            get => (ConfigurationDataBasicLogin)base.configData;
            set => base.configData = value;
        }

        public GimmePeers(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(name: "GimmePeers",
                description: "Formerly ILT",
                link: "https://www.gimmepeers.com/",
                caps: new TorznabCapabilities(),
                configService: configService,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLogin())
        {
            Encoding = Encoding.GetEncoding("iso-8859-1");
            Language = "en-us";
            Type = "private";

            AddCategoryMapping(1, TorznabCatType.TVAnime);
            AddCategoryMapping(5, TorznabCatType.BooksEbook);
            AddCategoryMapping(10, TorznabCatType.PCGames);
            AddCategoryMapping(11, TorznabCatType.ConsolePS3);
            AddCategoryMapping(11, TorznabCatType.ConsolePS4);
            AddCategoryMapping(11, TorznabCatType.ConsolePSP);
            AddCategoryMapping(12, TorznabCatType.ConsoleXBOX360DLC);
            AddCategoryMapping(12, TorznabCatType.ConsoleXbox);
            AddCategoryMapping(12, TorznabCatType.ConsoleXbox360);
            AddCategoryMapping(12, TorznabCatType.ConsoleXboxOne);
            AddCategoryMapping(6, TorznabCatType.Audio);

            AddCategoryMapping(21, TorznabCatType.TV);
            AddCategoryMapping(20, TorznabCatType.TVSD);
            AddCategoryMapping(21, TorznabCatType.TVHD);
            AddCategoryMapping(22, TorznabCatType.TV);
            AddCategoryMapping(24, TorznabCatType.TVSD);
            AddCategoryMapping(25, TorznabCatType.TVHD);

            AddCategoryMapping(50, TorznabCatType.XXX);
            AddCategoryMapping(49, TorznabCatType.XXXDVD);
            AddCategoryMapping(50, TorznabCatType.XXXx264);

            AddCategoryMapping(13, TorznabCatType.Movies3D);
            AddCategoryMapping(14, TorznabCatType.MoviesBluRay);
            AddCategoryMapping(15, TorznabCatType.MoviesDVD);
            AddCategoryMapping(16, TorznabCatType.MoviesHD);
            AddCategoryMapping(17, TorznabCatType.Movies);
            AddCategoryMapping(19, TorznabCatType.Movies);
            AddCategoryMapping(26, TorznabCatType.MoviesUHD);
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "returnto", "/" },
                { "login", "Log in!" }
            };

            var loginPage = await RequestStringWithCookies(SiteLink, string.Empty);

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, loginPage.Cookies, true, SiteLink, SiteLink);
            await ConfigureIfOK(result.Cookies, result.Content?.Contains("logout.php") == true, () =>
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(result.Content);
                var messageEl = dom.QuerySelector("body > div");
                var errorMessage = messageEl.TextContent.Trim();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();
            var searchUrl = BrowseUrl;
            var queryCollection = new NameValueCollection();

            // Tracker can only search OR return things in categories
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                queryCollection.Add("search", searchString);
                queryCollection.Add("cat", "0");
            }
            else
            {
                foreach (var cat in MapTorznabCapsToTrackers(query))
                    queryCollection.Add("c" + cat, "1");
                queryCollection.Add("incldead", "0");
            }

            searchUrl += "?" + queryCollection.GetQueryString();
            var response = await RequestStringWithCookiesAndRetry(searchUrl, null, BrowseUrl);
            if (response.IsRedirect)
            {
                // re login
                await ApplyConfiguration(null);
                response = await RequestStringWithCookiesAndRetry(searchUrl, null, BrowseUrl);
            }

            var results = response.Content;
            try
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(results);

                var rows = dom.QuerySelectorAll(".browsetable").LastOrDefault()?.QuerySelectorAll("tr");
                if (rows == null)
                    return releases;
                foreach (var row in rows.Skip(1))
                {
                    var release = new ReleaseInfo();

                    var link = row.QuerySelector("td:nth-of-type(2) a:nth-of-type(1)");
                    release.Guid = new Uri(SiteLink + link.GetAttribute("href"));
                    release.Comments = release.Guid;
                    release.Title = link.TextContent.Trim();
                    release.Description = release.Title;

                    // If we search an get no results, we still get a table just with no info.
                    if (string.IsNullOrWhiteSpace(release.Title))
                        break;

                    // Check if the release has been assigned a category
                    var category = row.QuerySelector("td:nth-of-type(1) a");
                    if (category != null)
                    {
                        var cat = category.GetAttribute("href").Substring(15);
                        release.Category = MapTrackerCatToNewznab(cat);
                    }

                    var qLink = row.QuerySelector("td:nth-of-type(3) a");
                    release.Link = new Uri(SiteLink + qLink.GetAttribute("href"));

                    var added = row.QuerySelector("td:nth-of-type(7)").TextContent.Trim(); //column changed from 7 to 6
                    var date = added.Substring(0, 10);
                    var time = added.Substring(11, 8); //date layout wasn't quite right
                    var dateTime = date + time;
                    release.PublishDate = DateTime.ParseExact(dateTime, "yyyy-MM-ddHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToLocalTime();

                    var sizeStr = row.QuerySelector("td:nth-of-type(6)").TextContent.Trim(); //size column moved from 8 to 5
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(11)").TextContent.Trim());
                    release.Peers = ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(12)").TextContent.Trim()) + release.Seeders;

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results, ex);
            }

            return releases;
        }
    }
}
