using System.Collections.Generic;
using System.Linq;
using Jackett.Controllers.V20;
using Jackett.Indexers;

namespace Jackett.Models.DTO
{
    public class Capability
    {
        public string ID { get; set; }
        public string Name { get; set; }
    }

    public class Indexer
    {
        public string id { get; private set; }
        public string name { get; private set; }
        public string description { get; private set; }
        public string type { get; private set; }
        public bool configured { get; private set; }
        public string site_link { get; private set; }
        public IEnumerable<string> alternativesitelinks { get; private set; }
        public string language { get; private set; }
        public string last_error { get; private set; }
        public bool potatoenabled { get; private set; }
        public IEnumerable<Capability> caps { get; private set; }

        public Indexer(IIndexer indexer)
        {
            id = indexer.ID;
            name = indexer.DisplayName;
            description = indexer.DisplayDescription;
            type = indexer.Type;
            configured = indexer.IsConfigured;
            site_link = indexer.SiteLink;
            language = indexer.Language;
            last_error = indexer.LastError;
            potatoenabled = indexer.TorznabCaps.Categories.Any(i => TorznabCatType.Movies.Contains(i));

            alternativesitelinks = indexer.AlternativeSiteLinks;

            caps = indexer.TorznabCaps.Categories.Select(c => new Capability
            {
                ID = c.ID.ToString(),
                Name = c.Name
            });
        }
    }
}