namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataBasicLoginWithFreeleech : ConfigurationDataBasicLogin
    {
        public BoolItem Freeleech { get; private set; }

        public ConfigurationDataBasicLoginWithFreeleech(string instructionMessageOptional = null)
        {
            Freeleech = new BoolItem() { Name = "Search Freeleech only (optional)", Value = false };
        }


    }
}
