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

namespace Jackett.Indexers
{
    public class nCore : BaseIndexer, IIndexer
    {
        private string SearchUrl = "https://ncore.cc/torrents.php";
        private static string LoginUrl = "https://ncore.cc/login.php";
        private readonly string LoggedInUrl = "https://ncore.cc/index.php";
        //private string cookieHeader = "";
        private JToken configData = null;

        private readonly string enSearch = "torrents.php?oldal=1&tipus=kivalasztottak_kozott&kivalasztott_tipus=xvidser,dvdser,hdser&mire={0}&miben=name";
        private readonly string hunSearch = "torrents.php?oldal=1&tipus=kivalasztottak_kozott&kivalasztott_tipus=xvidser_hun,dvdser_hun,hdser_hun,mire={0}&miben=name";
        private readonly string enHunSearch = "torrents.php?oldal=1&tipus=kivalasztottak_kozott&kivalasztott_tipus=xvidser_hun,xvidser,dvdser_hun,dvdser,hdser_hun,hdser&mire={0}&miben=name";

        private string SearchUrlEn { get { return SiteLink.ToString() + enSearch; } }
        private string SearchUrlHun { get { return SiteLink.ToString() + hunSearch; } }
        private string SearchUrlEnHun { get { return SiteLink.ToString() + enHunSearch; } }


        public nCore(IIndexerManagerService i, IWebClient wc, Logger l)
            : base(name: "nCore",
                description: "A Hungarian private torrent site.",
                link: "https://ncore.cc/",
                caps: TorznabCapsUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                client:wc,
                logger: l)
        {
            SearchUrl = SearchUrlEnHun;
            //webclient = wc;
        }

        public Task<ConfigurationData> GetConfigurationForSetup()
        {
            var config = configData == null ? new ConfigurationDatanCore() : new ConfigurationDatanCore(configData);
            return Task.FromResult<ConfigurationData>(new ConfigurationDatanCore(configData));
        }

        public async Task ApplyConfiguration(JToken configJson)
        {
            var config = new ConfigurationDatanCore();
            config.LoadValuesFromJson(configJson);

            if (config.Hungarian.Value == false && config.English.Value == false)
                throw new ExceptionWithConfigData("Please select atleast one language.", (ConfigurationData)config);

            var pairs = new Dictionary<string, string> {
				{ "nev", config.Username.Value },
				{ "pass", config.Password.Value },
                {"ne_leptessen_ki", "on"}
			};

            var response = await webclient.GetString(new Utils.Clients.WebRequest()
            {
                Url = LoginUrl,
                PostData = pairs,
                Referer = SiteLink.ToString(),
                Type = RequestType.POST,
            });

            if (!response.RedirectingTo.Equals("index.php"))
            {
                var errorMessage = "Couldn't login";
                throw new ExceptionWithConfigData(errorMessage, (ConfigurationData)config);
            }
            else
            {
                var configSaveData = new JObject();
                cookieHeader = response.Cookies;

                cookieHeader = cookieHeader.Substring(0, cookieHeader.IndexOf(' ') - 1) + ";stilus=brutecore; nyelv=hu";
                configSaveData["cookies"] = cookieHeader;
                configSaveData["config"] = configData = config.ToJson();
                SaveConfig(configSaveData);
                IsConfigured = true;
            }
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            var searchString = query.SanitizedSearchTerm + " " + query.GetEpisodeSearchString();
            var episodeSearchUrl = string.Format(SearchUrl, HttpUtility.UrlEncode(searchString));

            //var response = await webclient.GetString(new Utils.Clients.WebRequest()
            //{
            //    Url = episodeSearchUrl,
            //    Cookies = cookieHeader,
            //    Referer = SiteLink.ToString(),
            //});

           // var results = response.Content;
            var results= "";
            try
            {
                CQ dom = results;

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

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                //OnParseError(response.Content, ex);
            }


            return releases.ToArray();
        }

        //public void LoadFromSavedConfiguration(JToken jsonConfig)
        //{
        //    if (jsonConfig["config"] != null)
        //    {
        //        string hun, eng;
        //        Dictionary<string, string>[] configDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(jsonConfig["config"].ToString());
        //        configDictionary[2].TryGetValue("value", out hun);
        //        configDictionary[3].TryGetValue("value", out eng);

        //        bool isHun = Boolean.Parse(hun);
        //        bool isEng = Boolean.Parse(eng);

        //        if (isHun && isEng)
        //            SearchUrl = SearchUrlEnHun;
        //        else if (isHun && !isEng)
        //            SearchUrl = SearchUrlHun;
        //        else if (!isHun && isEng)
        //            SearchUrl = SearchUrlEn;

        //        configData = jsonConfig["config"];
        //    }
        //    cookieHeader = (string)jsonConfig["cookies"];
        //    IsConfigured = true;
        //}

    }
}