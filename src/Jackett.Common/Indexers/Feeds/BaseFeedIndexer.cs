using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers.Feeds
{
    public abstract class BaseFeedIndexer : BaseWebIndexer
    {
        protected abstract Uri FeedUri { get; }

        protected BaseFeedIndexer(string name, string link, string description, IIndexerConfigurationService configService, WebClient client, Logger logger, ConfigurationData configData, IProtectionService p, TorznabCapabilities caps = null, string downloadBase = null) : base(name, link, description, configService, client, logger, configData, p, caps, downloadBase)
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
            if (!query.SearchTerm.IsNullOrEmptyOrWhitespace())
                requestUri = requestUri + "?q=" + query.SearchTerm;
            var request = new WebRequest
            {
                Url = requestUri,
                Type = RequestType.GET,
                Encoding = Encoding
            };
            var result = await webclient.GetString(request);

            var results = ParseFeedForResults(result.Content);

            return results;
        }

        protected abstract IEnumerable<ReleaseInfo> ParseFeedForResults(string feedContent);
    }
}
