using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;

[TestFixture]
public class DeploymentIntegrationTests
{
    [Test]
    public async Task InfoEndpoint_Returns_Info_From_Built_Image()
    {
        // The orchestrator should have built and tagged the image as 'asionyx/deployment:local'.
        var image = "asionyx/deployment:local";
        var containerName = "asionyx_integration_test";

    // Start or reuse a container. If an orchestrator-run container named 'asionyx_local' exists, reuse it.
    var reuseExisting = false;
    var startedContainer = false;
    // Check for orchestrator-managed container
    var checkInfo = new ProcessStartInfo("docker", "ps -a --filter name=asionyx_local --format \"{{.Names}}\"") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
    var checkProc = Process.Start(checkInfo);
    string checkOut = string.Empty;
    if (checkProc != null)
    {
        checkOut = (await checkProc.StandardOutput.ReadToEndAsync()).Trim();
        checkProc.WaitForExit(2000);
    }
    if (!string.IsNullOrWhiteSpace(checkOut))
    {
        containerName = "asionyx_local";
        reuseExisting = true;
    }

    if (!reuseExisting)
    {
        // Start the container (publish exposed ports to random host ports).
        // Ensure the test and container share an API key: prefer environment variable, otherwise generate one here and inject it.
        var apiKeyEnv = Environment.GetEnvironmentVariable("API_KEY");
        var usedApiKey = apiKeyEnv;
        if (string.IsNullOrWhiteSpace(usedApiKey))
        {
            usedApiKey = Guid.NewGuid().ToString("N");
            // export for later client use
            Environment.SetEnvironmentVariable("API_KEY", usedApiKey);
        }

        var runArgs = $"run -d --name {containerName} -P -e API_KEY={usedApiKey} {image}";
        var startInfo = new ProcessStartInfo("docker", runArgs) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        var proc = Process.Start(startInfo);
        if (proc == null) Assert.Fail("Failed to start docker process");
        var containerId = (await proc.StandardOutput.ReadToEndAsync()).Trim();
        proc.WaitForExit(5000);
        if (string.IsNullOrWhiteSpace(containerId))
        {
            var err = await proc.StandardError.ReadToEndAsync();
            Assert.Fail($"Failed to start container: {err}");
        }
        startedContainer = true;
    }

        try
        {
            // Get mapped host port for container's port 5000
            var portInfo = new ProcessStartInfo("docker", $"port {containerName} 5000") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
            var portProc = Process.Start(portInfo);
            var portOutput = (await portProc.StandardOutput.ReadToEndAsync()).Trim();
            portProc.WaitForExit(2000);
            if (string.IsNullOrWhiteSpace(portOutput)) Assert.Fail("Could not determine mapped host port for container");

            // portOutput looks like: 0.0.0.0:32768
            var parts = portOutput.Split(':');
            var hostPort = parts[^1];

            // Determine API key to use for authenticated endpoints. If not set in the test environment,
            // try to read it from the container's /etc/asionyx_api_key so client and server share the same key.
            var apiKey = Environment.GetEnvironmentVariable("API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                try
                {
                    var execInfo = new ProcessStartInfo("docker", $"exec {containerName} cat /etc/asionyx_api_key") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
                    var execProc = Process.Start(execInfo);
                    if (execProc != null)
                    {
                        apiKey = (await execProc.StandardOutput.ReadToEndAsync()).Trim();
                        execProc.WaitForExit(2000);
                    }
                }
                catch { /* ignore, apiKey may remain null */ }
            }

            using var client = new HttpClient();
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                if (client.DefaultRequestHeaders.Contains("X-API-KEY")) client.DefaultRequestHeaders.Remove("X-API-KEY");
                client.DefaultRequestHeaders.Add("X-API-KEY", apiKey);
            }

            // Poll the /info endpoint until it becomes available (timeout ~30s)
            HttpResponseMessage response = null;
            var attempts = 30;
            for (int i = 0; i < attempts; i++)
            {
                try
                {
                    response = await client.GetAsync($"http://localhost:{hostPort}/info");
                    if (response.IsSuccessStatusCode) break;
                }
                catch { /* ignore and retry */ }
                await Task.Delay(1000);
            }
            if (response == null) Assert.Fail("Failed to get response from /info");

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(body, Does.Contain("Asionyx.Services.Deployment"));

            // Verify /status endpoint
            var statusResp = await client.GetAsync($"http://localhost:{hostPort}/status");
            statusResp.EnsureSuccessStatusCode();
            var statusBody = await statusResp.Content.ReadAsStringAsync();
            Assert.That(statusBody, Does.Contain("status"));

            // Request the systemd emulator to start HelloWorld via the API
            var sysReq = new System.Net.Http.StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(new { Action = "start", Name = "Asionyx.Services.HelloWorld" }), System.Text.Encoding.UTF8, "application/json");
            var sysResp = await client.PostAsync($"http://localhost:{hostPort}/systemd", sysReq);
            sysResp.EnsureSuccessStatusCode();
            var sysBody = await sysResp.Content.ReadAsStringAsync();
            Assert.That(sysBody, Does.Contain("started") | Does.Contain("already running") | Does.Contain("Executable not found"));

