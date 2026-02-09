using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Jackett.Common.Services
{
    public static class ServiceLocator
    {
        private static IServiceProvider _serviceProvider;

        public static void Initialize(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public static T GetService<T>()
        {
            if (_serviceProvider == null)
            {
                throw new InvalidOperationException("ServiceProvider not initialized.");
            }

            return _serviceProvider.GetService<T>();
        }
    }
}
