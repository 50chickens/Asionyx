using System.Linq;
using Asionyx.Library.Core;
using Asionyx.Services.Deployment.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Asionyx.Services.Deployment.Tests
{
    [TestFixture]
    public class ControllerResolutionTests
    {
        [Test]
        public void Resolve_All_Controllers_From_DI()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().Build();

            services.AddSingleton<IConfiguration>(config);
            services.AddOptions();
            services.AddControllers().AddNewtonsoftJson();

            // Register known concrete dependencies used by controllers
            services.AddSingleton<ISystemConfigurator, TestSystemConfigurator>();
            // Provide a default process runner so controllers depending on IProcessRunner resolve in tests
            services.AddSingleton<Asionyx.Services.Deployment.Services.IProcessRunner, Asionyx.Services.Deployment.Services.DefaultProcessRunner>();

            var provider = services.BuildServiceProvider();

            var controllerTypes = typeof(InfoController).Assembly
                .GetTypes()
                .Where(t => t.IsClass && t.Name.EndsWith("Controller"));

            foreach (var type in controllerTypes)
            {
                // Use ActivatorUtilities so DI is used for constructor injection
                var instance = Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance(provider, type);
                Assert.That(instance, Is.Not.Null, $"Failed to resolve controller {type.FullName}");
            }
        }

        // TestSystemConfigurator is provided as a top-level test helper in `TestSystemConfigurator.cs`.
    }
}

