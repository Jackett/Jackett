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
        private const string PasswordReplacement = "|||%%PREVJACKPASSWD%%|||";
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

        public ConfigurationData(JToken json, IProtectionService ps) => LoadValuesFromJson(json, ps);

        public void LoadValuesFromJson(JToken json, IProtectionService ps = null)
        {
            if (json == null)
                return;
            var arr = (JArray)json;

            // transistion from alternatelink to sitelink
            var alternatelinkItem = arr.FirstOrDefault(f => f.Value<string>("id") == "alternatelink");
            if (!string.IsNullOrEmpty(alternatelinkItem?.Value<string>("value")))
            {
                //SiteLink.Value = alternatelinkItem.Value<string>("value");
            }

            foreach (var item in GetItems(false))
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
                            if (newValue != PasswordReplacement)
                            {
                                sItem.Value = newValue;
                                if (ps != null)
                                    sItem.Value = ps.UnProtect(newValue);
                            }
                        }
                        else
                            sItem.Value = newValue;

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
                var jObject = new JObject
                {
                    ["id"] = item.ID,
                    ["type"] = item.ItemType.ToString().ToLower(),
                    ["name"] = item.Name
                };
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
                        if (string.Equals(item.Name, "password", StringComparison.InvariantCultureIgnoreCase)
                            ) // if we chagne this logic we've to change the MigratedFromDPAPI() logic too, #2114 is realted
                        {
                            if (string.IsNullOrEmpty(value))
                                value = string.Empty;
                            else if (forDisplay)
                                value = PasswordReplacement;
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
                            jObject["options"][option.Key] = option.Value;
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
            var properties = GetType().GetProperties().Where(p => p.CanRead)
                                      .Where(p => p.PropertyType.IsSubclassOf(typeof(Item)))
                                      .Select(p => (Item)p.GetValue(this)).ToList();

            // remove/insert Site Link manualy to make sure it shows up first
            properties.Remove(SiteLink);
            properties.Insert(0, SiteLink);
            properties.AddRange(dynamics.Values);
            if (!forDisplay)
                properties = properties.Where(
                                           p => p.ItemType == ItemType.HiddenData || p.ItemType == ItemType.InputBool ||
                                                p.ItemType == ItemType.InputString || p.ItemType == ItemType.InputSelect ||
                                                p.ItemType == ItemType.Recaptcha || p.ItemType == ItemType.DisplayInfo)
                                       .ToList();
            return properties.ToArray();
        }

        public void AddDynamic(string id, Item item) => dynamics[id] = item;

        public Item GetDynamic(string id)
        {
            try
            {
                return dynamics[id];
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        public Item GetDynamicByName(string name) => dynamics
                                                     .Values.Where(
                                                         i => string.Equals(
                                                             i.Name, name, StringComparison.InvariantCultureIgnoreCase))
                                                     .FirstOrDefault();

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

        public class RecaptchaItem : StringItem
        {
            public string Version { get; set; }
            public string Challenge { get; set; }

            public RecaptchaItem()
            {
                Version = "2";
                ItemType = ItemType.Recaptcha;
            }
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
