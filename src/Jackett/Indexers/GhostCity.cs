using CsQuery;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jackett.Models.IndexerConfig;

namespace Jackett.Indexers
{
    public class GhostCity : BaseWebIndexer
    {
        string LoginUrl { get { return SiteLink + "takelogin.php"; } }
        string BrowsePage { get { return SiteLink + "browse.php"; } }

        new ConfigurationDataBasicLoginWithRSSAndDisplay configData
        {
            get { return (ConfigurationDataBasicLoginWithRSSAndDisplay)base.configData; }
            set { base.configData = value; }
        }

        public GhostCity(IIndexerConfigurationService configService, IWebClient wc, Logger l, IProtectionService ps)
                : base(name: "Ghost City",
                description: "A German general tracker",
                link: "http://ghostcity.dyndns.info/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                configService: configService,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLoginWithRSSAndDisplay())
        {
            Encoding = Encoding.GetEncoding("UTF-8");
            Language = "de-de";
            Type = "private";

            this.configData.DisplayText.Value = "Only the results from the first search result page are shown, adjust your profile settings to show the maximum.";
            this.configData.DisplayText.Name = "Notice";
            AddMultiCategoryMapping(TorznabCatType.TVAnime, 8, 34, 35, 36);
            AddMultiCategoryMapping(TorznabCatType.TVDocumentary, 12, 44, 106, 45, 46, 47);
            AddMultiCategoryMapping(TorznabCatType.Console, 92, 93, 95, 96, 97);
            AddMultiCategoryMapping(TorznabCatType.ConsoleNDS, 92);
            AddMultiCategoryMapping(TorznabCatType.ConsolePS3, 95);
            AddMultiCategoryMapping(TorznabCatType.ConsolePS4, 95);
            AddMultiCategoryMapping(TorznabCatType.ConsolePS4, 95);
            AddMultiCategoryMapping(TorznabCatType.ConsolePSP, 95);
            AddMultiCategoryMapping(TorznabCatType.ConsoleXbox, 97);
            AddMultiCategoryMapping(TorznabCatType.ConsoleXbox360, 97);
            AddMultiCategoryMapping(TorznabCatType.ConsoleXBOX360DLC, 97);
            AddMultiCategoryMapping(TorznabCatType.ConsoleXboxOne, 97);
            AddMultiCategoryMapping(TorznabCatType.ConsoleWii, 96);
            AddMultiCategoryMapping(TorznabCatType.PC, 20, 94, 40);
            AddMultiCategoryMapping(TorznabCatType.PCGames, 94);
            AddMultiCategoryMapping(TorznabCatType.PCMac, 39);
            AddMultiCategoryMapping(TorznabCatType.PCPhoneOther, 37, 38);
            AddMultiCategoryMapping(TorznabCatType.TVSport, 22, 98, 99, 100, 101);
            AddMultiCategoryMapping(TorznabCatType.Movies, 68, 69, 70, 102, 104, 103, 72, 71, 73, 74, 75, 77, 78, 79);
            AddMultiCategoryMapping(TorznabCatType.MoviesSD, 68, 69, 102, 104, 103, 72, 71, 73, 74);
            AddMultiCategoryMapping(TorznabCatType.MoviesHD, 75, 76, 77, 78, 79);
            AddMultiCategoryMapping(TorznabCatType.MoviesOther, 73);
            AddMultiCategoryMapping(TorznabCatType.MoviesBluRay, 70);
            AddMultiCategoryMapping(TorznabCatType.MoviesDVD, 102, 104, 103, 72, 71);
            AddMultiCategoryMapping(TorznabCatType.Movies3D, 69);
            AddMultiCategoryMapping(TorznabCatType.AudioVideo, 109);
            AddMultiCategoryMapping(TorznabCatType.TV, 8, 34, 35, 36, 23, 90, 88, 107, 89);
            AddMultiCategoryMapping(TorznabCatType.TVHD, 107);
            AddMultiCategoryMapping(TorznabCatType.TVSD, 89);
            AddMultiCategoryMapping(TorznabCatType.XXX, 25);
            AddMultiCategoryMapping(TorznabCatType.TVDocumentary, 88);
            AddMultiCategoryMapping(TorznabCatType.AudioAudiobook, 84);
            AddMultiCategoryMapping(TorznabCatType.BooksEbook, 83);
            AddMultiCategoryMapping(TorznabCatType.BooksMagazines, 85);
            AddMultiCategoryMapping(TorznabCatType.BooksOther, 108);
            AddMultiCategoryMapping(TorznabCatType.Other, 3, 93, 24);
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value }
            };

            var result1 = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, SiteLink);
            CQ result1Dom = result1.Content;
            var link = result1Dom[".trow2 a"].First();
            var result2 = await RequestStringWithCookies(link.Attr("href"), result1.Cookies);
            CQ result2Dom = result2.Content;

            await ConfigureIfOK(result1.Cookies, result2.Content.Contains("/logout.php"), () =>
            {
                var errorMessage = "Login failed.";
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();
            var searchUrl = BrowsePage;
            var queryCollection = new NameValueCollection();

            queryCollection.Add("do", "search");

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                queryCollection.Add("keywords", searchString);
            }

            queryCollection.Add("search_type", "t_name");

            // FIXME: Tracker doesn't support multi category search
            foreach (var cat in MapTorznabCapsToTrackers(query))
            {
                queryCollection.Add("category", cat);
            }

            if (queryCollection.Count > 0)
            {
                searchUrl += "?" + queryCollection.GetQueryString();
            }

            var results = await RequestStringWithCookiesAndRetry(searchUrl);

            if (results.Content.Contains("<meta http-equiv=\"refresh\"")) // relogin needed?
            {
                await ApplyConfiguration(null);
                results = await RequestStringWithCookiesAndRetry(searchUrl);
            }

            try
            {
                CQ dom = results.Content;

                var rows = dom["#sortabletable tr"];
                foreach (var row in rows.Skip(1))
                {
                    var release = new ReleaseInfo();
                    var qRow = row.Cq();
                    release.Title = qRow.Find(".tooltip-content div").First().Text();
                    if (string.IsNullOrWhiteSpace(release.Title))
                        continue;
                    release.Description = qRow.Find(".tooltip-content div").Get(1).InnerText.Trim();

                    var qLink = row.Cq().Find("td:eq(2) a:eq(0)");
                    release.Link = new Uri(qLink.Attr("href"));
                    release.Guid = release.Link;
                    release.Comments = new Uri(qRow.Find(".tooltip-target a").First().Attr("href"));

                    var dateString = qRow.Find("td:eq(1) div").Last().Children().Remove().End().Text().Trim();
                    release.PublishDate = DateTime.ParseExact(dateString, "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture);

                    var sizeStr = qRow.Find("td:eq(4)").Text().Trim();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(qRow.Find("td:eq(6)").Text().Trim());
                    release.Peers = ParseUtil.CoerceInt(qRow.Find("td:eq(7)").Text().Trim()) + release.Seeders;

                    var catLink = row.Cq().Find("td:eq(0) a").First().Attr("href");
                    var catSplit = catLink.IndexOf("category=");
                    if (catSplit > -1)
                    {
                        catLink = catLink.Substring(catSplit + 9);
                    }

                    release.Category = MapTrackerCatToNewznab(catLink);
                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.Content, ex);
            }

            return releases;
        }
    }
}
