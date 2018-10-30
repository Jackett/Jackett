namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    internal class ConfigurationDataRarbg : ConfigurationData
    {
        public HiddenItem BaseUri { get; set; }
        public DisplayItem ItorrentsWarning { get; private set; }
        public BoolItem ItorrentsEnabled { get; private set; }

        public ConfigurationDataRarbg()
        {
            BaseUri = new HiddenItem { Name = "BaseLUri", Value = "https://torrentapi.org/" };
            ItorrentsWarning = new DisplayItem("The RarBG API provides only magnets. Enabling the option below will include a .torrent link from Itorrents.org.<br /> <b>However</b>, be aware that Itorrents.org does not store .torrent files for all RarBG magnets, and after a new magnet is released it may take several hours before a .torrent file becomes available on Itorrents.org's DB.<br />Also, if Itorrents.org enable cloudflare DDOS protection, this link will most likely timeout.") { Name = "Itorrents.org Link" };
            ItorrentsEnabled = new BoolItem() { Name = "Include .torrent link from Itorrents.org (Optional)", Value = false };
        }
    }
}
