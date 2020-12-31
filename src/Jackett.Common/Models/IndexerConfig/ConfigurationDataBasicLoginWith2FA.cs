namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataBasicLoginWith2FA : ConfigurationData
    {
        public StringItem Username { get; private set; }
        public StringItem Password { get; private set; }
        public StringItem TwoFactorAuth { get; private set; }
        public DisplayItem Instructions { get; private set; }

        public ConfigurationDataBasicLoginWith2FA(string instructionMessageOptional = null)
        {
            Username = new StringItem { Name = "Username" };
            Password = new StringItem { Name = "Password" };
            TwoFactorAuth = new StringItem { Name = "Two-Factor Auth" };
            Instructions = new DisplayItem(instructionMessageOptional) { Name = "" };
        }
    }
}
