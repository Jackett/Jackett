using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    [ExcludeFromCodeCoverage]
    public class PolishTracker : BaseWebIndexer
    {
        private string SearchUrl => SiteLink + "apitorrents";
        private static string CdnUrl => "https://cdn.pte.nu/";

        public override string[] LegacySiteLinks { get; protected set; } = {
            "https://polishtracker.net/"
        };

        private new ConfigurationDataCookie configData => (ConfigurationDataCookie)base.configData;

        public PolishTracker(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(id: "polishtracker",
                   name: "PolishTracker",
                   description: "Polish Tracker is a POLISH Private site for 0DAY / MOVIES / GENERAL",
                   link: "https://pte.nu/",
                   caps: new TorznabCapabilities
                   {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId
                       },
                       MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q, MovieSearchParam.ImdbId
                       },
                       MusicSearchParams = new List<MusicSearchParam>
                       {
                           MusicSearchParam.Q
                       },
                       BookSearchParams = new List<BookSearchParam>
                       {
                           BookSearchParam.Q
                       }
                   },
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   configData: new ConfigurationDataCookie())
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

            CookieHeader = configData.Cookie.Value;
            try
            {
                var results = await PerformQuery(new TorznabQuery());
                if (!results.Any())
                    throw new Exception("Found 0 results in the tracker");

                IsConfigured = true;
                SaveConfig();
                return IndexerConfigurationStatus.Completed;
            }
            catch (Exception)
            {
                IsConfigured = false;
                throw;
            }
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var qc = new List<KeyValuePair<string, string>> // NameValueCollection don't support cat[]=19&cat[]=6
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
            var result = await RequestWithCookiesAndRetryAsync(searchUrl, referer: SearchUrl);
            if (result.IsRedirect)
                throw new Exception($"Your cookie did not work. Please, configure the tracker again. Message: {result.ContentString}");

            if (!result.ContentString.StartsWith("{")) // not JSON => error
                throw new ExceptionWithConfigData(result.ContentString, configData);

            var json = JsonConvert.DeserializeObject<dynamic>(result.ContentString);
            try
            {
                var torrents = json["torrents"]; // latest torrents
                if (json["hits"] != null) // is search result
                    torrents = json.SelectTokens("$.hits[?(@._type == 'torrent')]._source");
                foreach (var torrent in torrents)
                {
                    var torrentId = (long)torrent.id;
                    var details = new Uri(SiteLink + "torrents/" + torrentId);
                    var link = new Uri(SiteLink + "download/" + torrentId);
                    var publishDate = DateTime.Parse(torrent.added.ToString());
                    var imdbId = ParseUtil.GetImdbID(torrent.imdb_id.ToString());

                    Uri poster = null;
                    if ((bool)torrent.poster)
                    {
                        if (torrent["imdb_id"] != null)
                            poster = new Uri(CdnUrl + "images/torrents/poster/imd/l/" + torrent["imdb_id"] + ".jpg");
                        else if (torrent["cdu_id"] != null)
                            poster = new Uri(CdnUrl + "images/torrents/poster/cdu/b/" + torrent["cdu_id"] + "_front.jpg");
                        else if (torrent["steam_id"] != null)
                            poster = new Uri(CdnUrl + "images/torrents/poster/ste/l/" + torrent["steam_id"] + ".jpg");
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
                        Details = details,
                        Guid = details,
                        Link = link,
                        PublishDate = publishDate,
                        Category = MapTrackerCatToNewznab(torrent.category.ToString()),
                        Size = (long)torrent.size,
                        Grabs = (long)torrent.completed,
                        Seeders = (int)torrent.seeders,
                        Peers = (int)torrent.seeders + (int)torrent.leechers,
                        Imdb = imdbId,
                        Poster = poster,
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
