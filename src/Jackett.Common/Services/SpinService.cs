using System.Threading;
using Jackett.Common.Services.Interfaces;

namespace Jackett.Common.Services
{
    internal class RunTimeService : IRunTimeService
    {
        private readonly bool isRunning = true;

        public void Spin()
        {
            while (isRunning)
            {
                Thread.Sleep(2000);
            }
        }
    }
}
