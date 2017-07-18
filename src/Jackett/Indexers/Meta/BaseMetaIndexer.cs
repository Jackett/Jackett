using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jackett.Models;
using Jackett.Models.IndexerConfig;
using Jackett.Services;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Indexers.Meta
{
    public abstract class BaseMetaIndexer : BaseWebIndexer
    {
        protected BaseMetaIndexer(string name, string description, IFallbackStrategyProvider fallbackStrategyProvider, IResultFilterProvider resultFilterProvider, IIndexerConfigurationService configService, IWebClient webClient, Logger logger, ConfigurationData configData, IProtectionService p, Func<IIndexer, bool> filter)
            : base(name, "http://127.0.0.1/", description, configService, webClient, logger, configData, p, null, null)
        {
            filterFunc = filter;
            this.fallbackStrategyProvider = fallbackStrategyProvider;
            this.resultFilterProvider = resultFilterProvider;
        }

        public override Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            return Task.FromResult(IndexerConfigurationStatus.Completed);
        }

        public override async Task<IEnumerable<ReleaseInfo>> ResultsForQuery(TorznabQuery query)
        {
            return await PerformQuery(query);
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var indexers = validIndexers;
            IEnumerable<Task<IEnumerable<ReleaseInfo>>> supportedTasks = indexers.Where(i => i.CanHandleQuery(query)).Select(i => i.ResultsForQuery(query)).ToList(); // explicit conversion to List to execute LINQ query

            var fallbackStrategies = fallbackStrategyProvider.FallbackStrategiesForQuery(query);
            var fallbackQueries = fallbackStrategies.Select(async f => await f.FallbackQueries()).SelectMany(t => t.Result);
            var fallbackTasks = fallbackQueries.SelectMany(q => indexers.Where(i => !i.CanHandleQuery(query) && i.CanHandleQuery(q)).Select(i => i.ResultsForQuery(q.Clone())));
            var tasks = supportedTasks.Concat(fallbackTasks.ToList()); // explicit conversion to List to execute LINQ query
            var aggregateTask = Task.WhenAll(tasks);

            try
            {
                await aggregateTask;
            }
            catch
            {
                logger.Error(aggregateTask.Exception, "Error during request in metaindexer " + ID);
            }

            var unorderedResult = tasks.Where(x => x.Status == TaskStatus.RanToCompletion).SelectMany(x => x.Result);
            var resultFilters = resultFilterProvider.FiltersForQuery(query);
            var filteredResults = resultFilters.Select(async f => await f.FilterResults(unorderedResult)).SelectMany(t => t.Result);
            var uniqueFilteredResults = filteredResults.Distinct();
            var orderedResults = uniqueFilteredResults.OrderByDescending(r => r.Gain);
            // Limiting the response size might be interesting for use-cases where there are
            // tons of trackers configured in Jackett. For now just use the limit param if
            // someone wants to do that.
            IEnumerable<ReleaseInfo> result = orderedResults;
            if (query.Limit > 0)
                result = result.Take(query.Limit);
            return result;
        }

        public override TorznabCapabilities TorznabCaps
        {
            get
            {
                return validIndexers.Select(i => i.TorznabCaps).Aggregate(new TorznabCapabilities(), TorznabCapabilities.Concat);
            }
        }

        public override bool IsConfigured
        {
            get
            {
                return Indexers != null;
            }
        }

        private IEnumerable<IIndexer> validIndexers
        {
            get
            {
                if (Indexers == null)
                    return null;

                return Indexers.Where(i => i.IsConfigured && filterFunc(i));
            }
        }

        public IEnumerable<IIndexer> Indexers;

        private Func<IIndexer, bool> filterFunc;
        private IFallbackStrategyProvider fallbackStrategyProvider;
        private IResultFilterProvider resultFilterProvider;
    }
}
