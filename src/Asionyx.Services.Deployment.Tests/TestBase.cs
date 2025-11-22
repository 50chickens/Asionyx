using System;
using Microsoft.Extensions.DependencyInjection;

namespace Asionyx.Services.Deployment.Tests
{
    public abstract class TestBase
    {
        private readonly TestServiceFactory _factory = new TestServiceFactory();
        protected IServiceProvider Provider { get; }

        protected TestBase()
        {
            Provider = _factory.BuildDefaultProvider();
        }

        protected T Get<T>() where T : class
        {
            return ActivatorUtilities.CreateInstance<T>(Provider);
        }
    }
}
