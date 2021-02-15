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
        protected Dictionary<string, ConfigurationItem> dynamics = new Dictionary<string, ConfigurationItem>(); // list for dynamic items

        public HiddenStringConfigurationItem CookieHeader { get; private set; } = new HiddenStringConfigurationItem(name:"CookieHeader");
        public HiddenStringConfigurationItem LastError { get; private set; } = new HiddenStringConfigurationItem(name:"LastError");
        public StringConfigurationItem SiteLink { get; private set; } = new StringConfigurationItem(name:"Site Link");

        public ConfigurationData()
        {

        }

        public void LoadConfigDataValuesFromJson(JToken json, IProtectionService ps = null)
        {
            if (json == null)
                return;

            var arr = (JArray)json;

            foreach (var item in GetConfigurationItems(forDisplay: false))
            {
                var arrItem = arr.FirstOrDefault(f => f.Value<string>("id") == item.ID);
                if (arrItem == null)
                    continue;

                switch (item)
                {
                    case StringConfigurationItem stringItem:
                    {
                        if (HasPasswordValue(item))
                        {
                            var pw = arrItem.Value<string>("value");
                            if (pw != PASSWORD_REPLACEMENT)
                            {
                                stringItem.Value = ps != null ? ps.UnProtect(pw) : pw;
                            }
                        }
                        else
                        {
                            stringItem.Value = arrItem.Value<string>("value");
                        }
                        break;
                    }
                    case HiddenStringConfigurationItem hiddenStringItem:
                    {
                        hiddenStringItem.Value = arrItem.Value<string>("value");
                        break;
                    }
                    case BoolConfigurationItem boolItem:
                    {
                        boolItem.Value = arrItem.Value<bool>("value");
                        break;
                    }
                    case SingleSelectConfigurationItem singleSelectItem:
                    {
                        singleSelectItem.Value = arrItem.Value<string>("value");
                        break;
                    }
                    case MultiSelectConfigurationItem multiSelectItem:
                    {
                        var values = arrItem.Value<JArray>("values");
                        if (values != null)
                        {
                            multiSelectItem.Values = values.Values<string>().ToArray();
                        }
                        break;
                    }
                    case PasswordConfigurationItem passwordItem:
                    {
                        var pw = arrItem.Value<string>("value");
                        if (pw != PASSWORD_REPLACEMENT)
                        {
                            passwordItem.Value = ps != null ? ps.UnProtect(pw) : pw;
                        }
                        break;
                    }
                    default:
                    {
                        //Not in switch:
                            //DisplayInfoConfigurationItem
                            //DisplayImageConfigurationItem
                        break;
                    }
                }
            }
        }

        private bool HasPasswordValue(ConfigurationItem item)
            => string.Equals(item.Name, "password", StringComparison.InvariantCultureIgnoreCase);

        private void SetPasswordInJson(in JObject jObject, string password, bool forDisplay, IProtectionService ps = null)
        {
            if (string.IsNullOrEmpty(password))
                password = string.Empty;
            else if (forDisplay)
                password = PASSWORD_REPLACEMENT;
            else if (ps != null)
                password = ps.Protect(password);

            jObject["value"] = password;
        }

        public JToken ToJson(IProtectionService ps, bool forDisplay = true)
        {
            var items = GetConfigurationItems(forDisplay);
            var jArray = new JArray();
            foreach (var item in items)
            {
                var jObject = new JObject
                {
                    ["id"] = item.ID,
                    ["type"] = item.ItemType.ToLower(),
                    ["name"] = item.Name
                };

                switch (item)
                {
                    case StringConfigurationItem stringItem:
                    {
                        if (HasPasswordValue(stringItem))
                        {
                            SetPasswordInJson(jObject, stringItem.Value, forDisplay, ps);
                        }
                        else
                        {
                            jObject["value"] = stringItem.Value;
                        }
                        break;
                    }
                    case HiddenStringConfigurationItem hiddenStringItem:
                    {
                        if (HasPasswordValue(hiddenStringItem))
                        {
                            SetPasswordInJson(jObject, hiddenStringItem.Value, forDisplay, ps);
                        }
                        else
                        {
                            jObject["value"] = hiddenStringItem.Value;
                        }
                        break;
                    }
                    case DisplayInfoConfigurationItem displayInfoItem:
                    {
                        if (HasPasswordValue(displayInfoItem))
                        {
                            SetPasswordInJson(jObject, displayInfoItem.Value, forDisplay, ps);
                        }
                        else
                        {
                            jObject["value"] = displayInfoItem.Value;
                        }
                        break;
                    }
                    case BoolConfigurationItem boolItem:
                    {
                        jObject["value"] = boolItem.Value;
                        break;
                    }
                    case SingleSelectConfigurationItem singleSelectItem:
                    {
                        jObject["value"] = singleSelectItem.Value;
                        jObject["options"] = new JObject();

                        foreach (var option in singleSelectItem.Options)
                        {
                            jObject["options"][option.Key] = option.Value;
                        }
                        break;
                    }
                    case MultiSelectConfigurationItem multiSelectItem:
                    {
                        jObject["values"] = new JArray(multiSelectItem.Values);
                        jObject["options"] = new JObject();

                        foreach (var option in multiSelectItem.Options)
                        {
                            jObject["options"][option.Key] = option.Value;
                        }
                        break;
                    }
                    case DisplayImageConfigurationItem imageItem:
                    {
                        var dataUri = DataUrlUtils.BytesToDataUrl(imageItem.Value, "image/jpeg");
                        jObject["value"] = dataUri;
                        break;
                    }
                    case PasswordConfigurationItem passwordItem:
                    {
                        SetPasswordInJson(jObject, passwordItem.Value, forDisplay, ps);
                        break;
                    }
                }

                jArray.Add(jObject);
            }
            return jArray;
        }

        private ConfigurationItem[] GetConfigurationItems(bool forDisplay)
        {
            var properties = GetType()
                .GetProperties()
                .Where(p => p.CanRead)
                .Where(p => p.PropertyType.IsSubclassOf(typeof(ConfigurationItem)))
                .Where(p => p.GetValue(this) != null)
                .Select(p => (ConfigurationItem)p.GetValue(this)).ToList();

            // remove/insert Site Link manualy to make sure it shows up first
            properties.Remove(SiteLink);
            properties.Insert(0, SiteLink);

            properties.AddRange(dynamics.Values);

            if (forDisplay)
            {
                properties = properties.Where(p => p.IsVisibleToUser).ToList();
            }

            return properties.ToArray();
        }

        public void AddDynamic(string ID, ConfigurationItem item) => dynamics[ID] = item;

        public ConfigurationItem GetDynamic(string ID) => dynamics.TryGetValue(ID, out var value) ? value : null;

        public ConfigurationItem GetDynamicByName(string Name)
            => dynamics.Values.FirstOrDefault(i => string.Equals(i.Name, Name, StringComparison.InvariantCultureIgnoreCase));

        public abstract class ConfigurationItem
        {
            public string ID => Name.Replace(" ", "").ToLower();
            public string Name { get; }
            public string ItemType { get; }

            public bool IsVisibleToUser { get; }
            public bool IsObfuscated { get; protected set; } = false;

            protected ConfigurationItem(string name, string itemType, bool isVisibleToUser)
            {
                Name = name; // TODO Error when name is null/empty/not unique/...
                ItemType = itemType;
                IsVisibleToUser = isVisibleToUser;
            }
        }

        public class StringConfigurationItem : ConfigurationItem
        {
            public string Value { get; set; }

            public string SiteKey { get; set; } //TODO Find out why StringConfigurationItem needs this.
            public string Cookie { get; set; } //TODO Find out why StringConfigurationItem needs this.

            public StringConfigurationItem(string name)
                : base(name, itemType: "inputstring", isVisibleToUser: true)
            {
            }
        }

        public class HiddenStringConfigurationItem : ConfigurationItem
        {
            public string Value { get; set; }

            public HiddenStringConfigurationItem(string name, string value = "")
                : base(name, itemType: "hiddendata", isVisibleToUser: false)
            {
                Value = value;
            }
        }

        public class DisplayInfoConfigurationItem : ConfigurationItem
        {
            public string Value { get; set; }

            public DisplayInfoConfigurationItem(string name, string value)
                : base(name, itemType: "displayinfo", isVisibleToUser: true)
            {
                Value = value;
            }
        }

        public class BoolConfigurationItem : ConfigurationItem
        {
            public bool Value { get; set; }

            public BoolConfigurationItem(string name)
                : base(name, itemType: "inputbool", isVisibleToUser: true)
            {
            }
        }

        public class DisplayImageConfigurationItem : ConfigurationItem
        {
            public byte[] Value { get; set; }

            public DisplayImageConfigurationItem(string name)
                : base(name, itemType: "displayimage", isVisibleToUser: true)
            {
            }
        }

        public class SingleSelectConfigurationItem : ConfigurationItem
        {
            public string Value { get; set; }

            public Dictionary<string, string> Options { get; }

            public SingleSelectConfigurationItem(string name, Dictionary<string, string> options)
                : base(name, itemType: "inputselect", isVisibleToUser: true)
            {
                Options = options;
            }
        }

        public class MultiSelectConfigurationItem : ConfigurationItem
        {
            public string[] Values { get; set; }

            public Dictionary<string, string> Options { get; }

            public MultiSelectConfigurationItem(string name, Dictionary<string, string> options)
                : base(name, itemType: "inputcheckbox", isVisibleToUser: true)
            {
                Options = options;
            }
        }

        public class PasswordConfigurationItem : ConfigurationItem
        {
            public string Value { get; set; }

            public PasswordConfigurationItem(string name)
                : base(name, itemType: "password", isVisibleToUser: true)
            {
                IsObfuscated = true;
            }
        }
    }
}
