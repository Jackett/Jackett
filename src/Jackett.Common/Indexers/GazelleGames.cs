using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Parser.Html;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    public class GazelleGames : BaseWebIndexer
    {
        private string LoginUrl { get { return SiteLink + "login.php"; } }
        private string BrowseUrl { get { return SiteLink + "torrents.php"; } }

        private new ConfigurationDataCookie configData
        {
            get { return (ConfigurationDataCookie)base.configData; }
            set { base.configData = value; }
        }

        public GazelleGames(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(name: "GazelleGames",
                   description: "A gaming tracker.",
                   link: "https://gazellegames.net/",
                   caps: new TorznabCapabilities(),
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   configData: new ConfigurationDataCookie())
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";

            // Apple
            AddCategoryMapping("Mac", TorznabCatType.ConsoleOther, "Mac");
            AddCategoryMapping("iOS", TorznabCatType.PCPhoneIOS, "iOS");
            AddCategoryMapping("Apple Bandai Pippin", TorznabCatType.ConsoleOther, "Apple Bandai Pippin");

            // Google
            AddCategoryMapping("Android", TorznabCatType.PCPhoneAndroid, "Android");

            // Microsoft
            AddCategoryMapping("DOS", TorznabCatType.PCGames, "DOS");
            AddCategoryMapping("Windows", TorznabCatType.PCGames, "Windows");
            AddCategoryMapping("Xbox", TorznabCatType.ConsoleXbox, "Xbox");
            AddCategoryMapping("Xbox 360", TorznabCatType.ConsoleXbox360, "Xbox 360");

            // Nintendo
            AddCategoryMapping("Game Boy", TorznabCatType.ConsoleOther, "Game Boy");
            AddCategoryMapping("Game Boy Advance", TorznabCatType.ConsoleOther, "Game Boy Advance");
            AddCategoryMapping("Game Boy Color", TorznabCatType.ConsoleOther, "Game Boy Color");
            AddCategoryMapping("NES", TorznabCatType.ConsoleOther, "NES");
            AddCategoryMapping("Nintendo 64", TorznabCatType.ConsoleOther, "Nintendo 64");
            AddCategoryMapping("Nintendo 3DS", TorznabCatType.ConsoleOther, "Nintendo 3DS");
            AddCategoryMapping("New Nintendo 3DS", TorznabCatType.ConsoleOther, "New Nintendo 3DS");
            AddCategoryMapping("Nintendo DS", TorznabCatType.ConsoleNDS, "Nintendo DS");
            AddCategoryMapping("Nintendo GameCube", TorznabCatType.ConsoleOther, "Nintendo GameCube");
            AddCategoryMapping("Pokemon Mini", TorznabCatType.ConsoleOther, "Pokemon Mini");
            AddCategoryMapping("SNES", TorznabCatType.ConsoleOther, "SNES");
            AddCategoryMapping("Virtual Boy", TorznabCatType.ConsoleOther, "Virtual Boy");
            AddCategoryMapping("Wii", TorznabCatType.ConsoleWii, "Wii");
            AddCategoryMapping("Wii U", TorznabCatType.ConsoleWiiU, "Wii U");

            // Sony
            AddCategoryMapping("PlayStation 1", TorznabCatType.ConsoleOther, "PlayStation 1");
            AddCategoryMapping("PlayStation 2", TorznabCatType.ConsoleOther, "PlayStation 2");
            AddCategoryMapping("PlayStation 3", TorznabCatType.ConsolePS3, "PlayStation 3");
            AddCategoryMapping("PlayStation 4", TorznabCatType.ConsolePS4, "PlayStation 4");
            AddCategoryMapping("PlayStation Portable", TorznabCatType.ConsolePSP, "PlayStation Portable");
            AddCategoryMapping("PlayStation Vita", TorznabCatType.ConsolePSVita, "PlayStation Vita");

            // Sega
            AddCategoryMapping("Dreamcast", TorznabCatType.ConsoleOther, "Dreamcast");
            AddCategoryMapping("Game Gear", TorznabCatType.ConsoleOther, "Game Gear");
            AddCategoryMapping("Master System", TorznabCatType.ConsoleOther, "Master System");
            AddCategoryMapping("Mega Drive", TorznabCatType.ConsoleOther, "Mega Drive");
            AddCategoryMapping("Pico", TorznabCatType.ConsoleOther, "Pico");
            AddCategoryMapping("Saturn", TorznabCatType.ConsoleOther, "Saturn");
            AddCategoryMapping("SG-1000", TorznabCatType.ConsoleOther, "SG-1000");

            // Atari
            AddCategoryMapping("Atari 2600", TorznabCatType.ConsoleOther, "Atari 2600");
            AddCategoryMapping("Atari 5200", TorznabCatType.ConsoleOther, "Atari 5200");
            AddCategoryMapping("Atari 7800", TorznabCatType.ConsoleOther, "Atari 7800");
            AddCategoryMapping("Atari Jaguar", TorznabCatType.ConsoleOther, "Atari Jaguar");
            AddCategoryMapping("Atari Lynx", TorznabCatType.ConsoleOther, "Atari Lynx");
            AddCategoryMapping("Atari ST", TorznabCatType.ConsoleOther, "Atari ST");

            // Amstrad
            AddCategoryMapping("Amstrad CPC", TorznabCatType.ConsoleOther, "Amstrad CPC");

            // Sinclair
            AddCategoryMapping("ZX Spectrum", TorznabCatType.ConsoleOther, "ZX Spectrum");

            // Spectravideo
            AddCategoryMapping("MSX", TorznabCatType.ConsoleOther, "MSX");
            AddCategoryMapping("MSX 2", TorznabCatType.ConsoleOther, "MSX 2");

            // Tiger
            AddCategoryMapping("Game.com", TorznabCatType.ConsoleOther, "Game.com");
            AddCategoryMapping("Gizmondo", TorznabCatType.ConsoleOther, "Gizmondo");

            // VTech
            AddCategoryMapping("V.Smile", TorznabCatType.ConsoleOther, "V.Smile");
            AddCategoryMapping("CreatiVision", TorznabCatType.ConsoleOther, "CreatiVision");

            // Tabletop Games
            AddCategoryMapping("Board Game", TorznabCatType.ConsoleOther, "Board Game");
            AddCategoryMapping("Card Game", TorznabCatType.ConsoleOther, "Card Game");
            AddCategoryMapping("Miniature Wargames", TorznabCatType.ConsoleOther, "Miniature Wargames");
            AddCategoryMapping("Pen and Paper RPG", TorznabCatType.ConsoleOther, "Pen and Paper RPG");

            // Other
            AddCategoryMapping("3DO", TorznabCatType.ConsoleOther, "3DO");
            AddCategoryMapping("Bandai WonderSwan", TorznabCatType.ConsoleOther, "Bandai WonderSwan");
            AddCategoryMapping("Bandai WonderSwan Color", TorznabCatType.ConsoleOther, "Bandai WonderSwan Color");
            AddCategoryMapping("Casio Loopy", TorznabCatType.ConsoleOther, "Casio Loopy");
            AddCategoryMapping("Casio PV-1000", TorznabCatType.ConsoleOther, "Casio PV-1000");
            AddCategoryMapping("Colecovision", TorznabCatType.ConsoleOther, "Colecovision");
            AddCategoryMapping("Commodore 64", TorznabCatType.ConsoleOther, "Commodore 64");
            AddCategoryMapping("Commodore 128", TorznabCatType.ConsoleOther, "Commodore 128");
            AddCategoryMapping("Commodore Amiga", TorznabCatType.ConsoleOther, "Commodore Amiga");
            AddCategoryMapping("Commodore Plus-4", TorznabCatType.ConsoleOther, "Commodore Plus-4");
            AddCategoryMapping("Commodore VIC-20", TorznabCatType.ConsoleOther, "Commodore VIC-20");
            AddCategoryMapping("Emerson Arcadia 2001", TorznabCatType.ConsoleOther, "Emerson Arcadia 2001");
            AddCategoryMapping("Entex Adventure Vision", TorznabCatType.ConsoleOther, "Entex Adventure Vision");
            AddCategoryMapping("Epoch Super Casette Vision", TorznabCatType.ConsoleOther, "Epoch Super Casette Vision");
            AddCategoryMapping("Fairchild Channel F", TorznabCatType.ConsoleOther, "Fairchild Channel F");
            AddCategoryMapping("Funtech Super Acan", TorznabCatType.ConsoleOther, "Funtech Super Acan");
            AddCategoryMapping("GamePark GP32", TorznabCatType.ConsoleOther, "GamePark GP32");
            AddCategoryMapping("General Computer Vectrex", TorznabCatType.ConsoleOther, "General Computer Vectrex");
            AddCategoryMapping("Interactive DVD", TorznabCatType.ConsoleOther, "Interactive DVD");
            AddCategoryMapping("Linux", TorznabCatType.ConsoleOther, "Linux");
            AddCategoryMapping("Hartung Game Master", TorznabCatType.ConsoleOther, "Hartung Game Master");
            AddCategoryMapping("Magnavox-Phillips Odyssey", TorznabCatType.ConsoleOther, "Magnavox-Phillips Odyssey");
            AddCategoryMapping("Mattel Intellivision", TorznabCatType.ConsoleOther, "Mattel Intellivision");
            AddCategoryMapping("Memotech MTX", TorznabCatType.ConsoleOther, "Memotech MTX");
            AddCategoryMapping("Miles Gordon Sam Coupe", TorznabCatType.ConsoleOther, "Miles Gordon Sam Coupe");
            AddCategoryMapping("NEC PC-98", TorznabCatType.ConsoleOther, "NEC PC-98");
            AddCategoryMapping("NEC PC-FX", TorznabCatType.ConsoleOther, "NEC PC-FX");
            AddCategoryMapping("NEC SuperGrafx", TorznabCatType.ConsoleOther, "NEC SuperGrafx");
            AddCategoryMapping("NEC TurboGrafx-16", TorznabCatType.ConsoleOther, "NEC TurboGrafx-16");
            AddCategoryMapping("Nokia N-Gage", TorznabCatType.ConsoleOther, "Nokia N-Gage");
            AddCategoryMapping("Ouya", TorznabCatType.ConsoleOther, "Ouya");
            AddCategoryMapping("Philips Videopac+", TorznabCatType.ConsoleOther, "Philips Videopac+");
            AddCategoryMapping("Phone/PDA", TorznabCatType.ConsoleOther, "Phone/PDA");
            AddCategoryMapping("RCA Studio II", TorznabCatType.ConsoleOther, "RCA Studio II");
            AddCategoryMapping("Sharp X1", TorznabCatType.ConsoleOther, "Sharp X1");
            AddCategoryMapping("Sharp X68000", TorznabCatType.ConsoleOther, "Sharp X68000");
            AddCategoryMapping("SNK Neo Geo", TorznabCatType.ConsoleOther, "SNK Neo Geo");
            AddCategoryMapping("SNK Neo Geo Pocket", TorznabCatType.ConsoleOther, "SNK Neo Geo Pocket");
            AddCategoryMapping("Taito Type X", TorznabCatType.ConsoleOther, "Taito Type X");
            AddCategoryMapping("Tandy Color Computer", TorznabCatType.ConsoleOther, "Tandy Color Computer");
            AddCategoryMapping("Tangerine Oric", TorznabCatType.ConsoleOther, "Tangerine Oric");
            AddCategoryMapping("Thomson MO5", TorznabCatType.ConsoleOther, "Thomson MO5");
            AddCategoryMapping("Watara Supervision", TorznabCatType.ConsoleOther, "Watara Supervision");
            AddCategoryMapping("Retro - Other", TorznabCatType.ConsoleOther, "Retro - Other");

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
                    throw new Exception("Your cookie did not work");
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

            var queryCollection = new NameValueCollection
            {
                {"searchstr", searchString},
                {"order_by", "time"},
                {"order_way", "desc"},
                {"action", "basic"},
                {"searchsubmit", "1"}
            };

            var i = 0;
            foreach (var cat in MapTorznabCapsToTrackers(query))
            {
                queryCollection.Add("artistcheck["+i+"]", cat);
                i++;
            }

            searchUrl += "?" + queryCollection.GetQueryString();

            var results = await RequestStringWithCookies(searchUrl);
            try
            {
                string RowsSelector = ".torrent_table > tbody > tr";

                var SearchResultParser = new HtmlParser();
                var SearchResultDocument = SearchResultParser.Parse(results.Content);
                var Rows = SearchResultDocument.QuerySelectorAll(RowsSelector);

                bool stickyGroup = false;
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

                        var release = new ReleaseInfo();

                        release.MinimumRatio = 1;
                        release.MinimumSeedTime = 80 * 3600;

                        var qDetailsLink = Row.QuerySelector("a[href^=\"torrents.php?id=\"]");
                        var qDescription = qDetailsLink.QuerySelector("span.torrent_info_tags");
                        var qDLLink = Row.QuerySelector("a[href^=\"torrents.php?action=download\"]");
                        var qTime = Row.QuerySelector("span.time");
                        // some users have an extra colum (8), we can't use nth-last-child
                        var qSize = Row.QuerySelector("td:nth-child(4)"); 
                        var qGrabs = Row.QuerySelector("td:nth-child(5)");
                        var qSeeders = Row.QuerySelector("td:nth-child(6)");
                        var qLeechers = Row.QuerySelector("td:nth-child(7)");
                        var qFreeLeech = Row.QuerySelector("strong.freeleech_label");
                        var qNeutralLeech = Row.QuerySelector("strong.neutralleech_label");
                        var RowTitle = Row.GetAttribute("title");
                        
                        var Time = qTime.GetAttribute("title");
                        release.PublishDate = DateTime.SpecifyKind(DateTime.ParseExact(Time, "MMM dd yyyy, HH:mm", CultureInfo.InvariantCulture), DateTimeKind.Unspecified).ToLocalTime();
                        release.Category = GroupCategory;

                        if (qDescription != null)
                            release.Description = qDescription.TextContent;

                        release.Title = qDetailsLink.TextContent;
                        release.Title = release.Title.Replace(", Freeleech!", "");
                        release.Title = release.Title.Replace(", Neutral Leech!", "");

                        if (stickyGroup) // AND match for sticky releases
                            if ((query.ImdbID == null || !TorznabCaps.SupportsImdbSearch) && !query.MatchQueryStringAND(release.Title))
                                continue;

                        var Size = qSize.TextContent;
                        if (string.IsNullOrEmpty(Size)) // external links, example BlazBlue: Calamity Trigger Manual - Guide [GameDOX - External Link]
                            continue;
                        release.Size = ReleaseInfo.GetBytes(Size);

                        release.Link = new Uri(SiteLink + qDLLink.GetAttribute("href"));
                        release.Comments = new Uri(SiteLink + qDetailsLink.GetAttribute("href"));
                        release.Guid = release.Link;

                        release.Grabs = ParseUtil.CoerceLong(qGrabs.TextContent);
                        release.Seeders = ParseUtil.CoerceInt(qSeeders.TextContent);
                        release.Peers = ParseUtil.CoerceInt(qLeechers.TextContent) + release.Seeders;

                        if (qFreeLeech != null)
                        {
                            release.UploadVolumeFactor = 1;
                            release.DownloadVolumeFactor = 0;
                        }
                        else if (qNeutralLeech != null)
                        {
                            release.UploadVolumeFactor = 0;
                            release.DownloadVolumeFactor = 0;
                        }
                        else
                        {
                            release.UploadVolumeFactor = 1;
                            release.DownloadVolumeFactor = 1;
                        }

                        releases.Add(release);
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
