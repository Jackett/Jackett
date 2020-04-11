using System;
using System.Collections.Generic;
using System.Globalization;
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
    public class NCore : BaseWebIndexer
    {
        private string LoginUrl => SiteLink + "login.php";
        private string SearchUrl => SiteLink + "torrents.php";
        private readonly string[] LanguageCats = { "xvidser", "dvdser", "hdser", "xvid", "dvd", "dvd9", "hd", "mp3", "lossless", "ebook" };

        private new ConfigurationDataNCore configData
        {
            get => (ConfigurationDataNCore)base.configData;
            set => base.configData = value;
        }

        public NCore(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(name: "nCore",
                description: "A Hungarian private torrent site.",
                link: "https://ncore.cc/",
                caps: new TorznabCapabilities(),
                configService: configService,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataNCore())
        {
            Encoding = Encoding.UTF8;
            Language = "hu-hu";
            Type = "private";

            AddCategoryMapping("xvid_hun", TorznabCatType.MoviesSD, "Film SD/HU");
            AddCategoryMapping("xvid", TorznabCatType.MoviesSD, "Film SD/EN");
            AddCategoryMapping("dvd_hun", TorznabCatType.MoviesDVD, "Film DVDR/HU");
            AddCategoryMapping("dvd", TorznabCatType.MoviesDVD, "Film DVDR/EN");
            AddCategoryMapping("dvd9_hun", TorznabCatType.MoviesDVD, "Film DVD9/HU");
            AddCategoryMapping("dvd9", TorznabCatType.MoviesDVD, "Film DVD9/EN");
            AddCategoryMapping("hd_hun", TorznabCatType.MoviesHD, "Film HD/HU");
            AddCategoryMapping("hd", TorznabCatType.MoviesHD, "Film HD/EN");

            AddCategoryMapping("xvidser_hun", TorznabCatType.TVSD, "Sorozat SD/HU");
            AddCategoryMapping("xvidser", TorznabCatType.TVSD, "Sorozat SD/EN");
            AddCategoryMapping("dvdser_hun", TorznabCatType.TVSD, "Sorozat DVDR/HU");
            AddCategoryMapping("dvdser", TorznabCatType.TVSD, "Sorozat DVDR/EN");
            AddCategoryMapping("hdser_hun", TorznabCatType.TVHD, "Sorozat HD/HU");
            AddCategoryMapping("hdser", TorznabCatType.TVHD, "Sorozat HD/EN");

            AddCategoryMapping("mp3_hun", TorznabCatType.AudioMP3, "Zene MP3/HU");
            AddCategoryMapping("mp3", TorznabCatType.AudioMP3, "Zene MP3/EN");
            AddCategoryMapping("lossless_hun", TorznabCatType.AudioLossless, "Zene Lossless/HU");
            AddCategoryMapping("lossless", TorznabCatType.AudioLossless, "Zene Lossless/EN");
            AddCategoryMapping("clip", TorznabCatType.AudioVideo, "Zene Klip");

            AddCategoryMapping("xxx_xvid", TorznabCatType.XXXXviD, "XXX SD");
            AddCategoryMapping("xxx_dvd", TorznabCatType.XXXDVD, "XXX DVDR");
            AddCategoryMapping("xxx_imageset", TorznabCatType.XXXImageset, "XXX Imageset");
            AddCategoryMapping("xxx_hd", TorznabCatType.XXX, "XXX HD");

            AddCategoryMapping("game_iso", TorznabCatType.PCGames, "Játék PC/ISO");
            AddCategoryMapping("game_rip", TorznabCatType.PCGames, "Játék PC/RIP");
            AddCategoryMapping("console", TorznabCatType.Console, "Játék Konzol");

            AddCategoryMapping("iso", TorznabCatType.PCISO, "Program Prog/ISO");
            AddCategoryMapping("misc", TorznabCatType.PC0day, "Program Prog/RIP");
            AddCategoryMapping("mobil", TorznabCatType.PCPhoneOther, "Program Prog/Mobil");

            AddCategoryMapping("ebook_hun", TorznabCatType.Books, "Könyv eBook/HU");
            AddCategoryMapping("ebook", TorznabCatType.Books, "Könyv eBook/EN");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            if (configData.Hungarian.Value == false && configData.English.Value == false)
                throw new ExceptionWithConfigData("Please select atleast one language.", configData);

            var loginPage = await RequestStringWithCookies(LoginUrl, string.Empty);
            var pairs = new Dictionary<string, string> {
                { "nev", configData.Username.Value },
                { "pass", configData.Password.Value },
                { "ne_leptessen_ki", "1"},
                { "set_lang", "en" },
                { "submitted", "1" },
                { "submit", "Access!" }
            };

            if (!string.IsNullOrEmpty(configData.TwoFactor.Value))
                pairs.Add("2factor", configData.TwoFactor.Value);

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, loginPage.Cookies, true, referer: SiteLink);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("profile.php"), () =>
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(result.Content);
                var messageEl = dom.QuerySelector("#hibauzenet table tbody tr");
                var msgContainer = messageEl.Children[1];
                var errorMessage = msgContainer != null ? msgContainer.TextContent : "Error while trying to login.";
                throw new ExceptionWithConfigData(errorMessage, configData);
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var results = await PerformQuery(query, null);
            if (results.Count() == 0 && query.IsTVSearch) // if we search for a localized title ncore can't handle any extra S/E information, search without it and AND filter the results. See #1450
            {
                results = await PerformQuery(query, query.GetEpisodeSearchString());
            }
            return results;
        }

        private async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query, string seasonep)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();
            var pairs = new List<KeyValuePair<string, string>>();

            if (seasonep != null)
                searchString = query.SanitizedSearchTerm;

            pairs.Add(new KeyValuePair<string, string>("nyit_sorozat_resz", "true"));
            pairs.Add(new KeyValuePair<string, string>("miben", "name"));
            pairs.Add(new KeyValuePair<string, string>("tipus", "kivalasztottak_kozott"));
            pairs.Add(new KeyValuePair<string, string>("submit.x", "1"));
            pairs.Add(new KeyValuePair<string, string>("submit.y", "1"));
            pairs.Add(new KeyValuePair<string, string>("submit", "Ok"));
            pairs.Add(new KeyValuePair<string, string>("mire", searchString));

            var cats = MapTorznabCapsToTrackers(query);

            if (cats.Count == 0)
                cats = GetAllTrackerCategories();

            foreach (var lcat in LanguageCats)
            {
                if (!configData.Hungarian.Value)
                    cats.Remove(lcat + "_hun");
                if (!configData.English.Value)
                    cats.Remove(lcat);
            }

            foreach (var cat in cats)
                pairs.Add(new KeyValuePair<string, string>("kivalasztott_tipus[]", cat));

            var results = await PostDataWithCookiesAndRetry(SearchUrl, pairs);


            var parser = new HtmlParser();
            var dom = parser.ParseDocument(results.Content);
            var numVal = 0;

            // find number of torrents / page
            var torrentPerPage = dom.QuerySelector(".box_torrent_all")?.QuerySelectorAll(".box_torrent").Length ?? 0;
            if (torrentPerPage == 0)
                return releases;
            var startPage = (query.Offset / torrentPerPage) + 1;
            var previouslyParsedOnPage = query.Offset - (startPage * torrentPerPage) + 1; //+1 because indexing start from 0
            if (previouslyParsedOnPage < 0)
                previouslyParsedOnPage = query.Offset;

            // find pagelinks in the bottom
            var pageLinks = dom.QuerySelector("div[id=pager_bottom]")?.QuerySelectorAll("a");
            if (pageLinks?.Length > 0)
            {
                // If there are several pages find the link for the latest one
                for (var i = pageLinks.Length - 1; i > 0; i--)
                {
                    var lastPageLink = pageLinks[i].GetAttribute("href").Trim();
                    if (lastPageLink.Contains("oldal"))
                    {
                        var match = Regex.Match(lastPageLink, @"(?<=oldal=)(\d+)");
                        numVal = int.Parse(match.Value);
                        break;
                    }
                }
            }

            var limit = query.Limit;
            if (limit == 0)
                limit = 100;

            if (startPage == 1)
            {
                releases = parseTorrents(results, seasonep, query, releases.Count, limit, previouslyParsedOnPage);
                previouslyParsedOnPage = 0;
                startPage++;
            }


            // Check all the pages for the torrents.
            // The starting index is 2. (the first one is the original where we parse out the pages.)
            for (var i = startPage; (i <= numVal && releases.Count < limit); i++)
            {
                pairs.Add(new KeyValuePair<string, string>("oldal", i.ToString()));
                results = await PostDataWithCookiesAndRetry(SearchUrl, pairs);
                releases.AddRange(parseTorrents(results, seasonep, query, releases.Count, limit, previouslyParsedOnPage));
                previouslyParsedOnPage = 0;
                pairs.Remove(new KeyValuePair<string, string>("oldal", i.ToString()));
            }

            return releases;
        }

        private List<ReleaseInfo> parseTorrents(WebClientStringResult results, string seasonep, TorznabQuery query,
                                                int alreadyFounded, int limit, int previouslyParsedOnPage)
        {
            var releases = new List<ReleaseInfo>();
            try
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(results.Content);

                var rows = dom.QuerySelector(".box_torrent_all").QuerySelectorAll(".box_torrent");

                // Check torrents only till we reach the query Limit
                for (var i = previouslyParsedOnPage; (i < rows.Length && ((alreadyFounded + releases.Count) < limit)); i++)
                {
                    try
                    {
                        var row = rows[i];
                        var key = dom.QuerySelector("link[rel=alternate]").GetAttribute("href").Split('=').Last();

                        var release = new ReleaseInfo();
                        var torrentTxt = row.QuerySelector(".torrent_txt, .torrent_txt2").QuerySelector("a");
                        //if (torrentTxt == null) continue;
                        release.Title = torrentTxt.GetAttribute("title");
                        var descr = row.QuerySelector("span")?.GetAttribute("title") + " " + row.QuerySelector("a.infolink")?.TextContent;
                        release.Description = descr.Trim();

                        release.MinimumRatio = 1;
                        release.MinimumSeedTime = 172800; // 48 hours
                        release.DownloadVolumeFactor = 0;
                        release.UploadVolumeFactor = 1;

                        var downloadLink = SiteLink + torrentTxt.GetAttribute("href");
                        var downloadId = downloadLink.Substring(downloadLink.IndexOf("&id=") + 4);

                        release.Link = new Uri(SiteLink + "torrents.php?action=download&id=" + downloadId + "&key=" + key);
                        release.Comments = new Uri(SiteLink + "torrents.php?action=details&id=" + downloadId);
                        release.Guid = new Uri(release.Comments + "#comments");

                        release.Seeders = ParseUtil.CoerceInt(row.QuerySelector(".box_s2").QuerySelector("a").TextContent);
                        release.Peers = ParseUtil.CoerceInt(row.QuerySelector(".box_l2").QuerySelector("a").TextContent) + release.Seeders;
                        var imdblink = row.QuerySelector("a[href*=\".imdb.com/title\"]")?.GetAttribute("href");
                        if (!string.IsNullOrWhiteSpace(imdblink))
                            release.Imdb = ParseUtil.GetLongFromString(imdblink);
                        var banner = row.QuerySelector("img.infobar_ico")?.GetAttribute("onmouseover");
                        if (banner != null)
                        {
                            var bannerRegEx = new Regex(@"mutat\('(.*?)', '", RegexOptions.Compiled);
                            var bannerMatch = bannerRegEx.Match(banner);
                            var bannerurl = bannerMatch.Groups[1].Value;
                            release.BannerUrl = new Uri(bannerurl);
                        }
                        release.PublishDate = DateTime.Parse(row.QuerySelector(".box_feltoltve2").InnerHtml.Replace("<br>", " "), CultureInfo.InvariantCulture);
                        var sizeSplit = row.QuerySelector(".box_meret2").TextContent.Split(' ');
                        release.Size = ReleaseInfo.GetBytes(sizeSplit[1].ToLower(), ParseUtil.CoerceFloat(sizeSplit[0]));
                        var catlink = row.QuerySelector("a:has(img[class='categ_link'])").GetAttribute("href");
                        var cat = ParseUtil.GetArgumentFromQueryString(catlink, "tipus");
                        release.Category = MapTrackerCatToNewznab(cat);

                        /* if the release name not contains the language we add it because it is know from category */
                        if (cat.Contains("hun") && !release.Title.ToLower().Contains("hun"))
                            release.Title += ".hun";

                        if (seasonep == null)
                            releases.Add(release);

                        else
                        {
                            if (query.MatchQueryStringAND(release.Title, null, seasonep))
                            {
                                /* For sonnar if the search querry was english the title must be english also so we need to change the Description and Title */
                                var temp = release.Title;

                                // releasedata everithing after Name.S0Xe0X
                                var releasedata = release.Title.Split(new[] { seasonep }, StringSplitOptions.None)[1].Trim();

                                /* if the release name not contains the language we add it because it is know from category */
                                if (cat.Contains("hun") && !releasedata.Contains("hun"))
                                    releasedata += ".hun";

                                // release description contains [imdb: ****] but we only need the data before it for title
                                string[] description = { release.Description, "" };
                                if (release.Description.Contains("[imdb:"))
                                {
                                    description = release.Description.Split('[');
                                    description[1] = "[" + description[1];
                                }

                                release.Title = (description[0].Trim() + "." + seasonep.Trim() + "." + releasedata.Trim('.')).Replace(' ', '.');

                                // if search is done for S0X than we dont want to put . between S0X and E0X
                                var match = Regex.Match(releasedata, @"^E\d\d?");
                                if (seasonep.Length == 3 && match.Success)
                                    release.Title = (description[0].Trim() + "." + seasonep.Trim() + releasedata.Trim('.')).Replace(' ', '.');

                                // add back imdb points to the description [imdb: 8.7]
                                release.Description = temp + " " + description[1];
                                release.Description = release.Description.Trim();
                                releases.Add(release);
                            }
                        }
                    }
                    catch (FormatException ex)
                    {
                        logger.Error("Problem of parsing Torrent:" + rows[i].InnerHtml);
                        logger.Error("Exception was the following:" + ex);
                    }
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
