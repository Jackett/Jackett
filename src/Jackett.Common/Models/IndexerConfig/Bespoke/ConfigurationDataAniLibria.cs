namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    internal class ConfigurationDataAniLibria : ConfigurationData
    {
        public StringItem ApiLink { get; private set; }
        public StringItem StaticLink { get; private set; }

        public ConfigurationDataAniLibria() : base()
        {
            ApiLink = new StringItem
            {
                Name = "API Url",
                Value = "https://api.anilibria.tv/v2/"
            };
            StaticLink = new StringItem
            {
                Name = "Static Url",
                Value = "https://static.anilibria.tv/"
            };
        }
    }
}
