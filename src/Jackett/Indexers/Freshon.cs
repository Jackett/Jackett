using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;

namespace Jackett
{
    public class Freshon : IndexerInterface
    {

        public string DisplayName
        {
            get { return "FreshOnTV"; }
        }

        public string DisplayDescription
        {
            get { return "Our goal is to provide the latest stuff in the TV show domain"; }
        }

        public Uri SiteLink
        {
            get { return new Uri("https://freshon.tv/"); }
        }

        public Task<ConfigurationData> GetConfigurationForSetup()
        {
            throw new NotImplementedException();
        }

        public Task ApplyConfiguration(JToken jsonConfig)
        {
            throw new NotImplementedException();
        }

        public Task VerifyConnection()
        {
            throw new NotImplementedException();
        }

        public event Action<IndexerInterface, JToken> OnSaveConfigurationRequested;

        public bool IsConfigured
        {
            get { return false; }
        }

        public void LoadFromSavedConfiguration(JToken jsonConfig)
        {
            throw new NotImplementedException();
        }
    }
}
