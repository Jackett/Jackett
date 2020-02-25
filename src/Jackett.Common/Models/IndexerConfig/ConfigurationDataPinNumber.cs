namespace Jackett.Common.Models.IndexerConfig
{
    internal class ConfigurationDataPinNumber : ConfigurationDataBasicLogin
    {
        public StringItem Pin { get; private set; }

        public ConfigurationDataPinNumber() => Pin = new StringItem { Name = "Login Pin Number" };
    }
}
