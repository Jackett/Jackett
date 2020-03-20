using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
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
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers
{
    // ReSharper disable once UnusedType.Global
    public class InternetArchive : BaseWebIndexer
    {
        private string SearchUrl => SiteLink + "advancedsearch.php";
        private string CommentsUrl => SiteLink + "details/";
        private string LinkUrl => SiteLink + "download/";

        private readonly NameValueCollection _trackers = new NameValueCollection
        {
            {"tr", "udp://tracker.coppersurfer.tk:6969/announce"},
            {"tr", "udp://tracker.leechers-paradise.org:6969/announce"},
            {"tr", "udp://tracker.opentrackr.org:1337/announce"},
            {"tr", "udp://tracker.internetwarriors.net:1337/announce"},
            {"tr", "udp://open.demonii.si:1337/announce"}
        };

        private string _sort;
        private string _order;
        private bool _titleOnly;

        private ConfigurationData ConfigData => configData;

        public InternetArchive(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(name: "Internet Archive",
                   description: "Internet Archive is a non-profit digital library offering free universal access to books, movies & music, as well as 406 billion archived web pages",
                   link: "https://archive.org/",
                   caps: new TorznabCapabilities(),
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   configData: new ConfigurationData())
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "public";

            var sort = new SelectItem(new Dictionary<string, string>
            {
                {"publicdate", "created"},
                {"downloads", "downloads"},
                {"item_size", "size"}
            })
            { Name = "Sort requested from site", Value = "publicdate" };
            configData.AddDynamic("sort", sort);

            var order = new SelectItem(new Dictionary<string, string>
            {
                {"desc", "desc"},
                {"asc", "asc"},
            })
            { Name = "Order requested from site", Value = "desc" };
            configData.AddDynamic("order", order);

            var titleOnly = new BoolItem() { Name = "Search only in title", Value = true };
            configData.AddDynamic("titleOnly", titleOnly);

            AddCategoryMapping("audio", TorznabCatType.Audio);
            AddCategoryMapping("etree", TorznabCatType.Audio);
            AddCategoryMapping("movies", TorznabCatType.Movies);
            AddCategoryMapping("image", TorznabCatType.OtherMisc);
            AddCategoryMapping("texts", TorznabCatType.Books);
            AddCategoryMapping("software", TorznabCatType.PC);
            AddCategoryMapping("web", TorznabCatType.Other);
            AddCategoryMapping("collection", TorznabCatType.Other);
            AddCategoryMapping("account", TorznabCatType.Other);
            AddCategoryMapping("data", TorznabCatType.Other);
            AddCategoryMapping("other", TorznabCatType.Other);
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var releases = await PerformQuery(new TorznabQuery());

            await ConfigureIfOK(string.Empty, releases.Any(), () =>
                throw new Exception("Could not find release from this URL."));

            return IndexerConfigurationStatus.Completed;
        }

        public override void LoadValuesFromJson(JToken jsonConfig, bool useProtectionService = false)
        {
            base.LoadValuesFromJson(jsonConfig, useProtectionService);

            var sort = (SelectItem)configData.GetDynamic("sort");
            _sort = sort != null ? sort.Value : "publicdate";

            var order = (SelectItem)configData.GetDynamic("order");
            _order = order != null && order.Value.Equals("asc") ? "" : "-";

            var titleOnly = (BoolItem)configData.GetDynamic("titleOnly");
            _titleOnly = titleOnly != null && titleOnly.Value;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var searchTerm = "format:(\"Archive BitTorrent\")";
            var sort = "-publicdate";
            if (!string.IsNullOrEmpty(query.SearchTerm))
            {
                if (_titleOnly)
                    searchTerm = "title:(" + query.SearchTerm + ") AND " + searchTerm;
                else
                    searchTerm = query.SearchTerm + " AND " + searchTerm;
                sort = _order + _sort;
            }

            var querycats = MapTorznabCapsToTrackers(query);
            if (querycats.Any())
                searchTerm += " AND mediatype:(" + string.Join(" OR ", querycats) + ")";

            var qc = new NameValueCollection
            {
                {"q", searchTerm},
                {"fl[]", "identifier,title,mediatype,item_size,downloads,btih,publicdate"},
                {"sort", sort},
                {"rows", "100"},
                {"output", "json"}
            };
            var fullSearchUrl = SearchUrl + "?" + qc.GetQueryString();
            var result = await RequestStringWithCookiesAndRetry(fullSearchUrl);
            foreach (var torrent in ParseResponse(result))
                releases.Add(MakeRelease(torrent));

            return releases;
        }

        private JArray ParseResponse(BaseWebResult result)
        {
            try
            {
                if (result.Status != HttpStatusCode.OK)
                    throw new Exception("Response code error. HTTP code: " + result.Status);
                var json = JsonConvert.DeserializeObject<dynamic>(result.ContentString);
                if (!(json is JObject) || !(json["response"] is JObject) || !(json["response"]["docs"] is JArray))
                    throw new Exception("Response format error");
                return (JArray)json["response"]["docs"];
            }
            catch (Exception e)
            {
                logger.Error("ParseResponse Error: ", e.Message);
                throw new ExceptionWithConfigData(result.ContentString, ConfigData);
            }
        }

        private ReleaseInfo MakeRelease(JToken torrent)
        {
            var release = new ReleaseInfo();

            var title = GetFieldAs<string>("title", torrent);
            release.Title = title;

            var id = GetFieldAs<string>("identifier", torrent);
            release.Comments = new Uri(CommentsUrl + id);
            release.Guid = release.Comments;

            release.PublishDate = GetFieldAs<DateTime>("publicdate", torrent);
            release.Category = MapTrackerCatToNewznab(GetFieldAs<string>("mediatype", torrent));
            release.Size = GetFieldAs<long>("item_size", torrent);
            release.Seeders = 1;
            release.Peers = 2;
            release.Grabs = GetFieldAs<long>("downloads", torrent);

            var btih = GetFieldAs<string>("btih", torrent);
            release.Link = new Uri(LinkUrl + id + "/" + id + "_archive.torrent");
            release.MagnetUri = GenerateMagnetLink(btih, title);
            release.InfoHash = btih;

            release.MinimumRatio = 1;
            release.MinimumSeedTime = 172800; // 48 hours
            release.DownloadVolumeFactor = 0;
            release.UploadVolumeFactor = 1;

            return release;
        }

        private Uri GenerateMagnetLink(string btih, string title)
        {
            _trackers.Set("dn", title);
            return new Uri("magnet:?xt=urn:btih:" + btih + "&" + _trackers.GetQueryString());
        }

        private static T GetFieldAs<T>(string field, JToken torrent) =>
            torrent[field] is JArray array ? array.First.ToObject<T>() : torrent.Value<T>(field);
    }
}
