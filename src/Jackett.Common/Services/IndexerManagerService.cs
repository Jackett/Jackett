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
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using NLog;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Jackett.Common.Services
{
    public class IndexerManagerService : IIndexerManagerService
    {
        private readonly ICacheService _cacheService;
        private readonly IIndexerConfigurationService _configService;
        private readonly IProtectionService _protectionService;
        private readonly WebClient _webClient;
        private readonly IProcessService _processService;
        private readonly IConfigurationService _globalConfigService;
        private readonly ServerConfig _serverConfig;
        private readonly Logger _logger;

        private readonly Dictionary<string, IIndexer> _indexers = new Dictionary<string, IIndexer>();
        private AggregateIndexer _aggregateIndexer;

        public IndexerManagerService(IIndexerConfigurationService config, IProtectionService protectionService,
                                     WebClient webClient, Logger l, ICacheService cache, IProcessService processService,
                                     IConfigurationService globalConfigService, ServerConfig serverConfig)
        {
            _configService = config;
            _protectionService = protectionService;
            _webClient = webClient;
            _processService = processService;
            _globalConfigService = globalConfigService;
            _serverConfig = serverConfig;
            _logger = l;
            _cacheService = cache;
        }

        public void InitIndexers(IEnumerable<string> path)
        {
            InitIndexers();
            InitCardigannIndexers(path);
            InitAggregateIndexer();
        }

        private void InitIndexers()
        {
            _logger.Info($"Using HTTP Client: {_webClient.GetType().Name}");
            var allTypes = GetType().Assembly.GetTypes();
            var allIndexerTypes = allTypes.Where(p => typeof(IIndexer).IsAssignableFrom(p));
            var allInstantiatableIndexerTypes = allIndexerTypes.Where(p => !p.IsInterface && !p.IsAbstract);
            var allNonMetaInstantiatableIndexerTypes =
                allInstantiatableIndexerTypes.Where(p => !typeof(BaseMetaIndexer).IsAssignableFrom(p));
            var indexerTypes = allNonMetaInstantiatableIndexerTypes.Where(p => p.Name != "CardigannIndexer");
            var ixs = indexerTypes.Select(
                type =>
                {
                    var constructorArgumentTypes = new[]
                    {
                        typeof(IIndexerConfigurationService),
                        typeof(WebClient),
                        typeof(Logger),
                        typeof(IProtectionService)
                    };
                    var constructor = type.GetConstructor(constructorArgumentTypes);
                    if (constructor != null)
                    {
                        // create own webClient instance for each indexer (seperate cookies stores, etc.)
                        var indexerWebClientInstance = (WebClient)Activator.CreateInstance(
                            _webClient.GetType(), _processService, _logger, _globalConfigService, _serverConfig);
                        var arguments = new object[]
                        {
                            _configService,
                            indexerWebClientInstance,
                            _logger,
                            _protectionService
                        };
                        var indexer = (IIndexer)constructor.Invoke(arguments);
                        return indexer;
                    }

                    _logger.Error($"Cannot instantiate {type.Name}");
                    return null;
                });
            foreach (var idx in ixs)
            {
                if (idx == null)
                    continue;
                _indexers.Add(idx.ID, idx);
                _configService.Load(idx);
            }
        }

        private void InitCardigannIndexers(IEnumerable<string> path)
        {
            _logger.Info($"Loading Cardigann definitions from: {string.Join(", ", path)}");
            var deserializer = new DeserializerBuilder()
                               .WithNamingConvention(CamelCaseNamingConvention.Instance).IgnoreUnmatchedProperties().Build();
            try
            {
                var directoryInfos = path.Select(p => new DirectoryInfo(p));
                var existingDirectories = directoryInfos.Where(d => d.Exists);
                var files = existingDirectories.SelectMany(d => d.GetFiles("*.yml"));
                var definitions = files.Select(
                    file =>
                    {
                        _logger.Info($"Loading Cardigann definition {file.FullName}");
                        try
                        {
                            var definitionString = File.ReadAllText(file.FullName);
                            var definition =
                                deserializer.Deserialize<IndexerDefinition>(definitionString);
                            return definition;
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(
                                ex, $"Error while parsing Cardigann definition {file.FullName}: {ex.Message}");
                            return null;
                        }
                    }).Where(definition => definition != null);
                var cardigannIndexers = definitions.Select(
                                                       definition =>
                                                       {
                                                           try
                                                           {
                                                               // create own webClient instance for each indexer (seperate cookies stores, etc.)
                                                               var indexerWebClientInstance =
                                                                   (WebClient)Activator.CreateInstance(
                                                                       _webClient.GetType(), _processService, _logger,
                                                                       _globalConfigService, _serverConfig);
                                                               IIndexer indexer = new CardigannIndexer(
                                                                   _configService, indexerWebClientInstance, _logger,
                                                                   _protectionService, definition);
                                                               _configService.Load(indexer);
                                                               return indexer;
                                                           }
                                                           catch (Exception ex)
                                                           {
                                                               _logger.Error(
                                                                   ex,
                                                                   $"Error while creating Cardigann instance from Definition: {ex.Message}");
                                                               return null;
                                                           }
                                                       }).Where(cardigannIndexer => cardigannIndexer != null)
                                                   .ToList(); // Explicit conversion to list to avoid repeated resource loading
                foreach (var indexer in cardigannIndexers)
                {
                    if (_indexers.ContainsKey(indexer.ID))
                    {
                        _logger.Debug(string.Format("Ignoring definition ID={0}: Indexer already exists", indexer.ID));
                        continue;
                    }

                    _indexers.Add(indexer.ID, indexer);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error while loading Cardigann definitions: {ex.Message}");
            }
        }

        public void InitAggregateIndexer()
        {
            var omdbApiKey = _serverConfig.OmdbApiKey;
            IFallbackStrategyProvider fallbackStrategyProvider;
            IResultFilterProvider resultFilterProvider;
            if (!omdbApiKey.IsNullOrEmptyOrWhitespace())
            {
                var imdbResolver = new OmdbResolver(_webClient, omdbApiKey.ToNonNull(), _serverConfig.OmdbApiUrl);
                fallbackStrategyProvider = new ImdbFallbackStrategyProvider(imdbResolver);
                resultFilterProvider = new ImdbTitleResultFilterProvider(imdbResolver);
            }
            else
            {
                fallbackStrategyProvider = new NoFallbackStrategyProvider();
                resultFilterProvider = new NoResultFilterProvider();
            }

            _logger.Info("Adding aggregate indexer");
            _aggregateIndexer = new AggregateIndexer(
                fallbackStrategyProvider, resultFilterProvider, _configService, _webClient, _logger, _protectionService)
            {
                Indexers = _indexers.Values
            };
        }

        public IIndexer GetIndexer(string name)
        {
            if (_indexers.ContainsKey(name))
                return _indexers[name];
            if (name == "all")
                return _aggregateIndexer;
            _logger.Error($"Request for unknown indexer: {name}");
            throw new Exception($"Unknown indexer: {name}");
        }

        public IWebIndexer GetWebIndexer(string name)
        {
            if (_indexers.ContainsKey(name))
                return _indexers[name] as IWebIndexer;
            if (name == "all")
                return _aggregateIndexer;
            _logger.Error($"Request for unknown indexer: {name}");
            throw new Exception($"Unknown indexer: {name}");
        }

        public IEnumerable<IIndexer> GetAllIndexers() => _indexers.Values.OrderBy(_ => _.DisplayName);

        public async Task TestIndexer(string name)
        {
            var indexer = GetIndexer(name);
            var browseQuery = new TorznabQuery { QueryType = "search", SearchTerm = "", IsTest = true };
            var result = await indexer.ResultsForQuery(browseQuery);
            _logger.Info(string.Format("Found {0} releases from {1}", result.Releases.Count(), indexer.DisplayName));
            if (result.Releases.Count() == 0)
                throw new Exception("Found no results while trying to browse this tracker");
            _cacheService.CacheRssResults(indexer, result.Releases);
        }

        public void DeleteIndexer(string name)
        {
            var indexer = GetIndexer(name);
            _configService.Delete(indexer);
            indexer.Unconfigure();
        }
    }
}
