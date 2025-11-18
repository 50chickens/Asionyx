using System.Net.Http;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.WaitStrategies;
using NUnit.Framework;

[TestFixture]
public class DeploymentIntegrationTests
{
    [Test, Explicit("Requires docker engine and built image; run as part of orchestrator")]
    public async Task InfoEndpoint_Returns_Info_From_Built_Image()
    {
        // The orchestrator should have built and tagged the image as 'asionyx/deployment:local'.
        var image = "asionyx/deployment:local";

        var containerBuilder = new TestcontainersBuilder<TestcontainersContainer>()
            .WithImage(image)
            .WithName("asionyx_integration_test")
            .WithExposedPort(5000)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(req => req.ForPath("/info").ForPort(5000)));

        await using var container = containerBuilder.Build();
        await container.StartAsync();

        var host = container.Hostname;
        var port = container.GetMappedPublicPort(5000);
        using var client = new HttpClient();
        var response = await client.GetAsync($"http://{host}:{port}/info");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("Asionyx.Services.Deployment"));

        await container.StopAsync();
    }
}
