using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataRutracker : ConfigurationDataCaptchaLogin
    {
        public BoolConfigurationItem StripRussianLetters { get; private set; }
        public DisplayInfoConfigurationItem CaptchaWarning { get; private set; }

        public ConfigurationDataRutracker()
            : base()
        {
            StripRussianLetters = new BoolConfigurationItem("Strip Russian Letters") { Value = true };
            CaptchaWarning = new DisplayInfoConfigurationItem("Captcha Info", "<b>About Captcha:</b> If the Captcha Image is missing then leave the Captcha Text empty.");
        }
    }
}
