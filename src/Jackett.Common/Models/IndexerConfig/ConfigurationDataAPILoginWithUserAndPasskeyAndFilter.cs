namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataAPILoginWithUserAndPasskeyAndFilter : ConfigurationData
    {
        public StringItem Passkey { get; private set; }
        public DisplayItem KeyHint { get; private set; }
        public StringItem User { get; private set; }
        public StringItem Key { get; private set; }
        public DisplayItem FilterExample { get; private set; }
        public StringItem FilterString { get; private set; }

        public ConfigurationDataAPILoginWithUserAndPasskeyAndFilter(string FilterInstructions)
        {
            Passkey = new StringItem { Name = "Passkey", Value = string.Empty };

            KeyHint = new DisplayItem("<ul><li>Visit the security tab on your user settings page to access your ApiUser and ApiKey <li>If you haven't yet generated a key, you may have to first generate one using the checkbox below your keys</ul>")
            {
                Name = "API Authentication"
            };
            User = new StringItem { Name = "ApiUser", Value = string.Empty };
            Key = new StringItem { Name = "ApiKey", Value = string.Empty };

            FilterExample = new DisplayItem(FilterInstructions)
            {
                Name = ""
            };
            FilterString = new StringItem { Name = "Filters (optional)" };
        }
    }
}