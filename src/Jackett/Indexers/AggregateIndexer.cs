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
            Indexers = indexers.Where(i => i.IsConfigured);

            var caps = new TorznabCapabilities();
            foreach (var indexer in indexers) {
                var indexerCaps = indexer.TorznabCaps;
                caps.SearchAvailable = caps.SearchAvailable || indexerCaps.SearchAvailable;
                caps.TVSearchAvailable = caps.TVSearchAvailable || indexerCaps.TVSearchAvailable;
                caps.MovieSearchAvailable = caps.MovieSearchAvailable || indexerCaps.MovieSearchAvailable;
                caps.SupportsTVRageSearch = caps.SupportsTVRageSearch || indexerCaps.SupportsTVRageSearch;
                caps.SupportsImdbSearch = caps.SupportsImdbSearch || indexerCaps.SupportsImdbSearch;
                caps.Categories.AddRange(indexerCaps.Categories.Except (caps.Categories));
            }

            base.TorznabCaps = caps;
            base.IsConfigured = true;
        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            return IndexerConfigurationStatus.Completed;
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var tasks = new List<Task<IEnumerable<ReleaseInfo>>>();
            foreach (var indexer in Indexers)
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
            var validIndexers = Indexers.Where(i => i.SiteLink.StartsWith(prefix));
            if (validIndexers.Count() > 0)
                return validIndexers.First();

            return null;
        }
    }
}