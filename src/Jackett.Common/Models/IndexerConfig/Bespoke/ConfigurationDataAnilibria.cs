namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    public class ConfigurationDataAnilibria : ConfigurationData
    {
        public BoolConfigurationItem AddRussianToTitle { get; private set; }
        public ConfigurationDataAnilibria()
        {
            AddRussianToTitle = new BoolConfigurationItem("Add RUS to end of all titles to improve language detection by Sonarr and Radarr. Will cause English-only results to be misidentified.") { Value = false };

        }
    }
}
