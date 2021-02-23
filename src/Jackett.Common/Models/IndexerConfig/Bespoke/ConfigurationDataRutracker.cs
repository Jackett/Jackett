using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataRutracker : ConfigurationDataCaptchaLogin
    {
        public BoolItem StripRussianLetters { get; private set; }
        public DisplayItem MoveTagsInfo { get; private set; }
        public BoolItem MoveFirstTagsToEndOfReleaseTitle { get; private set; }
        public BoolItem MoveAllTagsToEndOfReleaseTitle { get; private set; }
        public DisplayItem CaptchaWarning { get; private set; }

        public ConfigurationDataRutracker()
            : base()
        {
            StripRussianLetters = new BoolItem() { Name = "Strip Russian Letters", Value = true };
            MoveTagsInfo = new DisplayItem("<b>About moving tags:</b> "+
                                            "We define a tag as a part of the release title between round or square brackets. "+
                                            "If the release title contains tags then these options will move those tags and their brackets to the end of the release title. "+
                                            "Moving only the first tags will try to detect where the actual title of the release begins, and move only the tags that are found before that point. "+
                                            "Enabling both options will enable moving of all tags.")
                                            { Name = "Move Tags Info" };
            MoveFirstTagsToEndOfReleaseTitle = new BoolItem() { Name = "Move first tags to end of release title", Value = false };
            MoveAllTagsToEndOfReleaseTitle = new BoolItem() { Name = "Move all tags to end of release title", Value = false };
            CaptchaWarning = new DisplayItem("<b>About Captcha:</b> If the Captcha Image is missing then leave the Captcha Text empty.") { Name = "Captcha Info" };
        }
    }
}
