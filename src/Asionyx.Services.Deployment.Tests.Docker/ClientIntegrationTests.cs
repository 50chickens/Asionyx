using System.Threading.Tasks;
using NUnit.Framework;

namespace Asionyx.Services.Deployment.IntegrationTests;

[TestFixture]
[Category("Integration")]
public class ClientIntegrationTests
{
    [OneTimeSetUp]
    public async Task OneTimeSetUp() => await IntegrationTestSetup.EnsureInfoAvailableAsync();

    [Test]
    public async Task Client_Can_Call_Info()
    {
        var client = IntegrationTestSetup.Client;
        Assert.That(client, Is.Not.Null, "HttpClient must be initialized by IntegrationTestSetup");

        var resp = await client.GetAsync("/info");
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("Asionyx.Services.Deployment"));
    }
}
