namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationGazelleTracker : ConfigurationData
    {
        public DisplayItem Instructions { get; private set; }
        public StringItem Username { get; private set; }
        public StringItem Password { get; private set; }
        public StringItem ApiKey { get; private set; }
        public DisplayItem CookieHint { get; private set; }
        public StringItem CookieItem { get; private set; }
        public BoolItem UseTokenItem { get; private set; }
        public bool UseAPIKey { get; private set; }
        public bool SupportsFreeleechToken { get; private set; }
        public bool Has2Fa { get; private set; }

        public ConfigurationGazelleTracker(string instructionMessageOptional = null, bool has2Fa = false, bool supportsFreeleechToken = false, bool useApiKey = false)
        {
            Has2Fa = has2Fa;
            SupportsFreeleechToken = supportsFreeleechToken;
            UseAPIKey = useApiKey;

            if (useApiKey)
                ApiKey = new StringItem { Name = "APIKey" };
            else
            {
                Username = new StringItem { Name = "Username" };
                Password = new StringItem { Name = "Password" };
            }

            if (has2Fa)
            {
                CookieHint = new ConfigurationData.DisplayItem(
                "<ol><li>(use this only if 2FA is enabled for your account)</li><li>Login to this tracker with your browser<li>Open the <b>DevTools</b> panel by pressing <b>F12</b><li>Select the <b>Network</b> tab<li>Click on the <b>Doc</b> button<li>Refresh the page by pressing <b>F5</b><li>Select the <b>Headers</b> tab<li>Find 'cookie:' in the <b>Request Headers</b> section<li>Copy & paste the whole cookie string to here.</ol>")
                {
                    Name = "CookieHint"
                };
                CookieItem = new ConfigurationData.StringItem { Name = "Cookie", Value = "" };                
            }


            if (supportsFreeleechToken)
                UseTokenItem = new ConfigurationData.BoolItem { Name = "Use Freeleech Tokens when Available", Value = false };

            Instructions = new DisplayItem(instructionMessageOptional) { Name = "" };
        }


    }
}
