using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using CsQuery;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    public class RevolutionTT : BaseWebIndexer
    {
        private string LandingPageURL { get { return SiteLink + "login.php"; } }
        private string LoginUrl { get { return SiteLink + "takelogin.php"; } }
        private string GetRSSKeyUrl { get { return SiteLink + "getrss.php"; } }
        private string RSSUrl { get { return SiteLink + "rss.php?feed=dl&passkey="; } }
        private string SearchUrl { get { return SiteLink + "browse.php"; } }
        private string DetailsURL { get { return SiteLink + "details.php?id={0}&hit=1"; } }

        private new ConfigurationDataBasicLoginWithRSS configData
        {
            get { return (ConfigurationDataBasicLoginWithRSS)base.configData; }
            set { base.configData = value; }
        }

        public RevolutionTT(IIndexerConfigurationService configService, Utils.Clients.WebClient wc, Logger l, IProtectionService ps)
            : base(name: "RevolutionTT",
                description: "The Revolution has begun",
                link: "https://revolutiontt.me/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                configService: configService,
                client: wc,
                logger: l,
                p: ps,
                downloadBase: "https://revolutiontt.me/download.php/",
                configData: new ConfigurationDataBasicLoginWithRSS())
        {
            Encoding = Encoding.GetEncoding("iso-8859-1");
            Language = "en-us";
            Type = "private";

            /* Original RevolutionTT Categories -

			Anime - 23
			Appz/Misc - 22
			Appz/PC-ISO - 1
			E-Book - 36
			Games/PC-ISO - 4
			Games/PC-Rips - 21
			Games/PS3 - 16
			Games/Wii - 40
			Games/XBOX360 - 39
			Handheld/NDS - 35
			Handheld/PSP - 34
			Mac	- 2
			Movies/BluRay - 10
			Movies/DVDR - 20
			Movies/HDx264 - 12
			Movies/Packs - 44
			Movies/SDx264 - 11
			Movies/XviD - 19
			Music - 6
			Music/FLAC - 8
			Music/Packs - 46
			MusicVideos - 29
			TV/DVDR - 43
			TV/HDx264 - 42
			TV/Packs - 45
			TV/SDx264 - 41
			TV/XViD - 7

			*/

            //AddCategoryMapping("cat_id", TorznabCatType.Console);
            AddCategoryMapping("35", TorznabCatType.ConsoleNDS);
            AddCategoryMapping("34", TorznabCatType.ConsolePSP);
            AddCategoryMapping("40", TorznabCatType.ConsoleWii);
            //AddCategoryMapping("cat_id", TorznabCatType.ConsoleXbox);
            AddCategoryMapping("39", TorznabCatType.ConsoleXbox360);
            //AddCategoryMapping("cat_id", TorznabCatType.ConsoleWiiwareVC);
            //AddCategoryMapping("cat_id", TorznabCatType.ConsoleXBOX360DLC);
            AddCategoryMapping("16", TorznabCatType.ConsolePS3);
            //AddCategoryMapping("cat_id", TorznabCatType.ConsoleOther);
            //AddCategoryMapping("cat_id", TorznabCatType.Console3DS);
            //AddCategoryMapping("cat_id", TorznabCatType.ConsolePSVita);
            AddCategoryMapping("40", TorznabCatType.ConsoleWiiU);
            //AddCategoryMapping("cat_id", TorznabCatType.ConsoleXboxOne);
            //AddCategoryMapping("cat_id", TorznabCatType.ConsolePS4);
            AddCategoryMapping("44", TorznabCatType.Movies);
            //AddCategoryMapping("cat_id", TorznabCatType.MoviesForeign);
            //AddCategoryMapping("cat_id", TorznabCatType.MoviesOther);
            //Movies/DVDR, Movies/SDx264, Movies/XviD
            AddMultiCategoryMapping(TorznabCatType.MoviesSD, 20, 11, 19);
            //Movies/BluRay, Movies/HDx264
            AddMultiCategoryMapping(TorznabCatType.MoviesHD, 10, 12);
            //AddCategoryMapping("cat_id", TorznabCatType.Movies3D);
            AddCategoryMapping("10", TorznabCatType.MoviesBluRay);
            AddCategoryMapping("20", TorznabCatType.MoviesDVD);
            //AddCategoryMapping("cat_id", TorznabCatType.MoviesWEBDL);
            //Music, Music/Packs
            AddMultiCategoryMapping(TorznabCatType.Audio, 6, 46);
            //AddCategoryMapping("cat_id", TorznabCatType.AudioMP3);
            AddCategoryMapping("29", TorznabCatType.AudioVideo);
            //AddCategoryMapping("cat_id", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("8", TorznabCatType.AudioLossless);
            //AddCategoryMapping("cat_id", TorznabCatType.AudioOther);
            //AddCategoryMapping("cat_id", TorznabCatType.AudioForeign);
            AddCategoryMapping("21", TorznabCatType.PC);
            AddCategoryMapping("22", TorznabCatType.PC0day);
            AddCategoryMapping("4", TorznabCatType.PCISO);
            AddCategoryMapping("2", TorznabCatType.PCMac);
            //AddCategoryMapping("cat_id", TorznabCatType.PCPhoneOther);
            //Games/PC-ISO, Games/PC-Rips
            AddMultiCategoryMapping(TorznabCatType.PCGames, 4, 21);
            //AddCategoryMapping("cat_id", TorznabCatType.PCPhoneIOS);
            //AddCategoryMapping("cat_id", TorznabCatType.PCPhoneAndroid);
            AddCategoryMapping("45", TorznabCatType.TV);
            //AddCategoryMapping("cat_id", TorznabCatType.TVWEBDL);
            //AddCategoryMapping("cat_id", TorznabCatType.TVFOREIGN);
            //TV/DVDR, TV/SDx264, TV/XViD
            AddMultiCategoryMapping(TorznabCatType.TVSD, 43, 41, 7);
            AddCategoryMapping("42", TorznabCatType.TVHD);
            //AddCategoryMapping("cat_id", TorznabCatType.TVOTHER);
            //AddCategoryMapping("cat_id", TorznabCatType.TVSport);
            AddCategoryMapping("23", TorznabCatType.TVAnime);
            //AddCategoryMapping("cat_id", TorznabCatType.TVDocumentary);
            //AddCategoryMapping("cat_id", TorznabCatType.XXX);
            //AddCategoryMapping("cat_id", TorznabCatType.XXXDVD);
            //AddCategoryMapping("cat_id", TorznabCatType.XXXWMV);
            //AddCategoryMapping("cat_id", TorznabCatType.XXXXviD);
            //AddCategoryMapping("cat_id", TorznabCatType.XXXx264);
            //AddCategoryMapping("cat_id", TorznabCatType.XXXOther);
            //AddCategoryMapping("cat_id", TorznabCatType.XXXImageset);
            //AddCategoryMapping("cat_id", TorznabCatType.XXXPacks);
            //AddCategoryMapping("cat_id", TorznabCatType.Other);
            //AddCategoryMapping("cat_id", TorznabCatType.OtherMisc);
            //AddCategoryMapping("cat_id", TorznabCatType.OtherHashed);
            AddCategoryMapping("36", TorznabCatType.Books);
            AddCategoryMapping("36", TorznabCatType.BooksEbook);
            //AddCategoryMapping("cat_id", TorznabCatType.BooksComics);
            //AddCategoryMapping("cat_id", TorznabCatType.BooksMagazines);
            //AddCategoryMapping("cat_id", TorznabCatType.BooksTechnical);
            //AddCategoryMapping("cat_id", TorznabCatType.BooksOther);
            //AddCategoryMapping("cat_id", TorznabCatType.BooksForeign);

            // RSS Textual categories
            AddCategoryMapping("Anime", TorznabCatType.TVAnime);
            AddCategoryMapping("Appz/Misc", TorznabCatType.PC0day);
            AddCategoryMapping("Appz/PC-ISO", TorznabCatType.Books);
            AddCategoryMapping("E-Book", TorznabCatType.BooksEbook);
            AddCategoryMapping("Games/PC-ISO", TorznabCatType.PCGames);
            AddCategoryMapping("Games/PC-Rips", TorznabCatType.PCGames);
            AddCategoryMapping("Games/PS3", TorznabCatType.ConsolePS3);
            AddCategoryMapping("Games/Wii", TorznabCatType.ConsoleWii);
            AddCategoryMapping("Games/XBOX360", TorznabCatType.ConsoleXbox360);
            AddCategoryMapping("Handheld/NDS", TorznabCatType.ConsoleNDS);
            AddCategoryMapping("Handheld/PSP", TorznabCatType.ConsolePSP);
            AddCategoryMapping("Mac", TorznabCatType.PCMac);
            AddCategoryMapping("Movies/BluRay", TorznabCatType.MoviesBluRay);
            AddCategoryMapping("Movies/DVDR", TorznabCatType.MoviesDVD);
            AddCategoryMapping("Movies/HDx264", TorznabCatType.MoviesHD);
            AddCategoryMapping("Movies/Packs", TorznabCatType.Movies);
            AddCategoryMapping("Movies/SDx264", TorznabCatType.MoviesSD);
            AddCategoryMapping("Movies/XviD", TorznabCatType.MoviesSD);
            AddCategoryMapping("Music", TorznabCatType.Audio);
            AddCategoryMapping("Music/FLAC", TorznabCatType.AudioLossless);
            AddCategoryMapping("Music/Packs", TorznabCatType.AudioOther);
            AddCategoryMapping("MusicVideos", TorznabCatType.AudioVideo);
            AddCategoryMapping("TV/DVDR", TorznabCatType.TV);
            AddCategoryMapping("TV/HDx264", TorznabCatType.TVHD);
            AddCategoryMapping("TV/Packs", TorznabCatType.TV);
            AddCategoryMapping("TV/SDx264", TorznabCatType.TVSD);
            AddCategoryMapping("TV/XViD", TorznabCatType.TVSD);
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value }
            };

            //  need to do an initial request to get PHP session cookie (any better way to do this?)
            var homePageLoad = await RequestLoginAndFollowRedirect(LandingPageURL, new Dictionary<string, string> { }, null, true, null, SiteLink);

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, homePageLoad.Cookies, true, null, LandingPageURL);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("/logout.php"), () =>
            {
                CQ dom = result.Content;
                var messageEl = dom[".error"];
                var errorMessage = messageEl.Text().Trim();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });

            //  Store RSS key from feed generator page
            try
            {
                var rssParams = new Dictionary<string, string> {
                { "feed", "dl" }
            };
                var rssPage = await PostDataWithCookies(GetRSSKeyUrl, rssParams, result.Cookies);
                var match = Regex.Match(rssPage.Content, "(?<=passkey\\=)([a-zA-z0-9]*)");
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
            var searchUrl = SearchUrl;

            // If query is empty, use the RSS Feed
            if (string.IsNullOrWhiteSpace(searchString))
            {
                var rssPage = await RequestStringWithCookiesAndRetry(RSSUrl + configData.RSSKey.Value);
                var rssDoc = XDocument.Parse(rssPage.Content);

                foreach (var item in rssDoc.Descendants("item"))
                {
                    var title = item.Descendants("title").First().Value;
                    if (title.StartsWith("Support YOUR site!"))
                        continue;
                    var description = item.Descendants("description").First().Value;
                    var link = item.Descendants("link").First().Value;
                    var date = item.Descendants("pubDate").First().Value;

                    var torrentIdMatch = Regex.Match(link, "(?<=download\\.php/)([a-zA-z0-9]*)");
                    var torrentId = torrentIdMatch.Success ? torrentIdMatch.Value : string.Empty;
                    if (string.IsNullOrWhiteSpace(torrentId))
                        throw new Exception("Missing torrent id");

                    var infoMatch = Regex.Match(description, @"Category:\W(?<cat>.*)\W\n\WSize:\W(?<size>.*)\n\WStatus:\W(?<seeders>.*)\Wseeder(.*)\Wand\W(?<leechers>.*)\Wleecher(.*)\n\WAdded:\W(?<added>.*)\n\WDescription:");
                    if (!infoMatch.Success)
                        throw new Exception("Unable to find info");

                    var imdbMatch = Regex.Match(description, "(?<=http://www.imdb.com/title/tt)([0-9]*)");
                    long? imdbID = null;
                    if (imdbMatch.Success)
                    {
                        long l;
                        if (long.TryParse(imdbMatch.Value, out l))
                        {
                            imdbID = l;
                        }
                    }

                    var release = new ReleaseInfo()
                    {
                        Title = title,
                        Description = title,
                        Guid = new Uri(string.Format(DetailsURL, torrentId)),
                        Comments = new Uri(string.Format(DetailsURL, torrentId)),
                        PublishDate = DateTime.ParseExact(infoMatch.Groups["added"].Value, "yyyy-MM-dd H:mm:ss", CultureInfo.InvariantCulture), //2015-08-08 21:20:31
                        Link = new Uri(link),
                        Seeders = ParseUtil.CoerceInt(infoMatch.Groups["seeders"].Value == "no" ? "0" : infoMatch.Groups["seeders"].Value),
                        Peers = ParseUtil.CoerceInt(infoMatch.Groups["leechers"].Value == "no" ? "0" : infoMatch.Groups["leechers"].Value),
                        Size = ReleaseInfo.GetBytes(infoMatch.Groups["size"].Value),
                        Category = MapTrackerCatToNewznab(infoMatch.Groups["cat"].Value),
                        Imdb = imdbID
                    };

                    //  if unknown category, set to "other"
                    if (release.Category.Count() == 0)
                        release.Category.Add(7000);

                    release.Peers += release.Seeders;
                    releases.Add(release);
                }
            }
            else
            {
                searchUrl += "?titleonly=1&search=" + WebUtility.UrlEncode(searchString);
                string.Format(SearchUrl, WebUtility.UrlEncode(searchString));

                var cats = MapTorznabCapsToTrackers(query);
                if (cats.Count > 0)
                {
                    foreach (var cat in cats)
                    {
                        searchUrl += "&c" + cat + "=1";
                    }
                }

                var results = await RequestStringWithCookiesAndRetry(searchUrl);
                try
                {
                    CQ dom = results.Content;

                    //  table header is the first <tr> in table body, get all rows except this
                    CQ qRows = dom["#torrents-table > tbody > tr:not(:first-child)"];

                    foreach (var row in qRows)
                    {
                        var release = new ReleaseInfo();

                        var qRow = row.Cq();

                        var debug = qRow.Html();

                        release.MinimumRatio = 1;
                        release.MinimumSeedTime = 172800;

                        CQ qLink = qRow.Find(".br_right > a").First();
                        release.Guid = new Uri(SiteLink + qLink.Attr("href"));
                        release.Comments = new Uri(SiteLink + qLink.Attr("href") + "&tocomm=1");
                        release.Title = qLink.Find("b").Text();
                        release.Description = release.Title;

                        var releaseLinkURI = qRow.Find("td:nth-child(4) > a").Attr("href");
                        release.Link = new Uri(SiteLink + releaseLinkURI);
                        if (releaseLinkURI != null)
                        {
                            var dateString = qRow.Find("td:nth-child(6) nobr")[0].TextContent.Trim();
                            //"2015-04-25 23:38:12"
                            //"yyyy-MMM-dd hh:mm:ss"
                            release.PublishDate = DateTime.ParseExact(dateString, "yyyy-MM-ddHH:mm:ss", CultureInfo.InvariantCulture);

                            var sizeStr = qRow.Children().ElementAt(6).InnerHTML.Trim();
                            sizeStr = sizeStr.Substring(0, sizeStr.IndexOf('<'));
                            release.Size = ReleaseInfo.GetBytes(sizeStr);

                            release.Seeders = ParseUtil.CoerceInt(qRow.Find("td:nth-child(9)").Text());
                            release.Peers = release.Seeders + ParseUtil.CoerceInt(qRow.Find("td:nth-child(10)").Text());

                            var category = qRow.Find(".br_type > a").Attr("href").Replace("browse.php?cat=", string.Empty);
                            release.Category = MapTrackerCatToNewznab(category);
                        }
                        releases.Add(release);
                    }
                }
                catch (Exception ex)
                {
                    OnParseError(results.Content, ex);
                }
            }

            return releases;
        }
    }
}