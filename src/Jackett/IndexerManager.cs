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

        static string AppConfigDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Jackett");
        static string IndexerConfigDirectory = Path.Combine(AppConfigDirectory, "Indexers");

        public Dictionary<string, IndexerInterface> Indexers { get; private set; }

        public IndexerManager()
        {
            Indexers = new Dictionary<string, IndexerInterface>();

            var implementedIndexerTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(IndexerInterface).IsAssignableFrom(p) && !p.IsInterface)
                .ToArray();

            foreach (var t in implementedIndexerTypes)
            {
                LoadIndexer(t);
            }
        }

        IndexerInterface LoadIndexer(Type indexerType)
        {
            var name = indexerType.Name.Trim().ToLower();

            IndexerInterface newIndexer = (IndexerInterface)Activator.CreateInstance(indexerType);
            newIndexer.OnSaveConfigurationRequested += newIndexer_OnSaveConfigurationRequested;

            var configFilePath = GetIndexerConfigFilePath(newIndexer);
            if (File.Exists(configFilePath))
            {
                var jsonString = JObject.Parse(File.ReadAllText(configFilePath));
                newIndexer.LoadFromSavedConfiguration(jsonString);
            }

            Indexers.Add(name, newIndexer);
            return newIndexer;
        }

        string GetIndexerConfigFilePath(IndexerInterface indexer)
        {
            return Path.Combine(IndexerConfigDirectory, indexer.GetType().Name.ToLower() + ".json");
        }

        void newIndexer_OnSaveConfigurationRequested(IndexerInterface indexer, JToken obj)
        {
            var name = indexer.GetType().Name.Trim().ToLower();
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

    }
}
