using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Jackett.Common.Indexers;

namespace Jackett.Common.Models.DTO
{
    [DataContract]
    public class Capability
    {
        [DataMember]
        public string ID { get; set; }
        [DataMember]
        public string Name { get; set; }
    }

    [DataContract]
    public class Indexer
    {
        [DataMember]
        public string id { get; private set; }
        [DataMember]
        public string name { get; private set; }
        [DataMember]
        public string description { get; private set; }
        [DataMember]
        public string type { get; private set; }
        [DataMember]
        public bool configured { get; private set; }
        [DataMember]
        public string site_link { get; private set; }
        [DataMember]
        public IEnumerable<string> alternativesitelinks { get; private set; }
        [DataMember]
        public string language { get; private set; }
        [DataMember]
        public IEnumerable<string> tags { get; private set; }
        [DataMember]
        public string last_error { get; private set; }
        [DataMember]
        public bool potatoenabled { get; private set; }

        [DataMember]
        public IEnumerable<Capability> caps { get; private set; }

        public Indexer(IIndexer indexer)
        {
            id = indexer.Id;
            name = indexer.DisplayName;
            description = indexer.DisplayDescription;
            type = indexer.Type;
            configured = indexer.IsConfigured;
            site_link = indexer.SiteLink;
            language = indexer.Language;
            last_error = indexer.LastError ?? "";
            potatoenabled = indexer.TorznabCaps.Categories.GetTorznabCategoryTree().Any(i => TorznabCatType.Movies.Contains(i));

            alternativesitelinks = indexer.AlternativeSiteLinks;

            tags = indexer.Tags;

            caps = indexer.TorznabCaps.Categories.GetTorznabCategoryList(true)
                .Select(c => new Capability
                {
                    ID = c.ID.ToString(),
                    Name = c.Name
                });
        }
    }
}
