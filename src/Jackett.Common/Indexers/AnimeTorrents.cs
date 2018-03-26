using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CsQuery;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    public class AnimeTorrents : BaseWebIndexer
    {
        private string LoginUrl { get { return SiteLink + "login.php"; } }
        private string SearchUrl { get { return SiteLink + "ajax/torrents_data.php"; } }
        private string SearchUrlReferer { get { return SiteLink + "torrents.php?cat=0&searchin=filename&search="; } }

        private new ConfigurationDataBasicLogin configData
        {
            get { return (ConfigurationDataBasicLogin)base.configData; }
            set { base.configData = value; }
        }

        public AnimeTorrents(IIndexerConfigurationService configService, WebClient c, Logger l, IProtectionService ps)
            : base(name: "AnimeTorrents",
                description: "Definitive source for anime and manga",
                link: "https://animetorrents.me/",
                caps: new TorznabCapabilities(),
                configService: configService,
                client: c,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLogin())
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";

            AddCategoryMapping(1, TorznabCatType.MoviesSD); // Anime Movie
            AddCategoryMapping(6, TorznabCatType.MoviesHD); // Anime Movie HD
            AddCategoryMapping(2, TorznabCatType.TVAnime); // Anime Series
            AddCategoryMapping(7, TorznabCatType.TVAnime); // Anime Series HD
            AddCategoryMapping(5, TorznabCatType.XXXDVD); // Hentai (censored)
            AddCategoryMapping(9, TorznabCatType.XXXDVD); // Hentai (censored) HD
            AddCategoryMapping(4, TorznabCatType.XXXDVD); // Hentai (un-censored)
            AddCategoryMapping(8, TorznabCatType.XXXDVD); // Hentai (un-censored) HD
            AddCategoryMapping(13, TorznabCatType.BooksForeign); // Light Novel
            AddCategoryMapping(3, TorznabCatType.BooksComics); // Manga
            AddCategoryMapping(10, TorznabCatType.BooksComics); // Manga 18+
            AddCategoryMapping(11, TorznabCatType.TVAnime); // OVA
            AddCategoryMapping(12, TorznabCatType.TVAnime); // OVA HD
            AddCategoryMapping(14, TorznabCatType.BooksComics); // Doujin Anime
            AddCategoryMapping(15, TorznabCatType.XXXDVD); // Doujin Anime 18+
            AddCategoryMapping(16, TorznabCatType.AudioForeign); // Doujin Music
            AddCategoryMapping(17, TorznabCatType.BooksComics); // Doujinshi
            AddCategoryMapping(18, TorznabCatType.BooksComics); // Doujinshi 18+
            AddCategoryMapping(19, TorznabCatType.Audio); // OST
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

            var loginPage = await RequestStringWithCookiesAndRetry(LoginUrl, "", LoginUrl);

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, loginPage.Cookies, true);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("logout.php"), () =>
            {
                CQ dom = result.Content;
                var errorMessage = dom[".ui-state-error"].Text().Trim();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();

            //  replace any space, special char, etc. with % (wildcard)
            Regex ReplaceRegex = new Regex("[^a-zA-Z0-9]+");
            searchString = ReplaceRegex.Replace(searchString, "%");

            var searchUrl = SearchUrl;
            var queryCollection = new NameValueCollection();

            queryCollection.Add("total", "146"); // Not sure what this is about but its required!

            var cat = "0";
            var queryCats = MapTorznabCapsToTrackers(query);
            if (queryCats.Count == 1)
            {
                cat = queryCats.First().ToString();
            }

            queryCollection.Add("cat", cat);
            queryCollection.Add("searchin", "filename");
            queryCollection.Add("search", searchString);
            queryCollection.Add("page", "1");
            searchUrl += "?" + queryCollection.GetQueryString();

            var extraHeaders = new Dictionary<string, string>()
            {
                { "X-Requested-With", "XMLHttpRequest" }
            };

            var response = await RequestStringWithCookiesAndRetry(searchUrl, null, SearchUrlReferer, extraHeaders);

            var results = response.Content;
            try
            {
                CQ dom = results;

                var rows = dom["tr"];
                foreach (var row in rows.Skip(1))
                {
                    var release = new ReleaseInfo();
                    var qRow = row.Cq();
                    var qTitleLink = qRow.Find("td:eq(1) a:eq(0)").First();
                    release.Title = qTitleLink.Text().Trim();

                    // If we search an get no results, we still get a table just with no info.
                    if (string.IsNullOrWhiteSpace(release.Title))
                    {
                        break;
                    }

                    release.Guid = new Uri(qTitleLink.Attr("href"));
                    release.Comments = release.Guid;

                    var dateString = qRow.Find("td:eq(4)").Text();
                    release.PublishDate = DateTime.ParseExact(dateString, "dd MMM yy", CultureInfo.InvariantCulture);

                    var qLink = qRow.Find("td:eq(2) a");
                    if (qLink.Length != 0) // newbie users don't see DL links
                    {
                        release.Link = new Uri(qLink.Attr("href"));
                    }
                    else
                    {
                        // use comments link as placeholder
                        // null causes errors during export to torznab
                        // skipping the release prevents newbie users from adding the tracker (empty result)
                        release.Link = release.Comments;
                    }

                    var sizeStr = qRow.Find("td:eq(5)").Text();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    var connections = qRow.Find("td:eq(7)").Text().Trim().Split("/".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                    release.Seeders = ParseUtil.CoerceInt(connections[0].Trim());
                    release.Peers = ParseUtil.CoerceInt(connections[1].Trim()) + release.Seeders;
                    release.Grabs = ParseUtil.CoerceLong(connections[2].Trim());

                    var rCat = row.Cq().Find("td:eq(0) a").First().Attr("href");
                    var rCatIdx = rCat.IndexOf("cat=");
                    if (rCatIdx > -1)
                    {
                        rCat = rCat.Substring(rCatIdx + 4);
                    }

                    release.Category = MapTrackerCatToNewznab(rCat);

                    if (qRow.Find("img[alt=\"Gold Torrent\"]").Length >= 1)
                        release.DownloadVolumeFactor = 0;
                    else if (qRow.Find("img[alt=\"Silver Torrent\"]").Length >= 1)
                        release.DownloadVolumeFactor = 0.5;
                    else
                        release.DownloadVolumeFactor = 1;

                    var ULFactorImg = qRow.Find("img[alt*=\"x Multiplier Torrent\"]");
                    if (ULFactorImg.Length >= 1)
                    {
                        release.UploadVolumeFactor = ParseUtil.CoerceDouble(ULFactorImg.Attr("alt").Split('x')[0]);
                    }
                    else
                    {
                        release.UploadVolumeFactor = 1;
                    }

                    qTitleLink.Remove();
                    release.Description = qRow.Find("td:eq(1)").Text();

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
