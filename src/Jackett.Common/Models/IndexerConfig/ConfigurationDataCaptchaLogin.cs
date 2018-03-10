namespace Jackett.Common.Models.IndexerConfig
{
    class ConfigurationDataCaptchaLogin : ConfigurationData
    {
        public StringItem Username { get; private set; }

        public StringItem Password { get; private set; }

        public ImageItem CaptchaImage { get; private set; }

        public StringItem CaptchaText { get; private set; }

        public HiddenItem CaptchaCookie { get; private set; }

        public DisplayItem Instructions { get; private set; }

        /// <param name="instructionMessageOptional">Enter any instructions the user will need to setup the tracker</param>
        public ConfigurationDataCaptchaLogin(string instructionMessageOptional = null)
        {
            Username = new StringItem { Name = "Username" };
            Password = new StringItem { Name = "Password" };
            CaptchaImage = new ImageItem { Name = "Captcha Image" };
            CaptchaText = new StringItem { Name = "Captcha Text" };
            CaptchaCookie = new HiddenItem("") { Name = "Captcha Cookie" };
            Instructions = new DisplayItem(instructionMessageOptional) { Name = "" };
        }
    }
}
