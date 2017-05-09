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

namespace Jackett.Indexers
{
    public abstract class BaseMetaIndexer : BaseIndexer, IIndexer
    {
        protected BaseMetaIndexer(string name, string link, string description, IIndexerManagerService manager, IWebClient client, Logger logger, ConfigurationData configData, IProtectionService p, TorznabCapabilities caps = null, string downloadBase = null)
            : base(name, link, description, manager, client, logger, configData, p, caps, downloadBase)
        {
        }

        public abstract Task<IEnumerable<ReleaseInfo>> PerformQuery (TorznabQuery query);

        public Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            return Task.FromResult(IndexerConfigurationStatus.Completed);
        }

        public void SetIndexers(IEnumerable<IIndexer> indexers)
        {
            Indexers = indexers.Where(i => i.IsConfigured);

            var caps = new TorznabCapabilities ();
            foreach (var indexer in indexers) {
                var indexerCaps = indexer.TorznabCaps;
                caps.SearchAvailable = caps.SearchAvailable || indexerCaps.SearchAvailable;
                caps.TVSearchAvailable = caps.TVSearchAvailable || indexerCaps.TVSearchAvailable;
                caps.MovieSearchAvailable = caps.MovieSearchAvailable || indexerCaps.MovieSearchAvailable;
                caps.SupportsTVRageSearch = caps.SupportsTVRageSearch || indexerCaps.SupportsTVRageSearch;
                caps.SupportsImdbSearch = caps.SupportsImdbSearch || indexerCaps.SupportsImdbSearch;
                caps.Categories.AddRange (indexerCaps.Categories.Except (caps.Categories));
            }

            base.TorznabCaps = caps;
            base.IsConfigured = true;
        }

        public override Uri UncleanLink (Uri link)
        {
            var indexer = GetOriginalIndexerForLink (link);
            if (indexer != null)
                return indexer.UncleanLink (link);

            return base.UncleanLink (link);
        }

        public override Task<byte []> Download (Uri link)
        {
            var indexer = GetOriginalIndexerForLink (link);
            if (indexer != null)
                return indexer.Download (link);

            return base.Download (link);
        }

        private IIndexer GetOriginalIndexerForLink (Uri link)
        {
            var prefix = string.Format ("{0}://{1}", link.Scheme, link.Host);
            var validIndexers = Indexers.Where (i => i.SiteLink.StartsWith (prefix, StringComparison.CurrentCulture));
            if (validIndexers.Count () > 0)
                return validIndexers.First ();

            return null;
        }

        protected IEnumerable<IIndexer> Indexers;
    }
}
