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
using Autofac.Integration.SignalR;
using Jackett.Services;
using Autofac.Features.Variance;
using MediatR;
using Jackett.Services.SignalR;

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
            builder.RegisterType<HttpWebClient>();
            builder.RegisterType<JackettHub>();

            // Register the best web client for the platform or the override
            switch (Startup.ClientOverride)
            {
                case "httpclient":
                    builder.RegisterType<HttpWebClient>().As<IWebClient>();
                    break;
                case "safecurl":
                    builder.RegisterType<UnixSafeCurlWebClient>().As<IWebClient>();
                    break;
                case "libcurl":
                    builder.RegisterType<UnixLibCurlWebClient>().As<IWebClient>();
                    break;
                case "automatic":
                    default:
                    if (System.Environment.OSVersion.Platform == PlatformID.Unix)
                    {
                        builder.RegisterType<UnixLibCurlWebClient>().As<IWebClient>();
                    }
                    else
                    {
                        builder.RegisterType<HttpWebClient>().As<IWebClient>();
                    }
                    break;
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
            Mapper.CreateMap<ReleaseInfo, ReleaseInfo>();

            Mapper.CreateMap<ReleaseInfo, TrackerCacheResult>().AfterMap((r, t) =>
            {
                t.CategoryDesc = TorznabCatType.GetCatDesc(r.Category);
            });

            builder.RegisterSource(new ContravariantRegistrationSource());
            builder.RegisterAssemblyTypes(typeof(IMediator).Assembly).AsImplementedInterfaces();

            builder.Register<SingleInstanceFactory>(ctx =>
            {
                var c = ctx.Resolve<IComponentContext>();
                return t => c.Resolve(t);
            });
            builder.Register<MultiInstanceFactory>(ctx =>
            {
                var c = ctx.Resolve<IComponentContext>();
                return t => (IEnumerable<object>)c.Resolve(typeof(IEnumerable<>).MakeGenericType(t));
            });

            
        }
    }
}
