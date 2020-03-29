using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    public class Pretome : BaseWebIndexer
    {
        private string LoginUrl => SiteLink + "takelogin.php";
        private string SearchUrl => SiteLink + "browse.php";
        private readonly List<CategoryMapping> resultMapping = new List<CategoryMapping>();
        private new ConfigurationDataPinNumber configData => (ConfigurationDataPinNumber)base.configData;

        public Pretome(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base("PreToMe",
                description: "BitTorrent site for High Quality, High Definition (HD) movies and TV Shows",
                link: "https://pretome.info/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                client: wc,
                configService: configService,
                logger: l,
                p: ps,
                configData: new ConfigurationDataPinNumber())
        {
            Encoding = Encoding.GetEncoding("iso-8859-1");
            Language = "en-us";
            Type = "private";

            TorznabCaps.SupportsImdbMovieSearch = true;
            TorznabCaps.SupportsImdbTVSearch = true;

            // Applications
            AddCategoryMapping("cat[]=22&tags=Windows", TorznabCatType.PC0day);
            AddCategoryMapping("cat[]=22&tags=MAC", TorznabCatType.PCMac);
            AddCategoryMapping("cat[]=22&tags=Linux", TorznabCatType.PC);
            AddCategoryMapping("cat[]=22", TorznabCatType.PC);

            // Ebooks
            AddCategoryMapping("cat[]=27", TorznabCatType.BooksEbook);

            // Games
            AddCategoryMapping("cat[]=4&tags=PC", TorznabCatType.PCGames);
            AddCategoryMapping("cat[]=4&tags=RIP", TorznabCatType.PCGames);
            AddCategoryMapping("cat[]=4&tags=ISO", TorznabCatType.PCGames);
            AddCategoryMapping("cat[]=4&tags=XBOX360", TorznabCatType.ConsoleXbox360);
            AddCategoryMapping("cat[]=4&tags=PS3", TorznabCatType.ConsolePS3);
            AddCategoryMapping("cat[]=4&tags=Wii", TorznabCatType.ConsoleWii);
            AddCategoryMapping("cat[]=4&tags=PSP", TorznabCatType.ConsolePSP);
            AddCategoryMapping("cat[]=4&tags=NSD", TorznabCatType.ConsoleNDS);
            AddCategoryMapping("cat[]=4&tags=XBox", TorznabCatType.ConsoleXbox);
            AddCategoryMapping("cat[]=4&tags=PS2", TorznabCatType.ConsoleOther);
            AddCategoryMapping("cat[]=4", TorznabCatType.PCGames);

            // Miscellaneous
            AddCategoryMapping("cat[]=31&tags=Ebook", TorznabCatType.BooksEbook);
            AddCategoryMapping("cat[]=31&tags=RARFiX", TorznabCatType.Other);
            AddCategoryMapping("cat[]=31", TorznabCatType.Other);

            // Movies
            AddCategoryMapping("cat[]=19&tags=x264", TorznabCatType.Movies);
            AddCategoryMapping("cat[]=19&tags=720p", TorznabCatType.MoviesHD);
            AddCategoryMapping("cat[]=19&tags=XviD", TorznabCatType.MoviesSD);
            AddCategoryMapping("cat[]=19&tags=BluRay", TorznabCatType.MoviesHD);
            AddCategoryMapping("cat[]=19&tags=DVDRiP", TorznabCatType.MoviesSD);
            AddCategoryMapping("cat[]=19&tags=1080p", TorznabCatType.MoviesHD);
            AddCategoryMapping("cat[]=19&tags=DVD", TorznabCatType.MoviesSD);
            AddCategoryMapping("cat[]=19&tags=DVDR", TorznabCatType.MoviesSD);
            AddCategoryMapping("cat[]=19&tags=WMV", TorznabCatType.Movies);
            AddCategoryMapping("cat[]=19&tags=CAM", TorznabCatType.Movies);
            AddCategoryMapping("cat[]=19", TorznabCatType.Movies);

            // Music
            AddCategoryMapping("cat[]=6&tags=MP3", TorznabCatType.AudioMP3);
            AddCategoryMapping("cat[]=6&tags=V2", TorznabCatType.AudioMP3);
            AddCategoryMapping("cat[]=6&tags=FLAC", TorznabCatType.AudioLossless);
            AddCategoryMapping("cat[]=6&tags=320kbps", TorznabCatType.AudioMP3);
            AddCategoryMapping("cat[]=6", TorznabCatType.Audio);

            // TV
            AddCategoryMapping("cat[]=7&tags=x264", TorznabCatType.TVHD);
            AddCategoryMapping("cat[]=7&tags=720p", TorznabCatType.TVHD);
            AddCategoryMapping("cat[]=7&tags=HDTV", TorznabCatType.TVHD);
            AddCategoryMapping("cat[]=7&tags=XviD", TorznabCatType.TVSD);
            AddCategoryMapping("cat[]=7&BluRay", TorznabCatType.TVHD);
            AddCategoryMapping("cat[]=7&tags=DVDRip", TorznabCatType.TVSD);
            AddCategoryMapping("cat[]=7&tags=DVD", TorznabCatType.TVSD);
            AddCategoryMapping("cat[]=7&tags=Documentary", TorznabCatType.TVDocumentary);
            AddCategoryMapping("cat[]=7&tags=PDTV", TorznabCatType.TVSD);
            AddCategoryMapping("cat[]=7&tags=HD-DVD", TorznabCatType.TVSD);
            AddCategoryMapping("cat[]=7", TorznabCatType.TV);

            // XXX
            AddCategoryMapping("cat[]=51&tags=XviD", TorznabCatType.XXXXviD);
            AddCategoryMapping("cat[]=51&tags=DVDRiP", TorznabCatType.XXXDVD);
            AddCategoryMapping("cat[]=51", TorznabCatType.XXX);
/*
            // Unfortunately they are tags not categories so return the results
            // as the parent category so do not get results removed with the filtering.

            AddResultCategoryMapping("cat[]=22&tags=Windows", TorznabCatType.PC);
            AddResultCategoryMapping("cat[]=22&tags=MAC", TorznabCatType.PC);
            AddResultCategoryMapping("cat[]=22&tags=Linux", TorznabCatType.PC);
            AddResultCategoryMapping("cat[]=22&tags=All", TorznabCatType.PC);
            AddResultCategoryMapping("cat[]=22", TorznabCatType.PC);

            AddResultCategoryMapping("cat[]=27&tags=All", TorznabCatType.Books);
            AddResultCategoryMapping("cat[]=27", TorznabCatType.Books);

            AddResultCategoryMapping("cat[]=4&tags=PC", TorznabCatType.PC);
            AddResultCategoryMapping("cat[]=4&tags=RIP", TorznabCatType.PC);
            AddResultCategoryMapping("cat[]=4&tags=ISO", TorznabCatType.PC);
            AddResultCategoryMapping("cat[]=4&tags=XBOX360", TorznabCatType.Console);
            AddResultCategoryMapping("cat[]=4&tags=PS3", TorznabCatType.Console);
            AddResultCategoryMapping("cat[]=4&tags=Wii", TorznabCatType.Console);
            AddResultCategoryMapping("cat[]=4&tags=PSP", TorznabCatType.Console);
            AddResultCategoryMapping("cat[]=4&tags=NSD", TorznabCatType.Console);
            AddResultCategoryMapping("cat[]=4&tags=XBox", TorznabCatType.Console);
            AddResultCategoryMapping("cat[]=4&tags=PS2", TorznabCatType.Console);
            AddResultCategoryMapping("cat[]=4&tags=All", TorznabCatType.Console);
            AddResultCategoryMapping("cat[]=4", TorznabCatType.Console);

            AddResultCategoryMapping("cat[]=31&tags=Ebook", TorznabCatType.Books);
            AddResultCategoryMapping("cat[]=31&tags=RARFiX", TorznabCatType.Other);
            AddResultCategoryMapping("cat[]=31&tags=All", TorznabCatType.Other);
            AddResultCategoryMapping("cat[]=31", TorznabCatType.Other);

            AddResultCategoryMapping("cat[]=19&tags=x264", TorznabCatType.Movies);
            AddResultCategoryMapping("cat[]=19&tags=720p", TorznabCatType.Movies);
            AddResultCategoryMapping("cat[]=19&tags=XviD", TorznabCatType.Movies);
            AddResultCategoryMapping("cat[]=19&tags=BluRay", TorznabCatType.Movies);
            AddResultCategoryMapping("cat[]=19&tags=DVDRiP", TorznabCatType.Movies);
            AddResultCategoryMapping("cat[]=19&tags=1080p", TorznabCatType.Movies);
            AddResultCategoryMapping("cat[]=19&tags=DVD", TorznabCatType.Movies);
            AddResultCategoryMapping("cat[]=19&tags=DVDR", TorznabCatType.Movies);
            AddResultCategoryMapping("cat[]=19&tags=WMV", TorznabCatType.Movies);
            AddResultCategoryMapping("cat[]=19&tags=CAM", TorznabCatType.Movies);
            AddResultCategoryMapping("cat[]=19&tags=All", TorznabCatType.Movies);
            AddResultCategoryMapping("cat[]=19", TorznabCatType.Movies);

            AddResultCategoryMapping("cat[]=6&tags=MP3", TorznabCatType.Audio);
            AddResultCategoryMapping("cat[]=6&tags=V2", TorznabCatType.Audio);
            AddResultCategoryMapping("cat[]=6&tags=FLAC", TorznabCatType.Audio);
            AddResultCategoryMapping("cat[]=6&tags=320kbps", TorznabCatType.Audio);
            AddResultCategoryMapping("cat[]=6&tags=All", TorznabCatType.Audio);
            AddResultCategoryMapping("cat[]=6", TorznabCatType.Audio);

            AddResultCategoryMapping("cat[]=7&tags=x264", TorznabCatType.TV);
            AddResultCategoryMapping("cat[]=7&tags=720p", TorznabCatType.TV);
            AddResultCategoryMapping("cat[]=7&tags=HDTV", TorznabCatType.TV);
            AddResultCategoryMapping("cat[]=7&tags=XviD", TorznabCatType.TV);
            AddResultCategoryMapping("cat[]=7&BluRay", TorznabCatType.TV);
            AddResultCategoryMapping("cat[]=7&tags=DVDRip", TorznabCatType.TV);
            AddResultCategoryMapping("cat[]=7&tags=DVD", TorznabCatType.TV);
            AddResultCategoryMapping("cat[]=7&tags=Documentary", TorznabCatType.TV);
            AddResultCategoryMapping("cat[]=7&tags=PDTV", TorznabCatType.TV);
            AddResultCategoryMapping("cat[]=7&tags=HD-DVD", TorznabCatType.TV);
            AddResultCategoryMapping("cat[]=7&tags=All", TorznabCatType.TV);
            AddResultCategoryMapping("cat[]=7", TorznabCatType.TV);

            AddResultCategoryMapping("cat[]=51&tags=XviD", TorznabCatType.XXX);
            AddResultCategoryMapping("cat[]=51&tags=DVDRiP", TorznabCatType.XXX);
            AddResultCategoryMapping("cat[]=51&tags=All", TorznabCatType.XXX);
            AddResultCategoryMapping("cat[]=51", TorznabCatType.XXX);*/
        }
/*
        protected void AddResultCategoryMapping(string trackerCategory, TorznabCategory newznabCategory)
        {
            resultMapping.Add(new CategoryMapping(trackerCategory, null, newznabCategory.ID));
            if (!TorznabCaps.Categories.Contains(newznabCategory))
                TorznabCaps.Categories.Add(newznabCategory);
        }
*/
        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var loginPage = await RequestStringWithCookies(LoginUrl, string.Empty);

            var pairs = new Dictionary<string, string> {
                { "returnto", "%2F" },
                { "login_pin", configData.Pin.Value },
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "login", "Login" }
            };

            // Send Post
            var result = await PostDataWithCookies(LoginUrl, pairs, loginPage.Cookies);
            if (result.RedirectingTo == null)
                throw new ExceptionWithConfigData("Login failed. Did you use the PIN number that pretome emailed you?", configData);

            // Get result from redirect
            var loginCookies = result.Cookies;
            await FollowIfRedirect(result, LoginUrl, null, loginCookies);

            await ConfigureIfOK(loginCookies, result.Content?.Contains("logout.php") == true,
                                () => throw new ExceptionWithConfigData("Failed", configData));

            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var qc = new List<KeyValuePair<string, string>>(); // NameValueCollection don't support cat[]=19&cat[]=6
            if (query.IsImdbQuery)
            {
                qc.Add("search", query.ImdbID);
                qc.Add("st", "1");
                qc.Add("sd", "1");
            }
            else if (!string.IsNullOrWhiteSpace(query.GetQueryString()))
            {
                qc.Add("search", query.GetQueryString());
                qc.Add("st", "1");
            }

            // parse categories and tags
            var catGroups = new List<string>();
            var tagGroups = new List<string>();
            var cats = MapTorznabCapsToTrackers(query);
            foreach (var cat in cats)
            {
                // "cat[]=7&tags=x264"
                var cSplit = cat.Split('&');

                var gSplit = cSplit[0].Split('=');
                if (gSplit.Length > 1)
                    catGroups.Add(gSplit[1]); // category = 7

                if (cSplit.Length > 1)
                {
                    var tSplit = cSplit[1].Split('=');
                    if (tSplit.Length > 1)
                        tagGroups.Add(tSplit[1]); // tag = x264
                }
            }
            catGroups = catGroups.Distinct().ToList();
            tagGroups = tagGroups.Distinct().ToList();

            // add categories
            foreach (var cat in catGroups)
                qc.Add("cat[]", cat);

            // do not include too many tags as it'll mess with their servers
            if (tagGroups.Count < 7)
            {
                qc.Add("tags", string.Join(",", tagGroups));
                // if tags are specified match any
                // if no tags are specified match all, with any we get random results
                qc.Add("tf", tagGroups.Any() ? "any" : "all");
            }

            var searchUrl = SearchUrl + "?" + qc.GetQueryString();
            var response = await RequestStringWithCookiesAndRetry(searchUrl);

            if (response.IsRedirect) // re-login
            {
                await ApplyConfiguration(null);
                response = await RequestStringWithCookiesAndRetry(searchUrl);
            }

            try
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(response.Content);
                var rows = dom.QuerySelectorAll("table > tbody > tr.browse");
                foreach (var row in rows)
                {
                    var qLink = row.Children[1].QuerySelector("a");
                    var title = qLink.GetAttribute("title");
                    if (qLink.QuerySelectorAll("span").Length == 1 && title.StartsWith("NEW! |"))
                        title = title.Substring(6).Trim();

                    var comments = new Uri(SiteLink + qLink.GetAttribute("href"));
                    var link = new Uri(SiteLink + row.Children[2].QuerySelector("a").GetAttribute("href"));
                    var dateStr = Regex.Replace(row.Children[5].InnerHtml, @"\<br[\s]{0,1}[\/]{0,1}\>", " ");
                    var publishDate = DateTimeUtil.FromTimeAgo(dateStr);
                    var files = ParseUtil.CoerceInt(row.Children[3].TextContent);
                    var size = ReleaseInfo.GetBytes(row.Children[7].TextContent);
                    var grabs = ParseUtil.CoerceInt(row.Children[8].TextContent);
                    var seeders = ParseUtil.CoerceInt(row.Children[9].TextContent);
                    var leechers = ParseUtil.CoerceInt(row.Children[10].TextContent);
                    var cat = row.FirstElementChild.FirstElementChild.GetAttribute("href").Replace("browse.php?", string.Empty);

                    var release = new ReleaseInfo
                    {
                        Title = title,
                        Comments = comments,
                        Guid = comments,
                        Link = link,
                        PublishDate = publishDate,
                        Size = size,
                        Category = MapTrackerCatToNewznab(cat),
                        Files = files,
                        Grabs = grabs,
                        Seeders = seeders,
                        Peers = leechers + seeders,
                        MinimumRatio = 1,
                        MinimumSeedTime = 172800, // 48 hours
                        DownloadVolumeFactor = 0, // ratioless
                        UploadVolumeFactor = 1
                    };

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.Content, ex);
            }
            return releases;
        }
    }
}
