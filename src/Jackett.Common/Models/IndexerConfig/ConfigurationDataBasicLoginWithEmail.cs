namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataBasicLoginWithEmail : ConfigurationData
    {
        public StringItem Email { get; private set; }
        public StringItem Password { get; private set; }
        public DisplayItem Instructions { get; private set; }

        public ConfigurationDataBasicLoginWithEmail(string instructionMessageOptional = null)
        {
            Email = new StringItem { Name = "Email" };
            Password = new StringItem { Name = "Password" };
            Instructions = new DisplayItem(instructionMessageOptional) { Name = "" };
        }


    }
}
