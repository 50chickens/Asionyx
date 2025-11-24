using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Asionyx.Library.Core;
using Asionyx.Library.Shared.Diagnostics;
using Asionyx.Services.Deployment.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Asionyx.Services.Deployment.Tests
{
    [TestFixture]
    public class ErrorHandlingMiddlewareTests
    {
        [Test]
        public async Task UnhandledException_Returns_Sanitized_ErrorDto()
        {
            using var host = await new HostBuilder()
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.ConfigureServices(services =>
                    {
                        services.AddRouting();
                        services.AddSingleton<Asionyx.Services.Deployment.Services.IProcessRunner, Asionyx.Services.Deployment.Services.DefaultProcessRunner>();
                        services.AddSingleton<IAppDiagnostics, ConsoleDiagnostics>();
                        services.AddSingleton(typeof(ILog<>), typeof(NLogLoggerCore<>));
                    });

                    webHost.Configure(app =>
                    {
                        app.UseCorrelationId();
                        app.UseErrorHandling();
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapGet("/boom", context => throw new InvalidOperationException("boom"));
                        });
                    });
                })
                .StartAsync();

            var client = host.GetTestClient();
            var resp = await client.GetAsync("/boom");

            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
            var body = await resp.Content.ReadAsStringAsync();
            dynamic? obj = JsonConvert.DeserializeObject(body);
            Assert.That((string?)obj?.Error, Is.EqualTo("Internal server error"));
            // Detailed stack traces must not be leaked to clients
            Assert.That((string?)obj?.Detail, Is.Null);
        }
    }
}
