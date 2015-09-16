using Jackett.Models.AutoDL.Parser;
using Jackett.Models.Commands.IRC;
using MediatR;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Services
{
    public interface IAutoDLProcessorService
    {

    }

    public class AutoDLProcessorService : IAutoDLProcessorService, INotificationHandler<IRCMessageEvent>
    {
        IAutoDLProfileService autodlProfileService;
        IIRCProfileService ircProfileService;
        Logger logger;

        public AutoDLProcessorService(IAutoDLProfileService a, IIRCProfileService p, Logger l)
        {
            this.autodlProfileService = a;
            this.ircProfileService = p;
            this.logger = l;
        }

        public void Handle(IRCMessageEvent notification)
        {
            var profile = ircProfileService.Get(notification.Profile);
            if (profile != null)
            {
               var tracker =  autodlProfileService.GetTracker(profile.Profile);
                if (tracker != null)
                {
                    ParseMessage(tracker.Parser, tracker.Type, notification.Channel, notification.From, notification.Message);
                }
            }
        }

        private void ParseMessage(ParseInfo info, string profile, string channel, string user, string message)
        {


            foreach(var single in info.SingleLineMatches)
            {
                var state = new ParserState()
                {
                    CurrentItem = message,
                    Logger = this.logger,
                    Tracker = profile
                };
                var matchedStates = single.Execute(state);

                foreach(var matchedState in matchedStates)
                {
                    foreach(var matcedParser in info.MatchParsers)
                    {
                        var ok = matcedParser.Execute(matchedState);
                    }
                }
            }
        }
    }
}
