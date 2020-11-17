using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers.Feeds
{
    [ExcludeFromCodeCoverage]
    public abstract class BaseFeedIndexer : BaseWebIndexer
    {
        protected abstract Uri FeedUri { get; }

        protected BaseFeedIndexer(string link, string id, string name, string description,
                                  IIndexerConfigurationService configService, WebClient client, Logger logger,
                                  ConfigurationData configData, IProtectionService p, TorznabCapabilities caps = null,
                                  string downloadBase = null)
            : base(id: id,
                   name: name,
                   description: description,
                   link: link,
                   caps: caps,
                   configService: configService,
                   client: client,
                   logger: logger,
                   p: p,
                   configData: configData,
                   downloadBase: downloadBase)
        {
        }

        public override Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            IsConfigured = true;
            SaveConfig();

            return Task.FromResult(IndexerConfigurationStatus.RequiresTesting);
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var requestUri = FeedUri.ToString();
            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
                requestUri = requestUri + "?q=" + query.SearchTerm;
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

        protected abstract IEnumerable<ReleaseInfo> ParseFeedForResults(string feedContent);
    }
}
