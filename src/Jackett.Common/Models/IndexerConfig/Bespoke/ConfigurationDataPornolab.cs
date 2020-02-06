namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    internal class ConfigurationDataPornolab : ConfigurationDataCaptchaLogin
    {
        public BoolItem StripRussianLetters { get; private set; }

        public ConfigurationDataPornolab() =>
            StripRussianLetters = new BoolItem { Name = "Strip Russian Letters", Value = false };
    }
}
