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
    class AggregateIndexer : BaseIndexer, IIndexer
    {
        private IEnumerable<IIndexer> Indexers;
        public AggregateIndexer(IIndexerManagerService i, IWebClient wc, Logger l, IProtectionService ps)
            : base("AggregateSearch", "http://127.0.0.1/", "This feed includes all configured trackers", i, wc, l, new Models.IndexerConfig.ConfigurationData(), ps)
        {
        }

        public void SetIndexers(IEnumerable<IIndexer> indexers)
        {
            Indexers = indexers;
            base.IsConfigured = true;
        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            return IndexerConfigurationStatus.Completed;
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var tasks = new List<Task<IEnumerable<ReleaseInfo>>>();
            foreach (var indexer in Indexers.Where(i => i.IsConfigured))
                tasks.Add(indexer.PerformQuery(query));

            var t = Task.WhenAll<IEnumerable<ReleaseInfo>>(tasks);
            t.Wait();

            IEnumerable<ReleaseInfo> result = t.Result.SelectMany(x => x).OrderByDescending(r => r.PublishDate);
            // Limiting the response size might be interesting for use-cases where there are
            // tons of trackers configured in Jackett. For now just use the limit param if
            // someone wants to do that.
            if (query.Limit > 0)
                result = result.Take(query.Limit);
            return result;
        }
    }
}