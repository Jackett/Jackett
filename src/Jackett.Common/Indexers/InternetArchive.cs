using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
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
    [ExcludeFromCodeCoverage]
    public class InternetArchive : BaseWebIndexer
    {
        private string SearchUrl => SiteLink + "advancedsearch.php";
        private string DetailsUrl => SiteLink + "details/";
        private string LinkUrl => SiteLink + "download/";

        private string _sort;
        private string _order;
        private bool _titleOnly;

        private ConfigurationData ConfigData => configData;

        public InternetArchive(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(id: "internetarchive",
                   name: "Internet Archive",
                   description: "Internet Archive is a non-profit digital library offering free universal access to books, movies & music, as well as 406 billion archived web pages",
                   link: "https://archive.org/",
                   caps: new TorznabCapabilities
                   {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q
                       },
                       MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q
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
                {"asc", "asc"}
            })
            { Name = "Order requested from site", Value = "desc" };
            configData.AddDynamic("order", order);

            var titleOnly = new BoolItem { Name = "Search only in title", Value = true };
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
            var result = await RequestWithCookiesAndRetryAsync(fullSearchUrl);
            foreach (var torrent in ParseResponse(result))
                releases.Add(MakeRelease(torrent));

            return releases;
        }

        private JArray ParseResponse(WebResult result)
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
            var id = GetFieldAs<string>("identifier", torrent);
            var title = GetFieldAs<string>("title", torrent) ?? id;
            var details = new Uri(DetailsUrl + id);
            var btih = GetFieldAs<string>("btih", torrent);
            var link = new Uri(LinkUrl + id + "/" + id + "_archive.torrent");

            var release = new ReleaseInfo
            {
                Title = title,
                Details = details,
                Guid = details,
                PublishDate = GetFieldAs<DateTime>("publicdate", torrent),
                Category = MapTrackerCatToNewznab(GetFieldAs<string>("mediatype", torrent)),
                Size = GetFieldAs<long>("item_size", torrent),
                Seeders = 1,
                Peers = 2,
                Grabs = GetFieldAs<long>("downloads", torrent),
                Link = link,
                InfoHash = btih, // magnet link is auto generated from infohash
                DownloadVolumeFactor = 0,
                UploadVolumeFactor = 1
            };

            return release;
        }

        private static T GetFieldAs<T>(string field, JToken torrent) =>
            torrent[field] is JArray array ? array.First.ToObject<T>() : torrent.Value<T>(field);
    }
}
