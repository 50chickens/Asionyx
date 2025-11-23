using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Asionyx.Services.Deployment.Tests.Integration;

[TestFixture]
public class ApiKeyEnforcementTests
{
    [SetUp]
    public async Task SetUp()
    {
        await IntegrationTestSetup.ContainerManager.EnsureInfoAvailableAsync().ConfigureAwait(false);
    }

    [Test]
    public async Task Post_RequiresApiKey_Returns401_WhenMissing()
    {
        // Create a request that intentionally omits the X-API-KEY header
        var req = new HttpRequestMessage(HttpMethod.Post, "/packages")
        {
            Content = new StringContent(string.Empty, Encoding.UTF8, "application/json")
        };

        var client = new HttpClient { BaseAddress = IntegrationTestSetup.ContainerManager.Client.BaseAddress };

        var resp = await client.SendAsync(req).ConfigureAwait(false);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Post_WithApiKey_IsNot401()
    {
        // Use the shared client which includes the injected ContainerApiKey header.
        var resp = await IntegrationTestSetup.ContainerManager.Client.PostAsync("/packages", new StringContent(string.Empty, Encoding.UTF8, "application/json")).ConfigureAwait(false);
        Assert.That(resp.StatusCode, Is.Not.EqualTo(HttpStatusCode.Unauthorized));
    }
}
