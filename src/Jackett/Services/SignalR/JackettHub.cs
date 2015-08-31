using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Services.SignalR
{
    public interface IJackettHub
    {

    }

    public class JackettHub : Hub
    {
        IObjectSyncService syncService;

        public JackettHub(IObjectSyncService o)
        {
            syncService = o;
        }

        public override Task OnConnected()
        {
            Clients.Caller.transferState(syncService.Root.GetAllData(syncService));
            return base.OnConnected();
        }
    }
}
