namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    class ConfigurationDataAnimeBytes : ConfigurationDataCaptchaLogin
    {
        public BoolItem IncludeRaw { get; private set; }
        public DisplayItem DateWarning { get; private set; }
        public BoolItem InsertSeason { get; private set; }
        public BoolItem AddSynonyms { get; private set; }
        public BoolItem FilterSeasonEpisode { get; private set; }

        public ConfigurationDataAnimeBytes()
            : base()
        {
            IncludeRaw = new BoolItem() { Name = "IncludeRaw", Value = false };
            DateWarning = new DisplayItem("This tracker does not supply upload dates so they are based off year of release.") { Name = "DateWarning" };
            InsertSeason = new BoolItem() { Name = "Prefix episode number with E0 for Sonarr Compatability", Value = false };
            AddSynonyms = new BoolItem() { Name = "Add releases for each synonym title", Value = true };
            FilterSeasonEpisode = new BoolItem() { Name = "Filter results by season/episode", Value = false };
        }
    }
}
