using Autofac;
using Jackett.Indexers;
using Jackett.Models;
using Jackett.Utils;
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
        void TestIndexer(string name);
        void DeleteIndexer(string name);
        IIndexer GetIndexer(string name);
        IEnumerable<IIndexer> GetAllIndexers();
        void SaveConfig(IIndexer indexer, JToken obj);
        void InitIndexers();
    }

    public class IndexerManagerService : IIndexerManagerService
    {
        private IContainer container;
        private IConfigurationService configService;
        private Logger logger;
        private Dictionary<string, IIndexer> indexers = new Dictionary<string, IIndexer>();

        public IndexerManagerService(IContainer c, IConfigurationService config, Logger l)
        {
            container = c;
            configService = config;
            logger = l;
        }

        public void InitIndexers()
        {
            foreach (var idx in container.Resolve<IEnumerable<IIndexer>>().OrderBy(_ => _.DisplayName))
            {
                indexers.Add(idx.DisplayName, idx);
                var configFilePath = GetIndexerConfigFilePath(idx);
                if (File.Exists(configFilePath))
                {
                    var jsonString = JObject.Parse(File.ReadAllText(configFilePath));
                    idx.LoadFromSavedConfiguration(jsonString);
                }
            }
        }

        public IIndexer GetIndexer(string name)
        {
            var indexer = indexers.Values.Where(i => string.Equals(StringUtil.StripNonAlphaNumeric(i.DisplayName), name, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (indexer != null)
            {
                return indexer;
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

        public async void TestIndexer(string name)
        {
            var indexer = GetIndexer(name);
            var browseQuery = new TorznabQuery();
            var results = await indexer.PerformQuery(browseQuery);
            logger.Debug(string.Format("Found {0} releases from {1}", results.Length, indexer.DisplayName));
            if (results.Length == 0)
                throw new Exception("Found no results while trying to browse this tracker");
        }

        public void DeleteIndexer(string name)
        {
            var indexer = GetIndexer(name);
            var configPath = GetIndexerConfigFilePath(indexer);
            File.Delete(configPath);
            indexers[name] = container.ResolveNamed<IIndexer>(name);
        }

        private string GetIndexerConfigFilePath(IIndexer indexer)
        {
            return Path.Combine(configService.GetIndexerConfigDir(), indexer.GetType().Name.ToLower() + ".json");
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
