using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Asionyx.Services.Deployment.Tests.Integration;

[TestFixture]
public class ApiKeyEnforcementTests
{
    [SetUp]
    public async Task SetUp()
    {
        await IntegrationTestSetup.EnsureInfoAvailableAsync().ConfigureAwait(false);
    }

    [Test]
    public async Task Post_RequiresApiKey_Returns401_WhenMissing()
    {
        // Create a request that intentionally omits the X-API-KEY header
        var req = new HttpRequestMessage(HttpMethod.Post, "/packages")
        {
            Content = new StringContent(string.Empty)
        };

        var client = new HttpClient { BaseAddress = IntegrationTestSetup.Client.BaseAddress };

        var resp = await client.SendAsync(req).ConfigureAwait(false);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Post_WithApiKey_IsNot401()
    {
        // Use the shared client which includes the injected ContainerApiKey header.
        var resp = await IntegrationTestSetup.Client.PostAsync("/packages", new StringContent(string.Empty)).ConfigureAwait(false);
        Assert.That(resp.StatusCode, Is.Not.EqualTo(HttpStatusCode.Unauthorized));
    }
}
