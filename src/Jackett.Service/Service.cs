using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

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
            Engine.BuildContainer(new WebApi2Module());
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
