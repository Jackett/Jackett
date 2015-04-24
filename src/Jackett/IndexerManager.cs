using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett
{
    public class IndexerManager
    {

        static string IndexerConfigDirectory = Path.Combine(Program.AppConfigDirectory, "Indexers");

        public Dictionary<string, IndexerInterface> Indexers { get; private set; }

        public IndexerManager()
        {
            Indexers = new Dictionary<string, IndexerInterface>();
            LoadMissingIndexers();
        }

        void LoadMissingIndexers()
        {
            var implementedIndexerTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(IndexerInterface).IsAssignableFrom(p) && !p.IsInterface)
                .ToArray();

            foreach (var t in implementedIndexerTypes)
            {
                LoadIndexer(t);
            }
        }

        void LoadIndexer(Type indexerType)
        {
            var name = indexerType.Name.Trim().ToLower();

            if (Indexers.ContainsKey(name))
                return;

            IndexerInterface newIndexer = (IndexerInterface)Activator.CreateInstance(indexerType);
            newIndexer.OnSaveConfigurationRequested += newIndexer_OnSaveConfigurationRequested;

            var configFilePath = GetIndexerConfigFilePath(newIndexer);
            if (File.Exists(configFilePath))
            {
                var jsonString = JObject.Parse(File.ReadAllText(configFilePath));
                newIndexer.LoadFromSavedConfiguration(jsonString);
            }

            Indexers.Add(name, newIndexer);
        }

        string GetIndexerConfigFilePath(IndexerInterface indexer)
        {
            return Path.Combine(IndexerConfigDirectory, indexer.GetType().Name.ToLower() + ".json");
        }

        void newIndexer_OnSaveConfigurationRequested(IndexerInterface indexer, JToken obj)
        {
            var configFilePath = GetIndexerConfigFilePath(indexer);
            if (!Directory.Exists(IndexerConfigDirectory))
                Directory.CreateDirectory(IndexerConfigDirectory);
            File.WriteAllText(configFilePath, obj.ToString());
        }

        public IndexerInterface GetIndexer(string name)
        {
            IndexerInterface indexer;
            if (!Indexers.TryGetValue(name, out indexer))
                throw new Exception(string.Format("No indexer with ID '{0}'", name));
            return indexer;
        }

        public void DeleteIndexer(string name)
        {
            var indexer = GetIndexer(name);
            var configPath = GetIndexerConfigFilePath(indexer);
            File.Delete(configPath);
            Indexers.Remove(name);
            LoadMissingIndexers();
        }

        public async Task TestIndexer(IndexerInterface indexer)
        {
            var browseQuery = new TorznabQuery();
            var results = await indexer.PerformQuery(browseQuery);
            Program.LoggerInstance.Debug(string.Format("Found {0} releases from {1}", results.Length, indexer.DisplayName));
            if (results.Length == 0)
                throw new Exception("Found no results while trying to browse this tracker");

        }

    }
}
