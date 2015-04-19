using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Jackett
{
    class Program
    {
        public static string AppConfigDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Jackett");

        public static ManualResetEvent ExitEvent = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            try
            {
                if (!Directory.Exists(AppConfigDirectory))
                    Directory.CreateDirectory(AppConfigDirectory);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Could not create settings directory");
            }

            Task.Run(() =>
            {
                var server = new Server();
                server.Start();
            });
            ExitEvent.WaitOne();
        }
    }
}
