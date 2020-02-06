using System;
using System.IO;
using System.Reflection;
using Autofac;
using Jackett.Common.Models.Config;
using Jackett.Common.Plumbing;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using Jackett.Server.Services;
using Microsoft.AspNetCore.DataProtection;
using NLog;

namespace Jackett.Test
{
    internal static class TestUtil
    {
        public static void SetupContainer()
        {
            IDataProtectionProvider dataProtectionProvider = new EphemeralDataProtectionProvider();
            var builder = new ContainerBuilder();
            builder.RegisterModule(new JackettModule(new RuntimeSettings()));
            builder.RegisterType<ProtectionService>().As<IProtectionService>();
            builder.RegisterType<TestWebClient>().As<WebClient>().SingleInstance();
            builder.RegisterInstance(LogManager.GetCurrentClassLogger()).SingleInstance();
            builder.RegisterType<TestIndexerManagerServiceHelper>().As<IIndexerManagerService>().SingleInstance();
            builder.RegisterInstance(dataProtectionProvider).SingleInstance();
            Container = builder.Build();
        }

        public static TestIndexerManagerServiceHelper IndexManager =>
            Container.Resolve<IIndexerManagerService>() as TestIndexerManagerServiceHelper;

        public static IContainer Container { get; private set; }

        public static void RegisterByteCall(WebRequest r, Func<WebRequest, WebClientByteResult> f)
        {
            var client = Container.Resolve<WebClient>() as TestWebClient;
            client.RegisterByteCall(r, f);
        }

        public static void RegisterStringCall(WebRequest r, Func<WebRequest, WebClientStringResult> f)
        {
            var client = Container.Resolve<WebClient>() as TestWebClient;
            client.RegisterStringCall(r, f);
        }

        public static string GetResource(string item)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Jackett.Test." + item.Replace('/', '.');
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }
    }
}
