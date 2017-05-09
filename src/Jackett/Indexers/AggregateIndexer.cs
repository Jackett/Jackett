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

        public override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var tasks = new List<Task<IEnumerable<ReleaseInfo>>>();
            foreach (var indexer in Indexers.Where(i => i.CanHandleQuery(query)))
                tasks.Add(indexer.PerformQuery(query));

            var t = Task.WhenAll<IEnumerable<ReleaseInfo>>(tasks);
            try
            {
                t.Wait();
            }
            catch (AggregateException exception)
            {
                logger.Error(exception, "Error during request from Aggregate");
            }

            IEnumerable<ReleaseInfo> result = tasks.Where(x => x.Status == TaskStatus.RanToCompletion).SelectMany(x => x.Result).OrderByDescending(r => r.PublishDate);
            // Limiting the response size might be interesting for use-cases where there are
            // tons of trackers configured in Jackett. For now just use the limit param if
            // someone wants to do that.
            if (query.Limit > 0)
                result = result.Take(query.Limit);
            return result;
        }
    }
}