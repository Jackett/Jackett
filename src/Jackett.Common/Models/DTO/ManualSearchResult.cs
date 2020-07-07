using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Jackett.Common.Models.DTO
{
    [DataContract]
    public enum ManualSearchResultIndexerStatus { Unknown = 0, Error = 1, OK = 2 };

    [DataContract]
    public class ManualSearchResultIndexer
    {
        [DataMember]
        public string ID { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public ManualSearchResultIndexerStatus Status { get; set; }
        [DataMember]
        public int Results { get; set; }
        [DataMember]
        public string Error { get; set; }
    }

    [DataContract]
    public class ManualSearchResult
    {
        [DataMember]
        public IEnumerable<TrackerCacheResult> Results { get; set; }
        [DataMember]
        public IList<ManualSearchResultIndexer> Indexers { get; set; }
    }
}
