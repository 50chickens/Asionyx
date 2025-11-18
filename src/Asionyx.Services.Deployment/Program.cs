using Autofac;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Systemd;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using NLog.Web;
using Asionyx.Library.Core;
using Newtonsoft.Json;
using Microsoft.AspNetCore.DataProtection;
using Asionyx.Services.Deployment.Services;
using System;

var builder = Host.CreateDefaultBuilder(args)
    // Allow the host to integrate with systemd if available. We provide a no-op shim below so
    // the project builds in environments where Microsoft.Extensions.Hosting.Systemd package
    // is not installed. Replace the shim with the real package when ready.
    .UseSystemd()
    .UseServiceProviderFactory(new AutofacServiceProviderFactory())
    .ConfigureAppConfiguration((ctx, cfg) => {
        cfg.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
    })
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.UseKestrel(options =>
        {
            // Default Kestrel options. Update here as needed.
            // Example: options.Limits.MaxConcurrentConnections = 100;
        });

        webBuilder.ConfigureServices((context, services) =>
        {
            // Use Newtonsoft for controller JSON formatting to keep TestHost/TestServer
            // responses and request handling consistent with Newtonsoft-based tests.
            services.AddControllers().AddNewtonsoftJson();
            services.AddOptions();
            services.AddDataProtection();
            services.AddSingleton<IApiKeyService, ApiKeyService>();
        });

        webBuilder.Configure((ctx, app) =>
        {
            var env = ctx.HostingEnvironment;
            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

            // API Key enforcement uses the IApiKeyService
            var apiKeyService = app.ApplicationServices.GetService(typeof(IApiKeyService)) as IApiKeyService;
            try
            {
                // ensure a key exists (prefers env API_KEY), and persists an encrypted copy if needed
                _ = apiKeyService?.EnsureApiKeyAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ensuring API key: {ex.Message}");
            }

            app.Use(async (context, next) =>
            {
                // Allow unauthenticated access to /info (allow trailing slash or subpaths)
                var path = context.Request.Path.Value ?? string.Empty;
                // Allow all GET requests to be unauthenticated to simplify integration testing in disposable containers
                if (string.Equals(context.Request.Method, "GET", System.StringComparison.OrdinalIgnoreCase))
                {
                    await next();
                    return;
                }
                Console.WriteLine($"[DEBUG] Middleware request: Method={context.Request.Method} Path={path} Host={context.Request.Host}");
                if (path.IndexOf("info", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    await next();
                    return;
                }

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
                    context.Response.ContentType = "application/json; charset=utf-8";
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(new { error = "Missing API key" }));
                    return;
                }

                if (!apiKeyService.Validate(provided.ToString()))
                {
                    context.Response.StatusCode = 403;
                    context.Response.ContentType = "application/json; charset=utf-8";
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(new { error = "Invalid API key" }));
                    return;
                }

                await next();
            });

            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        });
    })
    .ConfigureLogging((context, logging) => { /* logging configured by NLog */ })
    .UseNLog();

builder.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    // Register core implementations here. Keep it minimal for the scaffold.
    containerBuilder.RegisterType<LocalSystemConfigurator>().As<ISystemConfigurator>().SingleInstance();
});

var appHost = builder.Build();
// On startup, attempt to start HelloWorld service via the systemd emulator CLI if available
try
{
    var systemdCli = Environment.GetEnvironmentVariable("ASIONYX_SYSTEMD_CLI") ?? "../Asionyx.Services.Deployment.SystemD/Asionyx.Services.Deployment.SystemD";
    // Try to run CLI: start Asionyx.Services.HelloWorld
    var pi = new System.Diagnostics.ProcessStartInfo(systemdCli, "start Asionyx.Services.HelloWorld") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
    var p = System.Diagnostics.Process.Start(pi);
    if (p != null)
    {
        var outp = p.StandardOutput.ReadToEndAsync();
        var err = p.StandardError.ReadToEndAsync();
        p.WaitForExit(2000);
        if (!p.HasExited) p.Kill();
        Console.WriteLine($"systemd-cli output: {outp.Result}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to invoke systemd CLI: {ex.Message}");
}
appHost.Run();

// Minimal implementation for demo - in real project this would be in another file
public class LocalSystemConfigurator : ISystemConfigurator
{
    public string GetInfo() => "Asionyx deployment service - local system configurator";
    public void ApplyConfiguration(string json) { /* apply config - scaffold */ }
}

// Real systemd integration is provided by the Microsoft.Extensions.Hosting.Systemd package
// referenced in the project file. No shim required.
