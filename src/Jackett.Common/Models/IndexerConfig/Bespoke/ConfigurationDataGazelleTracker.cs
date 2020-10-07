namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    public class ConfigurationDataGazelleTracker : ConfigurationData
    {
        public StringItem Username { get; private set; }
        public StringItem Password { get; private set; }
        public StringItem ApiKey { get; private set; }
        public DisplayItem CookieHint { get; private set; }
        public StringItem CookieItem { get; private set; }
        public BoolItem UseTokenItem { get; private set; }
        public DisplayItem Instructions { get; private set; }

        public ConfigurationDataGazelleTracker(bool has2Fa = false, bool supportsFreeleechToken = false,
                                               bool useApiKey = false, string instructionMessageOptional = null)
        {
            if (useApiKey)
                ApiKey = new StringItem { Name = "APIKey" };
            else
            {
                Username = new StringItem { Name = "Username" };
                Password = new StringItem { Name = "Password" };
            }

            if (has2Fa)
            {
                CookieHint = new DisplayItem(
                    @"Use the Cookie field only if 2FA is enabled for your account, let it empty otherwise.
<ol><li>Login to this tracker with your browser
<li>Open the <b>DevTools</b> panel by pressing <b>F12</b>
<li>Select the <b>Network</b> tab
<li>Click on the <b>Doc</b> button
<li>Refresh the page by pressing <b>F5</b>
<li>Select the <b>Headers</b> tab
<li>Find 'cookie:' in the <b>Request Headers</b> section
<li>Copy & paste the whole cookie string to here.</ol>")
                {
                    Name = "CookieHint"
                };
                CookieItem = new StringItem { Name = "Cookie", Value = "" };
            }

            if (supportsFreeleechToken)
                UseTokenItem = new BoolItem { Name = "Use Freeleech Tokens when Available", Value = false };

            Instructions = new DisplayItem(instructionMessageOptional) { Name = "" };
        }

    }
}
