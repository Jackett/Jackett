namespace Jackett.Common.Models.IndexerConfig
{
    internal class ConfigurationDataCaptchaLogin : ConfigurationData
    {
        public StringConfigurationItem Username { get; private set; }

        public StringConfigurationItem Password { get; private set; }

        public DisplayImageConfigurationItem CaptchaImage { get; private set; }

        public StringConfigurationItem CaptchaText { get; private set; }

        public HiddenStringConfigurationItem CaptchaCookie { get; private set; }

        public DisplayInfoConfigurationItem Instructions { get; private set; }

        /// <param name="instructionMessageOptional">Enter any instructions the user will need to setup the tracker</param>
        public ConfigurationDataCaptchaLogin(string instructionMessageOptional = null)
        {
            Username = new StringConfigurationItem("Username");
            Password = new StringConfigurationItem("Password");
            CaptchaImage = new DisplayImageConfigurationItem("Captcha Image");
            CaptchaText = new StringConfigurationItem("Captcha Text");
            CaptchaCookie = new HiddenStringConfigurationItem("Captcha Cookie");
            Instructions = new DisplayInfoConfigurationItem("", instructionMessageOptional);
        }
    }
}
