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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jackett.Models.IndexerConfig;

namespace Jackett.Indexers
{
    public class SpeedCD : BaseIndexer, IIndexer
    {
        private string LoginUrl { get { return SiteLink + "take.login.php"; } }
        private string SearchUrl { get { return SiteLink + "V3/API/API.php"; } }
        private string CommentsUrl { get { return SiteLink + "t/{0}"; } }
        private string DownloadUrl { get { return SiteLink + "download.php?torrent={0}"; } }

        new ConfigurationDataBasicLogin configData
        {
            get { return (ConfigurationDataBasicLogin)base.configData; }
            set { base.configData = value; }
        }

        public SpeedCD(IIndexerManagerService i, Logger l, IWebClient wc, IProtectionService ps)
            : base(name: "Speed.cd",
                description: "Your home now!",
                link: "http://speed.cd/",
                caps: new TorznabCapabilities(),
                manager: i,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLogin())
        {
            AddCategoryMapping("1", TorznabCatType.MoviesOther);
            AddCategoryMapping("42", TorznabCatType.Movies);
            AddCategoryMapping("32", TorznabCatType.Movies);
            AddCategoryMapping("43", TorznabCatType.MoviesHD);
            AddCategoryMapping("47", TorznabCatType.Movies);
            AddCategoryMapping("28", TorznabCatType.MoviesBluRay);
            AddCategoryMapping("48", TorznabCatType.Movies3D);
            AddCategoryMapping("40", TorznabCatType.MoviesDVD);
            AddCategoryMapping("49", TorznabCatType.TVHD);
            AddCategoryMapping("50", TorznabCatType.TVSport);
            AddCategoryMapping("52", TorznabCatType.TVHD);
            AddCategoryMapping("53", TorznabCatType.TVSD);
            AddCategoryMapping("41", TorznabCatType.TV);
            AddCategoryMapping("55", TorznabCatType.TV);
            AddCategoryMapping("2", TorznabCatType.TV);
            AddCategoryMapping("30", TorznabCatType.TVAnime);
            AddCategoryMapping("25", TorznabCatType.PCISO);
            AddCategoryMapping("39", TorznabCatType.ConsoleWii);
            AddCategoryMapping("45", TorznabCatType.ConsolePS3);
            AddCategoryMapping("35", TorznabCatType.Console);
            AddCategoryMapping("33", TorznabCatType.ConsoleXbox360);
            AddCategoryMapping("46", TorznabCatType.PCPhoneOther);
            AddCategoryMapping("24", TorznabCatType.PC0day);
            AddCategoryMapping("51", TorznabCatType.PCMac);
            AddCategoryMapping("27", TorznabCatType.Books);
            AddCategoryMapping("26", TorznabCatType.Audio);
            AddCategoryMapping("44", TorznabCatType.Audio);
            AddCategoryMapping("29", TorznabCatType.AudioVideo);
        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, SiteLink);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("logout.php"), () =>
            {
                CQ dom = result.Content;
                var errorMessage = dom["h5"].First().Text().Trim();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            Dictionary<string, string> qParams = new Dictionary<string, string>();
            qParams.Add("jxt", "4");
            qParams.Add("jxw", "b");

            if (!string.IsNullOrEmpty(query.SanitizedSearchTerm))
            {
                qParams.Add("search", query.SanitizedSearchTerm);
            }

            List<string> catList = MapTorznabCapsToTrackers(query);
            foreach (string cat in catList)
            {
                qParams.Add("c" + cat, "1");
            }
            
            var response = await PostDataWithCookiesAndRetry(SearchUrl, qParams);
            try
            {
                var jsonResult = JObject.Parse(response.Content);
                var resultArray = ((JArray)jsonResult["Fs"])[0]["Cn"]["torrents"];
                foreach (var jobj in resultArray)
                {
                    var release = new ReleaseInfo();

                    var id = (int)jobj["id"];
                    release.Comments = new Uri(string.Format(CommentsUrl, id));
                    release.Guid = release.Comments;
                    release.Link = new Uri(string.Format(DownloadUrl, id));

                    release.Title = Regex.Replace((string)jobj["name"], "<.*?>", String.Empty);

                    var SizeStr = ((string)jobj["size"]);
                    release.Size = ReleaseInfo.GetBytes(SizeStr);

                    var cat = (int)jobj["cat"];
                    release.Category = MapTrackerCatToNewznab(cat.ToString());

                    release.Seeders = ParseUtil.CoerceInt((string)jobj["seed"]);
                    release.Peers = ParseUtil.CoerceInt((string)jobj["leech"]) + release.Seeders;

                    // ex: Tuesday, May 26, 2015 at 6:00pm
                    var dateStr = new Regex("title=\"(.*?)\"").Match((string)jobj["added"]).Groups[1].ToString();
                    dateStr = dateStr.Replace(" at", "");
                    var dateTime = DateTime.ParseExact(dateStr, "dddd, MMMM d, yyyy h:mmtt", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                    release.PublishDate = dateTime.ToLocalTime();

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.Content, ex);
            }
            return releases;
        }
    }
}
