namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    public class ConfigurationDataAnilibria : ConfigurationData
    {
        public BoolConfigurationItem AddRussianToTitle { get; private set; }
        public BoolConfigurationItem AddSeasonToTitle { get; private set; }
        public BoolConfigurationItem EnglishTitleOnly { get; private set; }
        public ConfigurationDataAnilibria()
        {
            AddRussianToTitle = new BoolConfigurationItem("Add RUS to end of all titles to improve language detection by Sonarr. Will cause English-only results to be misidentified.") { Value = false };
            AddSeasonToTitle = new BoolConfigurationItem("Improve Sonarr compatibility by trying to better parse Season information in release titles.") { Value = false };
            EnglishTitleOnly = new BoolConfigurationItem("Improve Sonarr compatibility by returning the English Title instead of the Russian title.") { Value = false };
        }
    }
}
