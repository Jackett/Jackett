namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataBasicLogin : ConfigurationData
    {
        public StringItem Username { get; private set; }
        public StringItem Password { get; private set; }
        public DisplayItem Instructions { get; private set; }

        public ConfigurationDataBasicLogin(string instructionMessageOptional = null)
        {
            Username = new StringItem { Name = "Username" };
            Password = new StringItem { Name = "Password" };
            Instructions = new DisplayItem(instructionMessageOptional) { Name = "" };
        }


    }
}
