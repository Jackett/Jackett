using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    public class PolishTracker : BaseWebIndexer
    {
        private string LoginUrl { get { return SiteLink + "login"; } }
        private string TorrentApiUrl { get { return SiteLink + "apitorrents"; } }
        private string CDNUrl { get { return "https://cdn.pte.nu/"; } }

        public override string[] LegacySiteLinks { get; protected set; } = new string[] {
            "https://polishtracker.net/",
            };

        private new ConfigurationDataBasicLoginWithRSSAndDisplay configData
        {
            get { return (ConfigurationDataBasicLoginWithRSSAndDisplay)base.configData; }
            set { base.configData = value; }
        }

        public PolishTracker(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(name: "PolishTracker",
                   description: "Polish Tracker is a POLISH Private site for 0DAY / MOVIES / GENERAL",
                   link: "https://pte.nu/",
                   caps: new TorznabCapabilities(),
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   configData: new ConfigurationDataBasicLoginWithRSSAndDisplay())
        {
Encoding = Encoding.UTF8;
            Language = "pl-pl";
            Type = "private";

            AddCategoryMapping(1, TorznabCatType.PC0day, "0-Day");
            AddCategoryMapping(3, TorznabCatType.PC0day, "Apps");
            AddCategoryMapping(4, TorznabCatType.Console, "Consoles");
            AddCategoryMapping(5, TorznabCatType.Books, "E-book");
            AddCategoryMapping(6, TorznabCatType.MoviesHD, "Movies HD");
            AddCategoryMapping(7, TorznabCatType.MoviesSD, "Movies SD");
            AddCategoryMapping(8, TorznabCatType.Audio, "Music");
            AddCategoryMapping(9, TorznabCatType.MoviesUHD, "Movies UHD");
            AddCategoryMapping(10, TorznabCatType.PCGames, "PcGames");
            AddCategoryMapping(11, TorznabCatType.TVHD, "TV HD");
            AddCategoryMapping(12, TorznabCatType.TVSD, "TV SD");
            AddCategoryMapping(13, TorznabCatType.XXX, "XXX");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string>
            {
                { "email", configData.Username.Value },
                { "pass", configData.Password.Value }
            };
            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, SiteLink);

            await ConfigureIfOK(result.Cookies, result.Cookies != null && result.Cookies.Contains("id="), () =>
            {
                var errorMessage = result.Content;
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var searchUrl = TorrentApiUrl;
            var searchString = query.GetQueryString();
            var queryCollection = new List<KeyValuePair<string, string>>();

            queryCollection.Add("tpage", "1");
            foreach (var cat in MapTorznabCapsToTrackers(query))
            {
                queryCollection.Add("cat[]", cat);
            }

            if (!string.IsNullOrWhiteSpace(searchString))
                queryCollection.Add("search", searchString);

            searchUrl += "?" + queryCollection.GetQueryString();

            var result = await RequestStringWithCookiesAndRetry(searchUrl, null, TorrentApiUrl);
            if (result.IsRedirect)
            {
                // re-login
                await ApplyConfiguration(null);
                result = await RequestStringWithCookiesAndRetry(searchUrl, null, TorrentApiUrl);
            }

            if (!result.Content.StartsWith("{")) // not JSON => error
                throw new ExceptionWithConfigData(result.Content, configData);
            dynamic json = JsonConvert.DeserializeObject<dynamic>(result.Content);
            try
            {
                dynamic torrents = json["torrents"]; // latest torrents

                if (json["hits"] != null) // is search result
                    torrents = json.SelectTokens("$.hits[?(@._type == 'torrent')]._source");
                /*
                {
                    "id":426868,
                    "name":"Realease-nameE",
                    "size":"2885494332",
                    "category":11,
                    "added":"2017-09-11T11:36:26.936Z",
                    "comments":0,
                    "leechers":0,
                    "seeders":1,
                    "completed":0,
                    "poster":true,
                    "imdb_id":"3743822",
                    "cdu_id":null,
                    "steam_id":null,
                    "subs":null,
                    "language":"en"
                },
                */

                foreach (var torrent in torrents)
                {
                    var release = new ReleaseInfo();
                    var descriptions = new List<string>();
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 0;

                    release.Category = MapTrackerCatToNewznab(torrent.category.ToString());
                    release.Title = torrent.name.ToString();
                    var torrentID = (long)torrent.id;
                    release.Comments = new Uri(SiteLink + "torrents/" + torrentID);
                    release.Guid = release.Comments;
                    release.Link = new Uri(SiteLink + "download/" + torrentID);
                    var date = (DateTime)torrent.added;
                    release.PublishDate = date;
                    release.Size = ParseUtil.CoerceLong(torrent.size.ToString());
                    release.Seeders = (int)torrent.seeders;
                    release.Peers = release.Seeders + (int)torrent.leechers;
                    var imdbid = torrent.imdb_id.ToString();
                    if (!string.IsNullOrEmpty(imdbid))
                        release.Imdb = ParseUtil.CoerceLong(imdbid);

                    if ((bool)torrent.poster == true)
                    {
                        if (release.Imdb != null)
                            release.BannerUrl = new Uri(CDNUrl + "images/torrents/poster/imd/l/" + imdbid + ".jpg");
                        else if (torrent["cdu_id"] != null)
                            release.BannerUrl = new Uri(CDNUrl + "images/torrents/poster/cdu/b/" + torrent["cdu_id"] + "_front.jpg");
                        else if (torrent["steam_id"] != null)
                            release.BannerUrl = new Uri(CDNUrl + "images/torrents/poster/ste/l/" + torrent["steam_id"] + ".jpg");
                    }

                    release.UploadVolumeFactor = 1;
                    release.DownloadVolumeFactor = 1;

                    release.Grabs = (long)torrent.completed;

                    var language = (string)torrent.language;
                    if (!string.IsNullOrEmpty(language))
                        descriptions.Add("Language: " + language);
                    else if ((bool?)torrent.polish == true)
                        descriptions.Add("Language: pl");

                    if (descriptions.Count > 0)
                        release.Description = string.Join("<br />\n", descriptions);

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(result.ToString(), ex);
            }

            return releases;
        }
    }
}
