using Autofac;
using Jackett.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Services
{
    public interface IIndexerManagerService
    {
        void TestIndexer(string name);
        void DeleteIndexer(string name);
        IndexerInterface GetIndexer(string name);
        IEnumerable<IndexerInterface> GetAllIndexers();
    }

    public class IndexerManagerService : IIndexerManagerService
    {
        private IContainer container;
        private IConfigurationService configService;
        private Logger logger;

        public IndexerManagerService(IContainer c, IConfigurationService config, Logger l)
        {
            container = c;
            configService = config;
            logger = l;
        }

        public IndexerInterface GetIndexer(string name)
        {
            return container.ResolveNamed<IndexerInterface>(name.ToLowerInvariant()); 
        }

        public IEnumerable<IndexerInterface> GetAllIndexers()
        {
            return container.Resolve<IEnumerable<IndexerInterface>>().OrderBy(_ => _.DisplayName);
        }

        public async void TestIndexer(string name)
        {
            var indexer = GetIndexer(name);
            var browseQuery = new TorznabQuery();
            var results = await indexer.PerformQuery(browseQuery);
            logger.Debug(string.Format("Found {0} releases from {1}", results.Length, indexer.DisplayName));
            if (results.Length == 0)
                throw new Exception("Found no results while trying to browse this tracker");
        }

        public void DeleteIndexer(string name)
        {
            var indexer = GetIndexer(name);
            var configPath = configService.GetIndexerConfigDir();
            File.Delete(configPath);
            //Indexers.Remove(name);
            //LoadMissingIndexers();
        }

        private string GetIndexerConfigFilePath(IndexerInterface indexer)
        {
            return Path.Combine(configService.GetIndexerConfigDir(), indexer.GetType().Name.ToLower() + ".json");
        }
    }
}
