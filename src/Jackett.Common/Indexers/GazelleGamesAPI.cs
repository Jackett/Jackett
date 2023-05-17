using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using Jackett.Common.Indexers.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class GazelleGamesApi : GazelleTracker
    {
        public override string Id => "gazellegamesapi";
        public override string Name => "GazelleGames (API)";
        public override string Description => "A gaming tracker";
        public override string SiteLink { get; protected set; } = "https://gazellegames.net/";
        public override string Language => "en-US";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        // API Reference: https://gazellegames.net/wiki.php?action=article&id=401
        protected override string APIUrl => SiteLink + "api.php";
        protected override string AuthorizationName => "X-API-Key";
        protected override int ApiKeyLength => 64;
        protected override string FlipOptionalTokenString(string requestLink) => requestLink.Replace("&usetoken=1", "");
        public GazelleGamesApi(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cs: cs,
                   supportsFreeleechTokens: true,
                   has2Fa: false,
                   useApiKey: true,
                   usePassKey: true,
                   useAuthKey: true,
                   instructionMessageOptional: "<ol><li>Go to GGn's site and open your account settings.</li><li>Under <b>Access Settings</b> click on 'Create a new token'</li><li>Give it a name you like and click <b>Generate</b>.</li><li>Copy the generated API Key and paste it in the above text field.</li></ol>")
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

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = GetSearchTerm(query);

            var searchUrl = APIUrl;
            var queryCollection = new NameValueCollection
            {
                { "request", "search" },
                { "search_type", "torrents" },
                //{"group_results", "0"}, # results won't include all information
                { "order_by", "time" },
                { "order_way", "desc" }
            };

            if (!string.IsNullOrWhiteSpace(searchString))
                queryCollection.Add("searchstr", searchString);

            var i = 0;
            foreach (var cat in MapTorznabCapsToTrackers(query))
                queryCollection.Add($"artistcheck[{i++}]", cat);

            // remove . as not used in titles
            searchUrl += "?" + queryCollection.GetQueryString().Replace(".", " ");

            var apiKey = ((ConfigurationDataGazelleTracker)configData).ApiKey;
            var headers = apiKey != null ? new Dictionary<string, string> { [AuthorizationName] = String.Format(AuthorizationFormat, apiKey.Value) } : null;

            var response = await RequestWithCookiesAndRetryAsync(searchUrl, headers: headers);
            // we get a redirect in html pages and an error message in json response (api)
            if (response.IsRedirect && !useApiKey)
            {
                // re-login only if API key is not in use.
                await ApplyConfiguration(null);
                response = await RequestWithCookiesAndRetryAsync(searchUrl);
            }
            else if (response.ContentString != null && response.ContentString.Contains("failure") && useApiKey)
            {
                // reason for failure should be explained.
                var jsonError = JObject.Parse(response.ContentString);
                var errorReason = (string)jsonError["error"];
                throw new Exception(errorReason);
            }


            try
            {
                var json = JObject.Parse(response.ContentString);
                foreach (var gObj in JObject.FromObject(json["response"]))
                {
                    var group = gObj.Value as JObject;

                    foreach (var tObj in JObject.FromObject(group["Torrents"]))
                    {
                        var torrent = tObj.Value as JObject;
                        var torrentId = torrent["ID"].ToString();

                        var Category = "Windows";
                        if (((JArray)group["Artists"]).Count > 0)
                            Category = group["Artists"][0]["name"].ToString();
                        var GroupCategory = MapTrackerCatToNewznab(Category);

                        var publishDate = DateTime.SpecifyKind(
                            DateTime.ParseExact(torrent["Time"].ToString(), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                            DateTimeKind.Unspecified).ToLocalTime();

                        var size = ParseUtil.CoerceLong(torrent["Size"].ToString());
                        var details = new Uri(DetailsUrl + torrentId);
                        var link = new Uri(DownloadUrl + torrentId);
                        var grabs = ParseUtil.CoerceLong(torrent["Snatched"].ToString());
                        var seeders = ParseUtil.CoerceLong(torrent["Seeders"].ToString());
                        var leechers = ParseUtil.CoerceLong(torrent["Leechers"].ToString());
                        var title = WebUtility.HtmlDecode(torrent["ReleaseTitle"].ToString());

                        List<string> tags = new List<string>();
                        string[] tagNames = { "Format", "Encoding", "Region", "Language", "Scene", "Miscellaneous", "GameDOXType", "GameDOXVers" };
                        foreach (var tag in tagNames)
                        {
                            string tagValue;
                            if (tag.Equals("Scene"))
                                tagValue = torrent[tag].ToString().Equals("1") ? "Scene" : "";
                            else
                                tagValue = torrent[tag].ToString();

                            if (!string.IsNullOrEmpty(tagValue))
                                tags.Add(tagValue);
                        }

                        if (tags.Count > 0)
                            title += " [" + string.Join(", ", tags) + "]";

                        var freeTorrent = ParseUtil.CoerceInt(torrent["FreeTorrent"].ToString());
                        var files = ParseUtil.CoerceInt(torrent["FileCount"].ToString());

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
                            Files = files,
                            UploadVolumeFactor = freeTorrent >= 2 ? 0 : 1,
                            DownloadVolumeFactor = freeTorrent >= 1 ? 0 : 1
                        };
                        releases.Add(release);
                    }
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
