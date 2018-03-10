using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    public class ConfigurationDataNCore : ConfigurationData
    {
        public StringItem Username { get; private set; }
        public StringItem Password { get; private set; }
        public StringItem TwoFactor { get; private set; }
        public BoolItem Hungarian { get; set; }
        public BoolItem English { get; set; }

        public ConfigurationDataNCore()
        {
            Username = new StringItem { Name = "Username", Value = "" };
            Password = new StringItem { Name = "Password", Value = "" };
            TwoFactor = new StringItem { Name = "Twofactor", Value = "" };
            Hungarian = new BoolItem { Name = "Hungarian", Value = true };
            English = new BoolItem { Name = "English", Value = true };
        }

        public ConfigurationDataNCore(JToken json)
        {
            ConfigurationDataNCore configData = new ConfigurationDataNCore();

            dynamic configArray = JsonConvert.DeserializeObject(json.ToString());
            foreach (var config in configArray)
            {
                string propertyName = UppercaseFirst((string)config.id);
                switch (propertyName)
                {
                    case "Username":
                        Username = new StringItem { Name = propertyName, Value = config.value };
                        break;
                    case "Password":
                        Password = new StringItem { Name = propertyName, Value = config.value };
                        break;
                    case "Twofactor":
                        TwoFactor = new StringItem { Name = propertyName, Value = config.value };
                        break;
                    case "Hungarian":
                        Hungarian = new BoolItem { Name = propertyName, Value = config.value };
                        break;
                    case "English":
                        English = new BoolItem { Name = propertyName, Value = config.value };
                        break;
                    default:
                        break;
                }
            }
        }

        static string UppercaseFirst(string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;
            return char.ToUpper(s[0]) + s.Substring(1);
        }
    }
}