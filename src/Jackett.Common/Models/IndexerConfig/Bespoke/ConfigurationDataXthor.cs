using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataXthor : ConfigurationData
    {
        public DisplayInfoConfigurationItem CredentialsWarning { get; private set; }
        public StringConfigurationItem PassKey { get; set; }
        public DisplayInfoConfigurationItem PagesWarning { get; private set; }
        public StringConfigurationItem Accent { get; set; }
        public BoolConfigurationItem Freeleech { get; private set; }
        public StringConfigurationItem ReplaceMulti { get; private set; }
        public BoolConfigurationItem EnhancedAnime { get; private set; }

        public BoolConfigurationItem Vostfr { get; private set; }

        public ConfigurationDataXthor()
            : base()
        {
            CredentialsWarning = new DisplayInfoConfigurationItem("Credentials", "<b>Credentials Configuration</b> (<i>Private Tracker</i>),<br /><br /> <ul><li><b>PassKey</b> is your private key on your account</li></ul>");
            PassKey = new StringConfigurationItem("PassKey") { Value = "" };
            Accent = new StringConfigurationItem("Accent") { Value = "" };
            PagesWarning = new DisplayInfoConfigurationItem("Preferences", "<b>Preferences Configuration</b> (<i>Tweak your search settings</i>),<br /><br /> <ul><li><b>Freeleech Only</b> let you search <u>only</u> for torrents which are marked Freeleech.</li><li><b>Replace MULTI</b>, replace multi keyword in the resultset (leave empty  to deactivate)</li><li><b>Enhanced anime search</b>, Enhance sonarr compatibility with Xthor. Only effective on requests with the <u>TVAnime Torznab category</u>.</li><li><b>Accent</b> is the french accent you want. 1 for VFF (Truefrench) 2 for VFQ (FRENCH, canada). When one is selected, the other will not be searched.</li></ul>");
            Freeleech = new BoolConfigurationItem("Freeleech Only (Optional)") { Value = false };
            ReplaceMulti = new StringConfigurationItem("Replace MULTI") { Value = "MULTI.FRENCH" };
            EnhancedAnime = new BoolConfigurationItem("Enhanced anime search") { Value = false };
            Vostfr = new BoolConfigurationItem("Replace VOSTFR or SUBFRENCH with ENGLISH") { Value = false };
        }
    }
}
