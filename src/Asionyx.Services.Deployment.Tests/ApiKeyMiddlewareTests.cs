using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Asionyx.Services.Deployment.Services;
using Asionyx.Library.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;

namespace Asionyx.Services.Deployment.Tests;

[TestFixture]
public class ApiKeyMiddlewareTests
{
    [SetUp]
    public void SetUp()
    {
        // Ensure deterministic API key for the lifetime of the factory
        Environment.SetEnvironmentVariable("API_KEY", "test-key");
    }

    [TearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable("API_KEY", null);
    }

    [Test]
    public async Task ValidApiKey_AllowsPost()
    {
        // Build a small in-memory app that uses the same ApiKeyService and middleware logic,
        // but exposes a simple POST endpoint that returns plain text (avoids MVC JSON output issues).
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
        builder.WebHost.UseTestServer();
        builder.Services.AddDataProtection();
        builder.Services.AddSingleton<IApiKeyService, ApiKeyService>();

        var app = builder.Build();

        // ensure key exists
        var apiKeyService = app.Services.GetService(typeof(IApiKeyService)) as IApiKeyService;
        _ = apiKeyService?.EnsureApiKeyAsync().GetAwaiter().GetResult();

        app.Use(async (context, next) =>
        {
            if (string.Equals(context.Request.Method, "GET", StringComparison.OrdinalIgnoreCase))
            {
                await next();
                return;
            }

            if (!context.Request.Headers.TryGetValue("X-API-KEY", out var provided) || string.IsNullOrWhiteSpace(provided))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Missing");
                return;
            }

            if (!apiKeyService!.Validate(provided.ToString()))
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Invalid");
                return;
            }

            await next();
        });

        app.MapPost("/test-protected", async ctx =>
        {
            await ctx.Response.WriteAsync("ok");
        });

        await app.StartAsync();
        var client = app.GetTestClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/test-protected");
        request.Headers.Add("X-API-KEY", "test-key");

        var resp = await client.SendAsync(request);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK), await resp.Content.ReadAsStringAsync());

        await app.StopAsync();
    }

    [Test]
    public async Task MissingOrInvalidKey_Returns401or403()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
        builder.WebHost.UseTestServer();
        builder.Services.AddDataProtection();
        builder.Services.AddSingleton<IApiKeyService, ApiKeyService>();

        var app = builder.Build();
        var apiKeyService = app.Services.GetService(typeof(IApiKeyService)) as IApiKeyService;
        _ = apiKeyService?.EnsureApiKeyAsync().GetAwaiter().GetResult();

        app.Use(async (context, next) =>
        {
            if (string.Equals(context.Request.Method, "GET", StringComparison.OrdinalIgnoreCase))
            {
                await next();
                return;
            }

            if (!context.Request.Headers.TryGetValue("X-API-KEY", out var provided) || string.IsNullOrWhiteSpace(provided))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Missing");
                return;
            }

            if (!apiKeyService!.Validate(provided.ToString()))
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Invalid");
                return;
            }

            await next();
        });

        app.MapPost("/test-protected", async ctx => { await ctx.Response.WriteAsync("ok"); });
        await app.StartAsync();
        var client = app.GetTestClient();

        var r1 = new HttpRequestMessage(HttpMethod.Post, "/test-protected");
        var resp1 = await client.SendAsync(r1);
        Assert.That(resp1.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        var r2 = new HttpRequestMessage(HttpMethod.Post, "/test-protected");
        r2.Headers.Add("X-API-KEY", "bad-key");
        var resp2 = await client.SendAsync(r2);
        Assert.That(resp2.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        await app.StopAsync();
    }
}
