using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class NCore : IndexerBase
    {
        public override string Id => "ncore";
        public override string Name => "nCore";
        public override string Description => "A Hungarian private torrent site.";
        public override string SiteLink { get; protected set; } = "https://ncore.pro/";
        public override string[] LegacySiteLinks => new[]
        {
            "https://ncore.cc/"
        };
        public override string Language => "hu-HU";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private string LoginUrl => SiteLink + "login.php";
        private string SearchUrl => SiteLink + "torrents.php";

        private new ConfigurationDataNCore configData => (ConfigurationDataNCore)base.configData;

        private readonly string[] _languageCats =
        {
            "xvidser",
            "dvdser",
            "hdser",
            "xvid",
            "dvd",
            "dvd9",
            "hd",
            "mp3",
            "lossless",
            "ebook"
        };

        public NCore(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
                     ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataNCore())
        {
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

            caps.Categories.AddCategoryMapping("xvid_hun", TorznabCatType.MoviesSD, "Film SD/HU");
            caps.Categories.AddCategoryMapping("xvid", TorznabCatType.MoviesSD, "Film SD/EN");
            caps.Categories.AddCategoryMapping("dvd_hun", TorznabCatType.MoviesDVD, "Film DVDR/HU");
            caps.Categories.AddCategoryMapping("dvd", TorznabCatType.MoviesDVD, "Film DVDR/EN");
            caps.Categories.AddCategoryMapping("dvd9_hun", TorznabCatType.MoviesDVD, "Film DVD9/HU");
            caps.Categories.AddCategoryMapping("dvd9", TorznabCatType.MoviesDVD, "Film DVD9/EN");
            caps.Categories.AddCategoryMapping("hd_hun", TorznabCatType.MoviesHD, "Film HD/HU");
            caps.Categories.AddCategoryMapping("hd", TorznabCatType.MoviesHD, "Film HD/EN");
            caps.Categories.AddCategoryMapping("xvidser_hun", TorznabCatType.TVSD, "Sorozat SD/HU");
            caps.Categories.AddCategoryMapping("xvidser", TorznabCatType.TVSD, "Sorozat SD/EN");
            caps.Categories.AddCategoryMapping("dvdser_hun", TorznabCatType.TVSD, "Sorozat DVDR/HU");
            caps.Categories.AddCategoryMapping("dvdser", TorznabCatType.TVSD, "Sorozat DVDR/EN");
            caps.Categories.AddCategoryMapping("hdser_hun", TorznabCatType.TVHD, "Sorozat HD/HU");
            caps.Categories.AddCategoryMapping("hdser", TorznabCatType.TVHD, "Sorozat HD/EN");
            caps.Categories.AddCategoryMapping("mp3_hun", TorznabCatType.AudioMP3, "Zene MP3/HU");
            caps.Categories.AddCategoryMapping("mp3", TorznabCatType.AudioMP3, "Zene MP3/EN");
            caps.Categories.AddCategoryMapping("lossless_hun", TorznabCatType.AudioLossless, "Zene Lossless/HU");
            caps.Categories.AddCategoryMapping("lossless", TorznabCatType.AudioLossless, "Zene Lossless/EN");
            caps.Categories.AddCategoryMapping("clip", TorznabCatType.AudioVideo, "Zene Klip");
            caps.Categories.AddCategoryMapping("xxx_xvid", TorznabCatType.XXXXviD, "XXX SD");
            caps.Categories.AddCategoryMapping("xxx_dvd", TorznabCatType.XXXDVD, "XXX DVDR");
            caps.Categories.AddCategoryMapping("xxx_imageset", TorznabCatType.XXXImageSet, "XXX Imageset");
            caps.Categories.AddCategoryMapping("xxx_hd", TorznabCatType.XXX, "XXX HD");
            caps.Categories.AddCategoryMapping("game_iso", TorznabCatType.PCGames, "Játék PC/ISO");
            caps.Categories.AddCategoryMapping("game_rip", TorznabCatType.PCGames, "Játék PC/RIP");
            caps.Categories.AddCategoryMapping("console", TorznabCatType.Console, "Játék Konzol");
            caps.Categories.AddCategoryMapping("iso", TorznabCatType.PCISO, "Program Prog/ISO");
            caps.Categories.AddCategoryMapping("misc", TorznabCatType.PC0day, "Program Prog/RIP");
            caps.Categories.AddCategoryMapping("mobil", TorznabCatType.PCMobileOther, "Program Prog/Mobil");
            caps.Categories.AddCategoryMapping("ebook_hun", TorznabCatType.Books, "Könyv eBook/HU");
            caps.Categories.AddCategoryMapping("ebook", TorznabCatType.Books, "Könyv eBook/EN");

            return caps;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            if (configData.Hungarian.Value == false && configData.English.Value == false)
                throw new ExceptionWithConfigData("Please select at least one language.", configData);
            var loginPage = await RequestWithCookiesAsync(LoginUrl, string.Empty);
            var pairs = new Dictionary<string, string>
            {
                {"nev", configData.Username.Value},
                {"pass", configData.Password.Value},
                {"ne_leptessen_ki", "1"},
                {"set_lang", "en"},
                {"submitted", "1"},
                {"submit", "Access!"}
            };
            if (!string.IsNullOrEmpty(configData.TwoFactor.Value))
                pairs.Add("2factor", configData.TwoFactor.Value);
            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, loginPage.Cookies, true, referer: SiteLink);
            await ConfigureIfOK(
                result.Cookies, result.ContentString?.Contains("profile.php") == true, () =>
                {
                    var parser = new HtmlParser();
                    using var dom = parser.ParseDocument(result.ContentString);
                    var msgContainer = dom.QuerySelector("#hibauzenet table tbody tr")?.Children[1];
                    throw new ExceptionWithConfigData(msgContainer?.TextContent ?? "Error while trying to login.", configData);
                });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var results = new List<ReleaseInfo>();
            if (!(query.IsImdbQuery && query.IsTVSearch))
                results = await PerformQueryAsync(query, null);
            // if we search for a localized title nCore can't handle any extra S/E information
            // search without it and AND filter the results. See #1450
            if (query.IsTVSearch && (!results.Any() || query.IsImdbQuery))
                results = await PerformQueryAsync(query, query.GetEpisodeSearchString());
            return results;
        }

        private async Task<List<ReleaseInfo>> PerformQueryAsync(TorznabQuery query, string episodeString)
        {
            var releases = new List<ReleaseInfo>();
            var pairs = new NameValueCollection
            {
                {"nyit_sorozat_resz", "true"},
                {"tipus", "kivalasztottak_kozott"},
                {"submit.x", "1"},
                {"submit.y", "1"},
                {"submit", "Ok"}
            };
            if (query.IsImdbQuery)
            {
                pairs.Add("miben", "imdb");
                pairs.Add("mire", query.ImdbID);
            }
            else
            {
                pairs.Add("miben", "name");
                pairs.Add("mire", episodeString == null ? query.GetQueryString() : query.SanitizedSearchTerm);
            }

            var cats = MapTorznabCapsToTrackers(query);
            if (cats.Count == 0)
                cats = GetAllTrackerCategories();
            if (!configData.Hungarian.Value)
                cats.RemoveAll(cat => cat.Contains("_hun"));
            if (!configData.English.Value)
                cats = cats.Except(_languageCats).ToList();

            pairs.Add("kivalasztott_tipus[]", string.Join(",", cats));
            var results = await RequestWithCookiesAndRetryAsync(
                SearchUrl, null, RequestType.POST, null, pairs.ToEnumerable(true));
            var parser = new HtmlParser();
            using var dom = parser.ParseDocument(results.ContentString);

            // find number of torrents / page
            var torrentPerPage = dom.QuerySelectorAll(".box_torrent").Length;
            if (torrentPerPage == 0)
                return releases;
            var startPage = (query.Offset / torrentPerPage) + 1;
            var previouslyParsedOnPage = query.Offset % torrentPerPage;

            // find page links in the bottom
            var lastPageLink = dom.QuerySelectorAll("div[id=pager_bottom] a[href*=oldal]")
                                  .LastOrDefault()?.GetAttribute("href");
            var pages = int.TryParse(ParseUtil.GetArgumentFromQueryString(lastPageLink, "oldal"), out var lastPage)
                ? lastPage
                : 1;

            var limit = query.Limit;
            if (limit == 0)
                limit = 100;
            if (startPage == 1)
            {
                releases = ParseTorrents(results, episodeString, query, releases.Count, limit, previouslyParsedOnPage);
                previouslyParsedOnPage = 0;
                startPage++;
            }

            // Check all the pages for the torrents.
            // The starting index is 2. (the first one is the original where we parse out the pages.)
            for (var page = startPage; page <= pages && releases.Count < limit; page++)
            {
                pairs["oldal"] = page.ToString();
                results = await RequestWithCookiesAndRetryAsync(
                    SearchUrl, null, RequestType.POST, null, pairs.ToEnumerable(true));
                releases.AddRange(ParseTorrents(results, episodeString, query, releases.Count, limit, previouslyParsedOnPage));
                previouslyParsedOnPage = 0;
            }

            return releases;
        }

        private List<ReleaseInfo> ParseTorrents(WebResult results, string episodeString, TorznabQuery query,
                                                int alreadyFound, int limit, int previouslyParsedOnPage)
        {
            var releases = new List<ReleaseInfo>();
            try
            {
                var parser = new HtmlParser();
                using var dom = parser.ParseDocument(results.ContentString);
                var rows = dom.QuerySelectorAll(".box_torrent").Skip(previouslyParsedOnPage).Take(limit - alreadyFound);

                var key = ParseUtil.GetArgumentFromQueryString(
                    dom.QuerySelector("link[rel=alternate]").GetAttribute("href"), "key");
                // Check torrents only till we reach the query Limit
                foreach (var row in rows)
                    try
                    {
                        var torrentTxt = row.QuerySelector(".torrent_txt, .torrent_txt2").QuerySelector("a");
                        //if (torrentTxt == null) continue;
                        var infoLink = row.QuerySelector("a.infolink");
                        var imdbId = ParseUtil.GetLongFromString(infoLink?.GetAttribute("href"));
                        var desc = row.QuerySelector("span")?.GetAttribute("title") + " " +
                                   infoLink?.TextContent;
                        var torrentLink = SiteLink + torrentTxt.GetAttribute("href");
                        var downloadId = ParseUtil.GetArgumentFromQueryString(torrentLink, "id");

                        //Build site links
                        var details = new Uri(SiteLink + "torrents.php?action=details&id=" + downloadId);
                        var downloadLink = SiteLink + "torrents.php?action=download&id=" + downloadId;
                        var linkUri = new Uri(QueryHelpers.AddQueryString(downloadLink, "key", key));

                        var seeders = ParseUtil.CoerceInt(row.QuerySelector(".box_s2 a").TextContent);
                        var leechers = ParseUtil.CoerceInt(row.QuerySelector(".box_l2 a").TextContent);
                        var publishDate = DateTime.Parse(
                            row.QuerySelector(".box_feltoltve2").InnerHtml.Replace("<br>", " "),
                            CultureInfo.InvariantCulture);
                        var sizeSplit = row.QuerySelector(".box_meret2").TextContent.Split(' ');
                        var size = ParseUtil.GetBytes(sizeSplit[1].ToLower(), ParseUtil.CoerceFloat(sizeSplit[0]));
                        var catLink = row.QuerySelector("a:has(img[class='categ_link'])").GetAttribute("href");
                        var cat = ParseUtil.GetArgumentFromQueryString(catLink, "tipus");
                        var title = torrentTxt.GetAttribute("title");
                        // if the release name does not contain the language we add from the category
                        if (cat.Contains("hun") && !title.ToLower().Contains("hun"))
                            title += ".hun";

                        // Minimum seed time is 48 hours + 24 minutes (.4 hours) per GB of torrent size if downloaded in full.
                        // Or a 1.0 ratio on the torrent
                        var seedTime = TimeSpan.FromHours(48) +
                                       TimeSpan.FromMinutes(24 * ReleaseInfo.GigabytesFromBytes(size).Value);

                        var release = new ReleaseInfo
                        {
                            Title = title,
                            Description = desc.Trim(),
                            MinimumRatio = 1,
                            MinimumSeedTime = (long)seedTime.TotalSeconds,
                            DownloadVolumeFactor = 0,
                            UploadVolumeFactor = 1,
                            Link = linkUri,
                            Details = details,
                            Guid = details,
                            Seeders = seeders,
                            Peers = leechers + seeders,
                            Imdb = imdbId,
                            PublishDate = publishDate,
                            Size = size,
                            Category = MapTrackerCatToNewznab(cat)
                        };
                        var posterStr = row.QuerySelector("img.infobar_ico")?.GetAttribute("onmouseover");
                        if (posterStr != null)
                        {
                            // static call to Regex.Match caches the pattern, so we aren't recompiling every loop.
                            var posterMatch = Regex.Match(posterStr, @"mutat\('(.*?)', '", RegexOptions.Compiled);
                            release.Poster = new Uri(posterMatch.Groups[1].Value);
                        }

                        //TODO there is room for improvement here.
                        if (episodeString != null &&
                            query.MatchQueryStringAND(release.Title, queryStringOverride: episodeString) &&
                            !query.IsImdbQuery)
                        {
                            // For Sonarr if the search query was english the title must be english also
                            // The description holds the alternate language name
                            // so we need to swap title and description names
                            var tempTitle = release.Title;

                            // releaseData everything after Name.S0Xe0X
                            var releaseIndex = tempTitle.IndexOf(episodeString, StringComparison.OrdinalIgnoreCase) +
                                               episodeString.Length;
                            var releaseData = tempTitle.Substring(releaseIndex).Trim();

                            // release description contains [imdb: ****] but we only need the data before it for title
                            var description = new[]
                            {
                                release.Description,
                                ""
                            };
                            if (release.Description.Contains("[imdb:"))
                            {
                                description = release.Description.Split('[');
                                description[1] = "[" + description[1];
                            }

                            var match = Regex.Match(releaseData, @"^E\d\d?");
                            // if search is done for S0X than we don't want to put . between S0X and E0X
                            var episodeSeparator = episodeString.Length == 3 && match.Success ? null : ".";
                            release.Title =
                                (description[0].Trim() + "." + episodeString.Trim() + episodeSeparator +
                                 releaseData.Trim('.')).Replace(' ', '.');

                            // add back imdb points to the description [imdb: 8.7]
                            release.Description = tempTitle + " " + description[1];
                            release.Description = release.Description.Trim();
                        }

                        releases.Add(release);
                    }
                    catch (FormatException ex)
                    {
                        logger.Error("Problem of parsing Torrent:" + row.InnerHtml);
                        logger.Error("Exception was the following:" + ex);
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
