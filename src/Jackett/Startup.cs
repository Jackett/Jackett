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
using System.Web.Http.Tracing;
using Jackett.Utils;
using Microsoft.AspNet.Identity;
using System.IO;
using Microsoft.AspNet.SignalR;
using Autofac.Integration.SignalR;
using Swashbuckle.Application;
using Jackett.Services.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;

[assembly: OwinStartup(typeof(Startup))]
namespace Jackett
{
    public class Startup
    {
        public static bool TracingEnabled
        {
            get;
            set;
        }

        public static bool LogRequests
        {
            get;
            set;
        }

        public static string ClientOverride
        {
            get;
            set;
        }

        public static bool? DoSSLFix
        {
            get;
            set;
        }

        public void Configuration(IAppBuilder appBuilder)
        {
            // Configure Web API for self-host. 
            var config = new HttpConfiguration();

            // ES6 Imports always pull from a .js file however signalr doesn't support the call with an extensions so remap it.
            appBuilder.Rewrite("/signalr/hubs.js", "/signalr/hubs");

            appBuilder.Use<WebApiRootRedirectMiddleware>();

            // Setup tracing if enabled
            if (TracingEnabled)
            {
                config.EnableSystemDiagnosticsTracing();
                config.Services.Replace(typeof(ITraceWriter), new WebAPIToNLogTracer());
            }
            // Add request logging if enabled
            if (LogRequests)
            {
                config.MessageHandlers.Add(new WebAPIRequestLogger());
            }

            config.DependencyResolver = new AutofacWebApiDependencyResolver(Engine.GetContainer());
            config.MapHttpAttributeRoutes();
            appBuilder.UseWebApi(config);

            config.Routes.MapHttpRoute(
             name: "irccommand",
             routeTemplate: "webapi/irccommand",
               defaults: new { controller = "IRCChannel", action = "Command" }
            );


            config.Routes.MapHttpRoute(
            name: "ircmessages",
            routeTemplate: "webapi/ircmessages/{network}/{room}",
              defaults: new { controller = "IRCChannel", action = "Messages", room = RouteParameter.Optional }
           );

            config.Routes.MapHttpRoute(
              name: "ircusers",
              routeTemplate: "webapi/ircusers/{network}/{room}",
                defaults: new { controller = "IRCChannel", action = "Users" }
             );

            config.Routes.MapHttpRoute(
             name: "irc",
             routeTemplate: "webapi/ircstate",
                defaults: new { controller = "IRCState" }
            );

            config.Routes.MapHttpRoute(
              name: "webapi",
              routeTemplate: "webapi/{controller}/{id}",
              defaults: new { id = RouteParameter.Optional }
             );

            config.Routes.MapHttpRoute(
                name: "Admin",
                routeTemplate: "admin/{action}",
                defaults: new { controller = "Admin" }
            );

            config.Routes.MapHttpRoute(
                name: "apiDefault",
                routeTemplate: "api/{indexerID}",
                defaults: new { controller = "Torznab", action = "Call" }
            );

            config.Routes.MapHttpRoute(
               name: "api",
               routeTemplate: "api/{indexerID}/api",
               defaults: new { controller = "Torznab", action = "Call" }
           );

            config.Routes.MapHttpRoute(
               name: "torznabDefault",
               routeTemplate: "torznab/{indexerID}",
               defaults: new { controller = "Torznab", action = "Call" }
           );

            config.Routes.MapHttpRoute(
               name: "torznab",
               routeTemplate: "torznab/{indexerID}/api",
               defaults: new { controller = "Torznab", action = "Call" }
           );

            config.Routes.MapHttpRoute(
              name: "potatoDefault",
              routeTemplate: "potato/{indexerID}",
              defaults: new { controller = "Potato", action = "Call" }
          );

            config.Routes.MapHttpRoute(
               name: "potato",
               routeTemplate: "potato/{indexerID}/api",
               defaults: new { controller = "Potato", action = "Call" }
           );

            config.Routes.MapHttpRoute(
                name: "download",
                routeTemplate: "dl/{indexerID}/{apikey}/{path}/t.torrent",
                defaults: new { controller = "Download", action = "Download" }
            );

            config.Routes.MapHttpRoute(
              name: "blackhole",
              routeTemplate: "bh/{indexerID}/{apikey}/{path}",
              defaults: new { controller = "Blackhole", action = "Blackhole" }
          );

            appBuilder.UseAutofacMiddleware(Engine.GetContainer());
            var dr = new AutofacDependencyResolver(Engine.GetContainer());
            appBuilder.MapSignalR(new Microsoft.AspNet.SignalR.HubConfiguration()
            {
                EnableDetailedErrors = true,
                EnableJSONP = true,
                Resolver = dr
});

            appBuilder.UseFileServer(new FileServerOptions
            {
                RequestPath = new PathString("/dev"),
                FileSystem = new PhysicalFileSystem(Path.GetFullPath(Path.Combine(Engine.ConfigService.GetContentFolder(), "../../Jackett.Web"))),
                EnableDirectoryBrowsing = false,
            });

            appBuilder.UseFileServer(new FileServerOptions
            {
                RequestPath = new PathString(string.Empty),
                FileSystem = new PhysicalFileSystem(Engine.ConfigService.GetContentFolder()),
                EnableDirectoryBrowsing = false,
            });

            config
            .EnableSwagger(c => c.SingleApiVersion("v1", "Jackett API"))
            .EnableSwaggerUi("docs/{*assetPath}");

            var conManager = dr.Resolve<IConnectionManager>();
            var builder = new ContainerBuilder();
            builder.RegisterInstance(conManager).As<IConnectionManager>();
            builder.Update(Engine.GetContainer());

           // IHubContext hubContext = dr.Resolve<IConnectionManager>().GetHubContext<JackettHub>(); 
           //  var context = GlobalHost.ConnectionManager.GetHubContext<JackettHub>();
        }
    }
}
