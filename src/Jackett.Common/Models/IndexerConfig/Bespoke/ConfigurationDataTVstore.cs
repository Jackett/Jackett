using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataTVstore : ConfigurationData
    {
        public StringConfigurationItem Username { get; private set; }
        public StringConfigurationItem Password { get; private set; }
        public DisplayInfoConfigurationItem AccountActivity { get; private set; }

        public ConfigurationDataTVstore()
        {
            Username = new StringConfigurationItem("Username") { Value = "" };
            Password = new StringConfigurationItem("Password") { Value = "" };
            AccountActivity = new DisplayInfoConfigurationItem("Account Inactivity", "If a user does not log in to the site for 100 days (Tag, Storekeeper for 1 year), it is automatically deleted.");
        }

        public ConfigurationDataTVstore(JToken json)
        {
            var configData = new ConfigurationDataTVstore();

            dynamic configArray = JsonConvert.DeserializeObject(json.ToString());
            foreach (var config in configArray)
            {
                var propertyName = UppercaseFirst((string)config.id);
                switch (propertyName)
                {
                    case "Username":
                        Username = new StringConfigurationItem(propertyName) { Value = config.value };
                        break;
                    case "Password":
                        Password = new StringConfigurationItem(propertyName) { Value = config.value };
                        break;
                    default:
                        break;
                }
            }
        }

        private static string UppercaseFirst(string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;
            return char.ToUpper(s[0]) + s.Substring(1);
        }
    }
}
