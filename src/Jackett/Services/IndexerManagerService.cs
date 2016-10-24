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
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Services
{
    public interface IIndexerManagerService
    {
        Task TestIndexer(string name);
        void DeleteIndexer(string name);
        IIndexer GetIndexer(string name);
        IEnumerable<IIndexer> GetAllIndexers();
        void SaveConfig(IIndexer indexer, JToken obj);
        void InitIndexers();
        void InitCardigannIndexers(string path);
    }

    public class IndexerManagerService : IIndexerManagerService
    {
        private IContainer container;
        private IConfigurationService configService;
        private Logger logger;
        private Dictionary<string, IIndexer> indexers = new Dictionary<string, IIndexer>();
        private ICacheService cacheService;

        public IndexerManagerService(IContainer c, IConfigurationService config, Logger l, ICacheService cache)
        {
            container = c;
            configService = config;
            logger = l;
            cacheService = cache;
        }

        protected void LoadIndexerConfig(IIndexer idx)
        {
            var configFilePath = GetIndexerConfigFilePath(idx);
            if (File.Exists(configFilePath))
            {
                var fileStr = File.ReadAllText(configFilePath);
                var jsonString = JToken.Parse(fileStr);
                try
                {
                    idx.LoadFromSavedConfiguration(jsonString);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed loading configuration for {0}, you must reconfigure this indexer", idx.DisplayName);
                }
            }
        }

        public void InitIndexers()
        {
            logger.Info("Using HTTP Client: " + container.Resolve<IWebClient>().GetType().Name);

            foreach (var idx in container.Resolve<IEnumerable<IIndexer>>().Where(p => p.ID != "cardigannindexer").OrderBy(_ => _.DisplayName))
            {
                indexers.Add(idx.ID, idx);
                LoadIndexerConfig(idx);
            }
        }

        public void InitCardigannIndexers(string path)
        {
            logger.Info("Loading Cardigann definitions from: " + path);

            DirectoryInfo d = new DirectoryInfo(path);

            foreach (var file in d.GetFiles("*.yml"))
            {
                string DefinitionString = File.ReadAllText(file.FullName);
                CardigannIndexer idx = new CardigannIndexer(this, container.Resolve<IWebClient>(), logger, container.Resolve<IProtectionService>(), DefinitionString);
                if (indexers.ContainsKey(idx.ID))
                {
                    logger.Debug(string.Format("Ignoring definition ID={0}, file={1}: Indexer already exists", idx.ID, file.FullName));
                }
                else
                { 
                    indexers.Add(idx.ID, idx);
                    LoadIndexerConfig(idx);
                }
            }
        }

        public IIndexer GetIndexer(string name)
        {
            if (indexers.ContainsKey(name))
            {
                return indexers[name];
            }
            else
            {
                logger.Error("Request for unknown indexer: " + name);
                throw new Exception("Unknown indexer: " + name);
            }
        }

        public IEnumerable<IIndexer> GetAllIndexers()
        {
            return indexers.Values;
        }

        public async Task TestIndexer(string name)
        {
            var indexer = GetIndexer(name);
            var browseQuery = new TorznabQuery();
            var results = await indexer.PerformQuery(browseQuery);
            results = indexer.CleanLinks(results);
            logger.Info(string.Format("Found {0} releases from {1}", results.Count(), indexer.DisplayName));
            if (results.Count() == 0)
                throw new Exception("Found no results while trying to browse this tracker");
            cacheService.CacheRssResults(indexer, results);
        }

        public void DeleteIndexer(string name)
        {
            var indexer = GetIndexer(name);
            var configPath = GetIndexerConfigFilePath(indexer);
            File.Delete(configPath);
            indexers[name] = container.ResolveNamed<IIndexer>(indexer.ID);
        }

        private string GetIndexerConfigFilePath(IIndexer indexer)
        {
            return Path.Combine(configService.GetIndexerConfigDir(), indexer.ID + ".json");
        }

        public void SaveConfig(IIndexer indexer, JToken obj)
        {
            var configFilePath = GetIndexerConfigFilePath(indexer);
            if (!Directory.Exists(configService.GetIndexerConfigDir()))
                Directory.CreateDirectory(configService.GetIndexerConfigDir());
            File.WriteAllText(configFilePath, obj.ToString());
        }
    }
}
