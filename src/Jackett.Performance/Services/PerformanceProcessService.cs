using System;
using Jackett.Common.Services.Interfaces;

namespace Jackett.Performance.Services
{
    public class PerformanceProcessService : IProcessService
    {
        public void StartProcessAndLog(string exe, string args, bool asAdmin = false) => throw new NotImplementedException();

        public string StartProcessAndGetOutput(string exe, string args, bool keepnewlines = false, bool asAdmin = false) => throw new NotImplementedException();
    }
}