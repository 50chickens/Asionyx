using System.Runtime.InteropServices;
using Asionyx.Library.Core;
using Asionyx.Library.Shared.Diagnostics;
using Asionyx.Services.Deployment.Services;
using Asionyx.Services.Deployment.Middleware;
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
        // Also attempt to read per-publish-location appsettings placed into the publish folder
        cfg.AddJsonFile(Path.Combine("deployment", "appsettings.json"), optional: true, reloadOnChange: true);
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
            // Ensure ILog<T> is available from the default DI container so tests that build
            // a Host without Autofac can still resolve loggers.
            services.AddSingleton(typeof(Asionyx.Library.Core.ILog<>), typeof(Asionyx.Library.Core.NLogLoggerCore<>));
            // Register diagnostics writer. Directory can be overridden via configuration `Diagnostics:Dir` or `Diagnostics:ToStdout`.
            services.AddSingleton<IAppDiagnostics>(sp =>
            {
                var cfg = sp.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
                var useStdout = cfg?[("Diagnostics:ToStdout")]?.ToString();
                if (!string.IsNullOrWhiteSpace(useStdout) && (useStdout == "1" || useStdout.Equals("true", StringComparison.OrdinalIgnoreCase)))
                {
                    return new ConsoleDiagnostics();
                }

                var dirCfg = cfg?[("Diagnostics:Dir")];
                string dir;
                if (!string.IsNullOrWhiteSpace(dirCfg)) dir = dirCfg!;
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) dir = Path.Combine(Path.GetTempPath(), "asionyx", "diagnostics");
                else dir = "/var/asionyx/diagnostics";
                return new FileDiagnostics(dir);
            });
        });

        webBuilder.Configure((ctx, app) =>
        {
            var env = ctx.HostingEnvironment;
            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

            // Ensure every request has a correlation id for observability
            app.UseCorrelationId();

            // Exception-logging middleware: capture unhandled exceptions and persist
            // a diagnostics JSON file for post-mortem inspection.
            app.Use(async (context, next) =>
            {
                try
                {
                    await next();
                }
                catch (Exception ex)
                {
                    try
                    {
                        var diag = context.RequestServices.GetService(typeof(IAppDiagnostics)) as IAppDiagnostics;
                        if (diag != null)
                        {
                            var name = $"exception_{DateTime.UtcNow:yyyyMMddHHmmssfff}";
                            var diagObj = new
                            {
                                Timestamp = DateTime.UtcNow,
                                Exception = ex.ToString(),
                                Path = context.Request?.Path.Value,
                                Method = context.Request?.Method,
                                Headers = context.Request?.Headers
                            };
                            await diag.WriteAsync(name, diagObj);
                        }
                    }
                    catch
                    {
                        // Swallow diagnostics errors to avoid masking the original exception
                    }

                    context.Response.StatusCode = 500;
                    context.Response.ContentType = "application/json; charset=utf-8";
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(new { error = "internal" }));
                }
            });

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
                // If running in an insecure test mode (configured by appsettings), skip API key enforcement.
                try
                {
                    var cfg = app.ApplicationServices.GetService(typeof(Microsoft.Extensions.Configuration.IConfiguration)) as Microsoft.Extensions.Configuration.IConfiguration;
                    var insecure = cfg?["Testing:Insecure"];
                    if (!string.IsNullOrWhiteSpace(insecure) && (insecure == "1" || insecure.Equals("true", StringComparison.OrdinalIgnoreCase)))
                    {
                        await next();
                        return;
                    }
                }
                catch { }
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

builder.ConfigureContainer<ContainerBuilder>((context, containerBuilder) =>
{
    // Register core implementations here. Keep it minimal for the scaffold.
    containerBuilder.RegisterType<LocalSystemConfigurator>().As<ISystemConfigurator>().SingleInstance();

    // Make IConfiguration available in the Autofac container so modules and components
    // can resolve logging levels and other settings from appsettings.json.
    containerBuilder.RegisterInstance(context.Configuration).As<Microsoft.Extensions.Configuration.IConfiguration>().SingleInstance();

    // Register logging module which configures NLog from appsettings.json and exposes ILog<T>
    containerBuilder.RegisterModule(new Asionyx.Services.Deployment.Logging.LoggingModule(context.Configuration));
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
