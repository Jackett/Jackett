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
    public abstract class BaseMetaIndexer : BaseIndexer, IIndexer
    {
        protected BaseMetaIndexer(string name, string description, IIndexerManagerService manager, Logger logger, ConfigurationData configData, IProtectionService p, Func<IIndexer, bool> filter)
            : base(name, "http://127.0.0.1/", description, manager, null, logger, configData, p, null, null)
        {
            filterFunc = filter;
        }

        public Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            return Task.FromResult(IndexerConfigurationStatus.Completed);
        }

        public virtual async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var tasks = Indexers.Where(i => i.CanHandleQuery(query)).Select(i => i.PerformQuery(query)).ToList(); // explicit conversion to List to execute LINQ query
            var aggregateTask = Task.WhenAll<IEnumerable<ReleaseInfo>>(tasks);
            await aggregateTask;
            if (aggregateTask.Exception != null)
                logger.Error(aggregateTask.Exception, "Error during request in metaindexer " + ID);

            IEnumerable<ReleaseInfo> result = tasks.Where(x => x.Status == TaskStatus.RanToCompletion).SelectMany(x => x.Result).OrderByDescending(r => r.PublishDate); // Ordering by the number of seeders might be useful as well.
            // Limiting the response size might be interesting for use-cases where there are
            // tons of trackers configured in Jackett. For now just use the limit param if
            // someone wants to do that.
            if (query.Limit > 0)
                result = result.Take(query.Limit);
            return result;
        }

        public override Uri UncleanLink(Uri link)
        {
            var indexer = GetOriginalIndexerForLink(link);
            if (indexer != null)
                return indexer.UncleanLink(link);

            return base.UncleanLink(link);
        }

        public override Task<byte[]> Download(Uri link)
        {
            var indexer = GetOriginalIndexerForLink(link);
            if (indexer != null)
                return indexer.Download(link);

            return base.Download(link);
        }

        private IIndexer GetOriginalIndexerForLink(Uri link)
        {
            var prefix = string.Format("{0}://{1}", link.Scheme, link.Host);
            var validIndexers = Indexers.Where(i => i.SiteLink.StartsWith(prefix, StringComparison.CurrentCulture));
            if (validIndexers.Count() > 0)
                return validIndexers.First();

            return null;
        }

        private Func<IIndexer, bool> filterFunc;
        private IEnumerable<IIndexer> indexers;
        public IEnumerable<IIndexer> Indexers {
            get {
                return indexers;
            }
            set {
                indexers = value.Where(i => i.IsConfigured && filterFunc(i));
                TorznabCaps = value.Select(i => i.TorznabCaps).Aggregate(new TorznabCapabilities(), TorznabCapabilities.Concat); ;
                IsConfigured = true;
            }
        }
    }
}
