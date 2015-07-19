using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Autofac.Integration.WebApi;
using Microsoft.Owin;
using Jackett;
using Microsoft.Owin.StaticFiles;
using Microsoft.Owin.FileSystems;
using Autofac;
using Jackett.Services;

[assembly: OwinStartup(typeof(Startup))]
namespace Jackett
{
    public class Startup
    {
        public void Configuration(IAppBuilder appBuilder)
        {
            // Configure Web API for self-host. 
            var config = new HttpConfiguration();

            config.DependencyResolver = new AutofacWebApiDependencyResolver(Server.GetContainer());

           

            //  Enable attribute based routing
            //  http://www.asp.net/web-api/overview/web-api-routing-and-actions/attribute-routing-in-web-api-2
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
              name: "Content",
              routeTemplate: "{controller}/{action}",
              defaults: new { controller =  "Admin"}
            );

            appBuilder.UseFileServer(new FileServerOptions
            {
                RequestPath = new PathString(string.Empty),
                FileSystem = new PhysicalFileSystem(Server.GetContainer().Resolve<IConfigurationService>().GetContentFolder()),
                EnableDirectoryBrowsing = true,
            });

            appBuilder.UseWebApi(config);

        }
    }
}
