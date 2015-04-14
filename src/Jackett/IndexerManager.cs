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
        static string AppConfigDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
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

            var configFilePath = Path.Combine(IndexerConfigDirectory, name.ToString().ToLower());
            if (File.Exists(configFilePath))
            {
                string jsonString = File.ReadAllText(configFilePath);
                newIndexer.LoadFromSavedConfiguration(jsonString);
            }

            Indexers.Add(name, newIndexer);
            return newIndexer;
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
