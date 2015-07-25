using Autofac;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac.Integration.WebApi;
using Jackett.Indexers;
using Jackett.Utils;
using Jackett.Utils.Clients;

namespace Jackett
{
    public class JackettModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // Just register everything!
            var thisAssembly = typeof(JackettModule).Assembly;
            builder.RegisterAssemblyTypes(thisAssembly).Except<IIndexer>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterApiControllers(thisAssembly).InstancePerRequest();

            // Register the best web client for the platform or exec curl as a safe option
            if (Startup.CurlSafe)
            {
                builder.RegisterType<UnixSafeCurlWebClient>().As<IWebClient>();
                Console.WriteLine("Using UnixSafeCurlWebClient");
            }
            else if(System.Environment.OSVersion.Platform == PlatformID.Unix)
            {
                builder.RegisterType<UnixLibCurlWebClient>().As<IWebClient>();
                Console.WriteLine("Using UnixLibCurlWebClient");
            }
            else
            {
                builder.RegisterType<WindowsWebClient>().As<IWebClient>();
                Console.WriteLine("Using WindowsWebClient");
            }

            // Register indexers
            foreach (var indexer in thisAssembly.GetTypes()
                .Where(p => typeof(IIndexer).IsAssignableFrom(p) && !p.IsInterface)
                .ToArray())
            {
                builder.RegisterType(indexer).Named<IIndexer>(BaseIndexer.GetIndexerID(indexer));
            }
        }
    }
}
