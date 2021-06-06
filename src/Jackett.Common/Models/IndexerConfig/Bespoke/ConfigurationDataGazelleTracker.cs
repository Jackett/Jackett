using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataGazelleTracker : ConfigurationData
    {
        public StringConfigurationItem Username { get; private set; }
        public PasswordConfigurationItem Password { get; private set; }
        public StringConfigurationItem ApiKey { get; private set; }
        public StringConfigurationItem PassKey { get; private set; }
        public DisplayInfoConfigurationItem CookieHint { get; private set; }
        public StringConfigurationItem CookieItem { get; private set; }
        public BoolConfigurationItem UseTokenItem { get; private set; }
        public DisplayInfoConfigurationItem Instructions { get; private set; }

        public ConfigurationDataGazelleTracker(bool has2Fa = false, bool supportsFreeleechToken = false,
                                               bool useApiKey = false, bool usePassKey = false,
                                               string instructionMessageOptional = null)
        {
            if (useApiKey)
                ApiKey = new StringConfigurationItem("APIKey");
            else
            {
                Username = new StringConfigurationItem("Username");
                Password = new PasswordConfigurationItem("Password");
            }

            if (has2Fa)
            {
                CookieHint = new DisplayInfoConfigurationItem("CookieHint",
                    @"Use the Cookie field only if 2FA is enabled for your account, let it empty otherwise.
<ol><li>Login to this tracker with your browser
<li>Open the <b>DevTools</b> panel by pressing <b>F12</b>
<li>Select the <b>Network</b> tab
<li>Click on the <b>Doc</b> button
<li>Refresh the page by pressing <b>F5</b>
<li>Select the <b>Headers</b> tab
<li>Find 'cookie:' in the <b>Request Headers</b> section
<li>Copy & paste the whole cookie string to here.</ol>");
                CookieItem = new StringConfigurationItem("Cookie") { Value = "" };
            }

            if (usePassKey)
                PassKey = new StringConfigurationItem("Passkey");

            if (supportsFreeleechToken)
                UseTokenItem = new BoolConfigurationItem("Use Freeleech Tokens when Available") { Value = false };

            Instructions = new DisplayInfoConfigurationItem("", instructionMessageOptional);
        }
    }
}
