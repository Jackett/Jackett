namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    internal class ConfigurationDataFileList : ConfigurationDataUserPasskey
    {
        public BoolItem IncludeRomanianReleases { get; private set; }
        public DisplayItem CatWarning { get; private set; }

        public ConfigurationDataFileList()
            : base("Note this is <b>not</b> your <i>password</i>. Access your FileList account profile and copy the <b>passkey</b>.")
        {
            IncludeRomanianReleases = new BoolItem {Name = "IncludeRomanianReleases", Value = false};
            CatWarning = new DisplayItem("When mapping TV ensure you add category 5000 in addition to 5030, 5040.") {Name = "CatWarning"};
        }
    }
}
