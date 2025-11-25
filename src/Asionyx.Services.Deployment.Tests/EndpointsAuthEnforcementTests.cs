using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Asionyx.Library.Core;
using Asionyx.Services.Deployment.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Asionyx.Services.Deployment.Tests
{
    [TestFixture]
    public class EndpointsAuthEnforcementTests
    {
        private readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(30);
        private readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);
        private readonly TestDiagnostics _diag = new TestDiagnostics();

        private async Task<IHost> BuildAndStartHostAsync(CancellationToken ct)
        {
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.ConfigureAppConfiguration(cfg => { });
                    webHost.ConfigureServices(services =>
                    {
                        services.AddControllers()
                            .AddApplicationPart(typeof(Asionyx.Services.Deployment.Controllers.InfoController).Assembly)
                            .AddNewtonsoftJson();
                        services.AddRouting();
                        services.AddAuthorization();
                        services.AddAuthentication(options =>
                        {
                            options.DefaultAuthenticateScheme = Asionyx.Services.Deployment.Security.ApiKeyAuthenticationHandler.SchemeName;
                            options.DefaultChallengeScheme = Asionyx.Services.Deployment.Security.ApiKeyAuthenticationHandler.SchemeName;
                        }).AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, Asionyx.Services.Deployment.Security.ApiKeyAuthenticationHandler>(
                            Asionyx.Services.Deployment.Security.ApiKeyAuthenticationHandler.SchemeName, options => { });
                        services.AddSingleton<TimeProvider>(TimeProvider.System);
                        services.AddSingleton<ISystemConfigurator, TestSystemConfigurator>();
                        // Ensure process runner available for controller activation in tests
                        services.AddSingleton<Asionyx.Services.Deployment.Services.IProcessRunner, Asionyx.Services.Deployment.Services.DefaultProcessRunner>();
                        services.AddSingleton<IApiKeyService>(new TestApiKeyService("valid-key"));
                        services.AddSingleton(typeof(ILog<>), typeof(NLogLoggerCore<>));
                    });

                    webHost.Configure(app =>
                    {
                        app.UseCorrelationId();
                        app.UseRouting();
                        app.UseAuthentication();
                        app.UseAuthorization();
                        app.UseEndpoints(endpoints => endpoints.MapControllers());
                    });
                });

            var startTask = hostBuilder.StartAsync(ct);
            var completed = await Task.WhenAny(startTask, Task.Delay(TestTimeout, ct));
            if (completed != startTask)
            {
                var msg = $"Host failed to start within {TestTimeout}";
                _diag.AppendDiag(msg);
                throw new TimeoutException(msg);
            }

            return await startTask;
        }

        private async Task<T> WithTimeout<T>(Task<T> task, TimeSpan timeout, CancellationToken ct)
        {
            var completed = await Task.WhenAny(task, Task.Delay(timeout, ct));
            if (completed != task)
            {
                throw new TimeoutException($"Operation did not complete within {timeout}");
            }
            return await task; // already completed
        }

        [TestCase("GET", "/info", false, 200)]
        [TestCase("POST", "/packages", false, 401)]
        [TestCase("POST", "/packages", true, -1)]
        public async Task Endpoint_Auth_Behavior(string method, string path, bool provideKey, int expectedStatus)
        {
            using var cts = new CancellationTokenSource(TestTimeout);
            _diag.AppendDiag($"[Test] Endpoint_Auth_Behavior start method={method} path={path} provideKey={provideKey} {DateTime.UtcNow:O}");
            using var host = await BuildAndStartHostAsync(cts.Token);
            var client = host.GetTestClient();
            client.Timeout = RequestTimeout + TimeSpan.FromSeconds(5);

            HttpResponseMessage resp;
            if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
            {
                var req = new HttpRequestMessage(HttpMethod.Get, path);
                if (provideKey)
                {
                    req.Headers.Add("X-Api-Key", "valid-key");
                }
                resp = await WithTimeout(client.SendAsync(req, cts.Token), RequestTimeout, cts.Token);
            }
            else
            {
                var postContent = new StringContent(JsonConvert.SerializeObject(new { Action = "list", Packages = new string[] { "pkg" } }), Encoding.UTF8, "application/json");
                var req = new HttpRequestMessage(new HttpMethod(method), path) { Content = postContent };
                if (provideKey)
                {
                    req.Headers.Add("X-Api-Key", "valid-key");
                }
                resp = await WithTimeout(client.SendAsync(req, cts.Token), RequestTimeout, cts.Token);
            }

            _diag.AppendDiag($"[Test] Response status {(int)resp.StatusCode} for method={method} path={path} provideKey={provideKey}");

            if (expectedStatus == -1)
            {
                Assert.That((int)resp.StatusCode, Is.Not.EqualTo(401), "Expected not 401");
            }
            else
            {
                Assert.That((int)resp.StatusCode, Is.EqualTo(expectedStatus));
            }
        }
    }
}
