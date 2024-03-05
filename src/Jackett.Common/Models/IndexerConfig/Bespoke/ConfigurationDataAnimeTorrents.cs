using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataAnimeTorrents : ConfigurationDataBasicLogin
    {
        public BoolConfigurationItem FreeleechOnly { get; private set; }
        public BoolConfigurationItem DownloadableOnly { get; private set; }
        public DisplayInfoConfigurationItem AccountActivity { get; private set; }

        public ConfigurationDataAnimeTorrents()
        {
            FreeleechOnly = new BoolConfigurationItem("Show freeleech only") { Value = false };
            DownloadableOnly = new BoolConfigurationItem("Search downloadable torrents only (enable this only if your account class is Newbie)") { Value = false };
            AccountActivity = new DisplayInfoConfigurationItem("Account Inactivity", "You must login and download at least 1 torrent per year or your account will be deleted. There is no re-activation. AnimeTorrents do not send email reminders, it is your responsibility.");
        }
    }
}
