using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jackett.Models;
using Newtonsoft.Json.Linq;
using Jackett.Utils.Clients;
using Jackett.Services;
using NLog;
using Jackett.Utils;
using CsQuery;
using System.Web;
using Jackett.Models.IndexerConfig;
using System.Collections.Specialized;

namespace Jackett.Indexers
{
    public class Pretome : BaseWebIndexer
    {
        private string LoginUrl { get { return SiteLink + "takelogin.php"; } }
        private string LoginReferer { get { return SiteLink + "index.php?cat=1"; } }
        private string SearchUrl { get { return SiteLink + "browse.php"; } }

        private List<CategoryMapping> resultMapping = new List<CategoryMapping>();

        new ConfigurationDataPinNumber configData
        {
            get { return (ConfigurationDataPinNumber)base.configData; }
            set { base.configData = value; }
        }

        public Pretome(IIndexerConfigurationService configService, IWebClient wc, Logger l, IProtectionService ps)
            : base(name: "PreToMe",
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

            AddCategoryMapping("cat[]=22&tags=Windows", TorznabCatType.PC0day);
            AddCategoryMapping("cat[]=22&tags=MAC", TorznabCatType.PCMac);
            AddCategoryMapping("cat[]=22&tags=Linux", TorznabCatType.PC);

            AddCategoryMapping("cat[]=27", TorznabCatType.BooksEbook);

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

            AddCategoryMapping("cat[]=31&tags=Ebook", TorznabCatType.BooksEbook);
            AddCategoryMapping("cat[]=31&tags=RARFiX", TorznabCatType.Other);

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

            AddCategoryMapping("cat[]=6&tags=MP3", TorznabCatType.AudioMP3);
            AddCategoryMapping("cat[]=6&tags=V2", TorznabCatType.AudioMP3);
            AddCategoryMapping("cat[]=6&tags=FLAC", TorznabCatType.AudioLossless);
            AddCategoryMapping("cat[]=6&tags=320kbps", TorznabCatType.AudioMP3);

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


            AddCategoryMapping("cat[]=51&tags=XviD", TorznabCatType.XXXXviD);
            AddCategoryMapping("cat[]=51&tags=DVDRiP", TorznabCatType.XXXDVD);

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
            AddResultCategoryMapping("cat[]=51", TorznabCatType.XXX);
        }

        protected void AddResultCategoryMapping(string trackerCategory, TorznabCategory newznabCategory)
        {
            resultMapping.Add(new CategoryMapping(trackerCategory.ToString(), null, newznabCategory.ID));
            if (!TorznabCaps.Categories.Contains(newznabCategory))
                TorznabCaps.Categories.Add(newznabCategory);
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
            {
                throw new ExceptionWithConfigData("Login failed. Did you use the PIN number that pretome emailed you?", configData);
            }
            var loginCookies = result.Cookies;
            // Get result from redirect
            await FollowIfRedirect(result, LoginUrl, null, loginCookies);

            await ConfigureIfOK(loginCookies, result.Content != null && result.Content.Contains("logout.php"), () =>
            {
                CookieHeader = string.Empty;
                throw new ExceptionWithConfigData("Failed", configData);
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var queryUrl = SearchUrl;
            var queryCollection = new NameValueCollection();
            var cats = MapTorznabCapsToTrackers(query);
            var tags = string.Empty;
            var catGroups = new List<string>();
            foreach (var cat in cats)
            {
                //"cat[]=7&tags=x264"
                var cSplit = cat.Split('&');
                if (cSplit.Length > 0)
                {
                    var gsplit = cSplit[0].Split('=');
                    if (gsplit.Length > 1)
                    {
                        catGroups.Add(gsplit[1]);
                    }
                }

                if (cSplit.Length > 1)
                {
                    var gsplit = cSplit[1].Split('=');
                    if (gsplit.Length > 1)
                    {
                        if (tags != string.Empty)
                            tags += ",";
                        tags += gsplit[1];
                    }
                }
            }

            if (catGroups.Distinct().Count() == 1)
            {
                queryCollection.Add("cat[]", catGroups.First());
            }

            if (!string.IsNullOrWhiteSpace(query.GetQueryString()))
            {
                queryCollection.Add("st", "1");
                queryCollection.Add("search", query.GetQueryString());
            }

            // Do not include too many tags as it'll mess with their servers.
            if (tags.Split(',').Length < 7)
            {
                queryCollection.Add("tags", tags);
                if(!string.IsNullOrWhiteSpace(tags)) {
                    // if tags are specified match any
                    queryCollection.Add("tf", "any");
                }
                else
                { 
                    // if no tags are specified match all, with any we get random results
                    queryCollection.Add("tf", "all");
                }
            }

            if (queryCollection.Count > 0)
            {
                queryUrl += "?" + queryCollection.GetQueryString();
            }

            var response = await RequestStringWithCookiesAndRetry(queryUrl);

            if (response.IsRedirect)
            {
                await ApplyConfiguration(null);
                response = await RequestStringWithCookiesAndRetry(queryUrl);
            }

            try
            {
                CQ dom = response.Content;
                var rows = dom["table > tbody > tr.browse"];
                foreach (var row in rows)
                {
                    CQ qRow = row.Cq();
                    var release = new ReleaseInfo();

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;

                    var qLink = row.ChildElements.ElementAt(1).Cq().Find("a").First();
                    release.Title = qLink.Text().Trim();
                    if (qLink.Find("span").Count() == 1 && release.Title.StartsWith("NEW! |"))
                    {
                        release.Title = release.Title.Substring(6).Trim();
                    }

                    release.Comments = new Uri(SiteLink + qLink.Attr("href"));
                    release.Guid = release.Comments;

                    var qDownload = row.ChildElements.ElementAt(2).Cq().Find("a").First();
                    release.Link = new Uri(SiteLink + qDownload.Attr("href"));

                    var dateStr = row.ChildElements.ElementAt(5).InnerHTML.Replace("<br>", " ").Replace("<br/>", " ");
                    release.PublishDate = DateTimeUtil.FromTimeAgo(dateStr);

                    var sizeStr = row.ChildElements.ElementAt(7).Cq().Text();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(row.ChildElements.ElementAt(9).InnerText);
                    release.Peers = ParseUtil.CoerceInt(row.ChildElements.ElementAt(10).InnerText) + release.Seeders;

                    var cat = row.ChildElements.ElementAt(0).ChildElements.ElementAt(0).GetAttribute("href").Replace("browse.php?", string.Empty);
                    release.Category = MapTrackerCatToNewznab(cat);

                    var files = qRow.Find("td:nth-child(4)").Text();
                    release.Files = ParseUtil.CoerceInt(files);

                    var grabs = qRow.Find("td:nth-child(9)").Text();
                    release.Grabs = ParseUtil.CoerceInt(grabs);

                    release.DownloadVolumeFactor = 0; // ratioless
                    release.UploadVolumeFactor = 1;

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
