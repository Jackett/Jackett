using Autofac;
using NLog;
using System;
using System.IO;
using System.Reflection;
using Jackett.Common.Plumbing;
using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;

namespace Jackett.Test
{
    class TestUtil
    {
        private static IContainer testContainer = null;

        public static void SetupContainer()
        {
            var builder = new ContainerBuilder();            
            builder.RegisterModule(new JackettModule(new RuntimeSettings()));
            builder.RegisterModule<WebApi2Module>();
            builder.RegisterType<TestWebClient>().As<WebClient>().SingleInstance();
            builder.RegisterInstance(LogManager.GetCurrentClassLogger()).SingleInstance();
            builder.RegisterType<TestIndexerManagerServiceHelper>().As<IIndexerManagerService>().SingleInstance();
            testContainer = builder.Build();
        }

        public static TestIndexerManagerServiceHelper IndexManager
        {
            get
            {
                return testContainer.Resolve<IIndexerManagerService>() as TestIndexerManagerServiceHelper;
            }
        }

        public static IContainer Container
        {
            get { return testContainer;  }
        }

        public static void RegisterByteCall(WebRequest r, Func<WebRequest, WebClientByteResult> f)
        {
            var client = testContainer.Resolve<WebClient>() as TestWebClient;
            client.RegisterByteCall(r, f);
        }

        public static void RegisterStringCall(WebRequest r, Func<WebRequest, WebClientStringResult> f)
        {
            var client = testContainer.Resolve<WebClient>() as TestWebClient;
            client.RegisterStringCall(r, f);
        }

        public static string GetResource(string item)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Jackett.Test." + item.Replace('/','.');

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
