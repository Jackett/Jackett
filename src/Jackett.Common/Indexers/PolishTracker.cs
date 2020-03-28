using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
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
        private string LoginUrl => SiteLink + "login";
        private string SearchUrl => SiteLink + "apitorrents";
        private static string CdnUrl => "https://cdn.pte.nu/";

        public override string[] LegacySiteLinks { get; protected set; } = {
            "https://polishtracker.net/",
        };

        private new ConfigurationDataBasicLoginWithEmail configData => (ConfigurationDataBasicLoginWithEmail)base.configData;

        public PolishTracker(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base("PolishTracker",
                   description: "Polish Tracker is a POLISH Private site for 0DAY / MOVIES / GENERAL",
                   link: "https://pte.nu/",
                   caps: new TorznabCapabilities(),
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   configData: new ConfigurationDataBasicLoginWithEmail())
        {
            Encoding = Encoding.UTF8;
            Language = "pl-pl";
            Type = "private";

            TorznabCaps.SupportsImdbMovieSearch = true;
            TorznabCaps.SupportsImdbTVSearch = true;

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
                { "email", configData.Email.Value },
                { "pass", configData.Password.Value }
            };
            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, SiteLink);

            await ConfigureIfOK(result.Cookies, result.Cookies?.Contains("id=") == true, () =>
            {
                var errorMessage = result.Content;
                if (errorMessage.Contains("Error!"))
                    errorMessage = "E-mail or password is incorrect";
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var qc = new NameValueCollection
            {
                { "tpage", "1" }
            };

            if (query.IsImdbQuery)
            {
                qc.Add("search", query.ImdbID);
                qc.Add("nfo", "true");
            }
            else if (!string.IsNullOrWhiteSpace(query.GetQueryString()))
                qc.Add("search", query.GetQueryString());

            foreach (var cat in MapTorznabCapsToTrackers(query))
                qc.Add("cat[]", cat);

            var searchUrl = SearchUrl + "?" + qc.GetQueryString();
            var result = await RequestStringWithCookiesAndRetry(searchUrl, null, SearchUrl);
            if (result.IsRedirect)
            {
                // re-login
                await ApplyConfiguration(null);
                result = await RequestStringWithCookiesAndRetry(searchUrl, null, SearchUrl);
            }

            if (!result.Content.StartsWith("{")) // not JSON => error
                throw new ExceptionWithConfigData(result.Content, configData);

            var json = JsonConvert.DeserializeObject<dynamic>(result.Content);
            try
            {
                var torrents = json["torrents"]; // latest torrents
                if (json["hits"] != null) // is search result
                    torrents = json.SelectTokens("$.hits[?(@._type == 'torrent')]._source");
                foreach (var torrent in torrents)
                {
                    var torrentId = (long)torrent.id;
                    var comments = new Uri(SiteLink + "torrents/" + torrentId);
                    var link = new Uri(SiteLink + "download/" + torrentId);
                    var publishDate = DateTime.Parse(torrent.added.ToString());
                    var imdbId = ParseUtil.GetImdbID(torrent.imdb_id.ToString());

                    Uri banner = null;
                    if ((bool)torrent.poster)
                    {
                        if (torrent["imdb_id"] != null)
                            banner = new Uri(CdnUrl + "images/torrents/poster/imd/l/" + torrent["imdb_id"] + ".jpg");
                        else if (torrent["cdu_id"] != null)
                            banner = new Uri(CdnUrl + "images/torrents/poster/cdu/b/" + torrent["cdu_id"] + "_front.jpg");
                        else if (torrent["steam_id"] != null)
                            banner = new Uri(CdnUrl + "images/torrents/poster/ste/l/" + torrent["steam_id"] + ".jpg");
                    }

                    var descriptions = new List<string>();
                    var language = (string)torrent.language;
                    if (!string.IsNullOrEmpty(language))
                        descriptions.Add("Language: " + language);
                    else if ((bool?)torrent.polish == true)
                        descriptions.Add("Language: pl");
                    var description = descriptions.Any() ? string.Join("<br />\n", descriptions) : null;

                    var release = new ReleaseInfo
                    {
                        Title = torrent.name.ToString(),
                        Comments = comments,
                        Guid = comments,
                        Link = link,
                        PublishDate = publishDate,
                        Category = MapTrackerCatToNewznab(torrent.category.ToString()),
                        Size = (long)torrent.size,
                        Grabs = (long)torrent.completed,
                        Seeders = (int)torrent.seeders,
                        Peers = (int)torrent.seeders + (int)torrent.leechers,
                        Imdb = imdbId,
                        BannerUrl = banner,
                        Description = description,
                        MinimumRatio = 1,
                        MinimumSeedTime = 259200, // 72 hours (I can't verify this, but this is a safe value in most trackers)
                        UploadVolumeFactor = 1,
                        DownloadVolumeFactor = 1
                    };

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
