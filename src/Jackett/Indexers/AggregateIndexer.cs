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

namespace Jackett.Indexers
{
    class AggregateIndexer : BaseMetaIndexer, IIndexer
    {
        public AggregateIndexer(IIndexerManagerService i, IWebClient wc, Logger l, IProtectionService ps)
            : base("AggregateSearch", "http://127.0.0.1/", "This feed includes all configured trackers", i, wc, l, new Models.IndexerConfig.ConfigurationData(), ps)
        {
        }
    }
}