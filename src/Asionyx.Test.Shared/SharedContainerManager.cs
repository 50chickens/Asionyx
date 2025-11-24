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
        var apiKey = Guid.NewGuid().ToString("N");
        ContainerApiKey = apiKey;

        _container = new ContainerBuilder()
            .WithImage("asionyx/deployment:local")
            .WithCleanUp(true)
            .WithPortBinding(5000, true)
            // Tests inject the API key into the container via the `API_KEY` environment variable.
            .WithEnvironment("API_KEY", apiKey)
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
        if (!string.IsNullOrWhiteSpace(ContainerApiKey))
        {
            // Client uses the HTTP header `X-API-KEY` when making requests, but the container
            // receives the secret via `API_KEY` env var at startup (do not log it).
            Client.DefaultRequestHeaders.Add("X-API-KEY", ContainerApiKey);
        }

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
