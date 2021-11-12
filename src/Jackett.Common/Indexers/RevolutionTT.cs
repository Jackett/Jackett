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

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class RevolutionTT : BaseWebIndexer
    {
        private string LandingPageURL => SiteLink + "login.php";
        private string LoginUrl => SiteLink + "takelogin.php";
        private string SearchUrl => SiteLink + "browse.php";

        private new ConfigurationDataBasicLogin configData => (ConfigurationDataBasicLogin)base.configData;

        public RevolutionTT(IIndexerConfigurationService configService, Utils.Clients.WebClient wc, Logger l,
            IProtectionService ps, ICacheService cs)
            : base(id: "revolutiontt",
                   name: "RevolutionTT",
                   description: "The Revolution has begun",
                   link: "https://revolutiontt.me/",
                   caps: new TorznabCapabilities
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
                   },
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataBasicLogin("For best results, change the 'Torrents per page' setting to 100 in your Profile."))
        {
            Encoding = Encoding.GetEncoding("iso-8859-1");
            Language = "en-US";
            Type = "private";

            AddCategoryMapping("23", TorznabCatType.TVAnime);
            AddCategoryMapping("22", TorznabCatType.PC0day);
            AddCategoryMapping("1", TorznabCatType.PCISO);
            AddCategoryMapping("36", TorznabCatType.Books);
            AddCategoryMapping("36", TorznabCatType.BooksEBook);
            AddCategoryMapping("4", TorznabCatType.PCGames);
            AddCategoryMapping("21", TorznabCatType.PCGames);
            AddCategoryMapping("16", TorznabCatType.ConsolePS3);
            AddCategoryMapping("40", TorznabCatType.ConsoleWii);
            AddCategoryMapping("39", TorznabCatType.ConsoleXBox360);
            AddCategoryMapping("35", TorznabCatType.ConsoleNDS);
            AddCategoryMapping("34", TorznabCatType.ConsolePSP);
            AddCategoryMapping("2", TorznabCatType.PCMac);
            AddCategoryMapping("10", TorznabCatType.MoviesBluRay);
            AddCategoryMapping("20", TorznabCatType.MoviesDVD);
            AddCategoryMapping("12", TorznabCatType.MoviesHD);
            AddCategoryMapping("44", TorznabCatType.MoviesOther);
            AddCategoryMapping("11", TorznabCatType.MoviesSD);
            AddCategoryMapping("19", TorznabCatType.MoviesSD);
            AddCategoryMapping("6", TorznabCatType.Audio);
            AddCategoryMapping("8", TorznabCatType.AudioLossless);
            AddCategoryMapping("46", TorznabCatType.AudioOther);
            AddCategoryMapping("29", TorznabCatType.AudioVideo);
            AddCategoryMapping("43", TorznabCatType.TVOther);
            AddCategoryMapping("42", TorznabCatType.TVHD);
            AddCategoryMapping("45", TorznabCatType.TVOther);
            AddCategoryMapping("41", TorznabCatType.TVSD);
            AddCategoryMapping("7", TorznabCatType.TVSD);
            AddCategoryMapping("9", TorznabCatType.XXX);
            AddCategoryMapping("49", TorznabCatType.XXX);
            AddCategoryMapping("47", TorznabCatType.XXXDVD);
            AddCategoryMapping("48", TorznabCatType.XXX);
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
                var dom = parser.ParseDocument(results.ContentString);
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

                    var size = ReleaseInfo.GetBytes(row.QuerySelector("td:nth-child(7)").InnerHtml.Split('<').First().Trim());
                    var files = ParseUtil.GetLongFromString(row.QuerySelector("td:nth-child(7) > a").TextContent);
                    var grabs = ParseUtil.GetLongFromString(row.QuerySelector("td:nth-child(8)").TextContent);
                    var seeders = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(9)").TextContent);
                    var leechers = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(10)").TextContent);

                    var category = row.QuerySelector(".br_type > a").GetAttribute("href").Replace("browse.php?cat=", string.Empty);

                    var qImdb = row.QuerySelector("a[href*=\"www.imdb.com/\"]");
                    var imdb = qImdb != null ? ParseUtil.GetImdbID(qImdb.GetAttribute("href").Split('/').Last()) : null;

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
