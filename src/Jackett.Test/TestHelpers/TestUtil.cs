using System.IO;
using System.Reflection;
using Autofac;
using Jackett.Common.Models.Config;
using Jackett.Common.Plumbing;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using Microsoft.AspNetCore.DataProtection;
using NLog;

namespace Jackett.Test.TestHelpers
{
    internal static class TestUtil
    {
        private static IContainer testContainer;

        public static void SetupContainer()
        {
            IDataProtectionProvider dataProtectionProvider = new EphemeralDataProtectionProvider();

            var builder = new ContainerBuilder();
            builder.RegisterModule(new JackettModule(new RuntimeSettings()));
            builder.RegisterType<Jackett.Server.Services.ProtectionService>().As<IProtectionService>();
            builder.RegisterType<TestWebClient>().As<WebClient>().SingleInstance();
            builder.RegisterInstance(LogManager.GetCurrentClassLogger()).SingleInstance();
            builder.RegisterType<TestIndexerManagerServiceHelper>().As<IIndexerManagerService>().SingleInstance();
            builder.RegisterInstance(dataProtectionProvider).SingleInstance();
            testContainer = builder.Build();
        }

        public static TestIndexerManagerServiceHelper IndexManager => testContainer.Resolve<IIndexerManagerService>() as TestIndexerManagerServiceHelper;

        public static IContainer Container => testContainer;

        //Currently not used in any Unit Tests
        public static void RegisterRequestCallback(string requestUrl, string responseFileName)
        {
            var client = testContainer.Resolve<WebClient>() as TestWebClient;
            client.RegisterRequestCallback(requestUrl, responseFileName);
        }

        public static string GetResource(string item)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Jackett.Test." + item.Replace('/', '.');

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public static string LoadTestFile(string fileName)
        {
            var applicationFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            var filePath = Path.Combine(Path.Combine(applicationFolder, "Resources"), fileName);
            return File.ReadAllText(filePath);
        }
    }
}
