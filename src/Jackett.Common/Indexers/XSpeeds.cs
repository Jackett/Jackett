using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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

        private string LandingUrl => SiteLink + "login.php";
        private string LoginUrl => SiteLink + "takelogin.php";
        private string GetRSSKeyUrl => SiteLink + "getrss.php";
        private string SearchUrl => SiteLink + "browse.php";
        private readonly Regex _dateMatchRegex = new Regex(@"\d{2}-\d{2}-\d{4} \d{2}:\d{2}", RegexOptions.Compiled);

        private new ConfigurationDataBasicLoginWithRSSAndDisplay configData =>
            (ConfigurationDataBasicLoginWithRSSAndDisplay)base.configData;

        public XSpeeds(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(
                   caps: new TorznabCapabilities
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
                   },
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataBasicLoginWithRSSAndDisplay())
        {
            AddCategoryMapping(92, TorznabCatType.MoviesUHD, "4K Movies");
            AddCategoryMapping(91, TorznabCatType.TVUHD, "4K TV");
            AddCategoryMapping(94, TorznabCatType.TVUHD, "4K TV Boxsets");
            AddCategoryMapping(70, TorznabCatType.TVAnime, "Anime");
            AddCategoryMapping(80, TorznabCatType.AudioAudiobook, "Audiobooks");
            AddCategoryMapping(66, TorznabCatType.MoviesBluRay, "Blu-Ray");
            AddCategoryMapping(48, TorznabCatType.Books, "Books Magazines");
            AddCategoryMapping(68, TorznabCatType.MoviesOther, "Cams/TS");
            AddCategoryMapping(65, TorznabCatType.TVDocumentary, "Documentaries");
            AddCategoryMapping(10, TorznabCatType.MoviesDVD, "DVDR");
            AddCategoryMapping(72, TorznabCatType.MoviesForeign, "Foreign");
            AddCategoryMapping(74, TorznabCatType.TVOther, "Kids");
            AddCategoryMapping(95, TorznabCatType.PCMac, "Mac Games");
            AddCategoryMapping(44, TorznabCatType.TVSport, "MMA");
            AddCategoryMapping(11, TorznabCatType.Movies, "Movie Boxsets");
            AddCategoryMapping(12, TorznabCatType.Movies, "Movies");
            AddCategoryMapping(100, TorznabCatType.MoviesHD, "Movies HEVC");
            AddCategoryMapping(13, TorznabCatType.Audio, "Music");
            AddCategoryMapping(15, TorznabCatType.AudioVideo, "Music Videos");
            AddCategoryMapping(32, TorznabCatType.ConsoleNDS, "NDS Games");
            AddCategoryMapping(9, TorznabCatType.Other, "Other");
            AddCategoryMapping(6, TorznabCatType.PCGames, "PC Games");
            AddCategoryMapping(45, TorznabCatType.Other, "Pictures");
            AddCategoryMapping(31, TorznabCatType.ConsolePS4, "Playstation");
            AddCategoryMapping(71, TorznabCatType.TV, "PPV");
            AddCategoryMapping(54, TorznabCatType.TV, "Soaps");
            AddCategoryMapping(20, TorznabCatType.TVSport, "Sports");
            AddCategoryMapping(102, TorznabCatType.TVSport, "Sports FIFA World Cup");
            AddCategoryMapping(86, TorznabCatType.TVSport, "Sports MotorSports");
            AddCategoryMapping(89, TorznabCatType.TVSport, "Sports Olympics");
            AddCategoryMapping(88, TorznabCatType.TVSport, "Sports UK Football");
            AddCategoryMapping(83, TorznabCatType.Movies, "TOTM");
            AddCategoryMapping(21, TorznabCatType.TVSD, "TV Boxsets");
            AddCategoryMapping(76, TorznabCatType.TVHD, "TV HD Boxsets");
            AddCategoryMapping(97, TorznabCatType.TVHD, "TV HECV Boxsets");
            AddCategoryMapping(47, TorznabCatType.TVHD, "TV HD");
            AddCategoryMapping(96, TorznabCatType.TVHD, "TV HD HEVC");
            AddCategoryMapping(16, TorznabCatType.TVSD, "TV SD");
            AddCategoryMapping(7, TorznabCatType.ConsoleWii, "Wii Games");
            AddCategoryMapping(43, TorznabCatType.TVSport, "Wrestling");
            AddCategoryMapping(8, TorznabCatType.ConsoleXBox, "Xbox Games");

            // RSS Textual categories
            AddCategoryMapping("4K Movies", TorznabCatType.MoviesUHD);
            AddCategoryMapping("4K TV", TorznabCatType.TVUHD);
            AddCategoryMapping("4K TV Boxsets", TorznabCatType.TVUHD);
            AddCategoryMapping("Anime", TorznabCatType.TVAnime);
            AddCategoryMapping("Audiobooks", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("Blu-Ray", TorznabCatType.MoviesBluRay);
            AddCategoryMapping("Books Magazines", TorznabCatType.Books);
            AddCategoryMapping("Cams/TS", TorznabCatType.MoviesOther);
            AddCategoryMapping("Documentaries", TorznabCatType.TVDocumentary);
            AddCategoryMapping("DVDR", TorznabCatType.MoviesDVD);
            AddCategoryMapping("Foreign", TorznabCatType.MoviesForeign);
            AddCategoryMapping("Kids", TorznabCatType.TVOther);
            AddCategoryMapping("MMA", TorznabCatType.TVSport);
            AddCategoryMapping("Movie Boxsets", TorznabCatType.Movies);
            AddCategoryMapping("Movies", TorznabCatType.Movies);
            AddCategoryMapping("Music", TorznabCatType.Audio);
            AddCategoryMapping("Music Videos", TorznabCatType.AudioVideo);
            AddCategoryMapping("NDS Games", TorznabCatType.ConsoleNDS);
            AddCategoryMapping("Other", TorznabCatType.Other);
            AddCategoryMapping("PC Games", TorznabCatType.PCGames);
            AddCategoryMapping("Pictures", TorznabCatType.Other);
            AddCategoryMapping("Playstation", TorznabCatType.ConsolePS4);
            AddCategoryMapping("PPV", TorznabCatType.TV);
            AddCategoryMapping("Soaps", TorznabCatType.TV);
            AddCategoryMapping("Sports", TorznabCatType.TVSport);
            AddCategoryMapping("FIFA World Cup", TorznabCatType.TVSport);
            AddCategoryMapping("MotorSports", TorznabCatType.TVSport);
            AddCategoryMapping("Olympics", TorznabCatType.TVSport);
            AddCategoryMapping("UK Football", TorznabCatType.TVSport);
            AddCategoryMapping("TOTM", TorznabCatType.Movies);
            AddCategoryMapping("TV Boxsets", TorznabCatType.TVSD);
            AddCategoryMapping("HD Boxsets", TorznabCatType.TVHD);
            AddCategoryMapping("TV-HD", TorznabCatType.TVHD);
            AddCategoryMapping("TV-SD", TorznabCatType.TVSD);
            AddCategoryMapping("Wii Games", TorznabCatType.ConsoleWii);
            AddCategoryMapping("Wrestling", TorznabCatType.TVSport);
            AddCategoryMapping("Xbox Games", TorznabCatType.ConsoleXBox);

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
