using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jackett.Models;
using Jackett.Models.IndexerConfig;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Indexers
{
    public abstract class BaseFeedIndexer : BaseWebIndexer
    {
        protected abstract Uri FeedUri { get; }

        protected BaseFeedIndexer(string name, string link, string description, IIndexerConfigurationService configService, IWebClient client, Logger logger, ConfigurationData configData, IProtectionService p, TorznabCapabilities caps = null, string downloadBase = null) : base(name, link, description, configService, client, logger, configData, p, caps, downloadBase)
        {
        }

        public override Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            IsConfigured = true;
            return Task.FromResult(IndexerConfigurationStatus.RequiresTesting);
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var requestUri = FeedUri.ToString();
            if (!query.SanitizedSearchTerm.IsNullOrEmptyOrWhitespace())
                requestUri = requestUri + "?q=" + query.SanitizedSearchTerm;
            var request = new WebRequest
            {
                Url = requestUri,
                Type = RequestType.GET
            };
            var result = await webclient.GetString(request);

            var results = ParseFeedForResults(result.Content);

            return results;
        }

        protected abstract IEnumerable<ReleaseInfo> ParseFeedForResults(string feedContent);
    }
}
