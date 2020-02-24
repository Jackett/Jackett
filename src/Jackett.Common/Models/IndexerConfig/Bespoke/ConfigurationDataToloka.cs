namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    internal class ConfigurationDataToloka : ConfigurationDataBasicLogin
    {
        public BoolItem StripCyrillicLetters { get; private set; }

        public ConfigurationDataToloka()
            : base()
        {
            StripCyrillicLetters = new BoolItem() { Name = "Strip Cyrillic Letters", Value = true };
        }
    }
}
