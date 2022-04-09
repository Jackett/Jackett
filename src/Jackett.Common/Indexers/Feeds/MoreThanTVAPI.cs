using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;

namespace Jackett.Common.Indexers.Feeds
{
    [ExcludeFromCodeCoverage]
    public class MoreThanTVAPI : BaseNewznabIndexer
    {
        private new ConfigurationDataPasskey configData => (ConfigurationDataPasskey)base.configData;

        public MoreThanTVAPI(IIndexerConfigurationService configService, WebClient client, Logger logger,
            IProtectionService ps, ICacheService cs)
            : base(id: "morethantv-api",
                   name: "MoreThanTV (API)",
                   description: "Private torrent tracker for TV / MOVIES",
                   link: "https://www.morethantv.me/",
                   caps: new TorznabCapabilities
                   {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId, TvSearchParam.TvdbId
                       },
                       MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q, MovieSearchParam.ImdbId, MovieSearchParam.TmdbId
                       }
                   },
                   configService: configService,
                   client: client,
                   logger: logger,
                   p: ps,
                   cs: cs,
                   configData: new ConfigurationDataPasskey("You can create and find the API key under " +
                                                            "user security."))
        {
            Encoding = Encoding.UTF8;
            Language = "en-US";
            Type = "private";

            AddCategoryMapping(5030, TorznabCatType.TVSD);
            AddCategoryMapping(5040, TorznabCatType.TVHD);
            AddCategoryMapping(5045, TorznabCatType.TVUHD);
            AddCategoryMapping(5060, TorznabCatType.TVSport);
            AddCategoryMapping(2030, TorznabCatType.MoviesSD);
            AddCategoryMapping(2040, TorznabCatType.MoviesHD);
            AddCategoryMapping(2045, TorznabCatType.MoviesUHD);
            AddCategoryMapping(2050, TorznabCatType.MoviesBluRay);
        }

        public override Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            if (configData.Passkey.Value.Length != 32)
                throw new Exception("Invalid Passkey configured. Expected length: 32");

            IsConfigured = true;
            SaveConfig();

            return Task.FromResult(IndexerConfigurationStatus.RequiresTesting);
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var requestUri = FeedUri.ToString();
            var qc = new NameValueCollection
            {
                {"t", "search"},
                {"apikey", configData.Passkey.Value},
            };
            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
                qc.Add("q", query.SearchTerm);

            var queryCats = new List<int>();
            foreach (var queryCategory in query.Categories)
            {
                queryCats.Add(queryCategory);
            }
            if (queryCats.Any())
                qc.Add("cat", string.Join(",", queryCats));
            if (!string.IsNullOrWhiteSpace(query.ImdbID))
                qc.Add("imdbid", query.ImdbID);
            if (query.TvdbID != null)
                qc.Add("tvdbid", query.TvdbID.ToString());
            if (!string.IsNullOrWhiteSpace(query.Episode))
                qc.Add("ep", query.Episode);
            if (query.Season > 0)
                qc.Add("season", query.Season.ToString());
            requestUri = requestUri + "?" + qc.GetQueryString();
            var request = new WebRequest
            {
                Url = requestUri,
                Type = RequestType.GET,
                Encoding = Encoding
            };
            var result = await webclient.GetResultAsync(request);

            var results = ParseFeedForResults(result.ContentString);

            return results;
        }

        protected override ReleaseInfo ResultFromFeedItem(XElement item)
        {
            var release = base.ResultFromFeedItem(item);
            var enclosures = item.Descendants("enclosure").Where(e => e.Attribute("type").Value == "application/x-bittorrent");
            if (enclosures.Any())
            {
                var enclosure = enclosures.First().Attribute("url").Value;
                release.Link = new Uri(enclosure);
            }
            // add some default values if none returned by feed
            release.Seeders = release.Seeders > 0 ? release.Seeders : 0;
            release.Peers = release.Peers > 0 ? release.Peers : 0;
            release.DownloadVolumeFactor = release.DownloadVolumeFactor > 0 ? release.DownloadVolumeFactor : 0;
            release.UploadVolumeFactor = release.UploadVolumeFactor > 0 ? release.UploadVolumeFactor : 1;
            return release;
        }

        protected override Uri FeedUri => new Uri(SiteLink + "api/torznab");
    }
}
