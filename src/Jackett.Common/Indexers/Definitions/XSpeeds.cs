using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
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
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;

namespace Jackett.Common.Indexers.Definitions
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
        private string SearchUrl => SiteLink + "browse.php";
        private readonly Regex _dateMatchRegex = new Regex(@"\d{2}-\d{2}-\d{4} \d{2}:\d{2}", RegexOptions.Compiled);

        private new ConfigurationDataBasicLogin configData =>
            (ConfigurationDataBasicLogin)base.configData;

        public XSpeeds(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps, ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataBasicLogin())
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

            configData.AddDynamic("freeleech", new BoolConfigurationItem("Filter freeleech only") { Value = false });
            configData.AddDynamic("Account Inactivity", new DisplayInfoConfigurationItem("Account Inactivity", "Accounts with no activity for 30 days will automatically be deleted. Note: The activity has to be a login on site, meaning that download client, IRC and Discord are NOT counted as site activity. If you have to be away from site for a prolonged time, you can park your account up to 60 days in the User CP area."));
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

            caps.Categories.AddCategoryMapping(70, TorznabCatType.TVAnime, "Anime");
            caps.Categories.AddCategoryMapping(113, TorznabCatType.TVAnime, "Anime Boxsets");
            caps.Categories.AddCategoryMapping(112, TorznabCatType.MoviesOther, "Anime Movies");
            caps.Categories.AddCategoryMapping(111, TorznabCatType.MoviesOther, "Anime TV");
            caps.Categories.AddCategoryMapping(150, TorznabCatType.PC, "Apps");
            caps.Categories.AddCategoryMapping(156, TorznabCatType.TV, "AV1");
            caps.Categories.AddCategoryMapping(156, TorznabCatType.Movies, "AV1");
            caps.Categories.AddCategoryMapping(159, TorznabCatType.Movies, "Movie Boxsets AV1");
            caps.Categories.AddCategoryMapping(158, TorznabCatType.Movies, "Movies AV1");
            caps.Categories.AddCategoryMapping(157, TorznabCatType.TV, "TV AV1");
            caps.Categories.AddCategoryMapping(160, TorznabCatType.TV, "TV Boxsets AV1");
            caps.Categories.AddCategoryMapping(153, TorznabCatType.Books, "Books");
            caps.Categories.AddCategoryMapping(154, TorznabCatType.AudioAudiobook, "Audiobooks");
            caps.Categories.AddCategoryMapping(155, TorznabCatType.Books, "Books & Magazines");
            caps.Categories.AddCategoryMapping(68, TorznabCatType.MoviesOther, "Cams/TS");
            caps.Categories.AddCategoryMapping(140, TorznabCatType.TVDocumentary, "Documentary");
            caps.Categories.AddCategoryMapping(10, TorznabCatType.MoviesDVD, "DVDR");
            caps.Categories.AddCategoryMapping(109, TorznabCatType.MoviesBluRay, "Bluray Disc");
            caps.Categories.AddCategoryMapping(131, TorznabCatType.TVSport, "Fighting");
            caps.Categories.AddCategoryMapping(134, TorznabCatType.TVSport, "Fighting/Boxing");
            caps.Categories.AddCategoryMapping(133, TorznabCatType.TVSport, "Fighting/MMA");
            caps.Categories.AddCategoryMapping(132, TorznabCatType.TVSport, "Fighting/Wrestling");
            caps.Categories.AddCategoryMapping(72, TorznabCatType.MoviesForeign, "Foreign");
            caps.Categories.AddCategoryMapping(116, TorznabCatType.TVForeign, "Foreign Boxsets");
            caps.Categories.AddCategoryMapping(114, TorznabCatType.MoviesForeign, "Foreign Movies");
            caps.Categories.AddCategoryMapping(115, TorznabCatType.TVForeign, "Foreign TV");
            caps.Categories.AddCategoryMapping(103, TorznabCatType.ConsoleOther, "Games Console");
            caps.Categories.AddCategoryMapping(105, TorznabCatType.ConsoleOther, "Games Console/Nintendo");
            caps.Categories.AddCategoryMapping(104, TorznabCatType.ConsolePS4, "Games Console/Playstation");
            caps.Categories.AddCategoryMapping(106, TorznabCatType.ConsoleXBox, "Games Console/XBOX");
            caps.Categories.AddCategoryMapping(6, TorznabCatType.PCGames, "Games PC");
            caps.Categories.AddCategoryMapping(108, TorznabCatType.PC, "Games PC/Linux");
            caps.Categories.AddCategoryMapping(107, TorznabCatType.PCMac, "Games PC/Mac");
            caps.Categories.AddCategoryMapping(11, TorznabCatType.Movies, "Movie Boxsets");
            caps.Categories.AddCategoryMapping(118, TorznabCatType.MoviesUHD, "Movie Boxsets/Boxset 4K");
            caps.Categories.AddCategoryMapping(143, TorznabCatType.MoviesHD, "Movie Boxsets/Boxset HD");
            caps.Categories.AddCategoryMapping(119, TorznabCatType.MoviesHD, "Movie Boxsets/Boxset HEVC");
            caps.Categories.AddCategoryMapping(144, TorznabCatType.MoviesSD, "Movie Boxsets/Boxset SD");
            caps.Categories.AddCategoryMapping(12, TorznabCatType.Movies, "Movies");
            caps.Categories.AddCategoryMapping(117, TorznabCatType.MoviesUHD, "Movies 4K");
            caps.Categories.AddCategoryMapping(145, TorznabCatType.MoviesHD, "Movies HD");
            caps.Categories.AddCategoryMapping(100, TorznabCatType.MoviesHD, "Movies HEVC");
            caps.Categories.AddCategoryMapping(146, TorznabCatType.MoviesSD, "Movies SD");
            caps.Categories.AddCategoryMapping(13, TorznabCatType.Audio, "Music");
            caps.Categories.AddCategoryMapping(135, TorznabCatType.AudioLossless, "Music/FLAC");
            caps.Categories.AddCategoryMapping(151, TorznabCatType.Audio, "Karaoke");
            caps.Categories.AddCategoryMapping(136, TorznabCatType.Audio, "Music Boxset");
            caps.Categories.AddCategoryMapping(148, TorznabCatType.AudioVideo, "Music Videos");
            caps.Categories.AddCategoryMapping(9, TorznabCatType.Other, "Other");
            caps.Categories.AddCategoryMapping(125, TorznabCatType.Other, "Other/Pictures");
            caps.Categories.AddCategoryMapping(54, TorznabCatType.TVOther, "Soaps");
            caps.Categories.AddCategoryMapping(83, TorznabCatType.TVOther, "Specials");
            caps.Categories.AddCategoryMapping(139, TorznabCatType.TV, "TOTM (Freeleech)");
            caps.Categories.AddCategoryMapping(138, TorznabCatType.TV, "TOTW (x2 upload)");
            caps.Categories.AddCategoryMapping(139, TorznabCatType.Movies, "TOTM (Freeleech)");
            caps.Categories.AddCategoryMapping(138, TorznabCatType.Movies, "TOTW (x2 upload)");
            caps.Categories.AddCategoryMapping(20, TorznabCatType.TVSport, "Sports");
            caps.Categories.AddCategoryMapping(88, TorznabCatType.TVSport, "Sports/Football");
            caps.Categories.AddCategoryMapping(86, TorznabCatType.TVSport, "Sports/MotorSports");
            caps.Categories.AddCategoryMapping(89, TorznabCatType.TVSport, "Sports/Olympics");
            caps.Categories.AddCategoryMapping(126, TorznabCatType.TV, "TV");
            caps.Categories.AddCategoryMapping(149, TorznabCatType.TV, "TV Specials");
            caps.Categories.AddCategoryMapping(127, TorznabCatType.TVUHD, "TV 4K");
            caps.Categories.AddCategoryMapping(129, TorznabCatType.TVHD, "TV HD");
            caps.Categories.AddCategoryMapping(130, TorznabCatType.TVHD, "TV HEVC");
            caps.Categories.AddCategoryMapping(128, TorznabCatType.TVSD, "TV SD");
            caps.Categories.AddCategoryMapping(21, TorznabCatType.TVSD, "TV Boxsets");
            caps.Categories.AddCategoryMapping(120, TorznabCatType.TVUHD, "Boxset TV 4K");
            caps.Categories.AddCategoryMapping(76, TorznabCatType.TVHD, "Boxset TV HD");
            caps.Categories.AddCategoryMapping(97, TorznabCatType.TVHD, "Boxset TV HEVC");
            caps.Categories.AddCategoryMapping(147, TorznabCatType.TVSD, "Boxset TV SD");

            return caps;
        }

        private string GetSortBy => ((SingleSelectConfigurationItem)configData.GetDynamic("sortrequestedfromsite")).Value;

        private string GetOrder => ((SingleSelectConfigurationItem)configData.GetDynamic("orderrequestedfromsite")).Value;

        public override async Task<ConfigurationData> GetConfigurationForSetup()
        {
            var loginPage = await RequestWithCookiesAsync(LandingUrl);
            var parser = new HtmlParser();
            using var dom = parser.ParseDocument(loginPage.ContentString);
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
            {
                logger.Debug($"{Id}: No captcha image found");
            }

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
            {
                pairs.Add("imagestring", captchaText.Value);
            }

            //var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, SiteLink, true);
            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, SearchUrl, LandingUrl, true);
            await ConfigureIfOK(result.Cookies, result.ContentString?.Contains("logout.php") == true, () =>
            {
                var parser = new HtmlParser();
                using var dom = parser.ParseDocument(result.ContentString);

                var errorMessage = dom.QuerySelector(".left_side table:nth-of-type(1) tr:nth-of-type(2)")?.TextContent.Trim().Replace("\n\t", " ");
                if (errorMessage.IsNullOrWhiteSpace())
                {
                    errorMessage = dom.QuerySelector("div.notification-body")?.TextContent.Trim().Replace("\n\t", " ");
                }

                throw new ExceptionWithConfigData(errorMessage ?? "Login failed.", configData);
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var prevCook = CookieHeader + "";

            var categoryMapping = MapTorznabCapsToTrackers(query).Distinct().ToList();

            var searchParams = new Dictionary<string, string>
            {
                { "do", "search" },
                { "category", categoryMapping.FirstIfSingleOrDefault("0") }, // multi category search not supported
                { "include_dead_torrents", "yes" }
            };

            if (((BoolConfigurationItem)configData.GetDynamic("freeleech")).Value)
            {
                searchParams.Add("sort", "free");
                searchParams.Add("order", "desc");
            }
            else
            {
                searchParams.Add("sort", GetSortBy);
                searchParams.Add("order", GetOrder);
            }

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
                using var dom = parser.ParseDocument(searchPage.ContentString);
                var rows = dom.QuerySelectorAll("table#sortabletable > tbody > tr:has(div > a[href*=\"details.php?id=\"])");
                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();

                    if (row.QuerySelector("img[alt^=\"Free Torrent\"], img[alt^=\"Sitewide Free Torrent\"]") != null)
                        release.DownloadVolumeFactor = 0;
                    else if (row.QuerySelector("img[alt^=\"Silver Torrent\"]") != null)
                        release.DownloadVolumeFactor = 0.5;
                    else
                        release.DownloadVolumeFactor = 1;
                    if (((BoolConfigurationItem)configData.GetDynamic("freeleech")).Value &&
                        release.DownloadVolumeFactor != 0)
                        continue;
                    release.UploadVolumeFactor = row.QuerySelector("img[alt^=\"x2 Torrent\"]") != null ? 2 : 1;

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
