using Autofac;
using Jackett.Models.Commands.IRC;
using MediatR;
using Microsoft.AspNet.SignalR;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Services.SignalR
{

    public class JackettHubProxy : Hub
    {
        public JackettHubProxy()
        {

        }

        public override Task OnConnected()
        {
            return base.OnConnected();
        }
    }
}
