namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    internal class ConfigurationDataRutracker : ConfigurationDataCaptchaLogin
    {
        public BoolItem StripRussianLetters { get; private set; }
        public DisplayItem CaptchaWarning { get; private set; }

        public ConfigurationDataRutracker()
        {
            StripRussianLetters = new BoolItem { Name = "Strip Russian Letters", Value = true };
            CaptchaWarning =
                new DisplayItem("<b>About Captcha:</b> If the Captcha Image is missing then leave the Captcha Text empty.")
                {
                    Name = "Captcha Info"
                };
        }
    }
}
