using System.Diagnostics;

namespace Asionyx.Services.Deployment.Services
{
    public interface IProcessRunner
    {
        Task<(int ExitCode, string StdOut, string StdErr)> RunAsync(string fileName, string arguments, int timeoutMs = 60000, CancellationToken ct = default);
    }

    // Default implementation that runs processes using ProcessStartInfo.ArgumentList,
    // supports async reads, timeouts and CancellationToken.
    public class DefaultProcessRunner : IProcessRunner
    {
        public async Task<(int ExitCode, string StdOut, string StdErr)> RunAsync(string fileName, string arguments, int timeoutMs = 60000, CancellationToken ct = default)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // Populate ArgumentList in a way that respects quoted segments
                foreach (var arg in SplitArguments(arguments))
                {
                    if (!string.IsNullOrEmpty(arg)) psi.ArgumentList.Add(arg);
                }

                using var proc = new Process() { StartInfo = psi };
                proc.Start();

                var stdoutTask = proc.StandardOutput.ReadToEndAsync();
                var stderrTask = proc.StandardError.ReadToEndAsync();

                var waitForExitTask = proc.WaitForExitAsync(ct);

                var completed = await Task.WhenAny(waitForExitTask, Task.Delay(timeoutMs, ct));
                if (completed != waitForExitTask)
                {
                    try { proc.Kill(entireProcessTree: true); } catch { }
                    return (-1, string.Empty, "process timeout");
                }

                await Task.WhenAll(stdoutTask, stderrTask);

                return (proc.ExitCode, stdoutTask.Result, stderrTask.Result);
            }
            catch (OperationCanceledException)
            {
                return (-1, string.Empty, "cancelled");
            }
            catch (Exception ex)
            {
                return (-1, string.Empty, ex.ToString());
            }
        }

        // Simple argument splitter that handles quoted strings ("like this") and escapes.
        private static IEnumerable<string> SplitArguments(string args)
        {
            if (string.IsNullOrWhiteSpace(args)) yield break;

            var current = new System.Text.StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < args.Length; i++)
            {
                var c = args[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (!inQuotes && char.IsWhiteSpace(c))
                {
                    if (current.Length > 0)
                    {
                        yield return current.ToString();
                        current.Clear();
                    }
                }
                else
                {
                    current.Append(c);
                }
            }

            if (current.Length > 0) yield return current.ToString();
        }
    }
}
