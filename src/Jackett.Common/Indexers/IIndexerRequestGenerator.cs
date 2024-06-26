using Jackett.Common.Models;

namespace Jackett.Common.Indexers
{
    public interface IIndexerRequestGenerator
    {
        IndexerPageableRequestChain GetSearchRequests(TorznabQuery query);
    }
}
