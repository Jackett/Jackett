namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataAPILoginWithUserAndPasskeyAndFilter : ConfigurationData
    {
        public StringItem User { get; private set; }
        public StringItem Key { get; private set; }
        public StringItem Passkey { get; private set; }
        public DisplayItem FilterExample { get; private set; }
        public StringItem FilterString { get; private set; }

        public ConfigurationDataAPILoginWithUserAndPasskeyAndFilter(string FilterInstructions)
        {
            User = new StringItem { Name = "ApiUser", Value = string.Empty };
            Key = new StringItem { Name = "ApiKey", Value = string.Empty };
            Passkey = new StringItem { Name = "Passkey", Value = string.Empty };
            FilterExample = new DisplayItem(FilterInstructions)
            {
                Name = ""
            };
            FilterString = new StringItem { Name = "Filters (optional)" };
        }
    }
}