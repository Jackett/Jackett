namespace Jackett.Common.Models.IndexerConfig
{
    internal class ConfigurationDataPinNumber : ConfigurationDataBasicLogin
    {
        public StringConfigurationItem Pin { get; private set; }

        public ConfigurationDataPinNumber(string instructionMessageOptional = null)
            : base(instructionMessageOptional)
            => Pin = new StringConfigurationItem("Login Pin Number");
    }
}
