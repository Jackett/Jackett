namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataBasicLoginWithFreeleech : ConfigurationData
    {
        public StringItem Username { get; private set; }
        public StringItem Password { get; private set; }
        public BoolItem Freeleech { get; private set; }
        public DisplayItem Instructions { get; private set; }

        public ConfigurationDataBasicLoginWithFreeleech(string instructionMessageOptional = null)
        {
            Username = new StringItem { Name = "Username" };
            Password = new StringItem { Name = "Password" };
            Freeleech = new BoolItem { Name = "Freeleech", Value = false };
            Instructions = new DisplayItem(instructionMessageOptional) { Name = "" };
        }
    }
}
