using System.Collections.Generic;
using Jackett.Common.Models;
using Jackett.Common.Utils.Clients;

namespace Jackett.Common.Indexers
{
    public class IndexerQueryResult
    {
        public IndexerQueryResult()
        {
            Releases = new List<ReleaseInfo>();
        }

        public IList<ReleaseInfo> Releases { get; set; }
        public WebResult Response { get; set; }
    }
}
