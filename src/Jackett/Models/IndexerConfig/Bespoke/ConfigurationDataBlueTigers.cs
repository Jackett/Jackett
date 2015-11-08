using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jackett.Models.IndexerConfig.Bespoke
{
    public class ConfigurationDataBlueTigers : ConfigurationData
    {
        public StringItem Username { get; private set; }
        public StringItem Password { get; private set; }
        public DisplayItem Instructions { get; set; }
        public BoolItem French { get; set; }
        public BoolItem English { get; set; }
        public BoolItem Spanish { get; set; }

        public ConfigurationDataBlueTigers(string displayInstructions)
        {
            Username = new StringItem { Name = "Username", Value = "" };
            Password = new StringItem { Name = "Password", Value = "" };
            Instructions = new DisplayItem(displayInstructions) { Name = "" };
            French = new BoolItem { Name = "French", Value = true };
            English = new BoolItem { Name = "English", Value = true };
            Spanish = new BoolItem { Name = "Spanish", Value = true };
        }

        public ConfigurationDataBlueTigers(JToken json)
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
                    case "French":
                        French = new BoolItem { Name = propertyName, Value = config.value };
                        break;
                    case "English":
                        English = new BoolItem { Name = propertyName, Value = config.value };
                        break;
                    case "Spanish":
                        Spanish = new BoolItem { Name = propertyName, Value = config.value };
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