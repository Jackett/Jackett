using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers.Meta
{
    public abstract class BaseMetaIndexer : BaseWebIndexer
    {
        protected BaseMetaIndexer(string name, string id, string description,
                                  IFallbackStrategyProvider fallbackStrategyProvider,
                                  IResultFilterProvider resultFilterProvider, IIndexerConfigurationService configService,
                                  WebClient client, Logger logger, ConfigurationData configData, IProtectionService ps,
                                  ICacheService cs, Func<IIndexer, bool> filter)
            : base(id: id,
                   name: name,
                   description: description,
                   link: "http://127.0.0.1/",
                   caps: new TorznabCapabilities(),
                   configService: configService,
                   client: client,
                   logger: logger,
                   p: ps,
                   cacheService: cs,
                   configData: configData)
        {
            filterFunc = filter;
            this.fallbackStrategyProvider = fallbackStrategyProvider;
            this.resultFilterProvider = resultFilterProvider;
        }

        public override bool CanHandleQuery(TorznabQuery query)
        {
            if (query == null)
                return false;
            if (query.QueryType == "indexers")
                return true;
            return base.CanHandleQuery(query);
        }

        public override Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson) => Task.FromResult(IndexerConfigurationStatus.Completed);

        public override async Task<IndexerResult> ResultsForQuery(TorznabQuery query, bool isMetaIndexer)
        {
            if (!CanHandleQuery(query) || !CanHandleCategories(query, true))
                return new IndexerResult(this, new ReleaseInfo[0], false);

            try
            {
                var results = await PerformQuery(query);
                // the results are already filtered and fixed by each indexer
                // some results may come from cache, but we can't inform without refactor the code
                return new IndexerResult(this, results, false);
            }
            catch (Exception ex)
            {
                throw new IndexerException(this, ex);
            }
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var indexers = ValidIndexers;
            IEnumerable<Task<IndexerResult>> supportedTasks = indexers.Where(i => i.CanHandleQuery(query)).Select(i => i.ResultsForQuery(query, true)).ToList(); // explicit conversion to List to execute LINQ query

            var fallbackStrategies = fallbackStrategyProvider.FallbackStrategiesForQuery(query);
            var fallbackQueries = fallbackStrategies.Select(async f => await f.FallbackQueries()).SelectMany(t => t.Result);
            var fallbackTasks = fallbackQueries.SelectMany(q => indexers.Where(i => !i.CanHandleQuery(query) && i.CanHandleQuery(q)).Select(i => i.ResultsForQuery(q.Clone(), true)));
            var tasks = supportedTasks.Concat(fallbackTasks.ToList()); // explicit conversion to List to execute LINQ query

            // When there are many indexers used by a metaindexer querying each and every one of them can take very very
            // long. This may result in a problem especially with Sonarr, which does consecutive searches when searching
            // for a season. Also, there might be indexers that do not support fast consecutive searches.
            // Therefore doing a season search in Sonarr might take for more than 90 seconds (approx. timeout in Sonarr)
            // which will mark Jackett as unresponsive (and therefore deactivated).
            // Although that 40 second is just an arbitrary number, doing a 40 second timeout is acceptable since an API
            // not responding for 40 second.. well, no one should really use that.
            // I hope in the future these queries will speed up (using caching or some other magic), however until then
            // just stick with a timeout.
            var aggregateTask = tasks.Until(TimeSpan.FromSeconds(40));

            try
            {
                await aggregateTask;
            }
            catch
            {
                logger.Error(aggregateTask.Exception, "Error during request in metaindexer " + Id);
            }

            var unorderedResult = aggregateTask.Result.SelectMany(r => r.Releases);
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

        public override TorznabCapabilities TorznabCaps => ValidIndexers.Select(i => i.TorznabCaps).Aggregate(new TorznabCapabilities(), TorznabCapabilities.Concat);

        public override bool IsConfigured => Indexers != null;

        public override string[] Tags => Array.Empty<string>();

        public IEnumerable<IIndexer> ValidIndexers => Indexers?.Where(i => i.IsConfigured && filterFunc(i));

        public IEnumerable<IIndexer> Indexers;

        private readonly Func<IIndexer, bool> filterFunc;
        private readonly IFallbackStrategyProvider fallbackStrategyProvider;
        private readonly IResultFilterProvider resultFilterProvider;
    }
}
