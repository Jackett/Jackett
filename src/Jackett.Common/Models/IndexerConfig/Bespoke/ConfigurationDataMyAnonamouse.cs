namespace Jackett.Common.Models.IndexerConfig.Bespoke
{

    public class ConfigurationDataMyAnonamouse : ConfigurationData
    {
        public StringItem MamId { get; private set; }
        public DisplayItem MamIdHint { get; private set; }
        public BoolItem ExcludeVip { get; private set; }
        public DisplayItem Instructions { get; private set; }

        public ConfigurationDataMyAnonamouse()
        {
            MamId = new StringItem { Name = "mam_id" };
            MamIdHint = new DisplayItem(
            "Go to your <a href=\"https://www.myanonamouse.net/preferences/index.php?view=security\" target=\"_blank\">security preferences</a> and create a new session for the IP used by the Jackett server. Then paste the resulting mam_id value into the mam_id field here.")
            {
                Name = "mam_id instructions"
            };
            ExcludeVip = new BoolItem { Name = "Exclude VIP torrents" };
            Instructions = new DisplayItem("For best results, change the 'Torrents per page' setting to 100 in your Profile => Torrent tab.") { Name = "" };
        }
    }

}
