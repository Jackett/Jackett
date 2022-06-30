using Autofac;
using Jackett.Common.Indexers;
using Jackett.Common.Indexers.Meta;
using Jackett.Common.Models.Config;
using Jackett.Common.Services;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;

namespace Jackett.Common.Plumbing
{
    public class JackettModule : Autofac.Module
    {
        private readonly RuntimeSettings _runtimeSettings;

        public JackettModule(RuntimeSettings runtimeSettings) => _runtimeSettings = runtimeSettings;

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
            builder.Register(BuildServerConfig).As<ServerConfig>().SingleInstance();
            builder.RegisterType<HttpWebClient>();

            // Register the best web client for the platform or the override
            switch (_runtimeSettings.ClientOverride)
            {
                case "httpclient2":
                    RegisterWebClient<HttpWebClient2>(builder);
                    break;
                default: // "httpclient"
                    RegisterWebClient<HttpWebClient>(builder);
                    break;
            }
        }

        private void RegisterWebClient<WebClientType>(ContainerBuilder builder) => builder.RegisterType<WebClientType>().As<WebClient>();

        private ServerConfig BuildServerConfig(IComponentContext ctx)
        {
            var configService = ctx.Resolve<IConfigurationService>();
            return configService.BuildServerConfig(_runtimeSettings);
        }
    }
}
