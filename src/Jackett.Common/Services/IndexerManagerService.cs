using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jackett.Common.Indexers;
using Jackett.Common.Indexers.Definitions;
using Jackett.Common.Indexers.Meta;
using Jackett.Common.Models;
using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using FilterFunc = Jackett.Common.Utils.FilterFunc;

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
        private ConcurrentDictionary<string, IWebIndexer> _availableFilters = new ConcurrentDictionary<string, IWebIndexer>();

        // maps used to maintain backward compatibility when renaming the id of an indexer
        // (the id is used in the torznab/download/search urls and in the indexer configuration file)
        private Dictionary<string, string> _renamedIndexers = new Dictionary<string, string>();

        public IndexerManagerService(IIndexerConfigurationService config, IProtectionService protectionService, WebClient webClient, Logger l, ICacheService cache, IProcessService processService, IConfigurationService globalConfigService, ServerConfig serverConfig)
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

        public void InitIndexers(List<string> path)
        {
            _logger.Info("Using HTTP Client: {0}", _webClient.GetType().Name);

            InitIndexers();
            InitCardigannIndexers(path);

            _logger.Info("Loaded {0} indexers in total", _indexers.Count);

            MigrateRenamedIndexers();
            InitIndexersConfiguration();
            InitMetaIndexers();
            RemoveLegacyConfigurations();
        }

        private void MigrateRenamedIndexers()
        {
            _renamedIndexers = _indexers
                               .Where(x => x.Value.Replaces.Any())
                               .SelectMany(x => x.Value.Replaces.ToDictionary(y => y, _ => x.Key))
                               .ToDictionary(x => x.Key, x => x.Value);

            foreach (var oldId in _renamedIndexers.Keys)
            {
                var oldPath = _configService.GetIndexerConfigFilePath(oldId);

                if (File.Exists(oldPath))
                {
                    // if the old configuration exists, we rename it to be used by the renamed indexer
                    _logger.Info("Old configuration detected: {0}", oldPath);
                    var newPath = _configService.GetIndexerConfigFilePath(_renamedIndexers[oldId]);

                    if (File.Exists(newPath))
                    {
                        File.Delete(newPath);
                    }

                    File.Move(oldPath, newPath);

                    // backups
                    var oldPathBak = oldPath + ".bak";
                    var newPathBak = newPath + ".bak";

                    if (File.Exists(oldPathBak))
                    {
                        if (File.Exists(newPathBak))
                        {
                            File.Delete(newPathBak);
                        }

                        File.Move(oldPathBak, newPathBak);
                    }

                    _logger.Info("Configuration renamed: {0} => {1}", oldPath, newPath);
                }
            }
        }

        private void InitIndexers()
        {
            _logger.Info("Loading Native indexers ...");

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
                    var indexerWebClientInstance = (WebClient)Activator.CreateInstance(_webClient.GetType(), _processService, _logger, _globalConfigService, _serverConfig);

                    var arguments = new object[] { _configService, indexerWebClientInstance, _logger, _protectionService, _cacheService };
                    var indexer = (IIndexer)constructor.Invoke(arguments);
                    return indexer;
                }

                _logger.Error("Cannot instantiate Native indexer: {0}", type.Name);
                return null;
            }).Where(indexer => indexer != null).ToList();

            foreach (var indexer in nativeIndexers)
            {
                _indexers.Add(indexer.Id, indexer);
            }

            _logger.Info("Loaded {0} Native indexers.", nativeIndexers.Count);
            _logger.Debug("Native indexers loaded: {0}", string.Join(", ", nativeIndexers.Select(i => i.Id)));
        }

        private void InitIndexersConfiguration()
        {
            foreach (var indexer in _indexers)
            {
                _configService.Load(indexer.Value);
            }
        }

        private void InitCardigannIndexers(List<string> path)
        {
            _logger.Info("Loading Cardigann indexers from: " + string.Join(", ", path));

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
                    _logger.Debug("Loading Cardigann definition " + file.FullName);
                    try
                    {
                        var definitionString = File.ReadAllText(file.FullName);
                        var definition = deserializer.Deserialize<IndexerDefinition>(definitionString);
                        return definition;
                    }
                    catch (Exception e)
                    {
                        _logger.Error($"Error while parsing Cardigann definition {file.FullName}\n{e}");
                        return null;
                    }
                }).Where(definition => definition != null);

                var cardigannIndexers = definitions.Select(definition =>
                {
                    try
                    {
                        // create own webClient instance for each indexer (separate cookies stores, etc.)
                        var indexerWebClientInstance = (WebClient)Activator.CreateInstance(_webClient.GetType(), _processService, _logger, _globalConfigService, _serverConfig);

                        IIndexer indexer = new CardigannIndexer(_configService, indexerWebClientInstance, _logger, _protectionService, _cacheService, definition);
                        _configService.Load(indexer);
                        return indexer;
                    }
                    catch (Exception e)
                    {
                        _logger.Error($"Error while creating Cardigann instance from definition ID={definition.Id}: {e}");
                        return null;
                    }
                }).Where(cardigannIndexer => cardigannIndexer != null).ToList(); // Explicit conversion to list to avoid repeated resource loading

                var cardigannCounter = 0;
                var cardiganIds = new List<string>();
                foreach (var indexer in cardigannIndexers)
                {
                    if (_indexers.ContainsKey(indexer.Id))
                    {
                        _logger.Warn($"Ignoring definition ID {indexer.Id}: The indexer already exists");
                        continue;
                    }
                    _indexers.Add(indexer.Id, indexer);

                    cardigannCounter++;
                    cardiganIds.Add(indexer.Id);
                }

                _logger.Info($"Loaded {cardigannCounter} Cardigann indexers.");
                _logger.Debug($"Cardigann indexers loaded: {string.Join(", ", cardiganIds)}");
            }
            catch (Exception e)
            {
                _logger.Error($"Error while loading Cardigann definitions: {e}");
            }
        }

        public void InitMetaIndexers()
        {
            var (fallbackStrategyProvider, resultFilterProvider) = GetStrategyProviders();

            _logger.Info("Adding aggregate indexer ('all' indexer) ...");
            _aggregateIndexer = new AggregateIndexer(fallbackStrategyProvider, resultFilterProvider, _configService, _webClient, _logger, _protectionService, _cacheService)
            {
                Indexers = _indexers.Values
            };

            var predefinedFilters =
                new[] { "public", "private", "semi-public" }
                    .Select(type => (filter: FilterFunc.Type.ToFilter(type), func: FilterFunc.Type.ToFunc(type)))
                .Concat(
                _indexers.Values.SelectMany(x => x.Tags).Distinct()
                    .Select(tag => (filter: FilterFunc.Tag.ToFilter(tag), func: FilterFunc.Tag.ToFunc(tag)))
                ).Select(x => new KeyValuePair<string, IWebIndexer>(x.filter, CreateFilterIndexer(x.filter, x.func)));

            _availableFilters = new ConcurrentDictionary<string, IWebIndexer>(predefinedFilters);
        }

        public void RemoveLegacyConfigurations()
        {
            var directoryInfo = new DirectoryInfo(_globalConfigService.GetIndexerConfigDir());
            if (!directoryInfo.Exists)
                return; // the directory does not exist the first start
            var files = directoryInfo.GetFiles("*.json*");
            foreach (var file in files)
            {
                var indexerId = file.Name.Replace(".bak", "").Replace(".json", "");
                if (!_indexers.ContainsKey(indexerId) && File.Exists(file.FullName))
                {
                    _logger.Info($"Removing old configuration file: {file.FullName}");
                    File.Delete(file.FullName);
                }
            }
        }

        public IIndexer GetIndexer(string name)
        {
            // old id of renamed indexer is used to maintain backward compatibility
            // both, the old id and the new one can be used until we remove it from renamedIndexers
            var realName = name;

            if (_renamedIndexers.TryGetValue(name, out var indexerName))
            {
                realName = indexerName;
                _logger.Warn(@"Indexer {0} has been renamed to {1}. Please, update the URL of the feeds.
 This may stop working in the future.", name, realName);
            }

            return GetWebIndexer(realName);
        }


        public IWebIndexer GetWebIndexer(string name)
        {
            if (_indexers.TryGetValue(name, out var webIndexer))
            {
                return webIndexer as IWebIndexer;
            }

            if (name == "all")
            {
                return _aggregateIndexer;
            }

            if (_availableFilters.TryGetValue(name, out var indexer))
            {
                return indexer;
            }

            if (FilterFunc.TryParse(name, out var filterFunc))
            {
                return _availableFilters.GetOrAdd(name, x => CreateFilterIndexer(name, filterFunc));
            }

            _logger.Error($"Request for unknown indexer: {name.Replace(Environment.NewLine, "")}");
            throw new Exception($"Unknown indexer: {name}");
        }

        public List<IIndexer> GetAllIndexers() => _indexers.Values.OrderBy(x => x.Name).ToList();

        public async Task TestIndexer(string name)
        {
            var stopwatch = Stopwatch.StartNew();

            var indexer = GetIndexer(name);

            var query = new TorznabQuery
            {
                QueryType = "search",
                SearchTerm = "",
                IsTest = true
            };

            var result = await indexer.ResultsForQuery(query);

            stopwatch.Stop();

            _logger.Info($"Test search in {indexer.Name} => Found {result.Releases.Count()} releases [{stopwatch.ElapsedMilliseconds:0}ms]");

            if (!result.Releases.Any())
            {
                throw new Exception($"Test search in {indexer.Name} => Found no results while trying to browse this tracker. This may be an issue with the indexer, or other indexer settings such as search freeleech only etc.");
            }
        }

        public void DeleteIndexer(string name)
        {
            var indexer = GetIndexer(name);
            _configService.Delete(indexer);
            indexer.Unconfigure();
        }

        private IWebIndexer CreateFilterIndexer(string filter, Func<IIndexer, bool> filterFunc)
        {
            var (fallbackStrategyProvider, resultFilterProvider) = GetStrategyProviders();
            _logger.Info($"Adding filter indexer ('{filter.Replace(Environment.NewLine, "")}' indexer) ...");
            return new FilterIndexer(
                    filter,
                    fallbackStrategyProvider,
                    resultFilterProvider,
                    _configService,
                    _webClient,
                    _logger,
                    _protectionService,
                    _cacheService,
                    filterFunc
                )
            {
                Indexers = _indexers.Values
            };
        }

        private (IFallbackStrategyProvider fallbackStrategyProvider, IResultFilterProvider resultFilterProvider)
            GetStrategyProviders()
        {
            var omdbApiKey = _serverConfig.OmdbApiKey;
            IFallbackStrategyProvider fallbackStrategyProvider;
            IResultFilterProvider resultFilterProvider;
            if (!string.IsNullOrWhiteSpace(omdbApiKey))
            {
                var imdbResolver = new OmdbResolver(_webClient, omdbApiKey, _serverConfig.OmdbApiUrl);
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
