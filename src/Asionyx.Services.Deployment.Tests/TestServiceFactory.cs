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
            // Provide a default IProcessRunner for tests so controllers resolve cleanly
            services.AddSingleton<Asionyx.Services.Deployment.Services.IProcessRunner, Asionyx.Services.Deployment.Services.DefaultProcessRunner>();

            configure?.Invoke(services);

            return services.BuildServiceProvider();
        }
    }
}
