using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class PreToMe : IndexerBase
    {
        public override string Id => "pretome";
        public override string Name => "PreToMe";
        public override string Description => "BitTorrent site for High Quality, High Definition (HD) movies and TV Shows";
        public override string SiteLink { get; protected set; } = "https://pretome.info/";
        public override Encoding Encoding => Encoding.GetEncoding("iso-8859-1");
        public override string Language => "en-US";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private string LoginUrl => SiteLink + "takelogin.php";
        private string SearchUrl => SiteLink + "browse.php";
        private new ConfigurationDataPinNumber configData => (ConfigurationDataPinNumber)base.configData;

        public PreToMe(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps, ICacheService cs)
            : base(client: wc,
                   configService: configService,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataPinNumber("For best results, change the 'Torrents per page' setting to 100 in 'Profile => Torrent browse settings'."))
        {
            configData.AddDynamic("Account Inactivity", new DisplayInfoConfigurationItem("Account Inactivity", "Inactive accounts are removed after 28 days"));
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q, MovieSearchParam.ImdbId
                },
                MusicSearchParams = new List<MusicSearchParam>
                {
                    MusicSearchParam.Q
                },
                BookSearchParams = new List<BookSearchParam>
                {
                    BookSearchParam.Q
                }
            };

            // Unfortunately most of them are tags not categories and they return the parent category
            // we have to re-add the tags with the parent category so the results are not removed with the filtering

            // Applications
            caps.Categories.AddCategoryMapping("cat[]=22", TorznabCatType.PC, "Applications");
            caps.Categories.AddCategoryMapping("cat[]=22&tags=Windows", TorznabCatType.PC0day, "Applications/Windows");
            caps.Categories.AddCategoryMapping("cat[]=22&tags=MAC", TorznabCatType.PCMac, "Applications/MAC");
            caps.Categories.AddCategoryMapping("cat[]=22&tags=Linux", TorznabCatType.PC, "Applications/Linux");

            // Ebooks
            caps.Categories.AddCategoryMapping("cat[]=27", TorznabCatType.BooksEBook, "Ebooks");

            // Games
            // NOTE: Console 1000 category is not working well because it contains pc and console results mixed
            caps.Categories.AddCategoryMapping("cat[]=4", TorznabCatType.Console, "Games");
            caps.Categories.AddCategoryMapping("cat[]=4&tags=PC", TorznabCatType.PCGames, "Games/PC");
            caps.Categories.AddCategoryMapping("cat[]=4&tags=RIP", TorznabCatType.PCGames, "Games/RIP");
            caps.Categories.AddCategoryMapping("cat[]=4&tags=ISO", TorznabCatType.PCGames, "Games/ISO");
            caps.Categories.AddCategoryMapping("cat[]=4&tags=NSW", TorznabCatType.ConsoleOther, "Games/NSW");
            caps.Categories.AddCategoryMapping("cat[]=4&tags=GAMES-NSW", TorznabCatType.ConsoleOther, "Games/NSW");
            caps.Categories.AddCategoryMapping("cat[]=4&tags=XBOX360", TorznabCatType.ConsoleXBox360, "Games/XBOX360");
            caps.Categories.AddCategoryMapping("cat[]=4&tags=PS3", TorznabCatType.ConsolePS3, "Games/PS3");
            caps.Categories.AddCategoryMapping("cat[]=4&tags=Wii", TorznabCatType.ConsoleWii, "Games/Wii");
            caps.Categories.AddCategoryMapping("cat[]=4&tags=PSP", TorznabCatType.ConsolePSP, "Games/PSP");
            caps.Categories.AddCategoryMapping("cat[]=4&tags=NDS", TorznabCatType.ConsoleNDS, "Games/NDS");
            caps.Categories.AddCategoryMapping("cat[]=4&tags=Xbox", TorznabCatType.ConsoleXBox, "Games/Xbox");
            caps.Categories.AddCategoryMapping("cat[]=4&tags=PS2", TorznabCatType.ConsoleOther, "Games/PS2");

            // Miscellaneous
            caps.Categories.AddCategoryMapping("cat[]=31", TorznabCatType.Other, "Miscellaneous");
            caps.Categories.AddCategoryMapping("cat[]=31&tags=Ebook", TorznabCatType.BooksEBook, "Miscellaneous/Ebook");
            caps.Categories.AddCategoryMapping("cat[]=31&tags=RARFiX", TorznabCatType.Other, "Miscellaneous/RARFiX");

            // Movies
            caps.Categories.AddCategoryMapping("cat[]=19", TorznabCatType.Movies, "Movies");
            caps.Categories.AddCategoryMapping("cat[]=19&tags=1080p", TorznabCatType.MoviesHD, "Movies/1080p");
            caps.Categories.AddCategoryMapping("cat[]=19&tags=720p", TorznabCatType.MoviesHD, "Movies/720p");
            caps.Categories.AddCategoryMapping("cat[]=19&tags=2160p", TorznabCatType.MoviesUHD, "Movies/2160p");
            caps.Categories.AddCategoryMapping("cat[]=19&tags=x264", TorznabCatType.MoviesHD, "Movies/x264");
            caps.Categories.AddCategoryMapping("cat[]=19&tags=x265", TorznabCatType.MoviesHD, "Movies/x265");
            caps.Categories.AddCategoryMapping("cat[]=19&tags=BluRay", TorznabCatType.MoviesHD, "Movies/BluRay");
            caps.Categories.AddCategoryMapping("cat[]=19&tags=XviD", TorznabCatType.MoviesSD, "Movies/XviD");
            caps.Categories.AddCategoryMapping("cat[]=19&tags=DVDRiP", TorznabCatType.MoviesSD, "Movies/DVDRiP");
            caps.Categories.AddCategoryMapping("cat[]=19&tags=DVD", TorznabCatType.MoviesSD, "Movies/DVD");
            caps.Categories.AddCategoryMapping("cat[]=19&tags=DVDR", TorznabCatType.MoviesSD, "Movies/DVDR");
            caps.Categories.AddCategoryMapping("cat[]=19&tags=WMV", TorznabCatType.Movies, "Movies/WMV");
            caps.Categories.AddCategoryMapping("cat[]=19&tags=CAM", TorznabCatType.Movies, "Movies/CAM");
            caps.Categories.AddCategoryMapping("cat[]=19&tags=DolbyVision", TorznabCatType.Movies, "Movies/DolbyVision");

            // Music
            caps.Categories.AddCategoryMapping("cat[]=6", TorznabCatType.Audio, "Music");
            caps.Categories.AddCategoryMapping("cat[]=6&tags=MP3", TorznabCatType.AudioMP3, "Music/MP3");
            caps.Categories.AddCategoryMapping("cat[]=6&tags=V2", TorznabCatType.AudioMP3, "Music/V2");
            caps.Categories.AddCategoryMapping("cat[]=6&tags=FLAC", TorznabCatType.AudioLossless, "Music/FLAC");
            caps.Categories.AddCategoryMapping("cat[]=6&tags=320kbps", TorznabCatType.AudioMP3, "Music/320kbps");

            // TV
            caps.Categories.AddCategoryMapping("cat[]=7", TorznabCatType.TV, "TV");
            caps.Categories.AddCategoryMapping("cat[]=7&tags=1080p", TorznabCatType.TVHD, "TV/1080p");
            caps.Categories.AddCategoryMapping("cat[]=7&tags=720p", TorznabCatType.TVHD, "TV/720p");
            caps.Categories.AddCategoryMapping("cat[]=7&tags=2160p", TorznabCatType.TVUHD, "TV/2160p");
            caps.Categories.AddCategoryMapping("cat[]=7&tags=x264", TorznabCatType.TVHD, "TV/x264");
            caps.Categories.AddCategoryMapping("cat[]=7&tags=x265", TorznabCatType.TVHD, "TV/x265");
            caps.Categories.AddCategoryMapping("cat[]=7&tags=HDTV", TorznabCatType.TVHD, "TV/HDTV");
            caps.Categories.AddCategoryMapping("cat[]=7&tags=BluRay", TorznabCatType.TVHD, "TV/BluRay");
            caps.Categories.AddCategoryMapping("cat[]=7&tags=Documentary", TorznabCatType.TVDocumentary, "TV/Documentary");
            caps.Categories.AddCategoryMapping("cat[]=7&tags=PDTV", TorznabCatType.TVSD, "TV/PDTV");
            caps.Categories.AddCategoryMapping("cat[]=7&tags=DolbyVision", TorznabCatType.TVUHD, "TV/DolbyVision");
            caps.Categories.AddCategoryMapping("cat[]=7&tags=DVDRiP", TorznabCatType.TVSD, "TV/DVDRiP");
            caps.Categories.AddCategoryMapping("cat[]=7&tags=XviD", TorznabCatType.TVSD, "TV/XviD");
            caps.Categories.AddCategoryMapping("cat[]=7&tags=DVD", TorznabCatType.TVSD, "TV/DVD");
            caps.Categories.AddCategoryMapping("cat[]=7&tags=HD-DVD", TorznabCatType.TVSD, "TV/HD-DVD");

            // XXX
            caps.Categories.AddCategoryMapping("cat[]=51", TorznabCatType.XXX, "XXX");
            caps.Categories.AddCategoryMapping("cat[]=51&tags=1080p", TorznabCatType.XXXx264, "XXX/1080p");
            caps.Categories.AddCategoryMapping("cat[]=51&tags=720p", TorznabCatType.XXXx264, "XXX/720p");
            caps.Categories.AddCategoryMapping("cat[]=51&tags=2160p", TorznabCatType.XXXUHD, "XXX/2160p");
            caps.Categories.AddCategoryMapping("cat[]=51&tags=x264", TorznabCatType.XXXx264, "XXX/x264");
            caps.Categories.AddCategoryMapping("cat[]=51&tags=x265", TorznabCatType.XXXx264, "XXX/x265");
            caps.Categories.AddCategoryMapping("cat[]=51&tags=XviD", TorznabCatType.XXXXviD, "XXX/XviD");
            caps.Categories.AddCategoryMapping("cat[]=51&tags=DVDRiP", TorznabCatType.XXXDVD, "XXX/DVDRiP");

            return caps;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var loginPage = await RequestWithCookiesAsync(LoginUrl, string.Empty);

            var pairs = new Dictionary<string, string> {
                { "returnto", "%2F" },
                { "login_pin", configData.Pin.Value },
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "login", "Login" }
            };

            // Send Post
            var result = await RequestWithCookiesAsync(LoginUrl, loginPage.Cookies, RequestType.POST, data: pairs);
            if (result.RedirectingTo == null)
                throw new ExceptionWithConfigData("Login failed. Did you use the PIN number that PreToMe emailed you?", configData);

            // Get result from redirect
            var loginCookies = result.Cookies;
            result = await FollowIfRedirect(result, LoginUrl, null, loginCookies);

            await ConfigureIfOK(loginCookies, result.ContentString?.Contains("logout.php") == true, () =>
            {
                var loginResultParser = new HtmlParser();
                using var loginResultDocument = loginResultParser.ParseDocument(result.ContentString);
                var errorMessage = loginResultDocument.QuerySelector("table.body_table font[color~=\"red\"]")?.TextContent.Trim();

                throw new ExceptionWithConfigData(errorMessage ?? "Login failed", configData);
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var qc = new List<KeyValuePair<string, string>> // NameValueCollection don't support cat[]=19&cat[]=6
            {
                {"st", "1"} // search in title
            };

            if (query.IsImdbQuery)
            {
                qc.Add("search", query.ImdbID);
                qc.Add("sd", "1"); // search in description
            }
            else
                qc.Add("search", query.GetQueryString());

            // parse categories and tags
            var catGroups = new HashSet<string>(); // HashSet instead of List to avoid duplicates
            var tagGroups = new HashSet<string>();
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
            var response = await RequestWithCookiesAndRetryAsync(searchUrl);

            if (response.IsRedirect) // re-login
            {
                await ApplyConfiguration(null);
                response = await RequestWithCookiesAndRetryAsync(searchUrl);
            }

            try
            {
                var parser = new HtmlParser();
                using var dom = parser.ParseDocument(response.ContentString);

                var rows = dom.QuerySelectorAll("table > tbody > tr.browse");
                foreach (var row in rows)
                {
                    var qDetails = row.QuerySelector("a[href^=\"details.php?id=\"]");
                    var title = qDetails?.GetAttribute("title");

                    if (!query.MatchQueryStringAND(title))
                        continue; // we have to skip bad titles due to tags + any word search

                    var details = new Uri(SiteLink + qDetails.GetAttribute("href"));
                    var link = new Uri(SiteLink + row.QuerySelector("a[href^=\"download.php\"]")?.GetAttribute("href"));

                    var dateStr = Regex.Replace(row.QuerySelector("td:nth-of-type(6)").InnerHtml, @"\<br[\s]{0,1}[\/]{0,1}\>", " ").Trim();
                    var publishDate = DateTimeUtil.FromTimeAgo(dateStr);

                    var seeders = ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(10)")?.TextContent);
                    var leechers = ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(11)")?.TextContent);

                    var cat = row.QuerySelector("td:nth-of-type(1) a[href^=\"browse.php\"]")?.GetAttribute("href")?.Split('?').Last();

                    var release = new ReleaseInfo
                    {
                        Title = title,
                        Details = details,
                        Guid = details,
                        Link = link,
                        PublishDate = publishDate,
                        Size = ParseUtil.GetBytes(row.QuerySelector("td:nth-of-type(8)")?.TextContent),
                        Category = MapTrackerCatToNewznab(cat),
                        Files = ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(4)")?.TextContent),
                        Grabs = ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(9)")?.TextContent),
                        Seeders = seeders,
                        Peers = leechers + seeders,
                        MinimumRatio = 0.75,
                        MinimumSeedTime = 216000, // 60 hours
                        DownloadVolumeFactor = 0, // ratioless
                        UploadVolumeFactor = 1
                    };

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.ContentString, ex);
            }
            return releases;
        }
    }
}
