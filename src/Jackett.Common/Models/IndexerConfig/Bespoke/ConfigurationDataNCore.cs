using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataNCore : ConfigurationData
    {
        public StringConfigurationItem Username { get; private set; }
        public StringConfigurationItem Password { get; private set; }
        public StringConfigurationItem TwoFactor { get; private set; }
        public BoolConfigurationItem Hungarian { get; set; }
        public BoolConfigurationItem English { get; set; }

        public ConfigurationDataNCore()
        {
            Username = new StringConfigurationItem("Username") { Value = "" };
            Password = new StringConfigurationItem("Password") { Value = "" };
            TwoFactor = new StringConfigurationItem("Twofactor") { Value = "" };
            Hungarian = new BoolConfigurationItem("Hungarian") { Value = true };
            English = new BoolConfigurationItem("English") { Value = true };
        }

        public ConfigurationDataNCore(JToken json)
        {
            var configData = new ConfigurationDataNCore();

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
                    case "Twofactor":
                        TwoFactor = new StringConfigurationItem(propertyName) { Value = config.value };
                        break;
                    case "Hungarian":
                        Hungarian = new BoolConfigurationItem(propertyName) { Value = config.value };
                        break;
                    case "English":
                        English = new BoolConfigurationItem(propertyName) { Value = config.value };
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
