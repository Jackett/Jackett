namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    class ConfigurationDataEliteTracker : ConfigurationDataBasicLogin
    {
        public BoolItem TorrentHTTPSMode { get; private set; }

        public ConfigurationDataEliteTracker()
            : base()
        {
            TorrentHTTPSMode = new BoolItem { Name = "Use https for tracker URL (Experimental)", Value = false };
        }
    }
}
