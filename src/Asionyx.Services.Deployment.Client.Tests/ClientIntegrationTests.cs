using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Asionyx.Services.Deployment.Client.Tests;

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
		// The integration container is started by the shared IntegrationTestSetup (it contains the published deployment, systemd emulator and helloworld).
		Assert.That(IntegrationTestSetup.ContainerManager, Is.Not.Null, "Shared ContainerManager must be initialized");

		var port = IntegrationTestSetup.ContainerManager.TestHostPort;
		var baseAddress = new System.Uri($"http://localhost:{port}");

		// Call the deployment service /info endpoint without an API key
		using var client = new HttpClient { BaseAddress = baseAddress };
		var resp = await client.GetAsync("/info");
		resp.EnsureSuccessStatusCode();
		var body = await resp.Content.ReadAsStringAsync();
		Assert.That(body, Does.Contain("Asionyx.Services.Deployment"));
	}
}

