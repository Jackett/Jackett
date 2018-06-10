using Jackett.Server;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace Jackett.Service.Windows
{
    public class Service : ServiceBase
    {
        private CancellationTokenSource tokenSource = new CancellationTokenSource();

        protected override void OnStart(string[] args)
        {
            CancellationToken token = tokenSource.Token;

            Task.Run(async () =>
            {
                //Registering callback that would cancel downloading
                token.Register(() => Helper.StopWebHost());
                Jackett.Server.Program.Main(new string[0]);
            }, token);
        }

        protected override void OnStop()
        {
            tokenSource.Cancel(true);
        }
    }
}
