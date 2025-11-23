using System.Threading.Tasks;
using NUnit.Framework;

namespace Asionyx.Services.Deployment.IntegrationTests;

[TestFixture]
[Category("Integration")]
public class ClientIntegrationTests
{
    [OneTimeSetUp]
    public async Task OneTimeSetUp() => await IntegrationTestSetup.ContainerManager.EnsureInfoAvailableAsync();
    [Test]
    [Description("the client info endpoint test")]
    public async Task The_Client_Info_Endpoint_Test()
    {
        // Call /info without an API key. Do not reuse the ContainerManager's client since it may include the key header.
        var port = IntegrationTestSetup.ContainerManager.TestHostPort;
        var baseAddress = new System.Uri($"http://localhost:{port}");
        using var client = new System.Net.Http.HttpClient { BaseAddress = baseAddress };

        var resp = await client.GetAsync("/info");
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("Asionyx.Services.Deployment"));
    }
}
