using System;
using Jackett.Common.Indexers;

namespace Jackett.Common
{
    public class IndexerException : Exception
    {
        public IIndexer Indexer { get; protected set; }

        public IndexerException(IIndexer indexer, string message, Exception innerException)
            : base(message, innerException)
        {
            this.Indexer = indexer;
        }

        public IndexerException(IIndexer indexer, string message)
            : this(indexer, message, null)
        {
        }

        public IndexerException(IIndexer indexer, Exception innerException)
            : this(indexer, "Exception (" + indexer.Id + "): " + innerException.GetBaseException().Message, innerException)
        {
        }
    }
}
