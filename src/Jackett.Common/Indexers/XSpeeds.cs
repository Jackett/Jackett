using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class XSpeeds : IndexerBase
    {
        public override string Id => "xspeeds";
        public override string Name => "XSpeeds";
        public override string Description => "XSpeeds (XS) is a Private Torrent Tracker for MOVIES / TV / GENERAL";
        public override string SiteLink { get; protected set; } = "https://www.xspeeds.eu/";
        public override string Language => "en-US";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private string LandingUrl => SiteLink + "login.php";
        private string LoginUrl => SiteLink + "takelogin.php";
        private string GetRSSKeyUrl => SiteLink + "getrss.php";
        private string SearchUrl => SiteLink + "browse.php";
        private readonly Regex _dateMatchRegex = new Regex(@"\d{2}-\d{2}-\d{4} \d{2}:\d{2}", RegexOptions.Compiled);

        private new ConfigurationDataBasicLoginWithRSSAndDisplay configData =>
            (ConfigurationDataBasicLoginWithRSSAndDisplay)base.configData;

        public XSpeeds(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps, ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataBasicLoginWithRSSAndDisplay())
        {
            // Configure the sort selects
            var sortBySelect = new SingleSelectConfigurationItem(
                "Sort by",
                new Dictionary<string, string>
                {
                    { "added", "created" },
                    { "seeders", "seeders" },
                    { "size", "size" },
                    { "name", "title" }
                })
            { Value = "added" };
            configData.AddDynamic("sortrequestedfromsite", sortBySelect);

            var orderSelect = new SingleSelectConfigurationItem(
                "Order",
                new Dictionary<string, string>
                {
                    { "desc", "descending" },
                    { "asc", "ascending" }
                })
            { Value = "desc" };
            configData.AddDynamic("orderrequestedfromsite", orderSelect);
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
                BookSearchParams = new List<BookSearchParam>
                {
                    BookSearchParam.Q
                }
            };

            caps.Categories.AddCategoryMapping(92, TorznabCatType.MoviesUHD, "4K Movies");
            caps.Categories.AddCategoryMapping(91, TorznabCatType.TVUHD, "4K TV");
            caps.Categories.AddCategoryMapping(94, TorznabCatType.TVUHD, "4K TV Boxsets");
            caps.Categories.AddCategoryMapping(70, TorznabCatType.TVAnime, "Anime");
            caps.Categories.AddCategoryMapping(80, TorznabCatType.AudioAudiobook, "Audiobooks");
            caps.Categories.AddCategoryMapping(66, TorznabCatType.MoviesBluRay, "Blu-Ray");
            caps.Categories.AddCategoryMapping(48, TorznabCatType.Books, "Books Magazines");
            caps.Categories.AddCategoryMapping(68, TorznabCatType.MoviesOther, "Cams/TS");
            caps.Categories.AddCategoryMapping(65, TorznabCatType.TVDocumentary, "Documentaries");
            caps.Categories.AddCategoryMapping(10, TorznabCatType.MoviesDVD, "DVDR");
            caps.Categories.AddCategoryMapping(72, TorznabCatType.MoviesForeign, "Foreign");
            caps.Categories.AddCategoryMapping(74, TorznabCatType.TVOther, "Kids");
            caps.Categories.AddCategoryMapping(95, TorznabCatType.PCMac, "Mac Games");
            caps.Categories.AddCategoryMapping(44, TorznabCatType.TVSport, "MMA");
            caps.Categories.AddCategoryMapping(11, TorznabCatType.Movies, "Movie Boxsets");
            caps.Categories.AddCategoryMapping(12, TorznabCatType.Movies, "Movies");
            caps.Categories.AddCategoryMapping(100, TorznabCatType.MoviesHD, "Movies HEVC");
            caps.Categories.AddCategoryMapping(13, TorznabCatType.Audio, "Music");
            caps.Categories.AddCategoryMapping(15, TorznabCatType.AudioVideo, "Music Videos");
            caps.Categories.AddCategoryMapping(32, TorznabCatType.ConsoleNDS, "NDS Games");
            caps.Categories.AddCategoryMapping(9, TorznabCatType.Other, "Other");
            caps.Categories.AddCategoryMapping(6, TorznabCatType.PCGames, "PC Games");
            caps.Categories.AddCategoryMapping(45, TorznabCatType.Other, "Pictures");
            caps.Categories.AddCategoryMapping(31, TorznabCatType.ConsolePS4, "Playstation");
            caps.Categories.AddCategoryMapping(71, TorznabCatType.TV, "PPV");
            caps.Categories.AddCategoryMapping(54, TorznabCatType.TV, "Soaps");
            caps.Categories.AddCategoryMapping(20, TorznabCatType.TVSport, "Sports");
            caps.Categories.AddCategoryMapping(102, TorznabCatType.TVSport, "Sports FIFA World Cup");
            caps.Categories.AddCategoryMapping(86, TorznabCatType.TVSport, "Sports MotorSports");
            caps.Categories.AddCategoryMapping(89, TorznabCatType.TVSport, "Sports Olympics");
            caps.Categories.AddCategoryMapping(88, TorznabCatType.TVSport, "Sports UK Football");
            caps.Categories.AddCategoryMapping(83, TorznabCatType.Movies, "TOTM");
            caps.Categories.AddCategoryMapping(21, TorznabCatType.TVSD, "TV Boxsets");
            caps.Categories.AddCategoryMapping(76, TorznabCatType.TVHD, "TV HD Boxsets");
            caps.Categories.AddCategoryMapping(97, TorznabCatType.TVHD, "TV HECV Boxsets");
            caps.Categories.AddCategoryMapping(47, TorznabCatType.TVHD, "TV HD");
            caps.Categories.AddCategoryMapping(96, TorznabCatType.TVHD, "TV HD HEVC");
            caps.Categories.AddCategoryMapping(16, TorznabCatType.TVSD, "TV SD");
            caps.Categories.AddCategoryMapping(7, TorznabCatType.ConsoleWii, "Wii Games");
            caps.Categories.AddCategoryMapping(43, TorznabCatType.TVSport, "Wrestling");
            caps.Categories.AddCategoryMapping(8, TorznabCatType.ConsoleXBox, "Xbox Games");

            // RSS Textual categories
            caps.Categories.AddCategoryMapping("4K Movies", TorznabCatType.MoviesUHD);
            caps.Categories.AddCategoryMapping("4K TV", TorznabCatType.TVUHD);
            caps.Categories.AddCategoryMapping("4K TV Boxsets", TorznabCatType.TVUHD);
            caps.Categories.AddCategoryMapping("Anime", TorznabCatType.TVAnime);
            caps.Categories.AddCategoryMapping("Audiobooks", TorznabCatType.AudioAudiobook);
            caps.Categories.AddCategoryMapping("Blu-Ray", TorznabCatType.MoviesBluRay);
            caps.Categories.AddCategoryMapping("Books Magazines", TorznabCatType.Books);
            caps.Categories.AddCategoryMapping("Cams/TS", TorznabCatType.MoviesOther);
            caps.Categories.AddCategoryMapping("Documentaries", TorznabCatType.TVDocumentary);
            caps.Categories.AddCategoryMapping("DVDR", TorznabCatType.MoviesDVD);
            caps.Categories.AddCategoryMapping("Foreign", TorznabCatType.MoviesForeign);
            caps.Categories.AddCategoryMapping("Kids", TorznabCatType.TVOther);
            caps.Categories.AddCategoryMapping("MMA", TorznabCatType.TVSport);
            caps.Categories.AddCategoryMapping("Movie Boxsets", TorznabCatType.Movies);
            caps.Categories.AddCategoryMapping("Movies", TorznabCatType.Movies);
            caps.Categories.AddCategoryMapping("Music", TorznabCatType.Audio);
            caps.Categories.AddCategoryMapping("Music Videos", TorznabCatType.AudioVideo);
            caps.Categories.AddCategoryMapping("NDS Games", TorznabCatType.ConsoleNDS);
            caps.Categories.AddCategoryMapping("Other", TorznabCatType.Other);
            caps.Categories.AddCategoryMapping("PC Games", TorznabCatType.PCGames);
            caps.Categories.AddCategoryMapping("Pictures", TorznabCatType.Other);
            caps.Categories.AddCategoryMapping("Playstation", TorznabCatType.ConsolePS4);
            caps.Categories.AddCategoryMapping("PPV", TorznabCatType.TV);
            caps.Categories.AddCategoryMapping("Soaps", TorznabCatType.TV);
            caps.Categories.AddCategoryMapping("Sports", TorznabCatType.TVSport);
            caps.Categories.AddCategoryMapping("FIFA World Cup", TorznabCatType.TVSport);
            caps.Categories.AddCategoryMapping("MotorSports", TorznabCatType.TVSport);
            caps.Categories.AddCategoryMapping("Olympics", TorznabCatType.TVSport);
            caps.Categories.AddCategoryMapping("UK Football", TorznabCatType.TVSport);
            caps.Categories.AddCategoryMapping("TOTM", TorznabCatType.Movies);
            caps.Categories.AddCategoryMapping("TV Boxsets", TorznabCatType.TVSD);
            caps.Categories.AddCategoryMapping("HD Boxsets", TorznabCatType.TVHD);
            caps.Categories.AddCategoryMapping("TV-HD", TorznabCatType.TVHD);
            caps.Categories.AddCategoryMapping("TV-SD", TorznabCatType.TVSD);
            caps.Categories.AddCategoryMapping("Wii Games", TorznabCatType.ConsoleWii);
            caps.Categories.AddCategoryMapping("Wrestling", TorznabCatType.TVSport);
            caps.Categories.AddCategoryMapping("Xbox Games", TorznabCatType.ConsoleXBox);

            return caps;
        }

        private string GetSortBy => ((SingleSelectConfigurationItem)configData.GetDynamic("sortrequestedfromsite")).Value;

        private string GetOrder => ((SingleSelectConfigurationItem)configData.GetDynamic("orderrequestedfromsite")).Value;

        public override async Task<ConfigurationData> GetConfigurationForSetup()
        {
            var loginPage = await RequestWithCookiesAsync(LandingUrl);
            var parser = new HtmlParser();
            var dom = parser.ParseDocument(loginPage.ContentString);
            var qCaptchaImg = dom.QuerySelector("img#regimage");
            if (qCaptchaImg != null)
            {
                var captchaUrl = qCaptchaImg.GetAttribute("src");
                var captchaImageResponse = await RequestWithCookiesAsync(captchaUrl, loginPage.Cookies, RequestType.GET, LandingUrl);

                var captchaText = new StringConfigurationItem("Captcha Text");
                var captchaImage = new DisplayImageConfigurationItem("Captcha Image") { Value = captchaImageResponse.ContentBytes };

                configData.AddDynamic("CaptchaText", captchaText);
                configData.AddDynamic("CaptchaImage", captchaImage);
            }
            else
                logger.Debug($"{Id}: No captcha image found");

            return configData;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string>
            {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value }
            };

            var captchaText = (StringConfigurationItem)configData.GetDynamic("CaptchaText");
            if (captchaText != null)
                pairs.Add("imagestring", captchaText.Value);

            //var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, SiteLink, true);
            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, SearchUrl, LandingUrl, true);
            await ConfigureIfOK(result.Cookies, result.ContentString?.Contains("logout.php") == true, () =>
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(result.ContentString);
                var errorMessage = dom.QuerySelector(".left_side table:nth-of-type(1) tr:nth-of-type(2)")?.TextContent.Trim().Replace("\n\t", " ");
                if (string.IsNullOrWhiteSpace(errorMessage))
                    errorMessage = dom.QuerySelector("div.notification-body")?.TextContent.Trim().Replace("\n\t", " ");

                throw new ExceptionWithConfigData(errorMessage ?? "Login failed.", configData);
            });

            try
            {
                // Get RSS key
                var rssParams = new Dictionary<string, string>
                {
                    { "feedtype", "download" },
                    { "timezone", "0" },
                    { "showrows", "50" }
                };
                var rssPage = await RequestWithCookiesAsync(
                    GetRSSKeyUrl, result.Cookies, RequestType.POST, data: rssParams);
                var match = Regex.Match(rssPage.ContentString, "(?<=secret_key\\=)([a-zA-z0-9]*)");
                configData.RSSKey.Value = match.Success ? match.Value : string.Empty;
                if (string.IsNullOrWhiteSpace(configData.RSSKey.Value))
                    throw new Exception("Failed to get RSS Key");
                SaveConfig();
            }
            catch
            {
                IsConfigured = false;
                throw;
            }

            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var prevCook = CookieHeader + "";

            var categoryMapping = MapTorznabCapsToTrackers(query);

            var searchParams = new Dictionary<string, string>
            {
                { "do", "search" },
                { "category", categoryMapping.FirstIfSingleOrDefault("0") }, // multi category search not supported
                { "include_dead_torrents", "yes" },
                { "sort", GetSortBy },
                { "order", GetOrder }
            };

            var searchString = Regex.Replace(query.GetQueryString(), @"[ -._]+", " ").Trim();

            if (query.IsImdbQuery)
            {
                searchParams.Add("keywords", query.ImdbID);
                searchParams.Add("search_type", "t_both");
            }
            else if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchParams.Add("keywords", searchString);
                searchParams.Add("search_type", "t_name");
            }

            var searchPage = await RequestWithCookiesAndRetryAsync(SearchUrl, CookieHeader, RequestType.POST, null, searchParams);
            // Occasionally the cookies become invalid, login again if that happens
            if (searchPage.IsRedirect)
            {
                await ApplyConfiguration(null);
                searchPage = await RequestWithCookiesAndRetryAsync(SearchUrl, CookieHeader, RequestType.POST, null, searchParams);
            }

            var releases = new List<ReleaseInfo>();

            try
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(searchPage.ContentString);
                var rows = dom.QuerySelectorAll("table#sortabletable > tbody > tr:has(div > a[href*=\"details.php?id=\"])");
                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();

                    var qDetails = row.QuerySelector("div > a[href*=\"details.php?id=\"]");
                    var qTitle = qDetails; // #7975

                    release.Title = qTitle.TextContent;

                    release.Guid = new Uri(row.QuerySelector("td:nth-of-type(3) a").GetAttribute("href"));
                    release.Link = release.Guid;
                    release.Details = new Uri(qDetails.GetAttribute("href"));

                    // 08-08-2015 12:51
                    // requests can be 'Pre Release Time: 25-04-2021 15:00 Uploaded: 3 Weeks, 2 Days, 23 Hours, 53 Minutes, 39 Seconds after Pre'
                    var dateMatch = _dateMatchRegex.Match(row.QuerySelector("td:nth-of-type(2) > div:last-child").TextContent.Trim());
                    if (dateMatch.Success)
                        release.PublishDate = DateTime.ParseExact(dateMatch.Value, "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture);

                    release.Seeders = ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(7)").TextContent);
                    release.Peers = release.Seeders + ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(8)").TextContent.Trim());
                    release.Size = ParseUtil.GetBytes(row.QuerySelector("td:nth-of-type(5)").TextContent.Trim());

                    var qPoster = row.QuerySelector("td:nth-of-type(2) .tooltip-content img");
                    if (qPoster != null)
                        release.Poster = new Uri(qPoster.GetAttribute("src"));

                    var categoryLink = row.QuerySelector("td:nth-of-type(1) a").GetAttribute("href");
                    var cat = ParseUtil.GetArgumentFromQueryString(categoryLink, "category");
                    release.Category = MapTrackerCatToNewznab(cat);

                    var grabs = row.QuerySelector("td:nth-child(6)").TextContent;
                    release.Grabs = ParseUtil.CoerceInt(grabs);

                    var cover = row.QuerySelector("td:nth-of-type(2) > div > img[src]")?.GetAttribute("src")?.Trim();
                    release.Poster = !string.IsNullOrEmpty(cover) && cover.StartsWith("http") ? new Uri(cover) : null;

                    if (row.QuerySelector("img[alt^=\"Free Torrent\"], img[alt^=\"Sitewide Free Torrent\"]") != null)
                        release.DownloadVolumeFactor = 0;
                    else if (row.QuerySelector("img[alt^=\"Silver Torrent\"]") != null)
                        release.DownloadVolumeFactor = 0.5;
                    else
                        release.DownloadVolumeFactor = 1;

                    release.UploadVolumeFactor = row.QuerySelector("img[alt^=\"x2 Torrent\"]") != null ? 2 : 1;

                    release.MinimumRatio = 0.8;

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(searchPage.ContentString, ex);
            }

            if (!CookieHeader.Trim().Equals(prevCook.Trim()))
                SaveConfig();

            return releases;
        }
    }
}
