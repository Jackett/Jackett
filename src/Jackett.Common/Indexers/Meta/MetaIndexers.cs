using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;

namespace Jackett.Common.Indexers.Meta
{
    public class AggregateIndexer : BaseMetaIndexer
    {
        public AggregateIndexer(IFallbackStrategyProvider fallbackStrategyProvider,
                                IResultFilterProvider resultFilterProvider, IIndexerConfigurationService configService,
                                WebClient client, Logger logger, IProtectionService ps, ICacheService cs)
            : base(id: "all",
                   name: "AggregateSearch",
                   description: "This feed includes all configured trackers",
                   configService: configService,
                   client: client,
                   logger: logger,
                   ps: ps,
                   cs: cs,
                   configData: new ConfigurationData(),
                   fallbackStrategyProvider: fallbackStrategyProvider,
                   resultFilterProvider: resultFilterProvider,
                   filter: x => true)
        {
        }

        public override TorznabCapabilities TorznabCaps
        {
            get
            {
                // increase the limits (workaround until proper paging is supported, issue #1661)
                var caps = base.TorznabCaps;
                caps.LimitsMax = caps.LimitsDefault = 1000;
                return caps;
            }
        }
    }
}
