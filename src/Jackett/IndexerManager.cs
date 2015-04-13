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

        Dictionary<string, IndexerInterface> loadedIndexers;

        Dictionary<string, Type> implementedIndexerTypes;

        public IndexerManager()
        {
            loadedIndexers = new Dictionary<string, IndexerInterface>();

            implementedIndexerTypes = (AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(IndexerInterface).IsAssignableFrom(p)))
                .ToDictionary(t => t.Name.ToLower());


            // TODO: initialize all indexers at start, read all saved config json files then fill their indexers
        }

        IndexerInterface LoadIndexer(string name)
        {
            name = name.Trim().ToLower();

            Type indexerType;
            if (!implementedIndexerTypes.TryGetValue(name, out indexerType))
                throw new Exception(string.Format("No indexer of type '{0}'", name));

            IndexerInterface newIndexer = (IndexerInterface)Activator.CreateInstance(indexerType);

            var configFilePath = Path.Combine(IndexerConfigDirectory, name.ToString().ToLower());
            if (File.Exists(configFilePath))
            {
                string jsonString = File.ReadAllText(configFilePath);
                newIndexer.LoadFromSavedConfiguration(jsonString);
            }

            loadedIndexers.Add(name, newIndexer);
            return newIndexer;
        }

        public IndexerInterface GetIndexer(string name)
        {
            IndexerInterface indexer;
            if (!loadedIndexers.TryGetValue(name, out indexer))
                indexer = LoadIndexer(name);
            return indexer;
        }

    }
}
