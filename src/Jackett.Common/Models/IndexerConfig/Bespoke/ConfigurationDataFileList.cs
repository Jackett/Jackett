using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataFileList : ConfigurationDataUserPasskey
    {
        public BoolConfigurationItem Freeleech { get; set; }
        public DisplayInfoConfigurationItem CatWarning { get; set; }

        public ConfigurationDataFileList()
            : base("Note this is <b>not</b> your <i>password</i>.<ul><li>Login to the FileList Website</li><li>Click on the <b>Profile</b> link</li><li>Scroll down to the <b>Reset Passkey</b> section</li><li>Copy the <b>passkey</b>.</li><li>Also be aware of not leaving a trailing blank at the end of the passkey after pasting it here.</li></ul>BTW, you will not see your current passkey on your Profile until after you have downloaded your first .torrent")
        {
            Freeleech = new BoolConfigurationItem("Search freeleech only") { Value = false };
            CatWarning = new DisplayInfoConfigurationItem("CatWarning", "When mapping TV ensure you add category 5000 in addition to 5020, 5030, 5040, 5045.");
        }
    }
}
