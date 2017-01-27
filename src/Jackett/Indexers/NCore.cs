using CsQuery;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json;
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
using Jackett.Models.IndexerConfig.Bespoke;

namespace Jackett.Indexers
{
    public class NCore : BaseIndexer, IIndexer
    {
        private string LoginUrl { get { return SiteLink + "login.php"; } }
        private string SearchUrl { get { return SiteLink + "torrents.php"; } }

        new ConfigurationDataNCore configData
        {
            get { return (ConfigurationDataNCore)base.configData; }
            set { base.configData = value; }
        }

        public NCore(IIndexerManagerService i, IWebClient wc, Logger l, IProtectionService ps)
            : base(name: "nCore",
                description: "A Hungarian private torrent site.",
                link: "https://ncore.cc/",
                caps: new TorznabCapabilities(),
                manager: i,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataNCore())
        {
            Encoding = Encoding.UTF8;
            Language = "hu-hu";
            Type = "private";

            AddCategoryMapping("Sorozatok SD felbontásban angolul és egyéb nyelveken.", TorznabCatType.TVSD);
            AddCategoryMapping("Nagyfelbontású sorozatok angolul és egyéb nyelveken.", TorznabCatType.TVHD);
            AddCategoryMapping("Sorozatok SD felbontásban magyarul.", TorznabCatType.TVFOREIGN);
            AddCategoryMapping("Nagyfelbontású sorozatok magyarul.", TorznabCatType.TVFOREIGN);
            AddCategoryMapping("Nagyfelbontású filmek, angolul és egyéb nyelveken.", TorznabCatType.MoviesHD);
            AddCategoryMapping("Nagyfelbontású filmek, magyarul.", TorznabCatType.MoviesForeign);
            AddCategoryMapping("Filmek tömörített formátumban, angolul.", TorznabCatType.MoviesSD);
            AddCategoryMapping("Filmek tömörített formátumban, magyarul.", TorznabCatType.MoviesForeign);
            AddCategoryMapping("Filmek DVD-n, angolul és egyéb nyelveken.", TorznabCatType.MoviesDVD);
            AddCategoryMapping("Filmek DVD-n, magyarul.", TorznabCatType.MoviesDVD);
            AddCategoryMapping("Filmek DVD9 formátumban angolul és egyéb nyelveken.", TorznabCatType.MoviesDVD);
            AddCategoryMapping("Filmek DVD9 formátumban magyarul.", TorznabCatType.MoviesDVD);
            AddCategoryMapping("Zene külföldi előadóktól (MP3).", TorznabCatType.AudioMP3);
            AddCategoryMapping("Zene magyar előadóktól (MP3).", TorznabCatType.AudioForeign);
            AddCategoryMapping("Lossless, azaz veszteségmentes formátumú zene külföldi eloadótól (APE/FLAC/DTS/WAV).", TorznabCatType.AudioLossless);
            AddCategoryMapping("Lossless, azaz veszteségmentes formátumú zene magyar eloadótól (APE/FLAC/DTS/WAV).", TorznabCatType.AudioLossless);
            AddCategoryMapping("Koncertek, Zenei anyagok.", TorznabCatType.AudioVideo);
            AddCategoryMapping("Erotikus tartalom.", TorznabCatType.XXXXviD);
            AddCategoryMapping("Erotikus tartalom (HD).", TorznabCatType.XXXx264);
            AddCategoryMapping("Erotikus tartalom (DVD).", TorznabCatType.XXXDVD);
            AddCategoryMapping("Erotikus tartalom (Képsorozatok).", TorznabCatType.XXXImageset);
            AddCategoryMapping("Könyvek, dokumentációk, tananyagok, eBook-ok angolul.", TorznabCatType.BooksEbook);
            AddCategoryMapping("Könyvek, dokumentációk, tananyagok, eBook-ok magyarul.", TorznabCatType.BooksForeign);
            AddCategoryMapping("Játékok CD/DVD képben.", TorznabCatType.PCGames);
            AddCategoryMapping("Szoftverek, programok CD/DVD képben.", TorznabCatType.PCISO);
            AddCategoryMapping("Programok és játékok mobilra.", TorznabCatType.PCPhoneOther);
            AddCategoryMapping("Játékok Xbox-ra, PS-re, PSP-re, GC-re, Wii-re.", TorznabCatType.Console);
        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
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

        List<KeyValuePair<string, string>> CreateKeyValueList(params string[][] keyValues)
        {
            var list = new List<KeyValuePair<string, string>>();
            foreach (var d in keyValues)
            {
                list.Add(new KeyValuePair<string, string>(d[0], d[1]));
            }
            return list;
        }

        private IEnumerable<KeyValuePair<string, string>> GetSearchFormData(string searchString)
        {
            const string searchTypeKey = "kivalasztott_tipus[]";
            var baseList = CreateKeyValueList(
                new[] { "nyit_sorozat_resz", "true" },
                new[] { "miben", "name" },
                new[] { "tipus", "kivalasztottak_kozott" },
                new[] { "submit.x", "1" },
                new[] { "submit.y", "1" },
                new[] { "submit", "Ok" },
                new[] { "mire", searchString }
            );

            if (configData.English.Value)
            {
                baseList.AddRange(CreateKeyValueList(
                    new[] { searchTypeKey, "xvidser" },
                    new[] { searchTypeKey, "dvdser" },
                    new[] { searchTypeKey, "hdser" },
                    new[] { searchTypeKey, "xvid" },
                    new[] { searchTypeKey, "dvd" },
                    new[] { searchTypeKey, "dvd9" },
                    new[] { searchTypeKey, "hd" },
                    new[] { searchTypeKey, "dvdser" },
                    new[] { searchTypeKey, "xxx_xvid" },
                    new[] { searchTypeKey, "xxx_dvd" },
                    new[] { searchTypeKey, "xxx_imageset" },
                    new[] { searchTypeKey, "xxx_hd" },
                    new[] { searchTypeKey, "mp3" },
                    new[] { searchTypeKey, "lossless" },
                    new[] { searchTypeKey, "clip" },
                    new[] { searchTypeKey, "game_iso" },
                    new[] { searchTypeKey, "console" },
                    new[] { searchTypeKey, "ebook" },
                    new[] { searchTypeKey, "iso" },
                    new[] { searchTypeKey, "mobil" }
                ));
            }

            if (configData.Hungarian.Value)
            {
                baseList.AddRange(CreateKeyValueList(
                    new[] { searchTypeKey, "xvidser_hun" },
                    new[] { searchTypeKey, "dvdser_hun" },
                    new[] { searchTypeKey, "hdser_hun" },
                    new[] { searchTypeKey, "xvid_hun" },
                    new[] { searchTypeKey, "dvd_hun" },
                    new[] { searchTypeKey, "dvd9_hun" },
                    new[] { searchTypeKey, "hd_hun" },
                    new[] { searchTypeKey, "mp3_hun" },
                    new[] { searchTypeKey, "lossless_hun" },
                    new[] { searchTypeKey, "ebook_hun" }
                ));
            }
            return baseList;
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var results = await PostDataWithCookiesAndRetry(SearchUrl, GetSearchFormData(query.GetQueryString()));

            try
            {
                CQ dom = results.Content;

                ReleaseInfo release;
                var rows = dom[".box_torrent_all"].Find(".box_torrent");

                foreach (var row in rows)
                {
                    CQ qRow = row.Cq();

                    release = new ReleaseInfo();
                    var torrentTxt = qRow.Find(".torrent_txt").Find("a").Get(0);
                    if (torrentTxt == null) continue;
                    release.Title = torrentTxt.GetAttribute("title");
                    release.Description = release.Title;
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;
                    release.DownloadVolumeFactor = 0;
                    release.UploadVolumeFactor = 1;

                    string downloadLink = SiteLink + torrentTxt.GetAttribute("href");
                    string downloadId = downloadLink.Substring(downloadLink.IndexOf("&id=") + 4);

                    release.Link = new Uri(SiteLink.ToString() + "torrents.php?action=download&id=" + downloadId);
                    release.Comments = new Uri(SiteLink.ToString() + "torrents.php?action=details&id=" + downloadId);
                    release.Guid = new Uri(release.Comments.ToString() + "#comments"); ;
                    release.Seeders = ParseUtil.CoerceInt(qRow.Find(".box_s2").Find("a").First().Text());
                    release.Peers = ParseUtil.CoerceInt(qRow.Find(".box_l2").Find("a").First().Text()) + release.Seeders;
                    release.PublishDate = DateTime.Parse(qRow.Find(".box_feltoltve2").Get(0).InnerHTML.Replace("<br />", " "), CultureInfo.InvariantCulture);
                    string[] sizeSplit = qRow.Find(".box_meret2").Get(0).InnerText.Split(' ');
                    release.Size = ReleaseInfo.GetBytes(sizeSplit[1].ToLower(), ParseUtil.CoerceFloat(sizeSplit[0]));
                    string cat = qRow.Find("img[class='categ_link']").First().Attr("title").Trim();
                    release.Category = MapTrackerCatToNewznab(cat);

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.Content, ex);
            }


            return releases.ToArray();
        }

    }
}
