using Autofac;
using Jackett;
using Jackett.Services;
using Jackett.Utils.Clients;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Test
{
    class TestUtil
    {
        private static IContainer testContainer = null;

        public static void SetupContainer()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule<JackettModule>();
            builder.RegisterType<TestWebClient>().As<IWebClient>().SingleInstance();
            builder.RegisterInstance<Logger>(LogManager.GetCurrentClassLogger()).SingleInstance();
            builder.RegisterType<TestIndexerManagerServiceHelper>().As<IIndexerManagerService>().SingleInstance();
            testContainer = builder.Build();

            // Register the container in itself to allow for late resolves
            var secondaryBuilder = new ContainerBuilder();
            secondaryBuilder.RegisterInstance<IContainer>(testContainer).SingleInstance();
            secondaryBuilder.Update(testContainer);
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
            var client = testContainer.Resolve<IWebClient>() as TestWebClient;
            client.RegisterByteCall(r, f);
        }

        public static void RegisterStringCall(WebRequest r, Func<WebRequest, WebClientStringResult> f)
        {
            var client = testContainer.Resolve<IWebClient>() as TestWebClient;
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
