using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Asionyx.Services.Deployment.Client.Tests;
[TestFixture]
public class ClientIntegrationTests
{
    [Test]
    public async Task Client_Can_Call_Info()
    {
        using var client = new HttpClient();
        var response = await client.GetAsync("http://localhost:5000/info");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("Asionyx.Services.Deployment"));
    }
}
