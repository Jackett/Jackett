using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Jackett.Common.Models
{
    public class TorznabCategory
    {
        public int ID { get; set; }
        public string Name { get; set; }

        public List<TorznabCategory> SubCategories { get; private set; }

        public TorznabCategory() => SubCategories = new List<TorznabCategory>();

        public TorznabCategory(int id, string name)
        {
            ID = id;
            Name = name;
            SubCategories = new List<TorznabCategory>();
        }

        public bool Contains(TorznabCategory cat) =>
            Equals(this, cat) || SubCategories.Contains(cat);

        public JToken ToJson() =>
            new JObject
            {
                ["ID"] = ID,
                ["Name"] = Name
            };

        public override bool Equals(object obj) => (obj as TorznabCategory)?.ID == ID;

        // Get Hash code should be calculated off read only properties.
        // ID is not readonly
        public override int GetHashCode() => ID;

        public TorznabCategory CopyWithoutSubCategories() => new TorznabCategory(ID, Name);
    }
}
