namespace Jackett.Models.IndexerConfig.Bespoke
{
    class ConfigurationDataXthor : ConfigurationData
    {
        public DisplayItem CredentialsWarning { get; private set; }
        public StringItem PassKey { get; set; }
        public DisplayItem PagesWarning { get; private set; }
        public BoolItem Freeleech { get; private set; }
        public DisplayItem DevWarning { get; private set; }
        public BoolItem DevMode { get; private set; }
        public BoolItem HardDriveCache { get; private set; }
        public StringItem HardDriveCacheKeepTime { get; private set; }

        public ConfigurationDataXthor()
            : base()
        {
            CredentialsWarning = new DisplayItem("<b>Credentials Configuration</b> (<i>Private Tracker</i>),<br /><br /> <ul><li><b>PassKey</b> is your private key on your account</li></ul>") { Name = "Credentials" };
            PassKey = new StringItem { Name = "PassKey", Value = "" };
            PagesWarning = new DisplayItem("<b>Preferences Configuration</b> (<i>Tweak your search settings</i>),<br /><br /> <ul><li><b>Freeleech Only</b> let you search <u>only</u> for torrents which are marked Freeleech.</li></ul>") { Name  = "Preferences" };
            Freeleech = new BoolItem() { Name = "Freeleech Only (Optional)", Value = false };
            DevWarning = new DisplayItem("<b>Development Facility</b> (<i>For Developers ONLY</i>),<br /><br /> <ul><li>By enabling development mode, <b>Jackett will bypass his cache</b> and will <u>output debug messages to console</u> instead of his log file.</li><li>By enabling Hard Drive Cache, <b>This provider</b> will <u>save each query answers from tracker</u> in temp directory, in fact this reduce drastically HTTP requests when building a provider at parsing step for example. So, <b> Jackett will search for a cached query answer on hard drive before executing query on tracker side !</b> <i>DEV MODE must be enabled to use it !</li></ul>") { Name = "Development" };
            DevMode = new BoolItem { Name = "Enable DEV MODE (Developers ONLY)", Value = false };
            HardDriveCache = new BoolItem { Name = "Enable HARD DRIVE CACHE (Developers ONLY)", Value = false };
            HardDriveCacheKeepTime = new StringItem { Name = "Keep Cached files for (ms)", Value = "300000" };
            }
    }
}
