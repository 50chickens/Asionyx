using System;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace Asionyx.Services.Deployment.Tests
{
    [TestFixture]
    public class ApiKeyAuthenticationHandlerTests
    {
        [Test]
        public async Task MissingApiKey_Returns_Unauthorized()
        {
            Environment.SetEnvironmentVariable("X_API_KEY", null);

            using var host = await new HostBuilder()
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.ConfigureServices(services =>
                    {
                        services.AddRouting();
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
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapGet("/authtest", async ctx => { await ctx.Response.WriteAsync("ok"); }).RequireAuthorization();
                        });
                    });
                })
                .StartAsync();

            var client = host.GetTestClient();
            var resp = await client.GetAsync("/authtest");
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Environment.SetEnvironmentVariable("X_API_KEY", null);
        }

        [Test]
        public async Task ValidApiKey_AllowsAccess()
        {
            Environment.SetEnvironmentVariable("X_API_KEY", "test-key-123");

            using var host = await new HostBuilder()
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.ConfigureServices(services =>
                    {
                        services.AddRouting();
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
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapGet("/authtest", async ctx => { await ctx.Response.WriteAsync("ok"); }).RequireAuthorization();
                        });
                    });
                })
                .StartAsync();

            var client = host.GetTestClient();
            client.DefaultRequestHeaders.Add("X-API-KEY", "test-key-123");
            var resp = await client.GetAsync("/authtest");
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Environment.SetEnvironmentVariable("X_API_KEY", null);
        }

        [Test]
        public async Task InvalidApiKey_Returns_Unauthorized()
        {
            Environment.SetEnvironmentVariable("X_API_KEY", "real-key");

            using var host = await new HostBuilder()
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.ConfigureServices(services =>
                    {
                        services.AddRouting();
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
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapGet("/authtest", async ctx => { await ctx.Response.WriteAsync("ok"); }).RequireAuthorization();
                        });
                    });
                })
                .StartAsync();

            var client = host.GetTestClient();
            client.DefaultRequestHeaders.Add("X-API-KEY", "wrong-key");
            var resp = await client.GetAsync("/authtest");
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public async Task ConfigApiKey_AllowsAccess()
        {
            Environment.SetEnvironmentVariable("X_API_KEY", null);

            using var host = await new HostBuilder()
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.ConfigureServices(services =>
                    {
                        services.AddRouting();
                        // Provide configuration with X-API-KEY
                        var cfg = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                            .AddInMemoryCollection(new[] { new System.Collections.Generic.KeyValuePair<string, string>("X-API-KEY", "cfg-key") })
                            .Build();
                        services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(cfg);

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
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapGet("/authtest", async ctx => { await ctx.Response.WriteAsync("ok"); }).RequireAuthorization();
                        });
                    });
                })
                .StartAsync();

            var client = host.GetTestClient();
            client.DefaultRequestHeaders.Add("X-API-KEY", "cfg-key");
            var resp = await client.GetAsync("/authtest");
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Environment.SetEnvironmentVariable("X_API_KEY", null);
        }

        [Test]
        public async Task ServiceFallback_AllowsAccess()
        {
            Environment.SetEnvironmentVariable("X_API_KEY", null);

            using var host = await new HostBuilder()
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.ConfigureServices(services =>
                    {
                        services.AddRouting();
                        // Register a test IApiKeyService that returns the key
                        services.AddSingleton<Asionyx.Library.Core.IApiKeyService>(sp => new TestApiKeyService("svc-key"));

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
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapGet("/authtest", async ctx => { await ctx.Response.WriteAsync("ok"); }).RequireAuthorization();
                        });
                    });
                })
                .StartAsync();

            var client = host.GetTestClient();
            client.DefaultRequestHeaders.Add("X-API-KEY", "svc-key");
            var resp = await client.GetAsync("/authtest");
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Environment.SetEnvironmentVariable("X_API_KEY", null);
        }

        [Test]
        public async Task HeaderProvidedButNoExpectedKey_Returns_Unauthorized()
        {
            // No env, no config, no service
            Environment.SetEnvironmentVariable("X_API_KEY", null);

            using var host = await new HostBuilder()
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.ConfigureServices(services =>
                    {
                        services.AddRouting();
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
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapGet("/authtest", async ctx => { await ctx.Response.WriteAsync("ok"); }).RequireAuthorization();
                        });
                    });
                })
                .StartAsync();

            var client = host.GetTestClient();
            client.DefaultRequestHeaders.Add("X-API-KEY", "some-key");
            var resp = await client.GetAsync("/authtest");
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Environment.SetEnvironmentVariable("X_API_KEY", null);
        }

        [Test]
        public async Task CaseInsensitive_AcceptsDifferentCasing()
        {
            Environment.SetEnvironmentVariable("X_API_KEY", "AbCdEf");

            using var host = await new HostBuilder()
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.ConfigureServices(services =>
                    {
                        services.AddRouting();
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
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapGet("/authtest", async ctx => { await ctx.Response.WriteAsync("ok"); }).RequireAuthorization();
                        });
                    });
                })
                .StartAsync();

            var client = host.GetTestClient();
            client.DefaultRequestHeaders.Add("X-API-KEY", "abcdef");
            var resp = await client.GetAsync("/authtest");
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Environment.SetEnvironmentVariable("X_API_KEY", null);
        }
    }
}
