using Autofac;
using Jackett.Models.Commands.IRC;
using MediatR;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Services.SignalR
{
    public class JackettHub : INotificationHandler<IRCMessageEvent>,
                              INotificationHandler<IRCUsersChangedEvent>,
                              INotificationHandler<IRCStateChangedEvent>
    {
        Logger logger;
        IConnectionManager manager;

        public JackettHub(Logger l, IConnectionManager m)
        {
            logger = l;
            manager = m;
        }

        public void Handle(IRCStateChangedEvent notification)
        {
            manager.GetHubContext<JackettHubProxy>().Clients.All.onEvent("IRC-State", notification);
        }

        public void Handle(IRCUsersChangedEvent notification)
        {
            manager.GetHubContext<JackettHubProxy>().Clients.All.onEvent("IRC-Users", notification);
        }

        public void Handle(IRCMessageEvent notification)
        {
            manager.GetHubContext<JackettHubProxy>().Clients.All.onEvent("IRC-Message", notification);
        }
    }
}
