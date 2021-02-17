using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataRutracker : ConfigurationDataCaptchaLogin
    {
        public BoolItem StripRussianLetters { get; private set; }
        public BoolItem MoveTagsToEndOfReleaseTitle { get; private set; }
        public DisplayItem CaptchaWarning { get; private set; }

        public ConfigurationDataRutracker()
            : base()
        {
            StripRussianLetters = new BoolItem() { Name = "Strip Russian Letters", Value = true };
            MoveTagsToEndOfReleaseTitle = new BoolItem() { Name = "Move tags to end of release title", Value = false };
            CaptchaWarning = new DisplayItem("<b>About Captcha:</b> If the Captcha Image is missing then leave the Captcha Text empty.") { Name = "Captcha Info" };
        }
    }
}
