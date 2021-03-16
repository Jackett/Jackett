namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataBasicLoginWithFilter : ConfigurationData
    {
        public StringConfigurationItem Username { get; private set; }
        public StringConfigurationItem Password { get; private set; }
        public HiddenStringConfigurationItem LastLoggedInCheck { get; private set; }
        public DisplayInfoConfigurationItem FilterExample { get; private set; }
        public StringConfigurationItem FilterString { get; private set; }

        public ConfigurationDataBasicLoginWithFilter(string FilterInstructions)
        {
            Username = new StringConfigurationItem("Username");
            Password = new StringConfigurationItem("Password");
            LastLoggedInCheck = new HiddenStringConfigurationItem("LastLoggedInCheck");
            FilterExample = new DisplayInfoConfigurationItem("", FilterInstructions);
            FilterString = new StringConfigurationItem("Filters (optional)");
        }
    }
}
