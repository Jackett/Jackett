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

namespace Jackett.Indexers
{
    public class TorrentShack : BaseIndexer, IIndexer
    {
        private string LoginUrl { get { return SiteLink + "login.php"; } }
        private string SearchUrl { get { return SiteLink + "torrents.php"; } }

        new ConfigurationDataBasicLogin configData
        {
            get { return (ConfigurationDataBasicLogin)base.configData; }
            set { base.configData = value; }
        }

        public TorrentShack(IIndexerManagerService i, Logger l, IWebClient wc, IProtectionService ps)
            : base(name: "TorrentShack",
                description: "TorrentShack",
                link: "https://torrentshack.me/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                client: wc,
                manager: i,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLogin())
        {
            Encoding = Encoding.GetEncoding("UTF-8");
            Language = "en-us";
            Type = "private";

            AddCategoryMapping(600, TorznabCatType.TVHD); // TV/HD
            AddCategoryMapping(960, TorznabCatType.MoviesForeign); // Foreign
            AddCategoryMapping(300, TorznabCatType.MoviesHD); // Movies/HD
            AddCategoryMapping(200, TorznabCatType.PCGames); // Games/PC
            AddCategoryMapping(100, TorznabCatType.PC0day); // Apps/PC
            AddCategoryMapping(450, TorznabCatType.AudioMP3); // Music/MP3
            AddCategoryMapping(280, TorznabCatType.PCPhoneOther); // HandHeld
            AddCategoryMapping(620, TorznabCatType.TVSD); // TV/SD
            AddCategoryMapping(320, TorznabCatType.MoviesOther); // REMUX
            AddCategoryMapping(400, TorznabCatType.MoviesSD); // Movies/SD
            AddCategoryMapping(240, TorznabCatType.ConsolePS3); // Games/PS3
            AddCategoryMapping(150, TorznabCatType.PC0day); // Apps/misc
            AddCategoryMapping(480, TorznabCatType.AudioLossless); // Music/FLAC
            AddCategoryMapping(180, TorznabCatType.BooksEbook); // eBooks
            AddCategoryMapping(700, TorznabCatType.TVOTHER); // TV/DVDrip
            AddCategoryMapping(970, TorznabCatType.MoviesBluRay); // Full Blu-ray
            AddCategoryMapping(350, TorznabCatType.MoviesDVD); // Movies/DVD-R
            AddCategoryMapping(260, TorznabCatType.ConsoleXbox360); // Games/Xbox360
            AddCategoryMapping(500, TorznabCatType.AudioVideo); // Music/Videos
            AddCategoryMapping(181, TorznabCatType.AudioAudiobook); // AudioBooks
            AddCategoryMapping(981, TorznabCatType.TVHD); // TV-HD Pack
            AddCategoryMapping(850, TorznabCatType.TVAnime); // Anime
            AddCategoryMapping(982, TorznabCatType.MoviesHD); // Movies-HD Pack
            AddCategoryMapping(986, TorznabCatType.PCGames); // Games Pack
            AddCategoryMapping(984, TorznabCatType.AudioMP3); // MP3 Pack
            AddCategoryMapping(800, TorznabCatType.Other); // Misc
            AddCategoryMapping(980, TorznabCatType.TVSD); // TV-SD Pack
            AddCategoryMapping(983, TorznabCatType.TVSD); // Movies-SD Pack
            AddCategoryMapping(985, TorznabCatType.AudioLossless); // FLAC Pack
        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "keeplogged", "1" },
                { "login", "Login" }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, LoginUrl);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("logout.php"), () =>
            {
                CQ dom = result.Content;
                var messageEl = dom["#loginform"];
                messageEl.Children("table").Remove();
                var errorMessage = messageEl.Text().Trim();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();

            var searchUrl = SearchUrl;
            var queryCollection = new NameValueCollection();
            queryCollection.Add("release_type", "both");
            queryCollection.Add("searchtags", "");
            queryCollection.Add("tags_type", "0");
            queryCollection.Add("order_by", "s3");
            queryCollection.Add("order_way", "desc");
            queryCollection.Add("torrent_preset", "all");

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                queryCollection.Add("searchstr", searchString);
            }

            foreach (var cat in MapTorznabCapsToTrackers(query))
            {
                queryCollection.Add("filter_cat["+cat+"]", "1");
            }

            searchUrl += "?" + queryCollection.GetQueryString();

            var results = await RequestStringWithCookiesAndRetry(searchUrl);
            try
            {
                CQ dom = results.Content;
                var rows = dom["#torrent_table > tbody > tr.torrent"];
                foreach (var row in rows)
                {
                    CQ qRow = row.Cq();
                    var release = new ReleaseInfo();

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;
                    release.Title = qRow.Find(".torrent_name_link").Text();
                    release.Description = release.Title;
                    release.Guid = new Uri(SiteLink + "/" + qRow.Find(".torrent_name_link").Parent().Attr("href"));
                    release.Comments = release.Guid;
                    release.Link = new Uri(SiteLink + "/" + qRow.Find(".torrent_handle_links > a").First().Attr("href"));

                    var dateStr = qRow.Find(".time").Text().Trim();
                    release.PublishDate = DateTimeUtil.FromTimeAgo(dateStr);

                    var sizeStr = qRow.Find(".size")[0].ChildNodes[0].NodeValue;
                    release.Size = ReleaseInfo.GetBytes(sizeStr);
                    release.Seeders = ParseUtil.CoerceInt(qRow.Children().ElementAt(6).InnerText.Trim());
                    release.Peers = ParseUtil.CoerceInt(qRow.Children().ElementAt(7).InnerText.Trim()) + release.Seeders;

                    var grabs = qRow.Find("td:nth-child(6)").Text();
                    release.Grabs = ParseUtil.CoerceInt(grabs);

                    release.DownloadVolumeFactor = 0; // ratioless
                    release.UploadVolumeFactor = 1;

                    var qCat = qRow.Find("a[href^=\"torrents.php?filter_cat\"]");
                    var cat = qCat.Attr("href").Split('[')[1].Split(']')[0];
                    release.Category = MapTrackerCatToNewznab(cat);

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.Content, ex);
            }
            return releases;
        }
    }
}
