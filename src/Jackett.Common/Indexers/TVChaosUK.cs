using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using CsQuery;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    public class TVChaosUK : BaseWebIndexer
    {
        private string LoginUrl { get { return SiteLink + "takelogin.php"; } }
        private string GetRSSKeyUrl { get { return SiteLink + "getrss.php"; } }
        private string SearchUrl { get { return SiteLink + "browse.php"; } }
        private string RSSUrl { get { return SiteLink + "rss.php?secret_key={0}&feedtype=download&timezone=0&showrows=50&categories=all"; } }
        private string CommentUrl { get { return SiteLink + "details.php?id={0}"; } }
        private string DownloadUrl { get { return SiteLink + "download.php?id={0}"; } }

        private new ConfigurationDataBasicLoginWithRSS configData
        {
            get { return (ConfigurationDataBasicLoginWithRSS)base.configData; }
            set { base.configData = value; }
        }

        public TVChaosUK(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(name: "TV Chaos",
                description: "Total Chaos",
                link: "https://www.tvchaosuk.com/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                configService: configService,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLoginWithRSS())
        {
            Encoding = Encoding.UTF8;
            Language = "en-uk";
            Type = "private";

            AddCategoryMapping(72, TorznabCatType.PC, "Appz");

            AddCategoryMapping(115, TorznabCatType.TV, "Classic TV");
            AddCategoryMapping(139, TorznabCatType.TV, "Classic Comedy");
            AddCategoryMapping(141, TorznabCatType.TV, "Classic Comedy Drama");
            AddCategoryMapping(140, TorznabCatType.TV, "Classic Crime Drama");
            AddCategoryMapping(118, TorznabCatType.TV, "Classic Documentary");
            AddCategoryMapping(138, TorznabCatType.TV, "Classic Drama");
            AddCategoryMapping(149, TorznabCatType.TV, "Classic Kids/Family");
            AddCategoryMapping(142, TorznabCatType.TV, "Classic Sci-fi");
            AddCategoryMapping(148, TorznabCatType.TV, "Classic Soap");

            AddCategoryMapping(78, TorznabCatType.TV, "Comedy");
            AddCategoryMapping(187, TorznabCatType.TV, "Comedy Panel/Talk show");
            AddCategoryMapping(172, TorznabCatType.TVHD, "HD Comedy");
            AddCategoryMapping(107, TorznabCatType.TV, "Stand-up Comedy");

            AddCategoryMapping(75, TorznabCatType.TVDocumentary, "Documentary & News");
            AddCategoryMapping(189, TorznabCatType.TVDocumentary, "Docudrama");
            AddCategoryMapping(224, TorznabCatType.TVDocumentary, "Documentary");
            AddCategoryMapping(174, TorznabCatType.TVDocumentary, "HD Documentary");
            AddCategoryMapping(113, TorznabCatType.TVDocumentary, "Historical");
            AddCategoryMapping(218, TorznabCatType.TVDocumentary, "News and Current Affairs");
            AddCategoryMapping(100, TorznabCatType.TVDocumentary, "True Crime");
            AddCategoryMapping(98, TorznabCatType.TVDocumentary, "Wildlife/Nature");

            AddCategoryMapping(74, TorznabCatType.TV, "Drama");
            AddCategoryMapping(180, TorznabCatType.TV, "Comedy-Drama");
            AddCategoryMapping(76, TorznabCatType.TV, "Crime Drama");
            AddCategoryMapping(99, TorznabCatType.TV, "Cult Drama");
            AddCategoryMapping(175, TorznabCatType.TVHD, "HD Drama");

            AddCategoryMapping(91, TorznabCatType.TV, "Entertainment");
            AddCategoryMapping(212, TorznabCatType.TV, "Chat Shows");
            AddCategoryMapping(223, TorznabCatType.TVHD, "HD Entertainment");
            AddCategoryMapping(188, TorznabCatType.TV, "Musical TV");
            AddCategoryMapping(217, TorznabCatType.TV, "Quiz, Panel & Game Shows");
            AddCategoryMapping(101, TorznabCatType.TV, "Special Interest");

            AddCategoryMapping(106, TorznabCatType.TV, "Factual & Reality");
            AddCategoryMapping(103, TorznabCatType.TV, "Cookery, Food and Drink");
            AddCategoryMapping(114, TorznabCatType.TV, "Factual TV");
            AddCategoryMapping(221, TorznabCatType.TVHD, "HD Factual/Reality");
            AddCategoryMapping(215, TorznabCatType.TV, "Home and Garden");
            AddCategoryMapping(219, TorznabCatType.TV, "Motoring");
            AddCategoryMapping(216, TorznabCatType.TV, "Reality TV");

            AddCategoryMapping(184, TorznabCatType.TV, "Full Series Packs");
            AddCategoryMapping(194, TorznabCatType.TV, "Classic Comedy");
            AddCategoryMapping(193, TorznabCatType.TV, "Classic Drama");
            AddCategoryMapping(196, TorznabCatType.TV, "Classic Kids/Family");
            AddCategoryMapping(170, TorznabCatType.TV, "Comedy");
            AddCategoryMapping(168, TorznabCatType.TV, "Comedy Drama");
            AddCategoryMapping(228, TorznabCatType.TV, "Commonwealth");
            AddCategoryMapping(190, TorznabCatType.TV, "Crime Drama");
            AddCategoryMapping(166, TorznabCatType.TV, "Documentary");
            AddCategoryMapping(185, TorznabCatType.TV, "Drama");
            AddCategoryMapping(191, TorznabCatType.TV, "Entertainment");
            AddCategoryMapping(210, TorznabCatType.TV, "Factual");
            AddCategoryMapping(226, TorznabCatType.TV, "Foreign");
            AddCategoryMapping(167, TorznabCatType.TV, "Kids/Family");
            AddCategoryMapping(186, TorznabCatType.TV, "Sci-fi");

            AddCategoryMapping(82, TorznabCatType.TV, "Kids/Family");

            AddCategoryMapping(198, TorznabCatType.TVFOREIGN, "Non-UK");
            AddCategoryMapping(201, TorznabCatType.TVFOREIGN, "Comedy");
            AddCategoryMapping(208, TorznabCatType.TVFOREIGN, "Documentary");
            AddCategoryMapping(200, TorznabCatType.TVFOREIGN, "Drama");
            AddCategoryMapping(209, TorznabCatType.TVFOREIGN, "Entertainment");
            AddCategoryMapping(203, TorznabCatType.TVFOREIGN, "Factual/Reality");
            AddCategoryMapping(227, TorznabCatType.TVFOREIGN, "Foreign");
            AddCategoryMapping(202, TorznabCatType.TVFOREIGN, "Sci-fi");
            AddCategoryMapping(199, TorznabCatType.TVFOREIGN, "Soaps");
            AddCategoryMapping(204, TorznabCatType.TVFOREIGN, "Special Interest");

            AddCategoryMapping(86, TorznabCatType.Audio, "Radio/Audio");
            AddCategoryMapping(87, TorznabCatType.AudioAudiobook, "Audio Books");
            AddCategoryMapping(88, TorznabCatType.Audio, "Radio Comedy");

            AddCategoryMapping(90, TorznabCatType.TVSD, "Sci-Fi");
            AddCategoryMapping(183, TorznabCatType.TVSD, "Fantasy");
            AddCategoryMapping(176, TorznabCatType.TVHD, "HD Sci-Fi");
            AddCategoryMapping(173, TorznabCatType.TV, "Supernatural/fantasy");

            AddCategoryMapping(220, TorznabCatType.TV, "Soaps");
            AddCategoryMapping(229, TorznabCatType.TV, "Monthly Archives");
            AddCategoryMapping(177, TorznabCatType.TVHD, "Soap HD");
            AddCategoryMapping(230, TorznabCatType.TVSD, "Soap SD");

            AddCategoryMapping(92, TorznabCatType.TVSport, "Sport");
            AddCategoryMapping(222, TorznabCatType.TVSport, "HD Sport");

            AddCategoryMapping(83, TorznabCatType.Movies, "TV Aired Movies");
            AddCategoryMapping(171, TorznabCatType.Movies, "Classic Movies");
            AddCategoryMapping(178, TorznabCatType.MoviesHD, "HD TV Aired Movies");
            AddCategoryMapping(181, TorznabCatType.Movies, "Made for TV");
            AddCategoryMapping(182, TorznabCatType.Movies, "TV Aired Movies");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, SearchUrl, SiteLink);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("logout.php"), () =>
            {
                CQ dom = result.Content;
                var errorMessage = dom[".left_side table:eq(0) tr:eq(1)"].Text().Trim().Replace("\n\t", " ");
                throw new ExceptionWithConfigData(errorMessage, configData);
            });

            try
            {
                // Get RSS key
                var rssParams = new Dictionary<string, string> {
                { "feedtype", "download" },
                { "timezone", "0" },
                { "showrows", "50" }
            };
                var rssPage = await PostDataWithCookies(GetRSSKeyUrl, rssParams, result.Cookies);
                var match = Regex.Match(rssPage.Content, "(?<=secret_key\\=)([a-zA-z0-9]*)");
                configData.RSSKey.Value = match.Success ? match.Value : string.Empty;
                if (string.IsNullOrWhiteSpace(configData.RSSKey.Value))
                    throw new Exception("Failed to get RSS Key");
                SaveConfig();
            }
            catch (Exception e)
            {
                IsConfigured = false;
                throw e;
            }
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();

            // If we have no query use the RSS Page as their server is slow enough at times!
            if (query.IsTest || string.IsNullOrWhiteSpace(searchString))
            {
                var rssPage = await RequestStringWithCookiesAndRetry(string.Format(RSSUrl, configData.RSSKey.Value));
                var rssDoc = XDocument.Parse(rssPage.Content);

                foreach (var item in rssDoc.Descendants("item"))
                {
                    var title = item.Descendants("title").First().Value;
                    var description = item.Descendants("description").First().Value;
                    var link = item.Descendants("link").First().Value;
                    var category = item.Descendants("category").First().Value;
                    var date = item.Descendants("pubDate").First().Value;

                    var torrentIdMatch = Regex.Match(link, "(?<=id=)(\\d)*");
                    var torrentId = torrentIdMatch.Success ? torrentIdMatch.Value : string.Empty;
                    if (string.IsNullOrWhiteSpace(torrentId))
                        throw new Exception("Missing torrent id");

                    var infoMatch = Regex.Match(description, @"Category:\W(?<cat>.*)\W\/\WSeeders:\W(?<seeders>[\d,]*)\W\/\WLeechers:\W(?<leechers>[\d,]*)\W\/\WSize:\W(?<size>[\d\.]*\W\S*)\W\/\WSnatched:\W(?<snatched>[\d,]*) x times");
                    if (!infoMatch.Success)
                        throw new Exception(string.Format("Unable to find info in {0}: ", description));

                    var release = new ReleaseInfo()
                    {
                        Title = title,
                        Description = description,
                        Guid = new Uri(string.Format(DownloadUrl, torrentId)),
                        Comments = new Uri(string.Format(CommentUrl, torrentId)),
                        PublishDate = DateTime.ParseExact(date, "yyyy-MM-dd H:mm:ss", CultureInfo.InvariantCulture), //2015-08-08 21:20:31
                        Link = new Uri(string.Format(DownloadUrl, torrentId)),
                        Seeders = ParseUtil.CoerceInt(infoMatch.Groups["seeders"].Value),
                        Peers = ParseUtil.CoerceInt(infoMatch.Groups["leechers"].Value),
                        Grabs = ParseUtil.CoerceInt(infoMatch.Groups["snatched"].Value),
                        Size = ReleaseInfo.GetBytes(infoMatch.Groups["size"].Value),
                        Category = MapTrackerCatDescToNewznab(infoMatch.Groups["cat"].Value)
                    };

                    release.Peers += release.Seeders;
                    releases.Add(release);
                }
            }
            if (query.IsTest || !string.IsNullOrWhiteSpace(searchString))
            {
                // The TVChaos UK search requires an exact match of the search string.
                // But it seems like they just send the unfiltered search to the SQL server in a like query (LIKE '%$searchstring%').
                // So we replace any whitespace/special character with % to make the search more usable.
                Regex ReplaceRegex = new Regex("[^a-zA-Z0-9]+");
                searchString = ReplaceRegex.Replace(searchString, "%");

                var searchParams = new Dictionary<string, string> {
                    { "do", "search" },
                    { "keywords",  searchString },
                    { "search_type", "t_name" },
                    { "category", "0" },
                    { "include_dead_torrents", "no" }
                };

                var searchPage = await PostDataWithCookiesAndRetry(SearchUrl, searchParams);
                if (searchPage.IsRedirect)
                {
                    // re-login
                    await ApplyConfiguration(null);
                    searchPage = await PostDataWithCookiesAndRetry(SearchUrl, searchParams);
                }

                try
                {
                    CQ dom = searchPage.Content;
                    var rows = dom["#listtorrents tbody tr"];
                    foreach (var row in rows.Skip(1))
                    {
                        var release = new ReleaseInfo();
                        var qRow = row.Cq();

                        release.Title = qRow.Find("td:eq(1) .tooltip-content div:eq(0)").Text();

                        if (string.IsNullOrWhiteSpace(release.Title))
                            continue;

                        var tooltip = qRow.Find("div.tooltip-content");
                        var banner = tooltip.Find("img");
                        release.Description = tooltip.Text();
                        if (banner.Any())
                            release.BannerUrl = new Uri(banner.Attr("src"));
                        release.Guid = new Uri(qRow.Find("td:eq(2) a").Attr("href"));
                        release.Link = release.Guid;
                        release.Comments = new Uri(qRow.Find("td:eq(1) .tooltip-target a").Attr("href"));
                        release.PublishDate = DateTime.ParseExact(qRow.Find("td:eq(1) div").Last().Text().Trim(), "dd-MM-yyyy H:mm", CultureInfo.InvariantCulture); //08-08-2015 12:51
                        release.Seeders = ParseUtil.CoerceInt(qRow.Find("td:eq(6)").Text());
                        release.Peers = release.Seeders + ParseUtil.CoerceInt(qRow.Find("td:eq(7)").Text().Trim());
                        release.Size = ReleaseInfo.GetBytes(qRow.Find("td:eq(4)").Text().Trim());

                        var cat = row.Cq().Find("td:eq(0) a").First().Attr("href");
                        var catSplit = cat.LastIndexOf('=');
                        if (catSplit > -1)
                            cat = cat.Substring(catSplit + 1);
                        release.Category = MapTrackerCatToNewznab(cat);

                        var grabs = qRow.Find("td:nth-child(6)").Text();
                        release.Grabs = ParseUtil.CoerceInt(grabs);

                        if (qRow.Find("img[alt*=\"Free Torrent\"]").Length >= 1)
                            release.DownloadVolumeFactor = 0;
                        else
                            release.DownloadVolumeFactor = 1;

                        if (qRow.Find("img[alt*=\"x2 Torrent\"]").Length >= 1)
                            release.UploadVolumeFactor = 2;
                        else
                            release.UploadVolumeFactor = 1;

                        releases.Add(release);
                    }
                }
                catch (Exception ex)
                {
                    OnParseError(searchPage.Content, ex);
                }
            }

            return releases;
        }
    }
}
