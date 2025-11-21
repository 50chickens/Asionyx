using System;
using System.Threading.Tasks;
using NUnit.Framework;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Configurations;

[SetUpFixture]
public class IntegrationTestSetup
{
    private static IContainer _container;
    private bool _startedContainer = false;

    // Public static properties replace TEST_* environment variables.
    public static string TestHostPort { get; private set; }
    public static System.Net.Http.HttpClient Client { get; private set; }

    // Structured exec result for commands run inside the container.
    public record ExecResult(long ExitCode, string Stdout, string Stderr);

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        try
        {
            TestContext.Progress.WriteLine("Starting container setup...");

            var apiKey = Guid.NewGuid().ToString("N");

            // Use the repository's Testcontainers implementation (ContainerBuilder)
            // Enforce a 60 second readiness timeout via the wait strategy modifier.
            _container = new ContainerBuilder()
                .WithName("asionyx_integration_shared")
                .WithImage("asionyx/deployment:local")
                .WithCleanUp(true)
                .WithPortBinding(5000, true)
                .WithEnvironment("API_KEY", apiKey)
                // Ensure container stdout/stderr are forwarded to the test process so initialization logs appear in NUnit output.
                .WithOutputConsumer(Consume.RedirectStdoutAndStderrToConsole())
                .WithWaitStrategy(Wait.ForUnixContainer()
                    .UntilHttpRequestIsSucceeded(request => request.ForPort(5000).ForPath("/info"),
                        waitStrategy => waitStrategy.WithTimeout(TimeSpan.FromSeconds(60))))
                .Build();

            try
            {
                _container.StartAsync().GetAwaiter().GetResult();
                _startedContainer = true;

                var mappedPort = _container.GetMappedPublicPort(5000);
                TestHostPort = mappedPort.ToString();

                TestContext.Progress.WriteLine($"Container setup succeeded: port={TestHostPort}");

                // Create a shared HttpClient for tests targeting the container.
                var baseAddress = new Uri($"http://localhost:{TestHostPort}");
                Client = new System.Net.Http.HttpClient { BaseAddress = baseAddress };

                // Attempt to read the API key from the container; if found, add it as a default header.
                try
                {
                    var containerApiKey = string.Empty;
                    for (int i = 0; i < 30; i++)
                    {
                        var keyExec = ExecInContainerAsync(new[] { "/bin/sh", "-c", "cat /etc/asionyx_api_key" }).GetAwaiter().GetResult();
                        if (keyExec != null && keyExec.ExitCode == 0 && !string.IsNullOrWhiteSpace(keyExec.Stdout))
                        {
                            containerApiKey = keyExec.Stdout.Trim();
                            break;
                        }
                        System.Threading.Thread.Sleep(500);
                    }
                    if (!string.IsNullOrWhiteSpace(containerApiKey)) Client.DefaultRequestHeaders.Add("X-API-KEY", containerApiKey);
                }
                catch { }
            }
            catch (Exception startEx)
            {
                TestContext.Progress.WriteLine($"Container setup failed: {startEx.Message}");

                // Attempt to retrieve container logs to aid diagnosis.
                try
                {
                    var (stdout, stderr) = _container != null ? _container.GetLogsAsync().GetAwaiter().GetResult() : (string.Empty, string.Empty);
                    if (!string.IsNullOrWhiteSpace(stdout)) TestContext.Progress.WriteLine("---- container stdout ----\n" + stdout);
                    if (!string.IsNullOrWhiteSpace(stderr)) TestContext.Progress.WriteLine("---- container stderr ----\n" + stderr);
                }
                catch (Exception logEx)
                {
                    TestContext.Progress.WriteLine($"Failed to read container logs: {logEx.Message}");
                }

                throw;
            }
        }
        catch (Exception ex)
        {
            TestContext.Progress.WriteLine($"IntegrationTestSetup failed: {ex.Message}");
            throw;
        }
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        if (_startedContainer && _container != null)
        {
            TestContext.Progress.WriteLine("Stopping container...");
            try
            {
                _container.StopAsync().GetAwaiter().GetResult();
                _container.DisposeAsync().GetAwaiter().GetResult();
                TestContext.Progress.WriteLine("Container stopped.");
            }
            catch (Exception ex)
            {
                TestContext.Progress.WriteLine($"Container stop/cleanup failed: {ex.Message}");
            }
        }
    }

    // Provides a small helper to run commands inside the started container and return stdout.
    public static async Task<string> ReadFileFromContainerAsync(string path)
    {
        try
        {
            if (_container == null) return null;
            var cmd = new[] { "/bin/sh", "-c", $"cat {path}" };
            var execResult = await _container.ExecAsync(cmd, default);
            if (execResult.ExitCode == 0)
            {
                return execResult.Stdout?.Trim() ?? string.Empty;
            }
        }
        catch { }
        return string.Empty;
    }

    // Run an arbitrary command inside the container and return a structured result.
    public static async Task<ExecResult> ExecInContainerAsync(string[] command)
    {
        if (_container == null) return new ExecResult(-1, string.Empty, string.Empty);
        try
        {
            var exec = await _container.ExecAsync(command, default);
            return new ExecResult(exec.ExitCode, exec.Stdout ?? string.Empty, exec.Stderr ?? string.Empty);
        }
        catch (Exception ex)
        {
            return new ExecResult(-1, string.Empty, ex.Message);
        }
    }

    // Wait for /info to become available. Used by tests to ensure readiness.
    public static async Task EnsureInfoAvailableAsync(int timeoutSeconds = 60)
    {
        if (Client == null) throw new InvalidOperationException("HttpClient not initialized");
        var attempts = Math.Max(1, timeoutSeconds);
        for (int i = 0; i < attempts; i++)
        {
            try
            {
                var resp = await Client.GetAsync("/info");
                if (resp.IsSuccessStatusCode) return;
            }
            catch { }
            await Task.Delay(1000);
        }
        throw new TimeoutException("/info did not become available within the timeout");
    }
}
