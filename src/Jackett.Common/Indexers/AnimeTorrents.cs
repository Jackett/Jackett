 using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class AnimeTorrents : BaseWebIndexer
    {
        private string LoginUrl => SiteLink + "login.php";
        private string SearchUrl => SiteLink + "ajax/torrents_data.php";
        private string SearchUrlReferer => SiteLink + "torrents.php?cat=0&searchin=filename&search=";

        private new ConfigurationDataBasicLogin configData
        {
            get => (ConfigurationDataBasicLogin)base.configData;
            set => base.configData = value;
        }

        public AnimeTorrents(IIndexerConfigurationService configService, WebClient c, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "animetorrents",
                   name: "AnimeTorrents",
                   description: "Definitive source for anime and manga",
                   link: "https://animetorrents.me/",
                   caps: new TorznabCapabilities {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                       },
                       MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q
                       }
                   },
                   configService: configService,
                   client: c,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataBasicLogin())
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";

            AddCategoryMapping(1, TorznabCatType.MoviesSD, "Anime Movie");
            AddCategoryMapping(6, TorznabCatType.MoviesHD, "Anime Movie HD");
            AddCategoryMapping(2, TorznabCatType.TVAnime, "Anime Series");
            AddCategoryMapping(7, TorznabCatType.TVAnime, "Anime Series HD");
            AddCategoryMapping(5, TorznabCatType.XXXDVD, "Hentai (censored)");
            AddCategoryMapping(9, TorznabCatType.XXXDVD, "Hentai (censored) HD");
            AddCategoryMapping(4, TorznabCatType.XXXDVD, "Hentai (un-censored)");
            AddCategoryMapping(8, TorznabCatType.XXXDVD, "Hentai (un-censored) HD");
            AddCategoryMapping(13, TorznabCatType.BooksForeign, "Light Novel");
            AddCategoryMapping(3, TorznabCatType.BooksComics, "Manga");
            AddCategoryMapping(10, TorznabCatType.BooksComics, "Manga 18+");
            AddCategoryMapping(11, TorznabCatType.TVAnime, "OVA");
            AddCategoryMapping(12, TorznabCatType.TVAnime, "OVA HD");
            AddCategoryMapping(14, TorznabCatType.BooksComics, "Doujin Anime");
            AddCategoryMapping(15, TorznabCatType.XXXDVD, "Doujin Anime 18+");
            AddCategoryMapping(16, TorznabCatType.AudioForeign, "Doujin Music");
            AddCategoryMapping(17, TorznabCatType.BooksComics, "Doujinshi");
            AddCategoryMapping(18, TorznabCatType.BooksComics, "Doujinshi 18+");
            AddCategoryMapping(19, TorznabCatType.Audio, "OST");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "form", "login" },
                { "rememberme[]", "1" }
            };

            var loginPage = await RequestWithCookiesAndRetryAsync(LoginUrl, "", RequestType.GET, LoginUrl);

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, loginPage.Cookies, true);
            await ConfigureIfOK(result.Cookies, result.ContentString != null && result.ContentString.Contains("logout.php"), () =>
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(result.ContentString);
                var errorMessage = dom.QuerySelector(".ui-state-error").Text().Trim();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();

            //  replace any space, special char, etc. with % (wildcard)
            var ReplaceRegex = new Regex("[^a-zA-Z0-9]+");
            searchString = ReplaceRegex.Replace(searchString, "%");
            var searchUrl = SearchUrl;
            var queryCollection = new NameValueCollection
            {
                {"total", "146"}, // Not sure what this is about but its required!
                {"cat", MapTorznabCapsToTrackers(query).FirstIfSingleOrDefault("0")},
                {"page", "1"},
                {"searchin", "filename"},
                {"search", searchString}
            };
            searchUrl += "?" + queryCollection.GetQueryString();

            var extraHeaders = new Dictionary<string, string>
            {
                { "X-Requested-With", "XMLHttpRequest" }
            };

            var response = await RequestWithCookiesAndRetryAsync(
                searchUrl, referer: SearchUrlReferer, headers: extraHeaders);

            var results = response.ContentString;
            try
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(results);

                var rows = dom.QuerySelectorAll("tr");
                foreach (var row in rows.Skip(1))
                {
                    var release = new ReleaseInfo();
                    var qTitleLink = row.QuerySelector("td:nth-of-type(2) a:nth-of-type(1)");
                    release.Title = qTitleLink.TextContent.Trim();

                    // If we search an get no results, we still get a table just with no info.
                    if (string.IsNullOrWhiteSpace(release.Title))
                    {
                        break;
                    }

                    release.Guid = new Uri(qTitleLink.GetAttribute("href"));
                    release.Details = release.Guid;

                    var dateString = row.QuerySelector("td:nth-of-type(5)").TextContent;
                    release.PublishDate = DateTime.ParseExact(dateString, "dd MMM yy", CultureInfo.InvariantCulture);

                    var qLink = row.QuerySelector("td:nth-of-type(3) a");
                    if (qLink != null) // newbie users don't see DL links
                    {
                        release.Link = new Uri(qLink.GetAttribute("href"));
                    }
                    else
                    {
                        // use details link as placeholder
                        // null causes errors during export to torznab
                        // skipping the release prevents newbie users from adding the tracker (empty result)
                        release.Link = release.Details;
                    }

                    var sizeStr = row.QuerySelector("td:nth-of-type(6)").TextContent;
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    var connections = row.QuerySelector("td:nth-of-type(8)").TextContent.Trim().Split("/".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                    release.Seeders = ParseUtil.CoerceInt(connections[0].Trim());
                    release.Peers = ParseUtil.CoerceInt(connections[1].Trim()) + release.Seeders;
                    release.Grabs = ParseUtil.CoerceLong(connections[2].Trim());

                    var rCat = row.QuerySelector("td:nth-of-type(1) a").GetAttribute("href");
                    var rCatIdx = rCat.IndexOf("cat=");
                    if (rCatIdx > -1)
                    {
                        rCat = rCat.Substring(rCatIdx + 4);
                    }

                    release.Category = MapTrackerCatToNewznab(rCat);

                    if (row.QuerySelector("img[alt=\"Gold Torrent\"]") != null)
                        release.DownloadVolumeFactor = 0;
                    else if (row.QuerySelector("img[alt=\"Silver Torrent\"]") != null)
                        release.DownloadVolumeFactor = 0.5;
                    else
                        release.DownloadVolumeFactor = 1;

                    var ULFactorImg = row.QuerySelector("img[alt*=\"x Multiplier Torrent\"]");
                    if (ULFactorImg != null)
                    {
                        release.UploadVolumeFactor = ParseUtil.CoerceDouble(ULFactorImg.GetAttribute("alt").Split('x')[0]);
                    }
                    else
                    {
                        release.UploadVolumeFactor = 1;
                    }

                    qTitleLink.Remove();
                    release.Description = row.QuerySelector("td:nth-of-type(2)").TextContent;

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results, ex);
            }

            return releases;
        }
    }
}
