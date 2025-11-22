using System.Threading.Tasks;
using Asionyx.Library.Core;

namespace Asionyx.Services.Deployment.Tests
{
    public class TestApiKeyService : IApiKeyService
    {
        private readonly string _valid;
        public TestApiKeyService(string valid) { _valid = valid; }
        public bool Validate(string key) => key == _valid;
        public Task<string> EnsureApiKeyAsync() => Task.FromResult(_valid);
    }
}
