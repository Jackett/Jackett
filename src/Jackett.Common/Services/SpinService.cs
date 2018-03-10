using System.Threading;
using Jackett.Common.Services.Interfaces;

namespace Jackett.Common.Services
{

    class RunTimeService : IRunTimeService
    {
        private bool isRunning = true;

        public void Spin()
        {
            while (isRunning)
            {
                Thread.Sleep(2000);
            }
        }
    }
}
