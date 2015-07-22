using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jackett.Services
{
    public interface IRunTimeService
    {
        void Spin();
    }

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
