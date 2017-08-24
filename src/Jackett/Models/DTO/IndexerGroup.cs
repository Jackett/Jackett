using System;
using System.Collections.Generic;
using System.Linq;
using Jackett.Indexers.Meta;

namespace Jackett.Models.DTO
{
    public class IndexerGroup
    {
        public string id { get; set; }
        public IEnumerable<string> indexers { get; set; }

        public IndexerGroup(IndexerCollectionMetaIndexer indexerGroup)
        {
            id = indexerGroup.ID;
            indexers = indexerGroup.Indexers.Select(i => i.DisplayName);
        }
    }
}
