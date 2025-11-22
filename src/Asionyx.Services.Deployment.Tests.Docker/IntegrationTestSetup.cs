using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using NUnit.Framework;

[SetUpFixture]
public class IntegrationTestSetup
{
    private static IContainer _container;
    private bool _startedContainer = false;

    // Public static properties replace TEST_* environment variables.
    public static string TestHostPort { get; private set; }
    public static System.Net.Http.HttpClient Client { get; private set; }
    // The API key injected into the container at start time. Tests can use this when calling protected endpoints.
    public static string ContainerApiKey { get; private set; }

    // Structured exec result for commands run inside the container.
    public record ExecResult(long ExitCode, string Stdout, string Stderr);

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        TestContext.Progress.WriteLine("Starting container setup...");

        var apiKey = Guid.NewGuid().ToString("N");
        ContainerApiKey = apiKey;

        _container = new ContainerBuilder()
            .WithName("asionyx_integration_shared")
            .WithImage("asionyx/deployment:local")
            .WithCleanUp(true)
            .WithPortBinding(5000, true)
            .WithEnvironment("X_API_KEY", apiKey)
            // .WithOutputConsumer(Consume.RedirectStdoutAndStderrToConsole())
            // .WithLogger(new ConsoleLogger())
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(request => request.ForPort(5000).ForPath("/info"),
                    waitStrategy => waitStrategy.WithTimeout(TimeSpan.FromSeconds(90))))
            .Build();

        // Start the container with a cancellation token to enforce a hard timeout
        var startTimeoutSeconds = 150;
        var envTimeout = Environment.GetEnvironmentVariable("TEST_CONTAINER_START_TIMEOUT_SECONDS");
        if (!string.IsNullOrWhiteSpace(envTimeout) && int.TryParse(envTimeout, out var parsed))
        {
            startTimeoutSeconds = parsed;
        }

        using (var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(startTimeoutSeconds)))
        {
            _container.StartAsync(cts.Token).GetAwaiter().GetResult();
        }

        _startedContainer = true;

        var mappedPort = _container.GetMappedPublicPort(5000);
        TestHostPort = mappedPort.ToString();
        TestContext.Progress.WriteLine($"Container setup succeeded: port={TestHostPort}");

        // Create a shared HttpClient for tests targeting the container.
        var baseAddress = new Uri($"http://localhost:{TestHostPort}");
        Client = new System.Net.Http.HttpClient { BaseAddress = baseAddress };

        if (!string.IsNullOrWhiteSpace(ContainerApiKey))
        {
            Client.DefaultRequestHeaders.Add("X-API-KEY", ContainerApiKey);
        }

        // Always write container logs to artifacts/diagnostics after startup
        var (stdout, stderr) = _container != null ? _container.GetLogsAsync().GetAwaiter().GetResult() : (string.Empty, string.Empty);
        var diagDir = System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "artifacts", $"diagnostics_{DateTime.UtcNow:yyyyMMddHHmmss}");
        System.IO.Directory.CreateDirectory(diagDir);
        System.IO.File.WriteAllText(System.IO.Path.Combine(diagDir, "container-stdout.txt"), stdout ?? string.Empty);
        System.IO.File.WriteAllText(System.IO.Path.Combine(diagDir, "container-stderr.txt"), stderr ?? string.Empty);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        if (_startedContainer && _container != null)
        {
            TestContext.Progress.WriteLine("Stopping container...");
            _container.StopAsync().GetAwaiter().GetResult();
            _container.DisposeAsync().GetAwaiter().GetResult();
            TestContext.Progress.WriteLine("Container stopped.");
        }
    }

    // Provides a small helper to run commands inside the started container and return stdout.
    public static async Task<string> ReadFileFromContainerAsync(string path)
    {
        if (_container == null) return null;
        var cmd = new[] { "/bin/sh", "-c", $"cat {path}" };
        var execResult = await _container.ExecAsync(cmd, default);
        if (execResult.ExitCode == 0)
        {
            return execResult.Stdout?.Trim() ?? string.Empty;
        }
        return string.Empty;
    }

    // Run an arbitrary command inside the container and return a structured result.
    public static async Task<ExecResult> ExecInContainerAsync(string[] command)
    {
        if (_container == null) return new ExecResult(-1, string.Empty, string.Empty);
        var exec = await _container.ExecAsync(command, default);
        return new ExecResult(exec.ExitCode, exec.Stdout ?? string.Empty, exec.Stderr ?? string.Empty);
    }

    // Wait for /info to become available. Used by tests to ensure readiness.
    public static async Task EnsureInfoAvailableAsync(int timeoutSeconds = 90)
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
