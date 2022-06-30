using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataMyAnonamouse : ConfigurationData
    {
        public StringConfigurationItem MamId { get; private set; }
        public DisplayInfoConfigurationItem MamIdHint { get; private set; }
        public BoolConfigurationItem ExcludeVip { get; private set; }
        public DisplayInfoConfigurationItem Instructions { get; private set; }

        public ConfigurationDataMyAnonamouse()
        {
            MamId = new StringConfigurationItem("mam_id");
            MamIdHint = new DisplayInfoConfigurationItem("mam_id instructions", "Go to your <a href=\"https://www.myanonamouse.net/preferences/index.php?view=security\" target=\"_blank\">security preferences</a> and create a new session for the IP used by the Jackett server. Then paste the resulting mam_id value into the mam_id field here.");
            ExcludeVip = new BoolConfigurationItem("Exclude VIP torrents");
            Instructions = new DisplayInfoConfigurationItem("", "For best results, change the 'Torrents per page' setting to 100 in your Profile => Torrent tab.");
        }
    }

}
