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
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Jackett.Models.IndexerConfig;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Jackett.Indexers
{
    public class TVChaosUK : BaseWebIndexer
    {
        string LoginUrl { get { return SiteLink + "takelogin.php"; } }
        string GetRSSKeyUrl { get { return SiteLink + "getrss.php"; } }
        string SearchUrl { get { return SiteLink + "browse.php"; } }
        string RSSUrl { get { return SiteLink + "rss.php?secret_key={0}&feedtype=download&timezone=0&showrows=50&categories=all"; } }
        string CommentUrl { get { return SiteLink + "details.php?id={0}"; } }
        string DownloadUrl { get { return SiteLink + "download.php?id={0}"; } }

        new ConfigurationDataBasicLoginWithRSS configData
        {
            get { return (ConfigurationDataBasicLoginWithRSS)base.configData; }
            set { base.configData = value; }
        }

        public TVChaosUK(IIndexerConfigurationService configService, IWebClient wc, Logger l, IProtectionService ps)
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
            Encoding = Encoding.GetEncoding("UTF-8");
            Language = "en-uk";
            Type = "private";

            AddCategoryMapping(72, TorznabCatType.PC);
            AddCategoryMapping(86, TorznabCatType.Audio);
            AddCategoryMapping(87, TorznabCatType.AudioAudiobook);
            AddCategoryMapping(88, TorznabCatType.Audio);

            AddCategoryMapping(83, TorznabCatType.Movies);
            AddCategoryMapping(171, TorznabCatType.Movies);
            AddCategoryMapping(178, TorznabCatType.MoviesHD);
            AddCategoryMapping(181, TorznabCatType.Movies);
            AddCategoryMapping(182, TorznabCatType.Movies);

            AddCategoryMapping(75, TorznabCatType.TVDocumentary);
            AddCategoryMapping(189, TorznabCatType.TVDocumentary);
            AddCategoryMapping(224, TorznabCatType.TVDocumentary);
            AddCategoryMapping(174, TorznabCatType.TVDocumentary);
            AddCategoryMapping(113, TorznabCatType.TVDocumentary);
            AddCategoryMapping(100, TorznabCatType.TVDocumentary);
            AddCategoryMapping(98, TorznabCatType.TVDocumentary);

            AddCategoryMapping(176, TorznabCatType.TVHD);
            AddCategoryMapping(175, TorznabCatType.TVHD);
            AddCategoryMapping(177, TorznabCatType.TVHD);
            AddCategoryMapping(223, TorznabCatType.TVHD);
            AddCategoryMapping(222, TorznabCatType.TVHD);
            AddCategoryMapping(172, TorznabCatType.TVHD);
            AddCategoryMapping(221, TorznabCatType.TVHD);

            // RSS Textual categories
            AddCategoryMapping("Appz", TorznabCatType.PC);
            AddCategoryMapping("Radio/Audio", TorznabCatType.Audio);
            AddCategoryMapping("Audio Books", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("Radio Comedy", TorznabCatType.AudioAudiobook);

            AddCategoryMapping("TV Aired Movies", TorznabCatType.Movies);
            AddCategoryMapping("Classic Movies", TorznabCatType.Movies);
            AddCategoryMapping("HD TV Aired Movies", TorznabCatType.MoviesHD);
            AddCategoryMapping("Made for TV", TorznabCatType.Movies);
            AddCategoryMapping("TV Aired Movies", TorznabCatType.Movies);

            AddCategoryMapping("Documentary & News", TorznabCatType.TVDocumentary);
            AddCategoryMapping("Docudrama", TorznabCatType.TVDocumentary);
            AddCategoryMapping("Documentary", TorznabCatType.TVDocumentary);
            AddCategoryMapping("HD Documentary", TorznabCatType.TVDocumentary);
            AddCategoryMapping("Historical", TorznabCatType.TVDocumentary);
            AddCategoryMapping("True Crime", TorznabCatType.TVDocumentary);
            AddCategoryMapping("Wildlife/Nature", TorznabCatType.TVDocumentary);

            AddCategoryMapping("HD Sci-Fi", TorznabCatType.TVHD);
            AddCategoryMapping("HD Drama", TorznabCatType.TVHD);
            AddCategoryMapping("HD Soaps", TorznabCatType.TVHD);
            AddCategoryMapping("HD Entertainment", TorznabCatType.TVHD);
            AddCategoryMapping("HD Sport", TorznabCatType.TVHD);
            AddCategoryMapping("HD Comedy", TorznabCatType.TVHD);
            AddCategoryMapping("HD Factual/Reality", TorznabCatType.TVHD);
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
                        Category = MapTrackerCatToNewznab(infoMatch.Groups["cat"].Value)
                    };

                    // If its not apps or audio we can only mark as general TV
                    if (release.Category.Count() == 0)
                        release.Category.Add(5030);

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

                        // If its not apps or audio we can only mark as general TV
                        if (release.Category.Count() == 0)
                            release.Category.Add(5030);

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
