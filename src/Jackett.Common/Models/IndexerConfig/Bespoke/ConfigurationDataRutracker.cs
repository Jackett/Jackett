namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    class ConfigurationDataRutracker : ConfigurationDataCaptchaLogin
    {
        public BoolItem StripRussianLetters { get; private set; }

        public ConfigurationDataRutracker()
            : base()
        {
            StripRussianLetters = new BoolItem() { Name = "Strip Russian Letters", Value = true };
        }
    }
}
