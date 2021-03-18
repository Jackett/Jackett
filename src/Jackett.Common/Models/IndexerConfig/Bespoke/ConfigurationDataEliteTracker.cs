using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataEliteTracker : ConfigurationDataBasicLogin
    {
        public BoolConfigurationItem TorrentHTTPSMode { get; }
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public DisplayInfoConfigurationItem PagesWarning { get; }
        public StringConfigurationItem ReplaceMulti { get; }
        public BoolConfigurationItem Vostfr { get; }

        public ConfigurationDataEliteTracker()
        {
            TorrentHTTPSMode = new BoolConfigurationItem("Use HTTPS for tracker URL") { Value = false };
            PagesWarning = new DisplayInfoConfigurationItem("Preferences", "<b>Preferences Configuration</b> (<i>Tweak your search settings</i>),<br /><br /> <ul><li><b>Replace MULTI</b>, replace multi keyword in the resultset (leave empty  to deactivate)</li><li><b>Replace VOSTFR with ENGLISH</b> lets you change the titles by replacing VOSTFR with ENGLISH.</li></ul>");
            ReplaceMulti = new StringConfigurationItem("Replace MULTI") { Value = "MULTI.FRENCH" };
            Vostfr = new BoolConfigurationItem("Replace VOSTFR with ENGLISH") { Value = false };
        }
    }
}
