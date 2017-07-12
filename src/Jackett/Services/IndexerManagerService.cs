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
        void InitCardigannIndexers(IEnumerable<string> path);
        void InitAggregateIndexer();
    }

    public class IndexerManagerService : IIndexerManagerService
    {
        private ICacheService cacheService;
        private IIndexerConfigurationService configService;
        private IProtectionService protectionService;
        private IWebClient webClient;

        private Logger logger;

        private Dictionary<string, IIndexer> indexers = new Dictionary<string, IIndexer>();
        private AggregateIndexer aggregateIndexer;

        public IndexerManagerService(IIndexerConfigurationService config, IProtectionService protectionService, IWebClient webClient, Logger l, ICacheService cache)
        {
            configService = config;
            this.protectionService = protectionService;
            this.webClient = webClient;
            logger = l;
            cacheService = cache;
        }

        public void InitIndexers()
        {
            logger.Info("Using HTTP Client: " + webClient.GetType().Name);

            var ixs = new IIndexer[]{
                new AlphaRatio(configService, webClient, logger, protectionService),
                new Andraste(configService, webClient, logger, protectionService),
                new AnimeTorrents(configService, (HttpWebClient)webClient, logger, protectionService),
                new ArcheTorrent(configService, webClient, logger, protectionService),
                new BB(configService, webClient, logger, protectionService),
                new BJShare(configService, webClient, logger, protectionService),
                new BakaBT(configService, webClient, logger, protectionService),
                new BestFriends(configService, webClient, logger, protectionService),
                new BeyondHD(configService, webClient, logger, protectionService),
                new BitCityReloaded(configService, webClient, logger, protectionService),
                new BitHdtv(configService, webClient, logger, protectionService),
                new BitMeTV(configService, webClient, logger, protectionService),
                new BitSoup(configService, webClient, logger, protectionService),
                new BroadcastTheNet(configService, webClient, logger, protectionService),
                new DanishBits(configService, webClient, logger, protectionService),
                new Demonoid(configService, webClient, logger, protectionService),
                new DigitalHive(configService, webClient, logger, protectionService),
                new EliteTracker(configService, webClient, logger, protectionService),
                new FileList(configService, webClient, logger, protectionService),
                new FunFile(configService, webClient, logger, protectionService),
                new Fuzer(configService, webClient, logger, protectionService),
                new GFTracker(configService, webClient, logger, protectionService),
                new GhostCity(configService, webClient, logger, protectionService),
                new GimmePeers(configService, webClient, logger, protectionService),
                new HD4Free(configService, webClient, logger, protectionService),
                new HDSpace(configService, webClient, logger, protectionService),
                new HDTorrents(configService, webClient, logger, protectionService),
                new Hardbay(configService, webClient, logger, protectionService),
                new Hebits(configService, webClient, logger, protectionService),
                new Hounddawgs(configService, webClient, logger, protectionService),
                new HouseOfTorrents(configService, webClient, logger, protectionService),
                new IPTorrents(configService, webClient, logger, protectionService),
                new ImmortalSeed(configService, webClient, logger, protectionService),
                new MoreThanTV(configService, webClient, logger, protectionService),
                new Myanonamouse(configService, webClient, logger, protectionService),
                new NCore(configService, webClient, logger, protectionService),
                new NewRealWorld(configService, webClient, logger, protectionService),
                new PassThePopcorn(configService, webClient, logger, protectionService),
                new PiXELHD(configService, webClient, logger, protectionService),
                new PirateTheNet(configService, webClient, logger, protectionService),
                new Pretome(configService, webClient, logger, protectionService),
                new Rarbg(configService, webClient, logger, protectionService),
                new RevolutionTT(configService, webClient, logger, protectionService),
                new RuTracker(configService, webClient, logger, protectionService),
                new SceneAccess(configService, webClient, logger, protectionService),
                new SceneFZ(configService, webClient, logger, protectionService),
                new SceneTime(configService, webClient, logger, protectionService),
                new SevenTor(configService, webClient, logger, protectionService),
                new Shazbat(configService, webClient, logger, protectionService),
                new ShowRSS(configService, webClient, logger, protectionService),
                new SpeedCD(configService, webClient, logger, protectionService),
                new Superbits(configService, webClient, logger, protectionService),
                new T411(configService, webClient, logger, protectionService),
                new TVChaosUK(configService, webClient, logger, protectionService),
                new TVVault(configService, webClient, logger, protectionService),
                new TehConnection(configService, webClient, logger, protectionService),
                new TorrentBytes(configService, webClient, logger, protectionService),
                new TorrentDay(configService, webClient, logger, protectionService),
                new TorrentHeaven(configService, webClient, logger, protectionService),
                new TorrentLeech(configService, webClient, logger, protectionService),
                new TorrentNetwork(configService, webClient, logger, protectionService),
                new TorrentSyndikat(configService, webClient, logger, protectionService),
                new Torrentech(configService, webClient, logger, protectionService),
                new TransmitheNet(configService, webClient, logger, protectionService),
                new Trezzor(configService, webClient, logger, protectionService),
                new XSpeeds(configService, webClient, logger, protectionService),
                new myAmity(configService, webClient, logger, protectionService),
                new x264(configService, webClient, logger, protectionService)
            };

            foreach (var idx in ixs)
            {
                indexers.Add(idx.ID, idx);
                configService.Load(idx);
            }
        }

        public void InitCardigannIndexers(IEnumerable<string> path)
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
                    IIndexer indexer = new CardigannIndexer(configService, webClient, logger, protectionService, definition);
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
            if (omdbApiKey != null)
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
