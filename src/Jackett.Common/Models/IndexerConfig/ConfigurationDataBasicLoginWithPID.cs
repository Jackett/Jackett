namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataBasicLoginWithPID : ConfigurationData
    {
        public StringItem Username { get; private set; }
        public StringItem Password { get; private set; }
        public StringItem Pid { get; private set; }
        public DisplayItem Instructions { get; private set; }

        public ConfigurationDataBasicLoginWithPID(string instructionMessageOptional = null)
        {
            Username = new StringItem { Name = "Username" };
            Password = new StringItem { Name = "Password" };
            Pid = new StringItem { Name = "Pid" };
            Instructions = new DisplayItem(instructionMessageOptional) { Name = "" };
        }
    }
}
