using IrcDotNet;
using Jackett.Models.Irc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Services
{
    class IRCService
    {
        private List<Network> networks = new List<Network>();

        public void Start()
        {
            networks.Add(new Network()
            {
               // Address  = 
            });

           foreach(var network in networks)
            {
                SetupNetwork(network);
                Connect(network);
            }
        }

        private void SetupNetwork(Network network)
        {
            var client =  network.Client = new StandardIrcClient();
            client.FloodPreventer = new IrcStandardFloodPreventer(4, 2000);
        }

        private void Connect(Network network)
        {

        }
        
    }
}
