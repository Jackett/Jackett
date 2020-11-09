using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class ZonaQ : BaseWebIndexer
    {
        private string Login1Url => SiteLink + "index.php";
        private string Login2Url => SiteLink + "paDentro.php";
        private string Login3Url => SiteLink + "retorno/include/puerta_8_ajax.php";
        private string Login4Url => SiteLink + "retorno/index.php";
        private string SearchUrl => SiteLink + "retorno/2/index.php";

        private new ConfigurationDataBasicLogin configData => (ConfigurationDataBasicLogin)base.configData;

        public ZonaQ(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(id: "zonaq",
                   name: "ZonaQ",
                   description: "ZonaQ is a SPANISH Private Torrent Tracker for MOVIES / TV",
                   link: "https://www.zonaq.pw/",
                   caps: new TorznabCapabilities
                   {
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
                   client: wc,
                   logger: l,
                   p: ps,
                   configData: new ConfigurationDataBasicLogin("For best results, change the 'Torrents por página' option to 100 in 'Mi Panel' page."))

        {
            Encoding = Encoding.UTF8;
            Language = "es-es";
            Type = "private";

            AddCategoryMapping("cat[]=1&subcat[]=1", TorznabCatType.MoviesDVD, "Películas/DVD");
            AddCategoryMapping("cat[]=1&subcat[]=2", TorznabCatType.MoviesDVD, "Películas/BDVD + Autorías");
            AddCategoryMapping("cat[]=1&subcat[]=3", TorznabCatType.MoviesBluRay, "Películas/BD");
            AddCategoryMapping("cat[]=1&subcat[]=4", TorznabCatType.MoviesUHD, "Películas/BD 4K");
            AddCategoryMapping("cat[]=1&subcat[]=5", TorznabCatType.Movies3D, "Películas/BD 3D");
            AddCategoryMapping("cat[]=1&subcat[]=6", TorznabCatType.MoviesBluRay, "Películas/BD Remux");
            AddCategoryMapping("cat[]=1&subcat[]=7", TorznabCatType.MoviesHD, "Películas/MKV");
            AddCategoryMapping("cat[]=1&subcat[]=8", TorznabCatType.MoviesUHD, "Películas/MKV 4K");
            AddCategoryMapping("cat[]=1&subcat[]=9", TorznabCatType.MoviesUHD, "Películas/BD Remux 4K");

            AddCategoryMapping("cat[]=2&subcat[]=1", TorznabCatType.MoviesDVD, "Animación/DVD");
            AddCategoryMapping("cat[]=2&subcat[]=2", TorznabCatType.MoviesDVD, "Animación/BDVD + Autorías");
            AddCategoryMapping("cat[]=2&subcat[]=3", TorznabCatType.MoviesBluRay, "Animación/BD");
            AddCategoryMapping("cat[]=2&subcat[]=4", TorznabCatType.MoviesUHD, "Animación/BD 4K");
            AddCategoryMapping("cat[]=2&subcat[]=5", TorznabCatType.Movies3D, "Animación/BD 3D");
            AddCategoryMapping("cat[]=2&subcat[]=6", TorznabCatType.MoviesBluRay, "Animación/BD Remux");
            AddCategoryMapping("cat[]=2&subcat[]=7", TorznabCatType.MoviesHD, "Animación/MKV");
            AddCategoryMapping("cat[]=2&subcat[]=8", TorznabCatType.MoviesUHD, "Animación/MKV 4K");
            AddCategoryMapping("cat[]=2&subcat[]=9", TorznabCatType.MoviesUHD, "Animación/BD Remux 4K");

            AddCategoryMapping("cat[]=3&subcat[]=1", TorznabCatType.AudioVideo, "Música/DVD");
            AddCategoryMapping("cat[]=3&subcat[]=2", TorznabCatType.AudioVideo, "Música/BDVD + Autorías");
            AddCategoryMapping("cat[]=3&subcat[]=3", TorznabCatType.AudioVideo, "Música/BD");
            AddCategoryMapping("cat[]=3&subcat[]=4", TorznabCatType.AudioVideo, "Música/BD 4K");
            AddCategoryMapping("cat[]=3&subcat[]=5", TorznabCatType.AudioVideo, "Música/BD 3D");
            AddCategoryMapping("cat[]=3&subcat[]=6", TorznabCatType.AudioVideo, "Música/BD Remux");
            AddCategoryMapping("cat[]=3&subcat[]=7", TorznabCatType.AudioVideo, "Música/MKV");
            AddCategoryMapping("cat[]=3&subcat[]=8", TorznabCatType.AudioVideo, "Música/MKV 4K");
            AddCategoryMapping("cat[]=3&subcat[]=9", TorznabCatType.AudioVideo, "Música/BD Remux 4K");

            AddCategoryMapping("cat[]=4&subcat[]=1", TorznabCatType.TVSD, "Series/DVD");
            AddCategoryMapping("cat[]=4&subcat[]=2", TorznabCatType.TVSD, "Series/BDVD + Autorías");
            AddCategoryMapping("cat[]=4&subcat[]=3", TorznabCatType.TVHD, "Series/BD");
            AddCategoryMapping("cat[]=4&subcat[]=4", TorznabCatType.TVUHD, "Series/BD 4K");
            AddCategoryMapping("cat[]=4&subcat[]=5", TorznabCatType.TVOther, "Series/BD 3D");
            AddCategoryMapping("cat[]=4&subcat[]=6", TorznabCatType.TVHD, "Series/BD Remux");
            AddCategoryMapping("cat[]=4&subcat[]=7", TorznabCatType.TVHD, "Series/MKV");
            AddCategoryMapping("cat[]=4&subcat[]=8", TorznabCatType.TVUHD, "Series/MKV 4K");
            AddCategoryMapping("cat[]=4&subcat[]=9", TorznabCatType.TVUHD, "Series/BD Remux 4K");

            AddCategoryMapping("cat[]=5&subcat[]=1", TorznabCatType.TVDocumentary, "Docus/DVD");
            AddCategoryMapping("cat[]=5&subcat[]=2", TorznabCatType.TVDocumentary, "Docus/BDVD + Autorías");
            AddCategoryMapping("cat[]=5&subcat[]=3", TorznabCatType.TVDocumentary, "Docus/BD");
            AddCategoryMapping("cat[]=5&subcat[]=4", TorznabCatType.TVDocumentary, "Docus/BD 4K");
            AddCategoryMapping("cat[]=5&subcat[]=5", TorznabCatType.TVDocumentary, "Docus/BD 3D");
            AddCategoryMapping("cat[]=5&subcat[]=6", TorznabCatType.TVDocumentary, "Docus/BD Remux");
            AddCategoryMapping("cat[]=5&subcat[]=7", TorznabCatType.TVDocumentary, "Docus/MKV");
            AddCategoryMapping("cat[]=5&subcat[]=8", TorznabCatType.TVDocumentary, "Docus/MKV 4K");
            AddCategoryMapping("cat[]=5&subcat[]=9", TorznabCatType.TVDocumentary, "Docus/BD Remux 4K");

            AddCategoryMapping("cat[]=6&subcat[]=1", TorznabCatType.OtherMisc, "Deportes y Otros/DVD");
            AddCategoryMapping("cat[]=6&subcat[]=2", TorznabCatType.OtherMisc, "Deportes y Otros/BDVD + Autorías");
            AddCategoryMapping("cat[]=6&subcat[]=3", TorznabCatType.OtherMisc, "Deportes y Otros/BD");
            AddCategoryMapping("cat[]=6&subcat[]=4", TorznabCatType.OtherMisc, "Deportes y Otros/BD 4K");
            AddCategoryMapping("cat[]=6&subcat[]=5", TorznabCatType.OtherMisc, "Deportes y Otros/BD 3D");
            AddCategoryMapping("cat[]=6&subcat[]=6", TorznabCatType.OtherMisc, "Deportes y Otros/BD Remux");
            AddCategoryMapping("cat[]=6&subcat[]=7", TorznabCatType.OtherMisc, "Deportes y Otros/MKV");
            AddCategoryMapping("cat[]=6&subcat[]=8", TorznabCatType.OtherMisc, "Deportes y Otros/MKV 4K");
            AddCategoryMapping("cat[]=6&subcat[]=9", TorznabCatType.OtherMisc, "Deportes y Otros/BD Remux 4K");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            try
            {
                await DoLogin();
                var results = await PerformQuery(new TorznabQuery());
                if (!results.Any())
                    throw new Exception("Found 0 results in the tracker");

                IsConfigured = true;
                SaveConfig();
                return IndexerConfigurationStatus.Completed;
            }
            catch (Exception e)
            {
                IsConfigured = false;
                throw new Exception("Login error: " + e.Message);
            }
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var qc = new NameValueCollection
            {
                {"page", "torrents"},
                {"search", query.GetQueryString()},
                {"active", "0"}
            };
            var searchUrl = SearchUrl + "?" + qc.GetQueryString();

            foreach (var cat in MapTorznabCapsToTrackers(query)) // categories are already encoded
                searchUrl += "&" + cat;

            var response = await RequestWithCookiesAsync(searchUrl);
            var results = response.ContentString;
            if (results == null || !results.Contains("/index.php?action=logout;"))
            {
                logger.Info("ZonaQ re-login");
                await DoLogin(); // re-login
                response = await RequestWithCookiesAsync(searchUrl);
                results = response.ContentString;
            }

            try
            {
                var parser = new HtmlParser();
                var doc = parser.ParseDocument(results);

                var rows = doc.QuerySelectorAll("table.torrent_list > tbody > tr");
                foreach (var row in rows.Skip(1))
                {
                    var qTitleLink = row.QuerySelector("a[href*=\"?page=torrent-details\"]");
                    if (qTitleLink == null) // no results
                        continue;

                    var title = qTitleLink.TextContent.Trim();
                    title += " SPANiSH"; // fix for Radarr
                    title = Regex.Replace(title, "4k", "2160p", RegexOptions.IgnoreCase);

                    var detailsStr = qTitleLink.GetAttribute("href");
                    var details = new Uri(detailsStr);
                    var link = new Uri(detailsStr.Replace("/index.php?page=torrent-details&", "/download.php?"));
                    var qPoster = qTitleLink.GetAttribute("title");
                    var poster = qPoster != null ? new Uri(qPoster) : null;

                    var publishDateStr = row.Children[4].InnerHtml.Split('>').Last();
                    var publishDate = DateTime.ParseExact(publishDateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    var size = ReleaseInfo.GetBytes(row.Children[5].TextContent.Replace(".", "").Replace(",", "."));
                    var seeders = ParseUtil.CoerceInt(row.Children[6].TextContent);
                    var leechers = ParseUtil.CoerceInt(row.Children[7].TextContent);
                    var grabs = ParseUtil.CoerceInt(row.Children[8].TextContent);

                    var cat1 = row.Children[0].FirstElementChild.GetAttribute("href").Split('=').Last();
                    var cat2 = row.Children[1].FirstElementChild.GetAttribute("href").Split('=').Last();
                    var cat = MapTrackerCatToNewznab($"cat[]={cat1}&subcat[]={cat2}");

                    var dlVolumeFactor = row.QuerySelector("img[src*=\"/gold.png\"]") != null ? 0 :
                        row.QuerySelector("img[src*=\"/silver.png\"]") != null ? 0.5 : 1;
                    var ulVolumeFactor = row.QuerySelector("img[src*=\"/por3.gif\"]") != null ? 3 :
                        row.QuerySelector("img[src*=\"/por2.gif\"]") != null ? 2 : 1;

                    var release = new ReleaseInfo
                    {
                        Title = title,
                        Details = details,
                        Guid = details,
                        Link = link,
                        PublishDate = publishDate,
                        Poster = poster,
                        Category = cat,
                        Size = size,
                        Grabs = grabs,
                        Seeders = seeders,
                        Peers = seeders + leechers,
                        DownloadVolumeFactor = dlVolumeFactor,
                        UploadVolumeFactor = ulVolumeFactor,
                        MinimumRatio = 1,
                        MinimumSeedTime = 259200 // 72 hours
                    };

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results, ex);
            }

            return releases;
        }

        private async Task DoLogin()
        {
            // The first page set the cookies and the session_id
            CookieHeader = "";
            var result = await RequestWithCookiesAsync(Login1Url,  "");
            var parser = new HtmlParser();
            var dom = parser.ParseDocument(result.ContentString);
            var sessionId = dom.QuerySelector("input#session_id")?.GetAttribute("value");
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ExceptionWithConfigData("Error getting the Session ID", configData);

            // The second page send the login with the hash
            // The hash is reverse engineering from https://www.zonaq.pw/retorno/2/smf/Themes/smf_ZQ/scripts/script.js
            // doForm.hash_passwrd.value = hex_sha1(hex_sha1(doForm.user.value.php_to8bit().php_strtolower() + doForm.passwrd.value.php_to8bit()) + cur_session_id);
            Thread.Sleep(3000);
            var hashPassword = Sha1Hash(Sha1Hash(configData.Username.Value.ToLower() + configData.Password.Value) + sessionId);
            var pairs = new Dictionary<string, string> {
                { "user", configData.Username.Value },
                { "passwrd", configData.Password.Value },
                { "hash_passwrd", hashPassword }
            };
            var headers = new Dictionary<string, string>
            {
                {"X-Requested-With", "XMLHttpRequest"}
            };
            result = await RequestWithCookiesAsync(Login2Url, method: RequestType.POST, data: pairs, headers: headers);
            var message = JObject.Parse(result.ContentString)["msg"]?.ToString();
            if (message == "puerta_2")
            {
                // The third page sets the cookie duration
                Thread.Sleep(3000);
                pairs = new Dictionary<string, string> {
                    { "passwd", "" },
                    { "cookielength", "43200" }, // 1 month
                    { "respuesta", "" }
                };
                result = await RequestWithCookiesAsync(Login3Url, method: RequestType.POST, data: pairs, headers: headers);
                message = JObject.Parse(result.ContentString)["msg"]?.ToString();
            }
            if (message != "last_door")
                throw new ExceptionWithConfigData($"Login error: {message}", configData);

            // The forth page sets the last cookie
            Thread.Sleep(3000);
            await RequestWithCookiesAsync(Login4Url);
        }

        private static string Sha1Hash(string input)
        {
            var hash = new SHA1Managed().ComputeHash(Encoding.UTF8.GetBytes(input));
            return string.Concat(hash.Select(b => b.ToString("x2")));
        }
    }
}
