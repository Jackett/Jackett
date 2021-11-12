using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jackett.Common.Indexers;
using Jackett.Common.Indexers.Meta;
using Jackett.Common.Models;
using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using NLog;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using FilterFunc = Jackett.Common.Utils.FilterFunc;

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
        private ConcurrentDictionary<string, IWebIndexer> availableFilters = new ConcurrentDictionary<string, IWebIndexer>();

        // this map is used to maintain backward compatibility when renaming the id of an indexer
        // (the id is used in the torznab/download/search urls and in the indexer configuration file)
        // if the indexer is removed, remove it from this list too
        // use: {"<old id>", "<new id>"}
        private readonly Dictionary<string, string> renamedIndexers = new Dictionary<string, string>
        {
            {"audiobooktorrents", "abtorrents"},
            {"broadcastthenet", "broadcasthenet"},
            {"hdreactor", "hdhouse"},
            {"icetorrent", "speedapp"},
            {"feedurneed", "devils-playground"},
            {"kickasstorrent-kathow", "kickasstorrents-ws"},
            {"legacyhd", "reelflix"},
            {"leaguehd", "lemonhd"},
            {"metaliplayro", "romanianmetaltorrents"},
            {"nbytez", "devils-playground"},
            {"nnm-club", "noname-club"},
            {"passtheheadphones", "redacted"},
            {"puntorrent", "puntotorrent"},
            {"rstorrent", "redstartorrent"},
            {"scenefz", "speedapp"},
            {"tehconnectionme", "anthelion"},
            {"torrentgalaxyorg", "torrentgalaxy"},
            {"transmithenet", "nebulance"},
            {"xtremezone", "speedapp"},
            {"yourexotic", "exoticaz"}
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
            logger.Info($"Using HTTP Client: {webClient.GetType().Name}");

            MigrateRenamedIndexers();
            InitIndexers();
            InitCardigannIndexers(path);
            InitMetaIndexers();
            RemoveLegacyConfigurations();
        }

        private void MigrateRenamedIndexers()
        {
            foreach (var oldId in renamedIndexers.Keys)
            {
                var oldPath = configService.GetIndexerConfigFilePath(oldId);
                if (File.Exists(oldPath))
                {
                    // if the old configuration exists, we rename it to be used by the renamed indexer
                    logger.Info($"Old configuration detected: {oldPath}");
                    var newPath = configService.GetIndexerConfigFilePath(renamedIndexers[oldId]);
                    if (File.Exists(newPath))
                        File.Delete(newPath);
                    File.Move(oldPath, newPath);
                    // backups
                    var oldPathBak = oldPath + ".bak";
                    var newPathBak = newPath + ".bak";
                    if (File.Exists(oldPathBak))
                    {
                        if (File.Exists(newPathBak))
                            File.Delete(newPathBak);
                        File.Move(oldPathBak, newPathBak);
                    }
                    logger.Info($"Configuration renamed: {oldPath} => {newPath}");
                }
            }
        }

        private void InitIndexers()
        {
            logger.Info("Loading Native indexers ...");

            var allTypes = GetType().Assembly.GetTypes();
            var allIndexerTypes = allTypes.Where(p => typeof(IIndexer).IsAssignableFrom(p));
            var allInstantiatableIndexerTypes = allIndexerTypes.Where(p => !p.IsInterface && !p.IsAbstract);
            var allNonMetaInstantiatableIndexerTypes = allInstantiatableIndexerTypes.Where(p => !typeof(BaseMetaIndexer).IsAssignableFrom(p));
            var indexerTypes = allNonMetaInstantiatableIndexerTypes.Where(p => p.Name != "CardigannIndexer");
            var nativeIndexers = indexerTypes.Select(type =>
            {
                var constructorArgumentTypes = new[] { typeof(IIndexerConfigurationService), typeof(WebClient), typeof(Logger), typeof(IProtectionService), typeof(ICacheService) };
                var constructor = type.GetConstructor(constructorArgumentTypes);
                if (constructor != null)
                {
                    // create own webClient instance for each indexer (separate cookies stores, etc.)
                    var indexerWebClientInstance = (WebClient)Activator.CreateInstance(webClient.GetType(), processService, logger, globalConfigService, serverConfig);

                    var arguments = new object[] { configService, indexerWebClientInstance, logger, protectionService, cacheService };
                    var indexer = (IIndexer)constructor.Invoke(arguments);
                    return indexer;
                }

                logger.Error($"Cannot instantiate Native indexer: {type.Name}");
                return null;
            }).Where(indexer => indexer != null).ToList();

            foreach (var indexer in nativeIndexers)
            {
                indexers.Add(indexer.Id, indexer);
                configService.Load(indexer);
            }

            logger.Info($"Loaded {nativeIndexers.Count} Native indexers: {string.Join(", ", nativeIndexers.Select(i => i.Id))}");
        }

        private void InitCardigannIndexers(IEnumerable<string> path)
        {
            logger.Info("Loading Cardigann indexers from: " + string.Join(", ", path));

            var deserializer = new DeserializerBuilder()
                        .WithNamingConvention(CamelCaseNamingConvention.Instance)
                        //.IgnoreUnmatchedProperties()
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
                        var definitionString = File.ReadAllText(file.FullName);
                        var definition = deserializer.Deserialize<IndexerDefinition>(definitionString);
                        return definition;
                    }
                    catch (Exception e)
                    {
                        logger.Error($"Error while parsing Cardigann definition {file.FullName}\n{e}");
                        return null;
                    }
                }).Where(definition => definition != null);

                var cardigannIndexers = definitions.Select(definition =>
                {
                    try
                    {
                        // create own webClient instance for each indexer (seperate cookies stores, etc.)
                        var indexerWebClientInstance = (WebClient)Activator.CreateInstance(webClient.GetType(), processService, logger, globalConfigService, serverConfig);

                        IIndexer indexer = new CardigannIndexer(configService, indexerWebClientInstance, logger, protectionService, cacheService, definition);
                        configService.Load(indexer);
                        return indexer;
                    }
                    catch (Exception e)
                    {
                        logger.Error($"Error while creating Cardigann instance from definition ID={definition.Id}: {e}");
                        return null;
                    }
                }).Where(cardigannIndexer => cardigannIndexer != null).ToList(); // Explicit conversion to list to avoid repeated resource loading

                var cardigannCounter = 0;
                var cardiganIds = new List<string>();
                foreach (var indexer in cardigannIndexers)
                {
                    if (indexers.ContainsKey(indexer.Id))
                    {
                        logger.Warn($"Ignoring definition ID={indexer.Id}: Indexer already exists");
                        continue;
                    }
                    indexers.Add(indexer.Id, indexer);

                    cardigannCounter++;
                    cardiganIds.Add(indexer.Id);
                }

                logger.Info($"Loaded {cardigannCounter} Cardigann indexers: {string.Join(", ", cardiganIds)}");
            }
            catch (Exception e)
            {
                logger.Error($"Error while loading Cardigann definitions: {e}");
            }
            logger.Info($"Loaded {indexers.Count} indexers in total");
        }

        public void InitMetaIndexers()
        {
            var (fallbackStrategyProvider, resultFilterProvider) = GetStrategyProviders();

            logger.Info("Adding aggregate indexer ('all' indexer) ...");
            aggregateIndexer = new AggregateIndexer(fallbackStrategyProvider, resultFilterProvider, configService, webClient, logger, protectionService, cacheService)
            {
                Indexers = indexers.Values
            };

            var predefinedFilters =
                new[] { "public", "private", "semi-public" }
                    .Select(type => (filter: FilterFunc.Type.ToFilter(type), func: FilterFunc.Type.ToFunc(type)))
                .Concat(
                indexers.Values.SelectMany(x => x.Tags).Distinct()
                    .Select(tag => (filter: FilterFunc.Tag.ToFilter(tag), func: FilterFunc.Tag.ToFunc(tag)))
                ).Select(x => new KeyValuePair<string, IWebIndexer>(x.filter, CreateFilterIndexer(x.filter, x.func)));

            availableFilters = new ConcurrentDictionary<string, IWebIndexer>(predefinedFilters);
        }

        public void RemoveLegacyConfigurations()
        {
            var directoryInfo = new DirectoryInfo(globalConfigService.GetIndexerConfigDir());
            if (!directoryInfo.Exists)
                return; // the directory does not exist the first start
            var files = directoryInfo.GetFiles("*.json*");
            foreach (var file in files)
            {
                var indexerId = file.Name.Replace(".bak", "").Replace(".json", "");
                if (!indexers.ContainsKey(indexerId) && File.Exists(file.FullName))
                {
                    logger.Info($"Removing old configuration file: {file.FullName}");
                    File.Delete(file.FullName);
                }
            }
        }

        public IIndexer GetIndexer(string name)
        {
            // old id of renamed indexer is used to maintain backward compatibility
            // both, the old id and the new one can be used until we remove it from renamedIndexers
            var realName = name;
            if (renamedIndexers.ContainsKey(name))
            {
                realName = renamedIndexers[name];
                logger.Warn($@"Indexer {name} has been renamed to {realName}. Please, update the URL of the feeds.
 This may stop working in the future.");
            }

            return GetWebIndexer(realName);
        }


        public IWebIndexer GetWebIndexer(string name)
        {
            if (indexers.ContainsKey(name))
                return indexers[name] as IWebIndexer;

            if (name == "all")
                return aggregateIndexer;

            if (availableFilters.TryGetValue(name, out var indexer))
                return indexer;

            if (FilterFunc.TryParse(name, out var filterFunc))
                return availableFilters.GetOrAdd(name, x => CreateFilterIndexer(name, filterFunc));

            logger.Error($"Request for unknown indexer: {name}");
            throw new Exception($"Unknown indexer: {name}");
        }

        public IEnumerable<IIndexer> GetAllIndexers() => indexers.Values.OrderBy(_ => _.DisplayName);

        public async Task TestIndexer(string name)
        {
            var indexer = GetIndexer(name);
            var query = new TorznabQuery
            {
                QueryType = "search",
                SearchTerm = "",
                IsTest = true
            };
            var result = await indexer.ResultsForQuery(query);

            logger.Info($"Test search in {indexer.DisplayName} => Found {result.Releases.Count()} releases");

            if (!result.Releases.Any())
                throw new Exception($"Test search in {indexer.DisplayName} => Found no results while trying to browse this tracker");
        }

        public void DeleteIndexer(string name)
        {
            var indexer = GetIndexer(name);
            configService.Delete(indexer);
            indexer.Unconfigure();
        }

        private IWebIndexer CreateFilterIndexer(string filter, Func<IIndexer, bool> filterFunc)
        {
            var (fallbackStrategyProvider, resultFilterProvider) = GetStrategyProviders();
            logger.Info($"Adding filter indexer ('{filter}' indexer) ...");
            return new FilterIndexer(
                    filter,
                    fallbackStrategyProvider,
                    resultFilterProvider,
                    configService,
                    webClient,
                    logger,
                    protectionService,
                    cacheService,
                    filterFunc
                )
            {
                Indexers = indexers.Values
            };
        }

        private (IFallbackStrategyProvider fallbackStrategyProvider, IResultFilterProvider resultFilterProvider)
            GetStrategyProviders()
        {
            var omdbApiKey = serverConfig.OmdbApiKey;
            IFallbackStrategyProvider fallbackStrategyProvider;
            IResultFilterProvider resultFilterProvider;
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

            return (fallbackStrategyProvider, resultFilterProvider);
        }

    }
}
