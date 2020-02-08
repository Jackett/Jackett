using System.Collections.Generic;
using Jackett.Common.Models;

namespace Jackett.Common.Services.Interfaces
{
    public interface ILogCacheService
    {
        // void AddLog(LogEventInfo l);
        List<CachedLog> Logs { get; }
    }
}
