using System.Threading;
using System.Threading.Tasks;
using Asionyx.Library.Shared.Diagnostics;

namespace Asionyx.Services.Deployment.Tests
{
    // Lightweight test implementation of IAppDiagnostics used by test Host builders.
    public class TestAppDiagnostics : IAppDiagnostics
    {
        public Task<T> ReadAsync<T>(string name, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<T>(default);
        }

        public Task WriteAsync(string name, object data, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
