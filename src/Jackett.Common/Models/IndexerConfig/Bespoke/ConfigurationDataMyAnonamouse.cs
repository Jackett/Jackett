using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataMyAnonamouse : ConfigurationData
    {
        public StringConfigurationItem MamId { get; private set; }
        public DisplayInfoConfigurationItem MamIdHint { get; private set; }
        public SingleSelectConfigurationItem SearchType { get; private set; }
        public BoolConfigurationItem SearchInDescription { get; private set; }
        public BoolConfigurationItem SearchInSeries { get; private set; }
        public BoolConfigurationItem SearchInFilenames { get; private set; }
        public DisplayInfoConfigurationItem AccountActivity { get; private set; }

        public ConfigurationDataMyAnonamouse()
        {
            MamId = new StringConfigurationItem("mam_id");
            MamIdHint = new DisplayInfoConfigurationItem("mam_id instructions", "Go to your <a href=\"https://www.myanonamouse.net/preferences/index.php?view=security\" target=\"_blank\">security preferences</a> and create a new session for the IP used by the Jackett server. Then paste the resulting mam_id value into the mam_id field here.");
            SearchType = new SingleSelectConfigurationItem(
                "Search Type",
                new Dictionary<string, string>
                {
                    { "all", "All torrents" },
                    { "active", "Only active" },
                    { "fl", "Freeleech" },
                    { "fl-VIP", "Freeleech or VIP" },
                    { "VIP", "VIP torrents" },
                    { "nVIP", "Torrents not VIP" },
                })
            { Value = "all" };
            SearchInDescription = new BoolConfigurationItem("Also search text in the description") { Value = false };
            SearchInSeries = new BoolConfigurationItem("Also search text in the series") { Value = false };
            SearchInFilenames = new BoolConfigurationItem("Also search text in the filenames") { Value = false };
            AccountActivity = new DisplayInfoConfigurationItem("Account Inactivity", "To prevent your account from being disabled for inactivity, you must log in on a regular basis. You must also use your account - if you do not, your account will be disabled. If you know that you will not be able to login for an extended period of time, you can park your account in your preferences and it will not be disabled.");
        }
    }
}
