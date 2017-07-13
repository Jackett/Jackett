using Jackett.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jackett.Indexers;
using Newtonsoft.Json.Linq;

namespace JackettTest
{
    class TestIndexerManagerServiceHelper : IIndexerManagerService
    {
        public JToken LastSavedConfig { get; set; }

        public void DeleteIndexer(string name)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IIndexer> GetAllIndexers()
        {
            throw new NotImplementedException();
        }

        public IIndexer GetIndexer(string name)
        {
            throw new NotImplementedException();
        }

        public IWebIndexer GetWebIndexer(string name)
        {
            throw new NotImplementedException();
        }

        public void InitIndexers(IEnumerable<string> path)
        {
            throw new NotImplementedException();
        }

        public void SaveConfig(IIndexer indexer, JToken obj)
        {
            LastSavedConfig = obj;
        }

        public Task TestIndexer(string name)
        {
            throw new NotImplementedException();
        }

        public void InitAggregateIndexer()
        {
            throw new NotImplementedException();
        }
    }
}
