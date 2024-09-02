using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Jackett.Common.Extensions;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class BakaBT : IndexerBase
    {
        public override string Id => "bakabt";
        public override string Name => "BakaBT";
        public override string Description => "Anime Comunity";
        public override string SiteLink { get; protected set; } = "https://bakabt.me/";
        public override string Language => "en-US";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private string SearchUrl => SiteLink + "browse.php?only=0&hentai=1&incomplete=1&lossless=1&hd=1&multiaudio=1&bonus=1&reorder=1&q=";
        private string LoginUrl => SiteLink + "login.php";
        private bool AddRomajiTitle => configData.AddRomajiTitle.Value;
        private bool AppendSeason => configData.AppendSeason.Value;

        private readonly List<int> defaultCategories = new List<int> { TorznabCatType.TVAnime.ID };

        private new ConfigurationDataBakaBT configData
        {
            get => (ConfigurationDataBakaBT)base.configData;
            set => base.configData = value;
        }

        public BakaBT(IIndexerConfigurationService configService, Utils.Clients.WebClient wc, Logger l,
            IProtectionService ps, ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataBakaBT("To show more results and prevent a 0 results error, go to your BakaBT account settings and in the 'Browse' section enable the 'Show Adult Content' option and change 'Torrents per page' to '100'."))
        {
            configData.AddDynamic("Account Inactivity", new DisplayInfoConfigurationItem("Account Inactivity", "Accounts are pruned after 11 months of disuse (i.e. not logging into the tracker account). Accounts with 0 download and 0 upload are pruned earlier. Donator accounts (people who've donated to BakaBT and received a donator status) have the option of disabling inactivity pruning, and safeguarding their account for as long as they choose."));
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
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

            caps.Categories.AddCategoryMapping(1, TorznabCatType.TVAnime, "Anime Series");
            caps.Categories.AddCategoryMapping(2, TorznabCatType.TVAnime, "OVA");
            caps.Categories.AddCategoryMapping(3, TorznabCatType.AudioOther, "Soundtrack");
            caps.Categories.AddCategoryMapping(4, TorznabCatType.BooksComics, "Manga");
            caps.Categories.AddCategoryMapping(5, TorznabCatType.TVAnime, "Anime Movie");
            caps.Categories.AddCategoryMapping(6, TorznabCatType.TVOther, "Live Action");
            caps.Categories.AddCategoryMapping(7, TorznabCatType.BooksOther, "Artbook");
            caps.Categories.AddCategoryMapping(8, TorznabCatType.AudioVideo, "Music Video");
            caps.Categories.AddCategoryMapping(9, TorznabCatType.BooksEBook, "Light Novel");
            caps.Categories.AddCategoryMapping(11, TorznabCatType.XXX, "Hentai Series");
            caps.Categories.AddCategoryMapping(12, TorznabCatType.XXX, "Hentai OVA");
            caps.Categories.AddCategoryMapping(13, TorznabCatType.XXX, "Hentai Soundtrack");
            caps.Categories.AddCategoryMapping(14, TorznabCatType.XXX, "Hentai Manga");
            caps.Categories.AddCategoryMapping(15, TorznabCatType.XXX, "Hentai Movie");
            caps.Categories.AddCategoryMapping(16, TorznabCatType.XXX, "Hentai Live Action");
            caps.Categories.AddCategoryMapping(17, TorznabCatType.XXX, "Hentai Artbook");
            caps.Categories.AddCategoryMapping(18, TorznabCatType.XXX, "Hentai Music Video");
            caps.Categories.AddCategoryMapping(19, TorznabCatType.XXX, "Hentai Light Novel");

            return caps;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            await DoLogin();
            return IndexerConfigurationStatus.RequiresTesting;
        }

        private async Task DoLogin()
        {
            var loginForm = await webclient.GetResultAsync(new Utils.Clients.WebRequest
            {
                Url = LoginUrl,
                Type = RequestType.GET
            });

            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "returnto", "/index.php" }
            };

            var htmlParser = new HtmlParser();
            using var dom = htmlParser.ParseDocument(loginForm.ContentString);

            var loginKey = dom.QuerySelector("input[name=\"loginKey\"]");
            if (loginKey != null)
            {
                pairs["loginKey"] = loginKey.GetAttribute("value");
            }

            var response = await RequestLoginAndFollowRedirect(LoginUrl, pairs, loginForm.Cookies, true, null, SiteLink);

            await ConfigureIfOK(response.Cookies, response.ContentString != null && !response.ContentString.Contains("loginForm"), () =>
            {
                using var document = htmlParser.ParseDocument(response.ContentString);
                var errorMessage = document.QuerySelector("#loginError, .error")?.Text().Trim();

                throw new ExceptionWithConfigData(errorMessage ?? "Login failed.", configData);
            });
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var searchString = query.SanitizedSearchTerm;

            var match = Regex.Match(query.SanitizedSearchTerm, @".*(?=\s(?:[Ee]\d+|\d+)$)");
            if (match.Success)
                searchString = match.Value;

            var releases = new List<ReleaseInfo>();
            var episodeSearchUrl = SearchUrl + WebUtility.UrlEncode(searchString);

            var response = await RequestWithCookiesAndRetryAsync(episodeSearchUrl);
            if (response.ContentString.Contains("loginForm"))
            {
                //Cookie appears to expire after a period of time or logging in to the site via browser
                await DoLogin();
                response = await RequestWithCookiesAndRetryAsync(episodeSearchUrl);
            }

            try
            {
                var parser = new HtmlParser();
                using var dom = parser.ParseDocument(response.ContentString);
                var rows = dom.QuerySelectorAll(".torrents tr.torrent, .torrents tr.torrent_alt");
                ICollection<int> currentCategories = new List<int> { TorznabCatType.TVAnime.ID };

                foreach (var row in rows)
                {
                    var downloadVolumeFactor = row.QuerySelector("span.freeleech") != null ? 0 : 1;

                    // Skip non-freeleech results when freeleech only is set
                    if (configData.FreeleechOnly.Value && downloadVolumeFactor != 0)
                    {
                        continue;
                    }

                    var qTitleLink = row.QuerySelector("a.title, a.alt_title");
                    if (qTitleLink == null)
                        continue;

                    var title = qTitleLink.TextContent.Trim();

                    // Insert before the release info
                    var taidx = title.IndexOf('(');
                    var tbidx = title.IndexOf('[');

                    if (taidx == -1)
                        taidx = title.Length;

                    if (tbidx == -1)
                        tbidx = title.Length;
                    var titleSplit = Math.Min(taidx, tbidx);
                    var titleSeries = title.Substring(0, titleSplit);
                    var releaseInfo = title.Substring(titleSplit);

                    currentCategories = GetNextCategory(row, currentCategories);

                    var stringSeparator = new[] { " | " };
                    var titles = titleSeries.Split(stringSeparator, StringSplitOptions.RemoveEmptyEntries);

                    if (titles.Length > 1 && !AddRomajiTitle)
                    {
                        titles = titles.Skip(1).ToArray();
                    }

                    foreach (var name in titles)
                    {
                        var release = new ReleaseInfo
                        {
                            Title = (name + releaseInfo).Trim()
                        };
                        // Ensure the season is defined as this tracker only deals with full seasons
                        if (release.Title.IndexOf("Season") == -1 && AppendSeason)
                        {
                            // Insert before the release info
                            var aidx = release.Title.IndexOf('(');
                            var bidx = release.Title.IndexOf('[');

                            if (aidx == -1)
                                aidx = release.Title.Length;

                            if (bidx == -1)
                                bidx = release.Title.Length;

                            var insertPoint = Math.Min(aidx, bidx);
                            release.Title = release.Title.Substring(0, insertPoint) + " Season 1 " + release.Title.Substring(insertPoint);
                        }

                        release.Category = currentCategories;
                        release.Description = row.QuerySelector("span.tags")?.TextContent;
                        release.Guid = new Uri(SiteLink + qTitleLink.GetAttribute("href"));
                        release.Details = release.Guid;

                        release.Link = new Uri(SiteLink + row.QuerySelector(".peers a").GetAttribute("href"));

                        var grabs = row.QuerySelectorAll(".peers")[0].FirstChild.NodeValue.TrimEnd().TrimEnd('/').TrimEnd();
                        grabs = grabs.Replace("k", "000");
                        release.Grabs = int.Parse(grabs);
                        release.Seeders = int.Parse(row.QuerySelectorAll(".peers a")[0].TextContent);
                        release.Peers = release.Seeders + int.Parse(row.QuerySelectorAll(".peers a")[1].TextContent);

                        release.MinimumRatio = 0.5;

                        var size = row.QuerySelector(".size").TextContent;
                        release.Size = ParseUtil.GetBytes(size);

                        //22 Jul 15
                        var dateStr = row.QuerySelector(".added").TextContent.Replace("'", string.Empty);
                        if (dateStr.Split(' ')[0].Length == 1)
                            dateStr = "0" + dateStr;

                        if (string.Equals(dateStr, "yesterday", StringComparison.InvariantCultureIgnoreCase))
                            release.PublishDate = DateTime.Now.AddDays(-1);
                        else if (string.Equals(dateStr, "today", StringComparison.InvariantCultureIgnoreCase))
                            release.PublishDate = DateTime.Now;
                        else
                            release.PublishDate = DateTime.ParseExact(dateStr, "dd MMM yy", CultureInfo.InvariantCulture);

                        release.DownloadVolumeFactor = downloadVolumeFactor;
                        release.UploadVolumeFactor = 1;

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

        private ICollection<int> GetNextCategory(IElement row, ICollection<int> currentCategories)
        {
            var nextCategoryName = GetCategoryName(row);
            if (nextCategoryName != null)
            {
                currentCategories = MapTrackerCatDescToNewznab(nextCategoryName);
                if (currentCategories.Count == 0)
                    return defaultCategories;
            }

            return currentCategories;
        }

        private string GetCategoryName(IElement row)
        {
            var categoryElement = row.QuerySelector("td.category span");
            if (categoryElement == null)
            {
                return null;
            }

            var categoryName = categoryElement.GetAttribute("title");

            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                var hentaiIcon = row.QuerySelector("td.name span.hentai");
                if (hentaiIcon == null)
                    return categoryName;

                if (!categoryName.StartsWith("Anime"))
                {
                    categoryName = "Hentai " + categoryName;
                    return categoryName;
                }

                categoryName = categoryName.Replace("Anime", "Hentai");
                return categoryName;
            }

            return null;
        }

        public override async Task<byte[]> Download(Uri link)
        {
            var downloadPage = await RequestWithCookiesAsync(link.ToString());

            var parser = new HtmlParser();
            using var dom = parser.ParseDocument(downloadPage.ContentString);

            var downloadLink = dom.QuerySelector(".download_link")?.GetAttribute("href");

            if (downloadLink.IsNullOrWhiteSpace())
            {
                throw new Exception("Unable to find download link.");
            }

            var response = await RequestWithCookiesAsync(SiteLink + downloadLink);

            return response.ContentBytes;
        }
    }
}
