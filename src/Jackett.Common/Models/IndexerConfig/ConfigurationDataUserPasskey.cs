namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataUserPasskey : ConfigurationData
    {
        public StringItem Username { get; private set; }
        public StringItem Passkey { get; private set; }
        public DisplayItem Instructions { get; private set; }

        public ConfigurationDataUserPasskey(string instructionMessageOptional = null)
        {
            Username = new StringItem { Name = "Username" };
            Passkey = new StringItem { Name = "Passkey" };
            Instructions = new DisplayItem(instructionMessageOptional) { Name = "" };
        }
    }
}
