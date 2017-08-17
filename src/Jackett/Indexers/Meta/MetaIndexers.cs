using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jackett.Models;
using Newtonsoft.Json.Linq;
using Jackett.Services;
using Jackett.Utils.Clients;
using NLog;
using Jackett.Models.IndexerConfig;

namespace Jackett.Indexers.Meta
{
    class AggregateIndexer : BaseMetaIndexer
    {
        public override string ID
        {
            get
            {
                return "all";
            }
        }
        public AggregateIndexer(IFallbackStrategyProvider fallbackStrategyProvider, IResultFilterProvider resultFilterProvider, IIndexerConfigurationService configService, IWebClient wc, Logger l, IProtectionService ps)
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