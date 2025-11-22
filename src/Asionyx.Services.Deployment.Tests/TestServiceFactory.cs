using System;
using Asionyx.Library.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Asionyx.Services.Deployment.Tests
{
    public class TestServiceFactory
    {
        public IServiceProvider BuildDefaultProvider(Action<IServiceCollection> configure = null)
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().Build();

            services.AddSingleton<IConfiguration>(config);
            services.AddSingleton<ISystemConfigurator, TestSystemConfigurator>();

            configure?.Invoke(services);

            return services.BuildServiceProvider();
        }
    }
}
