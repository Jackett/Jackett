using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Jackett.Common.Extensions;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using NLog.Fluent;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class YggTorrent : IndexerBase
    {
        public override string Id => "yggtorrent";
        public override string Name => "YggTorrent";

        public override string Description =>
            "YggTorrent (YGG) is a FRENCH Private Torrent Tracker for MOVIES / TV / GENERAL";

        public override string SiteLink { get; protected set; } = "https://www.yggtorrent.org/";

        public override string[] LegacySiteLinks => new[]
        {
            "https://www2.yggtorrent.si/",
            "https://www.yggtorrent.li/",
            "https://www4.yggtorrent.li/",
            "https://www3.yggtorrent.nz/",
            "https://www3.yggtorrent.re/",
            "https://www3.yggtorrent.la/",
            "https://www5.yggtorrent.la/",
            "https://www5.yggtorrent.fi/",
            "https://yggtorrent.lol/",
            "https://www6.yggtorrent.lol/",
            "https://www3.yggtorrent.do/",
            "https://www3.yggtorrent.wtf/",
            "https://www3.yggtorrent.qa/",
            "https://www3.yggtorrent.cool/",
            "https://www.ygg.re/"
        };

        public override string Language => "fr-FR";
        public override string Type => "private";

        public override bool SupportsPagination => true;

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private static readonly Regex _idRegex = new Regex(@"/(\d+)-", RegexOptions.Compiled);

        private static readonly Regex _multiReplaceRegex = new Regex(
            @"\b(MULTI(?!.*(?:FRENCH|ENGLISH|VOSTFR)))\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _vostfrRegex = new Regex(
            @"\b(vostfr|subfrench)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _separatorsRegex = new Regex(@"[\\\-\./!\s]+", RegexOptions.Compiled);

        private static readonly Regex _stripSeasonRegex = new Regex(
            @"\b(S\d{1,3})\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _quoteWordsRegex = new Regex(@"([^\s]+)", RegexOptions.Compiled);

        private static readonly Regex _saisonToSxxEyy1 = new Regex(
            @"(?i)\b(Saisons?[\s\.]*)(\d{4}(?:[\s\.\-aà]+\d{4})?)([\s\.]*[EÉ]pisodes?[\s\.]*)(\d{1,3}(?:[\s\.\-aà]+\d{1,3})?)\b",
            RegexOptions.Compiled);

        private static readonly Regex _saisonToSxxEyy2 = new Regex(
            @"(?i)\bSaisons?[\s\.]*(\d{1,3}(?:[\s\.\-aà]+\d{1,3})?)[\s\.]*[EÉ]pisodes?[\s\.]*(\d{1,3}(?:[\s\.\-aà]+\d{1,3})?)\b",
            RegexOptions.Compiled);

        private static readonly Regex _saisonToSxx1 = new Regex(
            @"(?i)\b(Saisons?[\s\.]*)(\d{4}(?:[\s\.\-aà]+\d{4})?)\b", RegexOptions.Compiled);

        private static readonly Regex _saisonToSxx2 = new Regex(
            @"(?i)\bSaisons?[\s\.]*(\d{1,3}(?:[\s\.\-aà]+\d{1,3})?)\b", RegexOptions.Compiled);

        private static readonly Regex _episodeToEyy1 = new Regex(
            @"(?i)\b([EÉ]pisodes?[\s\.]*)(\d{4}(?:[\s\.\-aà]+\d{4})?)\b", RegexOptions.Compiled);

        private static readonly Regex _episodeToEyy2 = new Regex(
            @"(?i)\b[EÉ]pisodes?[\s\.]*(\d{1,3}(?:[\s\.\-aà]+\d{1,3})?)\b", RegexOptions.Compiled);

        private static readonly Regex _range4Digits = new Regex(
            @"(?i)\b(S?\d*[SE])(\d{4})([\s\.\-aà]+)(\d{4})\b", RegexOptions.Compiled);

        private static readonly Regex _range1To3Digits = new Regex(
            @"(?i)\b(S?\d*[SE])(\d{1,3})[\s\.\-aà]+(\d{1,3})\b", RegexOptions.Compiled);

        private static readonly Regex _frenchDateToIso = new Regex(
            @"\b(\d{2})[\-_\.](\d{2})[\-_\.](\d{4})\b", RegexOptions.Compiled);

        private static readonly Regex _moveYearRegex = new Regex(
            @"(?i)^(?:(.+?)((?:[\.\-\s_\[]+(?:imax|(?:dvd|bd|tv)(?:rip|scr)|bluray(?:\-?rip)?|720\s*p?|1080\s*p?|vof?|vost(?:fr)?|multi|vf(?:f|q)?[1-3]?|(?:true)?french|eng?)[\.\-\s_\]]*)*)([\(\[]?(?:20|1[7-9])\d{2}[\)\]]?)(.*)$|(.*))$",
            RegexOptions.Compiled);

        private static readonly Regex _removeExtRegex = new Regex(
            @"(?i)(.\b(mkv|avi|divx|xvid|mp4)\b)$", RegexOptions.Compiled);

        private static readonly Regex _normalizeSpacesRegex = new Regex(@"\s+", RegexOptions.Compiled);

        private const string CfgUsername = "username";
        private const string CfgPassword = "password";
        private const string CfgMultiLang = "multilang";
        private const string CfgMultiLanguageValue = "multilanguage";
        private const string CfgVostfr = "vostfr";
        private const string CfgFilterTitle = "filter_title";
        private const string CfgStripSeason = "strip_season";
        private const string CfgEnhancedAnime = "enhancedAnime";
        private const string CfgEnhancedAnime4 = "enhancedAnime4";
        private const string CfgSort = "sort";
        private const string CfgOrder = "type";

        private new ConfigurationData configData => base.configData;

        public YggTorrent(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
                          ICacheService cs) : base(
            configService: configService, client: wc, logger: l, p: ps, cacheService: cs,
            configData: new ConfigurationData())
        {
            configData.AddDynamic(CfgUsername, new StringConfigurationItem("Username") { Value = "" });
            configData.AddDynamic(CfgPassword, new PasswordConfigurationItem("Password") { Value = "" });
            configData.AddDynamic(
                CfgMultiLang,
                new BoolConfigurationItem("Replace MULTi by another language in release name") { Value = false });
            configData.AddDynamic(
                CfgMultiLanguageValue,
                new SingleSelectConfigurationItem(
                    "Replace MULTi by this language",
                    new Dictionary<string, string>
                    {
                        ["FRENCH"] = "FRENCH",
                        ["MULTi.FRENCH"] = "MULTi.FRENCH",
                        ["ENGLISH"] = "ENGLISH",
                        ["MULTi.ENGLISH"] = "MULTi.ENGLISH",
                        ["VOSTFR"] = "VOSTFR",
                        ["MULTi.VOSTFR"] = "MULTi.VOSTFR"
                    }) { Value = "FRENCH" });
            configData.AddDynamic(
                CfgVostfr, new BoolConfigurationItem("Replace VOSTFR and SUBFRENCH with ENGLISH") { Value = false });
            configData.AddDynamic(
                CfgFilterTitle,
                new BoolConfigurationItem("Normalize release names by moving year after the title") { Value = false });
            configData.AddDynamic(
                CfgStripSeason,
                new BoolConfigurationItem(
                    "Strip season only (e.g. S01) from searches, as tracker does not support partial matches")
                    {
                        Value = true
                    });
            configData.AddDynamic(
                CfgEnhancedAnime,
                new BoolConfigurationItem("Enhance Sonarr compatibility with anime by renaming episodes (xxx > Exxx)")
                {
                    Value = false
                });
            configData.AddDynamic(
                CfgEnhancedAnime4,
                new BoolConfigurationItem(
                    "Extend the Sonarr compatibility with anime up to 4 digits (WILL break titles containing years)")
                    {
                        Value = false
                    });
            configData.AddDynamic(
                CfgSort,
                new SingleSelectConfigurationItem(
                    "Sort requested from site",
                    new Dictionary<string, string>
                    {
                        ["publish_date"] = "created", ["seed"] = "seeders", ["size"] = "size", ["name"] = "title"
                    }) { Value = "publish_date" });
            configData.AddDynamic(
                CfgOrder,
                new SingleSelectConfigurationItem(
                    "Order requested from site", new Dictionary<string, string> { ["desc"] = "desc", ["asc"] = "asc" })
                    {
                        Value = "desc"
                    });
            configData.AddDynamic(
                "categories_info",
                new DisplayInfoConfigurationItem(
                    "Categories",
                    "To avoid unnecessary additional requests, it's recommended to only use indexer-specific categories (>=100000) when configuring this indexer in Sonarr/Radarr/Lidarr."));
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                TvSearchParams = new List<TvSearchParam> { TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep },
                MovieSearchParams = new List<MovieSearchParam> { MovieSearchParam.Q },
                MusicSearchParams = new List<MusicSearchParam> { MusicSearchParam.Q },
                BookSearchParams = new List<BookSearchParam> { BookSearchParam.Q }
            };

            // We map those strings to TorznabCatType enums.
            AddCat(caps, 2145, TorznabCatType.TV, "Film/Vidéo");
            AddCat(caps, 2178, TorznabCatType.MoviesOther, "Film/Vidéo : Animation");
            AddCat(caps, 2179, TorznabCatType.TVAnime, "Film/Vidéo : Animation Série");
            AddCat(caps, 2180, TorznabCatType.AudioVideo, "Film/Vidéo : Concert");
            AddCat(caps, 2181, TorznabCatType.TVDocumentary, "Film/Vidéo : Documentaire");
            AddCat(caps, 2182, TorznabCatType.TV, "Film/Vidéo : Emission TV");
            AddCat(caps, 2183, TorznabCatType.Movies, "Film/Vidéo : Film");
            AddCat(caps, 2184, TorznabCatType.TV, "Film/Vidéo : Série TV");
            AddCat(caps, 2185, TorznabCatType.TV, "Film/Vidéo : Spectacle");
            AddCat(caps, 2186, TorznabCatType.TVSport, "Film/Vidéo : Sport");
            AddCat(caps, 2187, TorznabCatType.TVOther, "Film/Vidéo : Vidéo-clips");
            AddCat(caps, 2139, TorznabCatType.Audio, "Audio");
            AddCat(caps, 2147, TorznabCatType.AudioOther, "Audio : Karaoké");
            AddCat(caps, 2148, TorznabCatType.Audio, "Audio : Musique");
            AddCat(caps, 2150, TorznabCatType.AudioOther, "Audio : Podcast Radio");
            AddCat(caps, 2149, TorznabCatType.AudioOther, "Audio : Samples");
            AddCat(caps, 2144, TorznabCatType.PC, "Application");
            AddCat(caps, 2177, TorznabCatType.PC0day, "Application : Autre");
            AddCat(caps, 2176, TorznabCatType.PC0day, "Application : Formation");
            AddCat(caps, 2171, TorznabCatType.PCISO, "Application : Linux");
            AddCat(caps, 2172, TorznabCatType.PCMac, "Application : MacOS");
            AddCat(caps, 2174, TorznabCatType.PCMobileAndroid, "Application : Smartphone");
            AddCat(caps, 2175, TorznabCatType.PCMobileAndroid, "Application : Tablette");
            AddCat(caps, 2173, TorznabCatType.PC0day, "Application : Windows");
            AddCat(caps, 2142, TorznabCatType.PCGames, "Jeu vidéo");
            AddCat(caps, 2167, TorznabCatType.ConsoleOther, "Jeu vidéo : Autre");
            AddCat(caps, 2159, TorznabCatType.PCGames, "Jeu vidéo : Linux");
            AddCat(caps, 2160, TorznabCatType.PCGames, "Jeu vidéo : MacOS");
            AddCat(caps, 2162, TorznabCatType.ConsoleXBoxOne, "Jeu vidéo : Microsoft");
            AddCat(caps, 2163, TorznabCatType.ConsoleWii, "Jeu vidéo : Nintendo");
            AddCat(caps, 2165, TorznabCatType.PCMobileAndroid, "Jeu vidéo : Smartphone");
            AddCat(caps, 2164, TorznabCatType.ConsolePS4, "Jeu vidéo : Sony");
            AddCat(caps, 2166, TorznabCatType.PCMobileAndroid, "Jeu vidéo : Tablette");
            AddCat(caps, 2161, TorznabCatType.PCGames, "Jeu vidéo : Windows");
            AddCat(caps, 2140, TorznabCatType.Books, "eBook");
            AddCat(caps, 2151, TorznabCatType.AudioAudiobook, "eBook : Audio");
            AddCat(caps, 2152, TorznabCatType.BooksEBook, "eBook : Bds");
            AddCat(caps, 2153, TorznabCatType.BooksComics, "eBook : Comics");
            AddCat(caps, 2154, TorznabCatType.BooksEBook, "eBook : Livres");
            AddCat(caps, 2155, TorznabCatType.BooksComics, "eBook : Mangas");
            AddCat(caps, 2156, TorznabCatType.BooksMags, "eBook : Presse");
            AddCat(caps, 2300, TorznabCatType.Other, "Nulled");
            AddCat(caps, 2301, TorznabCatType.Other, "Nulled : Wordpress");
            AddCat(caps, 2302, TorznabCatType.Other, "Nulled : Scripts PHP & CMS");
            AddCat(caps, 2303, TorznabCatType.Other, "Nulled : Mobile");
            AddCat(caps, 2304, TorznabCatType.Other, "Nulled : Divers");
            AddCat(caps, 2200, TorznabCatType.Other, "Imprimante 3D");
            AddCat(caps, 2201, TorznabCatType.Other, "Imprimante 3D : Objets");
            AddCat(caps, 2202, TorznabCatType.Other, "Imprimante 3D : Personnages");
            AddCat(caps, 2141, TorznabCatType.Other, "Emulation");
            AddCat(caps, 2157, TorznabCatType.Other, "Emulation : Emulateurs");
            AddCat(caps, 2158, TorznabCatType.Other, "Emulation : Roms");
            AddCat(caps, 2143, TorznabCatType.Other, "GPS");
            AddCat(caps, 2168, TorznabCatType.Other, "GPS : Applications");
            AddCat(caps, 2169, TorznabCatType.Other, "GPS : Cartes");
            AddCat(caps, 2170, TorznabCatType.Other, "GPS : Divers");
            AddCat(caps, 2188, TorznabCatType.XXX, "XXX");
            AddCat(caps, 2401, TorznabCatType.XXXOther, "XXX : Ebooks");
            AddCat(caps, 2189, TorznabCatType.XXX, "XXX : Films");
            AddCat(caps, 2190, TorznabCatType.XXX, "XXX : Hentai");
            AddCat(caps, 2191, TorznabCatType.XXXImageSet, "XXX : Images");
            AddCat(caps, 2402, TorznabCatType.XXXOther, "XXX : Jeux");
            return caps;
        }

        private static void AddCat(TorznabCapabilities caps, int trackerId, TorznabCategory cat, string desc)
        {
            caps.Categories.AddCategoryMapping(trackerId, cat, desc);
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var username = ((StringConfigurationItem)configData.GetDynamic(CfgUsername)).Value;
            var password = ((PasswordConfigurationItem)configData.GetDynamic(CfgPassword)).Value;
            if (username.IsNullOrWhiteSpace() || password.IsNullOrWhiteSpace())
                throw new Exception("Username / Password is required.");
            var processLogin = SiteLink + "auth/process_login";
            var loginUrl = SiteLink + "auth/login";
            var post = new Dictionary<string, string> { ["id"] = username, ["pass"] = password };
            await RequestWithCookiesAsync(SiteLink, string.Empty);
            var resp = await RequestLoginAndFollowRedirect(processLogin, post, null, false, SiteLink, loginUrl);
            if (resp.Status == HttpStatusCode.OK)
            {
                IsConfigured = true;
                SaveConfig();
                return IndexerConfigurationStatus.Completed;
            }

            throw new Exception("Login failed (unexpected response).");
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            // Map requested torznab categories -> YGG tracker categories
            var trackerCatsStr = MapTorznabCapsToTrackers(query).Distinct().ToList();
            var trackerCats = trackerCatsStr.Select(x => int.TryParse(x, out var id) ? id : -1).ToList();
            if (!trackerCats.Any())
            {
                // default "all"
                trackerCats.Add(-1);
            }

            var keywords = BuildKeywords(query);

            // Sorting / ordering from config
            var order = ((SingleSelectConfigurationItem)configData.GetDynamic(CfgOrder)).Value;
            var sortKey = ((SingleSelectConfigurationItem)configData.GetDynamic(CfgSort)).Value;

            // the config stores key like "publish_date", we must map to site value "created"
            var siteSort = sortKey switch
            {
                "publish_date" => "created",
                "seed" => "seeders",
                "size" => "size",
                "name" => "title",
                _ => "created"
            };

            var isPage2 = query.Offset > 0;

            // 1 request per root-category to avoid duplicates, with sub_category when needed.
            foreach (var request in BuildSearchRequests(trackerCats))
            {
                var form = new Dictionary<string, string> { ["do"] = "search", ["order"] = order, ["sort"] = siteSort };

                form["category"] = request.RootCategory;
                if (!string.IsNullOrEmpty(request.SubCategory))
                    form["sub_category"] = request.SubCategory;

                // keywords
                form["name"] = keywords;

                // page
                if (isPage2)
                    form["page"] = "50";
                var queryString = string.Join(
                    "&", form.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
                var url = $"{SiteLink}engine/search?{queryString}";

                var resp = await RequestWithCookiesAndRetryAsync(
                    url : url, method: RequestType.POST, data: new Dictionary<string, string>(), referer: SiteLink);
                var html = resp.ContentString ?? "";

                // parse results table
                releases.AddRange(ParseSearchResults(html));
            }

            return releases;
        }

        public override async Task<byte[]> Download(Uri link)
        {
            if (link == null)
                throw new ArgumentNullException(nameof(link));

            // Only intercept YGG download endpoint
            if (link.AbsoluteUri.IndexOf("/engine/download_torrent", StringComparison.OrdinalIgnoreCase) < 0)
                return await base.Download(link);
            var torrentId = GetQueryParam(link, "id");
            if (torrentId.IsNullOrWhiteSpace())
                return await base.Download(link);

            // POST /engine/start_download_timer  { torrent_id }
            var timerUrl = SiteLink + "engine/start_download_timer";

            var payload = $"torrent_id={Uri.EscapeDataString(torrentId)}";

            var headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/x-www-form-urlencoded"
            };


            var timerResp = await RequestWithCookiesAndRetryAsync(
                url: timerUrl,
                method: RequestType.POST,
                referer: SiteLink,
                data: null,
                headers: headers,
                rawbody: payload
                );
            logger.Info("RESPONSE : " + timerResp.ContentString);
            logger.Info("RESPONSE STA : " + timerResp.Status);
            var timerBody = timerResp.ContentString ?? "";
            string token;
            try
            {
                var json = JObject.Parse(timerBody);
                token = json["token"]?.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to parse start_download_timer response as JSON: " + Take(timerBody, 200), ex);
            }

            if (token.IsNullOrWhiteSpace())
                throw new Exception("Token is missing from start_download_timer response.");
            var final = new Uri(
                $"{SiteLink.TrimEnd('/')}/engine/download_torrent?id={torrentId}&token={token}");

            return await base.Download(final);
        }

        // ----------------- Search request mapping (based on YAML paths) -----------------

        private sealed class SearchRequest
        {
            public string RootCategory { get; set; } // "all" or numeric string
            public string SubCategory { get; set; } // optional
            public bool UseSaisonRewrite { get; set; } // for TV groups which use "Saison X"
        }

        private IEnumerable<SearchRequest> BuildSearchRequests(List<int> trackerCats)
        {
            // If no category specified -> Tous (category=all, no sub)
            if (trackerCats.Count == 1 && trackerCats[0] == -1)
            {
                yield return new SearchRequest { RootCategory = "all", SubCategory = null, UseSaisonRewrite = true };
                yield break;
            }

            var produced = new HashSet<string>(StringComparer.Ordinal);
            foreach (var c in trackerCats)
            {
                // TV/Anime (2179) & Movies/Other (2178) & Movies (2183) are sub_categories of 2145
                if (c == 2179 || c == 2178 || c == 2183)
                {
                    var key = $"2145|{c}";
                    if (produced.Add(key))
                    {
                        yield return new SearchRequest
                        {
                            RootCategory = "2145",
                            SubCategory = c.ToString(CultureInfo.InvariantCulture),
                            UseSaisonRewrite = true
                        };
                    }

                    continue;
                }

                // Any other Film/Vidéo leaf -> root 2145 without sub_category
                if (IsFilmVideoCategory(c))
                {
                    var key = "2145|";
                    if (produced.Add(key))
                    {
                        yield return new SearchRequest
                        {
                            RootCategory = "2145", SubCategory = null, UseSaisonRewrite = true
                        };
                    }

                    continue;
                }

                if (IsAudioCategory(c))
                {
                    var key = "2139|";
                    if (produced.Add(key))
                    {
                        yield return new SearchRequest
                        {
                            RootCategory = "2139", SubCategory = null, UseSaisonRewrite = false
                        };
                    }

                    continue;
                }

                if (IsApplicationCategory(c))
                {
                    var key = "2144|";
                    if (produced.Add(key))
                    {
                        yield return new SearchRequest
                        {
                            RootCategory = "2144", SubCategory = null, UseSaisonRewrite = false
                        };
                    }

                    continue;
                }

                if (IsGameCategory(c))
                {
                    var key = "2142|";
                    if (produced.Add(key))
                    {
                        yield return new SearchRequest
                        {
                            RootCategory = "2142", SubCategory = null, UseSaisonRewrite = false
                        };
                    }

                    continue;
                }

                if (IsEbookCategory(c))
                {
                    var key = "2140|";
                    if (produced.Add(key))
                    {
                        yield return new SearchRequest
                        {
                            RootCategory = "2140", SubCategory = null, UseSaisonRewrite = false
                        };
                    }

                    continue;
                }

                if (IsNulledCategory(c))
                {
                    var key = "2300|";
                    if (produced.Add(key))
                    {
                        yield return new SearchRequest
                        {
                            RootCategory = "2300", SubCategory = null, UseSaisonRewrite = false
                        };
                    }

                    continue;
                }

                if (IsPrinter3DCategory(c))
                {
                    var key = "2200|";
                    if (produced.Add(key))
                    {
                        yield return new SearchRequest
                        {
                            RootCategory = "2200", SubCategory = null, UseSaisonRewrite = false
                        };
                    }

                    continue;
                }

                if (IsEmulationCategory(c))
                {
                    var key = "2141|";
                    if (produced.Add(key))
                    {
                        yield return new SearchRequest
                        {
                            RootCategory = "2141", SubCategory = null, UseSaisonRewrite = false
                        };
                    }

                    continue;
                }

                if (IsGpsCategory(c))
                {
                    var key = "2143|";
                    if (produced.Add(key))
                    {
                        yield return new SearchRequest
                        {
                            RootCategory = "2143", SubCategory = null, UseSaisonRewrite = false
                        };
                    }

                    continue;
                }

                if (IsXxxCategory(c))
                {
                    var key = "2188|";
                    if (produced.Add(key))
                    {
                        yield return new SearchRequest
                        {
                            RootCategory = "2188", SubCategory = null, UseSaisonRewrite = false
                        };
                    }

                    continue;
                }

                // fallback: all
                var fallbackKey = "all|";
                if (produced.Add(fallbackKey))
                {
                    yield return new SearchRequest { RootCategory = "all", SubCategory = null, UseSaisonRewrite = true };
                }
            }
        }

        private static bool IsFilmVideoCategory(int c) => c == 2145 || (c >= 2178 && c <= 2187);
        private static bool IsAudioCategory(int c) => c == 2139 || (c >= 2147 && c <= 2150);
        private static bool IsApplicationCategory(int c) => c == 2144 || (c >= 2171 && c <= 2177);
        private static bool IsGameCategory(int c) => c == 2142 || (c >= 2159 && c <= 2167);
        private static bool IsEbookCategory(int c) => c == 2140 || (c >= 2151 && c <= 2156);
        private static bool IsNulledCategory(int c) => c >= 2300 && c <= 2304;
        private static bool IsPrinter3DCategory(int c) => c >= 2200 && c <= 2202;
        private static bool IsEmulationCategory(int c) => c == 2141 || c == 2157 || c == 2158;
        private static bool IsGpsCategory(int c) => c == 2143 || (c >= 2168 && c <= 2170);
        private static bool IsXxxCategory(int c) => c == 2188 || (c >= 2189 && c <= 2191) || c == 2401 || c == 2402;

        // ----------------- Parsing -----------------

        private IEnumerable<ReleaseInfo> ParseSearchResults(string html)
        {
            var releases = new List<ReleaseInfo>();
            try
            {
                var parser = new HtmlParser();
                using var doc = parser.ParseDocument(html);

                var rows = doc.QuerySelectorAll("table.table > tbody > tr");
                foreach (var row in rows)
                {
                    var a = row.QuerySelector("td:nth-child(2) > a");
                    if (a == null)
                        continue;
                    var detailsHref = a.GetAttribute("href") ?? "";
                    var idMatch = _idRegex.Match(detailsHref);
                    if (!idMatch.Success)
                        continue;
                    var id = idMatch.Groups[1].Value;
                    var titleRaw = a.TextContent?.Trim() ?? "";
                    var title = NormalizeTitle(titleRaw);
                    var catHidden = row.QuerySelector("td:nth-child(1) > div.hidden")?.TextContent?.Trim() ?? "";
                    var catId = ParseUtil.CoerceInt(catHidden);
                    var category = MapTrackerCatToNewznab(catId);
                    var unix = row.QuerySelector("td:nth-child(5) > div.hidden")?.TextContent?.Trim() ?? "";
                    var publishDate = ParseUnix(unix);
                    var sizeText = row.QuerySelector("td:nth-child(6)")?.TextContent?.Trim() ?? "";
                    sizeText = sizeText.Replace("o", "B");
                    var size = ParseUtil.GetBytes(sizeText);
                    var grabs = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(7)")?.TextContent);
                    var seeders = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(8)")?.TextContent);
                    var leechers = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(9)")?.TextContent);
                    var details = new Uri(SiteLink + detailsHref.TrimStart('/'));
                    var link = new Uri($"{SiteLink.TrimEnd('/')}/engine/download_torrent?id={Uri.EscapeDataString(id)}");
                    releases.Add(
                        new ReleaseInfo
                        {
                            Guid = details,
                            Details = details,
                            Link = link,
                            Title = title,
                            Category = category,
                            Size = size,
                            Grabs = grabs,
                            Seeders = seeders,
                            Peers = seeders + leechers,
                            PublishDate = publishDate,
                            DownloadVolumeFactor = 1,
                            UploadVolumeFactor = 1
                        });
                }
            }
            catch (Exception ex)
            {
                OnParseError(html, ex);
            }

            return releases;
        }

        private ICollection<int> MapTrackerCatToNewznab(int trackerCat)
        {
            return TorznabCaps.Categories.MapTrackerCatToNewznab(trackerCat.ToString());
        }

        private static DateTime ParseUnix(string unix)
        {
            if (long.TryParse(unix, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ts))
            {
                try { return DateTimeOffset.FromUnixTimeSeconds(ts).UtcDateTime; }
                catch
                {
                    /* ignore */
                }
            }

            return DateTime.UtcNow;
        }

        // ----------------- Keyword & Title filters (from YAML) -----------------

        private string BuildKeywords(TorznabQuery query)
        {
            var keywords = query.GetQueryString() ?? "";
            keywords = keywords.Trim();
            if (keywords.IsNullOrWhiteSpace())
                return "";

            // enhancedAnime4: 1234 -> E1234
            if (((BoolConfigurationItem)configData.GetDynamic(CfgEnhancedAnime4)).Value)
                keywords = Regex.Replace(keywords, @"\b(\d{4})\b", "E$1", RegexOptions.Compiled);

            // enhancedAnime: 123 -> E123
            if (((BoolConfigurationItem)configData.GetDynamic(CfgEnhancedAnime)).Value)
                keywords = Regex.Replace(keywords, @"\b(\d{2,3})\b", "E$1", RegexOptions.Compiled);

            // fix separators
            keywords = _separatorsRegex.Replace(keywords, " ");

            // strip season if configured
            if (((BoolConfigurationItem)configData.GetDynamic(CfgStripSeason)).Value)
                keywords = _stripSeasonRegex.Replace(keywords, "");
            keywords = keywords.Trim();

            // quote each word
            keywords = _quoteWordsRegex.Replace(keywords, "\"$1\"");
            return keywords;
        }

        private string NormalizeTitle(string title)
        {
            if (title.IsNullOrWhiteSpace())
                return title ?? "";
            var enhancedAnime = ((BoolConfigurationItem)configData.GetDynamic(CfgEnhancedAnime)).Value;
            var enhancedAnime4 = ((BoolConfigurationItem)configData.GetDynamic(CfgEnhancedAnime4)).Value;

            // title_normal: apply season/episode transforms
            var t = title;

            // Saison 1 Episode 2 -> S01E02 (or keep as-is unless enhancedAnime4)
            t = _saisonToSxxEyy1.Replace(t, enhancedAnime4 ? "S$2E$4" : "$1$2$3$4");
            t = _saisonToSxxEyy2.Replace(t, "S$1E$2");

            // Saison 1 -> S01
            t = _saisonToSxx1.Replace(t, enhancedAnime4 ? "S$2" : "$1$2");
            t = _saisonToSxx2.Replace(t, "S$1");

            // Episode 1 -> E01
            t = _episodeToEyy1.Replace(t, enhancedAnime4 ? "E$2" : "$1$2");
            t = _episodeToEyy2.Replace(t, "E$1");

            // S1 à 2 -> S1-2
            t = _range4Digits.Replace(t, enhancedAnime4 ? "$1$2-$4" : "$1$2$3$4");
            t = _range1To3Digits.Replace(t, "$1$2-$3");

            // dd-mm-yyyy -> yyyy.mm.dd
            t = _frenchDateToIso.Replace(t, "$3.$2.$1");

            // title_filtered (filter_title option): move year after title
            if (((BoolConfigurationItem)configData.GetDynamic(CfgFilterTitle)).Value)
            {
                t = _moveYearRegex.Replace(t, "$1 $3 $2 $4 $5");
                t = t.Trim();
                t = _removeExtRegex.Replace(t, "");
                t = _normalizeSpacesRegex.Replace(t, " ").Trim();
            }

            // vostfr replacement
            if (((BoolConfigurationItem)configData.GetDynamic(CfgVostfr)).Value)
                t = _vostfrRegex.Replace(t, "ENGLISH");

            // multilang replacement
            if (((BoolConfigurationItem)configData.GetDynamic(CfgMultiLang)).Value)
            {
                var lang = ((SingleSelectConfigurationItem)configData.GetDynamic(CfgMultiLanguageValue)).Value;
                if (!lang.IsNullOrWhiteSpace())
                    t = _multiReplaceRegex.Replace(t, lang);
            }

            // final enhanced replacements
            if (enhancedAnime4)
                t = Regex.Replace(t, @"\b(\d{4})\b", "E$1", RegexOptions.Compiled);
            if (enhancedAnime)
                t = Regex.Replace(t, @"\b(\d{2,3})\b", "E$1", RegexOptions.Compiled);
            t = _normalizeSpacesRegex.Replace(t, " ").Trim();
            return t;
        }

        // ----------------- Utils -----------------

        private static string GetQueryParam(Uri uri, string name)
        {
            var q = uri.Query;
            if (string.IsNullOrWhiteSpace(q))
                return null;
            if (q.StartsWith("?"))
                q = q.Substring(1);
            foreach (var part in q.Split(
                         new[]
                         {
                             '&'
                         }, StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = part.Split(
                    new[]
                    {
                        '='
                    }, 2);
                var key = Uri.UnescapeDataString(kv[0] ?? "");
                if (!key.Equals(name, StringComparison.OrdinalIgnoreCase))
                    continue;
                var val = kv.Length > 1 ? kv[1] : "";
                return Uri.UnescapeDataString(val);
            }

            return null;
        }

        private static string Take(string s, int max)
        {
            if (string.IsNullOrEmpty(s))
                return s ?? "";
            return s.Length <= max ? s : s.Substring(0, max);
        }
    }
}
