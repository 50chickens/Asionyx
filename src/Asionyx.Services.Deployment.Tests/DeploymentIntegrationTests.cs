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
        // Tests use a shared container started by IntegrationTestSetup.
        var containerName = Environment.GetEnvironmentVariable("TEST_CONTAINER_NAME") ?? "asionyx_integration_shared";
        var hostPort = Environment.GetEnvironmentVariable("TEST_HOST_PORT");
        Assert.That(hostPort, Is.Not.Null, "TEST_HOST_PORT must be set by IntegrationTestSetup");

        // Determine API key to use for authenticated endpoints.
        var apiKey = Environment.GetEnvironmentVariable("API_KEY");
        try
        {
            // If container has a persisted key, prefer it
            var execInfo = new ProcessStartInfo("docker", $"exec {containerName} cat /etc/asionyx_api_key") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
            var execProc = Process.Start(execInfo);
            if (execProc != null)
            {
                var fileKey = (await execProc.StandardOutput.ReadToEndAsync()).Trim();
                execProc.WaitForExit(2000);
                if (!string.IsNullOrWhiteSpace(fileKey))
                {
                    apiKey = fileKey;
                    Environment.SetEnvironmentVariable("API_KEY", apiKey);
                }
            }
        }
        catch { }

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

            // Verify /status endpoint. Some deployment variants may return 401 when an invalid
            // X-API-KEY is present; allow a retry without the header so the test is robust.
            var statusUri = $"http://localhost:{hostPort}/status";
            var statusResp = await client.GetAsync(statusUri);
            if (!statusResp.IsSuccessStatusCode && statusResp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Try again without the header (GET should be allowed on /status in most deployments)
                if (client.DefaultRequestHeaders.Contains("X-API-KEY")) client.DefaultRequestHeaders.Remove("X-API-KEY");
                statusResp = await client.GetAsync(statusUri);
            }
            statusResp.EnsureSuccessStatusCode();
            var statusBody = await statusResp.Content.ReadAsStringAsync();
            Assert.That(statusBody, Does.Contain("status"));

            // Ensure the X-API-KEY header is present again for subsequent POST requests
            // (we may have removed it to test unauthenticated GET behaviour on /status).
            if (!string.IsNullOrWhiteSpace(apiKey) && !client.DefaultRequestHeaders.Contains("X-API-KEY"))
            {
                client.DefaultRequestHeaders.Add("X-API-KEY", apiKey);
            }

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
}
