using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataRutracker : ConfigurationDataCaptchaLogin
    {
        public BoolConfigurationItem StripRussianLetters { get; private set; }
        public DisplayInfoConfigurationItem MoveTagsInfo { get; private set; }
        public BoolConfigurationItem MoveFirstTagsToEndOfReleaseTitle { get; private set; }
        public BoolConfigurationItem MoveAllTagsToEndOfReleaseTitle { get; private set; }
        public DisplayInfoConfigurationItem CaptchaWarning { get; private set; }

        public ConfigurationDataRutracker()
            : base()
        {
            StripRussianLetters = new BoolConfigurationItem("Strip Russian Letters") { Value = true };
            MoveTagsInfo = new DisplayInfoConfigurationItem("Move Tags Info", "<b>About moving tags:</b> " +
                                            "We define a tag as a part of the release title between round or square brackets. " +
                                            "If the release title contains tags then these options will move those tags and their brackets to the end of the release title. " +
                                            "Moving only the first tags will try to detect where the actual title of the release begins, and move only the tags that are found before that point. " +
                                            "Enabling both options will enable moving of all tags.");
            MoveFirstTagsToEndOfReleaseTitle = new BoolConfigurationItem("Move first tags to end of release title") { Value = false };
            MoveAllTagsToEndOfReleaseTitle = new BoolConfigurationItem("Move all tags to end of release title") { Value = false };
            CaptchaWarning = new DisplayInfoConfigurationItem("Captcha Info", "<b>About Captcha:</b> If the Captcha Image is missing then leave the Captcha Text empty.");
        }
    }
}
