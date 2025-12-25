using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataAvistaZTracker : ConfigurationDataBasicLoginWithPID
    {
        [JsonPropertyOrder(5)]
        public BoolConfigurationItem Freeleech { get; private set; }

        [JsonPropertyOrder(6)]
        public DisplayInfoConfigurationItem AccountActivity { get; private set; }

        public ConfigurationDataAvistaZTracker()
            : base("You have to check 'Enable RSS Feed' in 'My Account', without this configuration the torrent download does not work.<br/>You can find the PID in 'My profile'.")
        {
            Freeleech = new BoolConfigurationItem("Search freeleech only") { Value = false };
            AccountActivity = new DisplayInfoConfigurationItem("Account Inactivity", "To avoid account deletion you must login at least 1 time every 60 days, and you must download at least 1 torrent every 90 days. Simply keeping torrents seeding long term will not protect your account. Do not rely on inactivity emails, we often do not send them.");
        }
    }
}
