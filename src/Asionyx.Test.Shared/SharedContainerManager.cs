using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Asionyx.Test.Shared;

public class SharedContainerManager : IAsyncDisposable
{
    private IContainer _container;
    private bool _startedContainer = false;

    public string TestHostPort { get; private set; }
    public System.Net.Http.HttpClient Client { get; private set; }
    public string ContainerApiKey { get; private set; }

        public async Task StartContainerAsync()
    {
            _container = new ContainerBuilder()
                .WithImage("asionyx/deployment:local")
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
        // Attempt to retrieve API key from the running container (avoid relying on env injection)
        try
        {
            // Try reading appsettings.json from the container first, then fall back to environment variables
            var containerId = _container.Id;
            string stdout = string.Empty;
            try
            {
                var psi = new ProcessStartInfo("docker", $"exec {containerId} sh -c 'cat /app/appsettings.json || cat /app/appsettings.Development.json'")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                if (p != null)
                {
                    stdout = await p.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                    var serr = await p.StandardError.ReadToEndAsync().ConfigureAwait(false);
                    p.WaitForExit();
                }
            }
            catch { }

            string apiKey = null;
            if (!string.IsNullOrWhiteSpace(stdout))
            {
                var m = System.Text.RegularExpressions.Regex.Match(stdout, "\"ApiKey\"\\s*:\\s*\"([^\"]+)\"");
                if (m.Success) apiKey = m.Groups[1].Value;
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                // fallback: printenv X_API_KEY or API_KEY
                try
                {
                    var psi2 = new ProcessStartInfo("docker", $"exec { _container.Id } sh -c 'printenv X_API_KEY 2>/dev/null || printenv API_KEY 2>/dev/null || true'")
                    {
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var p2 = Process.Start(psi2);
                    if (p2 != null)
                    {
                        var out2 = await p2.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                        p2.WaitForExit();
                        if (!string.IsNullOrWhiteSpace(out2)) apiKey = out2.Trim();
                    }
                }
                catch { }
            }

            ContainerApiKey = apiKey;
            if (!string.IsNullOrWhiteSpace(ContainerApiKey))
            {
                Client.DefaultRequestHeaders.Add("X-API-KEY", ContainerApiKey);
            }
        }
        catch { /* best-effort: do not fail startup when retrieving key for tests */ }

        // Always write container logs to artifacts/diagnostics after startup
        var (stdout, stderr) = _container != null ? await _container.GetLogsAsync() : (string.Empty, string.Empty);
        var diagDir = System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "artifacts", $"diagnostics_{DateTime.UtcNow:yyyyMMddHHmmss}");
        System.IO.Directory.CreateDirectory(diagDir);
        System.IO.File.WriteAllText(System.IO.Path.Combine(diagDir, "container-stdout.txt"), stdout ?? string.Empty);
        System.IO.File.WriteAllText(System.IO.Path.Combine(diagDir, "container-stderr.txt"), stderr ?? string.Empty);
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
