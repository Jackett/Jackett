using System.ServiceProcess;

namespace Jackett.Service
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static void Main()
        {
            ServiceBase[] servicesToRun;
            servicesToRun = new ServiceBase[]
            {
                new Service()
            };
            ServiceBase.Run(servicesToRun);
        }
    }
}
