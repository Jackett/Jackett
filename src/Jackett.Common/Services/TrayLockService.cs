using System;
using System.Threading;
using Jackett.Common.Services.Interfaces;

namespace Jackett.Common.Services
{

    public class TrayLockService : ITrayLockService
    {
        private readonly string EVENT_HANDLE_NAME = @"Global\JACKETT.TRAY";

        private EventWaitHandle GetEventHandle() => new EventWaitHandle(false, EventResetMode.ManualReset, EVENT_HANDLE_NAME);

        public void WaitForSignal()
        {
            if (System.Environment.OSVersion.Platform != PlatformID.Unix)
            {
                GetEventHandle().Reset();
                GetEventHandle().WaitOne();
            }
        }

        public void Signal()
        {
            if (System.Environment.OSVersion.Platform != PlatformID.Unix)
            {
                GetEventHandle().Set();
            }
        }
    }
}
