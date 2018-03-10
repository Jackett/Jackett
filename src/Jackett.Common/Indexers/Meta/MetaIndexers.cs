using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;

namespace Jackett.Common.Indexers.Meta
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
        public AggregateIndexer(IFallbackStrategyProvider fallbackStrategyProvider, IResultFilterProvider resultFilterProvider, IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base("AggregateSearch", "This feed includes all configured trackers", fallbackStrategyProvider, resultFilterProvider, configService, wc, l, new ConfigurationData(), ps, x => true)
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