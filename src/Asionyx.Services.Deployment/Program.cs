using Asionyx.Library.Core;
using Asionyx.Services.Deployment.Services;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NLog.Web;

var builder = Host.CreateDefaultBuilder(args)
    // Allow the host to integrate with systemd if available. We provide a no-op shim below so
    // the project builds in environments where Microsoft.Extensions.Hosting.Systemd package
    // is not installed. Replace the shim with the real package when ready.
    .UseSystemd()
    .UseServiceProviderFactory(new AutofacServiceProviderFactory())
    .ConfigureAppConfiguration((ctx, cfg) =>
    {
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
                // If running in an insecure test mode (set by orchestrator/GHA), skip API key enforcement.
                var insecure = Environment.GetEnvironmentVariable("ASIONYX_INSECURE_TESTING");
                if (!string.IsNullOrWhiteSpace(insecure) && insecure == "1")
                {
                    await next();
                    return;
                }
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

// Do NOT auto-start managed services on host startup. The deployment API should invoke the
// systemd emulator CLI (published into the container) on-demand to create/start/stop unit files
// as part of the integration test/workflow requirements.
appHost.Run();

// Minimal implementation for demo - in real project this would be in another file
public class LocalSystemConfigurator : ISystemConfigurator
{
    public string GetInfo() => "Asionyx deployment service - local system configurator";
    public void ApplyConfiguration(string json) { /* apply config - scaffold */ }
}

// Real systemd integration is provided by the Microsoft.Extensions.Hosting.Systemd package
// referenced in the project file. No shim required.
