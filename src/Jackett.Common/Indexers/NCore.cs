using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CsQuery;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    public class NCore : BaseWebIndexer
    {
        private string LoginUrl { get { return SiteLink + "login.php"; } }
        private string SearchUrl { get { return SiteLink + "torrents.php"; } }
        private string[] LanguageCats = new string[] { "xvidser", "dvdser", "hdser", "xvid", "dvd", "dvd9", "hd", "mp3", "lossless", "ebook" };

        private new ConfigurationDataNCore configData
        {
            get { return (ConfigurationDataNCore)base.configData; }
            set { base.configData = value; }
        }

        public NCore(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(name: "nCore",
                description: "A Hungarian private torrent site.",
                link: "https://ncore.cc/",
                caps: new TorznabCapabilities(),
                configService: configService,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataNCore())
        {
            Encoding = Encoding.UTF8;
            Language = "hu-hu";
            Type = "private";

            AddCategoryMapping("xvid_hun", TorznabCatType.MoviesSD, "Film SD/HU");
            AddCategoryMapping("xvid", TorznabCatType.MoviesSD, "Film SD/EN");
            AddCategoryMapping("dvd_hun", TorznabCatType.MoviesDVD, "Film DVDR/HU");
            AddCategoryMapping("dvd", TorznabCatType.MoviesDVD, "Film DVDR/EN");
            AddCategoryMapping("dvd9_hun", TorznabCatType.MoviesDVD, "Film DVD9/HU");
            AddCategoryMapping("dvd9", TorznabCatType.MoviesDVD, "Film DVD9/EN");
            AddCategoryMapping("hd_hun", TorznabCatType.MoviesHD, "Film HD/HU");
            AddCategoryMapping("hd", TorznabCatType.MoviesHD, "Film HD/EN");

            AddCategoryMapping("xvidser_hun", TorznabCatType.TVSD, "Sorozat SD/HU");
            AddCategoryMapping("xvidser", TorznabCatType.TVSD, "Sorozat SD/EN");
            AddCategoryMapping("dvdser_hun", TorznabCatType.TVSD, "Sorozat DVDR/HU");
            AddCategoryMapping("dvdser", TorznabCatType.TVSD, "Sorozat DVDR/EN");
            AddCategoryMapping("hdser_hun", TorznabCatType.TVHD, "Sorozat HD/HU");
            AddCategoryMapping("hdser", TorznabCatType.TVHD, "Sorozat HD/EN");

            AddCategoryMapping("mp3_hun", TorznabCatType.AudioMP3, "Zene MP3/HU");
            AddCategoryMapping("mp3", TorznabCatType.AudioMP3, "Zene MP3/EN");
            AddCategoryMapping("lossless_hun", TorznabCatType.AudioLossless, "Zene Lossless/HU");
            AddCategoryMapping("lossless", TorznabCatType.AudioLossless, "Zene Lossless/EN");
            AddCategoryMapping("clip", TorznabCatType.AudioVideo, "Zene Klip");

            AddCategoryMapping("xxx_xvid", TorznabCatType.XXXXviD, "XXX SD");
            AddCategoryMapping("xxx_dvd", TorznabCatType.XXXDVD, "XXX DVDR");
            AddCategoryMapping("xxx_imageset", TorznabCatType.XXXImageset, "XXX Imageset");
            AddCategoryMapping("xxx_hd", TorznabCatType.XXX, "XXX HD");

            AddCategoryMapping("game_iso", TorznabCatType.PCGames, "Játék PC/ISO");
            AddCategoryMapping("game_rip", TorznabCatType.PCGames, "Játék PC/RIP");
            AddCategoryMapping("console", TorznabCatType.Console, "Játék Konzol");

            AddCategoryMapping("iso", TorznabCatType.PCISO, "Program Prog/ISO");
            AddCategoryMapping("misc", TorznabCatType.PC0day, "Program Prog/RIP");
            AddCategoryMapping("mobil", TorznabCatType.PCPhoneOther, "Program Prog/Mobil");

            AddCategoryMapping("ebook_hun", TorznabCatType.Books, "Könyv eBook/HU");
            AddCategoryMapping("ebook", TorznabCatType.Books, "Könyv eBook/EN");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            if (configData.Hungarian.Value == false && configData.English.Value == false)
                throw new ExceptionWithConfigData("Please select atleast one language.", configData);

            var loginPage = await RequestStringWithCookies(LoginUrl, string.Empty);
            var pairs = new Dictionary<string, string> {
                { "nev", configData.Username.Value },
                { "pass", configData.Password.Value },
                { "ne_leptessen_ki", "1"},
                { "set_lang", "en" },
                { "submitted", "1" },
                { "submit", "Access!" }
            };

            if (!string.IsNullOrEmpty(configData.TwoFactor.Value))
            {
                pairs.Add("2factor", configData.TwoFactor.Value);
            }

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, loginPage.Cookies, true, referer: SiteLink);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("profile.php"), () =>
            {
                CQ dom = result.Content;
                var messageEl = dom["#hibauzenet table tbody tr"];
                var msgContainer = messageEl.Get(0).ChildElements.ElementAt(1);
                var errorMessage = msgContainer != null ? msgContainer.InnerText : "Error while trying to login.";
                throw new ExceptionWithConfigData(errorMessage, configData);
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();
            var pairs = new List<KeyValuePair<string, string>>();

            pairs.Add(new KeyValuePair<string, string>("nyit_sorozat_resz", "true"));
            pairs.Add(new KeyValuePair<string, string>("miben", "name"));
            pairs.Add(new KeyValuePair<string, string>("tipus", "kivalasztottak_kozott"));
            pairs.Add(new KeyValuePair<string, string>("submit.x", "1"));
            pairs.Add(new KeyValuePair<string, string>("submit.y", "1"));
            pairs.Add(new KeyValuePair<string, string>("submit", "Ok"));
            pairs.Add(new KeyValuePair<string, string>("mire", searchString));

            var cats = MapTorznabCapsToTrackers(query);

            if (cats.Count == 0)
                cats = GetAllTrackerCategories();

            foreach (var lcat in LanguageCats)
            {
                if (!configData.Hungarian.Value)
                    cats.Remove(lcat + "_hun");
                if (!configData.English.Value)
                    cats.Remove(lcat);
            }

            foreach (var cat in cats)
            {
                pairs.Add(new KeyValuePair<string, string>("kivalasztott_tipus[]", cat));
            }

            var results = await PostDataWithCookiesAndRetry(SearchUrl, pairs);

            try
            {
                CQ dom = results.Content;

                ReleaseInfo release;
                var rows = dom[".box_torrent_all"].Find(".box_torrent");

                foreach (var row in rows)
                {
                    CQ qRow = row.Cq();

                    var key = dom["link[rel=alternate]"].First().Attr("href").Split('=').Last();

                    release = new ReleaseInfo();
                    var torrentTxt = qRow.Find(".torrent_txt, .torrent_txt2").Find("a").Get(0);
                    //if (torrentTxt == null) continue;
                    release.Title = torrentTxt.GetAttribute("title");
                    release.Description = qRow.Find("div.siterank").Text();
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;
                    release.DownloadVolumeFactor = 0;
                    release.UploadVolumeFactor = 1;

                    string downloadLink = SiteLink + torrentTxt.GetAttribute("href");
                    string downloadId = downloadLink.Substring(downloadLink.IndexOf("&id=") + 4);

                    release.Link = new Uri(SiteLink.ToString() + "torrents.php?action=download&id=" + downloadId + "&key=" + key);
                    release.Comments = new Uri(SiteLink.ToString() + "torrents.php?action=details&id=" + downloadId);
                    release.Guid = new Uri(release.Comments.ToString() + "#comments"); ;
                    release.Seeders = ParseUtil.CoerceInt(qRow.Find(".box_s2").Find("a").First().Text());
                    release.Peers = ParseUtil.CoerceInt(qRow.Find(".box_l2").Find("a").First().Text()) + release.Seeders;
                    var imdblink = qRow.Find("a[href*=\".imdb.com/title\"]").Attr("href");
                    release.Imdb = ParseUtil.GetLongFromString(imdblink);
                    var banner = qRow.Find("img.infobar_ico").Attr("onmouseover");
                    if (banner != null)
                    {
                        Regex BannerRegEx = new Regex(@"mutat\('(.*?)', '", RegexOptions.Compiled);
                        var BannerMatch = BannerRegEx.Match(banner);
                        var bannerurl = BannerMatch.Groups[1].Value;
                        release.BannerUrl = new Uri(bannerurl);
                    }
                    release.PublishDate = DateTime.Parse(qRow.Find(".box_feltoltve2").Get(0).InnerHTML.Replace("<br />", " "), CultureInfo.InvariantCulture);
                    string[] sizeSplit = qRow.Find(".box_meret2").Get(0).InnerText.Split(' ');
                    release.Size = ReleaseInfo.GetBytes(sizeSplit[1].ToLower(), ParseUtil.CoerceFloat(sizeSplit[0]));
                    string catlink = qRow.Find("a:has(img[class='categ_link'])").First().Attr("href");
                    string cat = ParseUtil.GetArgumentFromQueryString(catlink, "tipus");
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
