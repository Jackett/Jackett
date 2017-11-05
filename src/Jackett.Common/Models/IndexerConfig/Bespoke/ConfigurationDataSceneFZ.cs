namespace Jackett.Models.IndexerConfig.Bespoke
{
    class ConfigurationDataSceneFZ : ConfigurationData
    {
        public DisplayItem CredentialsWarning { get; private set; }
        public StringItem Username { get; private set; }
        public StringItem Password { get; private set; }
        public DisplayItem PagesWarning { get; private set; }
        //public StringItem Pages { get; private set; }
        public BoolItem Dead { get; private set; }
        public DisplayItem SecurityWarning { get; private set; }
        public BoolItem Latency { get; private set; }
        public BoolItem Browser { get; private set; }
        public DisplayItem LatencyWarning { get; private set; }
        public StringItem LatencyStart { get; private set; }
        public StringItem LatencyEnd { get; private set; }
        public DisplayItem HeadersWarning { get; private set; }
        public StringItem HeaderAccept { get; private set; }
        public StringItem HeaderAcceptLang { get; private set; }
        public BoolItem HeaderDNT { get; private set; }
        public BoolItem HeaderUpgradeInsecure { get; private set; }
        public StringItem HeaderUserAgent { get; private set; }
        public DisplayItem DevWarning { get; private set; }
        public BoolItem DevMode { get; private set; }
        public BoolItem HardDriveCache { get; private set; }
        public StringItem HardDriveCacheKeepTime { get; private set; }

        public ConfigurationDataSceneFZ()
            : base()
        {
            CredentialsWarning = new DisplayItem("<b>Credentials Configuration</b> (<i>Private Tracker</i>),<br /><br /> <ul><li><b>Username</b> is your account name on this tracker.</li><li><b>Password</b> is your password associated to your account name.</li></ul>") { Name = "Credentials" };
            Username = new StringItem { Name = "Username (Required)", Value = "" };
            Password = new StringItem { Name = "Password (Required)", Value = "" };
            PagesWarning = new DisplayItem("<b>Preferences Configuration</b> (<i>Tweak your search settings</i>),<br /><br /> <ul><li><b>Include Dead</b> let you search <u>including</u> torrents which are marked as Dead.</li></ul>") { Name = "Preferences" };
            //Pages = new StringItem { Name = "Max Pages to Process (Required)", Value = "4" };
            Dead = new BoolItem() { Name = "Include Dead (Optional)", Value = false };
            SecurityWarning = new DisplayItem("<b>Security Configuration</b> (<i>Read this area carefully !</i>),<br /><br /> <ul><li><b>Latency Simulation</b> will simulate human browsing with Jacket by pausing Jacket for an random time between each request, to fake a real content browsing.</li><li><b>Browser Simulation</b> will simulate a real human browser by injecting additionals headers when doing requests to tracker.</li></ul>") { Name = "Security" };
            Latency = new BoolItem() { Name = "Latency Simulation (Optional)", Value = false };
            Browser = new BoolItem() { Name = "Browser Simulation (Optional)", Value = false };
            LatencyWarning = new DisplayItem("<b>Latency Configuration</b> (<i>Required if latency simulation enabled</i>),<br /><br/> <ul><li>By filling this range, <b>Jackett will make a random timed pause</b> <u>between requests</u> to tracker <u>to simulate a real browser</u>.</li><li>MilliSeconds <b>only</b></li></ul>") { Name = "Simulate Latency" };
            LatencyStart = new StringItem { Name = "Minimum Latency (ms)", Value = "1589" };
            LatencyEnd = new StringItem { Name = "Maximum Latency (ms)", Value = "3674" };
            HeadersWarning = new DisplayItem("<b>Browser Headers Configuration</b> (<i>Required if browser simulation enabled</i>),<br /><br /> <ul><li>By filling these fields, <b>Jackett will inject headers</b> with your values <u>to simulate a real browser</u>.</li><li>You can get <b>your browser values</b> here: <a href='https://www.whatismybrowser.com/detect/what-http-headers-is-my-browser-sending' target='blank'>www.whatismybrowser.com</a></li></ul><br /><i><b>Note that</b> some headers are not necessary because they are injected automatically by this provider such as Accept_Encoding, Connection, Host or X-Requested-With</i>") { Name = "Injecting headers" };
            HeaderAccept = new StringItem { Name = "Accept", Value = "" };
            HeaderAcceptLang = new StringItem { Name = "Accept-Language", Value = "" };
            HeaderDNT = new BoolItem { Name = "DNT", Value = false };
            HeaderUpgradeInsecure = new BoolItem { Name = "Upgrade-Insecure-Requests", Value = false };
            HeaderUserAgent = new StringItem { Name = "User-Agent", Value = "" };
            DevWarning = new DisplayItem("<b>Development Facility</b> (<i>For Developers ONLY</i>),<br /><br /> <ul><li>By enabling development mode, <b>Jackett will bypass his cache</b> and will <u>output debug messages to console</u> instead of his log file.</li><li>By enabling Hard Drive Cache, <b>This provider</b> will <u>save each query answers from tracker</u> in temp directory, in fact this reduce drastically HTTP requests when building a provider at parsing step for example. So, <b> Jackett will search for a cached query answer on hard drive before executing query on tracker side !</b> <i>DEV MODE must be enabled to use it !</li></ul>") { Name = "Development" };
            DevMode = new BoolItem { Name = "Enable DEV MODE (Developers ONLY)", Value = true };
            HardDriveCache = new BoolItem { Name = "Enable HARD DRIVE CACHE (Developers ONLY)", Value = true };
            HardDriveCacheKeepTime = new StringItem { Name = "Keep Cached files for (ms)", Value = "300000" };
        }
    }
}
