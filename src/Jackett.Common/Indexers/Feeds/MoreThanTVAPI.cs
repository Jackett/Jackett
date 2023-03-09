using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
        public override string Id => "morethantv-api";
        public override string Name => "MoreThanTV (API)";
        public override string Description => "Private torrent tracker for TV / MOVIES";
        public override string SiteLink { get; protected set; } = "https://www.morethantv.me/";
        public override string Language => "en-US";
        public override string Type => "private";

        private new ConfigurationDataAPIKey configData => (ConfigurationDataAPIKey)base.configData;

        public MoreThanTVAPI(IIndexerConfigurationService configService, WebClient client, Logger logger,
            IProtectionService ps, ICacheService cs)
            : base(
                   caps: new TorznabCapabilities
                   {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId, TvSearchParam.TvdbId
                       },
                       MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q, MovieSearchParam.ImdbId
                       }
                   },
                   configService: configService,
                   client: client,
                   logger: logger,
                   p: ps,
                   cs: cs,
                   configData: new ConfigurationDataAPIKey())
        {
            AddCategoryMapping(TorznabCatType.TVSD.ID, TorznabCatType.TVSD);
            AddCategoryMapping(TorznabCatType.TVHD.ID, TorznabCatType.TVHD);
            AddCategoryMapping(TorznabCatType.TVUHD.ID, TorznabCatType.TVUHD);
            AddCategoryMapping(TorznabCatType.TVSport.ID, TorznabCatType.TVSport);
            AddCategoryMapping(TorznabCatType.MoviesSD.ID, TorznabCatType.MoviesSD);
            AddCategoryMapping(TorznabCatType.MoviesHD.ID, TorznabCatType.MoviesHD);
            AddCategoryMapping(TorznabCatType.MoviesUHD.ID, TorznabCatType.MoviesUHD);
            AddCategoryMapping(TorznabCatType.MoviesBluRay.ID, TorznabCatType.MoviesBluRay);

            configData.AddDynamic("keyInfo", new DisplayInfoConfigurationItem(String.Empty, "Find or Generate a new API Key by accessing your <a href=\"https://www.morethantv.me/user/security\" target =_blank>MoreThanTV</a> account <i>User Security</i> page and scrolling to the <b>API Keys</b> section."));
        }

        public override Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            if (configData.Key.Value.Length != 32)
                throw new Exception("Invalid API Key configured. Expected length: 32");

            IsConfigured = true;
            SaveConfig();

            return Task.FromResult(IndexerConfigurationStatus.RequiresTesting);
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var requestUri = FeedUri.ToString();
            var qc = new NameValueCollection
            {
                {"apikey", configData.Key.Value},
                {"limit", "100"},
                {"extended", "1"},
            };
            if (query.IsTVSearch)
                qc.Add("t", "tvsearch");
            else if (query.IsMovieSearch)
                qc.Add("t", "movie");
            else
                qc.Add("t", "search");
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
            if (!result.ContentString.StartsWith("<")) // not XML => error
                throw new ExceptionWithConfigData(result.ContentString, configData);
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
