namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    internal class ConfigurationDataEliteTracker : ConfigurationDataBasicLogin
    {
        public BoolItem TorrentHTTPSMode { get; private set; }
        public DisplayItem PagesWarning { get; private set; }
        public StringItem ReplaceMulti { get; private set; }
        public BoolItem Vostfr { get; private set; }

        public ConfigurationDataEliteTracker()
            : base()
        {
            TorrentHTTPSMode = new BoolItem { Name = "Use https for tracker URL", Value = false };
            PagesWarning = new DisplayItem("<b>Preferences Configuration</b> (<i>Tweak your search settings</i>),<br /><br /> <ul><li><b>Replace MULTI</b>, replace multi keyword in the resultset (leave empty  to deactivate)</li><li><b>Replace VOSTFR with ENGLISH</b> lets you change the titles by replacing VOSTFR with ENGLISH.</li></ul>") { Name = "Preferences" };
            ReplaceMulti = new StringItem() { Name = "Replace MULTI", Value = "MULTI.FRENCH" };
            Vostfr = new BoolItem() { Name = "Replace VOSTFR with ENGLISH", Value = false };
        }
    }
}
