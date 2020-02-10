using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    public class ConfigurationDataTVstore : ConfigurationData
    {
        public StringItem Username { get; private set; }
        public StringItem Password { get; private set; }

        public ConfigurationDataTVstore()
        {
            Username = new StringItem { Name = "Username", Value = "" };
            Password = new StringItem { Name = "Password", Value = "" };
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
                        Username = new StringItem { Name = propertyName, Value = config.value };
                        break;
                    case "Password":
                        Password = new StringItem { Name = propertyName, Value = config.value };
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
