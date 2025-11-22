using System.Runtime.InteropServices;
using Asionyx.Library.Shared.Diagnostics;

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
    // Always use console diagnostics; do not write diagnostics/log files into the container.
    return new ConsoleDiagnostics();
});

var app = builder.Build();

// No startup diagnostics file is written; diagnostics are only output to console.

// Exception-capturing middleware for HelloWorld: log to console only.
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Exception] {DateTime.UtcNow:o} {ex}");
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("internal");
    }
});

app.MapGet("/info", () => Results.Ok(new { Service = "Asionyx.Services.HelloWorld", Message = "Hello from HelloWorld service" }));

app.Run();
