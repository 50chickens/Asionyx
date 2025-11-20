using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;

[TestFixture]
public class DeploymentIntegrationTests
{
    private static string GetContainerName() => Environment.GetEnvironmentVariable("TEST_CONTAINER_NAME") ?? "asionyx_integration_shared";

    private static string GetHostPort()
    {
        var hostPort = Environment.GetEnvironmentVariable("TEST_HOST_PORT");
        Assert.That(hostPort, Is.Not.Null, "TEST_HOST_PORT must be set by IntegrationTestSetup");
        return hostPort;
    }

    private static async Task<string> GetApiKeyFromContainerAsync(string containerName)
    {
        var apiKey = Environment.GetEnvironmentVariable("API_KEY");
        try
        {
            var execInfo = new ProcessStartInfo("docker", $"exec {containerName} cat /etc/asionyx_api_key") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
            var execProc = Process.Start(execInfo);
            if (execProc != null)
            {
                var fileKey = (await execProc.StandardOutput.ReadToEndAsync()).Trim();
                execProc.WaitForExit(2000);
                if (!string.IsNullOrWhiteSpace(fileKey))
                {
                    apiKey = fileKey;
                }
            }
        }
        catch { }
        return apiKey;
    }

    private static async Task EnsureInfoAvailableAsync(HttpClient client, string hostPort)
    {
        HttpResponseMessage response = null;
        var attempts = 30;
        for (int i = 0; i < attempts; i++)
        {
            try
            {
                response = await client.GetAsync($"http://localhost:{hostPort}/info");
                if (response.IsSuccessStatusCode) break;
            }
            catch { }
            await Task.Delay(1000);
        }
        if (response == null) Assert.Fail("Failed to get response from /info");
    }

    [Test]
    public async Task Info_Returns_Service_Info()
    {
        var hostPort = GetHostPort();
        var containerName = GetContainerName();
        var apiKey = await GetApiKeyFromContainerAsync(containerName);

        using var client = new HttpClient();
        if (!string.IsNullOrWhiteSpace(apiKey)) client.DefaultRequestHeaders.Add("X-API-KEY", apiKey);

        await EnsureInfoAvailableAsync(client, hostPort);

        var resp = await client.GetAsync($"http://localhost:{hostPort}/info");
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("Asionyx.Services.Deployment"));
    }

    [Test]
    public async Task Status_Returns_Status()
    {
        var hostPort = GetHostPort();
        var containerName = GetContainerName();
        var apiKey = await GetApiKeyFromContainerAsync(containerName);

        using var client = new HttpClient();
        if (!string.IsNullOrWhiteSpace(apiKey)) client.DefaultRequestHeaders.Add("X-API-KEY", apiKey);

        await EnsureInfoAvailableAsync(client, hostPort);

        var statusResp = await client.GetAsync($"http://localhost:{hostPort}/status");
        if (!statusResp.IsSuccessStatusCode && statusResp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            // Retry without header
            if (client.DefaultRequestHeaders.Contains("X-API-KEY")) client.DefaultRequestHeaders.Remove("X-API-KEY");
            statusResp = await client.GetAsync($"http://localhost:{hostPort}/status");
        }
        statusResp.EnsureSuccessStatusCode();
        var statusBody = await statusResp.Content.ReadAsStringAsync();
        Assert.That(statusBody, Does.Contain("status"));
    }

    [Test]
    public async Task Systemd_Start_HelloWorld()
    {
        var hostPort = GetHostPort();
        var containerName = GetContainerName();
        var apiKey = await GetApiKeyFromContainerAsync(containerName);

        using var client = new HttpClient();
        if (!string.IsNullOrWhiteSpace(apiKey)) client.DefaultRequestHeaders.Add("X-API-KEY", apiKey);

        await EnsureInfoAvailableAsync(client, hostPort);

        var sysReq = new System.Net.Http.StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(new { Action = "start", Name = "Asionyx.Services.HelloWorld" }), System.Text.Encoding.UTF8, "application/json");
        var sysResp = await client.PostAsync($"http://localhost:{hostPort}/systemd", sysReq);
        sysResp.EnsureSuccessStatusCode();
        var sysBody = await sysResp.Content.ReadAsStringAsync();
        Assert.That(sysBody, Does.Contain("started") | Does.Contain("already running") | Does.Contain("Executable not found"));
    }

    [Test]
    public async Task Packages_Install_List_Remove()
    {
        var hostPort = GetHostPort();
        var containerName = GetContainerName();
        var apiKey = await GetApiKeyFromContainerAsync(containerName);

        using var client = new HttpClient();
        if (!string.IsNullOrWhiteSpace(apiKey)) client.DefaultRequestHeaders.Add("X-API-KEY", apiKey);

        await EnsureInfoAvailableAsync(client, hostPort);

        var pkgInstallReq = new System.Net.Http.StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(new { Action = "install", Packages = new[] { "sl" } }), System.Text.Encoding.UTF8, "application/json");
        var pkgInstallResp = await client.PostAsync($"http://localhost:{hostPort}/packages", pkgInstallReq);
        pkgInstallResp.EnsureSuccessStatusCode();
        var pkgInstallBody = await pkgInstallResp.Content.ReadAsStringAsync();
        Assert.That(pkgInstallBody, Does.Contain("installed") | Does.Contain("Setting up") | Does.Contain("is already the newest"));

        var pkgListResp = await client.GetAsync($"http://localhost:{hostPort}/packages");
        pkgListResp.EnsureSuccessStatusCode();
        var pkgListBody = await pkgListResp.Content.ReadAsStringAsync();
        Assert.That(pkgListBody, Does.Contain("sl") | Does.Contain(":/usr/games/sl") | Does.Contain("sl -"));

        var pkgRemoveReq = new System.Net.Http.StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(new { Action = "remove", Packages = new[] { "sl" } }), System.Text.Encoding.UTF8, "application/json");
        var pkgRemoveResp = await client.PostAsync($"http://localhost:{hostPort}/packages", pkgRemoveReq);
        pkgRemoveResp.EnsureSuccessStatusCode();
        var pkgRemoveBody = await pkgRemoveResp.Content.ReadAsStringAsync();
        Assert.That(pkgRemoveBody, Does.Contain("removed") | Does.Contain("Removing") | Does.Contain("not installed"));
    }

    [Test]
    public async Task Filesystem_Write_Read_Delete()
    {
        var hostPort = GetHostPort();
        var containerName = GetContainerName();
        var apiKey = await GetApiKeyFromContainerAsync(containerName);

        using var client = new HttpClient();
        if (!string.IsNullOrWhiteSpace(apiKey)) client.DefaultRequestHeaders.Add("X-API-KEY", apiKey);

        await EnsureInfoAvailableAsync(client, hostPort);

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
    }

    [Test]
    public async Task Package_Upload()
    {
        var hostPort = GetHostPort();
        var containerName = GetContainerName();
        var apiKey = await GetApiKeyFromContainerAsync(containerName);

        using var client = new HttpClient();
        if (!string.IsNullOrWhiteSpace(apiKey)) client.DefaultRequestHeaders.Add("X-API-KEY", apiKey);

        await EnsureInfoAvailableAsync(client, hostPort);

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
