using System;
using Jackett.Common.Indexers;

namespace Jackett.Common
{
    public class IndexerException : Exception
    {
        public IIndexer Indexer { get; protected set; }

        public IndexerException(IIndexer Indexer, string message, Exception innerException)
            : base(message, innerException)
            => this.Indexer = Indexer;

        public IndexerException(IIndexer Indexer, string message)
            : this(Indexer, message, null)
        {
        }

        public IndexerException(IIndexer Indexer, Exception innerException)
            : this(Indexer, "Exception (" + Indexer.Id + "): " + innerException.GetBaseException().Message, innerException)
        {
        }
    }
}
