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
using System.Web.Http.ExceptionHandling;
using System.Net;

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

        public static string ProxyConnection
        {
            get;
            set;
        }

        public static bool? DoSSLFix
        {
            get;
            set;
        }

        public static bool? IgnoreSslErrors
        {
            get;
            set;
        }

        public static string CustomDataFolder
        {
            get;
            set;
        }

        public static string BasePath
        {
            get;
            set;
        }

        public static bool NoRestart
        {
            get;
            set;
        }

        public void Configuration(IAppBuilder appBuilder)
        {
            // Configure Web API for self-host. 
            var config = new HttpConfiguration();

            // try to fix SocketException crashes
            // based on http://stackoverflow.com/questions/23368885/signalr-owin-self-host-on-linux-mono-socketexception-when-clients-lose-connectio/30583109#30583109
            try
            {
                object httpListener;
                if (appBuilder.Properties.TryGetValue(typeof(HttpListener).FullName, out httpListener) && httpListener is HttpListener)
                {
                    // HttpListener should not return exceptions that occur when sending the response to the client
                    ((HttpListener)httpListener).IgnoreWriteExceptions = true;
                    Engine.Logger.Info("set HttpListener.IgnoreWriteExceptions = true");
                }
            }
            catch (Exception e)
            {
                Engine.Logger.Error(e, "Error while setting HttpListener.IgnoreWriteExceptions = true");
            }

            appBuilder.Use<WebApiRootRedirectMiddleware>();

            // register exception handler
            config.Services.Replace(typeof(IExceptionHandler), new WebAPIExceptionHandler());

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
                routeTemplate: "dl/{indexerID}/{apiKey}",
                defaults: new { controller = "Download", action = "Download" }
            );

            config.Routes.MapHttpRoute(
              name: "blackhole",
              routeTemplate: "bh/{indexerID}/{apikey}",
              defaults: new { controller = "Blackhole", action = "Blackhole" }
          );

            appBuilder.UseWebApi(config);


            appBuilder.UseFileServer(new FileServerOptions
            {
                RequestPath = new PathString(string.Empty),
                FileSystem = new PhysicalFileSystem(Engine.ConfigService.GetContentFolder()),
                EnableDirectoryBrowsing = false,

            });

        }
    }
}
