using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataNorbits : ConfigurationData
    {
        public DisplayInfoConfigurationItem CredentialsWarning { get; private set; }
        public StringConfigurationItem Username { get; private set; }
        public StringConfigurationItem Password { get; private set; }
        public DisplayInfoConfigurationItem PagesWarning { get; private set; }
        public StringConfigurationItem Pages { get; private set; }
        public BoolConfigurationItem UseFullSearch { get; private set; }
        public DisplayInfoConfigurationItem SecurityWarning { get; private set; }
        public BoolConfigurationItem Latency { get; private set; }
        public BoolConfigurationItem Browser { get; private set; }
        public DisplayInfoConfigurationItem LatencyWarning { get; private set; }
        public StringConfigurationItem LatencyStart { get; private set; }
        public StringConfigurationItem LatencyEnd { get; private set; }
        public DisplayInfoConfigurationItem HeadersWarning { get; private set; }
        public StringConfigurationItem HeaderAccept { get; private set; }
        public StringConfigurationItem HeaderAcceptLang { get; private set; }
        public BoolConfigurationItem HeaderDnt { get; private set; }
        public BoolConfigurationItem HeaderUpgradeInsecure { get; private set; }
        public StringConfigurationItem HeaderUserAgent { get; private set; }
        public DisplayInfoConfigurationItem DevWarning { get; private set; }
        public BoolConfigurationItem DevMode { get; private set; }
        public BoolConfigurationItem HardDriveCache { get; private set; }
        public StringConfigurationItem HardDriveCacheKeepTime { get; private set; }

        public ConfigurationDataNorbits()
        {
            CredentialsWarning = new DisplayInfoConfigurationItem("Credentials", "<b>Credentials Configuration</b> (<i>Private Tracker</i>),<br /><br /> <ul><li><b>Username</b> is your account name on this tracker.</li><li><b>Password</b> is your password associated to your account name.</li></ul>");
            Username = new StringConfigurationItem("Username") { Value = "" };
            Password = new StringConfigurationItem("Password") { Value = "" };
            PagesWarning = new DisplayInfoConfigurationItem("Preferences", "<b>Preferences Configuration</b> (<i>Tweak your search settings</i>),<br /><br /> <ul><li><b>Max Pages to Process</b> let you specify how many page (max) Jackett can process when doing a search. Setting a value <b>higher than 4 is dangerous</b> for you account ! (<b>Result of too many requests to tracker...that <u>will be suspect</u></b>).</li></ul>");
            Pages = new StringConfigurationItem("Max Pages to Process (Required)") { Value = "4" };
            UseFullSearch = new BoolConfigurationItem("Enable search in description.") { Value = false };
            SecurityWarning = new DisplayInfoConfigurationItem("Security", "<b>Security Configuration</b> (<i>Read this area carefully !</i>),<br /><br /> <ul><li><b>Latency Simulation</b> will simulate human browsing with Jacket by pausing Jacket for an random time between each request, to fake a real content browsing.</li><li><b>Browser Simulation</b> will simulate a real human browser by injecting additionals headers when doing requests to tracker.<b>You must enable it to use this provider!</b></li></ul>");
            Latency = new BoolConfigurationItem("Latency Simulation (Optional)") { Value = false };
            Browser = new BoolConfigurationItem("Browser Simulation (Forced)") { Value = true };
            LatencyWarning = new DisplayInfoConfigurationItem("Simulate Latency", "<b>Latency Configuration</b> (<i>Required if latency simulation enabled</i>),<br /><br/> <ul><li>By filling this range, <b>Jackett will make a random timed pause</b> <u>between requests</u> to tracker <u>to simulate a real browser</u>.</li><li>MilliSeconds <b>only</b></li></ul>");
            LatencyStart = new StringConfigurationItem("Minimum Latency (ms)") { Value = "1589" };
            LatencyEnd = new StringConfigurationItem("Maximum Latency (ms)") { Value = "3674" };
            HeadersWarning = new DisplayInfoConfigurationItem("Injecting headers", "<b>Browser Headers Configuration</b> (<i>Required if browser simulation enabled</i>),<br /><br /> <ul><li>By filling these fields, <b>Jackett will inject headers</b> with your values <u>to simulate a real browser</u>.</li><li>You can get <b>your browser values</b> here: <a href='https://www.whatismybrowser.com/detect/what-http-headers-is-my-browser-sending' target='blank'>www.whatismybrowser.com</a></li></ul><br /><i><b>Note that</b> some headers are not necessary because they are injected automatically by this provider such as Accept_Encoding, Connection, Host or X-Requested-With</i>");
            HeaderAccept = new StringConfigurationItem("Accept") { Value = "" };
            HeaderAcceptLang = new StringConfigurationItem("Accept-Language") { Value = "" };
            HeaderDnt = new BoolConfigurationItem("DNT") { Value = false };
            HeaderUpgradeInsecure = new BoolConfigurationItem("Upgrade-Insecure-Requests") { Value = false };
            HeaderUserAgent = new StringConfigurationItem("User-Agent") { Value = "" };
            DevWarning = new DisplayInfoConfigurationItem("Development", "<b>Development Facility</b> (<i>For Developers ONLY</i>),<br /><br /> <ul><li>By enabling development mode, <b>Jackett will bypass his cache</b> and will <u>output debug messages to console</u> instead of his log file.</li><li>By enabling Hard Drive Cache, <b>This provider</b> will <u>save each query answers from tracker</u> in temp directory, in fact this reduce drastically HTTP requests when building a provider at parsing step for example. So, <b> Jackett will search for a cached query answer on hard drive before executing query on tracker side !</b> <i>DEV MODE must be enabled to use it !</li></ul>");
            DevMode = new BoolConfigurationItem("Enable DEV MODE (Developers ONLY)") { Value = false };
            HardDriveCache = new BoolConfigurationItem("Enable HARD DRIVE CACHE (Developers ONLY)") { Value = false };
            HardDriveCacheKeepTime = new StringConfigurationItem("Keep Cached files for (ms)") { Value = "300000" };
        }
    }
}
