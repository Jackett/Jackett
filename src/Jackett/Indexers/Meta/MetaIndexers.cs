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
        public AggregateIndexer(IIndexerManagerService i, IWebClient wc, Logger l, IProtectionService ps)
            : base("AggregateSearch", "This feed includes all configured trackers", i, wc, l, new ConfigurationData(), ps, x => true)
        {
        }
    }
}