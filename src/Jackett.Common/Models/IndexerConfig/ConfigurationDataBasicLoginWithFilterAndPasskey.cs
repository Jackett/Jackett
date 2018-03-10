namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataBasicLoginWithFilterAndPasskey : ConfigurationData
    {
        public StringItem Username { get; private set; }
        public StringItem Password { get; private set; }
        public StringItem Passkey { get; private set; }
        public DisplayItem FilterExample { get; private set; }
        public StringItem FilterString { get; private set; }

        public ConfigurationDataBasicLoginWithFilterAndPasskey(string FilterInstructions)
        {
            Username = new StringItem { Name = "Username" };
            Password = new StringItem { Name = "Password" };
            Passkey = new StringItem { Name = "Passkey" };
            FilterExample = new DisplayItem(FilterInstructions)
            {
                Name = ""
            };
            FilterString = new StringItem { Name = "Filters (optional)" };
        }


    }
}