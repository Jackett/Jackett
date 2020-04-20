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
        private const string _PasswordReplacement = "|||%%PREVJACKPASSWD%%|||";
        protected readonly Dictionary<string, Item> _dynamics = new Dictionary<string, Item>(); // list for dynamic items
        public ConfigurationData() {}

        public ConfigurationData(JToken json, IProtectionService ps) => LoadValuesFromJson(json, ps);

        public HiddenItem CookieHeader { get; } = new HiddenItem {Name = "CookieHeader"};
        public HiddenItem LastError { get; } = new HiddenItem {Name = "LastError"};
        public StringItem SiteLink { get; } = new StringItem {Name = "Site Link"};

        public void AddDynamic(string id, Item item) => _dynamics[id] = item;

        public Item GetDynamic(string id)
        {
            _dynamics.TryGetValue(id, out var item);
            return item;
        }

        public Item GetDynamicByName(string name) =>
            _dynamics.Values.FirstOrDefault(i => string.Equals(i.Name, name, StringComparison.InvariantCultureIgnoreCase));

        private Item[] GetItems(bool forDisplay)
        {
            var properties = GetType().GetProperties().Where(p => p.CanRead)
                                      .Where(p => p.PropertyType.IsSubclassOf(typeof(Item)))
                                      .Select(p => (Item)p.GetValue(this)).ToList();

            // remove/insert Site Link manualy to make sure it shows up first
            properties.Remove(SiteLink);
            properties.Insert(0, SiteLink);
            properties.AddRange(_dynamics.Values);
            if (!forDisplay)
                properties.RemoveAll(property => property is ImageItem);
            return properties.ToArray();
        }

        public void LoadValuesFromJson(JToken json, IProtectionService ps = null)
        {
            if (json == null)
                return;
            var arr = (JArray)json;
            foreach (var item in GetItems(false))
            {
                var arrItem = arr.FirstOrDefault(f => f.Value<string>("id") == item.ID);
                if (arrItem == null)
                    continue;
                switch (item)
                {
                    case HiddenItem hiddenItem:
                        hiddenItem.Value = arrItem.Value<string>("value");
                        break;
                    case BoolItem boolItem:
                        boolItem.Value = arrItem.Value<bool>("value");
                        break;
                    case CheckboxItem checkboxItem:
                        var values = arrItem.Value<JArray>("values");
                        if (values != null)
                            checkboxItem.Values = values.Values<string>().ToArray();
                        break;
                    case SelectItem selectItem:
                        selectItem.Value = arrItem.Value<string>("value");
                        break;
                    case RecaptchaItem recaptcha:
                        recaptcha.Value = arrItem.Value<string>("value");
                        recaptcha.Cookie = arrItem.Value<string>("cookie");
                        recaptcha.Version = arrItem.Value<string>("version");
                        recaptcha.Challenge = arrItem.Value<string>("challenge");
                        break;
                    case StringItem sItem:
                        var newValue = arrItem.Value<string>("value");
                        if (string.Equals(item.Name, "password", StringComparison.InvariantCultureIgnoreCase))
                        {
                            if (newValue != _PasswordReplacement)
                            {
                                sItem.Value = newValue;
                                if (ps != null)
                                    sItem.Value = ps.UnProtect(newValue);
                            }
                        }
                        else
                            sItem.Value = newValue;

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
                    ["name"] = item.Name
                };
                switch (item)
                {
                    case RecaptchaItem recaptcha:
                        jObject["sitekey"] = recaptcha.SiteKey;
                        jObject["version"] = recaptcha.Version;
                        break;
                    case StringItem stringItem:
                        var value = stringItem.Value;
                        // if we change this logic we've to change the MigratedFromDPAPI() logic too, #2114 is realted
                        if (string.Equals(stringItem.Name, "password", StringComparison.InvariantCultureIgnoreCase))
                        {
                            if (string.IsNullOrEmpty(value))
                                value = string.Empty;
                            else if (forDisplay)
                                value = _PasswordReplacement;
                            else if (ps != null)
                                value = ps.Protect(value);
                        }

                        jObject["value"] = value;
                        break;
                    case BoolItem boolItem:
                        jObject["value"] = boolItem.Value;
                        break;
                    case CheckboxItem checkboxItem:
                        jObject["values"] = new JArray(checkboxItem.Values);
                        jObject["options"] = new JObject();
                        foreach (var option in checkboxItem.Options)
                            jObject["options"][option.Key] = option.Value;
                        break;
                    case SelectItem selectItem:
                        jObject["value"] = selectItem.Value;
                        jObject["options"] = new JObject();
                        foreach (var option in selectItem.Options)
                            jObject["options"][option.Key] = option.Value;
                        break;
                    case ImageItem imageItem:
                        var dataUri = DataUrlUtils.BytesToDataUrl(imageItem.Value, "image/jpeg");
                        jObject["value"] = dataUri;
                        break;
                }

                jArray.Add(jObject);
            }

            return jArray;
        }

        public class BoolItem : Item
        {
            public bool Value { get; set; }
        }

        public class CheckboxItem : Item
        {
            public CheckboxItem(Dictionary<string, string> options) => Options = options;

            public Dictionary<string, string> Options { get; }
            public string[] Values { get; set; }
        }

        public class DisplayItem : StringItem
        {
            public DisplayItem(string value) => Value = value;
        }

        public class HiddenItem : StringItem
        {
            public HiddenItem(string value = "") => Value = value;
        }

        public class ImageItem : Item
        {
            public byte[] Value { get; set; }
        }

        public class Item
        {
            public string ID => Name.Replace(" ", "").ToLower();
            public string Name { get; set; }
        }

        public class RecaptchaItem : StringItem
        {
            public RecaptchaItem() => Version = "2";
            public string Challenge { get; set; }
            public string Version { get; set; }
        }

        public class SelectItem : Item
        {
            public SelectItem(Dictionary<string, string> options) => Options = options;

            public Dictionary<string, string> Options { get; }
            public string Value { get; set; }
        }

        public class StringItem : Item
        {
            public string Cookie { get; set; }
            public string SiteKey { get; set; }
            public string Value { get; set; }
        }
    }
}
