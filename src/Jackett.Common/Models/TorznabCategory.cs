using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Jackett.Common.Models
{
    public class TorznabCategory
    {
        public int ID { get; set; }
        public string Name { get; set; }

        public List<TorznabCategory> SubCategories { get; private set; }

        public TorznabCategory()
        {
            SubCategories = new List<TorznabCategory>();
        }

        public TorznabCategory(int id, string name)
        {
            ID = id;
            Name = name;
            SubCategories = new List<TorznabCategory>();
        }

        public bool Contains(TorznabCategory cat)
        {
            if (this == cat)
                return true;

            if (SubCategories.Contains(cat))
                return true;

            return false;
        }

        public JToken ToJson()
        {
            var t = new JObject();
            t["ID"] = ID;
            t["Name"] = Name;
            return t;
        }

        public override bool Equals(Object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            return ID == ((TorznabCategory)obj).ID;
        }

        public override int GetHashCode()
        {
            return ID;
        }
    }
}
