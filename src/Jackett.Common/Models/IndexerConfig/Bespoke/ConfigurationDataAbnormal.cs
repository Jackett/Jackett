using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataAbnormal : ConfigurationData
    {
        public HiddenStringConfigurationItem AuthKey { get; set; }
        public HiddenStringConfigurationItem TorrentPass { get; set; }
        public DisplayInfoConfigurationItem CredentialsWarning { get; private set; }
        public StringConfigurationItem Username { get; private set; }
        public StringConfigurationItem Password { get; private set; }
        public DisplayInfoConfigurationItem PagesWarning { get; private set; }
        public StringConfigurationItem ReplaceMulti { get; private set; }
        public StringConfigurationItem Pages { get; private set; }

        public ConfigurationDataAbnormal()
            : base()
        {
            AuthKey = new HiddenStringConfigurationItem("AuthKey") { Value = "" };
            TorrentPass = new HiddenStringConfigurationItem("TorrentPass") { Value = "" };
            CredentialsWarning = new DisplayInfoConfigurationItem("Credentials", "<b>Credentials Configuration</b> (<i>Private Tracker</i>),<br /><br /> <ul><li><b>Username</b> is your account name on this tracker.</li><li><b>Password</b> is your password associated to your account name.</li></ul>");
            Username = new StringConfigurationItem("Username") { Value = "" };
            Password = new StringConfigurationItem("Password") { Value = "" };
            PagesWarning = new DisplayInfoConfigurationItem("Preferences", "<b>Preferences Configuration</b> (<i>Tweak your search settings</i>),<br /><br /> <ul><li><b>Replace MULTI</b>, replace multi keyword in the resultset (leave empty  to deactivate)</li><li><b>Max Pages to Process</b> let you specify how many page (max) Jackett can process when doing a search. Setting a value <b>higher than 4 is dangerous</b> for you account ! (<b>Result of too many requests to tracker...that <u>will be suspect</u></b>).</li></ul>");
            Pages = new StringConfigurationItem("Max Pages to Process (Required)") { Value = "4" };
            ReplaceMulti = new StringConfigurationItem("Replace MULTI") { Value = "MULTI.FRENCH" };
        }
    }
}
