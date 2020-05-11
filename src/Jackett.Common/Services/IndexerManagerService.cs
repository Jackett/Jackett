using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jackett.Common.Indexers;
using Jackett.Common.Indexers.Meta;
using Jackett.Common.Models;
using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Jackett.Common.Services
{

    public class IndexerManagerService : IIndexerManagerService
    {
        private readonly ICacheService cacheService;
        private readonly IIndexerConfigurationService configService;
        private readonly IProtectionService protectionService;
        private readonly WebClient webClient;
        private readonly IProcessService processService;
        private readonly IConfigurationService globalConfigService;
        private readonly ServerConfig serverConfig;
        private readonly Logger logger;

        private readonly Dictionary<string, IIndexer> indexers = new Dictionary<string, IIndexer>();
        private AggregateIndexer aggregateIndexer;

        // this map is used to maintain backward compatibility when renaming the id of an indexer
        // (the id is used in the torznab/download/search urls and in the indexer configuration file)
        // if the indexer is removed, remove it from this list too
        // use: {"<old id>", "<new id>"}
        private readonly Dictionary<string, string> renamedIndexers = new Dictionary<string, string>
        {
            {"nostalgic", "vhstapes"}
        };

        public IndexerManagerService(IIndexerConfigurationService config, IProtectionService protectionService, WebClient webClient, Logger l, ICacheService cache, IProcessService processService, IConfigurationService globalConfigService, ServerConfig serverConfig)
        {
            configService = config;
            this.protectionService = protectionService;
            this.webClient = webClient;
            this.processService = processService;
            this.globalConfigService = globalConfigService;
            this.serverConfig = serverConfig;
            logger = l;
            cacheService = cache;
        }

        public void InitIndexers(IEnumerable<string> path)
        {
            MigrateRenamedIndexers();
            InitIndexers();
            InitCardigannIndexers(path);
            InitAggregateIndexer();
        }

        private void MigrateRenamedIndexers()
        {
            foreach (var oldId in renamedIndexers.Keys)
            {
                var oldPath = configService.GetIndexerConfigFilePath(oldId);
                if (File.Exists(oldPath))
                {
                    // if the old configuration exists, we rename if to be used by the renamed indexer
                    var newPath = configService.GetIndexerConfigFilePath(renamedIndexers[oldId]);
                    File.Move(oldPath, newPath);
                    logger.Info($"Configuration renamed: {oldPath} => {newPath}");
                }
            }
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
                var constructorArgumentTypes = new Type[] { typeof(IIndexerConfigurationService), typeof(WebClient), typeof(Logger), typeof(IProtectionService) };
                var constructor = type.GetConstructor(constructorArgumentTypes);
                if (constructor != null)
                {
                    // create own webClient instance for each indexer (seperate cookies stores, etc.)
                    var indexerWebClientInstance = (WebClient)Activator.CreateInstance(webClient.GetType(), processService, logger, globalConfigService, serverConfig);

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
                indexers.Add(idx.Id, idx);
                configService.Load(idx);
            }
        }

        private void InitCardigannIndexers(IEnumerable<string> path)
        {
            logger.Info("Loading Cardigann definitions from: " + string.Join(", ", path));

            var deserializer = new DeserializerBuilder()
                        .WithNamingConvention(CamelCaseNamingConvention.Instance)
//                        .IgnoreUnmatchedProperties()
                        .Build();

            try
            {
                var directoryInfos = path.Select(p => new DirectoryInfo(p));
                var existingDirectories = directoryInfos.Where(d => d.Exists);
                var files = existingDirectories.SelectMany(d => d.GetFiles("*.yml"));
                var definitions = files.Select(file =>
                {
                    logger.Debug("Loading Cardigann definition " + file.FullName);
                    try
                    {
                        var DefinitionString = File.ReadAllText(file.FullName);
                        var definition = deserializer.Deserialize<IndexerDefinition>(DefinitionString);
                        return definition;
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Error while parsing Cardigann definition " + file.FullName + ": " + ex.Message);
                        return null;
                    }
                }).Where(definition => definition != null);

                var cardigannIndexers = definitions.Select(definition =>
                {
                    try
                    {
                        // create own webClient instance for each indexer (seperate cookies stores, etc.)
                        var indexerWebClientInstance = (WebClient)Activator.CreateInstance(webClient.GetType(), processService, logger, globalConfigService, serverConfig);

                        IIndexer indexer = new CardigannIndexer(configService, indexerWebClientInstance, logger, protectionService, definition);
                        configService.Load(indexer);
                        return indexer;
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Error while creating Cardigann instance from Definition: " + ex.Message);
                        return null;
                    }
                }).Where(cardigannIndexer => cardigannIndexer != null).ToList(); // Explicit conversion to list to avoid repeated resource loading

                foreach (var indexer in cardigannIndexers)
                {
                    if (indexers.ContainsKey(indexer.Id))
                    {
                        logger.Debug(string.Format("Ignoring definition ID={0}: Indexer already exists", indexer.Id));
                        continue;
                    }

                    indexers.Add(indexer.Id, indexer);
                }
                logger.Info("Cardigann definitions loaded: " + string.Join(", ", indexers.Keys));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error while loading Cardigann definitions: " + ex.Message);
            }
        }

        public void InitAggregateIndexer()
        {
            var omdbApiKey = serverConfig.OmdbApiKey;
            IFallbackStrategyProvider fallbackStrategyProvider = null;
            IResultFilterProvider resultFilterProvider = null;
            if (!string.IsNullOrWhiteSpace(omdbApiKey))
            {
                var imdbResolver = new OmdbResolver(webClient, omdbApiKey, serverConfig.OmdbApiUrl);
                fallbackStrategyProvider = new ImdbFallbackStrategyProvider(imdbResolver);
                resultFilterProvider = new ImdbTitleResultFilterProvider(imdbResolver);
            }
            else
            {
                fallbackStrategyProvider = new NoFallbackStrategyProvider();
                resultFilterProvider = new NoResultFilterProvider();
            }

            logger.Info("Adding aggregate indexer");
            aggregateIndexer = new AggregateIndexer(fallbackStrategyProvider, resultFilterProvider, configService, webClient, logger, protectionService)
            {
                Indexers = indexers.Values
            };
        }

        public IIndexer GetIndexer(string name)
        {
            // old id of renamed indexer is used to maintain backward compatibility
            // both, the old id and the new one can be used until we remove it from renamedIndexers
            var realName = name;
            if (renamedIndexers.ContainsKey(name))
            {
                realName = renamedIndexers[name];
                logger.Warn($"Indexer {name} has been renamed to {realName}. Please, update the URL of the feeds. " +
                            "This may stop working in the future.");
            }

            if (indexers.ContainsKey(realName))
                return indexers[realName];

            if (realName == "all")
                return aggregateIndexer;

            logger.Error("Request for unknown indexer: " + realName);
            throw new Exception("Unknown indexer: " + realName);
        }

        public IWebIndexer GetWebIndexer(string name)
        {
            if (indexers.ContainsKey(name))
            {
                return indexers[name] as IWebIndexer;
            }
            else if (name == "all")
            {
                return aggregateIndexer as IWebIndexer;
            }

            logger.Error("Request for unknown indexer: " + name);
            throw new Exception("Unknown indexer: " + name);
        }

        public IEnumerable<IIndexer> GetAllIndexers() => indexers.Values.OrderBy(_ => _.DisplayName);

        public async Task TestIndexer(string name)
        {
            var indexer = GetIndexer(name);
            var browseQuery = new TorznabQuery
            {
                QueryType = "search",
                SearchTerm = "",
                IsTest = true
            };
            var result = await indexer.ResultsForQuery(browseQuery);
            logger.Info(string.Format("Found {0} releases from {1}", result.Releases.Count(), indexer.DisplayName));
            if (result.Releases.Count() == 0)
                throw new Exception("Found no results while trying to browse this tracker");
            cacheService.CacheRssResults(indexer, result.Releases);
        }

        public void DeleteIndexer(string name)
        {
            var indexer = GetIndexer(name);
            configService.Delete(indexer);
            indexer.Unconfigure();
        }
    }
}
