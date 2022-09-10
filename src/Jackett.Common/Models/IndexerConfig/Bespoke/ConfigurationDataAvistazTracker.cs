using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataAvistazTracker : ConfigurationDataBasicLoginWithPID
    {
        public BoolConfigurationItem Freeleech { get; private set; }

        public ConfigurationDataAvistazTracker()
            => Freeleech = new BoolConfigurationItem("Search freeleech only") { Value = false };
    }
}
