using Jackett.Common.Indexers;
using Newtonsoft.Json.Linq;

namespace Jackett.Common.Services.Interfaces
{
    public interface IIndexerConfigurationService
    {
        void Load(IIndexer indexer);
        void Save(IIndexer indexer, JToken config);
        void Delete(IIndexer indexer);
        string GetIndexerConfigFilePath(string indexerId);
    }
}
