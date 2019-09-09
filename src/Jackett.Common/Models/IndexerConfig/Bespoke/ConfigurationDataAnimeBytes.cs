namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    class ConfigurationDataAnimeBytes : ConfigurationDataUserPasskey
    {
        public BoolItem IncludeRaw { get; private set; }
        //public DisplayItem DateWarning { get; private set; }
        public BoolItem PadEpisode { get; private set; }
        public BoolItem AddSynonyms { get; private set; }
        public BoolItem FilterSeasonEpisode { get; private set; }

        public ConfigurationDataAnimeBytes(string instructionMessageOptional = null)
            : base()
        {
            IncludeRaw = new BoolItem() { Name = "IncludeRaw", Value = false };
            //DateWarning = new DisplayItem("This tracker does not supply upload dates so they are based off year of release.") { Name = "DateWarning" };
            PadEpisode = new BoolItem() { Name = "Pad episode number for Sonarr compatability", Value = false };
            AddSynonyms = new BoolItem() { Name = "Add releases for each synonym title", Value = true };
            FilterSeasonEpisode = new BoolItem() { Name = "Filter results by season/episode", Value = false };
            Instructions = new DisplayItem(instructionMessageOptional) { Name = "" };
        }
    }
}
