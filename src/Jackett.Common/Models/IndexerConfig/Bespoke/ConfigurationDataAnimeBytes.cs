using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataAnimeBytes : ConfigurationDataUserPasskey
    {
        public BoolConfigurationItem FreeleechOnly { get; private set; }
        public BoolConfigurationItem ExcludeHentai { get; private set; }
        public BoolConfigurationItem IncludeRaw { get; private set; }
        public BoolConfigurationItem SearchByYear { get; private set; }
        //public DisplayItem DateWarning { get; private set; }
        public BoolConfigurationItem PadEpisode { get; private set; }
        public BoolConfigurationItem AddJapaneseTitle { get; private set; }
        public BoolConfigurationItem AddRomajiTitle { get; private set; }
        public BoolConfigurationItem AddAlternativeTitles { get; private set; }
        public BoolConfigurationItem AddFileNameTitles { get; private set; }
        public BoolConfigurationItem FilterSeasonEpisode { get; private set; }

        public ConfigurationDataAnimeBytes(string instructionMessageOptional = null)
            : base(instructionMessageOptional)
        {
            FreeleechOnly = new BoolConfigurationItem("Search freeleech only") { Value = false };
            ExcludeHentai = new BoolConfigurationItem("Exclude Hentai from results") { Value = false };
            IncludeRaw = new BoolConfigurationItem("Include RAW in results") { Value = false };
            SearchByYear = new BoolConfigurationItem("Search by year as a different argument in the request") { Value = false };
            //DateWarning = new DisplayItem("This tracker does not supply upload dates so they are based off year of release.") { Name = "DateWarning" };
            PadEpisode = new BoolConfigurationItem("Pad episode number for Sonarr compatability") { Value = false };
            AddJapaneseTitle = new BoolConfigurationItem("Add releases for Japanese Title") { Value = false };
            AddRomajiTitle = new BoolConfigurationItem("Add releases for Romaji Title") { Value = false };
            AddAlternativeTitles = new BoolConfigurationItem("Add releases for Alternative Title(s)") { Value = false };
            AddFileNameTitles = new BoolConfigurationItem("Add releases based on single filename") { Value = false };
            FilterSeasonEpisode = new BoolConfigurationItem("Filter results by season/episode") { Value = false };
        }
    }
}
