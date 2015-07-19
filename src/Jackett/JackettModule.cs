using Autofac;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac.Integration.WebApi;

namespace Jackett
{
    public class JackettModule: Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // Just register everything!
            var thisAssembly = typeof(JackettModule).Assembly;
            builder.RegisterAssemblyTypes(thisAssembly).AsImplementedInterfaces().SingleInstance();
            builder.RegisterApiControllers(thisAssembly).InstancePerRequest();

            // Register indexers
           foreach(var indexer in thisAssembly.GetTypes()
                .Where(p => typeof(IndexerInterface).IsAssignableFrom(p) && !p.IsInterface)
                .ToArray())
            {
                builder.RegisterType(indexer).Named<IndexerInterface>(indexer.Name.ToLowerInvariant()).SingleInstance();
            }
        }
    }
}
