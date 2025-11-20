using Newtonsoft.Json;

namespace Asionyx.Library.Shared.Diagnostics
{
    /// <summary>
    /// Diagnostics implementation that writes structured JSON to console stdout.
    /// This is intended for container deployments where stdout is captured by the
    /// container runtime (docker logs / testcontainers) and collected by the orchestrator.
    /// </summary>
    public class ConsoleDiagnostics : IAppDiagnostics
    {
        public Task<T?> ReadAsync<T>(string name, CancellationToken cancellationToken = default)
        {
            // Console-based diagnostics are ephemeral; reading by name is not supported.
            return Task.FromResult<T?>(default);
        }

        public Task WriteAsync(string name, object data, CancellationToken cancellationToken = default)
        {
            try
            {
                var obj = new { Name = name, Timestamp = DateTime.UtcNow, Payload = data };
                var json = JsonConvert.SerializeObject(obj);
                Console.Out.WriteLine(json);
            }
            catch
            {
                // Swallow any console errors to avoid masking application exceptions
            }

            return Task.CompletedTask;
        }
    }
}
