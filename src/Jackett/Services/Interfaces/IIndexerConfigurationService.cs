using Jackett.Indexers;
using Newtonsoft.Json.Linq;

namespace Jackett.Services.Interfaces
{
    public interface IIndexerConfigurationService
    {
        void Load(IIndexer indexer);
        void Save(IIndexer indexer, JToken config);
        void Delete(IIndexer indexer);
    }
}
