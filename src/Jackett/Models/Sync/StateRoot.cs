using Jackett.Models.Irc;
using Jackett.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.Sync
{
    public class StateRoot : SyncObjectBase
    {
        List<Network> networks = new List<Network>();

        public List<Network> Networks
        {
            get { return networks; }
        }

        public List<Change> GetAllData(IObjectSyncService s)
        {
            return Sync(s, null, true, true);
        }
    }
}
