namespace Jackett.Common.Models.IndexerConfig
{
    class ConfigurationDataPinNumber : ConfigurationDataBasicLogin
    {
        public StringItem Pin { get; private set; }

        public ConfigurationDataPinNumber() : base()
        {
            Pin = new StringItem { Name = "Login Pin Number" };
        }
    }
}
