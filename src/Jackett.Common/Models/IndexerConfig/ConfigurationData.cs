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
        const string PASSWORD_REPLACEMENT = "|||%%PREVJACKPASSWD%%|||";
        protected Dictionary<string, Item> dynamics = new Dictionary<string, Item>(); // list for dynamic items

        public enum ItemType
        {
            InputString,
            InputBool,
            InputSelect,
            DisplayImage,
            DisplayInfo,
            HiddenData,
            Recaptcha
        }

        public HiddenItem CookieHeader { get; private set; } = new HiddenItem { Name = "CookieHeader" };
        public HiddenItem LastError { get; private set; } = new HiddenItem { Name = "LastError" };
        public StringItem SiteLink { get; private set; } = new StringItem { Name = "Site Link" };

        public ConfigurationData()
        {

        }

        public ConfigurationData(JToken json, IProtectionService ps)
        {
            LoadValuesFromJson(json, ps);
        }

        public void LoadValuesFromJson(JToken json, IProtectionService ps= null)
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
                    case ItemType.InputSelect:
                        ((SelectItem)item).Value = arrItem.Value<string>("value");
                        break;
                    case ItemType.Recaptcha:
                        ((RecaptchaItem)item).Value = arrItem.Value<string>("value");
                        ((RecaptchaItem)item).Cookie = arrItem.Value<string>("cookie");
                        ((RecaptchaItem)item).Version = arrItem.Value<string>("version");
                        ((RecaptchaItem)item).Challenge = arrItem.Value<string>("challenge");
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
                var jObject = new JObject();
                jObject["id"] = item.ID;
                jObject["type"] = item.ItemType.ToString().ToLower();
                jObject["name"] = item.Name;
                switch (item.ItemType)
                {
                    case ItemType.Recaptcha:
                        jObject["sitekey"] = ((RecaptchaItem)item).SiteKey;
                        jObject["version"] = ((RecaptchaItem)item).Version;
                        break;
                    case ItemType.InputString:
                    case ItemType.HiddenData:
                    case ItemType.DisplayInfo:
                        var value = ((StringItem)item).Value;
                        if (string.Equals(item.Name, "password", StringComparison.InvariantCultureIgnoreCase))
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
                    case ItemType.InputSelect:
                        jObject["value"] = ((SelectItem)item).Value;
                        jObject["options"] = new JObject();

                        foreach (var option in ((SelectItem)item).Options)
                        {
                            jObject["options"][option.Key] = option.Value;
                        }
                        break;
                    case ItemType.DisplayImage:
                        string dataUri = DataUrlUtils.BytesToDataUrl(((ImageItem)item).Value, "image/jpeg");
                        jObject["value"] = dataUri;
                        break;
                }
                jArray.Add(jObject);
            }
            return jArray;
        }

        Item[] GetItems(bool forDisplay)
        {
            List<Item> properties = GetType()
                .GetProperties()
                .Where(p => p.CanRead)
                .Where(p => p.PropertyType.IsSubclassOf(typeof(Item)))
                .Select(p => (Item)p.GetValue(this)).ToList();

            // remove/insert Site Link manualy to make sure it shows up first
            properties.Remove(SiteLink);
            properties.Insert(0, SiteLink);

            properties.AddRange(dynamics.Values);

            if (!forDisplay)
            {
                properties = properties
                    .Where(p => p.ItemType == ItemType.HiddenData || p.ItemType == ItemType.InputBool || p.ItemType == ItemType.InputString || p.ItemType == ItemType.InputSelect || p.ItemType == ItemType.Recaptcha || p.ItemType == ItemType.DisplayInfo)
                    .ToList();
            }

            return properties.ToArray();
        }

        public void AddDynamic(string ID, Item item)
        {
            dynamics[ID] = item;
        }

        public Item GetDynamic(string ID)
        {
            try
            {
                return dynamics[ID];
            }
            catch(KeyNotFoundException)
            {
                return null;
            }
        }

        public class Item
        {
            public ItemType ItemType { get; set; }
            public string Name { get; set; }
            public string ID { get { return Name.Replace(" ", "").ToLower(); } }
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
            public StringItem()
            {
                ItemType = ConfigurationData.ItemType.InputString;
            }
        }

        public class RecaptchaItem : StringItem
        {
            public string Version { get; set; }
            public string Challenge { get; set; }
            public RecaptchaItem()
            {
                this.Version = "2";
                ItemType = ConfigurationData.ItemType.Recaptcha;
            }
        }

        public class BoolItem : Item
        {
            public bool Value { get; set; }
            public BoolItem()
            {
                ItemType = ConfigurationData.ItemType.InputBool;
            }
        }

        public class ImageItem : Item
        {
            public byte[] Value { get; set; }
            public ImageItem()
            {
                ItemType = ConfigurationData.ItemType.DisplayImage;
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
