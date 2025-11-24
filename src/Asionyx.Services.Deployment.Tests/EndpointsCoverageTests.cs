using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Asionyx.Library.Core;
using Asionyx.Library.Shared.Diagnostics;
using Asionyx.Services.Deployment.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Asionyx.Services.Deployment.Tests
{
    [TestFixture]
    public class EndpointsCoverageTests
    {
        private IHostBuilder CreateHostBuilder()
        {
            Environment.SetEnvironmentVariable("API_KEY", "valid-key");
            Environment.SetEnvironmentVariable("API_KEY", "valid-key");
            return new HostBuilder().ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureAppConfiguration(cfg => { /* enforce auth; no Testing:Insecure */ });
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
                    // Provide process runner for controller activation in test host
                    services.AddSingleton<Asionyx.Services.Deployment.Services.IProcessRunner, Asionyx.Services.Deployment.Services.DefaultProcessRunner>();
                    services.AddSingleton<IApiKeyService>(new TestApiKeyService("valid-key"));
                    services.AddSingleton<IAppDiagnostics>(new TestAppDiagnostics());
                    services.AddSingleton(typeof(ILog<>), typeof(NLogLoggerCore<>));
                });

                webHost.Configure(app =>
                {
                    app.UseCorrelationId();
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(e => e.MapControllers());
                });
            });
        }

        [Test]
        public async Task Info_Health_Status_Get_Returns_Ok()
        {
            using var host = await CreateHostBuilder().StartAsync();
            var client = host.GetTestClient();

            var r1 = await client.GetAsync("/info");
            Assert.That(r1.StatusCode, Is.EqualTo(HttpStatusCode.OK), "/info should return 200");

            var r2 = await client.GetAsync("/healthz");
            Assert.That(r2.StatusCode, Is.EqualTo(HttpStatusCode.OK), "/healthz should return 200");

            var r3 = await client.GetAsync("/status");
            Assert.That(r3.StatusCode, Is.EqualTo(HttpStatusCode.OK), "/status should return 200");

            await host.StopAsync();
        }

        [Test]
        public async Task Packages_Get_And_Post_Validation()
        {
            using var host = await CreateHostBuilder().StartAsync();
            var client = host.GetTestClient();

            // GET /packages: may return 200 (if dpkg available) or 500 when dpkg missing; assert either
            var gReq = new HttpRequestMessage(HttpMethod.Get, "/packages");
            gReq.Headers.Add("X-Api-Key", "valid-key");
            var g = await client.SendAsync(gReq);
            Assert.That((int)g.StatusCode, Is.EqualTo(200).Or.EqualTo(500));

            // POST /packages without body but with API key -> BadRequest
            var req = new HttpRequestMessage(HttpMethod.Post, "/packages") { Content = new StringContent("{}", Encoding.UTF8, "application/json") };
            req.Headers.Add("X-Api-Key", "valid-key");
            var p = await client.SendAsync(req);
            Assert.That(p.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "POST /packages with invalid body should return BadRequest");

            await host.StopAsync();
        }

        [Test]
        public async Task Package_Post_NonNupkg_Returns_BadRequest()
        {
            using var host = await CreateHostBuilder().StartAsync();
            var client = host.GetTestClient();

            var ms = new MemoryStream(Encoding.UTF8.GetBytes("hello"));
            var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(ms);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(streamContent, "file", "upload.txt");

            var req = new HttpRequestMessage(HttpMethod.Post, "/package") { Content = content };
            req.Headers.Add("X-Api-Key", "valid-key");

            var r = await client.SendAsync(req);
            Assert.That(r.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Uploading non-.nupkg should return BadRequest");

            await host.StopAsync();
        }

        [Test]
        public async Task Files_Write_List_Delete_Works()
        {
            using var host = await CreateHostBuilder().StartAsync();
            var client = host.GetTestClient();

            var tmp = Path.Combine(Path.GetTempPath(), "asionyx_test");
            if (!Directory.Exists(tmp)) Directory.CreateDirectory(tmp);
            var testFile = Path.Combine(tmp, "tf.txt");

            // Write
            var writeBody = JsonConvert.SerializeObject(new { Action = "write", Path = testFile, Content = "xyz" });
            var writeReq = new HttpRequestMessage(HttpMethod.Post, "/filesystem/files") { Content = new StringContent(writeBody, Encoding.UTF8, "application/json") };
            writeReq.Headers.Add("X-Api-Key", "valid-key");
            var wr = await client.SendAsync(writeReq);
            Assert.That(wr.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Write should return OK");
            Assert.That(File.Exists(testFile), Is.True, "File should exist after write");
            var content = File.ReadAllText(testFile);
            Assert.That(content, Is.EqualTo("xyz"));

            // List directory
            var listBody = JsonConvert.SerializeObject(new { Action = "list", Path = tmp });
            var listReq = new HttpRequestMessage(HttpMethod.Post, "/filesystem/files") { Content = new StringContent(listBody, Encoding.UTF8, "application/json") };
            listReq.Headers.Add("X-Api-Key", "valid-key");
            var lr = await client.SendAsync(listReq);
            Assert.That(lr.StatusCode, Is.EqualTo(HttpStatusCode.OK), "List should return OK");

            // Delete
            var delBody = JsonConvert.SerializeObject(new { Action = "delete", Path = testFile });
            var delReq = new HttpRequestMessage(HttpMethod.Post, "/filesystem/files") { Content = new StringContent(delBody, Encoding.UTF8, "application/json") };
            delReq.Headers.Add("X-Api-Key", "valid-key");
            var dr = await client.SendAsync(delReq);
            Assert.That(dr.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Delete should return OK");
            Assert.That(File.Exists(testFile), Is.False, "File should be removed after delete");

            // Cleanup
            try { Directory.Delete(tmp, true); } catch { }

            await host.StopAsync();
        }

        [Test]
        public async Task Systemd_Post_Returns_500_When_Exec_Missing()
        {
            using var host = await CreateHostBuilder().StartAsync();
            var client = host.GetTestClient();

            var body = JsonConvert.SerializeObject(new { Action = "start", Name = "foo" });
            var req = new HttpRequestMessage(HttpMethod.Post, "/systemd") { Content = new StringContent(body, Encoding.UTF8, "application/json") };
            req.Headers.Add("X-Api-Key", "valid-key");
            var r = await client.SendAsync(req);
            Assert.That(r.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError), "/systemd should return 500 when exec missing");

            await host.StopAsync();
        }
    }
}
