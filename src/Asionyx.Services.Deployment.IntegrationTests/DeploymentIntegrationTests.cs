using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Asionyx.Library.Testing;

namespace Asionyx.Services.Deployment.IntegrationTests;

[TestFixture]
[Category("Integration")]
public class DeploymentIntegrationTests
{
    [OneTimeSetUp]
    public async Task OneTimeSetUp() => await IntegrationTestSetup.EnsureInfoAvailableAsync();

    [Test]
    public async Task Info_Returns_Service_Info()
    {
        var client = IntegrationTestSetup.Client;
        Assert.That(client, Is.Not.Null, "HttpClient must be initialized by IntegrationTestSetup");

        var resp = await client.GetAsync("/info");
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("Asionyx.Services.Deployment"));
    }

    [Test]
    public async Task Status_Returns_Status()
    {
        var client = IntegrationTestSetup.Client;
        Assert.That(client, Is.Not.Null, "HttpClient must be initialized by IntegrationTestSetup");

        var statusResp = await client.GetAsync("/status");
        if (!statusResp.IsSuccessStatusCode && statusResp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            statusResp = await TestHelpers.RunWithoutApiKeyAsync(client, () => client.GetAsync("/status"));
        }
        statusResp.EnsureSuccessStatusCode();
        var statusBody = await statusResp.Content.ReadAsStringAsync();
        Assert.That(statusBody, Does.Contain("status"));
    }

    [Test]
    public async Task Systemd_Start_HelloWorld()
    {
        var client = IntegrationTestSetup.Client;
        Assert.That(client, Is.Not.Null, "HttpClient must be initialized by IntegrationTestSetup");

        var payload = new { Action = "start", Name = "Asionyx.Services.HelloWorld" };
        var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
        var sysResp = await client.PostAsync("/systemd", content);
        sysResp.EnsureSuccessStatusCode();
        var sysBody = await sysResp.Content.ReadAsStringAsync();
        Assert.That(sysBody, Does.Contain("started") | Does.Contain("already running") | Does.Contain("Executable not found"));
    }

    [Test]
    public async Task Packages_Install_List_Remove()
    {
        var client = IntegrationTestSetup.Client;
        Assert.That(client, Is.Not.Null, "HttpClient must be initialized by IntegrationTestSetup");

        var installPayload = new { Action = "install", Packages = new[] { "sl" } };
        var pkgInstallReq = new StringContent(JsonConvert.SerializeObject(installPayload), Encoding.UTF8, "application/json");
        var pkgInstallResp = await client.PostAsync("/packages", pkgInstallReq);
        pkgInstallResp.EnsureSuccessStatusCode();
        var pkgInstallBody = await pkgInstallResp.Content.ReadAsStringAsync();
        Assert.That(pkgInstallBody, Does.Contain("installed") | Does.Contain("Setting up") | Does.Contain("is already the newest"));

        var pkgListResp = await client.GetAsync("/packages");
        pkgListResp.EnsureSuccessStatusCode();
        var pkgListBody = await pkgListResp.Content.ReadAsStringAsync();
        Assert.That(pkgListBody, Does.Contain("sl") | Does.Contain(":/usr/games/sl") | Does.Contain("sl -"));

        var removePayload = new { Action = "remove", Packages = new[] { "sl" } };
        var pkgRemoveReq = new StringContent(JsonConvert.SerializeObject(removePayload), Encoding.UTF8, "application/json");
        var pkgRemoveResp = await client.PostAsync("/packages", pkgRemoveReq);
        pkgRemoveResp.EnsureSuccessStatusCode();
        var pkgRemoveBody = await pkgRemoveResp.Content.ReadAsStringAsync();
        Assert.That(pkgRemoveBody, Does.Contain("removed") | Does.Contain("Removing") | Does.Contain("not installed"));
    }

    [Test]
    public async Task Filesystem_Write_Read_Delete()
    {
        var client = IntegrationTestSetup.Client;
        Assert.That(client, Is.Not.Null, "HttpClient must be initialized by IntegrationTestSetup");

        var testPath = "/tmp/asionyx_integration_test.txt";
        var writePayload = new { Action = "write", Path = testPath, Content = "hello-asio" };
        var writeReq = new StringContent(JsonConvert.SerializeObject(writePayload), Encoding.UTF8, "application/json");
        var writeResp = await client.PostAsync("/filesystem/files", writeReq);
        writeResp.EnsureSuccessStatusCode();

        var readResp = await client.GetAsync($"/filesystem/files?path={Uri.EscapeDataString(testPath)}");
        readResp.EnsureSuccessStatusCode();
        var readBody = await readResp.Content.ReadAsStringAsync();
        Assert.That(readBody, Does.Contain("hello-asio"));

        var delReq = new StringContent(JsonConvert.SerializeObject(new { Action = "delete", Path = testPath }), Encoding.UTF8, "application/json");
        var delResp = await client.PostAsync("/filesystem/files", delReq);
        delResp.EnsureSuccessStatusCode();
    }

    [Test]
    public async Task Package_Upload()
    {
        var client = IntegrationTestSetup.Client;
        Assert.That(client, Is.Not.Null, "HttpClient must be initialized by IntegrationTestSetup");

        var nupkgPath = TestHelpers.CreateTestNupkg();
        var (form, fs) = TestHelpers.CreatePackageForm(nupkgPath);
        using (form)
        using (fs)
        {
            var uploadResp = await client.PostAsync("/package", form);
            uploadResp.EnsureSuccessStatusCode();
            var uploadBody = await uploadResp.Content.ReadAsStringAsync();
            Assert.That(uploadBody, Does.Contain("testpkg") | Does.Contain("manifest"));
        }
    }
}
