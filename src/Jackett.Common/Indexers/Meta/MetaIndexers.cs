using System;
using System.Linq;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Cache;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;

using NLog;

namespace Jackett.Common.Indexers.Meta
{
    public class AggregateIndexer : BaseMetaIndexer
    {
        public override string Id => "all";
        public override string Name => "AggregateSearch";
        public override string Description => "This feed includes all configured trackers";

        public AggregateIndexer(IFallbackStrategyProvider fallbackStrategyProvider,
                                IResultFilterProvider resultFilterProvider, IIndexerConfigurationService configService,
                                WebClient client, Logger logger, IProtectionService ps, CacheManager cm)
            : base(configService: configService,
                   client: client,
                   logger: logger,
                   ps: ps,
                   cm: cm,
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

    public class FilterIndexer : BaseMetaIndexer
    {
        public override string Id => _filter;
        public override string Name => _filter;
        public override string Description => $"This feed includes all configured trackers filter by {_filter}";

        private readonly string _filter;

        public FilterIndexer(string filter, IFallbackStrategyProvider fallbackStrategyProvider,
                          IResultFilterProvider resultFilterProvider, IIndexerConfigurationService configService,
                          WebClient client, Logger logger, IProtectionService ps, CacheManager cm, Func<IIndexer, bool> filterFunc)
            : base(configService: configService,
                   client: client,
                   logger: logger,
                   ps: ps,
                   cm: cm,
                   configData: new ConfigurationData(),
                   fallbackStrategyProvider: fallbackStrategyProvider,
                   resultFilterProvider: resultFilterProvider,
                   filter: filterFunc
                )
        {
            _filter = filter;
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

        public override bool IsConfigured => base.IsConfigured && (ValidIndexers?.Any() ?? false);

        public override void SaveConfig() { }
    }
}
