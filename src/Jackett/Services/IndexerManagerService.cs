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
        void SortIndexers();
    }

    public class IndexerManagerService : IIndexerManagerService
    {
        private static readonly object configWriteLock = new object();

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
                try
                {
                    var fileStr = File.ReadAllText(configFilePath);
                    var jsonString = JToken.Parse(fileStr);
                    idx.LoadFromSavedConfiguration(jsonString);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed loading configuration for {0}, trying backup", idx.DisplayName);
                    var configFilePathBak = configFilePath + ".bak";
                    if (File.Exists(configFilePathBak))
                    {
                        try
                        {
                            var fileStrBak = File.ReadAllText(configFilePathBak);
                            var jsonStringBak = JToken.Parse(fileStrBak);
                            idx.LoadFromSavedConfiguration(jsonStringBak);
                            logger.Info("Successfully loaded backup config for {0}", idx.DisplayName);
                            idx.SaveConfig();
                        }
                        catch (Exception exbak)
                        {
                            logger.Error(exbak, "Failed loading backup configuration for {0}, you must reconfigure this indexer", idx.DisplayName);
                        }
                    }
                    else
                    {
                        logger.Error(ex, "Failed loading backup configuration for {0} (no backup available), you must reconfigure this indexer", idx.DisplayName);
                    }
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

            try
            {
                if (!Directory.Exists(path))
                    return;

                DirectoryInfo d = new DirectoryInfo(path);

                foreach (var file in d.GetFiles("*.yml"))
                {
                    logger.Info("Loading Cardigann definition " + file.FullName);
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
            catch (Exception ex)
            {
                logger.Error(ex, "Error while loading Cardigann definitions: "+ ex.Message);
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
            browseQuery.IsTest = true;
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
            if (indexer.GetType() == typeof(CardigannIndexer))
            {
                indexers[name] = new CardigannIndexer(this, container.Resolve<IWebClient>(), logger, container.Resolve<IProtectionService>(), ((CardigannIndexer)indexer).DefinitionString);
            }
            else
            {
                indexers[name] = container.ResolveNamed<IIndexer>(indexer.ID);
            }
        }

        private string GetIndexerConfigFilePath(IIndexer indexer)
        {
            return Path.Combine(configService.GetIndexerConfigDir(), indexer.ID + ".json");
        }

        public void SaveConfig(IIndexer indexer, JToken obj)
        {
            lock (configWriteLock)
            { 
                var uID = Guid.NewGuid().ToString("N");
                var configFilePath = GetIndexerConfigFilePath(indexer);
                var configFilePathBak = configFilePath + ".bak";
                var configFilePathTmp = configFilePath + "." + uID + ".tmp";
                var content = obj.ToString();

                logger.Debug(string.Format("Saving new config file: {0}", configFilePathTmp));

                if (string.IsNullOrWhiteSpace(content))
                {
                    throw new Exception(string.Format("New config content for {0} is empty, please report this bug.", indexer.ID));
                }

                if (content.Contains("\x00"))
                {
                    throw new Exception(string.Format("New config content for {0} contains 0x00, please report this bug. Content: {1}", indexer.ID, content));
                }

                // make sure the config directory exists
                if (!Directory.Exists(configService.GetIndexerConfigDir()))
                    Directory.CreateDirectory(configService.GetIndexerConfigDir());

                // create new temporary config file
                File.WriteAllText(configFilePathTmp, content);
                var fileInfo = new FileInfo(configFilePathTmp);
                if (fileInfo.Length == 0)
                {
                    throw new Exception(string.Format("New config file {0} is empty, please report this bug.", configFilePathTmp));
                }

                // create backup file
                File.Delete(configFilePathBak);
                if (File.Exists(configFilePath))
                {
                    try
                    {
                        File.Move(configFilePath, configFilePathBak);
                    }
                    catch (IOException ex)
                    {
                        logger.Error(string.Format("Error while moving {0} to {1}: {2}", configFilePath, configFilePathBak, ex.ToString()));
                    }
                }

                // replace the actual config file
                File.Delete(configFilePath);
                try
                {
                    File.Move(configFilePathTmp, configFilePath);
                }
                catch (IOException ex)
                {
                    logger.Error(string.Format("Error while moving {0} to {1}: {2}", configFilePathTmp, configFilePath, ex.ToString()));
                }
            }
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
