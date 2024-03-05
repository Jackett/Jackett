using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataSpeedAppTracker : ConfigurationDataBasicLoginWithEmail
    {
        public BoolConfigurationItem FreeleechOnly { get; private set; }
        public DisplayInfoConfigurationItem AccountActivity { get; private set; }

        public ConfigurationDataSpeedAppTracker()
        {
            FreeleechOnly = new BoolConfigurationItem("Show freeleech only") { Value = false };
            AccountActivity = new DisplayInfoConfigurationItem("Account Inactivity", "The accounts shall not be deleted or automatically deactivated. Inactive accounts, regardless of class, will have the invitations deleted after 180 days.");
        }
    }
}
