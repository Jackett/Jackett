using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;

using Newtonsoft.Json.Linq;

namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationData
    {
        private const string PASSWORD_REPLACEMENT = "|||%%PREVJACKPASSWD%%|||";
        protected Dictionary<string, ConfigurationItem> dynamics = new Dictionary<string, ConfigurationItem>(); // list for dynamic items

        public HiddenStringConfigurationItem CookieHeader { get; private set; } = new HiddenStringConfigurationItem(name: "CookieHeader");
        public HiddenStringConfigurationItem LastError { get; private set; } = new HiddenStringConfigurationItem(name: "LastError");
        public StringConfigurationItem SiteLink { get; private set; } = new StringConfigurationItem(name: "Site Link");
        public TagsConfigurationItem Tags { get; private set; } = new TagsConfigurationItem(name: "Tags", charSet: "A-Za-z0-9\\-\\._~");

        public ConfigurationData()
        {

        }

        /// <summary>
        /// Loads all JSON values into the matching properties.
        /// </summary>
        public void LoadConfigDataValuesFromJson(JToken json, IProtectionService ps = null)
        {
            if (json == null)
                return;

            var jsonArray = (JArray)json;

            foreach (var item in GetAllConfigurationItems().Where(x => x.CanBeSavedToFile))
            {
                var jsonToken = jsonArray.FirstOrDefault(f => f.Value<string>("id") == item.ID);
                if (jsonToken == null)
                    continue;
                item.FromJson(jsonToken, ps);
            }
        }

        public JToken ToJson(IProtectionService ps, bool forDisplay = true)
        {
            var jArray = new JArray();

            var configurationItems = GetConfigurationItems(forDisplay);
            foreach (var configurationItem in configurationItems)
            {
                var jObject = configurationItem.ToJson(ps, forDisplay);

                if (jObject != null)
                {
                    jArray.Add(jObject);
                }
            }
            return jArray;
        }

        private IEnumerable<ConfigurationItem> GetAllConfigurationItems()
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

            // remove/insert Tags manualy to make sure it shows up last
            properties.Remove(Tags);

            properties.AddRange(dynamics.Values);

            properties.Add(Tags);

            return properties;
        }

        private ConfigurationItem[] GetConfigurationItems(bool forDisplay)
            => GetAllConfigurationItems().Where(p => forDisplay ? p.CanBeShownToUser : p.CanBeSavedToFile).ToArray();

        public void AddDynamic(string ID, ConfigurationItem item) => dynamics[ID] = item;

        public ConfigurationItem GetDynamic(string ID) => dynamics.TryGetValue(ID, out var value) ? value : null;

        public ConfigurationItem GetDynamicByName(string Name)
            => dynamics.Values.FirstOrDefault(i => string.Equals(i.Name, Name, StringComparison.InvariantCultureIgnoreCase));

        public abstract class ConfigurationItem
        {
            public string ID => Name.Replace(" ", "").ToLower();
            public string Name { get; }
            public string ItemType { get; }

            public bool CanBeShownToUser { get; } = true;
            public bool CanBeSavedToFile { get; } = true;

            protected ConfigurationItem(string name, string itemType, bool canBeShownToUser = true, bool canBeSavedToFile = true)
            {
                Name = name; // TODO Error when name is null/empty/not unique/...
                ItemType = itemType;
                CanBeShownToUser = canBeShownToUser;
                CanBeSavedToFile = canBeSavedToFile;
            }

            protected JObject CreateJObject()
            {
                return new JObject
                {
                    ["id"] = ID,
                    ["type"] = ItemType.ToLower(),
                    ["name"] = Name
                };
            }

            protected static T ReadValueAs<T>(JToken jToken) => jToken.Value<T>("value");

            protected static bool HasPasswordValue(ConfigurationItem item)
                => string.Equals(item.Name, "password", StringComparison.InvariantCultureIgnoreCase);

            public virtual JObject ToJson(IProtectionService protectionService = null, bool forDisplay = true) => null;
            public virtual void FromJson(JToken jsonToken, IProtectionService protectionService = null) { }
        }

        /// <summary>
        /// Remove this class when all passwords are configured to use the correct class: PasswordConfigurationItem.
        /// </summary>
        public abstract class ConfigurationItemMaybePassword : ConfigurationItem
        {
            public string Value { get; set; }

            protected ConfigurationItemMaybePassword(string name, string itemType, bool canBeShownToUser = true, bool canBeSavedToFile = true)
                : base(name, itemType, canBeShownToUser, canBeSavedToFile)
            {
            }

            public override JObject ToJson(IProtectionService protectionService = null, bool forDisplay = true)
            {
                var jObject = CreateJObject();

                if (string.Equals(Name, "password", StringComparison.InvariantCultureIgnoreCase))
                {
                    var password = Value;
                    if (string.IsNullOrEmpty(password))
                        password = string.Empty;
                    else if (protectionService != null)
                        password = protectionService.Protect(password);
                    jObject["value"] = password;
                }
                else
                {
                    jObject["value"] = Value;
                }
                return jObject;
            }
        }

        public class StringConfigurationItem : ConfigurationItemMaybePassword
        {
            public StringConfigurationItem(string name)
                : base(name, itemType: "inputstring")
            {
            }

            public override void FromJson(JToken jsonToken, IProtectionService ps = null)
            {
                if (HasPasswordValue(this))
                {
                    var pw = ReadValueAs<string>(jsonToken);
                    if (pw != PASSWORD_REPLACEMENT)
                    {
                        Value = ps != null ? ps.UnProtect(pw) : pw;
                    }
                }
                else
                {
                    Value = ReadValueAs<string>(jsonToken);
                }
            }
        }

        public class HiddenStringConfigurationItem : ConfigurationItemMaybePassword
        {
            public HiddenStringConfigurationItem(string name)
                : base(name, itemType: "hiddendata", canBeShownToUser: false)
            {
            }

            public override void FromJson(JToken jsonToken, IProtectionService ps = null)
            {
                Value = ReadValueAs<string>(jsonToken);
            }
        }

        public class DisplayInfoConfigurationItem : ConfigurationItemMaybePassword
        {
            public DisplayInfoConfigurationItem(string name, string value)
                : base(name, itemType: "displayinfo", canBeSavedToFile: false)
            {
                Value = value;
            }
        }

        public class BoolConfigurationItem : ConfigurationItem
        {
            public bool Value { get; set; }

            public BoolConfigurationItem(string name)
                : base(name, itemType: "inputbool")
            {
            }

            public override JObject ToJson(IProtectionService ps = null, bool forDisplay = true)
            {
                var jObject = CreateJObject();
                jObject["value"] = Value;
                return jObject;
            }

            public override void FromJson(JToken jsonToken, IProtectionService ps = null)
            {
                Value = ReadValueAs<bool>(jsonToken);
            }
        }

        public class DisplayImageConfigurationItem : ConfigurationItem
        {
            public byte[] Value { get; set; }

            public DisplayImageConfigurationItem(string name)
                : base(name, itemType: "displayimage", canBeSavedToFile: false)
            {
            }

            public override JObject ToJson(IProtectionService ps = null, bool forDisplay = true)
            {
                var jObject = CreateJObject();

                var dataUri = DataUrlUtils.BytesToDataUrl(Value, "image/jpeg");
                jObject["value"] = dataUri;

                return jObject;
            }
        }

        public class SingleSelectConfigurationItem : ConfigurationItem
        {
            public string Value { get; set; }

            public Dictionary<string, string> Options { get; }

            public SingleSelectConfigurationItem(string name, Dictionary<string, string> options)
                : base(name, itemType: "inputselect") => Options = options;

            public override JObject ToJson(IProtectionService ps = null, bool forDisplay = true)
            {
                var jObject = CreateJObject();

                jObject["value"] = Value;
                jObject["options"] = new JObject();
                foreach (var option in Options)
                {
                    jObject["options"][option.Key] = option.Value;
                }

                return jObject;
            }

            public override void FromJson(JToken jsonToken, IProtectionService ps = null)
            {
                Value = ReadValueAs<string>(jsonToken);
            }
        }

        public class MultiSelectConfigurationItem : ConfigurationItem
        {
            public string[] Values { get; set; }

            public Dictionary<string, string> Options { get; }

            public MultiSelectConfigurationItem(string name, Dictionary<string, string> options)
                : base(name, itemType: "inputcheckbox") => Options = options;

            public override JObject ToJson(IProtectionService ps, bool forDisplay)
            {
                var jObject = CreateJObject();

                jObject["values"] = new JArray(Values);
                jObject["options"] = new JObject();
                foreach (var option in Options)
                {
                    jObject["options"][option.Key] = option.Value;
                }

                return jObject;
            }

            public override void FromJson(JToken jsonToken, IProtectionService ps = null)
            {
                var values = jsonToken.Value<JArray>("values");
                if (values != null)
                {
                    Values = values.Values<string>().ToArray();
                }
            }
        }

        public class PasswordConfigurationItem : ConfigurationItem
        {
            public string Value { get; set; }

            public PasswordConfigurationItem(string name)
                : base(name, itemType: "password")
            {
            }

            public override JObject ToJson(IProtectionService protectionService = null, bool forDisplay = true)
            {
                var jObject = CreateJObject();

                var password = Value;
                if (string.IsNullOrEmpty(password))
                    password = string.Empty;
                else if (forDisplay)
                    password = PASSWORD_REPLACEMENT;
                else if (protectionService != null)
                    password = protectionService.Protect(password);
                jObject["value"] = password;

                return jObject;
            }

            public override void FromJson(JToken jsonToken, IProtectionService ps = null)
            {
                var pw = ReadValueAs<string>(jsonToken);
                if (pw != PASSWORD_REPLACEMENT)
                {
                    Value = ps != null ? ps.UnProtect(pw) : pw;
                }
            }
        }

        public class TagsConfigurationItem : ConfigurationItem
        {
            public HashSet<string> Values { get; }
            public string Pattern { get; set; }
            public char Separator { get; set; }
            public string Delimiters { get; set; }

            public HashSet<string> Whitelist { get; }
            public HashSet<string> Blacklist { get; }

            public TagsConfigurationItem(string name, string charSet = null, char separator = ',')
                : base(name, "inputtags")
            {
                Values = new HashSet<string>();
                Whitelist = new HashSet<string>();
                Blacklist = new HashSet<string>();
                if (!string.IsNullOrWhiteSpace(charSet))
                {
                    Pattern = $"^[{charSet}]+$";
                    Delimiters = $"[^{charSet}]+";
                }
                Separator = separator;
            }

            public override JObject ToJson(IProtectionService ps = null, bool forDisplay = true)
            {
                var jObject = CreateJObject();
                var separator = Separator.ToString();
                jObject["value"] = string.Join(separator, Values);
                if (forDisplay)
                {
                    jObject["separator"] = separator;
                    if (!string.IsNullOrWhiteSpace(Delimiters))
                        jObject["delimiters"] = Delimiters;
                    if (!string.IsNullOrWhiteSpace(Pattern))
                        jObject["pattern"] = Pattern;
                    if (Whitelist.Count > 0)
                        jObject["whitelist"] = string.Join(separator, Whitelist);
                    if (Blacklist.Count > 0)
                        jObject["blacklist"] = string.Join(separator, Blacklist);
                }

                return jObject;
            }

            public override void FromJson(JToken jsonToken, IProtectionService ps)
            {
                var value = ReadValueAs<string>(jsonToken);
                if (value == null)
                    return;
                Values.Clear();
                var tags = Regex.Split(value, !string.IsNullOrWhiteSpace(Delimiters) ? Delimiters : $"{Separator}+").Select(t => t.Trim().ToLowerInvariant());
                if (!string.IsNullOrWhiteSpace(Pattern))
                    tags = tags.Where(t => Whitelist.Contains(t) || Regex.IsMatch(t, Pattern));
                if (Blacklist.Count > 0)
                    tags = tags.Where(t => !Blacklist.Contains(t));
                foreach (var tag in tags)
                    Values.Add(tag);
            }
        }
    }
}
