using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class RevolutionTT : IndexerBase
    {
        public override string Id => "revolutiontt";
        public override string Name => "RevolutionTT";
        public override string Description => "The Revolution has begun";
        public override string SiteLink { get; protected set; } = "https://revolutiontt.me/";
        public override Encoding Encoding => Encoding.GetEncoding("iso-8859-1");
        public override string Language => "en-US";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private string LandingPageURL => SiteLink + "login.php";
        private string LoginUrl => SiteLink + "takelogin.php";
        private string SearchUrl => SiteLink + "browse.php";

        private new ConfigurationDataBasicLogin configData => (ConfigurationDataBasicLogin)base.configData;

        public RevolutionTT(IIndexerConfigurationService configService, Utils.Clients.WebClient wc, Logger l,
            IProtectionService ps, ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataBasicLogin("For best results, change the 'Torrents per page' setting to 100 in your Profile."))
        {
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
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

            caps.Categories.AddCategoryMapping("23", TorznabCatType.TVAnime);
            caps.Categories.AddCategoryMapping("22", TorznabCatType.PC0day);
            caps.Categories.AddCategoryMapping("1", TorznabCatType.PCISO);
            caps.Categories.AddCategoryMapping("36", TorznabCatType.Books);
            caps.Categories.AddCategoryMapping("36", TorznabCatType.BooksEBook);
            caps.Categories.AddCategoryMapping("4", TorznabCatType.PCGames);
            caps.Categories.AddCategoryMapping("21", TorznabCatType.PCGames);
            caps.Categories.AddCategoryMapping("16", TorznabCatType.ConsolePS3);
            caps.Categories.AddCategoryMapping("40", TorznabCatType.ConsoleWii);
            caps.Categories.AddCategoryMapping("39", TorznabCatType.ConsoleXBox360);
            caps.Categories.AddCategoryMapping("35", TorznabCatType.ConsoleNDS);
            caps.Categories.AddCategoryMapping("34", TorznabCatType.ConsolePSP);
            caps.Categories.AddCategoryMapping("2", TorznabCatType.PCMac);
            caps.Categories.AddCategoryMapping("10", TorznabCatType.MoviesBluRay);
            caps.Categories.AddCategoryMapping("20", TorznabCatType.MoviesDVD);
            caps.Categories.AddCategoryMapping("12", TorznabCatType.MoviesHD);
            caps.Categories.AddCategoryMapping("44", TorznabCatType.MoviesOther);
            caps.Categories.AddCategoryMapping("11", TorznabCatType.MoviesSD);
            caps.Categories.AddCategoryMapping("19", TorznabCatType.MoviesSD);
            caps.Categories.AddCategoryMapping("6", TorznabCatType.Audio);
            caps.Categories.AddCategoryMapping("8", TorznabCatType.AudioLossless);
            caps.Categories.AddCategoryMapping("46", TorznabCatType.AudioOther);
            caps.Categories.AddCategoryMapping("29", TorznabCatType.AudioVideo);
            caps.Categories.AddCategoryMapping("43", TorznabCatType.TVOther);
            caps.Categories.AddCategoryMapping("42", TorznabCatType.TVHD);
            caps.Categories.AddCategoryMapping("45", TorznabCatType.TVOther);
            caps.Categories.AddCategoryMapping("41", TorznabCatType.TVSD);
            caps.Categories.AddCategoryMapping("7", TorznabCatType.TVSD);
            caps.Categories.AddCategoryMapping("9", TorznabCatType.XXX);
            caps.Categories.AddCategoryMapping("49", TorznabCatType.XXX);
            caps.Categories.AddCategoryMapping("47", TorznabCatType.XXXDVD);
            caps.Categories.AddCategoryMapping("48", TorznabCatType.XXX);

            return caps;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value }
            };

            //  need to do an initial request to get PHP session cookie (any better way to do this?)
            var homePageLoad = await RequestLoginAndFollowRedirect(LandingPageURL, new Dictionary<string, string>(), null, true, null, SiteLink);

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, homePageLoad.Cookies, true, null, LandingPageURL);
            await ConfigureIfOK(result.Cookies, result.ContentString?.Contains("/logout.php") == true, () =>
                throw new ExceptionWithConfigData("Login failed! Check the username and password. If they are ok, try logging on the website.", configData));

            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var qc = new NameValueCollection
            {
                {"incldead", "1"}
            };

            if (query.IsImdbQuery)
            {
                qc.Add("titleonly", "0");
                qc.Add("search", query.ImdbID);
            }
            else
            {
                qc.Add("titleonly", "1");
                qc.Add("search", query.GetQueryString());
            }

            var cats = MapTorznabCapsToTrackers(query);
            if (cats.Count > 0)
                foreach (var cat in cats)
                    qc.Add($"c{cat}", "1");

            var searchUrl = SearchUrl + "?" + qc.GetQueryString();
            var results = await RequestWithCookiesAndRetryAsync(searchUrl);
            if (results.IsRedirect) // re-login
            {
                await ApplyConfiguration(null);
                results = await RequestWithCookiesAndRetryAsync(searchUrl);
            }

            try
            {
                var parser = new HtmlParser();
                using var dom = parser.ParseDocument(results.ContentString);
                var rows = dom.QuerySelectorAll("#torrents-table > tbody > tr");

                foreach (var row in rows.Skip(1))
                {
                    var qDetails = row.QuerySelector(".br_right > a");
                    var details = new Uri(SiteLink + qDetails.GetAttribute("href"));
                    var title = qDetails.QuerySelector("b").TextContent;
                    // Remove auto-generated [REQ] tag from fulfilled requests
                    if (title.StartsWith("[REQ] "))
                    {
                        title = title.Substring(6);
                    }
                    var qLink = row.QuerySelector("td:nth-child(4) > a");
                    if (qLink == null)
                        continue; // support/donation banner
                    var link = new Uri(SiteLink + qLink.GetAttribute("href"));

                    // dateString format "yyyy-MMM-dd hh:mm:ss" => eg "2015-04-25 23:38:12"
                    var dateString = row.QuerySelector("td:nth-child(6) nobr").TextContent.Trim();
                    var publishDate = DateTime.ParseExact(dateString, "yyyy-MM-ddHH:mm:ss", CultureInfo.InvariantCulture);

                    var size = ParseUtil.GetBytes(row.QuerySelector("td:nth-child(7)").InnerHtml.Split('<').First().Trim());
                    var files = ParseUtil.GetLongFromString(row.QuerySelector("td:nth-child(7) > a").TextContent);
                    var grabs = ParseUtil.GetLongFromString(row.QuerySelector("td:nth-child(8)").TextContent);
                    var seeders = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(9)").TextContent);
                    var leechers = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(10)").TextContent);

                    var category = row.QuerySelector(".br_type > a").GetAttribute("href").Replace("browse.php?cat=", string.Empty);

                    var qImdb = row.QuerySelector("a[href*=\"www.imdb.com/\"]");
                    var imdb = qImdb != null ? ParseUtil.GetImdbId(qImdb.GetAttribute("href").Split('/').Last()) : null;

                    var release = new ReleaseInfo
                    {
                        Details = details,
                        Guid = details,
                        Title = title,
                        Link = link,
                        PublishDate = publishDate,
                        Size = size,
                        Seeders = seeders,
                        Peers = seeders + leechers,
                        Grabs = grabs,
                        Files = files,
                        Category = MapTrackerCatToNewznab(category),
                        Imdb = imdb,
                        MinimumRatio = 1,
                        MinimumSeedTime = 172800, // 48 hours
                        UploadVolumeFactor = 1,
                        DownloadVolumeFactor = 1
                    };
                    releases.Add(release);
                }
            }
            catch (Exception e)
            {
                OnParseError(results.ContentString, e);
            }

            return releases;
        }
    }
}
