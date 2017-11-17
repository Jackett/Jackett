using Autofac;
using System;
using System.Linq;
using System.Text;
using Jackett.Indexers;
using Jackett.Utils.Clients;
using AutoMapper;
using Jackett.Models;
using System.Reflection;
using Jackett.Services;
using Jackett.Indexers.Meta;
using Jackett.Services.Interfaces;
using Jacket.Common;
using Jackett.Models.Config;
using System.IO;
using Newtonsoft.Json.Linq;
using Jackett.Utils;
using System.Collections.Generic;
using Jackett.Common.Models.Config;

namespace Jackett.Common.Plumbing
{
    public class JackettModule : Autofac.Module
    {
        private RuntimeSettings _runtimeSettings;

        public JackettModule (RuntimeSettings runtimeSettings)
            {
            _runtimeSettings = runtimeSettings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            // Just register everything! TODO: Something better and more explicit than scanning everything.
                builder.RegisterAssemblyTypes(typeof(JackettModule).Assembly)
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


            builder.RegisterInstance(_runtimeSettings);
            builder.Register(ctx =>
            {
                return BuildServerConfig(ctx);
            }).As<ServerConfig>().SingleInstance();
            builder.RegisterType<HttpWebClient>();
            
            // Register the best web client for the platform or the override
            switch (_runtimeSettings.ClientOverride)
            {
                case "httpclient":
                    builder.RegisterType<HttpWebClient>().As<WebClient>();
                    break;
                case "httpclient2":
                    builder.RegisterType<HttpWebClient2>().As<WebClient>();
                    break;
                case "safecurl":
                    builder.RegisterType<UnixSafeCurlWebClient>().As<WebClient>();
                    break;
                case "libcurl":
                    builder.RegisterType<UnixLibCurlWebClient>().As<WebClient>();
                    break;
                case "automatic":
                default:
                    if (System.Environment.OSVersion.Platform != PlatformID.Unix)
                    {
                        builder.RegisterType<HttpWebClient>().As<WebClient>();
                        break;
                    }
                    var usehttpclient = DetectMonoCompatabilityWithHttpClient();
                    if (usehttpclient)
                        builder.RegisterType<HttpWebClient>().As<WebClient>();
                    else
                        builder.RegisterType<UnixLibCurlWebClient>().As<WebClient>();
                    break;
            }

        }

        private ServerConfig BuildServerConfig(IComponentContext ctx)
        {
            var configService = ctx.Resolve<IConfigurationService>();
            // Load config
            var config = configService.GetConfig<ServerConfig>();
            if (config == null)
            {
                config = new ServerConfig(_runtimeSettings);
            }
            else
            {
                //We don't load these out of the config files as it could get confusing to users who accidently save. 
                //In future we could flatten the serverconfig, and use command line parameters to override any configuration.
                config.RuntimeSettings = _runtimeSettings;
            }

            if (string.IsNullOrWhiteSpace(config.APIKey))
            {
                // Check for legacy key config
                var apiKeyFile = Path.Combine(configService.GetAppDataFolder(), "api_key.txt");
                if (File.Exists(apiKeyFile))
                {
                    config.APIKey = File.ReadAllText(apiKeyFile);
                }

                // Check for legacy settings

                var path = Path.Combine(configService.GetAppDataFolder(), "config.json"); ;
                var jsonReply = new JObject();
                if (File.Exists(path))
                {
                    jsonReply = JObject.Parse(File.ReadAllText(path));
                    config.Port = (int)jsonReply["port"];
                    config.AllowExternal = (bool)jsonReply["public"];
                }

                if (string.IsNullOrWhiteSpace(config.APIKey))
                    config.APIKey = StringUtil.GenerateRandom(32);

                configService.SaveConfig(config);
            }

            if (string.IsNullOrWhiteSpace(config.InstanceId))
            {
                config.InstanceId = StringUtil.GenerateRandom(64);
                configService.SaveConfig(config);
            }
            config.ConfigChanged();
            return config;
        }

        

        private static bool DetectMonoCompatabilityWithHttpClient()
        {
            bool usehttpclient = false;
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

            return usehttpclient;
        }

      
    }
}
