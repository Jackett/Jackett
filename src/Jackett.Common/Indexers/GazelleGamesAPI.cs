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
        // API Reference: https://gazellegames.net/wiki.php?action=article&id=401
        protected override string APIUrl => SiteLink + "api.php";
        protected override string AuthorizationName => "X-API-Key";
        protected override int ApiKeyLength => 64;
        protected override string FlipOptionalTokenString(string requestLink) => requestLink.Replace("usetoken=1", "");
        public GazelleGamesApi(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "gazellegamesapi",
                   name: "GazelleGames (API)",
                   description: "A gaming tracker",
                   link: "https://gazellegames.net/",
                   caps: new TorznabCapabilities(),
                   configService: configService,
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
            Language = "en-US";
            Type = "private";

            configData.AddDynamic("searchgroupnames", new BoolConfigurationItem("Search Group Names Only") { Value = false });

            // Apple
            AddCategoryMapping("Mac", TorznabCatType.ConsoleOther, "Mac");
            AddCategoryMapping("iOS", TorznabCatType.PCMobileiOS, "iOS");
            AddCategoryMapping("Apple Bandai Pippin", TorznabCatType.ConsoleOther, "Apple Bandai Pippin");

            // Google
            AddCategoryMapping("Android", TorznabCatType.PCMobileAndroid, "Android");

            // Microsoft
            AddCategoryMapping("DOS", TorznabCatType.PCGames, "DOS");
            AddCategoryMapping("Windows", TorznabCatType.PCGames, "Windows");
            AddCategoryMapping("Xbox", TorznabCatType.ConsoleXBox, "Xbox");
            AddCategoryMapping("Xbox 360", TorznabCatType.ConsoleXBox360, "Xbox 360");

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

            // special categories (real categories/not platforms)
            AddCategoryMapping("OST", TorznabCatType.AudioOther, "OST");
            AddCategoryMapping("Applications", TorznabCatType.PC0day, "Applications");
            AddCategoryMapping("E-Books", TorznabCatType.BooksEBook, "E-Books");
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
