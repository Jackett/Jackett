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


        enum IndexerName
        {
            BitMeTV,
            Freshon,
            IPTorrents,
            BaconBits,
        }

        Dictionary<string, IndexerInterface> loadedIndexers;

        public IndexerManager()
        {
            loadedIndexers = new Dictionary<string, IndexerInterface>();
        }

        IndexerInterface LoadIndexer(string name)
        {
            IndexerInterface newIndexer;

            IndexerName indexerName;

            try
            {
                indexerName = (IndexerName)Enum.Parse(typeof(IndexerName), name, true);
            }
            catch (Exception)
            {
                throw new ArgumentException(string.Format("Unsupported indexer '{0}'", name));
            }

            switch (indexerName)
            {
                case IndexerName.BitMeTV:
                    newIndexer = new BitMeTV();
                    break;
                case IndexerName.Freshon:
                    newIndexer = new Freshon();
                    break;
                default:
                    throw new ArgumentException(string.Format("Unsupported indexer '{0}'", name));
            }


            var configFilePath = Path.Combine(IndexerConfigDirectory, indexerName.ToString().ToLower());
            if (File.Exists(configFilePath))
            {
                string jsonString = File.ReadAllText(configFilePath);
                newIndexer.LoadFromSavedConfiguration(jsonString);
            }

            //newIndexer.VerifyConnection();
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
