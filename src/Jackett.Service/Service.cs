using Jackett.Common.Models.Config;
using System.ServiceProcess;
using Jackett.Common;

namespace Jackett.Service
{
    public partial class Service : ServiceBase
    {
        public Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Engine.BuildContainer(new RuntimeSettings(), new WebApi2Module());
            Engine.Logger.Info("Service starting");
            Engine.Server.Initalize();
            Engine.Server.Start();
            Engine.Logger.Info("Service started");
        }

        protected override void OnStop()
        {
            Engine.Logger.Info("Service stopping");
            Engine.Server.Stop();
        }
    }
}
