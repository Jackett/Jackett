using System.Collections.Generic;
using System.Linq;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using NLog;
using NLog.Targets;

namespace Jackett.Common.Services
{
    [Target("LogService")]
    public class LogCacheService : TargetWithLayout, ILogCacheService
    {
        private static List<CachedLog> s_Logs = new List<CachedLog>();

        public void AddLog(LogEventInfo l)
        {
            lock (s_Logs)
            {
                s_Logs.Insert(0, new CachedLog { Level = l.Level.Name, Message = l.FormattedMessage, When = l.TimeStamp });
                s_Logs = s_Logs.Take(200).ToList();
            }
        }

        public List<CachedLog> Logs
        {
            get
            {
                lock (s_Logs)
                    return s_Logs.ToList();
            }
        }

        protected override void Write(LogEventInfo logEvent) => AddLog(logEvent);
    }
}
