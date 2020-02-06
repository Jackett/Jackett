using System.Threading;
using Jackett.Common.Services.Interfaces;

namespace Jackett.Common.Services
{
    internal class RunTimeService : IRunTimeService
    {
        private readonly bool _isRunning = true;

        public void Spin()
        {
            while (_isRunning)
                Thread.Sleep(2000);
        }
    }
}
