namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    public class ConfigurationDataMegaPeer : ConfigurationData
    {
        public BoolConfigurationItem AddRussianToTitle { get; private set; }
        public BoolConfigurationItem AddCategories { get; private set; }
        public BoolConfigurationItem EnglishTitleOnly { get; private set; }
        public ConfigurationDataMegaPeer()
        {
            AddRussianToTitle = new BoolConfigurationItem("Add RUS to end of all titles to improve language detection by Sonarr. Will cause English-only results to be misidentified.") { Value = false };
            EnglishTitleOnly = new BoolConfigurationItem("Improve Sonarr compatibility by returning the English Title instead of the Russian title.") { Value = false };
            AddCategories = new BoolConfigurationItem("Add category to search results. Enabling this option will increase the number of requests to the tracker and the processing time") { Value = false };
        }
    }
}
