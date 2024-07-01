using System.Collections.Generic;
using Jackett.Common.Models;

namespace Jackett.Common.Indexers
{
    public interface IParseIndexerResponse
    {
        IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse);
    }
}
