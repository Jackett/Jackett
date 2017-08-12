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

        void InitIndexers(IEnumerable<string> path);
        void InitAggregateIndexer();
    }

    public class IndexerManagerService : IIndexerManagerService
    {
        private ICacheService cacheService;
        private IIndexerConfigurationService configService;
        private IProtectionService protectionService;
        private IWebClient webClient;
        private IProcessService processService;
        private IConfigurationService globalConfigService;

        private Logger logger;

        private Dictionary<string, IIndexer> indexers = new Dictionary<string, IIndexer>();
        private AggregateIndexer aggregateIndexer;

        public IndexerManagerService(IIndexerConfigurationService config, IProtectionService protectionService, IWebClient webClient, Logger l, ICacheService cache, IProcessService processService, IConfigurationService globalConfigService)
        {
            configService = config;
            this.protectionService = protectionService;
            this.webClient = webClient;
            this.processService = processService;
            this.globalConfigService = globalConfigService;
            logger = l;
            cacheService = cache;
        }

        public void InitIndexers(IEnumerable<string> path)
        {
            InitIndexers();
            InitCardigannIndexers(path);
            InitAggregateIndexer();
        }

        private void InitIndexers()
        {
            logger.Info("Using HTTP Client: " + webClient.GetType().Name);

            var allTypes = GetType().Assembly.GetTypes();
            var allIndexerTypes = allTypes.Where(p => typeof(IIndexer).IsAssignableFrom(p));
            var allInstantiatableIndexerTypes = allIndexerTypes.Where(p => !p.IsInterface && !p.IsAbstract);
            var allNonMetaInstantiatableIndexerTypes = allInstantiatableIndexerTypes.Where(p => !typeof(BaseMetaIndexer).IsAssignableFrom(p));
            var indexerTypes = allNonMetaInstantiatableIndexerTypes.Where(p => p.Name != "CardigannIndexer");
            var ixs = indexerTypes.Select(type =>
            {
                var constructorArgumentTypes = new Type[] { typeof(IIndexerConfigurationService), typeof(IWebClient), typeof(Logger), typeof(IProtectionService) };
                var constructor = type.GetConstructor(constructorArgumentTypes);
                if (constructor != null)
                {
                    // create own webClient instance for each indexer (seperate cookies stores, etc.)
                    var indexerWebClientInstance = (IWebClient)Activator.CreateInstance(webClient.GetType(), processService, logger, globalConfigService);

                    var arguments = new object[] { configService, indexerWebClientInstance, logger, protectionService };
                    var indexer = (IIndexer)constructor.Invoke(arguments);
                    return indexer;
                }
                else
                {
                    logger.Error("Cannot instantiate " + type.Name);
                }
                return null;
            });

            foreach (var idx in ixs)
            {
                if (idx == null)
                    continue;
                indexers.Add(idx.ID, idx);
                configService.Load(idx);
            }
        }

        private void InitCardigannIndexers(IEnumerable<string> path)
        {
            logger.Info("Loading Cardigann definitions from: " + string.Join(", ", path));

            var deserializer = new DeserializerBuilder()
                        .WithNamingConvention(new CamelCaseNamingConvention())
                        .IgnoreUnmatchedProperties()
                        .Build();

            try
            {
                var directoryInfos = path.Select(p => new DirectoryInfo(p));
                var existingDirectories = directoryInfos.Where(d => d.Exists);
                var files = existingDirectories.SelectMany(d => d.GetFiles("*.yml"));
                var definitions = files.Select(file =>
                {
                    logger.Info("Loading Cardigann definition " + file.FullName);

                    string DefinitionString = File.ReadAllText(file.FullName);
                    var definition = deserializer.Deserialize<IndexerDefinition>(DefinitionString);

                    return definition;
                });
                var cardigannIndexers = definitions.Select(definition =>
                {
                    // create own webClient instance for each indexer (seperate cookies stores, etc.)
                    var indexerWebClientInstance = (IWebClient)Activator.CreateInstance(webClient.GetType(), processService, logger, globalConfigService);

                    IIndexer indexer = new CardigannIndexer(configService, indexerWebClientInstance, logger, protectionService, definition);
                    configService.Load(indexer);
                    return indexer;
                }).ToList(); // Explicit conversion to list to avoid repeated resource loading

                foreach (var indexer in cardigannIndexers)
                {
                    if (indexers.ContainsKey(indexer.ID))
                    {
                        logger.Debug(string.Format("Ignoring definition ID={0}: Indexer already exists", indexer.ID));
                        continue;
                    }

                    indexers.Add(indexer.ID, indexer);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error while loading Cardigann definitions: " + ex.Message);
            }
        }

        public void InitAggregateIndexer()
        {
            var omdbApiKey = Engine.Server.Config.OmdbApiKey;
            IFallbackStrategyProvider fallbackStrategyProvider = null;
            IResultFilterProvider resultFilterProvider = null;
            if (!omdbApiKey.IsNullOrEmptyOrWhitespace())
            {
                var imdbResolver = new OmdbResolver(webClient, omdbApiKey.ToNonNull());
                fallbackStrategyProvider = new ImdbFallbackStrategyProvider(imdbResolver);
                resultFilterProvider = new ImdbTitleResultFilterProvider(imdbResolver);
            }
            else
            {
                fallbackStrategyProvider = new NoFallbackStrategyProvider();
                resultFilterProvider = new NoResultFilterProvider();
            }

            logger.Info("Adding aggregate indexer");
            aggregateIndexer = new AggregateIndexer(fallbackStrategyProvider, resultFilterProvider, configService, webClient, logger, protectionService);
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
            return indexers.Values.OrderBy(_ => _.DisplayName);
        }

        public async Task TestIndexer(string name)
        {
            var indexer = GetIndexer(name);
            var browseQuery = new TorznabQuery();
            browseQuery.QueryType = "search";
            browseQuery.SearchTerm = "";
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
    }
}
