using Autofac;
using Autofac.Integration.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett
{
    public class WebApi2Module : Module
    {
           protected override void Load(ContainerBuilder builder)
            {
                     builder.RegisterAssemblyTypes(typeof(WebApi2Module).Assembly).AsImplementedInterfaces().SingleInstance();
                     builder.RegisterApiControllers(typeof(WebApi2Module).Assembly).InstancePerRequest();
            }
    }
}
