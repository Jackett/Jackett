using System;
using System.Threading;
using Jackett.Common.Services.Interfaces;

namespace Jackett.Common.Services
{
    public class TrayLockService : ITrayLockService
    {
        private readonly string _eventHandleName = @"Global\JACKETT.TRAY";

        private EventWaitHandle GetEventHandle() =>
            new EventWaitHandle(false, EventResetMode.ManualReset, _eventHandleName);

        public void WaitForSignal()
        {
            if (Environment.OSVersion.Platform != PlatformID.Unix)
            {
                GetEventHandle().Reset();
                GetEventHandle().WaitOne();
            }
        }

        public void Signal()
        {
            if (Environment.OSVersion.Platform != PlatformID.Unix)
                GetEventHandle().Set();
        }
    }
}
