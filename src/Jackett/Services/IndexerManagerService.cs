using Autofac;
using Jackett.Indexers;
using Jackett.Models;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jackett.Indexers.Meta;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Jackett.Services
{
    public interface IIndexerManagerService
    {
        Task TestIndexer(string name);
        void DeleteIndexer(string name);
        IIndexer GetIndexer(string name);
        IWebIndexer GetWebIndexer(string name);
        IEnumerable<IIndexer> GetAllIndexers();

        void InitIndexers();
        void InitCardigannIndexers(string path);
        void InitAggregateIndexer();
        void SortIndexers();
    }

    public class IndexerManagerService : IIndexerManagerService
    {
        private IContainer container;
        private Logger logger;
        private Dictionary<string, IIndexer> indexers = new Dictionary<string, IIndexer>();
        private ICacheService cacheService;
        private IIndexerConfigurationService configService;
        private AggregateIndexer aggregateIndexer;

        public IndexerManagerService(IContainer c, IIndexerConfigurationService config, Logger l, ICacheService cache)
        {
            container = c;
            configService = config;
            logger = l;
            cacheService = cache;
        }

        public void InitIndexers()
        {
            logger.Info("Using HTTP Client: " + container.Resolve<IWebClient>().GetType().Name);

            foreach (var idx in container.Resolve<IEnumerable<IIndexer>>().OrderBy(_ => _.DisplayName))
            {
                indexers.Add(idx.ID, idx);
                configService.Load(idx);
            }
        }

        public void InitCardigannIndexers(string path)
        {
            logger.Info("Loading Cardigann definitions from: " + path);

            try
            {
                if (!Directory.Exists(path))
                    return;

                DirectoryInfo d = new DirectoryInfo(path);

                foreach (var file in d.GetFiles("*.yml"))
                {
                    logger.Info("Loading Cardigann definition " + file.FullName);
                    string DefinitionString = File.ReadAllText(file.FullName);
                    var deserializer = new DeserializerBuilder()
                        .WithNamingConvention(new CamelCaseNamingConvention())
                        .IgnoreUnmatchedProperties()
                        .Build();
                    var definition = deserializer.Deserialize<IndexerDefinition>(DefinitionString);

                    CardigannIndexer idx = new CardigannIndexer(configService, container.Resolve<IWebClient>(), logger, container.Resolve<IProtectionService>(), definition);
                    if (indexers.ContainsKey(idx.ID))
                    {
                        logger.Debug(string.Format("Ignoring definition ID={0}, file={1}: Indexer already exists", idx.ID, file.FullName));
                    }
                    else
                    {
                        indexers.Add(idx.ID, idx);
                        configService.Load(idx);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error while loading Cardigann definitions: " + ex.Message);
            }
        }

        public void InitAggregateIndexer()
        {
            var omdbApiKey = container.Resolve<IServerService>().Config.OmdbApiKey;
            IFallbackStrategyProvider fallbackStrategyProvider = null;
            IResultFilterProvider resultFilterProvider = null;
            if (omdbApiKey != null)
            {
                var imdbResolver = new OmdbResolver(container.Resolve<IWebClient>(), omdbApiKey.ToNonNull());
                fallbackStrategyProvider = new ImdbFallbackStrategyProvider(imdbResolver);
                resultFilterProvider = new ImdbTitleResultFilterProvider(imdbResolver);
            }
            else
            {
                fallbackStrategyProvider = new NoFallbackStrategyProvider();
                resultFilterProvider = new NoResultFilterProvider();
            }

            logger.Info("Adding aggregate indexer");
            aggregateIndexer = new AggregateIndexer(fallbackStrategyProvider, resultFilterProvider, configService, container.Resolve<IWebClient>(), logger, container.Resolve<IProtectionService>());
            aggregateIndexer.Indexers = indexers.Values;
        }

        public IIndexer GetIndexer(string name)
        {
            if (indexers.ContainsKey(name))
            {
                return indexers[name];
            }
            else if (name == "all")
            {
                return aggregateIndexer;
            }
            else
            {
                logger.Error("Request for unknown indexer: " + name);
                throw new Exception("Unknown indexer: " + name);
            }
        }

        public IWebIndexer GetWebIndexer(string name)
        {
            if (indexers.ContainsKey(name))
                return indexers[name] as IWebIndexer;

            logger.Error("Request for unknown indexer: " + name);
            throw new Exception("Unknown indexer: " + name);
        }

        public IEnumerable<IIndexer> GetAllIndexers()
        {
            return indexers.Values;
        }

        public async Task TestIndexer(string name)
        {
            var indexer = GetIndexer(name);
            var browseQuery = new TorznabQuery();
            browseQuery.IsTest = true;
            var results = await indexer.ResultsForQuery(browseQuery);
            logger.Info(string.Format("Found {0} releases from {1}", results.Count(), indexer.DisplayName));
            if (results.Count() == 0)
                throw new Exception("Found no results while trying to browse this tracker");
            cacheService.CacheRssResults(indexer, results);
        }

        public void DeleteIndexer(string name)
        {
            var indexer = GetIndexer(name);
            configService.Delete(indexer);
            indexer.Unconfigure();
        }

        public void SortIndexers()
        {
            // Apparently Dictionary are ordered but can't be sorted again
            // This will recreate the indexers Dictionary to workaround this limitation
            Dictionary<string, IIndexer> newIndexers = new Dictionary<string, IIndexer>();
            foreach (var indexer in indexers.OrderBy(_ => _.Value.DisplayName))
                newIndexers.Add(indexer.Key, indexer.Value);
            indexers = newIndexers;
        }
    }
}
