using Autofac;
using System;
using System.Linq;
using System.Text;
using Autofac.Integration.WebApi;
using Jackett.Indexers;
using Jackett.Utils.Clients;
using AutoMapper;
using Jackett.Models;
using System.Reflection;
using Jackett.Services;
using Jackett.Indexers.Meta;

namespace Jackett
{
    public class JackettModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // Just register everything!
            var thisAssembly = typeof(JackettModule).Assembly;
            builder.RegisterAssemblyTypes(thisAssembly)
                   .Except<IIndexer>()
                   .Except<IImdbResolver>()
                   .Except<OmdbResolver>()
                   .Except<IFallbackStrategyProvider>()
                   .Except<ImdbFallbackStrategyProvider>()
                   .Except<IFallbackStrategy>()
                   .Except<ImdbFallbackStrategy>()
                   .Except<IResultFilterProvider>()
                   .Except<ImdbTitleResultFilterProvider>()
                   .Except<IResultFilter>()
                   .Except<ImdbTitleResultFilterProvider>()
                   .Except<BaseMetaIndexer>()
                   .Except<AggregateIndexer>()
                   .Except<CardigannIndexer>()
                   .AsImplementedInterfaces().SingleInstance();
            builder.RegisterApiControllers(thisAssembly).InstancePerRequest();
            builder.RegisterType<HttpWebClient>();

            // Register the best web client for the platform or the override
            switch (Startup.ClientOverride)
            {
                case "httpclient":
                    builder.RegisterType<HttpWebClient>().As<IWebClient>();
                    break;
                case "httpclient2":
                    builder.RegisterType<HttpWebClient2>().As<IWebClient>();
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
                        var usehttpclient = false;
                        try
                        {
                            Type monotype = Type.GetType("Mono.Runtime");
                            if (monotype != null)
                            {
                                MethodInfo displayName = monotype.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
                                if (displayName != null)
                                {
                                    var monoVersion = displayName.Invoke(null, null).ToString();
                                    var monoVersionO = new Version(monoVersion.Split(' ')[0]);
                                    if ((monoVersionO.Major >= 4 && monoVersionO.Minor >= 8) || monoVersionO.Major >= 5)
                                    {
                                        // check if btls is supported
                                        var monoSecurity = Assembly.Load("Mono.Security");
                                        Type monoTlsProviderFactory = monoSecurity.GetType("Mono.Security.Interface.MonoTlsProviderFactory");
                                        if (monoTlsProviderFactory != null)
                                        {
                                            MethodInfo isProviderSupported = monoTlsProviderFactory.GetMethod("IsProviderSupported");
                                            if (isProviderSupported != null)
                                            {
                                                var btlsSupported = (bool)isProviderSupported.Invoke(null, new string[] { "btls" });
                                                if (btlsSupported)
                                                {
                                                    // initialize btls
                                                    MethodInfo initialize = monoTlsProviderFactory.GetMethod("Initialize", new[] { typeof(string) });
                                                    if (initialize != null)
                                                    {
                                                        initialize.Invoke(null, new string[] { "btls" });
                                                        usehttpclient = true;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Console.Out.WriteLine("Error while deciding which HttpWebClient to use: " + e);
                        }

                        if (usehttpclient)
                            builder.RegisterType<HttpWebClient>().As<IWebClient>();
                        else
                            builder.RegisterType<UnixLibCurlWebClient>().As<IWebClient>();
                    }
                    else
                    {
                        builder.RegisterType<HttpWebClient>().As<IWebClient>();
                    }
                    break;
            }
            Mapper.Initialize(cfg =>
            {
                cfg.CreateMap<WebClientByteResult, WebClientStringResult>().ForMember(x => x.Content, opt => opt.Ignore()).AfterMap((be, str) =>
                {
                    str.Content = Encoding.UTF8.GetString(be.Content);
                });

                cfg.CreateMap<WebClientStringResult, WebClientByteResult>().ForMember(x => x.Content, opt => opt.Ignore()).AfterMap((str, be) =>
                {
                    if (!string.IsNullOrEmpty(str.Content))
                    {
                        be.Content = Encoding.UTF8.GetBytes(str.Content);
                    }
                });

                cfg.CreateMap<WebClientStringResult, WebClientStringResult>();
                cfg.CreateMap<WebClientByteResult, WebClientByteResult>();
                cfg.CreateMap<ReleaseInfo, ReleaseInfo>();

                cfg.CreateMap<ReleaseInfo, TrackerCacheResult>().AfterMap((r, t) =>
                {
                    if (r.Category != null)
                    {
                        var CategoryDesc = string.Join(", ", r.Category.Select(x => TorznabCatType.GetCatDesc(x)).Where(x => !string.IsNullOrEmpty(x)));
                        t.CategoryDesc = CategoryDesc;
                    }
                    else
                    {
                        t.CategoryDesc = "";
                    }
                });
            });
        }
    }
}
