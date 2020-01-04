namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    class ConfigurationDataEliteTracker : ConfigurationDataBasicLogin
    {
        public BoolItem TorrentHTTPSMode { get; private set; }
        public DisplayItem PagesWarning { get; private set; }
        public StringItem ReplaceMulti { get; private set; }

        public ConfigurationDataEliteTracker()
            : base()
        {
            TorrentHTTPSMode = new BoolItem { Name = "Use https for tracker URL", Value = false };
            PagesWarning = new DisplayItem("<b>Preferences Configuration</b> (<i>Tweak your search settings</i>),<br /><br /> <ul><li><b>Replace MULTI</b>, replace multi keyword in the resultset (leave empty  to deactivate)</li></ul>") { Name = "Preferences" };
            ReplaceMulti = new StringItem() { Name = "Replace MULTI", Value = "MULTI.FRENCH" };
        }
    }
}
