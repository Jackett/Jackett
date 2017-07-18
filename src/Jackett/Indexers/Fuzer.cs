using CsQuery;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Jackett.Models.IndexerConfig;
using System.Collections.Specialized;
using System.Threading;

namespace Jackett.Indexers
{
    public class Fuzer : BaseWebIndexer
    {
        private string SearchUrl { get { return SiteLink + "index.php?name=torrents&"; } }
        private string LoginUrl { get { return SiteLink + "login.php"; } }
        private const int MAXPAGES = 3;

        new ConfigurationDataBasicLogin configData
        {
            get { return (ConfigurationDataBasicLogin)base.configData; }
            set { base.configData = value; }
        }

        public Fuzer(IIndexerConfigurationService configService, IWebClient w, Logger l, IProtectionService ps)
            : base(name: "Fuzer",
                description: "Fuzer is a private torrent website with israeli torrents.",
                link: "https://fuzer.me/",
                configService: configService,
                client: w,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLogin())
        {
            Encoding = Encoding.GetEncoding("Windows-1255");
            Language = "he-il";
            Type = "private";
            TorznabCaps.Categories.Clear();

            AddMultiCategoryMapping(TorznabCatType.Movies, 7, 9, 58, 59, 60, 61, 83);
            AddMultiCategoryMapping(TorznabCatType.MoviesSD, 7, 58);
            AddMultiCategoryMapping(TorznabCatType.MoviesHD, 9, 59, 61);
            AddMultiCategoryMapping(TorznabCatType.MoviesBluRay, 59);
            AddMultiCategoryMapping(TorznabCatType.MoviesForeign, 83);
            AddMultiCategoryMapping(TorznabCatType.MoviesDVD, 58);
            AddMultiCategoryMapping(TorznabCatType.Movies3D, 9);
            AddMultiCategoryMapping(TorznabCatType.MoviesWEBDL, 9);
            AddMultiCategoryMapping(TorznabCatType.TV, 8, 10, 62, 63, 84);
            AddMultiCategoryMapping(TorznabCatType.TVHD, 10, 63);
            AddMultiCategoryMapping(TorznabCatType.TVFOREIGN, 62, 84);
            AddMultiCategoryMapping(TorznabCatType.TVSport, 64);
            AddMultiCategoryMapping(TorznabCatType.TVAnime, 65);
            AddMultiCategoryMapping(TorznabCatType.TVWEBDL, 10, 63);
            AddMultiCategoryMapping(TorznabCatType.TVSD, 8, 62, 84);
            AddMultiCategoryMapping(TorznabCatType.TVDocumentary, 8, 10, 62, 63);
            AddMultiCategoryMapping(TorznabCatType.Console, 12, 55, 56, 57);
            AddMultiCategoryMapping(TorznabCatType.ConsoleXbox, 55);
            AddMultiCategoryMapping(TorznabCatType.ConsoleXbox360, 55);
            AddMultiCategoryMapping(TorznabCatType.ConsoleXBOX360DLC, 55);
            AddMultiCategoryMapping(TorznabCatType.ConsolePS3, 12);
            AddMultiCategoryMapping(TorznabCatType.ConsolePS4, 12);
            AddMultiCategoryMapping(TorznabCatType.ConsoleXboxOne, 55);
            AddMultiCategoryMapping(TorznabCatType.ConsolePS4, 12);
            AddMultiCategoryMapping(TorznabCatType.ConsoleWii, 56);
            AddMultiCategoryMapping(TorznabCatType.ConsoleWiiwareVC, 56);
            AddMultiCategoryMapping(TorznabCatType.ConsolePSP, 57);
            AddMultiCategoryMapping(TorznabCatType.ConsoleNDS, 57);
            AddMultiCategoryMapping(TorznabCatType.MoviesOther, 57);
            AddMultiCategoryMapping(TorznabCatType.PC, 11, 15);
            AddMultiCategoryMapping(TorznabCatType.PCGames, 11);
            AddMultiCategoryMapping(TorznabCatType.PCMac, 71);
            AddMultiCategoryMapping(TorznabCatType.PCPhoneAndroid, 13);
            AddMultiCategoryMapping(TorznabCatType.PCPhoneIOS, 70);
            AddMultiCategoryMapping(TorznabCatType.Audio, 14, 66, 67, 68);
            AddMultiCategoryMapping(TorznabCatType.AudioForeign, 14);
            AddMultiCategoryMapping(TorznabCatType.AudioLossless, 67);
            AddMultiCategoryMapping(TorznabCatType.AudioAudiobook, 69);
            AddMultiCategoryMapping(TorznabCatType.AudioOther, 68);
            AddMultiCategoryMapping(TorznabCatType.Other, 17);
            AddMultiCategoryMapping(TorznabCatType.XXX, 16);
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var loginPage = await RequestStringWithCookies(LoginUrl, string.Empty);

            var pairs = new Dictionary<string, string> {
                { "vb_login_username", configData.Username.Value },
                { "vb_login_password", "" },
                { "securitytoken", "guest" },
                { "do","login"},
                { "vb_login_md5password", StringUtil.Hash(configData.Password.Value).ToLower()},
                { "vb_login_md5password_utf", StringUtil.Hash(configData.Password.Value).ToLower()},
                { "cookieuser", "1" }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, loginPage.Cookies, true, null, LoginUrl);

            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("images/loading.gif"), () =>
            {
                var errorMessage = "Couldn't login";
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            Thread.Sleep(2);
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var results = await performRegularQuery(query);
            if (results.Count() == 0)
            {
                return await performHebrewQuery(query);
            }

            return results;
        }

        private async Task<IEnumerable<ReleaseInfo>> performHebrewQuery(TorznabQuery query)
        {
            var name = await getHebName(query.SearchTerm);

            if (string.IsNullOrEmpty(name))
            {
                return new List<ReleaseInfo>();
            }
            else
            {
                return await performRegularQuery(query, name);
            }
        }

        private async Task<IEnumerable<ReleaseInfo>> performRegularQuery(TorznabQuery query, string hebName = null)
        {
            var releases = new List<ReleaseInfo>();
            var searchurls = new List<string>();
            var searchUrl = SearchUrl;
            var queryCollection = new NameValueCollection();
            var searchString = query.GetQueryString();

            if (hebName != null)
            {
                searchString = hebName + " - עונה " + query.Season + " פרק " + query.Episode;
            }

            int categoryCounter = 1;
            foreach (var cat in MapTorznabCapsToTrackers(query))
            {
                searchUrl += "c" + categoryCounter.ToString() + "=" + cat + "&";
                categoryCounter++;
            }


            if (string.IsNullOrWhiteSpace(searchString))
            {
                searchUrl = SiteLink + "index.php?name=torrents";
            }
            else
            {
                var strEncoded = HttpUtility.UrlEncode(searchString, Encoding.GetEncoding("Windows-1255"));
                searchUrl += "text=" + strEncoded + "&category=0&search=1";
            }

            var data = await RequestStringWithCookiesAndRetry(searchUrl);
            try
            {
                CQ dom = data.Content;
                ReleaseInfo release;

                int rowCount = 0;
                var rows = dom["#collapseobj_module_17 > tr"];
                foreach (var row in rows)
                {
                    CQ qRow = row.Cq();
                    if (rowCount < 1 || qRow.Children().Count() != 9) //skip 1 row because there's an empty row 
                    {
                        rowCount++;
                        continue;
                    }

                    release = new ReleaseInfo();
                    release.Description = qRow.Find("td:nth-child(2) > a").Text(); ;

                    if (hebName != null)
                    {
                        release.Title = query.SearchTerm + " " + release.Description.Substring(release.Description.IndexOf(string.Format("S{0:D2}E{1:D2}", query.Season, int.Parse(query.Episode))));
                    }
                    else
                    {
                        const string DELIMITER = " | ";
                        release.Title = release.Description.Substring(release.Description.IndexOf(DELIMITER) + DELIMITER.Length);
                    }

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;

                    int seeders, peers;
                    if (ParseUtil.TryCoerceInt(qRow.Find("td:nth-child(7) > div").Text(), out seeders))
                    {
                        release.Seeders = seeders;
                        if (ParseUtil.TryCoerceInt(qRow.Find("td:nth-child(8) > div").Text(), out peers))
                        {
                            release.Peers = peers + release.Seeders;
                        }
                    }

                    string fullSize = qRow.Find("td:nth-child(5) > div").Text();
                    release.Size = ReleaseInfo.GetBytes(fullSize);

                    release.Guid = new Uri(qRow.Find("td:nth-child(2) > a").Attr("href"));
                    release.Link = new Uri(SiteLink + qRow.Find("td:nth-child(3) > a").Attr("href"));
                    release.Comments = release.Guid;

                    string[] dateSplit = qRow.Find("td:nth-child(2) > span.torrentstime").Text().Split(' ');
                    string dateString = dateSplit[1] + " " + dateSplit[3];
                    release.PublishDate = DateTime.ParseExact(dateString, "dd-MM-yy HH:mm", CultureInfo.InvariantCulture);

                    string category = qRow.Find("script:nth-child(1)").Text();
                    int index = category.IndexOf("category=");
                    if (index == -1)
                    {
                        /// Other type
                        category = "17";
                    }
                    else
                    {
                        category = category.Substring(index + "category=".Length, 2);
                        if (category[1] == '\\')
                        {
                            category = category[0].ToString();
                        }
                    }

                    release.Category = MapTrackerCatToNewznab(category);

                    var grabs = qRow.Find("td:nth-child(6)").Text();
                    release.Grabs = ParseUtil.CoerceInt(grabs);

                    if (qRow.Find("img[src=\"/images/FL.png\"]").Length >= 1)
                        release.DownloadVolumeFactor = 0;
                    else
                        release.DownloadVolumeFactor = 1;

                    release.UploadVolumeFactor = 1;

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(data.Content, ex);
            }

            return releases;
        }

        private async Task<string> getHebName(string searchTerm)
        {
            const string site = "http://thetvdb.com";
            var url = site + "/index.php?searchseriesid=&tab=listseries&function=Search&";
            url += "string=" + searchTerm; // eretz + nehedert


            var results = await RequestStringWithCookies(url);

            CQ dom = results.Content;

            int rowCount = 0;
            var rows = dom["#listtable > tbody > tr"];

            foreach (var row in rows)
            {
                if (rowCount < 1)
                {
                    rowCount++;
                    continue;
                }

                CQ qRow = row.Cq();
                CQ link = qRow.Find("td:nth-child(1) > a");
                if (link.Text().Trim().ToLower() == searchTerm.Trim().ToLower())
                {
                    var address = link.Attr("href");
                    if (string.IsNullOrEmpty(address)) { continue; }

                    var realAddress = site + address.Replace("lid=7", "lid=24");
                    var realData = await RequestStringWithCookies(realAddress);

                    CQ realDom = realData.Content;
                    return realDom["#content:nth-child(1) > h1"].Text();
                }
            }

            return string.Empty;
        }
    }
}
