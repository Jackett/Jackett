namespace Jackett.Common.Models.IndexerConfig
{
    internal class ConfigurationDataPinNumber : ConfigurationDataBasicLogin
    {
        public StringItem Pin { get; private set; }

        public ConfigurationDataPinNumber(string instructionMessageOptional = null)
            : base(instructionMessageOptional)
            => Pin = new StringItem { Name = "Login Pin Number" };
    }
}
