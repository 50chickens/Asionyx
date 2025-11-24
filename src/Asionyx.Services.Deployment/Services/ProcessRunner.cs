using System.Diagnostics;
namespace Asionyx.Services.Deployment.Services
{
    public static class ProcessRunner
    {
        public static Task<(int ExitCode, string StdOut, string StdErr)> RunAsync(string fileName, string arguments, int timeoutMs = 60000, CancellationToken ct = default)
        {
            // Delegate to DefaultProcessRunner for a single canonical implementation.
            var impl = new DefaultProcessRunner();
            return impl.RunAsync(fileName, arguments, timeoutMs, ct);
        }
    }
}
