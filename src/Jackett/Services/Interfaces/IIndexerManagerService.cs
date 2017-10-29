using Jackett.Indexers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jackett.Services.Interfaces
{
    public interface IIndexerManagerService
    {
        Task TestIndexer(string name);
        void DeleteIndexer(string name);
        IIndexer GetIndexer(string name);
        IWebIndexer GetWebIndexer(string name);
        IEnumerable<IIndexer> GetAllIndexers();

        void InitIndexers(IEnumerable<string> path);
        void InitAggregateIndexer();
    }
}
