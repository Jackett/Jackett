using Autofac;
using Autofac.Integration.WebApi;

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
