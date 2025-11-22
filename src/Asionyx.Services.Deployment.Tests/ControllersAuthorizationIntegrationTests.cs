using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;

namespace Asionyx.Services.Deployment.Tests
{
    [TestFixture]
    public class ControllersAuthorizationIntegrationTests
    {
        [Test]
        public async Task FilesController_RequiresAuthorization()
        {
            Environment.SetEnvironmentVariable("X_API_KEY", "files-key");

            using var host = await new HostBuilder()
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.ConfigureServices(services =>
                    {
                        // Ensure controllers are discovered from the main assembly
                        services.AddRouting();
                        services.AddControllers().AddApplicationPart(typeof(Asionyx.Services.Deployment.Controllers.FilesController).Assembly);

                        services.AddAuthentication(options =>
                        {
                            options.DefaultAuthenticateScheme = Asionyx.Services.Deployment.Security.ApiKeyAuthenticationHandler.SchemeName;
                            options.DefaultChallengeScheme = Asionyx.Services.Deployment.Security.ApiKeyAuthenticationHandler.SchemeName;
                        }).AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, Asionyx.Services.Deployment.Security.ApiKeyAuthenticationHandler>(
                            Asionyx.Services.Deployment.Security.ApiKeyAuthenticationHandler.SchemeName, options => { });
                        services.AddAuthorization();
                    });
                    webHost.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseAuthentication();
                        app.UseAuthorization();
                        app.UseEndpoints(endpoints => endpoints.MapControllers());
                    });
                })
                .StartAsync();

            var client = host.GetTestClient();

            // Without header -> unauthorized
            var unauth = await client.GetAsync("/filesystem/files");
            Assert.That(unauth.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

            // With header -> should not be unauthorized (either 200 or 404 depending on FS), assert not 401
            client.DefaultRequestHeaders.Add("X-API-KEY", "files-key");
            var auth = await client.GetAsync("/filesystem/files");
            Assert.That(auth.StatusCode, Is.Not.EqualTo(HttpStatusCode.Unauthorized));
        }
    }

    // Test helper already exists in the test project: `TestApiKeyService.cs`.
}
