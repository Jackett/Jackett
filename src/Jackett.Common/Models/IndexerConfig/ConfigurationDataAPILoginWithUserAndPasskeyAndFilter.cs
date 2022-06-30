namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataAPILoginWithUserAndPasskeyAndFilter : ConfigurationData
    {
        public DisplayInfoConfigurationItem KeyHint { get; private set; }
        public StringConfigurationItem User { get; private set; }
        public StringConfigurationItem Key { get; private set; }
        public DisplayInfoConfigurationItem FilterExample { get; private set; }
        public StringConfigurationItem FilterString { get; private set; }

        public ConfigurationDataAPILoginWithUserAndPasskeyAndFilter(string FilterInstructions)
        {
            KeyHint = new DisplayInfoConfigurationItem("API Authentication", "<ul><li>Visit the security tab on your user settings page to access your ApiUser and ApiKey <li>If you haven't yet generated a key, you may have to first generate one using the checkbox below your keys</ul>");
            User = new StringConfigurationItem("ApiUser") { Value = string.Empty };
            Key = new StringConfigurationItem("ApiKey") { Value = string.Empty };

            FilterExample = new DisplayInfoConfigurationItem("", FilterInstructions);
            FilterString = new StringConfigurationItem("Filters (optional)");
        }
    }
}
