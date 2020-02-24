namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    internal class ConfigurationDataPornolab : ConfigurationDataCaptchaLogin
    {
        public BoolItem StripRussianLetters { get; private set; }

        public ConfigurationDataPornolab()
            : base()
        {
            StripRussianLetters = new BoolItem() { Name = "Strip Russian Letters", Value = false };
        }
    }
}
