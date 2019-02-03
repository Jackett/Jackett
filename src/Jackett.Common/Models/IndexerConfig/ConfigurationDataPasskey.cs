namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataPasskey : ConfigurationData
    {
        public StringItem Passkey { get; private set; }
        public DisplayItem Instructions { get; private set; }

        public ConfigurationDataPasskey(string instructionMessageOptional = null)
        {
            Passkey = new StringItem { Name = "Passkey" };
            Instructions = new DisplayItem(instructionMessageOptional) { Name = "" };
        }
    }
}
