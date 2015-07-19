using Jackett;
using Jackett.Indexers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace JackettConsole
{
    public class Program
    {
        static void Main(string[] args)
        {


            Server.Start();
            Console.ReadKey();


          /*  var serverTask = Task.Run(async () =>
            {
                ServerInstance = new Server();
                await ServerInstance.Start();
            });

            try
            {
                if (Program.IsWindows)
                {
#if !__MonoCS__
                    Application.Run(new Main());
#endif
                }
            }
            catch (Exception)
            {

            }*/

            Console.WriteLine("Running in headless mode.");



          //  Task.WaitAll(serverTask);
            Console.WriteLine("Server thread exit");
        }

       /* public static void RestartServer()
        {

            ServerInstance.Stop();
            ServerInstance = null;
            var serverTask = Task.Run(async () =>
            {
                ServerInstance = new Server();
                await ServerInstance.Start();
            });
            Task.WaitAll(serverTask);
        }*/

        

       

        static public void RestartAsAdmin()
        {
           // var startInfo = new ProcessStartInfo(Application.ExecutablePath.ToString()) { Verb = "runas" };
           // Process.Start(startInfo);
            Environment.Exit(0);
        }
    }
}

