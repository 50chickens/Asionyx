using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Asionyx.Library.Testing;

public class SharedContainerManager : IAsyncDisposable
{
    private IContainer _container;
    private bool _startedContainer = false;

    public string TestHostPort { get; private set; }
    public System.Net.Http.HttpClient Client { get; private set; }
    public string ContainerApiKey { get; private set; }

    public async Task StartContainerAsync()
    {
        var apiKey = Guid.NewGuid().ToString("N");
        ContainerApiKey = apiKey;

        _container = new ContainerBuilder()
            .WithImage("asionyx/deployment:local")
            // Inject the API key as an environment variable so the service sees it at process start.
            .WithEnvironment("API_KEY", apiKey)
            // Ensure the service entrypoint waits briefly for the test startup callback
            // to write `appsettings.Development.json`. This wrapper waits up to 30s
            // for the file, then execs the real entrypoint at `/app/entrypoint.sh`.
            .WithEntrypoint("/bin/sh", "-c", "for i in 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16 17 18 19 20 21 22 23 24 25 26 27 28 29 30; do [ -f /app/appsettings.Development.json ] && exec /app/entrypoint.sh || sleep 1; done; exec /app/entrypoint.sh")
            // Write the generated API key into a known file inside the container at startup.
            // This mirrors the "startup callback" pattern used by the Sshd helpers and
            // makes the key available inside the container early in the lifecycle.
            .WithStartupCallback(async (container, ct) =>
            {
                try
                {
                    // Best-effort: write a minimal appsettings.Development.json containing the ApiKey.
                    var json = $"{{\"ApiKey\":\"{apiKey}\"}}";
                    await container.CopyAsync(System.Text.Encoding.UTF8.GetBytes(json + "\n"), "/app/appsettings.Development.json", ct: ct).ConfigureAwait(false);
                }
                catch
                {
                    // ignore - this is best-effort and should not fail container startup
                }
            })
            .WithCleanUp(true)
            .WithPortBinding(5000, true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(request => request.ForPort(5000).ForPath("/info"),
                    waitStrategy => waitStrategy.WithTimeout(TimeSpan.FromSeconds(90))))
            .Build();

        var startTimeoutSeconds = 150;
        var envTimeout = Environment.GetEnvironmentVariable("TEST_CONTAINER_START_TIMEOUT_SECONDS");
        if (!string.IsNullOrWhiteSpace(envTimeout) && int.TryParse(envTimeout, out var parsed))
        {
            startTimeoutSeconds = parsed;
        }

        using (var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(startTimeoutSeconds)))
        {
            await _container.StartAsync(cts.Token);
        }

        _startedContainer = true;

        var mappedPort = _container.GetMappedPublicPort(5000);
        TestHostPort = mappedPort.ToString();

        var baseAddress = new Uri($"http://localhost:{TestHostPort}");
        Client = new System.Net.Http.HttpClient { BaseAddress = baseAddress };
        // Try to discover the API key from inside the running container.
        try
        {
            // 1) Try to read appsettings.json or appsettings.Development.json and parse "ApiKey"
            var res = await _container.ExecAsync(new[] { "/bin/sh", "-c", "cat /app/appsettings.json || cat /app/appsettings.Development.json || true" });
            var stdout = res.Stdout ?? string.Empty;
            var m = System.Text.RegularExpressions.Regex.Match(stdout, "\"ApiKey\"\\s*:\\s*\"([^\"]+)\"");
            if (m.Success)
            {
                ContainerApiKey = m.Groups[1].Value;
            }
            else
            {
                // 2) Fallback: try environment variables inside the container
                var envRes = await _container.ExecAsync(new[] { "/bin/sh", "-c", "printenv X_API_KEY 2>/dev/null || printenv API_KEY 2>/dev/null || true" });
                var envStdout = envRes.Stdout?.Trim();
                if (!string.IsNullOrWhiteSpace(envStdout)) ContainerApiKey = envStdout;
            }
        }
        catch
        {
            // best-effort: do not fail container startup if discovery isn't possible in this environment
        }

        if (!string.IsNullOrWhiteSpace(ContainerApiKey))
        {
            Client.DefaultRequestHeaders.Add("X-API-KEY", ContainerApiKey);
        }

        // Always write container logs to artifacts/diagnostics after startup
        var (containerStdout, containerStderr) = _container != null ? await _container.GetLogsAsync() : (string.Empty, string.Empty);
        var diagDir = System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "artifacts", $"diagnostics_{DateTime.UtcNow:yyyyMMddHHmmss}");
        System.IO.Directory.CreateDirectory(diagDir);
        System.IO.File.WriteAllText(System.IO.Path.Combine(diagDir, "container-stdout.txt"), containerStdout ?? string.Empty);
        System.IO.File.WriteAllText(System.IO.Path.Combine(diagDir, "container-stderr.txt"), containerStderr ?? string.Empty);
    }

    public async ValueTask DisposeAsync()
    {
        if (_startedContainer && _container != null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }

    public async Task EnsureInfoAvailableAsync(int timeoutSeconds = 90)
    {
        if (Client == null) throw new InvalidOperationException("HttpClient not initialized");
        var attempts = Math.Max(1, timeoutSeconds);
        for (int i = 0; i < attempts; i++)
        {
            var resp = await Client.GetAsync("/info");
            if (resp.IsSuccessStatusCode) return;
            await Task.Delay(1000);
        }
        throw new TimeoutException("/info did not become available within the timeout");
    }
}
