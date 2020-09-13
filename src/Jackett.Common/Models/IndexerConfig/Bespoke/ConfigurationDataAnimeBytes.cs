namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    internal class ConfigurationDataAnimeBytes : ConfigurationDataUserPasskey
    {
        public BoolItem IncludeRaw { get; private set; }
        //public DisplayItem DateWarning { get; private set; }
        public BoolItem PadEpisode { get; private set; }
        public BoolItem AddJapaneseTitle { get; private set; }
        public BoolItem AddRomajiTitle { get; private set; }
        public BoolItem AddAlternativeTitles { get; private set; }
        public BoolItem FilterSeasonEpisode { get; private set; }

        public ConfigurationDataAnimeBytes(string instructionMessageOptional = null)
            : base()
        {
            IncludeRaw = new BoolItem() { Name = "IncludeRaw", Value = false };
            //DateWarning = new DisplayItem("This tracker does not supply upload dates so they are based off year of release.") { Name = "DateWarning" };
            PadEpisode = new BoolItem() { Name = "Pad episode number for Sonarr compatability", Value = false };
            AddJapaneseTitle = new BoolItem() { Name = "Add releases for Japanese Title", Value = false };
            AddRomajiTitle = new BoolItem() { Name = "Add releases for Romaji Title", Value = false };
            AddAlternativeTitles = new BoolItem() { Name = "Add releases for Alternative Title(s)", Value = false };
            FilterSeasonEpisode = new BoolItem() { Name = "Filter results by season/episode", Value = false };
            Instructions = new DisplayItem(instructionMessageOptional) { Name = "" };
        }
    }
}
