using System;
using System.Collections.Generic;
using System.Linq;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;

namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationData
    {
        private const string PASSWORD_REPLACEMENT = "|||%%PREVJACKPASSWD%%|||";
        protected Dictionary<string, Item> dynamics = new Dictionary<string, Item>(); // list for dynamic items

        public enum ItemType
        {
            InputString,
            InputBool,
            InputCheckbox,
            InputSelect,
            DisplayImage,
            DisplayInfo,
            HiddenData
        }

        public HiddenItem CookieHeader { get; private set; } = new HiddenItem { Name = "CookieHeader" };
        public HiddenItem LastError { get; private set; } = new HiddenItem { Name = "LastError" };
        public StringItem SiteLink { get; private set; } = new StringItem { Name = "Site Link" };

        public ConfigurationData()
        {

        }

        public void LoadConfigDataValuesFromJson(JToken json, IProtectionService ps = null)
        {
            if (json == null)
                return;

            var arr = (JArray)json;

            // transistion from alternatelink to sitelink
            var alternatelinkItem = arr.FirstOrDefault(f => f.Value<string>("id") == "alternatelink");
            if (alternatelinkItem != null && !string.IsNullOrEmpty(alternatelinkItem.Value<string>("value")))
            {
                //SiteLink.Value = alternatelinkItem.Value<string>("value");
            }

            foreach (var item in GetItems(forDisplay: false))
            {
                var arrItem = arr.FirstOrDefault(f => f.Value<string>("id") == item.ID);
                if (arrItem == null)
                    continue;

                switch (item.ItemType)
                {
                    case ItemType.InputString:
                        var sItem = (StringItem)item;
                        var newValue = arrItem.Value<string>("value");

                        if (string.Equals(item.Name, "password", StringComparison.InvariantCultureIgnoreCase))
                        {
                            if (newValue != PASSWORD_REPLACEMENT)
                            {
                                sItem.Value = newValue;
                                if (ps != null)
                                    sItem.Value = ps.UnProtect(newValue);
                            }
                        }
                        else
                        {
                            sItem.Value = newValue;
                        }
                        break;
                    case ItemType.HiddenData:
                        ((HiddenItem)item).Value = arrItem.Value<string>("value");
                        break;
                    case ItemType.InputBool:
                        ((BoolItem)item).Value = arrItem.Value<bool>("value");
                        break;
                    case ItemType.InputCheckbox:
                        var values = arrItem.Value<JArray>("values");
                        if (values != null)
                            ((CheckboxItem)item).Values = values.Values<string>().ToArray();
                        break;
                    case ItemType.InputSelect:
                        ((SelectItem)item).Value = arrItem.Value<string>("value");
                        break;
                }
            }
        }

        public JToken ToJson(IProtectionService ps, bool forDisplay = true)
        {
            var items = GetItems(forDisplay);
            var jArray = new JArray();
            foreach (var item in items)
            {
                var jObject = new JObject
                {
                    ["id"] = item.ID,
                    ["type"] = item.ItemType.ToString().ToLower(),
                    ["name"] = item.Name
                };
                switch (item.ItemType)
                {
                    case ItemType.InputString:
                    case ItemType.HiddenData:
                    case ItemType.DisplayInfo:
                        var value = ((StringItem)item).Value;
                        if (string.Equals(item.Name, "password", StringComparison.InvariantCultureIgnoreCase)) // if we chagne this logic we've to change the MigratedFromDPAPI() logic too, #2114 is realted
                        {
                            if (string.IsNullOrEmpty(value))
                                value = string.Empty;
                            else if (forDisplay)
                                value = PASSWORD_REPLACEMENT;
                            else if (ps != null)
                                value = ps.Protect(value);
                        }
                        jObject["value"] = value;
                        break;
                    case ItemType.InputBool:
                        jObject["value"] = ((BoolItem)item).Value;
                        break;
                    case ItemType.InputCheckbox:
                        jObject["values"] = new JArray(((CheckboxItem)item).Values);
                        jObject["options"] = new JObject();

                        foreach (var option in ((CheckboxItem)item).Options)
                        {
                            jObject["options"][option.Key] = option.Value;
                        }
                        break;
                    case ItemType.InputSelect:
                        jObject["value"] = ((SelectItem)item).Value;
                        jObject["options"] = new JObject();

                        foreach (var option in ((SelectItem)item).Options)
                        {
                            jObject["options"][option.Key] = option.Value;
                        }
                        break;
                    case ItemType.DisplayImage:
                        var dataUri = DataUrlUtils.BytesToDataUrl(((ImageItem)item).Value, "image/jpeg");
                        jObject["value"] = dataUri;
                        break;
                }
                jArray.Add(jObject);
            }
            return jArray;
        }

        private Item[] GetItems(bool forDisplay)
        {
            var properties = GetType()
                .GetProperties()
                .Where(p => p.CanRead)
                .Where(p => p.PropertyType.IsSubclassOf(typeof(Item)))
                .Where(p => p.GetValue(this) != null)
                .Select(p => (Item)p.GetValue(this)).ToList();

            // remove/insert Site Link manualy to make sure it shows up first
            properties.Remove(SiteLink);
            properties.Insert(0, SiteLink);

            properties.AddRange(dynamics.Values);

            if (!forDisplay)
            {
                properties = properties
                    .Where(p => p.ItemType == ItemType.HiddenData || p.ItemType == ItemType.InputBool || p.ItemType == ItemType.InputString || p.ItemType == ItemType.InputCheckbox || p.ItemType == ItemType.InputSelect || p.ItemType == ItemType.DisplayInfo)
                    .ToList();
            }

            return properties.ToArray();
        }

        public void AddDynamic(string ID, Item item) => dynamics[ID] = item;

        // TODO Convert to TryGetValue to avoid throwing exception
        public Item GetDynamic(string ID)
        {
            try
            {
                return dynamics[ID];
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        public Item GetDynamicByName(string Name)
            => dynamics.Values.FirstOrDefault(i => string.Equals(i.Name, Name, StringComparison.InvariantCultureIgnoreCase));

        public class Item
        {
            public ItemType ItemType { get; set; }
            public string Name { get; set; }
            public string ID => Name.Replace(" ", "").ToLower();
        }

        public class HiddenItem : StringItem
        {
            public HiddenItem(string value = "")
            {
                Value = value;
                ItemType = ItemType.HiddenData;
            }
        }

        public class DisplayItem : StringItem
        {
            public DisplayItem(string value)
            {
                Value = value;
                ItemType = ItemType.DisplayInfo;
            }
        }

        public class StringItem : Item
        {
            public string SiteKey { get; set; }
            public string Value { get; set; }
            public string Cookie { get; set; }
            public StringItem() => ItemType = ItemType.InputString;
        }

        public class BoolItem : Item
        {
            public bool Value { get; set; }
            public BoolItem() => ItemType = ItemType.InputBool;
        }

        public class ImageItem : Item
        {
            public byte[] Value { get; set; }
            public ImageItem() => ItemType = ItemType.DisplayImage;
        }

        public class CheckboxItem : Item
        {
            public string[] Values { get; set; }

            public Dictionary<string, string> Options { get; }

            public CheckboxItem(Dictionary<string, string> options)
            {
                ItemType = ItemType.InputCheckbox;
                Options = options;
            }
        }

        public class SelectItem : Item
        {
            public string Value { get; set; }

            public Dictionary<string, string> Options { get; }

            public SelectItem(Dictionary<string, string> options)
            {
                ItemType = ItemType.InputSelect;
                Options = options;
            }
        }

        //public abstract Item[] GetItems();
    }
}
