using System.Text.Json.Serialization;

namespace Jackett.Common.Models.IndexerConfig
{
    internal class ConfigurationDataCaptchaLogin : ConfigurationData
    {
        [JsonPropertyOrder(1)]
        public StringConfigurationItem Username { get; private set; }

        [JsonPropertyOrder(2)]
        public StringConfigurationItem Password { get; private set; }

        [JsonPropertyOrder(3)]
        public DisplayImageConfigurationItem CaptchaImage { get; private set; }

        [JsonPropertyOrder(4)]
        public StringConfigurationItem CaptchaText { get; private set; }

        [JsonPropertyOrder(5)]
        public HiddenStringConfigurationItem CaptchaCookie { get; private set; }

        [JsonPropertyOrder(6)]
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