            // Test package install/remove (small package 'sl')
            var pkgInstallReq = new System.Net.Http.StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(new { Action = "install", Packages = new[] { "sl" } }), System.Text.Encoding.UTF8, "application/json");
            var pkgInstallResp = await client.PostAsync($"http://localhost:{hostPort}/packages", pkgInstallReq);
            pkgInstallResp.EnsureSuccessStatusCode();
            var pkgInstallBody = await pkgInstallResp.Content.ReadAsStringAsync();
            Assert.That(pkgInstallBody, Does.Contain("installed") | Does.Contain("Setting up") | Does.Contain("is already the newest"));

            // Verify package appears in package list
            var pkgListResp = await client.GetAsync($"http://localhost:{hostPort}/packages");
            pkgListResp.EnsureSuccessStatusCode();
            var pkgListBody = await pkgListResp.Content.ReadAsStringAsync();
            Assert.That(pkgListBody, Does.Contain("sl") | Does.Contain(":/usr/games/sl") | Does.Contain("sl -"));

            // Remove the package
            var pkgRemoveReq = new System.Net.Http.StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(new { Action = "remove", Packages = new[] { "sl" } }), System.Text.Encoding.UTF8, "application/json");
            var pkgRemoveResp = await client.PostAsync($"http://localhost:{hostPort}/packages", pkgRemoveReq);
            pkgRemoveResp.EnsureSuccessStatusCode();
            var pkgRemoveBody = await pkgRemoveResp.Content.ReadAsStringAsync();
            Assert.That(pkgRemoveBody, Does.Contain("removed") | Does.Contain("Removing") | Does.Contain("not installed"));

            // Test filesystem: write, read, delete
            var testPath = "/tmp/asionyx_integration_test.txt";
            var writeReq = new System.Net.Http.StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(new { Action = "write", Path = testPath, Content = "hello-asio" }), System.Text.Encoding.UTF8, "application/json");
            var writeResp = await client.PostAsync($"http://localhost:{hostPort}/filesystem/files", writeReq);
            writeResp.EnsureSuccessStatusCode();

            var readResp = await client.GetAsync($"http://localhost:{hostPort}/filesystem/files?path={System.Uri.EscapeDataString(testPath)}");
            readResp.EnsureSuccessStatusCode();
            var readBody = await readResp.Content.ReadAsStringAsync();
            Assert.That(readBody, Does.Contain("hello-asio"));

            var delReq = new System.Net.Http.StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(new { Action = "delete", Path = testPath }), System.Text.Encoding.UTF8, "application/json");
            var delResp = await client.PostAsync($"http://localhost:{hostPort}/filesystem/files", delReq);
            delResp.EnsureSuccessStatusCode();

                // Test package upload: create a small "nupkg" zip with manifest.json and upload
                var tmp = System.IO.Path.GetTempPath();
                var pkgDir = System.IO.Path.Combine(tmp, "asionyx_pkg_test");
                try { if (System.IO.Directory.Exists(pkgDir)) System.IO.Directory.Delete(pkgDir, true); } catch { }
                System.IO.Directory.CreateDirectory(pkgDir);
                var manifestPath = System.IO.Path.Combine(pkgDir, "manifest.json");
                System.IO.File.WriteAllText(manifestPath, "{ \"name\": \"testpkg\", \"version\": \"0.1.0\" }");
                var nupkgPath = System.IO.Path.Combine(tmp, "testpkg.nupkg");
                try { if (System.IO.File.Exists(nupkgPath)) System.IO.File.Delete(nupkgPath); } catch { }
                System.IO.Compression.ZipFile.CreateFromDirectory(pkgDir, nupkgPath);

                using var form = new System.Net.Http.MultipartFormDataContent();
                using var fs = System.IO.File.OpenRead(nupkgPath);
                var fileContent = new System.Net.Http.StreamContent(fs);
                form.Add(fileContent, "file", "testpkg.nupkg");
                var uploadResp = await client.PostAsync($"http://localhost:{hostPort}/package", form);
                uploadResp.EnsureSuccessStatusCode();
                var uploadBody = await uploadResp.Content.ReadAsStringAsync();
                Assert.That(uploadBody, Does.Contain("testpkg") | Does.Contain("manifest"));
        }
        finally
        {
            // Clean up container only if we started it in this test run
            if (startedContainer)
            {
                var rmProc = Process.Start(new ProcessStartInfo("docker", $"rm -f {containerName}") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false });
                if (rmProc != null) rmProc.WaitForExit(5000);
            }
        }
    }
}
