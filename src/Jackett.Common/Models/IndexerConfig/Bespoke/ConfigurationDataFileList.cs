namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    internal class ConfigurationDataFileList : ConfigurationDataUserPasskey
    {
        public BoolItem IncludeRomanianReleases { get; private set; }
        public DisplayItem CatWarning { get; private set; }

        public ConfigurationDataFileList()
            : base("Go into your filelist profile and copy the passkey.")
        {
            IncludeRomanianReleases = new BoolItem {Name = "IncludeRomanianReleases", Value = false};
            CatWarning = new DisplayItem("When mapping TV ensure you add category 5000 in addition to 5030, 5040.") {Name = "CatWarning"};
        }
    }
}
