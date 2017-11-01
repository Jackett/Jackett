using Jackett.Indexers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett
{
    class IndexerException : Exception
    {
        public IIndexer Indexer { get; protected set; }

        public IndexerException(IIndexer Indexer, string message, Exception innerException)
            : base(message, innerException)
        {
            this.Indexer = Indexer;
        }

        public IndexerException(IIndexer Indexer, string message)
            : this(Indexer, message, null)
        {
        }

        public IndexerException(IIndexer Indexer, Exception innerException)
            : this(Indexer, "Exception (" + Indexer.ID + "): " + innerException.Message, innerException)
        {
        }
    }
}
