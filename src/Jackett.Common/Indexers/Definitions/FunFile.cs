using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
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

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class FunFile : IndexerBase
    {
        public override string Id => "funfile";
        public override string Name => "FunFile";
        public override string Description => "A general tracker";
        public override string SiteLink { get; protected set; } = "https://www.funfile.org/";
        public override Encoding Encoding => Encoding.GetEncoding("iso-8859-1");
        public override string Language => "en-US";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private string LoginUrl => SiteLink + "takelogin.php";
        private string SearchUrl => SiteLink + "browse.php";

        private new ConfigurationDataBasicLogin configData => (ConfigurationDataBasicLogin)base.configData;

        public FunFile(IIndexerConfigurationService configService, WebClient w, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(configService: configService,
                   client: w,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataBasicLogin("For best results, change the 'Torrents per page' setting to 100 in your profile."))
        {
            configData.AddDynamic("freeleech", new BoolConfigurationItem("Search freeleech only") { Value = false });
            configData.AddDynamic("Account Inactivity", new DisplayInfoConfigurationItem("Account Inactivity", "User accounts that are inactive for more than 42 days (ie haven't logged into the site) and NOT PARKED are automatically AND irretrievably deleted. Those with Donor VIP status are immune to the 42 day purging up to 1 year."));
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId, TvSearchParam.Genre
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q, MovieSearchParam.ImdbId, MovieSearchParam.Genre
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

            caps.Categories.AddCategoryMapping(44, TorznabCatType.TVAnime, "Anime");
            caps.Categories.AddCategoryMapping(22, TorznabCatType.PC, "Applications");
            caps.Categories.AddCategoryMapping(43, TorznabCatType.AudioAudiobook, "Audio Books");
            caps.Categories.AddCategoryMapping(27, TorznabCatType.Books, "Ebook");
            caps.Categories.AddCategoryMapping(4, TorznabCatType.PCGames, "Games");
            caps.Categories.AddCategoryMapping(40, TorznabCatType.OtherMisc, "Miscellaneous");
            caps.Categories.AddCategoryMapping(19, TorznabCatType.Movies, "Movies");
            caps.Categories.AddCategoryMapping(6, TorznabCatType.Audio, "Music");
            caps.Categories.AddCategoryMapping(31, TorznabCatType.PCMobileOther, "Portable");
            caps.Categories.AddCategoryMapping(49, TorznabCatType.Other, "Tutorials");
            caps.Categories.AddCategoryMapping(7, TorznabCatType.TV, "TV");

            return caps;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "returnto", "" },
                { "login", "Login" }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, LoginUrl);
            await ConfigureIfOK(result.Cookies, result.ContentString?.Contains("logout.php") == true, () =>
            {
                var parser = new HtmlParser();
                using var dom = parser.ParseDocument(result.ContentString);
                var errorMessage = dom.QuerySelector("td.mf_content").TextContent;
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            /*
             * notes:
             * can search titles & filesnames with &s_title=1 (default)
             * and tags with &s_tag=1
             * and descriptions with &s_desc=1
             * however the parms are used in an OR fashion and not as an AND
             * so ?search=reality+love+s04e19&s_title=1&s_tag=1
             * will find Love Island S04E19 as well as My Kitchen Rules S12E02
             * since the former has a match for Love in the title, and the latter has a tag for Reality-TV.
             *
             * FunFiles only has genres for movies and tv
             */

            var validList = new List<string>
            {
                "action",
                "adventure",
                "animation",
                "biography",
                "comedy",
                "crime",
                "documentary",
                "drama",
                "family",
                "fantasy",
                "game-show",
                "history",
                "home_&_garden",
                "home_and_garden",
                "horror",
                "music",
                "musical",
                "mystery",
                "news",
                "reality",
                "reality-tv",
                "romance",
                "sci-fi",
                "science-fiction",
                "short",
                "sport",
                "talk-show",
                "thriller",
                "travel",
                "war",
                "western"
            };

            var qc = new NameValueCollection
            {
                { "cat", "0" },
                // incldead= 0 active, 1 incldead, 2 deadonly, 3 freeleech, 4 sceneonly, 5 requestsonly, 8 packsonly
                { "incldead", ((BoolConfigurationItem)configData.GetDynamic("freeleech")).Value ? "3" : "1" },
                { "showspam", "1" },
                { "s_title", "1" }
            };

            var queryCats = MapTorznabCapsToTrackers(query);
            queryCats.ForEach(cat => qc.Set($"c{cat}", "1"));

            if (query.IsImdbQuery)
            {
                qc.Set("search", query.ImdbID);
                qc.Set("s_desc", "1");
            }
            else if (query.IsGenreQuery)
            {
                qc.Set("search", query.Genre + " " + query.GetQueryString());
                qc.Set("s_tag", "1");
            }
            else
                qc.Set("search", query.GetQueryString());

            var searchUrl = SearchUrl + "?" + qc.GetQueryString();
            var results = await RequestWithCookiesAndRetryAsync(searchUrl);

            if (results.IsRedirect) // re-login
            {
                await ApplyConfiguration(null);
                results = await RequestWithCookiesAndRetryAsync(searchUrl);
            }

            char[] delimiters = { ',', ' ', '/', ')', '(', '.', ';', '[', ']', '"', '|', ':' };

            try
            {
                var parser = new HtmlParser();
                using var dom = parser.ParseDocument(results.ContentString);

                var rows = dom.QuerySelectorAll("table.mainframe table[cellpadding=\"2\"] > tbody > tr:has(td.row3)");
                foreach (var row in rows)
                {
                    var qDownloadLink = row.QuerySelector("a[href^=\"download.php\"]");
                    if (qDownloadLink == null)
                        throw new Exception("Download links not found. Make sure you can download from the website.");

                    var link = new Uri(SiteLink + qDownloadLink.GetAttribute("href"));

                    var qDetailsLink = row.QuerySelector("a[href^=\"details.php?id=\"]");
                    var title = qDetailsLink?.GetAttribute("title")?.Trim();
                    var details = new Uri(SiteLink + qDetailsLink?.GetAttribute("href")?.Replace("&hit=1", ""));

                    var categoryLink = row.QuerySelector("a[href^=\"browse.php?cat=\"]")?.GetAttribute("href");
                    var cat = ParseUtil.GetArgumentFromQueryString(categoryLink, "cat");

                    var seeders = ParseUtil.CoerceInt(row.Children[9].TextContent);
                    var leechers = ParseUtil.CoerceInt(row.Children[10].TextContent);

                    var release = new ReleaseInfo
                    {
                        Guid = link,
                        Link = link,
                        Details = details,
                        Title = title,
                        Category = MapTrackerCatToNewznab(cat),
                        Size = ParseUtil.GetBytes(row.Children[7].TextContent),
                        Files = ParseUtil.CoerceInt(row.Children[3].TextContent),
                        Grabs = ParseUtil.CoerceInt(row.Children[8].TextContent),
                        Seeders = seeders,
                        Peers = leechers + seeders,
                        PublishDate = DateTimeUtil.FromTimeAgo(row.Children[5].TextContent),
                        DownloadVolumeFactor = 1,
                        UploadVolumeFactor = 1,
                        MinimumRatio = 1,
                        MinimumSeedTime = 172800 // 48 hours
                    };

                    var nextRow = row.NextElementSibling;
                    if (nextRow != null)
                    {
                        var qStats = nextRow.QuerySelector("table > tbody > tr:nth-child(3)");
                        release.UploadVolumeFactor = ParseUtil.CoerceDouble(qStats?.Children[0].TextContent.Replace("X", ""));
                        release.DownloadVolumeFactor = ParseUtil.CoerceDouble(qStats?.Children[1].TextContent.Replace("X", ""));

                        release.Description = nextRow.QuerySelector("span[style=\"float:left\"]")?.TextContent.Trim();
                        var genres = release.Description.ToLower().Replace(" & ", "_&_").Replace(" and ", "_and_");

                        var releaseGenres = validList.Intersect(genres.Split(delimiters, StringSplitOptions.RemoveEmptyEntries));
                        release.Genres = releaseGenres.Select(x => x.Trim().Replace("_", " ")).ToList();
                    }

                    releases.Add(release);
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
