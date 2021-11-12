namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataBasicLoginWithPID : ConfigurationData
    {
        public StringConfigurationItem Username { get; private set; }
        public StringConfigurationItem Password { get; private set; }
        public StringConfigurationItem Pid { get; private set; }
        public DisplayInfoConfigurationItem Instructions { get; private set; }

        public ConfigurationDataBasicLoginWithPID(string instructionMessageOptional = null)
        {
            Username = new StringConfigurationItem("Username");
            Password = new StringConfigurationItem("Password");
            Pid = new StringConfigurationItem("Pid");
            Instructions = new DisplayInfoConfigurationItem("", instructionMessageOptional);
        }
    }
}
