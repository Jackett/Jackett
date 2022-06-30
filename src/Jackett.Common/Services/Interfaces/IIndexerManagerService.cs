using System.Collections.Generic;
using System.Threading.Tasks;
using Jackett.Common.Indexers;

namespace Jackett.Common.Services.Interfaces
{
    public interface IIndexerManagerService
    {
        Task TestIndexer(string name);
        void DeleteIndexer(string name);
        IIndexer GetIndexer(string name);
        IWebIndexer GetWebIndexer(string name);
        IEnumerable<IIndexer> GetAllIndexers();

        void InitIndexers(IEnumerable<string> path);
        void InitMetaIndexers();
    }
}
