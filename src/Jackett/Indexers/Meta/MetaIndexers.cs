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

namespace Jackett.Indexers.Meta
{
    class AggregateIndexer : BaseMetaIndexer, IIndexer
    {
        public AggregateIndexer(IIndexerManagerService i, Logger l, IProtectionService ps)
            : base("AggregateSearch", "This feed includes all configured trackers", i, l, new Models.IndexerConfig.ConfigurationData(), ps, x => true)
        {
        }
    }
}