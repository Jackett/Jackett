using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
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

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class GazelleGames : IndexerBase
    {
        public override string Id => "gazellegames";
        public override string Name => "GazelleGames";
        public override string Description => "A gaming tracker.";
        public override string SiteLink { get; protected set; } = "https://gazellegames.net/";
        public override string Language => "en-US";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private string LoginUrl => SiteLink + "login.php";
        private string BrowseUrl => SiteLink + "torrents.php";

        private new ConfigurationDataCookie configData
        {
            get => (ConfigurationDataCookie)base.configData;
            set => base.configData = value;
        }

        public GazelleGames(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataCookie())
        {
            configData.AddDynamic("searchgroupnames", new BoolConfigurationItem("Search Group Names Only") { Value = false });
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities();

            // Apple
            caps.Categories.AddCategoryMapping("Mac", TorznabCatType.ConsoleOther, "Mac");
            caps.Categories.AddCategoryMapping("iOS", TorznabCatType.PCMobileiOS, "iOS");
            caps.Categories.AddCategoryMapping("Apple Bandai Pippin", TorznabCatType.ConsoleOther, "Apple Bandai Pippin");

            // Google
            caps.Categories.AddCategoryMapping("Android", TorznabCatType.PCMobileAndroid, "Android");

            // Microsoft
            caps.Categories.AddCategoryMapping("DOS", TorznabCatType.PCGames, "DOS");
            caps.Categories.AddCategoryMapping("Windows", TorznabCatType.PCGames, "Windows");
            caps.Categories.AddCategoryMapping("Xbox", TorznabCatType.ConsoleXBox, "Xbox");
            caps.Categories.AddCategoryMapping("Xbox 360", TorznabCatType.ConsoleXBox360, "Xbox 360");

            // Nintendo
            caps.Categories.AddCategoryMapping("Game Boy", TorznabCatType.ConsoleOther, "Game Boy");
            caps.Categories.AddCategoryMapping("Game Boy Advance", TorznabCatType.ConsoleOther, "Game Boy Advance");
            caps.Categories.AddCategoryMapping("Game Boy Color", TorznabCatType.ConsoleOther, "Game Boy Color");
            caps.Categories.AddCategoryMapping("NES", TorznabCatType.ConsoleOther, "NES");
            caps.Categories.AddCategoryMapping("Nintendo 64", TorznabCatType.ConsoleOther, "Nintendo 64");
            caps.Categories.AddCategoryMapping("Nintendo 3DS", TorznabCatType.ConsoleOther, "Nintendo 3DS");
            caps.Categories.AddCategoryMapping("New Nintendo 3DS", TorznabCatType.ConsoleOther, "New Nintendo 3DS");
            caps.Categories.AddCategoryMapping("Nintendo DS", TorznabCatType.ConsoleNDS, "Nintendo DS");
            caps.Categories.AddCategoryMapping("Nintendo GameCube", TorznabCatType.ConsoleOther, "Nintendo GameCube");
            caps.Categories.AddCategoryMapping("Pokemon Mini", TorznabCatType.ConsoleOther, "Pokemon Mini");
            caps.Categories.AddCategoryMapping("SNES", TorznabCatType.ConsoleOther, "SNES");
            caps.Categories.AddCategoryMapping("Virtual Boy", TorznabCatType.ConsoleOther, "Virtual Boy");
            caps.Categories.AddCategoryMapping("Wii", TorznabCatType.ConsoleWii, "Wii");
            caps.Categories.AddCategoryMapping("Wii U", TorznabCatType.ConsoleWiiU, "Wii U");

            // Sony
            caps.Categories.AddCategoryMapping("PlayStation 1", TorznabCatType.ConsoleOther, "PlayStation 1");
            caps.Categories.AddCategoryMapping("PlayStation 2", TorznabCatType.ConsoleOther, "PlayStation 2");
            caps.Categories.AddCategoryMapping("PlayStation 3", TorznabCatType.ConsolePS3, "PlayStation 3");
            caps.Categories.AddCategoryMapping("PlayStation 4", TorznabCatType.ConsolePS4, "PlayStation 4");
            caps.Categories.AddCategoryMapping("PlayStation Portable", TorznabCatType.ConsolePSP, "PlayStation Portable");
            caps.Categories.AddCategoryMapping("PlayStation Vita", TorznabCatType.ConsolePSVita, "PlayStation Vita");

            // Sega
            caps.Categories.AddCategoryMapping("Dreamcast", TorznabCatType.ConsoleOther, "Dreamcast");
            caps.Categories.AddCategoryMapping("Game Gear", TorznabCatType.ConsoleOther, "Game Gear");
            caps.Categories.AddCategoryMapping("Master System", TorznabCatType.ConsoleOther, "Master System");
            caps.Categories.AddCategoryMapping("Mega Drive", TorznabCatType.ConsoleOther, "Mega Drive");
            caps.Categories.AddCategoryMapping("Pico", TorznabCatType.ConsoleOther, "Pico");
            caps.Categories.AddCategoryMapping("Saturn", TorznabCatType.ConsoleOther, "Saturn");
            caps.Categories.AddCategoryMapping("SG-1000", TorznabCatType.ConsoleOther, "SG-1000");

            // Atari
            caps.Categories.AddCategoryMapping("Atari 2600", TorznabCatType.ConsoleOther, "Atari 2600");
            caps.Categories.AddCategoryMapping("Atari 5200", TorznabCatType.ConsoleOther, "Atari 5200");
            caps.Categories.AddCategoryMapping("Atari 7800", TorznabCatType.ConsoleOther, "Atari 7800");
            caps.Categories.AddCategoryMapping("Atari Jaguar", TorznabCatType.ConsoleOther, "Atari Jaguar");
            caps.Categories.AddCategoryMapping("Atari Lynx", TorznabCatType.ConsoleOther, "Atari Lynx");
            caps.Categories.AddCategoryMapping("Atari ST", TorznabCatType.ConsoleOther, "Atari ST");

            // Amstrad
            caps.Categories.AddCategoryMapping("Amstrad CPC", TorznabCatType.ConsoleOther, "Amstrad CPC");

            // Sinclair
            caps.Categories.AddCategoryMapping("ZX Spectrum", TorznabCatType.ConsoleOther, "ZX Spectrum");

            // Spectravideo
            caps.Categories.AddCategoryMapping("MSX", TorznabCatType.ConsoleOther, "MSX");
            caps.Categories.AddCategoryMapping("MSX 2", TorznabCatType.ConsoleOther, "MSX 2");

            // Tiger
            caps.Categories.AddCategoryMapping("Game.com", TorznabCatType.ConsoleOther, "Game.com");
            caps.Categories.AddCategoryMapping("Gizmondo", TorznabCatType.ConsoleOther, "Gizmondo");

            // VTech
            caps.Categories.AddCategoryMapping("V.Smile", TorznabCatType.ConsoleOther, "V.Smile");
            caps.Categories.AddCategoryMapping("CreatiVision", TorznabCatType.ConsoleOther, "CreatiVision");

            // Tabletop Games
            caps.Categories.AddCategoryMapping("Board Game", TorznabCatType.ConsoleOther, "Board Game");
            caps.Categories.AddCategoryMapping("Card Game", TorznabCatType.ConsoleOther, "Card Game");
            caps.Categories.AddCategoryMapping("Miniature Wargames", TorznabCatType.ConsoleOther, "Miniature Wargames");
            caps.Categories.AddCategoryMapping("Pen and Paper RPG", TorznabCatType.ConsoleOther, "Pen and Paper RPG");

            // Other
            caps.Categories.AddCategoryMapping("3DO", TorznabCatType.ConsoleOther, "3DO");
            caps.Categories.AddCategoryMapping("Bandai WonderSwan", TorznabCatType.ConsoleOther, "Bandai WonderSwan");
            caps.Categories.AddCategoryMapping("Bandai WonderSwan Color", TorznabCatType.ConsoleOther, "Bandai WonderSwan Color");
            caps.Categories.AddCategoryMapping("Casio Loopy", TorznabCatType.ConsoleOther, "Casio Loopy");
            caps.Categories.AddCategoryMapping("Casio PV-1000", TorznabCatType.ConsoleOther, "Casio PV-1000");
            caps.Categories.AddCategoryMapping("Colecovision", TorznabCatType.ConsoleOther, "Colecovision");
            caps.Categories.AddCategoryMapping("Commodore 64", TorznabCatType.ConsoleOther, "Commodore 64");
            caps.Categories.AddCategoryMapping("Commodore 128", TorznabCatType.ConsoleOther, "Commodore 128");
            caps.Categories.AddCategoryMapping("Commodore Amiga", TorznabCatType.ConsoleOther, "Commodore Amiga");
            caps.Categories.AddCategoryMapping("Commodore Plus-4", TorznabCatType.ConsoleOther, "Commodore Plus-4");
            caps.Categories.AddCategoryMapping("Commodore VIC-20", TorznabCatType.ConsoleOther, "Commodore VIC-20");
            caps.Categories.AddCategoryMapping("Emerson Arcadia 2001", TorznabCatType.ConsoleOther, "Emerson Arcadia 2001");
            caps.Categories.AddCategoryMapping("Entex Adventure Vision", TorznabCatType.ConsoleOther, "Entex Adventure Vision");
            caps.Categories.AddCategoryMapping("Epoch Super Casette Vision", TorznabCatType.ConsoleOther, "Epoch Super Casette Vision");
            caps.Categories.AddCategoryMapping("Fairchild Channel F", TorznabCatType.ConsoleOther, "Fairchild Channel F");
            caps.Categories.AddCategoryMapping("Funtech Super Acan", TorznabCatType.ConsoleOther, "Funtech Super Acan");
            caps.Categories.AddCategoryMapping("GamePark GP32", TorznabCatType.ConsoleOther, "GamePark GP32");
            caps.Categories.AddCategoryMapping("General Computer Vectrex", TorznabCatType.ConsoleOther, "General Computer Vectrex");
            caps.Categories.AddCategoryMapping("Interactive DVD", TorznabCatType.ConsoleOther, "Interactive DVD");
            caps.Categories.AddCategoryMapping("Linux", TorznabCatType.ConsoleOther, "Linux");
            caps.Categories.AddCategoryMapping("Hartung Game Master", TorznabCatType.ConsoleOther, "Hartung Game Master");
            caps.Categories.AddCategoryMapping("Magnavox-Phillips Odyssey", TorznabCatType.ConsoleOther, "Magnavox-Phillips Odyssey");
            caps.Categories.AddCategoryMapping("Mattel Intellivision", TorznabCatType.ConsoleOther, "Mattel Intellivision");
            caps.Categories.AddCategoryMapping("Memotech MTX", TorznabCatType.ConsoleOther, "Memotech MTX");
            caps.Categories.AddCategoryMapping("Miles Gordon Sam Coupe", TorznabCatType.ConsoleOther, "Miles Gordon Sam Coupe");
            caps.Categories.AddCategoryMapping("NEC PC-98", TorznabCatType.ConsoleOther, "NEC PC-98");
            caps.Categories.AddCategoryMapping("NEC PC-FX", TorznabCatType.ConsoleOther, "NEC PC-FX");
            caps.Categories.AddCategoryMapping("NEC SuperGrafx", TorznabCatType.ConsoleOther, "NEC SuperGrafx");
            caps.Categories.AddCategoryMapping("NEC TurboGrafx-16", TorznabCatType.ConsoleOther, "NEC TurboGrafx-16");
            caps.Categories.AddCategoryMapping("Nokia N-Gage", TorznabCatType.ConsoleOther, "Nokia N-Gage");
            caps.Categories.AddCategoryMapping("Ouya", TorznabCatType.ConsoleOther, "Ouya");
            caps.Categories.AddCategoryMapping("Philips Videopac+", TorznabCatType.ConsoleOther, "Philips Videopac+");
            caps.Categories.AddCategoryMapping("Phone/PDA", TorznabCatType.ConsoleOther, "Phone/PDA");
            caps.Categories.AddCategoryMapping("RCA Studio II", TorznabCatType.ConsoleOther, "RCA Studio II");
            caps.Categories.AddCategoryMapping("Sharp X1", TorznabCatType.ConsoleOther, "Sharp X1");
            caps.Categories.AddCategoryMapping("Sharp X68000", TorznabCatType.ConsoleOther, "Sharp X68000");
            caps.Categories.AddCategoryMapping("SNK Neo Geo", TorznabCatType.ConsoleOther, "SNK Neo Geo");
            caps.Categories.AddCategoryMapping("SNK Neo Geo Pocket", TorznabCatType.ConsoleOther, "SNK Neo Geo Pocket");
            caps.Categories.AddCategoryMapping("Taito Type X", TorznabCatType.ConsoleOther, "Taito Type X");
            caps.Categories.AddCategoryMapping("Tandy Color Computer", TorznabCatType.ConsoleOther, "Tandy Color Computer");
            caps.Categories.AddCategoryMapping("Tangerine Oric", TorznabCatType.ConsoleOther, "Tangerine Oric");
            caps.Categories.AddCategoryMapping("Thomson MO5", TorznabCatType.ConsoleOther, "Thomson MO5");
            caps.Categories.AddCategoryMapping("Watara Supervision", TorznabCatType.ConsoleOther, "Watara Supervision");
            caps.Categories.AddCategoryMapping("Retro - Other", TorznabCatType.ConsoleOther, "Retro - Other");

            // special categories (real categories/not platforms)
            caps.Categories.AddCategoryMapping("OST", TorznabCatType.AudioOther, "OST");
            caps.Categories.AddCategoryMapping("Applications", TorznabCatType.PC0day, "Applications");
            caps.Categories.AddCategoryMapping("E-Books", TorznabCatType.BooksEBook, "E-Books");

            return caps;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            // TODO: implement captcha
            CookieHeader = configData.Cookie.Value;
            try
            {
                var results = await PerformQuery(new TorznabQuery());
                if (results.Count() == 0)
                {
                    throw new Exception("Found 0 results in the tracker");
                }

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
            var releases = new List<ReleaseInfo>();
            var searchUrl = BrowseUrl;
            var searchString = query.GetQueryString();

            var searchType = ((BoolConfigurationItem)configData.GetDynamic("searchgroupnames")).Value ? "groupname" : "searchstr";

            var queryCollection = new NameValueCollection
            {
                {searchType, searchString},
                {"order_by", "time"},
                {"order_way", "desc"},
                {"action", "basic"},
                {"searchsubmit", "1"}
            };

            var i = 0;
            foreach (var cat in MapTorznabCapsToTrackers(query))
                queryCollection.Add($"artistcheck[{i++}]", cat);


            searchUrl += "?" + queryCollection.GetQueryString();

            var results = await RequestWithCookiesAsync(searchUrl);
            if (results.IsRedirect && results.RedirectingTo.EndsWith("login.php"))
            {
                throw new Exception("relogin needed, please update your cookie");
            }

            try
            {
                var RowsSelector = ".torrent_table > tbody > tr";

                var SearchResultParser = new HtmlParser();
                var SearchResultDocument = SearchResultParser.ParseDocument(results.ContentString);
                var Rows = SearchResultDocument.QuerySelectorAll(RowsSelector);

                var stickyGroup = false;
                string CategoryStr;
                ICollection<int> GroupCategory = null;
                string GroupTitle = null;
                //Nullable<DateTime> GroupPublishDate = null;

                foreach (var Row in Rows)
                {
                    if (Row.ClassList.Contains("torrent"))
                    {
                        // garbage rows
                        continue;
                    }
                    else if (Row.ClassList.Contains("group"))
                    {
                        stickyGroup = Row.ClassList.Contains("sticky");
                        var dispalyname = Row.QuerySelector("#displayname");
                        var qCat = Row.QuerySelector("td.cats_col > div");
                        CategoryStr = qCat.GetAttribute("title");
                        var qArtistLink = dispalyname.QuerySelector("#groupplatform > a");
                        if (qArtistLink != null)
                            CategoryStr = ParseUtil.GetArgumentFromQueryString(qArtistLink.GetAttribute("href"), "artistname");
                        GroupCategory = MapTrackerCatToNewznab(CategoryStr);

                        var qDetailsLink = dispalyname.QuerySelector("#groupname > a");
                        GroupTitle = qDetailsLink.TextContent;
                    }
                    else if (Row.ClassList.Contains("group_torrent"))
                    {
                        if (Row.QuerySelector("td.edition_info") != null) // ignore edition rows
                            continue;

                        // some users have an extra colum (8), we can't use nth-last-child
                        var sizeString = Row.QuerySelector("td:nth-child(4)").TextContent;
                        if (string.IsNullOrEmpty(sizeString)) // external links, example BlazBlue: Calamity Trigger Manual - Guide [GameDOX - External Link]
                            continue;
                        var qDetailsLink = Row.QuerySelector("a[href^=\"torrents.php?id=\"]");
                        var title = qDetailsLink.TextContent.Replace(", Freeleech!", "").Replace(", Neutral Leech!", "");
                        if (stickyGroup && (query.ImdbID == null || !TorznabCaps.MovieSearchImdbAvailable) && !query.MatchQueryStringAND(title)) // AND match for sticky releases
                            continue;
                        var qDescription = qDetailsLink.QuerySelector("span.torrent_info_tags");
                        var qDLLink = Row.QuerySelector("a[href^=\"torrents.php?action=download\"]");
                        var qTime = Row.QuerySelector("span.time");
                        var qGrabs = Row.QuerySelector("td:nth-child(5)");
                        var qSeeders = Row.QuerySelector("td:nth-child(6)");
                        var qLeechers = Row.QuerySelector("td:nth-child(7)");
                        var qFreeLeech = Row.QuerySelector("strong.freeleech_label");
                        var qNeutralLeech = Row.QuerySelector("strong.neutralleech_label");
                        var Time = qTime.GetAttribute("title");
                        var link = new Uri(SiteLink + qDLLink.GetAttribute("href"));
                        var seeders = ParseUtil.CoerceInt(qSeeders.TextContent);
                        var publishDate = DateTime.SpecifyKind(
                            DateTime.ParseExact(Time, "MMM dd yyyy, HH:mm", CultureInfo.InvariantCulture),
                            DateTimeKind.Unspecified).ToLocalTime();
                        var details = new Uri(SiteLink + qDetailsLink.GetAttribute("href"));
                        var grabs = ParseUtil.CoerceLong(qGrabs.TextContent);
                        var leechers = ParseUtil.CoerceInt(qLeechers.TextContent);
                        var size = ParseUtil.GetBytes(sizeString);

                        var release = new ReleaseInfo
                        {
                            MinimumRatio = 1,
                            MinimumSeedTime = 288000, //80 hours
                            Category = GroupCategory,
                            PublishDate = publishDate,
                            Size = size,
                            Details = details,
                            Link = link,
                            Guid = link,
                            Grabs = grabs,
                            Seeders = seeders,
                            Peers = leechers + seeders,
                            Title = title,
                            Description = qDescription?.TextContent,
                            UploadVolumeFactor = qNeutralLeech is null ? 1 : 0,
                            DownloadVolumeFactor = qFreeLeech != null || qNeutralLeech != null ? 0 : 1
                        };
                        releases.Add(release);
                    }
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.ContentString, ex);
            }

            return releases;
        }
    }
}
