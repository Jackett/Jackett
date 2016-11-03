using CsQuery;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Jackett.Models.IndexerConfig;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Jackett.Indexers
{
    public class XSpeeds : BaseIndexer, IIndexer
    {
        string LoginUrl { get { return SiteLink + "takelogin.php"; } }
        string GetRSSKeyUrl { get { return SiteLink + "getrss.php"; } }
        string SearchUrl { get { return SiteLink + "browse.php"; } }
        string RSSUrl { get { return SiteLink + "rss.php?secret_key={0}&feedtype=download&timezone=0&showrows=50&categories=all"; } }
        string CommentUrl { get { return SiteLink + "details.php?id={0}"; } }
        string DownloadUrl { get { return SiteLink + "download.php?id={0}"; } }

        new ConfigurationDataBasicLoginWithRSSAndDisplay configData
        {
            get {return (ConfigurationDataBasicLoginWithRSSAndDisplay)base.configData; }
            set { base.configData = value; }
        }

        public XSpeeds(IIndexerManagerService i, IWebClient wc, Logger l, IProtectionService ps)
            : base(name: "XSpeeds",
                description: "XSpeeds",
                link: "https://www.xspeeds.eu/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLoginWithRSSAndDisplay())
        {
            this.configData.DisplayText.Value = "Expect an initial delay (often around 10 seconds) due to XSpeeds CloudFlare DDoS protection";
            this.configData.DisplayText.Name = "Notice";
            AddCategoryMapping(70, TorznabCatType.TVAnime);
            AddCategoryMapping(80, TorznabCatType.AudioAudiobook);
            AddCategoryMapping(66, TorznabCatType.MoviesBluRay);
            AddCategoryMapping(48, TorznabCatType.Books);
            AddCategoryMapping(68, TorznabCatType.MoviesOther);
            AddCategoryMapping(65, TorznabCatType.TVDocumentary);
            AddCategoryMapping(10, TorznabCatType.MoviesDVD);
            AddCategoryMapping(74, TorznabCatType.TVOTHER);
            AddCategoryMapping(44, TorznabCatType.TVSport);
            AddCategoryMapping(12, TorznabCatType.Movies);
            AddCategoryMapping(13, TorznabCatType.Audio);
            AddCategoryMapping(6, TorznabCatType.PC);
            AddCategoryMapping(4, TorznabCatType.PC);
            AddCategoryMapping(31, TorznabCatType.ConsolePS3);
            AddCategoryMapping(31, TorznabCatType.ConsolePS4);
            AddCategoryMapping(20, TorznabCatType.TVSport);
            AddCategoryMapping(86, TorznabCatType.TVSport);
            AddCategoryMapping(47, TorznabCatType.TVHD);
            AddCategoryMapping(16, TorznabCatType.TVSD);
            AddCategoryMapping(7, TorznabCatType.ConsoleWii);
            AddCategoryMapping(8, TorznabCatType.ConsoleXbox);

            // RSS Textual categories
            AddCategoryMapping("Apps", TorznabCatType.PC);
            AddCategoryMapping("Music", TorznabCatType.Audio);
            AddCategoryMapping("Audiobooks", TorznabCatType.AudioAudiobook);
            
        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, SiteLink, true);
            result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, result.Cookies, true, SearchUrl, SiteLink,true);
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

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();
            var prevCook = CookieHeader + "";

            // If we have no query use the RSS Page as their server is slow enough at times!
            if (string.IsNullOrWhiteSpace(searchString))
            {
                var rssPage = await RequestStringWithCookiesAndRetry(string.Format(RSSUrl, configData.RSSKey.Value));
                if (rssPage.Content.EndsWith("\0")) {
                    rssPage.Content = rssPage.Content.Substring(0, rssPage.Content.Length - 1);
                }
                rssPage.Content = rssPage.Content.Replace("\0x10", "").Replace("\0x07", "");
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

                    var infoMatch = Regex.Match(description, @"Category:\W(?<cat>.*)\W\/\WSeeders:\W(?<seeders>[\d\,]*)\W\/\WLeechers:\W(?<leechers>[\d\,]*)\W\/\WSize:\W(?<size>[\d\.]*\W\S*)");
                    if (!infoMatch.Success)
                        throw new Exception("Unable to find info");

                    var release = new ReleaseInfo()
                    {
                        Title = title,
                        Description = title,
                        Guid = new Uri(string.Format(DownloadUrl, torrentId)),
                        Comments = new Uri(string.Format(CommentUrl, torrentId)),
                        PublishDate = DateTime.ParseExact(date, "yyyy-MM-dd H:mm:ss", CultureInfo.InvariantCulture), //2015-08-08 21:20:31 
                        Link = new Uri(string.Format(DownloadUrl, torrentId)),
                        Seeders = ParseUtil.CoerceInt(infoMatch.Groups["seeders"].Value),
                        Peers = ParseUtil.CoerceInt(infoMatch.Groups["leechers"].Value),
                        Size = ReleaseInfo.GetBytes(infoMatch.Groups["size"].Value),
                        Category = MapTrackerCatToNewznab(infoMatch.Groups["cat"].Value)
                    };

                    // If its not apps or audio we can only mark as general TV
                    if (release.Category == 0)
                        release.Category = 5030;

                    release.Peers += release.Seeders;
                    releases.Add(release);
                }
            }
            else
            {
                if (searchString.Length < 3)
                {
                    OnParseError("", new Exception("Minimum search length is 3"));
                    return releases;
                }
                var searchParams = new Dictionary<string, string> {
                    { "do", "search" },
                    { "keywords",  searchString },
                    { "search_type", "t_name" },
                    { "category", "0" },
                    { "include_dead_torrents", "no" }
                };
                var pairs = new Dictionary<string, string> {
                    { "username", configData.Username.Value },
                    { "password", configData.Password.Value }
                };
                var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, this.CookieHeader, true, null, SiteLink, true);
                if (!result.Cookies.Trim().Equals(prevCook.Trim()))
                {
                    result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, result.Cookies, true, SearchUrl, SiteLink, true);
                }
                this.CookieHeader = result.Cookies;

                var attempt = 1;
                var searchPage = await PostDataWithCookiesAndRetry(SearchUrl, searchParams,this.CookieHeader);
                while (searchPage.IsRedirect && attempt < 3)
                {
                    // add any cookies
                    var cookieString = this.CookieHeader;
                    if (searchPage.Cookies != null)
                    {
                        cookieString += " " + searchPage.Cookies;
                        // resolve cookie conflicts - really no need for this as the webclient will handle it
                        System.Text.RegularExpressions.Regex expression = new System.Text.RegularExpressions.Regex(@"([^\s]+)=([^=]+)(?:\s|$)");
                        Dictionary<string, string> cookieDIctionary = new Dictionary<string, string>();
                        var matches = expression.Match(cookieString);
                        while (matches.Success)
                        {
                            if (matches.Groups.Count > 2) cookieDIctionary[matches.Groups[1].Value] = matches.Groups[2].Value;
                            matches = matches.NextMatch();
                        }
                        cookieString = string.Join(" ", cookieDIctionary.Select(kv => kv.Key.ToString() + "=" + kv.Value.ToString()).ToArray());
                    }
                    this.CookieHeader = cookieString;
                    attempt++;
                    searchPage = await PostDataWithCookiesAndRetry(SearchUrl, searchParams, this.CookieHeader);
                }
                try
                {
                    CQ dom = searchPage.Content;
                    var rows = dom["table#sortabletable > tbody > tr:not(:has(td.thead))"];
                    foreach (var row in rows)
                    {
                        var release = new ReleaseInfo();
                        var qRow = row.Cq();

                        var qDetails = qRow.Find("div > a[href*=\"details.php?id=\"]"); // details link, release name get's shortened if it's to long
                        var qTitle = qRow.Find("td:eq(1) .tooltip-content div:eq(0)"); // use Title from tooltip
                        if(!qTitle.Any()) // fallback to Details link if there's no tooltip
                        {
                            qTitle = qDetails;
                        }
                        release.Title = qTitle.Text();

                        release.Guid = new Uri(qRow.Find("td:eq(2) a").Attr("href"));
                        release.Link = release.Guid;
                        release.Comments = new Uri(qDetails.Attr("href"));
                        release.PublishDate = DateTime.ParseExact(qRow.Find("td:eq(1) div").Last().Text().Trim(), "dd-MM-yyyy H:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal); //08-08-2015 12:51 
                        release.Seeders = ParseUtil.CoerceInt(qRow.Find("td:eq(6)").Text());
                        release.Peers = release.Seeders + ParseUtil.CoerceInt(qRow.Find("td:eq(7)").Text().Trim());
                        release.Size = ReleaseInfo.GetBytes(qRow.Find("td:eq(4)").Text().Trim());


                        var cat = row.Cq().Find("td:eq(0) a").First().Attr("href");
                        var catSplit = cat.LastIndexOf('=');
                        if (catSplit > -1)
                            cat = cat.Substring(catSplit + 1);
                        release.Category = MapTrackerCatToNewznab(cat);

                        // If its not apps or audio we can only mark as general TV
                        if (release.Category == 0)
                            release.Category = 5030;

                        var grabs = qRow.Find("td:nth-child(6)").Text();
                        release.Grabs = ParseUtil.CoerceInt(grabs);

                        if (qRow.Find("img[alt^=\"Free Torrent\"]").Length >= 1)
                            release.DownloadVolumeFactor = 0;
                        else if (qRow.Find("img[alt^=\"Silver Torrent\"]").Length >= 1)
                            release.DownloadVolumeFactor = 0.5;
                        else
                            release.DownloadVolumeFactor = 1;

                        if (qRow.Find("img[alt^=\"x2 Torrent\"]").Length >= 1)
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
            if (!CookieHeader.Trim().Equals(prevCook.Trim()))
            {
                this.SaveConfig();
            }
            return releases;
        }
    }
}
