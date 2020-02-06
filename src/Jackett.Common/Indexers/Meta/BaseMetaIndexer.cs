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
        protected BaseMetaIndexer(string name, string description, IFallbackStrategyProvider fallbackStrategyProvider,
                                  IResultFilterProvider resultFilterProvider, IIndexerConfigurationService configService,
                                  WebClient webClient, Logger logger, ConfigurationData configData, IProtectionService p,
                                  Func<IIndexer, bool> filter) : base(
            name, "http://127.0.0.1/", description, configService, webClient, logger, configData, p)
        {
            _filterFunc = filter;
            _fallbackStrategyProvider = fallbackStrategyProvider;
            _resultFilterProvider = resultFilterProvider;
        }

        public override bool CanHandleQuery(TorznabQuery query) => query != null && (query.QueryType == "indexers" || base.CanHandleQuery(query));

        public override Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson) =>
            Task.FromResult(IndexerConfigurationStatus.Completed);

        public override async Task<IndexerResult> ResultsForQuery(TorznabQuery query)
        {
            try
            {
                if (!CanHandleQuery(query))
                    return new IndexerResult(this, new ReleaseInfo[0]);
                var results = await PerformQuery(query);
                var correctedResults = results.Select(
                    r =>
                    {
                        if (r.PublishDate > DateTime.Now)
                            r.PublishDate = DateTime.Now;
                        return r;
                    });
                return new IndexerResult(this, correctedResults);
            }
            catch (Exception ex)
            {
                throw new IndexerException(this, ex);
            }
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var indexers = validIndexers;
            IEnumerable<Task<IndexerResult>> supportedTasks =
                indexers.Where(i => i.CanHandleQuery(query)).Select(i => i.ResultsForQuery(query))
                        .ToList(); // explicit conversion to List to execute LINQ query
            var fallbackStrategies = _fallbackStrategyProvider.FallbackStrategiesForQuery(query);
            var fallbackQueries = fallbackStrategies.Select(async f => await f.FallbackQueries()).SelectMany(t => t.Result);
            var fallbackTasks = fallbackQueries.SelectMany(
                q => indexers.Where(i => !i.CanHandleQuery(query) && i.CanHandleQuery(q))
                             .Select(i => i.ResultsForQuery(q.Clone())));
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
                logger.Error(aggregateTask.Exception, $"Error during request in metaindexer {ID}");
            }

            var unorderedResult = aggregateTask.Result.Select(r => r.Releases).Flatten();
            var resultFilters = _resultFilterProvider.FiltersForQuery(query);
            var filteredResults = resultFilters.Select(async f => await f.FilterResults(unorderedResult))
                                               .SelectMany(t => t.Result);
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

        public override TorznabCapabilities TorznabCaps => validIndexers
                                                           .Select(i => i.TorznabCaps).Aggregate(
                                                               new TorznabCapabilities(), TorznabCapabilities.Concat);

        public override bool IsConfigured => Indexers != null;

        private IEnumerable<IIndexer> validIndexers => Indexers?.Where(i => i.IsConfigured && _filterFunc(i));

        public IEnumerable<IIndexer> Indexers;

        private readonly Func<IIndexer, bool> _filterFunc;
        private readonly IFallbackStrategyProvider _fallbackStrategyProvider;
        private readonly IResultFilterProvider _resultFilterProvider;
    }
}
