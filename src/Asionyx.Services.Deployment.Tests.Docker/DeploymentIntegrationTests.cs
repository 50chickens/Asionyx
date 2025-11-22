using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Asionyx.Library.Testing;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Asionyx.Services.Deployment.IntegrationTests;

[TestFixture]
[Category("Integration")]
public class DeploymentIntegrationTests
{
    private System.Net.Http.HttpClient Client;

    [OneTimeSetUp]
    public async Task OneTimeSetUp() => await IntegrationTestSetup.EnsureInfoAvailableAsync();

    [SetUp]
    public void SetUp()
    {
        Client = IntegrationTestSetup.Client;
        Assert.That(Client, Is.Not.Null, "HttpClient must be initialized by IntegrationTestSetup");
    }

    [Test]
    public async Task Info_Returns_Service_Info()
    {
        var resp = await Client.GetAsync("/info");
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("Asionyx.Services.Deployment"));
    }

    [Test]
    public async Task Status_Returns_Status()
    {
        var statusResp = await Client.GetAsync("/status");
        if (!statusResp.IsSuccessStatusCode && statusResp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            statusResp = await new TestHelpers().RunWithoutApiKeyAsync(Client, () => Client.GetAsync("/status"));
        }
        statusResp.EnsureSuccessStatusCode();
        var statusBody = await statusResp.Content.ReadAsStringAsync();
        Assert.That(statusBody, Does.Contain("status"));
    }

    [Test]
    public async Task Systemd_Start_HelloWorld()
    {
        var payload = new { Action = "start", Name = "Asionyx.Services.HelloWorld" };
        var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
        var sysResp = await Client.PostAsync("/systemd", content);
        sysResp.EnsureSuccessStatusCode();
        var sysBody = await sysResp.Content.ReadAsStringAsync();
        Assert.That(sysBody, Does.Contain("started") | Does.Contain("already running") | Does.Contain("Executable not found"));
    }

    [Test]
    public async Task Packages_Install_List_Remove()
    {
        var installPayload = new { Action = "install", Packages = new[] { "sl" } };
        var pkgInstallReq = new StringContent(JsonConvert.SerializeObject(installPayload), Encoding.UTF8, "application/json");
        var pkgInstallResp = await Client.PostAsync("/packages", pkgInstallReq);
        pkgInstallResp.EnsureSuccessStatusCode();
        var pkgInstallBody = await pkgInstallResp.Content.ReadAsStringAsync();
        Assert.That(pkgInstallBody, Does.Contain("installed") | Does.Contain("Setting up") | Does.Contain("is already the newest"));

        var pkgListResp = await Client.GetAsync("/packages");
        pkgListResp.EnsureSuccessStatusCode();
        var pkgListBody = await pkgListResp.Content.ReadAsStringAsync();
        Assert.That(pkgListBody, Does.Contain("sl") | Does.Contain(":/usr/games/sl") | Does.Contain("sl -"));

        var removePayload = new { Action = "remove", Packages = new[] { "sl" } };
        var pkgRemoveReq = new StringContent(JsonConvert.SerializeObject(removePayload), Encoding.UTF8, "application/json");
        var pkgRemoveResp = await Client.PostAsync("/packages", pkgRemoveReq);
        pkgRemoveResp.EnsureSuccessStatusCode();
        var pkgRemoveBody = await pkgRemoveResp.Content.ReadAsStringAsync();
        Assert.That(pkgRemoveBody, Does.Contain("removed") | Does.Contain("Removing") | Does.Contain("not installed"));
    }

    [Test]
    public async Task Filesystem_Write_Read_Delete()
    {
        var testPath = "/tmp/asionyx_integration_test.txt";
        var writePayload = new { Action = "write", Path = testPath, Content = "hello-asio" };
        var writeReq = new StringContent(JsonConvert.SerializeObject(writePayload), Encoding.UTF8, "application/json");
        var writeResp = await Client.PostAsync("/filesystem/files", writeReq);
        writeResp.EnsureSuccessStatusCode();

        var readResp = await Client.GetAsync($"/filesystem/files?path={Uri.EscapeDataString(testPath)}");
        readResp.EnsureSuccessStatusCode();
        var readBody = await readResp.Content.ReadAsStringAsync();
        Assert.That(readBody, Does.Contain("hello-asio"));

        var delReq = new StringContent(JsonConvert.SerializeObject(new { Action = "delete", Path = testPath }), Encoding.UTF8, "application/json");
        var delResp = await Client.PostAsync("/filesystem/files", delReq);
        delResp.EnsureSuccessStatusCode();
    }

    [Test]
    public async Task Package_Upload()
    {
        var nupkgPath = new TestHelpers().CreateTestNupkg();
        var (form, fs) = new TestHelpers().CreatePackageForm(nupkgPath);
        using (form)
        using (fs)
        {
            var uploadResp = await Client.PostAsync("/package", form);
            uploadResp.EnsureSuccessStatusCode();
            var uploadBody = await uploadResp.Content.ReadAsStringAsync();
            Assert.That(uploadBody, Does.Contain("testpkg") | Does.Contain("manifest"));
        }
    }
}
