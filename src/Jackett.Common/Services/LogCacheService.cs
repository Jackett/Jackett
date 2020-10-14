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
        private static List<CachedLog> logs = new List<CachedLog>();

        public void AddLog(LogEventInfo l)
        {
            lock (logs)
            {
                logs.Insert(0, new CachedLog()
                {
                    Level = l.Level.Name,
                    Message = l.FormattedMessage,
                    When = l.TimeStamp
                });
                logs = logs.Take(200).ToList();
            }

        }

        public List<CachedLog> Logs
        {
            get
            {
                lock (logs)
                {
                    return logs.ToList();
                }
            }
        }

        protected override void Write(LogEventInfo logEvent) => AddLog(logEvent);
    }
}
