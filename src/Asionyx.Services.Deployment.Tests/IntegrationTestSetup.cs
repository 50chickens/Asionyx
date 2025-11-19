using System;
using System.Diagnostics;
using NUnit.Framework;

[SetUpFixture]
public class IntegrationTestSetup
{
    private bool _startedContainer = false;
    private string _containerName = "asionyx_integration_shared";

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        // If an orchestrator-managed container exists, prefer reusing it
        try
        {
            var checkInfo = new ProcessStartInfo("docker", "ps -a --filter name=asionyx_local --format \"{{.Names}}\"") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
            var checkProc = Process.Start(checkInfo);
            string checkOut = string.Empty;
            if (checkProc != null)
            {
                checkOut = checkProc.StandardOutput.ReadToEnd().Trim();
                checkProc.WaitForExit(2000);
            }
            if (!string.IsNullOrWhiteSpace(checkOut))
            {
                _containerName = "asionyx_local";
                Environment.SetEnvironmentVariable("TEST_CONTAINER_NAME", _containerName);
                // determine mapped host port
                var portInfo = new ProcessStartInfo("docker", $"port {_containerName} 5000") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
                var portProc = Process.Start(portInfo);
                var portOutput = portProc?.StandardOutput.ReadToEnd().Trim();
                portProc?.WaitForExit(2000);
                if (!string.IsNullOrWhiteSpace(portOutput))
                {
                    var hostPort = portOutput.Split(':')[^1];
                    Environment.SetEnvironmentVariable("TEST_HOST_PORT", hostPort);
                }
                return;
            }

            // Start a shared container for integration tests
            var apiKey = Environment.GetEnvironmentVariable("API_KEY") ?? Guid.NewGuid().ToString("N");
            Environment.SetEnvironmentVariable("API_KEY", apiKey);

            var runArgs = $"run -d --name {_containerName} -P -e API_KEY={apiKey} asionyx/deployment:local";
            var startInfo = new ProcessStartInfo("docker", runArgs) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
            var proc = Process.Start(startInfo);
            if (proc == null) throw new Exception("Failed to start docker process");
            var containerId = proc.StandardOutput.ReadToEnd().Trim();
            proc.WaitForExit(5000);
            if (string.IsNullOrWhiteSpace(containerId))
            {
                var err = proc.StandardError.ReadToEnd();
                throw new Exception($"Failed to start container: {err}");
            }
            _startedContainer = true;
            Environment.SetEnvironmentVariable("TEST_CONTAINER_NAME", _containerName);

            // determine mapped host port
            var portInfo2 = new ProcessStartInfo("docker", $"port {_containerName} 5000") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
            var portProc2 = Process.Start(portInfo2);
            var portOutput2 = portProc2?.StandardOutput.ReadToEnd().Trim();
            portProc2?.WaitForExit(2000);
            if (!string.IsNullOrWhiteSpace(portOutput2))
            {
                var hostPort = portOutput2.Split(':')[^1];
                Environment.SetEnvironmentVariable("TEST_HOST_PORT", hostPort);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"IntegrationTestSetup failed: {ex.Message}");
            throw;
        }
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        if (_startedContainer)
        {
            try
            {
                var rmProc = Process.Start(new ProcessStartInfo("docker", $"rm -f {_containerName}") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false });
                rmProc?.WaitForExit(5000);
            }
            catch { }
        }
    }
}
