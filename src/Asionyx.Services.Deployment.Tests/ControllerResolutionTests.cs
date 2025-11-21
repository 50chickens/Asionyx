using System.Collections.Generic;
using Asionyx.Services.Deployment.Middleware;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using NUnit.Framework;
using Asionyx.Services.Deployment.Controllers;
using Asionyx.Library.Core;

namespace Asionyx.Services.Deployment.Tests
{
    [TestFixture]
    public class ControllerResolutionTests
    {
        [Test]
        public void Resolve_All_Controllers_From_DI()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { { "Testing:Insecure", "false" } })
                .Build();

            services.AddSingleton<IConfiguration>(config);
            services.AddOptions();
            services.AddControllers().AddNewtonsoftJson();

            // Register known concrete dependencies used by controllers
            services.AddSingleton<ISystemConfigurator, TestSystemConfigurator>();

            var provider = services.BuildServiceProvider();

            var controllerTypes = typeof(InfoController).Assembly
                .GetTypes()
                .Where(t => t.IsClass && t.Name.EndsWith("Controller"));

            foreach (var type in controllerTypes)
            {
                // Use ActivatorUtilities so DI is used for constructor injection
                var instance = Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance(provider, type);
                Assert.That(instance, Is.Not.Null, $"Failed to resolve controller {type.FullName}");
            }
        }

        [Test]
        public async Task Endpoints_Auth_Enforcement_Behavior()
        {
            // Build an in-memory host with the same middleware semantics for auth used in Program
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.ConfigureAppConfiguration(cfg => cfg.AddInMemoryCollection(new Dictionary<string, string?> { { "Testing:Insecure", "false" } }));
                    webHost.ConfigureServices(services =>
                    {
                        services.AddSingleton<IConfiguration>(sp => sp.GetRequiredService<IConfiguration>());
                        services.AddControllers().AddNewtonsoftJson();
                        services.AddSingleton<ISystemConfigurator, TestSystemConfigurator>();
                        // Provide a simple IApiKeyService for testing
                        services.AddSingleton<Asionyx.Library.Core.IApiKeyService>(new TestApiKeyService("valid-key"));
                        services.AddSingleton(typeof(Asionyx.Library.Core.ILog<>), typeof(Asionyx.Library.Core.NLogLoggerCore<>));
                    });

                    webHost.Configure(app =>
                    {
                        // Minimal middleware that mirrors Program's API key enforcement rules
                        app.UseCorrelationId();

                        app.Use(async (context, next) =>
                        {
                            // replicate Testing:Insecure check
                            var cfg = context.RequestServices.GetService<IConfiguration>();
                            var insecure = cfg?["Testing:Insecure"];
                            if (!string.IsNullOrWhiteSpace(insecure) && (insecure == "1" || insecure.Equals("true", System.StringComparison.OrdinalIgnoreCase)))
                            {
                                await next();
                                return;
                            }

                            var path = context.Request.Path.Value ?? string.Empty;
                            if (string.Equals(context.Request.Method, "GET", System.StringComparison.OrdinalIgnoreCase))
                            {
                                await next();
                                return;
                            }

                            if (path.IndexOf("info", System.StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                await next();
                                return;
                            }

                            var apiKeyService = context.RequestServices.GetService<Asionyx.Library.Core.IApiKeyService>();
                            if (apiKeyService == null)
                            {
                                context.Response.StatusCode = 500;
                                context.Response.ContentType = "application/json; charset=utf-8";
                                await context.Response.WriteAsync(JsonConvert.SerializeObject(new { error = "API key service not available" }));
                                return;
                            }

                            if (!context.Request.Headers.TryGetValue("X-API-KEY", out var provided) || string.IsNullOrWhiteSpace(provided))
                            {
                                context.Response.StatusCode = 401;
                                await context.Response.WriteAsJsonAsync(new { error = "Missing API key" });
                                return;
                            }

                                context.Response.ContentType = "application/json; charset=utf-8";
                                await context.Response.WriteAsync(JsonConvert.SerializeObject(new { error = "Missing API key" }));
                            {
                                context.Response.StatusCode = 403;
                                await context.Response.WriteAsJsonAsync(new { error = "Invalid API key" });
                                return;
                            }

                                context.Response.ContentType = "application/json; charset=utf-8";
                                await context.Response.WriteAsync(JsonConvert.SerializeObject(new { error = "Invalid API key" }));
                        });

                        app.UseRouting();
                        app.UseEndpoints(endpoints => endpoints.MapControllers());
                    });
                });

            using var host = await hostBuilder.StartAsync();
            var client = host.GetTestClient();

            // 1) /info is GET and should be 200 without API key
            var r1 = await client.GetAsync("/info");
            Assert.That((int)r1.StatusCode, Is.EqualTo(200), "GET /info should return 200 OK without API key");

            // 2) POST /packages requires API key. Without key -> 401
            var postContent = new StringContent(JsonConvert.SerializeObject(new { Action = "list", Packages = new string[] { "pkg" } }), Encoding.UTF8, "application/json");
            var r2 = await client.PostAsync("/packages", postContent);
            Assert.That((int)r2.StatusCode, Is.EqualTo(401), "POST /packages without API key should return 401 Unauthorized");

            // 3) POST /packages with API key should NOT be 401 (may be 400/200 depending on controller behavior)
            var req = new HttpRequestMessage(HttpMethod.Post, "/packages") { Content = postContent };
            req.Headers.Add("X-API-KEY", "valid-key");
            var r3 = await client.SendAsync(req);
            Assert.That((int)r3.StatusCode, Is.Not.EqualTo(401), "POST /packages with API key should not return 401 Unauthorized");

            await host.StopAsync();
        }

        private class TestApiKeyService : Asionyx.Library.Core.IApiKeyService
        {
            private readonly string _valid;
            public TestApiKeyService(string valid) { _valid = valid; }
            public bool Validate(string key) => key == _valid;
            public System.Threading.Tasks.Task<string> EnsureApiKeyAsync() => System.Threading.Tasks.Task.FromResult(_valid);
        }

        private class TestSystemConfigurator : ISystemConfigurator
        {
            public string GetInfo() => "test-config";
            public void ApplyConfiguration(string json) { }
        }
    }
}
