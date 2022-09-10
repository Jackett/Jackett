using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataAvistazTracker : ConfigurationDataBasicLoginWithPID
    {
        public BoolConfigurationItem Freeleech { get; private set; }

        public ConfigurationDataAvistazTracker()
            : base("You have to check 'Enable RSS Feed' in 'My Account', without this configuration the torrent download does not work.<br/>You can find the PID in 'My profile'.")
        {
            Freeleech = new BoolConfigurationItem("Search freeleech only") { Value = false };
        }
    }
}
