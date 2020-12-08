using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class SceneTime : BaseWebIndexer
    {
        private string SearchUrl => SiteLink + "browse.php";
        private string DownloadUrl => SiteLink + "download.php/{0}/download.torrent";

        private new ConfigurationDataSceneTime configData => (ConfigurationDataSceneTime)base.configData;

        public SceneTime(IIndexerConfigurationService configService, WebClient w, Logger l, IProtectionService ps)
            : base(id: "scenetime",
                   name: "SceneTime",
                   description: "Always on time",
                   link: "https://www.scenetime.com/",
                   caps: new TorznabCapabilities
                   {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                       },
                       MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q
                       },
                       MusicSearchParams = new List<MusicSearchParam>
                       {
                           MusicSearchParam.Q
                       },
                       BookSearchParams = new List<BookSearchParam>
                       {
                           BookSearchParam.Q
                       }
                   },
                   configService: configService,
                   client: w,
                   logger: l,
                   p: ps,
                   configData: new ConfigurationDataSceneTime())
        {
            Encoding = Encoding.GetEncoding("iso-8859-1");
            Language = "en-us";
            Type = "private";

            AddCategoryMapping(10, TorznabCatType.XXX, "Movies Adult");
            AddCategoryMapping(47, TorznabCatType.Movies, "Movie Packs");
            AddCategoryMapping(57, TorznabCatType.MoviesSD, "Movies SD");
            AddCategoryMapping(59, TorznabCatType.MoviesHD, "Movies HD");
            AddCategoryMapping(64, TorznabCatType.Movies3D, "Movies 3D");
            AddCategoryMapping(82, TorznabCatType.MoviesOther, "Movies CAM-TS");
            AddCategoryMapping(16, TorznabCatType.MoviesUHD, "Movies UHD");
            AddCategoryMapping(2, TorznabCatType.TVUHD, "TV UHD");
            AddCategoryMapping(43, TorznabCatType.TV, "TV Packs");
            AddCategoryMapping(9, TorznabCatType.TVHD, "TV HD");
            AddCategoryMapping(77, TorznabCatType.TVSD, "TV SD");
            AddCategoryMapping(6, TorznabCatType.PCGames, "Games PC ISO");
            AddCategoryMapping(48, TorznabCatType.ConsoleXBox, "Games XBOX");
            AddCategoryMapping(51, TorznabCatType.ConsoleWii, "Games Wii");
            AddCategoryMapping(55, TorznabCatType.ConsoleNDS, "Games Nintendo DS");
            AddCategoryMapping(12, TorznabCatType.ConsolePS4, "Games/PS");
            AddCategoryMapping(15, TorznabCatType.ConsoleOther, "Games Dreamcast");
            AddCategoryMapping(52, TorznabCatType.PCMac, "Mac/Linux");
            AddCategoryMapping(53, TorznabCatType.PC0day, "Apps");
            AddCategoryMapping(24, TorznabCatType.PCMobileOther, "Mobile Apps");
            AddCategoryMapping(7, TorznabCatType.Books, "Books and Magazines");
            AddCategoryMapping(65, TorznabCatType.BooksComics, "Books Comic");
            AddCategoryMapping(4, TorznabCatType.Audio, "Music");
            AddCategoryMapping(116, TorznabCatType.Audio, "Music Pack");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            CookieHeader = configData.Cookie.Value;
            try
            {
                var results = await PerformQuery(new TorznabQuery());
                if (!results.Any())
                    throw new Exception("Found 0 results in the tracker");

                IsConfigured = true;
                SaveConfig();
                return IndexerConfigurationStatus.Completed;
            }
            catch (Exception e)
            {
                IsConfigured = false;
                throw new Exception("Your cookie did not work: " + e.Message);
            }
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var qParams = new NameValueCollection
            {
                {"cata", "yes"},
                {"sec", "jax"}
            };

            var catList = MapTorznabCapsToTrackers(query);
            foreach (var cat in catList)
                qParams.Add("c" + cat, "1");

            if (!string.IsNullOrEmpty(query.SanitizedSearchTerm))
                qParams.Add("search", query.GetQueryString());

            // If Only Freeleech Enabled
            if (configData.Freeleech.Value)
                qParams.Add("freeleech", "on");

            var searchUrl = SearchUrl + "?" + qParams.GetQueryString();
            var results = await RequestWithCookiesAsync(searchUrl);

            // response without results (the message is misleading)
            if (results.ContentString?.Contains("slow down geek!!!") == true)
                return new List<ReleaseInfo>();

            // not logged in
            if (results.ContentString == null || !results.ContentString.Contains("/logout.php"))
                throw new Exception("The user is not logged in. It is possible that the cookie has expired or you " +
                                    "made a mistake when copying it. Please check the settings.");

            return ParseResponse(query, results.ContentString);
        }

        private List<ReleaseInfo> ParseResponse(TorznabQuery query, string htmlResponse)
        {
            var releases = new List<ReleaseInfo>();

            try
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(htmlResponse);

                var table = dom.QuerySelector("table.movehere");
                if (table == null)
                    return releases; // no results

                var headerColumns = table.QuerySelectorAll("tbody > tr > td.cat_Head")
                                         .Select(x => x.TextContent).ToList();
                var categoryIndex = headerColumns.FindIndex(x => x.Equals("Type"));
                var nameIndex = headerColumns.FindIndex(x => x.Equals("Name"));
                var sizeIndex = headerColumns.FindIndex(x => x.Equals("Size"));
                var seedersIndex = headerColumns.FindIndex(x => x.Equals("Seeders"));
                var leechersIndex = headerColumns.FindIndex(x => x.Equals("Leechers"));

                var rows = dom.QuerySelectorAll("tr.browse");
                foreach (var row in rows)
                {
                    // TODO convert to initializer
                    var release = new ReleaseInfo();
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 259200; // 72 hours

                    var qCatLink = row.Children[categoryIndex].QuerySelector("a");
                    if (qCatLink != null)
                    {
                        var catId = new Regex(@"\?cat=(\d*)").Match(qCatLink.GetAttribute("href")).Groups[1].ToString().Trim();
                        release.Category = MapTrackerCatToNewznab(catId);
                    }

                    var qDescCol = row.Children[nameIndex];
                    var qLink = qDescCol.QuerySelector("a");
                    release.Title = qLink.TextContent;
                    if (!query.MatchQueryStringAND(release.Title))
                        continue;

                    release.Details = new Uri(SiteLink + "/" + qLink.GetAttribute("href"));
                    release.Guid = release.Details;

                    var torrentId = qLink.GetAttribute("href").Split('=')[1];
                    release.Link = new Uri(string.Format(DownloadUrl, torrentId));

                    release.PublishDate = DateTimeUtil.FromTimeAgo(qDescCol.ChildNodes.Last().TextContent);

                    var sizeStr = row.Children[sizeIndex].TextContent;
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(row.Children[seedersIndex].TextContent.Trim());
                    release.Peers = ParseUtil.CoerceInt(row.Children[leechersIndex].TextContent.Trim()) + release.Seeders;

                    release.DownloadVolumeFactor = row.QuerySelector("font > b:contains(Freeleech)") != null ? 0 : 1;
                    release.UploadVolumeFactor = 1;

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(htmlResponse, ex);
            }

            return releases;
        }
    }
}
