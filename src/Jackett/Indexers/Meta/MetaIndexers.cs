using System.Collections.Generic;
using Jackett.Services;
using Jackett.Utils.Clients;
using NLog;
using Jackett.Models.IndexerConfig;

namespace Jackett.Indexers.Meta
{
    public class AggregateIndexer : BaseMetaIndexer
    {
        public override string ID
        {
            get
            {
                return "all";
            }
        }
        public AggregateIndexer(IFallbackStrategyProvider fallbackStrategyProvider, IResultFilterProvider resultFilterProvider, IIndexerConfigurationService configService, IWebClient wc, Logger l, IProtectionService ps)
            : base("AggregateSearch", "This feed includes all configured trackers", fallbackStrategyProvider, resultFilterProvider, configService, wc, l, new ConfigurationData(), ps, MetaIndexerOptimization.None)
        {
        }
    }

    public struct IndexerCollectionSettings
    {
        public string Id;
        public IEnumerable<string> Indexers;
    }

    public class IndexerCollectionMetaIndexer : BaseMetaIndexer
    {
        public override string ID
        {
            get
            {
                return GroupId;
            }
        }

        public IndexerCollectionMetaIndexer(string groupId, IEnumerable<IIndexer> indexers, IFallbackStrategyProvider fallbackStrategyProvider, IResultFilterProvider resultFilterProvider, IIndexerConfigurationService configService, IWebClient wc, Logger l, IProtectionService ps)
            : base("IndexerCollection " + groupId, "This feed includes some other configured trackers", fallbackStrategyProvider, resultFilterProvider, configService, wc, l, new ConfigurationData(), ps, MetaIndexerOptimization.None)
        {
            GroupId = groupId;
            Indexers = indexers;
        }

        private string GroupId;
    }
}