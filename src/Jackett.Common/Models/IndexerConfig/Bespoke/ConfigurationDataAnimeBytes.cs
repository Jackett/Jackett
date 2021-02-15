using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataAnimeBytes : ConfigurationDataUserPasskey
    {
        public BoolConfigurationItem IncludeRaw { get; private set; }
        //public DisplayItem DateWarning { get; private set; }
        public BoolConfigurationItem PadEpisode { get; private set; }
        public BoolConfigurationItem AddJapaneseTitle { get; private set; }
        public BoolConfigurationItem AddRomajiTitle { get; private set; }
        public BoolConfigurationItem AddAlternativeTitles { get; private set; }
        public BoolConfigurationItem FilterSeasonEpisode { get; private set; }

        public ConfigurationDataAnimeBytes(string instructionMessageOptional = null)
            : base()
        {
            IncludeRaw = new BoolConfigurationItem("IncludeRaw") { Value = false };
            //DateWarning = new DisplayItem("This tracker does not supply upload dates so they are based off year of release.") { Name = "DateWarning" };
            PadEpisode = new BoolConfigurationItem("Pad episode number for Sonarr compatability") { Value = false };
            AddJapaneseTitle = new BoolConfigurationItem("Add releases for Japanese Title") { Value = false };
            AddRomajiTitle = new BoolConfigurationItem("Add releases for Romaji Title") { Value = false };
            AddAlternativeTitles = new BoolConfigurationItem("Add releases for Alternative Title(s)") { Value = false };
            FilterSeasonEpisode = new BoolConfigurationItem("Filter results by season/episode") { Value = false };
            Instructions = new DisplayInfoConfigurationItem("", instructionMessageOptional);
        }
    }
}
