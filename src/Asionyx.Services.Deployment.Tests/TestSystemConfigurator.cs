using Asionyx.Library.Core;

namespace Asionyx.Services.Deployment.Tests
{
    public class TestSystemConfigurator : ISystemConfigurator
    {
        public string GetInfo() => "test-config";
        public void ApplyConfiguration(string json) { }
    }
}
