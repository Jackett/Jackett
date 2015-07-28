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
using AutoMapper;
using Jackett.Models;

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
            }
            else if(System.Environment.OSVersion.Platform == PlatformID.Unix)
            {
                builder.RegisterType<UnixLibCurlWebClient>().As<IWebClient>();
            }
            else
            {
                builder.RegisterType<WindowsWebClient>().As<IWebClient>();
            }

            // Register indexers
            foreach (var indexer in thisAssembly.GetTypes()
                .Where(p => typeof(IIndexer).IsAssignableFrom(p) && !p.IsInterface)
                .ToArray())
            {
                builder.RegisterType(indexer).Named<IIndexer>(BaseIndexer.GetIndexerID(indexer));
            }

            Mapper.CreateMap<WebClientByteResult, WebClientStringResult>().ForMember(x => x.Content, opt => opt.Ignore()).AfterMap((be, str) =>
            {
                str.Content = Encoding.UTF8.GetString(be.Content);
            });

            Mapper.CreateMap<WebClientStringResult, WebClientByteResult>().ForMember(x => x.Content, opt => opt.Ignore()).AfterMap((str, be) =>
            {
                if (!string.IsNullOrEmpty(str.Content))
                {
                    be.Content = Encoding.UTF8.GetBytes(str.Content);
                }
            });

            Mapper.CreateMap<WebClientStringResult, WebClientStringResult>();
            Mapper.CreateMap<WebClientByteResult, WebClientByteResult>();

            Mapper.CreateMap<ReleaseInfo, TrackerCacheResult>().AfterMap((r, t) =>
            {
                t.CategoryDesc = TorznabCatType.GetCatDesc(r.Category);
            });
        }
    }
}
