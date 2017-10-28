using Jackett.Models;
using System.Collections.Generic;

namespace Jackett.Services.Interfaces
{
    public interface ILogCacheService
    {
       // void AddLog(LogEventInfo l);
        List<CachedLog> Logs { get; }
    }
}
