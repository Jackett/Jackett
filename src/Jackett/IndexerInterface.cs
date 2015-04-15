using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;

namespace Jackett
{
    public interface IndexerInterface
    {
        string DisplayName { get; }
        string DisplayDescription { get; }
        Uri SiteLink { get; }

        // Retrieved for starting setup for the indexer via web API
        Task<ConfigurationData> GetConfigurationForSetup();

        // Called when web API wants to apply setup configuration via web API, usually this is where login and storing cookie happens
        Task ApplyConfiguration(JToken jsonConfig);

        // Called to check if configuration (cookie) is correct and indexer connection works
        Task VerifyConnection();

        // Invoked when the indexer configuration has been applied and verified so the cookie needs to be saved
        event Action<IndexerInterface, JToken> OnSaveConfigurationRequested;

        // Whether this indexer has been configured, verified and saved in the past and has the settings required for functioning
        bool IsConfigured { get; }

        // Called on startup when initializing indexers from saved configuration
        void LoadFromSavedConfiguration(JToken jsonConfig);
    }
}
