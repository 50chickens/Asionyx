using Asionyx.Library.Shared.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

var builder = WebApplication.CreateBuilder(args);

// If running on Linux with systemd available, this will enable systemd integration when the package is present.
try
{
    builder.Host.UseSystemd();
}
catch { /* optional: ignore if not available */ }

// Register diagnostics writer. Directory can be overridden with configuration `Diagnostics:Dir` or `Diagnostics:ToStdout`.
builder.Services.AddSingleton<IAppDiagnostics>(sp =>
{
    var cfg = sp.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
    var useStdout = cfg?["Diagnostics:ToStdout"];
    if (!string.IsNullOrWhiteSpace(useStdout) && (useStdout == "1" || useStdout.Equals("true", StringComparison.OrdinalIgnoreCase)))
    {
        return new ConsoleDiagnostics();
    }

    var dirCfg = cfg?["Diagnostics:Dir"];
    string dir;
    if (!string.IsNullOrWhiteSpace(dirCfg)) dir = dirCfg!;
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) dir = Path.Combine(Path.GetTempPath(), "asionyx", "diagnostics");
    else dir = "/var/asionyx/diagnostics";
    return new FileDiagnostics(dir);
});

var app = builder.Build();

// Write a small startup diagnostics file to aid container-run post-mortems.
try
{
    var diag = app.Services.GetService(typeof(IAppDiagnostics)) as IAppDiagnostics;
    diag?.WriteAsync("startup", new { Timestamp = DateTime.UtcNow, Message = "HelloWorld started" }).GetAwaiter().GetResult();
}
catch
{
    // don't let diagnostics failures block startup
}

// Exception-capturing middleware for HelloWorld so failures produce diagnostics files.
app.Use(async (context, next) =>
{
    var diag = context.RequestServices.GetService(typeof(IAppDiagnostics)) as IAppDiagnostics;
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        try
        {
            if (diag != null)
            {
                var name = $"exception_{DateTime.UtcNow:yyyyMMddHHmmssfff}";
                var diagObj = new
                {
                    Timestamp = DateTime.UtcNow,
                    Exception = ex.ToString(),
                    Path = context.Request?.Path.Value,
                    Method = context.Request?.Method
                };
                diag.WriteAsync(name, diagObj).GetAwaiter().GetResult();
            }
        }
        catch { }

        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("internal");
    }
});

app.MapGet("/info", () => Results.Ok(new { Service = "Asionyx.Services.HelloWorld", Message = "Hello from HelloWorld service" }));

app.Run();
