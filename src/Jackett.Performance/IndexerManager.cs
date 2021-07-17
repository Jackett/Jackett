using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AutoMapper;

using Jackett.Common.Indexers;
using Jackett.Common.Indexers.Meta;
using Jackett.Common.Models;
using Jackett.Common.Models.Config;
using Jackett.Common.Services;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using Jackett.Performance.Services;
using Jackett.Performance.Utils.Clients;
using Jackett.Server.Services;

using NLog;
using NLog.Targets;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Jackett.Performance
{
    internal class IndexerManager
    {
        private readonly PerformanceIndexerConfigurationService configService;
        private readonly PerformanceProtectionService protectionService;
        private readonly PerformanceCacheService cacheService;
        private readonly PerformanceProcessService processService;
        private readonly PerformanceConfigurationService configurationService;
        private readonly ServerConfig serverConfig;
        private Logger logger;
        private IIndexerManagerService indexerManager;
        private IServerService server;

        static IndexerManager()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public IndexerManager()
        {
            protectionService = new PerformanceProtectionService();
            configService = new PerformanceIndexerConfigurationService(protectionService);
            cacheService = new PerformanceCacheService();
            processService = new PerformanceProcessService();
            configurationService = new PerformanceConfigurationService();
            serverConfig = new ServerConfig(new RuntimeSettings());
            logger = LogManager.CreateNullLogger();
        }

        public void Init()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            Mapper.Initialize(cfg => { cfg.CreateMap<WebResult, WebResult>(); });
#pragma warning restore CS0618 // Type or member is obsolete

            var config = new NLog.Config.LoggingConfiguration();
            config.AddRule(LogLevel.Info, LogLevel.Fatal, new ConsoleTarget());
            LogManager.Configuration = config;
            logger = LogManager.GetCurrentClassLogger();

            var webClient = CreateWebClient();
            indexerManager = new IndexerManagerService(
                configService, protectionService, webClient, logger, cacheService, processService,
                configurationService, serverConfig);
            server = new ServerService(
                indexerManager, processService, new SerializeService(), configurationService, logger, webClient,
                new NullUpdateService(), protectionService, serverConfig);

            server.Initalize();
        }

        public List<IIndexer> GetIndexers()
        {
            return indexerManager.GetAllIndexers().Where(x => x.IsConfigured).ToList();
        }

        private void AddNativeIndexers(List<IIndexer> indexers)
        {
            var allTypes = typeof(IIndexer).Assembly.GetTypes();
            var allIndexerTypes = allTypes.Where(p => typeof(IIndexer).IsAssignableFrom(p));
            var allInstantiatableIndexerTypes = allIndexerTypes.Where(p => !p.IsInterface && !p.IsAbstract);
            var allNonMetaInstantiatableIndexerTypes =
                allInstantiatableIndexerTypes.Where(p => !typeof(BaseMetaIndexer).IsAssignableFrom(p));
            var indexerTypes = allNonMetaInstantiatableIndexerTypes.Where(p => p.Name != "CardigannIndexer");
            indexers.AddRange(indexerTypes.Select(
                                  type =>
                                  {
                                      var constructorArgumentTypes = new[]
                                      {
                                          typeof(IIndexerConfigurationService),
                                          typeof(WebClient),
                                          typeof(Logger),
                                          typeof(IProtectionService),
                                          typeof(ICacheService)
                                      };
                                      var constructor = type.GetConstructor(constructorArgumentTypes);
                                      if (constructor != null)
                                      {
                                          // create own webClient instance for each indexer (separate cookies stores, etc.)
                                          var indexerWebClientInstance = CreateWebClient();
                                          var arguments = new object[]
                                          {
                                              configService,
                                              indexerWebClientInstance,
                                              logger,
                                              protectionService,
                                              cacheService
                                          };
                                          var indexer = (IIndexer)constructor.Invoke(arguments);
                                          return indexer;
                                      }

                                      logger.Error($"Cannot instantiate Native indexer: {type.Name}");
                                      return null;
                                  }).Where(indexer => indexer?.Type == "public"));
        }

        private void AddCardigannIndexers(List<IIndexer> indexers, string applicationFolder)
        {
            var deserializer = new DeserializerBuilder()
                               .WithNamingConvention(CamelCaseNamingConvention.Instance)
                               //.IgnoreUnmatchedProperties()
                               .Build();
            var path = new[] { Path.Combine(applicationFolder, "Definitions") };
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
                    // create own webClient instance for each indexer (separate cookies stores, etc.)
                    var indexerWebClientInstance = CreateWebClient();

                    IIndexer indexer = new CardigannIndexer(configService, indexerWebClientInstance, logger, protectionService, cacheService, definition);
                    return indexer;
                }
                catch (Exception e)
                {
                    logger.Error($"Error while creating Cardigann instance from definition ID={definition.Id}: {e}");
                    return null;
                }
            }).Where(cardigannIndexer => cardigannIndexer?.Type == "public").ToList(); // Explicit conversion to list to avoid repeated resource loading

            var cardigannCounter = 0;
            var cardiganIds = new List<string>();
            foreach (var indexer in cardigannIndexers)
            {
                if (indexers.Any(x => x.Id == indexer.Id))
                {
                    logger.Warn($"Ignoring definition ID={indexer.Id}: Indexer already exists");
                    continue;
                }
                indexers.Add(indexer);

                cardigannCounter++;
                cardiganIds.Add(indexer.Id);
            }

            logger.Info($"Loaded {cardigannCounter} Cardigann indexers: {string.Join(", ", cardiganIds)}");
        }

        private PerformanceWebClient CreateWebClient() => new PerformanceWebClient(processService, logger, configurationService, serverConfig);

        public async Task SetupAsync(IIndexer indexer)
        {
            var configData = await indexer.GetConfigurationForSetup();
            indexer.LoadFromSavedConfiguration(configData.ToJson(protectionService));
        }
    }

    internal class NullUpdateService : IUpdateService
    {
        public void StartUpdateChecker() { }

        public void CheckForUpdatesNow() { }

        public void CleanupTempDir() { }

        public void CheckUpdaterLock() { }
    }
}
