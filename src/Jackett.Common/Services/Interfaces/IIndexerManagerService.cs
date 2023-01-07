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
        List<IIndexer> GetAllIndexers();

        void InitIndexers(List<string> path);
        void InitMetaIndexers();
    }
}
