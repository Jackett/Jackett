using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataSceneTime : ConfigurationDataCookie
    {
        public BoolConfigurationItem Freeleech { get; private set; }

        public ConfigurationDataSceneTime()
            : base("For best results, change the 'Torrents per page' setting to the maximum in your profile on the SceneTime webpage.")
        {
            Freeleech = new BoolConfigurationItem("Freeleech Only (Optional)") { Value = false };
        }
    }
}
