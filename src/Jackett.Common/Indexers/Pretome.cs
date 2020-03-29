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

            // Unfortunately most of them are tags not categories and they return the parent category
            // we have to re-add the tags with the parent category so the results are not removed with the filtering

            // Applications
            AddCategoryMappingAndParent("cat[]=22", TorznabCatType.PC);
            AddCategoryMappingAndParent("cat[]=22&tags=Windows", TorznabCatType.PC0day, TorznabCatType.PC);
            AddCategoryMappingAndParent("cat[]=22&tags=MAC", TorznabCatType.PCMac, TorznabCatType.PC);
            AddCategoryMappingAndParent("cat[]=22&tags=Linux", TorznabCatType.PC, TorznabCatType.PC);

            // Ebooks
            AddCategoryMappingAndParent("cat[]=27", TorznabCatType.BooksEbook);

            // Games
            AddCategoryMappingAndParent("cat[]=4", TorznabCatType.Console);
            AddCategoryMappingAndParent("cat[]=4&tags=PC", TorznabCatType.PCGames, TorznabCatType.Console);
            AddCategoryMappingAndParent("cat[]=4&tags=RIP", TorznabCatType.PCGames, TorznabCatType.Console);
            AddCategoryMappingAndParent("cat[]=4&tags=ISO", TorznabCatType.PCGames, TorznabCatType.Console);
            AddCategoryMappingAndParent("cat[]=4&tags=XBOX360", TorznabCatType.ConsoleXbox360, TorznabCatType.Console);
            AddCategoryMappingAndParent("cat[]=4&tags=PS3", TorznabCatType.ConsolePS3, TorznabCatType.Console);
            AddCategoryMappingAndParent("cat[]=4&tags=Wii", TorznabCatType.ConsoleWii, TorznabCatType.Console);
            AddCategoryMappingAndParent("cat[]=4&tags=PSP", TorznabCatType.ConsolePSP, TorznabCatType.Console);
            AddCategoryMappingAndParent("cat[]=4&tags=NSD", TorznabCatType.ConsoleNDS, TorznabCatType.Console);
            AddCategoryMappingAndParent("cat[]=4&tags=XBox", TorznabCatType.ConsoleXbox, TorznabCatType.Console);
            AddCategoryMappingAndParent("cat[]=4&tags=PS2", TorznabCatType.ConsoleOther, TorznabCatType.Console);

            // Miscellaneous
            AddCategoryMappingAndParent("cat[]=31", TorznabCatType.Other);
            AddCategoryMappingAndParent("cat[]=31&tags=Ebook", TorznabCatType.BooksEbook, TorznabCatType.Other);
            AddCategoryMappingAndParent("cat[]=31&tags=RARFiX", TorznabCatType.Other, TorznabCatType.Other);

            // Movies
            AddCategoryMappingAndParent("cat[]=19", TorznabCatType.Movies);
            AddCategoryMappingAndParent("cat[]=19&tags=x264", TorznabCatType.Movies, TorznabCatType.Movies);
            AddCategoryMappingAndParent("cat[]=19&tags=720p", TorznabCatType.MoviesHD, TorznabCatType.Movies);
            AddCategoryMappingAndParent("cat[]=19&tags=XviD", TorznabCatType.MoviesSD, TorznabCatType.Movies);
            AddCategoryMappingAndParent("cat[]=19&tags=BluRay", TorznabCatType.MoviesHD, TorznabCatType.Movies);
            AddCategoryMappingAndParent("cat[]=19&tags=DVDRiP", TorznabCatType.MoviesSD, TorznabCatType.Movies);
            AddCategoryMappingAndParent("cat[]=19&tags=1080p", TorznabCatType.MoviesHD, TorznabCatType.Movies);
            AddCategoryMappingAndParent("cat[]=19&tags=DVD", TorznabCatType.MoviesSD, TorznabCatType.Movies);
            AddCategoryMappingAndParent("cat[]=19&tags=DVDR", TorznabCatType.MoviesSD, TorznabCatType.Movies);
            AddCategoryMappingAndParent("cat[]=19&tags=WMV", TorznabCatType.Movies, TorznabCatType.Movies);
            AddCategoryMappingAndParent("cat[]=19&tags=CAM", TorznabCatType.Movies, TorznabCatType.Movies);

            // Music
            AddCategoryMappingAndParent("cat[]=6", TorznabCatType.Audio);
            AddCategoryMappingAndParent("cat[]=6&tags=MP3", TorznabCatType.AudioMP3, TorznabCatType.Audio);
            AddCategoryMappingAndParent("cat[]=6&tags=V2", TorznabCatType.AudioMP3, TorznabCatType.Audio);
            AddCategoryMappingAndParent("cat[]=6&tags=FLAC", TorznabCatType.AudioLossless, TorznabCatType.Audio);
            AddCategoryMappingAndParent("cat[]=6&tags=320kbps", TorznabCatType.AudioMP3, TorznabCatType.Audio);

            // TV
            AddCategoryMappingAndParent("cat[]=7", TorznabCatType.TV);
            AddCategoryMappingAndParent("cat[]=7&tags=x264", TorznabCatType.TVHD, TorznabCatType.TV);
            AddCategoryMappingAndParent("cat[]=7&tags=720p", TorznabCatType.TVHD, TorznabCatType.TV);
            AddCategoryMappingAndParent("cat[]=7&tags=HDTV", TorznabCatType.TVHD, TorznabCatType.TV);
            AddCategoryMappingAndParent("cat[]=7&tags=XviD", TorznabCatType.TVSD, TorznabCatType.TV);
            AddCategoryMappingAndParent("cat[]=7&BluRay", TorznabCatType.TVHD, TorznabCatType.TV);
            AddCategoryMappingAndParent("cat[]=7&tags=DVDRip", TorznabCatType.TVSD, TorznabCatType.TV);
            AddCategoryMappingAndParent("cat[]=7&tags=DVD", TorznabCatType.TVSD, TorznabCatType.TV);
            AddCategoryMappingAndParent("cat[]=7&tags=Documentary", TorznabCatType.TVDocumentary, TorznabCatType.TV);
            AddCategoryMappingAndParent("cat[]=7&tags=PDTV", TorznabCatType.TVSD, TorznabCatType.TV);
            AddCategoryMappingAndParent("cat[]=7&tags=HD-DVD", TorznabCatType.TVSD, TorznabCatType.TV);

            // XXX
            AddCategoryMappingAndParent("cat[]=51", TorznabCatType.XXX);
            AddCategoryMappingAndParent("cat[]=51&tags=XviD", TorznabCatType.XXXXviD, TorznabCatType.XXX);
            AddCategoryMappingAndParent("cat[]=51&tags=DVDRiP", TorznabCatType.XXXDVD, TorznabCatType.XXX);
        }

        private void AddCategoryMappingAndParent(string trackerCategory, TorznabCategory newznabCategory,
                                                 TorznabCategory parentCategory = null)
        {
            AddCategoryMapping(trackerCategory, newznabCategory);
            if (parentCategory != null && parentCategory.ID != newznabCategory.ID)
                AddCategoryMapping(trackerCategory, parentCategory);
        }

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

                    if (!query.MatchQueryStringAND(title))
                        continue; // we have to skip bad titles due to tags + any word search

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
