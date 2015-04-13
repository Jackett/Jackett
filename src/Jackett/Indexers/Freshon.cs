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

        public string DisplayName { get; private set; }


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

        public event Action<JToken> OnSaveConfigurationRequested;

        public bool IsConfigured
        {
            get { throw new NotImplementedException(); }
        }

        public void LoadFromSavedConfiguration(JToken jsonConfig)
        {
            throw new NotImplementedException();
        }


        public string DisplayDescription
        {
            get { throw new NotImplementedException(); }
        }

        public Uri SitLink
        {
            get { throw new NotImplementedException(); }
        }
    }
}
