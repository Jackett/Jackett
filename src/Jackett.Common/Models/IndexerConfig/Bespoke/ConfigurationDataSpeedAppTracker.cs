using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataSpeedAppTracker : ConfigurationDataBasicLoginWithEmail
    {
        public BoolConfigurationItem FreeleechOnly { get; private set; }

        public ConfigurationDataSpeedAppTracker()
        {
            FreeleechOnly = new BoolConfigurationItem("Show freeleech only") { Value = false };
        }
    }
}
