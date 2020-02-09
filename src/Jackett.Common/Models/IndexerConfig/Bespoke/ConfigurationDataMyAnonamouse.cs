namespace Jackett.Common.Models.IndexerConfig
{

    public class ConfigurationDataMyAnonamouse : ConfigurationData
    {
        public StringItem MamId { get; private set; }
        public DisplayItem MamIdHint { get; private set; }

        public ConfigurationDataMyAnonamouse()
        {
            MamId = new StringItem { Name = "mam_id" };
            MamIdHint = new DisplayItem(
            "Go to your <a href=\"https://www.myanonamouse.net/preferences/index.php?view=security\" target=\"_blank\">security preferences</a> and create a new session for the IP used by the Jackett server. Then paste the resulting mam_id value into the mam_id field here.")
            {
                Name = "mam_id instructions"
            };
        }
    }

}
